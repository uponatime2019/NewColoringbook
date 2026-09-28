using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using NewColoringbook.Core;
using NewColoringbook.Services;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace NewColoringbook.Views
{
    /// <summary>
    /// The coloring studio: zoom/pan line-art canvas, region fill with textures, freehand brush,
    /// eraser, palette + mixer, undo/redo, autosave, completion, export/print/share.
    /// </summary>
    public sealed partial class StudioPage : Page
    {
        private enum Tool { Fill, Brush, Eraser }

        private DesignDef _design = null!;
        private DesignProgress _progress = null!;
        private readonly List<Stroke> _strokes = new();
        private readonly List<Path> _strokePaths = new();
        private readonly Dictionary<string, Path> _regionPaths = new();
        private readonly History _history = new();

        private Tool _tool = Tool.Fill;
        private string _texture = "solid";
        private string _activeColor = "#F4845F";
        private double _brushSize = 20;

        private double _scale = 1.0, _tx, _ty;
        private bool _panning, _brushing, _pressed, _moved;
        private Point _pressPoint;
        private DesignRegion? _pressRegion;
        private Stroke? _liveStroke;
        private readonly List<(Button Btn, string Hex)> _swatchButtons = new();
        private bool _loaded, _populating;
        private DispatcherQueueTimer? _sessionTimer;
        private PrintService? _printService;

        public StudioPage()
        {
            InitializeComponent();
            _history.Changed += OnHistoryChanged;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            try
            {
                var id = e.Parameter as string ?? "owl";
                _design = DesignCatalog.Find(id) ?? DesignCatalog.All[0];
                _progress = App.Store.Profile.ProgressFor(_design.Id);
                if (_design.Id == DesignCatalog.FreeDrawId)
                {
                    TitleText.Text = "Free Draw";
                    PercentText.Text = "Free Draw";
                }
                else
                {
                    TitleText.Text = $"{_design.DisplayNumber}  {_design.Title}";
                }
                BuildCanvas();
                LoadStrokes();
                LoadSettings();
                PopulatePaletteCombo();
                BuildSwatches();
                BuildRecent();
                BuildCustomSwatches();
                BuildTextureFlyout();
                UpdateActiveUi();
                UpdateProgressUi();
                if (_design.Category == DesignCatalog.CatFreeDraw || _design.Id.StartsWith("free_draw", StringComparison.OrdinalIgnoreCase))
                {
                    SetTool(Tool.Brush);
                }
                _history.Clear();
                OnHistoryChanged();
                StartSessionTimer();
            }
            catch (Exception ex)
            {
                AppLog.Error("StudioPage nav-to failed", ex);
            }
        }

        protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            try
            {
                FlushSave();
                _sessionTimer?.Stop();
                _printService?.Dispose();
                _printService = null;
            }
            catch (Exception ex)
            {
                AppLog.Error("StudioPage nav-from failed", ex);
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e) => _loaded = true;

        private void OnUnloaded(object sender, RoutedEventArgs e) => _sessionTimer?.Stop();

        private void StartSessionTimer()
        {
            try
            {
                _sessionTimer ??= DispatcherQueue.CreateTimer();
                _sessionTimer.Interval = TimeSpan.FromSeconds(5);
                _sessionTimer.IsRepeating = true;
                _sessionTimer.Tick += (_, _) =>
                {
                    try
                    {
                        _progress.SecondsSpent += 5;
                        _progress.UpdatedUtc = DateTime.UtcNow;
                    }
                    catch { }
                };
                _sessionTimer.Start();
            }
            catch (Exception ex)
            {
                AppLog.Error("Session timer failed", ex);
            }
        }

        // ---------------------------------------------------------------- canvas build

        private void BuildCanvas()
        {
            try
            {
                RegionLayer.Children.Clear();
                _regionPaths.Clear();
                foreach (var region in _design.Regions)
                {
                    _progress.Fills.TryGetValue(region.Id, out var fill);
                    var path = DesignRenderer.BuildRegionPath(region, fill);
                    _regionPaths[region.Id] = path;
                    RegionLayer.Children.Add(path);
                }
                // The artwork is one interactive surface, not hundreds of unnamed shapes.
                Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(CanvasHost, "Coloring canvas");
                Microsoft.UI.Xaml.Automation.AutomationProperties.SetAccessibilityView(
                    RegionLayer, Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);
                Microsoft.UI.Xaml.Automation.AutomationProperties.SetAccessibilityView(
                    StrokeLayer, Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);
            }
            catch (Exception ex)
            {
                AppLog.Error("BuildCanvas failed", ex);
            }
        }

        private void LoadStrokes()
        {
            try
            {
                StrokeLayer.Children.Clear();
                _strokes.Clear();
                _strokePaths.Clear();
                foreach (var stored in _progress.Strokes)
                {
                    var stroke = new Stroke(stored.Color, stored.Width);
                    foreach (var pt in stored.Pts)
                    {
                        if (pt.Length >= 2)
                            stroke.Points.Add(new Point(pt[0], pt[1]));
                    }
                    if (stroke.Points.Count == 0) continue;
                    var path = DesignRenderer.BuildStrokePath(stroke);
                    _strokes.Add(stroke);
                    _strokePaths.Add(path);
                    StrokeLayer.Children.Add(path);
                }
            }
            catch (Exception ex)
            {
                AppLog.Error("LoadStrokes failed", ex);
            }
        }

        private void LoadSettings()
        {
            try
            {
                var s = App.Store.Profile.Settings;
                _activeColor = s.ActiveColor;
                _texture = Textures.Normalize(s.Texture);
                _brushSize = 20;
                BrushSizeSlider.Value = _brushSize;
                TextureButtonText.Text = Textures.NameOf(_texture);
            }
            catch (Exception ex)
            {
                AppLog.Error("LoadSettings failed", ex);
            }
        }

        // ---------------------------------------------------------------- view transform

        private void ApplyView()
        {
            try
            {
                ViewTransform.Matrix = new Matrix(_scale, 0, 0, _scale, _tx, _ty);
                ZoomPercentText.Text = (int)Math.Round(_scale * 100) + "%";
            }
            catch { }
        }

        private void FitView()
        {
            try
            {
                double w = CanvasHost.ActualWidth, h = CanvasHost.ActualHeight;
                if (w < 10 || h < 10) return;
                _scale = Math.Min(w, h) / DesignRenderer.DesignSize * 0.96;
                _tx = (w - DesignRenderer.DesignSize * _scale) / 2.0;
                _ty = (h - DesignRenderer.DesignSize * _scale) / 2.0;
                ApplyView();
            }
            catch { }
        }

        private void CanvasHost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                CanvasHost.Clip = new RectangleGeometry { Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height) };
                FitView();
            }
            catch (Exception ex) { AppLog.Error("CanvasHost_SizeChanged failed", ex); }
        }

        private Point ToDesign(Point screen)
        {
            try
            {
                return new Point((screen.X - _tx) / _scale, (screen.Y - _ty) / _scale);
            }
            catch
            {
                return screen;
            }
        }

        private void ZoomAt(Point anchor, double factor)
        {
            try
            {
                double next = Math.Clamp(_scale * factor, 0.2, 10.0);
                if (Math.Abs(next - _scale) < 1e-9) return;
                // keep the point under the anchor fixed
                _tx = anchor.X - (anchor.X - _tx) * (next / _scale);
                _ty = anchor.Y - (anchor.Y - _ty) * (next / _scale);
                _scale = next;
                ApplyView();
            }
            catch { }
        }

        private void CanvasHost_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                var pp = e.GetCurrentPoint(CanvasHost);
                double factor = Math.Pow(1.0016, -pp.Properties.MouseWheelDelta);
                ZoomAt(pp.Position, factor);
                e.Handled = true;
            }
            catch (Exception ex) { AppLog.Error("Wheel zoom failed", ex); }
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e) =>
            ZoomAt(new Point(CanvasHost.ActualWidth / 2, CanvasHost.ActualHeight / 2), 1.25);

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e) =>
            ZoomAt(new Point(CanvasHost.ActualWidth / 2, CanvasHost.ActualHeight / 2), 0.8);

        private void FitButton_Click(object sender, RoutedEventArgs e) => FitView();

        // ---------------------------------------------------------------- pointer interaction

        private void CanvasHost_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                var pp = e.GetCurrentPoint(CanvasHost);
                if (pp.Properties.IsRightButtonPressed || pp.Properties.IsMiddleButtonPressed)
                    return;

                try { CanvasHost.CapturePointer(e.Pointer); }
                catch { }

                _pressed = true;
                _moved = false;
                _panning = false;
                _pressPoint = pp.Position;
                _pressRegion = (e.OriginalSource as Path)?.Tag as DesignRegion;

                System.Diagnostics.Debug.WriteLine($"[PointerPressed] Tool={_tool}, Point=({pp.Position.X:F1},{pp.Position.Y:F1}), IsPan={IsPanGesture(e)}");

                if (_tool == Tool.Brush && !IsPanGesture(e))
                {
                    _brushing = true;
                    _liveStroke = new Stroke(_activeColor, _brushSize);
                    _liveStroke.Points.Add(ClampDesign(ToDesign(pp.Position)));
                    var path = DesignRenderer.BuildStrokePath(_liveStroke);
                    _strokePaths.Add(path);
                    StrokeLayer.Children.Add(path);
                }
                e.Handled = true;
            }
            catch (Exception ex) { AppLog.Error("PointerPressed failed", ex); }
        }

        private bool _isSpacePressed;

        private bool IsPanGesture(PointerRoutedEventArgs e)
        {
            if (_isSpacePressed) return true;
            try
            {
                return Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Space)
                    .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
            }
            catch
            {
                return false;
            }
        }

        private void CanvasHost_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                if (!_pressed) return;
                var pp = e.GetCurrentPoint(CanvasHost);
                if (_brushing && _liveStroke != null)
                {
                    var d = ClampDesign(ToDesign(pp.Position));
                    var last = _liveStroke.Points[^1];
                    if (Math.Sqrt((d.X - last.X) * (d.X - last.X) + (d.Y - last.Y) * (d.Y - last.Y)) > 2.5)
                    {
                        _liveStroke.Points.Add(d);
                        if (_strokePaths.Count > 0)
                            DesignRenderer.UpdateStrokePath(_strokePaths[^1], _liveStroke);
                    }
                    return;
                }

                double dx = pp.Position.X - _pressPoint.X, dy = pp.Position.Y - _pressPoint.Y;
                if (!_moved && Math.Sqrt(dx * dx + dy * dy) > 8) _moved = true;
                if (_moved)
                {
                    _panning = true;
                    _tx += pp.Position.X - _pressPoint.X;
                    _ty += pp.Position.Y - _pressPoint.Y;
                    _pressPoint = pp.Position;
                    ApplyView();
                }
            }
            catch (Exception ex) { AppLog.Error("PointerMoved failed", ex); }
        }

        private void CanvasHost_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                if (!_pressed) return;
                try { CanvasHost.ReleasePointerCapture(e.Pointer); }
                catch { }

                System.Diagnostics.Debug.WriteLine($"[PointerReleased] Tool={_tool}, Brushing={_brushing}, LiveStrokePts={_liveStroke?.Points.Count}");

                if (_tool == Tool.Brush)
                {
                    CommitLiveStroke();
                }
                else if (!_panning && _pressRegion != null)
                {
                    ApplyToolToRegion(_pressRegion);
                }
                _pressed = false;
                _brushing = false;
                _panning = false;
                _pressRegion = null;
            }
            catch (Exception ex) { AppLog.Error("PointerReleased failed", ex); }
        }

        private void CanvasHost_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[PointerCaptureLost] Brushing={_brushing}, LiveStrokePts={_liveStroke?.Points.Count}");
            if (_tool == Tool.Brush)
            {
                CommitLiveStroke();
            }
            _pressed = false;
            _brushing = false;
            _panning = false;
        }

        private void CommitLiveStroke()
        {
            try
            {
                if (_brushing && _liveStroke != null && _liveStroke.Points.Count > 0)
                {
                    var stroke = _liveStroke;
                    var path = _strokePaths.Count > 0 ? _strokePaths[^1] : null;
                    int index = _strokes.Count;
                    _strokes.Add(stroke);
                    System.Diagnostics.Debug.WriteLine($"[CommitLiveStroke] Committed stroke with {stroke.Points.Count} points. Total strokes: {_strokes.Count}");
                    _history.PushApplied(new ActionOp("Brush stroke",
                        () => RemoveStrokeAt(index),
                        () => RestoreStrokeAt(index, stroke, path!)));
                    _liveStroke = null;
                    MarkDirty();
                }
                else
                {
                    _liveStroke = null;
                }
            }
            catch (Exception ex)
            {
                AppLog.Error("CommitLiveStroke failed", ex);
                _liveStroke = null;
            }
        }

        private static Point ClampDesign(Point p) => new(Math.Clamp(p.X, -50, 1050), Math.Clamp(p.Y, -50, 1050));

        private void RemoveStrokeAt(int index)
        {
            try
            {
                if (index >= 0 && index < _strokes.Count)
                {
                    _strokes.RemoveAt(index);
                    if (index < _strokePaths.Count)
                    {
                        var path = _strokePaths[index];
                        _strokePaths.RemoveAt(index);
                        if (path != null && StrokeLayer.Children.Contains(path))
                        {
                            StrokeLayer.Children.Remove(path);
                        }
                    }
                    MarkDirty();
                }
            }
            catch (Exception ex)
            {
                AppLog.Error("RemoveStrokeAt failed", ex);
            }
        }

        private void RestoreStrokeAt(int index, Stroke stroke, Path path)
        {
            try
            {
                int at = Math.Clamp(index, 0, _strokes.Count);
                if (!_strokes.Contains(stroke))
                {
                    _strokes.Insert(at, stroke);
                }
                if (!_strokePaths.Contains(path))
                {
                    int atPath = Math.Clamp(index, 0, _strokePaths.Count);
                    _strokePaths.Insert(atPath, path);
                }
                if (path != null && !StrokeLayer.Children.Contains(path))
                {
                    StrokeLayer.Children.Add(path);
                }
                MarkDirty();
            }
            catch (Exception ex)
            {
                AppLog.Error("RestoreStrokeAt failed", ex);
            }
        }

        // ---------------------------------------------------------------- tools

        private void ApplyToolToRegion(DesignRegion region)
        {
            try
            {
                if (_tool == Tool.Eraser)
                {
                    EraseRegion(region);
                }
                else
                {
                    var before = _progress.Fills.TryGetValue(region.Id, out var b) ? b.Clone() : null;
                    var after = new StoredFill(_activeColor, _texture);
                    if (before != null && before.Color == after.Color && before.Texture == after.Texture) return;
                    _history.Push(new ActionOp("Fill area",
                        () => SetRegionFill(region.Id, before),
                        () => SetRegionFill(region.Id, after)));
                    PushRecentColor(_activeColor);
                }
            }
            catch (Exception ex)
            {
                AppLog.Error("ApplyToolToRegion failed", ex);
            }
        }

        private void SetRegionFill(string regionId, StoredFill? fill)
        {
            try
            {
                if (fill == null) _progress.Fills.Remove(regionId);
                else _progress.Fills[regionId] = fill.Clone();
                if (_regionPaths.TryGetValue(regionId, out var path))
                    DesignRenderer.ApplyFill(path, fill);
                UpdateProgressUi();
                MarkDirty();
            }
            catch (Exception ex)
            {
                AppLog.Error("SetRegionFill failed", ex);
            }
        }

        private void EraseRegion(DesignRegion region)
        {
            try
            {
                var before = _progress.Fills.TryGetValue(region.Id, out var b) ? b.Clone() : null;
                var bounds = DesignRenderer.RegionBounds(region);
                bounds.Intersect(new Rect(0, 0, 1000, 1000));
                var removed = new List<int>();
                for (int i = 0; i < _strokes.Count; i++)
                {
                    if (Overlaps(_strokes[i].Bounds(), bounds) || ContainsPoint(bounds, _strokes[i].Bounds()))
                        removed.Add(i);
                }
                if (before == null && removed.Count == 0) return;

                var snapshot = removed.Select(i => (Index: i, Stroke: _strokes[i], Path: _strokePaths[i])).ToList();
                _history.Push(new ActionOp("Erase",
                    () =>
                    {
                        SetRegionFill(region.Id, before);
                        foreach (var entry in snapshot.OrderByDescending(s => s.Index))
                            RestoreStrokeAt(entry.Index, entry.Stroke, entry.Path);
                    },
                    () =>
                    {
                        SetRegionFill(region.Id, null);
                        foreach (var entry in snapshot.OrderByDescending(s => s.Index))
                            RemoveStrokeAt(entry.Index);
                    }));
            }
            catch (Exception ex)
            {
                AppLog.Error("EraseRegion failed", ex);
            }
        }

        private static bool ContainsPoint(Rect outer, Rect inner) =>
            outer.X <= inner.X && outer.Y <= inner.Y &&
            outer.X + outer.Width >= inner.X + inner.Width &&
            outer.Y + outer.Height >= inner.Y + inner.Height;

        private static bool Overlaps(Rect a, Rect b) =>
            a.X < b.X + b.Width && b.X < a.X + a.Width &&
            a.Y < b.Y + b.Height && b.Y < a.Y + a.Height;

        private void FillTool_Click(object sender, RoutedEventArgs e) => SetTool(Tool.Fill);
        private void BrushTool_Click(object sender, RoutedEventArgs e) => SetTool(Tool.Brush);
        private void EraserTool_Click(object sender, RoutedEventArgs e) => SetTool(Tool.Eraser);

        private void SetTool(Tool tool)
        {
            try
            {
                _tool = tool;
                FillToolButton.IsChecked = tool == Tool.Fill;
                BrushToolButton.IsChecked = tool == Tool.Brush;
                EraserToolButton.IsChecked = tool == Tool.Eraser;
                BrushSizeSlider.IsEnabled = tool == Tool.Brush;
            }
            catch { }
        }

        private void BrushSizeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (!_loaded) return;
            _brushSize = e.NewValue;
        }

        private void TextureButton_Click(object sender, RoutedEventArgs e)
        {
            // Flyout opens automatically; contents were built in BuildTextureFlyout.
        }

        private void BuildTextureFlyout()
        {
            TextureFlyoutPanel.Children.Clear();
            var header = new TextBlock { Text = "Fill texture", Style = (Style)Application.Current.Resources["SectionText"], FontSize = 14 };
            TextureFlyoutPanel.Children.Add(header);
            foreach (var tex in Textures.All)
            {
                string id = tex.Id;
                var row = new Button
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(8, 6, 8, 6),
                    Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                };
                var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
                var preview = new Border { Width = 28, Height = 28, CornerRadius = new CornerRadius(6) };
                preview.Background = Textures.BrushFor(id, _activeColor);
                panel.Children.Add(preview);
                panel.Children.Add(new TextBlock { Text = tex.Name, VerticalAlignment = VerticalAlignment.Center });
                row.Content = panel;
                row.Click += (_, _) =>
                {
                    _texture = id;
                    TextureButtonText.Text = tex.Name;
                    ActiveTextureText.Text = tex.Name;
                    App.Store.Profile.Settings.Texture = id;
                    MarkDirty();
                    TextureFlyout.Hide();
                };
                AutomationProperties.SetName(row, "Texture " + tex.Name);
                TextureFlyoutPanel.Children.Add(row);
            }
        }

        // ---------------------------------------------------------------- progress / persistence

        private void UpdateProgressUi()
        {
            try
            {
                if (_design.Id == DesignCatalog.FreeDrawId || _design.Category == DesignCatalog.CatFreeDraw)
                {
                    PercentText.Text = "Free Draw";
                    return;
                }
                int pct = _progress.Percent(_design.Regions.Count);
                PercentText.Text = pct + "%";
                bool complete = _progress.Fills.Count >= _design.Regions.Count;
                bool wasComplete = _progress.Completed;
                _progress.Completed = complete;
                if (complete && !wasComplete)
                {
                    _ = ShowCompletionAsync();
                }
            }
            catch { }
        }

        private async System.Threading.Tasks.Task ShowCompletionAsync()
        {
            try
            {
                var dlg = new ContentDialog
                {
                    Title = "Beautiful work!",
                    Content = "You colored every area of " + _design.Title + ". Export or print it to keep it.",
                    PrimaryButtonText = "Export PNG",
                    CloseButtonText = "Keep coloring",
                    XamlRoot = XamlRoot,
                };
                var result = await dlg.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await ArtworkIO.ExportPngAsync(App.MainWnd!, _design, _progress, _strokes);
                }
            }
            catch (Exception ex) { AppLog.Error("Completion dialog failed", ex); }
        }

        private void MarkDirty()
        {
            try
            {
                _progress.UpdatedUtc = DateTime.UtcNow;
                StoreStrokes();
                App.Store.SaveDebounced(800);
            }
            catch { }
        }

        private void StoreStrokes()
        {
            try
            {
                _progress.Strokes = _strokes.Select(s => new StoredStroke
                {
                    Color = s.Color,
                    Width = s.Width,
                    Pts = s.Points.Select(p => new[] { Math.Round(p.X, 1), Math.Round(p.Y, 1) }).ToList(),
                }).ToList();
            }
            catch { }
        }

        private void FlushSave()
        {
            try
            {
                _progress.UpdatedUtc = DateTime.UtcNow;
                StoreStrokes();
                System.Diagnostics.Debug.WriteLine($"[StudioPage.FlushSave] Saving design '{_design.Id}' ({_design.Title})");
                System.Diagnostics.Debug.WriteLine($"  - Total strokes: {_strokes.Count}, Fills: {_progress.Fills.Count}");
                System.Diagnostics.Debug.WriteLine($"  - Saving to file: {App.Store.FilePath}");
                App.Store.Save();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StudioPage.FlushSave] ERROR: {ex.Message}");
                AppLog.Error("FlushSave failed", ex);
            }
        }

        // ---------------------------------------------------------------- undo / redo

        private void OnHistoryChanged()
        {
            UndoButton.IsEnabled = _history.CanUndo;
            RedoButton.IsEnabled = _history.CanRedo;
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e) => _history.Undo();

        private void RedoButton_Click(object sender, RoutedEventArgs e) => _history.Redo();

        private void UndoAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (FocusInTextInput()) return;
            _history.Undo();
            args.Handled = true;
        }

        private void RedoAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (FocusInTextInput()) return;
            _history.Redo();
            args.Handled = true;
        }

        private void ToolAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (FocusInTextInput()) return;
            if (sender.Key == Windows.System.VirtualKey.F) SetTool(Tool.Fill);
            else if (sender.Key == Windows.System.VirtualKey.B) SetTool(Tool.Brush);
            else if (sender.Key == Windows.System.VirtualKey.E) SetTool(Tool.Eraser);
            args.Handled = true;
        }

        private void ZoomAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (FocusInTextInput()) return;
            var center = new Point(CanvasHost.ActualWidth / 2, CanvasHost.ActualHeight / 2);
            if (sender.Key == Windows.System.VirtualKey.Add) ZoomAt(center, 1.25);
            else if (sender.Key == Windows.System.VirtualKey.Subtract) ZoomAt(center, 0.8);
            else if (sender.Key == Windows.System.VirtualKey.Number0) FitView();
            args.Handled = true;
        }

        private bool FocusInTextInput()
        {
            try
            {
                var focused = FocusManager.GetFocusedElement(XamlRoot);
                return focused is TextBox or AutoSuggestBox;
            }
            catch { return false; }
        }

        private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Space && !FocusInTextInput())
                _isSpacePressed = true;
        }

        private void Page_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Space)
                _isSpacePressed = false;
        }

        // ---------------------------------------------------------------- header actions

        private void BackButton_Click(object sender, RoutedEventArgs e) => Nav.Back();

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[StudioPage.SaveButton_Click] User clicked Save button for design '{_design.Id}' ('{_design.Title}')");
            FlushSave();
            const string glyphSave = "";
            const string glyphCheck = "";
            SaveButton.Content = new FontIcon
            {
                FontFamily = new FontFamily("Segoe Fluent Icons,Segoe MDL2 Assets"),
                Glyph = glyphCheck,
                Foreground = (Brush)Application.Current.Resources["AppAccentBrush"],
            };
            _ = DispatcherQueue.TryEnqueue(async () =>
            {
                await System.Threading.Tasks.Task.Delay(1200);
                SaveButton.Content = new FontIcon
                {
                    FontFamily = new FontFamily("Segoe Fluent Icons,Segoe MDL2 Assets"),
                    Glyph = glyphSave,
                };
            });
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FlushSave();
                bool ok = await ArtworkIO.ExportPngAsync(App.MainWnd!, _design, _progress, _strokes);
                if (!ok) await ArtworkIO.ShowInfoAsync(App.MainWnd!, "Export", "The artwork was not exported.");
            }
            catch (Exception ex) { AppLog.Error("Export failed", ex); }
        }

        private async void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FlushSave();
                _printService ??= new PrintService(App.MainWnd!);
                await _printService.TryPrintAsync(_design, _progress, _strokes);
            }
            catch (Exception ex) { AppLog.Error("Print failed", ex); }
        }

        private async void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FlushSave();
                await ArtworkIO.ShareAsync(App.MainWnd!, _design, _progress, _strokes);
            }
            catch (Exception ex) { AppLog.Error("Share failed", ex); }
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new ContentDialog
                {
                    Title = "Reset artwork?",
                    Content = "This clears every color and brush stroke on this page. You can undo it afterwards.",
                    PrimaryButtonText = "Reset",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = XamlRoot,
                };
                if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

                var fillsBefore = _progress.Fills.ToDictionary(kv => kv.Key, kv => kv.Value.Clone());
                var strokesBefore = _strokes.ToList();
                var strokePathsBefore = _strokePaths.ToList();
                _history.Push(new ActionOp("Reset artwork",
                    () =>
                    {
                        _progress.Fills.Clear();
                        foreach (var kv in fillsBefore) _progress.Fills[kv.Key] = kv.Value.Clone();
                        RebuildAllVisuals(strokesBefore, strokePathsBefore);
                    },
                    () =>
                    {
                        _progress.Fills.Clear();
                        RebuildAllVisuals(new List<Stroke>(), new List<Path>());
                    }));
            }
            catch (Exception ex) { AppLog.Error("Reset failed", ex); }
        }

        private void RebuildAllVisuals(List<Stroke> strokes, List<Path> paths)
        {
            foreach (var kv in _regionPaths)
            {
                _progress.Fills.TryGetValue(kv.Key, out var fill);
                DesignRenderer.ApplyFill(kv.Value, fill);
            }
            StrokeLayer.Children.Clear();
            _strokes.Clear();
            _strokePaths.Clear();
            for (int i = 0; i < strokes.Count; i++)
            {
                _strokes.Add(strokes[i]);
                _strokePaths.Add(paths[i]);
                StrokeLayer.Children.Add(paths[i]);
            }
            UpdateProgressUi();
            MarkDirty();
        }

        // ---------------------------------------------------------------- palette panel

        private void PopulatePaletteCombo()
        {
            _populating = true;
            PaletteCombo.Items.Clear();
            foreach (var palette in Palettes.All)
            {
                var item = new ComboBoxItem { Content = palette.Name, Tag = palette };
                PaletteCombo.Items.Add(item);
            }
            PaletteCombo.SelectedIndex = 0;
            _populating = false;
        }

        private void PaletteCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_populating) return;
            BuildSwatches();
        }

        private Palettes.PaletteDef? SelectedPalette()
        {
            return (PaletteCombo.SelectedItem as ComboBoxItem)?.Tag as Palettes.PaletteDef;
        }

        private void BuildSwatches()
        {
            _swatchButtons.RemoveAll(x => SwatchGrid.Children.Contains(x.Btn));
            SwatchGrid.Children.Clear();
            SwatchGrid.RowDefinitions.Clear();
            SwatchGrid.ColumnDefinitions.Clear();
            var palette = SelectedPalette() ?? Palettes.All[0];
            FillSwatchGrid(SwatchGrid, palette.Colors);
        }

        private void BuildRecent()
        {
            _swatchButtons.RemoveAll(x => RecentGrid.Children.Contains(x.Btn));
            RecentGrid.Children.Clear();
            RecentGrid.RowDefinitions.Clear();
            RecentGrid.ColumnDefinitions.Clear();
            var recents = App.Store.Profile.RecentColors.Take(12).ToList();
            if (recents.Count == 0)
            {
                var hint = new TextBlock
                {
                    Text = "Colors you use will appear here.",
                    Style = (Style)Application.Current.Resources["BodyText"],
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                };
                Grid.SetColumnSpan(hint, 4);
                RecentGrid.Children.Add(hint);
                return;
            }
            FillSwatchGrid(RecentGrid, recents);
        }

        private void BuildCustomSwatches()
        {
            _swatchButtons.RemoveAll(x => CustomGrid.Children.Contains(x.Btn));
            CustomGrid.Children.Clear();
            CustomGrid.RowDefinitions.Clear();
            CustomGrid.ColumnDefinitions.Clear();
            var customs = App.Store.Profile.CustomSwatches.ToList();
            CustomEmptyText.Visibility = customs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            FillSwatchGrid(CustomGrid, customs);
        }

        private void FillSwatchGrid(Grid grid, IReadOnlyList<string> hexes)
        {
            const int cols = 4;
            for (int c = 0; c < cols; c++)
                grid.ColumnDefinitions.Add(new ColumnDefinition());
            int rows = (hexes.Count + cols - 1) / cols;
            for (int r = 0; r < rows; r++)
                grid.RowDefinitions.Add(new RowDefinition());

            for (int i = 0; i < hexes.Count; i++)
            {
                string hex = hexes[i];
                var btn = new Button
                {
                    Width = 44,
                    Height = 44,
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(0),
                    Margin = new Thickness(0, 0, 6, 6),
                    Background = new SolidColorBrush(Textures.ToColor(hex)),
                    BorderThickness = new Thickness(2),
                    Tag = hex,
                    HorizontalAlignment = HorizontalAlignment.Left,
                };
                AutomationProperties.SetName(btn, "Color " + hex);
                btn.Click += Swatch_Click;
                Grid.SetColumn(btn, i % cols);
                Grid.SetRow(btn, i / cols);
                grid.Children.Add(btn);
                _swatchButtons.Add((btn, hex));
            }
            RefreshActiveMarks();
        }

        private void Swatch_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                SetActiveColor(hex);
            }
        }

        private void SetActiveColor(string hex)
        {
            try
            {
                _activeColor = hex;
                App.Store.Profile.Settings.ActiveColor = hex;
                PushRecentColor(hex);
                UpdateActiveUi();
                MarkDirty();
            }
            catch { }
        }

        private void PushRecentColor(string hex)
        {
            try
            {
                App.Store.Profile.PushRecentColor(hex);
                BuildRecent();
            }
            catch { }
        }

        private void UpdateActiveUi()
        {
            try
            {
                ActiveSwatch.Background = Textures.BrushFor(_texture, _activeColor);
                ActiveHexText.Text = _activeColor.ToUpperInvariant();
                ActiveTextureText.Text = Textures.NameOf(_texture);
                RefreshActiveMarks();
            }
            catch { }
        }

        private void RefreshActiveMarks()
        {
            try
            {
                foreach (var (btn, hex) in _swatchButtons)
                {
                    bool active = string.Equals(hex, _activeColor, StringComparison.OrdinalIgnoreCase);
                    btn.BorderBrush = active
                        ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 223, 106, 74))
                        : new SolidColorBrush(Windows.UI.Color.FromArgb(60, 0, 0, 0));
                }
            }
            catch { }
        }

        // ---------------------------------------------------------------- mixer dialog

        private async void MixButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ShowMixerAsync();
            }
            catch (Exception ex)
            {
                AppLog.Error("Mixer failed", ex);
            }
        }

        private async System.Threading.Tasks.Task ShowMixerAsync()
        {
            var (r, g, b) = ColorMath.FromHex(_activeColor);
            var hsv = ColorMath.ToHsv(r, g, b);
            bool syncing = false;
            string chosenHex = _activeColor;
            string chosenTexture = _texture;

            var preview = new Border
            {
                Width = 64,
                Height = 64,
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 200, 200, 200)),
            };

            var hexBox = new TextBox { Width = 110, Text = _activeColor.ToUpperInvariant() };
            var hSlider = MakeSlider(0, 360, hsv.H);
            var sSlider = MakeSlider(0, 100, hsv.S);
            var vSlider = MakeSlider(0, 100, hsv.V);
            var rSlider = MakeSlider(0, 255, r);
            var gSlider = MakeSlider(0, 255, g);
            var bSlider = MakeSlider(0, 255, b);

            void SyncPreview()
            {
                var (nr, ng, nb) = ColorMath.FromHex(chosenHex);
                preview.Background = Textures.BrushFor(chosenTexture, chosenHex);
                hexBox.Text = chosenHex.ToUpperInvariant();
                _ = nr; _ = ng; _ = nb;
            }

            void FromHsv()
            {
                if (syncing) return;
                syncing = true;
                var (nr, ng, nb) = ColorMath.FromHsv(hSlider.Value, sSlider.Value, vSlider.Value);
                chosenHex = ColorMath.ToHex(nr, ng, nb);
                rSlider.Value = nr; gSlider.Value = ng; bSlider.Value = nb;
                SyncPreview();
                syncing = false;
            }

            void FromRgb()
            {
                if (syncing) return;
                syncing = true;
                var rgb = ColorMath.FromHex(ColorMath.ToHex((byte)rSlider.Value, (byte)gSlider.Value, (byte)bSlider.Value));
                var nhsv = ColorMath.ToHsv(rgb.R, rgb.G, rgb.B);
                chosenHex = ColorMath.ToHex((byte)rSlider.Value, (byte)gSlider.Value, (byte)bSlider.Value);
                hSlider.Value = nhsv.H; sSlider.Value = nhsv.S; vSlider.Value = nhsv.V;
                SyncPreview();
                syncing = false;
            }

            hSlider.ValueChanged += (_, _) => FromHsv();
            sSlider.ValueChanged += (_, _) => FromHsv();
            vSlider.ValueChanged += (_, _) => FromHsv();
            rSlider.ValueChanged += (_, _) => FromRgb();
            gSlider.ValueChanged += (_, _) => FromRgb();
            bSlider.ValueChanged += (_, _) => FromRgb();
            hexBox.LostFocus += (_, _) =>
            {
                try
                {
                    var trimmed = hexBox.Text.Trim().TrimStart('#');
                    if (trimmed.Length == 6)
                    {
                        var (nr, ng, nb) = ColorMath.FromHex(trimmed);
                        chosenHex = ColorMath.ToHex(nr, ng, nb);
                        rSlider.Value = nr; gSlider.Value = ng; bSlider.Value = nb;
                        FromRgb();
                    }
                }
                catch { }
                SyncPreview();
            };

            var stack = new StackPanel { Spacing = 12, MinWidth = 330 };
            var topRow = new Grid { ColumnSpacing = 14 };
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(preview, 0);
            var hexLabel = new StackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
            hexLabel.Children.Add(new TextBlock { Text = "Hex", Style = (Style)Application.Current.Resources["BodyText"], FontSize = 12 });
            hexLabel.Children.Add(hexBox);
            Grid.SetColumn(hexLabel, 1);
            topRow.Children.Add(preview);
            topRow.Children.Add(hexLabel);
            stack.Children.Add(topRow);

            stack.Children.Add(MakeLabeledSlider("Hue", hSlider));
            stack.Children.Add(MakeLabeledSlider("Saturation", sSlider));
            stack.Children.Add(MakeLabeledSlider("Brightness", vSlider));
            stack.Children.Add(MakeLabeledSlider("Red", rSlider));
            stack.Children.Add(MakeLabeledSlider("Green", gSlider));
            stack.Children.Add(MakeLabeledSlider("Blue", bSlider));

            stack.Children.Add(new TextBlock
            {
                Text = "Texture",
                Style = (Style)Application.Current.Resources["SectionText"],
                FontSize = 14,
                Margin = new Thickness(0, 6, 0, 0),
            });
            var texGrid = new Grid { ColumnSpacing = 6 };
            for (int c = 0; c < 3; c++) texGrid.ColumnDefinitions.Add(new ColumnDefinition());
            texGrid.RowDefinitions.Add(new RowDefinition());
            texGrid.RowDefinitions.Add(new RowDefinition());
            for (int i = 0; i < Textures.All.Length; i++)
            {
                var tex = Textures.All[i];
                var btn = new ToggleButton
                {
                    Content = tex.Name,
                    FontSize = 12,
                    MinWidth = 92,
                    CornerRadius = new CornerRadius(8),
                    IsChecked = tex.Id == chosenTexture,
                    Tag = tex.Id,
                };
                btn.Click += (_, _) =>
                {
                    chosenTexture = tex.Id;
                    foreach (var child in texGrid.Children.OfType<ToggleButton>())
                        child.IsChecked = ReferenceEquals(child, btn);
                    SyncPreview();
                };
                Grid.SetColumn(btn, i % 3);
                Grid.SetRow(btn, i / 3);
                if (i >= 3) btn.Margin = new Thickness(0, 6, 0, 0);
                texGrid.Children.Add(btn);
            }
            stack.Children.Add(texGrid);
            SyncPreview();

            var dlg = new ContentDialog
            {
                Title = "Mix a color",
                Content = stack,
                PrimaryButtonText = "Use color",
                SecondaryButtonText = "Save swatch",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot,
            };
            dlg.PrimaryButtonClick += (_, _) =>
            {
                _texture = chosenTexture;
                SetActiveColor(chosenHex);
            };
            dlg.SecondaryButtonClick += (_, _) =>
            {
                _texture = chosenTexture;
                App.Store.Profile.CustomSwatches.RemoveAll(c => string.Equals(c, chosenHex, StringComparison.OrdinalIgnoreCase));
                App.Store.Profile.CustomSwatches.Insert(0, chosenHex);
                if (App.Store.Profile.CustomSwatches.Count > 16)
                    App.Store.Profile.CustomSwatches.RemoveRange(16, App.Store.Profile.CustomSwatches.Count - 16);
                SetActiveColor(chosenHex);
                BuildCustomSwatches();
            };
            await dlg.ShowAsync();
        }

        private static Slider MakeSlider(double min, double max, double value) => new()
        {
            Minimum = min,
            Maximum = max,
            Value = value,
            StepFrequency = 1,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        private static StackPanel MakeLabeledSlider(string label, Slider slider)
        {
            var sp = new StackPanel { Spacing = 2 };
            sp.Children.Add(new TextBlock
            {
                Text = label,
                Style = (Style)Application.Current.Resources["BodyText"],
                FontSize = 12,
            });
            sp.Children.Add(slider);
            return sp;
        }
    }
}

