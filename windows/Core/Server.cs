using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BatV2;

/// One server, every editable field. Protocol: vless | vmess | trojan | ss
public class Server
{
    public string Name { get; set; } = "Server";
    public string Protocol { get; set; } = "trojan";
    public string Address { get; set; } = "";
    public int Port { get; set; } = 443;
    /// password (trojan/ss) or uuid (vless/vmess)
    public string Secret { get; set; } = "";
    public string Method { get; set; } = "";
    public string Flow { get; set; } = "";
    public string Encryption { get; set; } = "none";
    public int AlterId { get; set; }
    public string Network { get; set; } = "tcp";
    public string HeaderType { get; set; } = "none";
    public string Host { get; set; } = "";
    public string Path { get; set; } = "";
    public string Mode { get; set; } = "";
    public string Security { get; set; } = "none";
    public string Sni { get; set; } = "";
    public string Fp { get; set; } = "chrome";
    public string Alpn { get; set; } = "";
    /// pinned certificate sha256 (hex), comma separated
    public string Pin { get; set; } = "";
    public string Vcn { get; set; } = "";
    public string Pbk { get; set; } = "";
    public string Sid { get; set; } = "";
    public string Spx { get; set; } = "";
    public bool Fragment { get; set; }
    /// skip certificate verification. Only works on old Xray (25.x). New Xray removed it
    public bool AllowInsecure { get; set; }
    public string Sub { get; set; } = "";

    public Server Copy() => (Server)MemberwiseClone();

    public bool SameAs(Server o) =>
        Protocol == o.Protocol && Address == o.Address && Port == o.Port && Secret == o.Secret;
}

public static class Store
{
    // snake_case keeps the file compatible with the Rust command line version
    static readonly JsonSerializerOptions Opt = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };

    public static string Dir
    {
        get
        {
            var d = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BatV2");
            Directory.CreateDirectory(d);
            return d;
        }
    }

    static string File_ => System.IO.Path.Combine(Dir, "servers.json");
    static string SelFile => System.IO.Path.Combine(Dir, "selected.txt");

    public static List<Server> Load()
    {
        try { return JsonSerializer.Deserialize<List<Server>>(File.ReadAllText(File_), Opt) ?? new(); }
        catch { return new(); }
    }

    public static void Save(IEnumerable<Server> list)
    {
        try { File.WriteAllText(File_, JsonSerializer.Serialize(list.ToList(), Opt)); } catch { }
    }

    public static int LoadSelected()
    {
        try { return int.Parse(File.ReadAllText(SelFile).Trim()); } catch { return 0; }
    }

    public static void SaveSelected(int i)
    {
        try { File.WriteAllText(SelFile, i.ToString()); } catch { }
    }
}
