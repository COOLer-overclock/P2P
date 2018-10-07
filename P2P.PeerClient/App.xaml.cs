using System;
using System.Windows;

namespace P2P.PeerClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, arg) => {
                var logger = NLog.LogManager.GetCurrentClassLogger();

                var ex = (Exception)arg.ExceptionObject;
                logger.Fatal(ex, $"Unhandled exception: {ex.Message}");
            };

            base.OnStartup(e);
        }
    }
}
