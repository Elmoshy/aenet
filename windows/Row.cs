using System.ComponentModel;
using System.Windows.Media;

namespace BatV2;

public class Row : INotifyPropertyChanged
{
    static Brush B(string hex) { var b = (SolidColorBrush)new BrushConverter().ConvertFromString(hex); b.Freeze(); return b; }
    static readonly Brush Ok = B("#2ECC71"), Grey = B("#8A8A8A"), Err = B("#FF5252");

    public Server S { get; }
    public Row(Server s) { S = s; }

    public string Title => S.Name;
    public string Detail => $"{S.Protocol.ToUpperInvariant()}   {S.Address}:{S.Port}" + (S.AllowInsecure ? "   INSECURE" : "");
    public void Refresh() => N(nameof(Detail));

    string ping = "—";
    public string Ping { get => ping; set { ping = value; N(nameof(Ping)); N(nameof(PingBrush)); } }
    public Brush PingBrush => ping.EndsWith("ms") ? Ok : ping == "—" ? Grey : Err;

    public event PropertyChangedEventHandler PropertyChanged;
    void N(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}
