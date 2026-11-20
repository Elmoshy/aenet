using System.Windows;

namespace BatV2;

public partial class App : Application
{
    static Mutex single;

    static void Cleanup()
    {
        try { Engine.Stop(); } catch { }
        try { SystemProxy.Set(false, Engine.Http); } catch { }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        bool first;
        single = new Mutex(true, @"Local\BatV2-single-instance", out first);
        if (!first)
        {
            MessageBox.Show("BatV2 is already running. Check the other window", "BatV2");
            Shutdown();
            return;
        }

        SystemProxy.ResetIfOurs(); // leftover from a kill or a power cut
        KillStraysAtStart();

        // if we crash, never leave the system proxy pointing at a dead port
        DispatcherUnhandledException += (s, ev) =>
        {
            Cleanup();
            MessageBox.Show("BatV2 hit an error and will close.\n\n" + ev.Exception.Message, "BatV2");
            ev.Handled = true;
            Shutdown(1);
        };
        AppDomain.CurrentDomain.UnhandledException += (s, ev) => Cleanup();
        Exit += (s, ev) => Cleanup();

        base.OnStartup(e);
    }

    static void KillStraysAtStart()
    {
        try { Engine.KillStrays(); } catch { }
    }
}
