using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NewColoringbook.Views;

namespace NewColoringbook.Services
{
    /// <summary>Tiny navigation helper over the main window frame.</summary>
    public static class Nav
    {
        public static Frame? Frame => App.MainWnd?.Frame;

        public static void Gallery() => Frame?.Navigate(typeof(GalleryPage));

        public static void Studio(string designId) => Frame?.Navigate(typeof(StudioPage), designId);

        public static void Settings() => Frame?.Navigate(typeof(SettingsPage));

        public static void Back()
        {
            var f = Frame;
            if (f != null && f.CanGoBack) f.GoBack();
        }
    }
}

