using System.Text;
using System.Text.Json.Nodes;

namespace BatV2;

/// Parses vless:// vmess:// trojan:// ss:// share links. Never throws from ParseAll.
public static class LinkParser
{
    public static (List<Server> Ok, List<string> Bad) ParseAll(string text)
    {
        var ok = new List<Server>();
        var bad = new List<string>();
        foreach (var tok in (text ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
        {
            var t = tok.Trim();
            if (!t.Contains("://")) continue;
            try { ok.Add(ParseOne(t)); }
            catch (Exception e) { bad.Add((t.Length > 40 ? t.Substring(0, 40) : t) + " : " + e.Message); }
        }
        return (ok, bad);
    }

    public static Server ParseOne(string raw)
    {
        var line = raw.Trim();
        var scheme = line.Substring(0, line.IndexOf("://", StringComparison.Ordinal)).ToLowerInvariant();
        return scheme switch
        {
            "vless" or "trojan" => UriStyle(scheme, line),
            "vmess" => VMess(line),
            "ss" => Ss(line),
            _ => throw new FormatException($"unsupported scheme '{scheme}'")
        };
    }

    /// a subscription body is plain links or base64 of links
    public static string DecodeSubscription(string body)
    {
        var t = (body ?? "").Trim();
        if (t.Contains("://")) return t;
        try
        {
            var s = Encoding.UTF8.GetString(B64(t));
            return s.Contains("://") ? s : t;
        }
        catch { return t; }
    }

    static Server UriStyle(string scheme, string line)
    {
        var rest = line.Substring(line.IndexOf("://", StringComparison.Ordinal) + 3);
        var name = "";
        int h = rest.IndexOf('#');
        if (h >= 0) { name = Pct(rest.Substring(h + 1)); rest = rest.Substring(0, h); }
        var qs = "";
        int qi = rest.IndexOf('?');
        if (qi >= 0) { qs = rest.Substring(qi + 1); rest = rest.Substring(0, qi); }
        rest = rest.TrimEnd('/');
        int at = rest.LastIndexOf('@');
        if (at < 0) throw new FormatException("missing user info");
        var secret = Pct(rest.Substring(0, at));
        var (addr, port) = HostPort(rest.Substring(at + 1));
        var q = Query(qs);
        string G(string k, string d = "") => q.TryGetValue(k, out var v) ? v : d;

        var net = G("type", "tcp").ToLowerInvariant();
        if (net == "raw") net = "tcp";
        var security = G("security", scheme == "trojan" ? "tls" : "none").ToLowerInvariant();
        var path = net == "grpc" ? (q.ContainsKey("servicename") ? G("servicename") : G("path")) : G("path");
        return new Server
        {
            Name = name.Length == 0 ? addr : name,
            Protocol = scheme, Address = addr, Port = port, Secret = secret,
            Flow = G("flow"), Encryption = G("encryption", "none"),
            Network = net, HeaderType = G("headertype", "none"),
            Host = G("host"), Path = path, Mode = G("mode"),
            Security = security, Sni = q.ContainsKey("sni") ? G("sni") : G("peer"),
            Fp = G("fp", "chrome"), Alpn = G("alpn"),
            Pin = G("pcs"), Vcn = G("vcn"),
            Pbk = G("pbk"), Sid = G("sid"), Spx = G("spx")
        };
    }

    static string J(JsonNode n, string k) => n[k]?.ToString() ?? "";
    static string Or(string s, string d) => string.IsNullOrEmpty(s) ? d : s;

    static Server VMess(string line)
    {
        var body = line.Substring(line.IndexOf("://", StringComparison.Ordinal) + 3).Split('#')[0];
        var j = JsonNode.Parse(Encoding.UTF8.GetString(B64(body))) ?? throw new FormatException("bad vmess json");
        var addr = J(j, "add");
        var tls = J(j, "tls").ToLowerInvariant() == "tls";
        return new Server
        {
            Name = Or(J(j, "ps"), addr), Protocol = "vmess", Address = addr,
            Port = int.TryParse(J(j, "port"), out var p) ? p : 443,
            Secret = J(j, "id"), Method = Or(J(j, "scy"), "auto"),
            AlterId = int.TryParse(J(j, "aid"), out var a) ? a : 0,
            Network = Or(J(j, "net"), "tcp").ToLowerInvariant(), HeaderType = Or(J(j, "type"), "none"),
            Host = J(j, "host"), Path = J(j, "path"),
            Security = tls ? "tls" : "none", Sni = J(j, "sni"), Fp = Or(J(j, "fp"), "chrome"), Alpn = J(j, "alpn")
        };
    }

    static Server Ss(string line)
    {
        var rest = line.Substring(line.IndexOf("://", StringComparison.Ordinal) + 3);
        var name = "";
        int h = rest.IndexOf('#');
        if (h >= 0) { name = Pct(rest.Substring(h + 1)); rest = rest.Substring(0, h); }
        var main = rest.Split('?')[0].TrimEnd('/');
        string cred, hp;
        int at = main.LastIndexOf('@');
        if (at >= 0)
        {
            var ui = main.Substring(0, at);
            cred = ui.IndexOf(':') >= 0 ? Pct(ui) : Encoding.UTF8.GetString(B64(ui));
            hp = main.Substring(at + 1);
        }
        else
        {
            var dec = Encoding.UTF8.GetString(B64(main));
            int a2 = dec.LastIndexOf('@');
            if (a2 < 0) throw new FormatException("bad ss body");
            cred = dec.Substring(0, a2);
            hp = dec.Substring(a2 + 1);
        }
        int c = cred.IndexOf(':');
        if (c < 0) throw new FormatException("bad ss credentials");
        var (addr, port) = HostPort(hp);
        return new Server
        {
            Name = name.Length == 0 ? addr : name, Protocol = "ss", Address = addr, Port = port,
            Method = cred.Substring(0, c), Secret = cred.Substring(c + 1)
        };
    }

    public static (string, int) HostPort(string s)
    {
        var t = s.Trim();
        if (t.StartsWith("["))
        {
            int end = t.IndexOf(']');
            if (end < 0) throw new FormatException("bad ipv6");
            var p = t.Substring(end + 1).TrimStart(':');
            return (t.Substring(1, end - 1), int.TryParse(p, out var pv) ? pv : 443);
        }
        int i = t.LastIndexOf(':');
        if (i < 0) return (t, 443);
        if (!int.TryParse(t.Substring(i + 1), out var port)) throw new FormatException("bad port");
        return (t.Substring(0, i), port);
    }

    static Dictionary<string, string> Query(string q)
    {
        var m = new Dictionary<string, string>();
        foreach (var pair in q.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries))
        {
            int e = pair.IndexOf('=');
            var k = (e < 0 ? pair : pair.Substring(0, e)).ToLowerInvariant();
            m[k] = e < 0 ? "" : Pct(pair.Substring(e + 1));
        }
        return m;
    }

    public static string Pct(string s) => s.IndexOf('%') >= 0 ? Uri.UnescapeDataString(s) : s;

    /// tolerant base64 (standard + url-safe, padding optional)
    public static byte[] B64(string s)
    {
        var t = new string(s.Where(c => !char.IsWhiteSpace(c) && c != '=').ToArray()).Replace('-', '+').Replace('_', '/');
        while (t.Length % 4 != 0) t += "=";
        return Convert.FromBase64String(t);
    }
}
