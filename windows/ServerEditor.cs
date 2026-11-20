using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace BatV2;

/// NetMod style editor: every field in its own box, only the fields that matter for the chosen protocol are shown.
public static class ServerEditor
{
    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int v, int size);

    static Brush B(string hex) => (Brush)new BrushConverter().ConvertFromString(hex);

    sealed class Chips : WrapPanel
    {
        public string Value { get; private set; }
        public event Action Changed;
        readonly List<Border> items = new List<Border>();

        public Chips(string[] options, string current)
        {
            Value = options.Contains(current) ? current : options[0];
            foreach (var o in options)
            {
                var b = new Border
                {
                    CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1.5),
                    Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(0, 0, 8, 8),
                    Cursor = Cursors.Hand, Tag = o,
                    Child = new TextBlock { Text = o, Foreground = Brushes.White, FontSize = 13 }
                };
                b.MouseLeftButtonUp += (_, __) => { Value = (string)b.Tag; Paint(); Changed?.Invoke(); };
                items.Add(b);
                Children.Add(b);
            }
            Paint();
        }

        void Paint()
        {
            foreach (var b in items)
            {
                bool on = (string)b.Tag == Value;
                b.Background = B(on ? "#161616" : "#0B0B0B");
                b.BorderBrush = B(on ? "#FFFFFF" : "#262626");
            }
        }
    }

    static Border Btn(string text, bool primary)
    {
        return new Border
        {
            CornerRadius = new CornerRadius(10), Padding = new Thickness(22, 9, 22, 9), Margin = new Thickness(8, 0, 0, 0),
            Cursor = Cursors.Hand, BorderThickness = new Thickness(1.5),
            Background = B(primary ? "#FFFFFF" : "#0B0B0B"), BorderBrush = B(primary ? "#FFFFFF" : "#262626"),
            Child = new TextBlock { Text = text, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = primary ? Brushes.Black : Brushes.White }
        };
    }

    /// src = null opens an empty form for a new server. Returns the edited copy or null if cancelled.
    public static Server Show(Window owner, Server src)
    {
        var s = src != null ? src.Copy() : new Server { Name = "", Protocol = "vless", Security = "tls", Port = 443 };
        var w = new Window
        {
            Title = src == null ? "New server" : "Edit server", Width = 440, Height = 720, MinHeight = 480,
            Owner = owner, WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = Brushes.Black
        };
        w.SourceInitialized += (_, __) =>
        {
            int v = 1;
            var h = new WindowInteropHelper(w).Handle;
            if (DwmSetWindowAttribute(h, 20, ref v, 4) != 0) DwmSetWindowAttribute(h, 19, ref v, 4);
        };

        var panel = new StackPanel { Margin = new Thickness(16) };
        var rows = new List<(UIElement El, Func<bool> Show)>();

        void AddRow(string label, UIElement ctl, Func<bool> show = null)
        {
            var g = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            if (label.Length > 0)
                g.Children.Add(new TextBlock { Text = label, Foreground = B("#8A8A8A"), FontSize = 12, Margin = new Thickness(0, 0, 0, 4) });
            g.Children.Add(ctl);
            panel.Children.Add(g);
            rows.Add((g, show ?? (() => true)));
        }

        TextBox Box(string text) => new TextBox
        {
            Text = text ?? "", Background = B("#0B0B0B"), Foreground = Brushes.White, CaretBrush = Brushes.White,
            BorderBrush = B("#262626"), BorderThickness = new Thickness(1.5), Padding = new Thickness(8, 6, 8, 6), FontSize = 14
        };

        var proto = new Chips(new[] { "vless", "vmess", "trojan", "ss" }, s.Protocol);
        var name = Box(s.Name);
        var addr = Box(s.Address);
        var port = Box(s.Port.ToString());
        var secret = Box(s.Secret);
        var method = Box(s.Method);
        var alter = Box(s.AlterId.ToString());
        var flow = Box(s.Flow);
        var enc = Box(s.Encryption);
        var net = new Chips(new[] { "tcp", "ws", "grpc", "httpupgrade", "xhttp" }, s.Network);
        var hdr = new Chips(new[] { "none", "http" }, s.HeaderType);
        var host = Box(s.Host);
        var path = Box(s.Path);
        var mode = Box(s.Mode);
        var sec = new Chips(new[] { "none", "tls", "reality" }, s.Security);
        var sni = Box(s.Sni);
        var fp = Box(s.Fp);
        var alpn = Box(s.Alpn);
        var pin = Box(s.Pin);
        var vcn = Box(s.Vcn);
        var pbk = Box(s.Pbk);
        var sid = Box(s.Sid);
        var spx = Box(s.Spx);
        var frag = new CheckBox { Content = "Fragment TLS hello (helps against filtering)", Foreground = Brushes.White, IsChecked = s.Fragment };
        var insec = new CheckBox { Content = "Trust any certificate (insecure, old Xray only)", Foreground = Brushes.White, IsChecked = s.AllowInsecure };

        bool NotSs() => proto.Value != "ss";
        bool Tls() => NotSs() && sec.Value == "tls";
        bool Real() => NotSs() && sec.Value == "reality";

        AddRow("Name", name);
        AddRow("Protocol", proto);
        AddRow("Address", addr);
        AddRow("Port", port);
        AddRow("UUID / Password", secret);
        AddRow("Cipher (ss) / Security (vmess)", method, () => proto.Value == "vmess" || proto.Value == "ss");
        AddRow("Alter ID", alter, () => proto.Value == "vmess");
        AddRow("Flow", flow, () => proto.Value == "vless");
        AddRow("Encryption", enc, () => proto.Value == "vless");
        AddRow("Network", net, NotSs);
        AddRow("Header type", hdr, () => NotSs() && net.Value == "tcp");
        AddRow("Host", host, () => NotSs() && net.Value != "grpc");
        AddRow("Path / Service name", path, NotSs);
        AddRow("Mode", mode, () => NotSs() && (net.Value == "grpc" || net.Value == "xhttp"));
        AddRow("Security", sec, NotSs);
        AddRow("SNI", sni, () => Tls() || Real());
        AddRow("Fingerprint", fp, () => Tls() || Real());
        AddRow("ALPN (comma separated)", alpn, Tls);
        AddRow("Pinned certificate sha256", pin, Tls);
        AddRow("Verify peer cert by name", vcn, Tls);
        AddRow("Public key", pbk, Real);
        AddRow("Short ID", sid, Real);
        AddRow("SpiderX", spx, Real);
        AddRow("", frag, NotSs);
        AddRow("", insec, Tls);

        void Refresh()
        {
            foreach (var r in rows) r.El.Visibility = r.Show() ? Visibility.Visible : Visibility.Collapsed;
        }
        proto.Changed += Refresh;
        net.Changed += Refresh;
        sec.Changed += Refresh;
        Refresh();

        var err = new TextBlock { Foreground = B("#FF5252"), FontSize = 12, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, MaxWidth = 200 };
        var save = Btn("Save", true);
        var cancel = Btn("Cancel", false);
        var bar = new DockPanel { Margin = new Thickness(16, 8, 16, 14), LastChildFill = false };
        DockPanel.SetDock(save, Dock.Right);
        DockPanel.SetDock(cancel, Dock.Right);
        DockPanel.SetDock(err, Dock.Left);
        bar.Children.Add(save);
        bar.Children.Add(cancel);
        bar.Children.Add(err);

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var scroll = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Grid.SetRow(scroll, 0);
        Grid.SetRow(bar, 1);
        grid.Children.Add(scroll);
        grid.Children.Add(bar);
        w.Content = grid;

        Server result = null;
        cancel.MouseLeftButtonUp += (_, __) => w.Close();
        save.MouseLeftButtonUp += (_, __) =>
        {
            if (string.IsNullOrWhiteSpace(addr.Text)) { err.Text = "Address is required"; return; }
            if (!int.TryParse(port.Text.Trim(), out var p) || p < 1 || p > 65535) { err.Text = "Port must be 1 to 65535"; return; }
            if (string.IsNullOrWhiteSpace(secret.Text)) { err.Text = "UUID or password is required"; return; }
            var r = s;
            r.Protocol = proto.Value;
            r.Address = addr.Text.Trim();
            r.Port = p;
            r.Secret = secret.Text.Trim();
            r.Name = string.IsNullOrWhiteSpace(name.Text) ? r.Address : name.Text.Trim();
            r.Method = method.Text.Trim();
            r.AlterId = int.TryParse(alter.Text.Trim(), out var a) ? a : 0;
            r.Flow = flow.Text.Trim();
            r.Encryption = string.IsNullOrWhiteSpace(enc.Text) ? "none" : enc.Text.Trim();
            r.Network = net.Value;
            r.HeaderType = hdr.Value;
            r.Host = host.Text.Trim();
            r.Path = path.Text.Trim();
            r.Mode = mode.Text.Trim();
            r.Security = sec.Value;
            r.Sni = sni.Text.Trim();
            r.Fp = fp.Text.Trim();
            r.Alpn = alpn.Text.Trim();
            r.Pin = pin.Text.Trim();
            r.Vcn = vcn.Text.Trim();
            r.Pbk = pbk.Text.Trim();
            r.Sid = sid.Text.Trim();
            r.Spx = spx.Text.Trim();
            r.Fragment = frag.IsChecked == true;
            r.AllowInsecure = insec.IsChecked == true;
            if (r.Protocol == "ss") { r.Network = "tcp"; r.Security = "none"; r.Fragment = false; r.AllowInsecure = false; }
            r.Sub = ""; // edited by hand: keep it when the subscription refreshes
            result = r;
            w.Close();
        };

        w.ShowDialog();
        return result;
    }
}
