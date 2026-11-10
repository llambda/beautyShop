using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BeautyStudio.Services;

/// <summary>Dialoghi applicativi coerenti con il tema Material Design di BeautyStudio.</summary>
public static class StyledMessageBox
{
    public static MessageBoxResult Show(string message, string caption,
        MessageBoxButton buttons = MessageBoxButton.OK,
        MessageBoxImage image = MessageBoxImage.None)
    {
        var owner = Application.Current?.MainWindow;
        var window = new Window
        {
            Title = caption,
            Width = 460,
            MinWidth = 360,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = owner is { IsVisible: true }
                ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
            Owner = owner is { IsVisible: true } ? owner : null,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            Background = Brush("MaterialDesign.Brush.Background", SystemColors.WindowBrush),
            Foreground = Brush("MaterialDesign.Brush.Foreground", SystemColors.WindowTextBrush),
            FontFamily = new FontFamily("Segoe UI")
        };

        var root = new StackPanel { Margin = new Thickness(26) };
        var heading = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 14) };
        var glyph = image switch
        {
            MessageBoxImage.Warning => "⚠",
            MessageBoxImage.Error => "!",
            MessageBoxImage.Question => "?",
            MessageBoxImage.Information => "i",
            _ => "•"
        };
        var accent = image switch
        {
            MessageBoxImage.Error => new SolidColorBrush(Color.FromRgb(198, 40, 40)),
            MessageBoxImage.Warning => new SolidColorBrush(Color.FromRgb(230, 145, 30)),
            _ => Brush("MaterialDesign.Brush.Primary", SystemColors.HighlightBrush)
        };
        heading.Children.Add(new Border
        {
            Width = 34, Height = 34, CornerRadius = new CornerRadius(17),
            Background = accent, Margin = new Thickness(0, 0, 12, 0),
            Child = new TextBlock { Text = glyph, FontSize = 19, FontWeight = FontWeights.Bold,
                Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Center }
        });
        heading.Children.Add(new TextBlock { Text = caption, FontSize = 20, FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap });
        root.Children.Add(heading);
        root.Children.Add(new TextBlock { Text = message, FontSize = 14, TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(46, 0, 0, 24), MaxWidth = 350,
            Foreground = Brush("MaterialDesign.Brush.Foreground", SystemColors.WindowTextBrush) });

        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        MessageBoxResult result = MessageBoxResult.None;
        void AddButton(string label, MessageBoxResult value, bool primary)
        {
            var button = new Button
            {
                Content = label, MinWidth = 92, MinHeight = 38, Padding = new Thickness(16, 7, 16, 7),
                Margin = new Thickness(8, 0, 0, 0), IsDefault = primary,
                Background = primary ? Brush("MaterialDesign.Brush.Primary", SystemColors.HighlightBrush) : Brushes.Transparent,
                Foreground = primary ? Brushes.White : Brush("MaterialDesign.Brush.Foreground", SystemColors.WindowTextBrush),
                BorderBrush = primary ? Brushes.Transparent : Brush("MaterialDesign.Brush.Primary", SystemColors.HighlightBrush),
                BorderThickness = new Thickness(1), Cursor = System.Windows.Input.Cursors.Hand
            };
            button.Click += (_, _) => { result = value; window.DialogResult = true; };
            actions.Children.Add(button);
        }

        if (buttons == MessageBoxButton.OK || buttons == MessageBoxButton.OKCancel)
        {
            if (buttons == MessageBoxButton.OKCancel) AddButton("Annulla", MessageBoxResult.Cancel, false);
            AddButton("OK", MessageBoxResult.OK, true);
        }
        else if (buttons == MessageBoxButton.YesNo)
        {
            AddButton("No", MessageBoxResult.No, false);
            AddButton("Sì", MessageBoxResult.Yes, true);
        }
        else
        {
            AddButton("Annulla", MessageBoxResult.Cancel, false);
            AddButton("No", MessageBoxResult.No, false);
            AddButton("Sì", MessageBoxResult.Yes, true);
        }

        root.Children.Add(actions);
        window.Content = root;
        window.KeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Escape) { result = MessageBoxResult.Cancel; window.DialogResult = false; } };
        window.ShowDialog();
        return result;
    }

    private static Brush Brush(string key, Brush fallback)
    {
        try { return Application.Current?.TryFindResource(key) as Brush ?? fallback; }
        catch { return fallback; }
    }
}