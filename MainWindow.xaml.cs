using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using NewColoringbook.Services;
using NewColoringbook.Views;
using NewColoringbook.Helpers;

namespace NewColoringbook
{
    /// <summary>
    /// Hosts the navigation frame, the persistent ambient-music bar, the offscreen render host
    /// used by export/print/share, and theme application.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private const string GlyphPlay = "";
        private const string GlyphPause = "";

        private bool _loaded;

        public Frame Frame => NavFrame;

        public MainWindow()
        {
            InitializeComponent();
            WindowHelper.SetAppIcon(this);
            WindowHelper.Maximize(this);

            try
            {
                Closed += OnClosed;
                Activated += OnWindowActivated;

                // Populate music tracks combo
                MusicTrackCombo.ItemsSource = MusicPlayer.Tracks;
                var savedTrack = MusicPlayer.GetTrackById(App.Store.Profile.Settings.MusicTrack);
                MusicTrackCombo.SelectedItem = savedTrack;

                NavFrame.Navigate(typeof(GalleryPage));
            }
            catch (Exception ex)
            {
                AppLog.Error("MainWindow init failed", ex);
            }

            _loaded = true;
        }

        private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
        {
            try
            {
                SyncMusicUi();
            }
            catch (Exception ex)
            {
                AppLog.Error("OnActivated music sync failed", ex);
            }
        }

        public void SyncMusicUi()
        {
            if (!_loaded) return;
            MusicPlayIcon.Glyph = App.Music.IsPlaying ? GlyphPause : GlyphPlay;
            double target = App.Store.Profile.Settings.Volume;
            if (Math.Abs(MusicVolumeSlider.Value - target) > 0.001)
            {
                MusicVolumeSlider.Value = target;
            }
            if (MusicTrackCombo.SelectedItem is MusicTrackInfo cur && cur.Id != App.Music.CurrentTrack.Id)
            {
                MusicTrackCombo.SelectedItem = App.Music.CurrentTrack;
            }
        }

        private async void MusicTrackCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_loaded) return;
            if (MusicTrackCombo.SelectedItem is MusicTrackInfo track)
            {
                try
                {
                    await App.Music.SelectTrackAsync(track);
                    App.Store.Profile.Settings.MusicTrack = track.Id;
                    App.Store.SaveDebounced(600);
                    SyncMusicUi();
                }
                catch (Exception ex)
                {
                    AppLog.Error("MusicTrackCombo_SelectionChanged failed", ex);
                }
            }
        }

        private async void MusicPlayButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await App.Music.ToggleAsync();
                App.Store.Profile.Settings.MusicOn = App.Music.IsPlaying;
                App.Store.SaveDebounced(500);
                SyncMusicUi();
            }
            catch (Exception ex)
            {
                AppLog.Error("MusicPlayButton_Click failed", ex);
            }
        }

        private void MusicVolumeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (!_loaded || App.Music == null) return;
            try
            {
                App.Music.SetVolume(e.NewValue);
                App.Store.Profile.Settings.Volume = e.NewValue;
                App.Store.SaveDebounced(900);
            }
            catch (Exception ex)
            {
                AppLog.Error("Volume change failed", ex);
            }
        }

        /// <summary>Apply a theme ("Light", "Dark" or "System") to the whole window content.</summary>
        public void ApplyTheme(string theme)
        {
            try
            {
                RootGrid.RequestedTheme = theme switch
                {
                    "Dark" => ElementTheme.Dark,
                    "Light" => ElementTheme.Light,
                    _ => ElementTheme.Default,
                };
            }
            catch (Exception ex)
            {
                AppLog.Error("ApplyTheme failed", ex);
            }
        }

        /// <summary>Temporarily parent an element to the live tree so RenderTargetBitmap can see it.</summary>
        public void ParkOffscreen(FrameworkElement element)
        {
            try
            {
                if (!OffscreenHost.Children.Contains(element))
                    OffscreenHost.Children.Add(element);
            }
            catch (Exception ex)
            {
                AppLog.Error("ParkOffscreen failed", ex);
            }
        }

        public void Unpark(FrameworkElement element)
        {
            try
            {
                if (OffscreenHost.Children.Contains(element))
                    OffscreenHost.Children.Remove(element);
            }
            catch { }
        }

        private void OnClosed(object sender, WindowEventArgs args)
        {
            try
            {
                App.Store?.Save();
                App.Music?.Dispose();
            }
            catch (Exception ex)
            {
                AppLog.Error("OnClosed cleanup failed", ex);
            }
        }
    }
}

