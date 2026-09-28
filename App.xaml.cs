using Microsoft.UI.Xaml;
using System;
using NewColoringbook.Services;

namespace NewColoringbook
{
    /// <summary>App entry point: global exception capture (never crash on a non-fatal UI fault)
    /// plus the shared profile store and ambient music player.</summary>
    public partial class App : Application
    {
        public static MainWindow? MainWnd { get; private set; }
        public static ProfileStore Store { get; private set; } = null!;
        public static MusicPlayer Music { get; private set; } = null!;

        public App()
        {
            InitializeComponent();
            UnhandledException += (sender, args) =>
            {
                AppLog.Error("GlobalUnhandledException", args.Exception);
                args.Handled = true;
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                AppLog.Error("AppDomainUnhandled", e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString()));
            };
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                AppLog.Error("UnobservedTask", e.Exception);
                e.SetObserved();
            };
            AppLog.Info("App starting");
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            Store = new ProfileStore();
            Music = new MusicPlayer();
            var savedTrack = MusicPlayer.GetTrackById(Store.Profile.Settings.MusicTrack);
            Music.SetVolume(Store.Profile.Settings.Volume);
            _ = Music.EnsureReadyAsync(savedTrack);

            MainWnd = new MainWindow();
            MainWnd.Activate();

            // Resume ambient music when the user left it on last session.
            if (Store.Profile.Settings.MusicOn)
            {
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(1200);
                    if (Store.Profile.Settings.MusicOn)
                    {
                        await Music.PlayAsync();
                    }
                });
            }
        }
    }
}

