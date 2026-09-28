using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using NewColoringbook.Core;
using NewColoringbook.Services;

namespace NewColoringbook.Views
{
    /// <summary>Settings &amp; about: theme, ambient music, data management (export/import/reset),
    /// privacy and shortcut help.</summary>
    public sealed partial class SettingsPage : Page
    {
        private bool _loaded;

        public SettingsPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_loaded) return;
            _loaded = true;
            try
            {
                var settings = App.Store.Profile.Settings;

                // theme radio (no XAML default selection — set here to avoid init-time events)
                int idx = settings.Theme switch
                {
                    "Dark" => 1,
                    "System" => 2,
                    _ => 0,
                };
                ThemeRadio.SelectedIndex = idx;

                MusicTrackCombo.ItemsSource = MusicPlayer.Tracks;
                var currentTrack = MusicPlayer.GetTrackById(settings.MusicTrack);
                MusicTrackCombo.SelectedItem = currentTrack;

                MusicToggle.IsOn = App.Music.IsPlaying || settings.MusicOn;
                VolumeSlider.Value = settings.Volume;
                App.MainWnd?.ApplyTheme(settings.Theme);
            }
            catch (Exception ex)
            {
                AppLog.Error("SettingsPage load failed", ex);
            }
            await Task.CompletedTask;
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
                    App.Store.SaveDebounced(400);
                    App.MainWnd?.SyncMusicUi();
                }
                catch (Exception ex)
                {
                    AppLog.Error("Settings MusicTrackCombo changed failed", ex);
                }
            }
        }

        private void ThemeRadio_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_loaded) return;
            try
            {
                var tag = (ThemeRadio.SelectedItem as RadioButton)?.Tag as string ?? "Light";
                App.Store.Profile.Settings.Theme = tag;
                App.MainWnd?.ApplyTheme(tag);
                App.Store.SaveDebounced(400);
            }
            catch (Exception ex)
            {
                AppLog.Error("Theme change failed", ex);
            }
        }

        private async void MusicToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            try
            {
                var settings = App.Store.Profile.Settings;
                if (MusicToggle.IsOn)
                {
                    await App.Music.PlayAsync();
                    settings.MusicOn = true;
                }
                else
                {
                    App.Music.Pause();
                    settings.MusicOn = false;
                }
                App.MainWnd?.SyncMusicUi();
                App.Store.SaveDebounced(400);
            }
            catch (Exception ex)
            {
                AppLog.Error("Music toggle failed", ex);
            }
        }

        private void VolumeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (!_loaded) return;
            try
            {
                App.Music.SetVolume(e.NewValue);
                App.Store.Profile.Settings.Volume = e.NewValue;
                App.Store.SaveDebounced(900);
            }
            catch (Exception ex)
            {
                AppLog.Error("Volume slider failed", ex);
            }
        }

        // ---------------------------------------------------------------- data management

        private async void ExportDataButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                App.Store.Save();
                var picker = new FileSavePicker
                {
                    SuggestedFileName = "stress-relief-coloring-book-data",
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                };
                picker.FileTypeChoices.Add("JSON data", new List<string> { ".json" });
                WinRT.Interop.InitializeWithWindow.Initialize(picker,
                    WinRT.Interop.WindowNative.GetWindowHandle(App.MainWnd!));

                var file = await picker.PickSaveFileAsync();
                if (file == null) return;
                await FileIO.WriteTextAsync(file, App.Store.CurrentJson);
                DataStatusText.Text = "Data exported to " + file.Name;
            }
            catch (Exception ex)
            {
                AppLog.Error("Data export failed", ex);
                DataStatusText.Text = "Export failed — see the app log.";
            }
        }

        private async void ImportDataButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileOpenPicker
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    ViewMode = PickerViewMode.List,
                };
                picker.FileTypeFilter.Add(".json");
                WinRT.Interop.InitializeWithWindow.Initialize(picker,
                    WinRT.Interop.WindowNative.GetWindowHandle(App.MainWnd!));

                var file = await picker.PickSingleFileAsync();
                if (file == null) return;
                var json = await FileIO.ReadTextAsync(file);
                var imported = Newtonsoft.Json.JsonConvert.DeserializeObject<AppProfile>(json);
                if (imported == null)
                {
                    DataStatusText.Text = "That file doesn't look like Stress Relief Coloring Book data.";
                    return;
                }

                var dlg = new ContentDialog
                {
                    Title = "Import data?",
                    Content = "This replaces all current progress, palettes and settings with the file's contents.",
                    PrimaryButtonText = "Import",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = XamlRoot,
                };
                if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

                App.Store.ImportProfile(imported);
                App.Music.SetVolume(imported.Settings.Volume);
                App.MainWnd?.ApplyTheme(imported.Settings.Theme);
                App.MainWnd?.SyncMusicUi();
                DataStatusText.Text = "Data imported from " + file.Name;
            }
            catch (Exception ex)
            {
                AppLog.Error("Data import failed", ex);
                DataStatusText.Text = "Import failed — see the app log.";
            }
        }

        private async void ResetDataButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new ContentDialog
                {
                    Title = "Reset all progress?",
                    Content = "Every colored area, brush stroke, saved swatch and recent color will be cleared. This cannot be undone.",
                    PrimaryButtonText = "Reset everything",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = XamlRoot,
                };
                if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

                App.Store.Profile.Designs.Clear();
                App.Store.Profile.RecentColors.Clear();
                App.Store.Save();
                DataStatusText.Text = "All coloring progress cleared.";
            }
            catch (Exception ex)
            {
                AppLog.Error("Data reset failed", ex);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) => Nav.Back();
    }
}

