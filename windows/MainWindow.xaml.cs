using System.Collections.ObjectModel;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace BatV2;

public partial class MainWindow : Window
{
    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int v, int size);

    readonly ObservableCollection<Row> rows = new();
    readonly Guard guard = new();
    bool on;

    static Brush B(string hex) => (Brush)new BrushConverter().ConvertFromString(hex);

    public MainWindow()
    {
        InitializeComponent();
        Lb.ItemsSource = rows;
        foreach (var s in Store.Load()) rows.Add(new Row(s));
        Lb.SelectedIndex = rows.Count > 0 ? Math.Min(Store.LoadSelected(), rows.Count - 1) : -1;

        guard.Changed += (st, d, e) => Dispatcher.Invoke(() => Apply(st, d, e));
        SourceInitialized += (_, _) =>
        {
            int v = 1; // dark title bar on Windows 10 and 11
            var h = new WindowInteropHelper(this).Handle;
            if (DwmSetWindowAttribute(h, 20, ref v, 4) != 0) DwmSetWindowAttribute(h, 19, ref v, 4);
        };
        Closing += (_, _) => guard.Stop();
        Apply("Disconnected", rows.Count == 0 ? "No server yet. Copy a link then press +" : "", "");
    }

    void Apply(string state, string detail, string error)
    {
        on = state is "Connected" or "Connecting" or "Retrying" or "Switching";
        Status.Text = state == "Switching" ? "Switching server" : state;
        SubText.Text = detail;
        ErrText.Text = error;
        ErrText.Visibility = error.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        var color = state == "Connected" ? "#2ECC71" : state == "Disconnected" ? "#8A8A8A" : "#FFB300";
        Status.Foreground = B(color);
        PowerIcon.Foreground = B(state == "Disconnected" ? "#8A8A8A" : color);
        Power.BorderBrush = B(state == "Disconnected" ? "#262626" : color);
    }

    void Power_Up(object s, MouseButtonEventArgs e)
    {
        if (on) { guard.Stop(); return; }
        if (Lb.SelectedIndex < 0) { Apply("Disconnected", "Add a server first", ""); return; }
        Store.SaveSelected(Lb.SelectedIndex);
        guard.Start(rows.Select(r => r.S).ToList(), Lb.SelectedIndex);
    }

    void Save() => Store.Save(rows.Select(r => r.S));

    int AddServers(IEnumerable<Server> list)
    {
        int n = 0;
        foreach (var s in list)
            if (!rows.Any(r => r.S.SameAs(s))) { rows.Add(new Row(s)); n++; }
        Save();
        if (Lb.SelectedIndex < 0 && rows.Count > 0) Lb.SelectedIndex = 0;
        return n;
    }

    void ShowInfo(string text, bool error = false)
    {
        if (on) return; // do not hide the live connection message
        SubText.Text = error ? "" : text;
        ErrText.Text = error ? text : "";
        ErrText.Visibility = error ? Visibility.Visible : Visibility.Collapsed;
    }

    void Add_Click(object s, RoutedEventArgs e)
    {
        string t = "";
        try { t = Clipboard.GetText(); } catch { }
        var (ok, bad) = LinkParser.ParseAll(t);
        if (ok.Count == 0)
        {
            ShowInfo(bad.Count > 0 ? bad[0] : "Copy a vless / vmess / trojan / ss link first then press +", true);
            return;
        }
        ShowInfo($"Added {AddServers(ok)} server(s)");
    }

    async void Sub_Click(object s, RoutedEventArgs e)
    {
        var url = InputBox.Ask(this, "Subscription link");
        if (string.IsNullOrWhiteSpace(url)) return;
        try
        {
            if (!url.StartsWith("http://") && !url.StartsWith("https://")) throw new FormatException("not an http link");
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("BatV2/1.0");
            var body = await http.GetStringAsync(url);
            if (body.Length > 2_000_000) throw new FormatException("subscription too large");
            var (ok, _) = LinkParser.ParseAll(LinkParser.DecodeSubscription(body));
            if (ok.Count == 0) throw new FormatException("no servers in this subscription");
            foreach (var r in rows.Where(r => r.S.Sub == url).ToList()) rows.Remove(r);
            foreach (var sv in ok) sv.Sub = url;
            AddServers(ok);
            ShowInfo($"Subscription: {ok.Count} server(s)");
        }
        catch (Exception ex) { ShowInfo(ex.Message, true); }
    }

    void Delete_Click(object s, RoutedEventArgs e)
    {
        if (Lb.SelectedItem is not Row r) return;
        int i = Lb.SelectedIndex;
        rows.Remove(r);
        Save();
        Lb.SelectedIndex = rows.Count == 0 ? -1 : Math.Min(i, rows.Count - 1);
    }

    void Item_Right(object s, MouseButtonEventArgs e)
    {
        if (s is ListBoxItem it) it.IsSelected = true;
    }

    static async Task PingRow(Row r)
    {
        r.Ping = "…";
        var ms = await Engine.TcpPingAsync(r.S.Address, r.S.Port);
        r.Ping = ms < 0 ? "failed" : $"{ms} ms";
    }

    async void PingAll_Click(object s, RoutedEventArgs e) =>
        await Task.WhenAll(rows.ToList().Select(PingRow));

    async void PingOne_Click(object s, RoutedEventArgs e)
    {
        if (Lb.SelectedItem is Row r) await PingRow(r);
    }

    void Edit_Click(object s, RoutedEventArgs e)
    {
        if (Lb.SelectedItem is not Row r) return;
        int i = Lb.SelectedIndex;
        var edited = ServerEditor.Show(this, r.S);
        if (edited == null) return;
        rows[i] = new Row(edited);
        Lb.SelectedIndex = i;
        Save();
        ShowInfo("Saved. Reconnect to apply");
    }

    void New_Click(object s, RoutedEventArgs e)
    {
        var n = ServerEditor.Show(this, null);
        if (n == null) return;
        ShowInfo(AddServers(new[] { n }) > 0 ? "Server added" : "This server already exists");
    }

    void Lb_DoubleClick(object s, MouseButtonEventArgs e)
    {
        if (Lb.SelectedItem != null) Edit_Click(s, e);
    }

    void Insecure_Click(object s, RoutedEventArgs e)
    {
        if (Lb.SelectedItem is not Row r) return;
        if (!r.S.AllowInsecure)
        {
            var warn = "This server will be trusted WITHOUT checking its certificate. Anyone on your network path could read or change your traffic.\n\nOnly works with old Xray (25.x). Turn it on";
            if (MessageBox.Show(this, warn + "?", "Trust any certificate", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        }
        r.S.AllowInsecure = !r.S.AllowInsecure;
        r.Refresh();
        Save();
        ShowInfo(r.S.AllowInsecure ? "Insecure mode ON for this server. Reconnect to apply" : "Insecure mode OFF. Reconnect to apply");
    }

    async void Pin_Click(object s, RoutedEventArgs e)
    {
        if (Lb.SelectedItem is not Row r) return;
        try
        {
            var c = await CertFetcher.FetchAsync(r.S.Address, r.S.Port, r.S.Sni.Length > 0 ? r.S.Sni : r.S.Address);
            var msg = $"Subject: {c.Subject}\nIssuer: {c.Issuer}\nValid until: {c.NotAfter:yyyy-MM-dd}\n\n" +
                      "Pinning trusts THIS certificate only, even if expired. Do it only on a network you trust.\n\nPin it";
            if (MessageBox.Show(this, msg + "?", "Pin certificate", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            r.S.Pin = c.Hash;
            Save();
            ShowInfo("Certificate pinned. Reconnect to apply");
        }
        catch (Exception ex) { ShowInfo(ex.Message, true); }
    }
}

public static class InputBox
{
    public static string Ask(Window owner, string title)
    {
        var w = new Window
        {
            Title = title, Width = 440, Height = 150, Owner = owner, ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = Brushes.Black
        };
        var tb = new TextBox
        {
            Margin = new Thickness(12), Height = 30, Background = new SolidColorBrush(Color.FromRgb(11, 11, 11)),
            Foreground = Brushes.White, CaretBrush = Brushes.White, VerticalContentAlignment = VerticalAlignment.Center,
            BorderBrush = new SolidColorBrush(Color.FromRgb(38, 38, 38))
        };
        string res = null;
        var ok = new Button { Content = "OK", Width = 80, Height = 30, IsDefault = true, Margin = new Thickness(12, 0, 12, 0), HorizontalAlignment = HorizontalAlignment.Right };
        ok.Click += (_, _) => { res = tb.Text.Trim(); w.Close(); };
        var sp = new StackPanel();
        sp.Children.Add(tb);
        sp.Children.Add(ok);
        w.Content = sp;
        w.Loaded += (_, _) => tb.Focus();
        w.ShowDialog();
        return res;
    }
}
