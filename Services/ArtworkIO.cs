using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Graphics.Printing;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using NewColoringbook.Core;

namespace NewColoringbook.Services
{
    /// <summary>
    /// Artwork output pipeline: builds a paper-backed visual of the colored design, renders it to
    /// PNG bytes (via RenderTargetBitmap + WIC), then saves (FileSavePicker), prints
    /// (PrintManagerInterop + PrintDocument) or shares (DataTransferManagerInterop).
    /// </summary>
    public static class ArtworkIO
    {
        /// <summary>Paper + strokes + regions, 1000x1000 design units, ready to render.</summary>
        public static Grid BuildExportVisual(DesignDef design, DesignProgress progress, List<Stroke> strokes)
        {
            try
            {
                var root = new Grid
                {
                    Width = DesignRenderer.DesignSize,
                    Height = DesignRenderer.DesignSize,
                    Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
                };
                var canvas = new Canvas
                {
                    Width = DesignRenderer.DesignSize,
                    Height = DesignRenderer.DesignSize,
                    IsHitTestVisible = false,
                };
                for (int i = 0; i < design.Regions.Count; i++)
                {
                    var region = design.Regions[i];
                    progress.Fills.TryGetValue(region.Id, out var fill);
                    canvas.Children.Add(DesignRenderer.BuildRegionPath(region, fill));
                }
                foreach (var stroke in strokes)
                    canvas.Children.Add(DesignRenderer.BuildStrokePath(stroke));
                root.Children.Add(canvas);
                return root;
            }
            catch (Exception ex)
            {
                AppLog.Error("BuildExportVisual failed", ex);
                return new Grid { Width = DesignRenderer.DesignSize, Height = DesignRenderer.DesignSize };
            }
        }

        /// <summary>Render a visual to raw BGRA pixels at the given square size.</summary>
        public static async Task<byte[]?> RenderPixelsAsync(FrameworkElement visual, int size)
        {
            var window = App.MainWnd;
            if (window == null) return null;
            bool parked = false;
            try
            {
                window.ParkOffscreen(visual);
                parked = true;
                // let the tree lay the element out before capturing
                await Task.Delay(60);
                var rtb = new RenderTargetBitmap();
                await rtb.RenderAsync(visual, size, size);
                var buffer = await rtb.GetPixelsAsync();
                if (buffer == null || buffer.Length == 0) return null;
                var bytes = new byte[buffer.Length];
                using var reader = DataReader.FromBuffer(buffer);
                reader.ReadBytes(bytes);
                return bytes;
            }
            catch (Exception ex)
            {
                AppLog.Error("RenderPixelsAsync failed", ex);
                return null;
            }
            finally
            {
                if (parked) window.Unpark(visual);
            }
        }

        /// <summary>PNG bytes for the artwork, or null on failure.</summary>
        public static async Task<byte[]?> RenderPngAsync(DesignDef design, DesignProgress progress, List<Stroke> strokes, int size = 1600)
        {
            var pixels = await RenderPixelsAsync(BuildExportVisual(design, progress, strokes), size);
            if (pixels == null) return null;
            try
            {
                using var ms = new InMemoryRandomAccessStream();
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, ms);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied,
                    (uint)size, (uint)size, 96, 96, pixels);
                await encoder.FlushAsync();
                var bytes = new byte[(int)ms.Size];
                using var reader = new DataReader(ms.GetInputStreamAt(0));
                await reader.LoadAsync((uint)ms.Size);
                reader.ReadBytes(bytes);
                return bytes;
            }
            catch (Exception ex)
            {
                AppLog.Error("RenderPngAsync encode failed", ex);
                return null;
            }
        }

        /// <summary>Direct pixel copy into a WriteableBitmap (print pages, previews).</summary>
        public static async Task<WriteableBitmap?> RenderBitmapAsync(DesignDef design, DesignProgress progress, List<Stroke> strokes, int size = 1500)
        {
            var pixels = await RenderPixelsAsync(BuildExportVisual(design, progress, strokes), size);
            if (pixels == null) return null;
            try
            {
                var wb = new WriteableBitmap(size, size);
                using var stream = wb.PixelBuffer.AsStream();
                await stream.WriteAsync(pixels, 0, pixels.Length);
                return wb;
            }
            catch (Exception ex)
            {
                AppLog.Error("RenderBitmapAsync failed", ex);
                return null;
            }
        }

        /// <summary>Ask the user where to save, then write the PNG. Returns true when written.</summary>
        public static async Task<bool> ExportPngAsync(Window window, DesignDef design, DesignProgress progress, List<Stroke> strokes)
        {
            try
            {
                var png = await RenderPngAsync(design, progress, strokes);
                if (png == null) return false;

                var picker = new FileSavePicker
                {
                    SuggestedFileName = Sanitize(design.Title),
                    SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                };
                picker.FileTypeChoices.Add("PNG image", new List<string> { ".png" });
                WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(window));

                var file = await picker.PickSaveFileAsync();
                if (file == null) return false;
                await FileIO.WriteBytesAsync(file, png);
                AppLog.Info("Exported " + file.Path);
                return true;
            }
            catch (Exception ex)
            {
                AppLog.Error("ExportPngAsync failed", ex);
                return false;
            }
        }

        /// <summary>Share the rendered PNG through the Windows Share contract (desktop interop).</summary>
        public static async Task ShareAsync(Window window, DesignDef design, DesignProgress progress, List<Stroke> strokes)
        {
            try
            {
                var png = await RenderPngAsync(design, progress, strokes, 1400);
                if (png == null) return;

                var shareDirPath = Path.Combine(StoragePaths.AppDataFolder, "share");
                Directory.CreateDirectory(shareDirPath);
                var filePath = Path.Combine(shareDirPath, Sanitize(design.Title) + ".png");
                File.WriteAllBytes(filePath, png);
                var file = await StorageFile.GetFileFromPathAsync(filePath);

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                if (hwnd == IntPtr.Zero) return;
                DataTransferManager? dtm = null;
                try { dtm = DataTransferManagerInterop.GetForWindow(hwnd); }
                catch (Exception ex) { AppLog.Error("GetForWindow failed", ex); }

                if (dtm == null)
                {
                    await ShowInfoAsync(window, "Share", "Sharing is not available on this device. Use Export instead.");
                    return;
                }

                dtm.DataRequested += OnDataRequested;
                DataTransferManagerInterop.ShowShareUIForWindow(hwnd);
                return;

                void OnDataRequested(DataTransferManager sender, DataRequestedEventArgs args)
                {
                    sender.DataRequested -= OnDataRequested;
                    var deferral = args.Request.GetDeferral();
                    try
                    {
                        args.Request.Data.Properties.Title = design.Title + " — Stress Relief Coloring Book";
                        args.Request.Data.Properties.Description = "Colored artwork";
                        args.Request.Data.SetStorageItems(new[] { file });
                    }
                    finally
                    {
                        deferral.Complete();
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Error("ShareAsync failed", ex);
            }
        }

        public static async Task ShowInfoAsync(Window window, string title, string message)
        {
            try
            {
                var root = (window.Content as FrameworkElement)?.XamlRoot;
                if (root == null) return;
                var dlg = new ContentDialog
                {
                    Title = title,
                    Content = message,
                    CloseButtonText = "OK",
                    XamlRoot = root,
                };
                await dlg.ShowAsync();
            }
            catch { }
        }

        private static string Sanitize(string name)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var c in name)
            {
                if (char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_') sb.Append(c);
            }
            var s = sb.ToString().Trim();
            return s.Length == 0 ? "artwork" : s;
        }
    }

    /// <summary>Prints the artwork on one white page via the WinRT print contract.</summary>
    public sealed class PrintService : IDisposable
    {
        private Microsoft.UI.Xaml.Printing.PrintDocument? _printDocument;
        private IPrintDocumentSource? _printSource;
        private Windows.Graphics.Printing.PrintManager? _printManager;
        private readonly IntPtr _hwnd;
        private WriteableBitmap? _page;
        private double _pageW = 1488, _pageH = 2105;
        private bool _registered;

        public PrintService(Window window)
        {
            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        }

        public async Task<bool> TryPrintAsync(DesignDef design, DesignProgress progress, List<Stroke> strokes)
        {
            try
            {
                if (!Windows.Graphics.Printing.PrintManager.IsSupported())
                {
                    AppLog.Info("Printing not supported on this device.");
                    return false;
                }
                _page = await ArtworkIO.RenderBitmapAsync(design, progress, strokes);
                if (_page == null) return false;
                EnsureRegistered();
                await Windows.Graphics.Printing.PrintManagerInterop.ShowPrintUIForWindowAsync(_hwnd);
                return true;
            }
            catch (Exception ex)
            {
                AppLog.Error("Print failed", ex);
                return false;
            }
        }

        private void EnsureRegistered()
        {
            if (_registered) return;
            _printManager = Windows.Graphics.Printing.PrintManagerInterop.GetForWindow(_hwnd);
            _printManager.PrintTaskRequested += OnPrintTaskRequested;
            _printDocument = new Microsoft.UI.Xaml.Printing.PrintDocument();
            _printSource = _printDocument.DocumentSource;
            _printDocument.Paginate += OnPaginate;
            _printDocument.GetPreviewPage += OnGetPreviewPage;
            _printDocument.AddPages += OnAddPages;
            _registered = true;
        }

        private void OnPrintTaskRequested(Windows.Graphics.Printing.PrintManager sender, Windows.Graphics.Printing.PrintTaskRequestedEventArgs args)
        {
            var task = args.Request.CreatePrintTask("Stress Relief Coloring Book artwork", req => req.SetSource(_printSource));
            task.Completed += (s, e) => { };
        }

        private void OnPaginate(object? sender, Microsoft.UI.Xaml.Printing.PaginateEventArgs e)
        {
            var desc = ((Windows.Graphics.Printing.PrintTaskOptions)e.PrintTaskOptions).GetPageDescription(0);
            _pageW = desc.PageSize.Width;
            _pageH = desc.PageSize.Height;
            _printDocument?.SetPreviewPageCount(1, Microsoft.UI.Xaml.Printing.PreviewPageCountType.Final);
        }

        private UIElement BuildPage()
        {
            var grid = new Grid
            {
                Width = _pageW,
                Height = _pageH,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
            };
            if (_page != null)
            {
                grid.Children.Add(new Image
                {
                    Source = _page,
                    Width = _pageW,
                    Height = _pageH,
                    Stretch = Stretch.Uniform,
                    Margin = new Thickness(48),
                });
            }
            return grid;
        }

        private void OnGetPreviewPage(object? sender, Microsoft.UI.Xaml.Printing.GetPreviewPageEventArgs e)
        {
            if (_printDocument != null && e.PageNumber == 1)
                _printDocument.SetPreviewPage(1, BuildPage());
        }

        private void OnAddPages(object? sender, Microsoft.UI.Xaml.Printing.AddPagesEventArgs e)
        {
            if (_printDocument != null)
            {
                _printDocument.AddPage(BuildPage());
                _printDocument.AddPagesComplete();
            }
        }

        public void Dispose()
        {
            try
            {
                if (_printManager != null)
                {
                    _printManager.PrintTaskRequested -= OnPrintTaskRequested;
                    _printManager = null;
                }
                if (_printDocument != null)
                {
                    _printDocument.Paginate -= OnPaginate;
                    _printDocument.GetPreviewPage -= OnGetPreviewPage;
                    _printDocument.AddPages -= OnAddPages;
                    _printDocument = null;
                }
                _registered = false;
            }
            catch { }
        }
    }
}

