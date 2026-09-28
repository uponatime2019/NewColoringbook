using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using NewColoringbook.Core;
using NewColoringbook.Services;

namespace NewColoringbook.Views
{
    /// <summary>
    /// Gallery home: top app bar (menu/search/settings), category chips, continue-coloring
    /// progress cards and the responsive explore grid with sorting.
    /// </summary>
    public sealed partial class GalleryPage : Page
    {
        private sealed class Chip
        {
            public string Label = "";
            public string Tag = "";
            public Button? Button;
        }

        private readonly List<Chip> _chips = new();
        private string _category = "Home";
        private string _sortMode = "popular";
        private string _complexityFilter = "all";
        private string _search = "";
        private bool _sortWired;
        private bool _complexityWired;
        private bool _initialized;

        public GalleryPage()
        {
            InitializeComponent();
            NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
            Loaded += OnLoaded;
        }

        private Brush Res(string key)
        {
            try { return (Brush)Application.Current.Resources[key]; }
            catch { return new SolidColorBrush(Windows.UI.Color.FromArgb(0, 255, 255, 255)); }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_initialized)
                {
                    InitAndRefresh();
                }
            }
            catch (Exception ex)
            {
                AppLog.Error("GalleryPage load failed", ex);
            }
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            try
            {
                if (!_initialized)
                {
                    InitAndRefresh();
                }
                else
                {
                    // Returning from another page (Studio, Settings, Free Draw) - do not wipe the grid!
                    if (_category == "My Artwork")
                    {
                        Refresh();
                    }
                    else
                    {
                        RebuildContinue();
                        UpdateReturningGridCards();
                    }
                }
            }
            catch (Exception ex) { AppLog.Error("GalleryPage nav-to refresh failed", ex); }
        }

        private void InitAndRefresh()
        {
            _initialized = true;
            if (_chips.Count == 0)
            {
                AddChip("Home", "Home");
                AddChip("Free Draw", DesignCatalog.CatFreeDraw);
                AddChip("My Artwork", "My Artwork");
                AddChip("Animals", DesignCatalog.CatAnimals);
                AddChip("Nature", DesignCatalog.CatNature);
                AddChip("Mandalas", DesignCatalog.CatMandalas);
                AddChip("Fantasy", DesignCatalog.CatFantasy);
                AddChip("Floral", DesignCatalog.CatFloral);
                AddChip("Geometric", DesignCatalog.CatGeometric);
                if (!_sortWired)
                {
                    SortCombo.SelectedIndex = 0;
                    _sortWired = true;
                }
                if (!_complexityWired)
                {
                    ComplexityCombo.SelectedIndex = 0;
                    _complexityWired = true;
                }
            }
            Refresh();
        }

        private void UpdateReturningGridCards()
        {
            try
            {
                for (int i = 0; i < DesignGrid.Items.Count; i++)
                {
                    if (DesignGrid.Items[i] is Button oldCard && oldCard.Tag is string designId)
                    {
                        var design = DesignCatalog.Find(designId);
                        if (design != null)
                        {
                            var progress = App.Store.Profile.ProgressFor(design.Id);
                            bool hasProgress = progress.Fills.Count > 0 || (progress.Strokes != null && progress.Strokes.Count > 0);
                            if (hasProgress)
                            {
                                int pct = progress.Percent(design.Regions.Count);
                                DesignGrid.Items[i] = MakeCard(design, pct, 204, 284, compact: false);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Error("UpdateReturningGridCards failed", ex);
            }
        }

        private void AddChip(string label, string tag)
        {
            var chip = new Chip { Label = label, Tag = tag };
            var btn = new Button
            {
                Content = label,
                MinWidth = 96,
                Height = 34,
                CornerRadius = new CornerRadius(17),
                FontSize = 13,
                Tag = chip,
            };
            AutomationProperties.SetName(btn, label + " category");
            btn.Click += Chip_Click;
            chip.Button = btn;
            _chips.Add(chip);
            CategoryPanel.Children.Add(btn);
        }

        private void Chip_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is Chip chip)
                {
                    _category = chip.Tag;
                    Refresh();
                }
            }
            catch (Exception ex) { AppLog.Error("Chip_Click failed", ex); }
        }

        private void Refresh()
        {
            UpdateChipStyles();
            RebuildContinue();
            RebuildGrid();
        }

        private void UpdateChipStyles()
        {
            foreach (var chip in _chips)
            {
                if (chip.Button == null) continue;
                bool active = chip.Tag == _category;
                chip.Button.Background = active ? Res("AppAccentBrush") : new SolidColorBrush(Windows.UI.Color.FromArgb(0, 255, 255, 255));
                chip.Button.Foreground = active ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)) : Res("AppSubtleBrush");
                chip.Button.BorderThickness = active ? new Thickness(0) : new Thickness(1);
                chip.Button.BorderBrush = Res("AppLineBrush");
            }
        }

        private IEnumerable<DesignDef> VisibleDesigns()
        {
            IEnumerable<DesignDef> seq = DesignCatalog.All;
            if (_category == "My Artwork")
            {
                seq = seq.Where(d => App.Store.Profile.Designs.TryGetValue(d.Id, out var p) && (p.Fills.Count > 0 || (p.Strokes != null && p.Strokes.Count > 0)));
            }
            else if (_category == "Home")
            {
                seq = seq.Where(d => d.Category != DesignCatalog.CatFreeDraw);
            }
            else
            {
                seq = seq.Where(d => d.Category == _category);
            }
            if (!string.IsNullOrWhiteSpace(_search))
            {
                var q = _search.Trim();
                seq = seq.Where(d =>
                    d.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    d.Category.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    d.Tags.Any(t => t.Contains(q, StringComparison.OrdinalIgnoreCase)));
            }
            if (_complexityFilter != "all" && int.TryParse(_complexityFilter, out int targetRating))
            {
                seq = seq.Where(d => d.ComplexityRating == targetRating);
            }
            return _sortMode switch
            {
                "az" => seq.OrderBy(d => d.Title),
                "progress" => seq.OrderByDescending(d => App.Store.Profile.ProgressFor(d.Id).Percent(d.Regions.Count)),
                "complex_asc" => seq.OrderBy(d => d.Regions.Count),
                "complex_desc" => seq.OrderByDescending(d => d.Regions.Count),
                _ => seq.OrderBy(d => d.Popularity),
            };
        }

        private void RebuildContinue()
        {
            ContinueList.Children.Clear();
            var inProgress = DesignCatalog.All
                .Where(d => d.Category != DesignCatalog.CatFreeDraw &&
                            App.Store.Profile.Designs.TryGetValue(d.Id, out var p) &&
                            (p.Fills.Count > 0 || (p.Strokes != null && p.Strokes.Count > 0)) && !p.Completed)
                .OrderByDescending(d => App.Store.Profile.Designs[d.Id].UpdatedUtc)
                .Take(6)
                .ToList();
            ContinueSection.Visibility = inProgress.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            foreach (var design in inProgress)
            {
                var progress = App.Store.Profile.ProgressFor(design.Id);
                int pct = progress.Percent(design.Regions.Count);
                ContinueList.Children.Add(MakeCard(design, pct, 190, 252, compact: true));
            }
        }

        private void RebuildGrid()
        {
            DesignGrid.Items.Clear();
            var designs = VisibleDesigns().ToList();
            EmptyResultsText.Visibility = designs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            foreach (var design in designs)
            {
                var progress = App.Store.Profile.ProgressFor(design.Id);
                DesignGrid.Items.Add(MakeCard(design, progress.Percent(design.Regions.Count), 204, 284, compact: false));
            }
        }

        /// <summary>Build one design card (preview artwork + title + progress).</summary>
        private Button MakeCard(DesignDef design, int percent, double width, double height, bool compact)
        {
            var progress = App.Store.Profile.ProgressFor(design.Id);
            bool isFreeDraw = design.Category == DesignCatalog.CatFreeDraw || design.Id.StartsWith("free_draw");
            bool hasProgress = progress.Fills.Count > 0 || (progress.Strokes != null && progress.Strokes.Count > 0);

            var preview = DesignRenderer.BuildPreview(
                design,
                hasProgress ? progress : null,
                compact ? width - 16 : width - 20,
                previewColors: !hasProgress && !isFreeDraw);

            var previewHost = new Border
            {
                Child = (FrameworkElement)preview,
                CornerRadius = new CornerRadius(10),
                Background = Res("AppPaperBrush"),
                Padding = new Thickness(4),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };

            var previewGrid = new Grid();
            previewGrid.Children.Add(previewHost);

            var idBadge = new Border
            {
                Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AppChromeBrush"],
                BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AppLineBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(6, 6, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Child = new TextBlock
                {
                    Text = design.DisplayNumber,
                    FontSize = 11,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AppAccentBrush"],
                }
            };
            previewGrid.Children.Add(idBadge);

            var title = new TextBlock
            {
                Text = $"{design.DisplayNumber}  {design.Title}",
                Style = (Style)Application.Current.Resources["SectionText"],
                FontSize = compact ? 15 : 16,
                Margin = new Thickness(2, 8, 2, 0),
                TextTrimming = TextTrimming.CharacterEllipsis,
            };

            var meta = new TextBlock
            {
                Style = (Style)Application.Current.Resources["BodyText"],
                FontSize = 12,
                Margin = new Thickness(2, 2, 2, 0),
                Text = isFreeDraw
                    ? (hasProgress ? $"{design.Category}  ·  {progress.Strokes?.Count ?? 0} strokes" : $"{design.Category}  ·  Blank Canvas")
                    : (percent >= 100
                        ? $"{design.Category}  ·  completed  ·  {design.ComplexityStars}"
                        : $"{design.Category}  ·  {design.Regions.Count} areas  ·  {design.ComplexityStars}"),
            };

            var bar = new ProgressBar
            {
                Value = percent,
                Maximum = 100,
                Height = 6,
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(2, 6, 2, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Visibility = isFreeDraw ? Visibility.Collapsed : Visibility.Visible,
            };

            var stack = new StackPanel();
            stack.Children.Add(previewGrid);
            stack.Children.Add(title);
            stack.Children.Add(meta);
            stack.Children.Add(bar);
            Grid.SetRowSpan(stack, 1);

            var card = new Button
            {
                Content = stack,
                Width = width,
                Height = height,
                Padding = new Thickness(8),
                CornerRadius = new CornerRadius(14),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Tag = design.Id,
            };
            AutomationProperties.SetName(card, design.Title + ", " + percent + " percent complete, " + design.ComplexityLabel);
            card.Click += Card_Click;
            return card;
        }

        private void Card_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is string id)
                {
                    Nav.Studio(id);
                }
            }
            catch (Exception ex) { AppLog.Error("Card_Click failed", ex); }
        }

        // ---------------------------------------------------------------- search & sort handlers

        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            try
            {
                _search = args.QueryText ?? "";
                Refresh();
            }
            catch (Exception ex) { AppLog.Error("Search submit failed", ex); }
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            try
            {
                if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
                var text = sender.Text ?? "";
                if (text.Trim().Length == 0)
                {
                    sender.ItemsSource = null;
                    if (_search.Length > 0)
                    {
                        _search = "";
                        Refresh();
                    }
                    return;
                }
                var suggestions = DesignCatalog.All
                    .Where(d => d.Title.Contains(text, StringComparison.OrdinalIgnoreCase))
                    .Select(d => d.Title)
                    .Take(6)
                    .ToList();
                sender.ItemsSource = suggestions;
            }
            catch (Exception ex) { AppLog.Error("Search text changed failed", ex); }
        }

        private void ComplexityCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ComplexityCombo.SelectedItem is ComboBoxItem item && item.Tag is string filter)
                {
                    _complexityFilter = filter;
                    if (_complexityWired) Refresh();
                }
            }
            catch (Exception ex) { AppLog.Error("Complexity filter change failed", ex); }
        }

        private void SortCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (SortCombo.SelectedItem is ComboBoxItem item && item.Tag is string mode)
                {
                    _sortMode = mode;
                    if (_sortWired) Refresh();
                }
            }
            catch (Exception ex) { AppLog.Error("Sort change failed", ex); }
        }

        // ---------------------------------------------------------------- top bar handlers

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            // Flyout opens automatically; nothing else to do.
        }

        private void FreeDrawButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Nav.Studio(DesignCatalog.FreeDrawId);
            }
            catch (Exception ex)
            {
                AppLog.Error("FreeDrawButton_Click failed", ex);
            }
        }

        private void MenuHome_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _category = "Home";
                _search = "";
                SearchBox.Text = "";
                Refresh();
            }
            catch (Exception ex) { AppLog.Error("MenuHome failed", ex); }
        }

        private void MenuArtwork_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _category = "My Artwork";
                Refresh();
            }
            catch (Exception ex) { AppLog.Error("MenuArtwork failed", ex); }
        }

        private void MenuSettings_Click(object sender, RoutedEventArgs e) => Nav.Settings();

        private void SettingsButton_Click(object sender, RoutedEventArgs e) => Nav.Settings();
    }
}

