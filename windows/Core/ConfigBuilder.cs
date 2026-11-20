using System.Text.Json;
using System.Text.Json.Nodes;

namespace BatV2;

/// Server -> full Xray JSON (local socks + http inbounds, our outbound first = default route)
public static class ConfigBuilder
{
    static string Or(string s, string d) => string.IsNullOrEmpty(s) ? d : s;
    static JsonArray List(string s) =>
        new JsonArray(s.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).Select(x => (JsonNode)x).ToArray());

    public static string NormalizePin(string p) =>
        string.Join(",", p.Split(',').Select(x => new string(x.Where(c => c != ':' && !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant()).Where(x => x.Length > 0));

    public static JsonObject Outbound(Server s, string ip = null)
    {
        var addr = ip ?? s.Address;
        var fallback = ip != null ? s.Address : "";
        JsonObject settings;
        switch (s.Protocol)
        {
            case "vless":
                var u = new JsonObject { ["id"] = s.Secret, ["encryption"] = Or(s.Encryption, "none"), ["level"] = 8 };
                if (s.Flow.Length > 0) u["flow"] = s.Flow;
                settings = new JsonObject
                {
                    ["vnext"] = new JsonArray(new JsonObject
                    {
                        ["address"] = addr, ["port"] = s.Port, ["users"] = new JsonArray(u)
                    })
                };
                break;
            case "vmess":
                settings = new JsonObject
                {
                    ["vnext"] = new JsonArray(new JsonObject
                    {
                        ["address"] = addr, ["port"] = s.Port,
                        ["users"] = new JsonArray(new JsonObject
                        {
                            ["id"] = s.Secret, ["alterId"] = s.AlterId, ["security"] = Or(s.Method, "auto"), ["level"] = 8
                        })
                    })
                };
                break;
            case "trojan":
                settings = new JsonObject
                {
                    ["servers"] = new JsonArray(new JsonObject
                    {
                        ["address"] = addr, ["port"] = s.Port, ["password"] = s.Secret, ["level"] = 8
                    })
                };
                break;
            case "ss":
                settings = new JsonObject
                {
                    ["servers"] = new JsonArray(new JsonObject
                    {
                        ["address"] = addr, ["port"] = s.Port, ["method"] = s.Method, ["password"] = s.Secret, ["level"] = 8
                    })
                };
                break;
            default:
                throw new FormatException($"unsupported protocol '{s.Protocol}'");
        }
        return new JsonObject
        {
            ["tag"] = "proxy",
            ["protocol"] = s.Protocol == "ss" ? "shadowsocks" : s.Protocol,
            ["settings"] = settings,
            ["streamSettings"] = Stream(s, fallback)
        };
    }

    public static JsonObject FragmentOutbound() => new()
    {
        ["tag"] = "fragment",
        ["protocol"] = "freedom",
        ["settings"] = new JsonObject
        {
            ["fragment"] = new JsonObject { ["packets"] = "tlshello", ["length"] = "100-200", ["interval"] = "10-20" }
        },
        ["streamSettings"] = new JsonObject { ["sockopt"] = new JsonObject { ["tcpNoDelay"] = true } }
    };

    static JsonObject Stream(Server s, string fallback)
    {
        var net = s.Network.ToLowerInvariant();
        if (net == "raw") net = "tcp";
        var host = Or(s.Host, fallback);
        var path = Or(s.Path, "/");
        var m = new JsonObject { ["network"] = net };
        switch (net)
        {
            case "ws":
                var w = new JsonObject { ["path"] = path };
                if (host.Length > 0) w["headers"] = new JsonObject { ["Host"] = host };
                m["wsSettings"] = w;
                break;
            case "grpc":
                m["grpcSettings"] = new JsonObject { ["serviceName"] = s.Path, ["multiMode"] = s.Mode == "multi" };
                break;
            case "httpupgrade":
                var hu = new JsonObject { ["path"] = path };
                if (host.Length > 0) hu["host"] = host;
                m["httpupgradeSettings"] = hu;
                break;
            case "xhttp":
                var xh = new JsonObject { ["path"] = path };
                if (host.Length > 0) xh["host"] = host;
                if (s.Mode.Length > 0) xh["mode"] = s.Mode;
                m["xhttpSettings"] = xh;
                break;
            default:
                if (s.HeaderType == "http")
                    m["tcpSettings"] = new JsonObject
                    {
                        ["header"] = new JsonObject
                        {
                            ["type"] = "http",
                            ["request"] = new JsonObject
                            {
                                ["path"] = new JsonArray((JsonNode)path),
                                ["headers"] = new JsonObject { ["Host"] = List(s.Host) }
                            }
                        }
                    };
                break;
        }
        if (s.Fragment) m["sockopt"] = new JsonObject { ["dialerProxy"] = "fragment" };
        switch (s.Security.ToLowerInvariant())
        {
            case "tls":
                m["security"] = "tls";
                var t = new JsonObject();
                var sni = Or(s.Sni, fallback);
                if (sni.Length > 0) t["serverName"] = sni;
                if (s.Fp.Length > 0) t["fingerprint"] = s.Fp;
                if (s.Alpn.Length > 0) t["alpn"] = List(s.Alpn);
                if (s.Pin.Length > 0) t["pinnedPeerCertSha256"] = NormalizePin(s.Pin);
                if (s.Vcn.Length > 0) t["verifyPeerCertByName"] = s.Vcn;
                if (s.AllowInsecure) t["allowInsecure"] = true;
                m["tlsSettings"] = t;
                break;
            case "reality":
                m["security"] = "reality";
                m["realitySettings"] = new JsonObject
                {
                    ["serverName"] = s.Sni, ["fingerprint"] = Or(s.Fp, "chrome"),
                    ["publicKey"] = s.Pbk, ["shortId"] = s.Sid, ["spiderX"] = Or(s.Spx, "/")
                };
                break;
            default:
                m["security"] = "none";
                break;
        }
        return m;
    }

    public static string Full(Server s, string ip, int socks, int http, string logPath, string logLevel = "warning")
    {
        var outs = new JsonArray(Outbound(s, ip));
        if (s.Fragment) outs.Add(FragmentOutbound());
        outs.Add(new JsonObject { ["protocol"] = "freedom", ["settings"] = new JsonObject(), ["tag"] = "direct" });
        outs.Add(new JsonObject
        {
            ["protocol"] = "blackhole",
            ["settings"] = new JsonObject { ["response"] = new JsonObject { ["type"] = "http" } },
            ["tag"] = "block"
        });
        var root = new JsonObject
        {
            ["log"] = new JsonObject { ["loglevel"] = logLevel, ["error"] = logPath, ["access"] = "none" },
            ["dns"] = new JsonObject { ["servers"] = new JsonArray((JsonNode)"1.1.1.1", (JsonNode)"8.8.8.8") },
            ["inbounds"] = new JsonArray(
                new JsonObject
                {
                    ["listen"] = "127.0.0.1", ["port"] = socks, ["protocol"] = "socks", ["tag"] = "socks",
                    ["settings"] = new JsonObject { ["auth"] = "noauth", ["udp"] = true, ["userLevel"] = 8 }
                },
                new JsonObject
                {
                    ["listen"] = "127.0.0.1", ["port"] = http, ["protocol"] = "http", ["tag"] = "http",
                    ["settings"] = new JsonObject { ["userLevel"] = 8 }
                }),
            ["outbounds"] = outs,
            ["routing"] = new JsonObject { ["domainStrategy"] = "AsIs", ["rules"] = new JsonArray() }
        };
        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }
}
