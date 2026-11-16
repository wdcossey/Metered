using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Metered.Controls;
using Button = Metered.Controls.Button;

namespace Metered.Tests.Controls;

/// <summary>
/// Drawn pixels: Base Web's hover overlay is an inset box-shadow (inset 999px 999px 0), its rings
/// spread shadows; Avalonia's BoxShadows draw the same pixels. And, when METERED_FRAMES names a
/// folder, a gallery of every kind and state, light and dark, saved there for docs/controls/Button.
/// </summary>
public class ButtonFrames
{
    public const string Variable = "METERED_FRAMES";

    [AvaloniaFact]
    public void Hover_overlay_is_drawn_over_the_fill_under_the_text()
    {
        // Light primary: #000000 under rgba(255,255,255,0.1) is #1A1A1A.
        var button = new Button { Content = "", Width = 120 };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { button } });
        var frame = scene.Capture();
        Assert.Equal(Color.Parse("#FF000000"), Frames.PixelAt(frame, 80, 44));

        scene.Hover(button);
        frame = scene.Capture();
        Assert.Equal(Color.Parse("#FF1A1A1A"), Frames.PixelAt(frame, 80, 44));
        // Corners keep their radius: the overlay is clipped as the fill is.
        Assert.Equal(Color.Parse("#FFFFFFFF"), Frames.PixelAt(frame, 20, 20));
    }

    [AvaloniaFact]
    public void Focus_ring_is_two_pixels_inside_and_two_outside()
    {
        // Light primary, focus-visible: inset 0 0 0 2px #FFFFFF, 0 0 0 2px #276EF1.
        var button = new Button { Content = "", Width = 120, Height = 48 };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { button } });
        button.Focus(NavigationMethod.Tab);
        var frame = scene.Capture();
        var y = 20 + 24;
        Assert.Equal(Color.Parse("#FF276EF1"), Frames.PixelAt(frame, 18, y)); // outside
        Assert.Equal(Color.Parse("#FF276EF1"), Frames.PixelAt(frame, 19, y));
        Assert.Equal(Color.Parse("#FFFFFFFF"), Frames.PixelAt(frame, 20, y)); // inside
        Assert.Equal(Color.Parse("#FFFFFFFF"), Frames.PixelAt(frame, 21, y));
        Assert.Equal(Color.Parse("#FF000000"), Frames.PixelAt(frame, 22, y)); // the fill
        Assert.NotEqual(Color.Parse("#FF276EF1"), Frames.PixelAt(frame, 17, y));
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Gallery(string theme)
    {
        var folder = Environment.GetEnvironmentVariable(Variable);
        if (string.IsNullOrEmpty(folder)) return;

        var kinds = Enum.GetValues<ButtonKind>();
        string[] states = ["rest", "selected", "loading", "disabled", "hover", "active", "focus"];
        var grid = new Grid { Margin = new Thickness(12), ColumnSpacing = 12, RowSpacing = 12 };
        foreach (var _ in states) grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        foreach (var _ in kinds) grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        var interactive = new List<(Button button, string state)>();
        for (var r = 0; r < kinds.Length; r++)
        {
            for (var c = 0; c < states.Length; c++)
            {
                var b = new Button
                {
                    Content = states[c],
                    Kind = kinds[r],
                    IsSelected = states[c] == "selected",
                    IsLoading = states[c] == "loading",
                    IsEnabled = states[c] != "disabled",
                };
                Grid.SetRow(b, r);
                Grid.SetColumn(b, c);
                grid.Children.Add(b);
                if (states[c] is "hover" or "active" or "focus") interactive.Add((b, states[c]));
            }
        }

        var sizes = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, Margin = new Thickness(12) };
        foreach (var size in new[] { ButtonSize.XSmall, ButtonSize.Small, ButtonSize.Medium, ButtonSize.Large })
        {
            sizes.Children.Add(new Button { Content = size.ToString(), Size = size });
            sizes.Children.Add(new Button { Content = "+", Size = size, Shape = ButtonShape.Circle });
            sizes.Children.Add(new Button { Content = "Pill", Size = size, Shape = ButtonShape.Pill, Kind = ButtonKind.Secondary });
        }

        var group = new ButtonGroup { Mode = ButtonGroupMode.Radio, Selected = [1], Margin = new Thickness(12) };
        foreach (var label in new[] { "Day", "Week", "Month" }) group.Items.Add(new Button { Content = label });
        var outline = new ButtonGroup { Mode = ButtonGroupMode.Checkbox, Kind = ButtonKind.Outline, Shape = ButtonShape.Pill, Selected = [0, 2], Margin = new Thickness(12) };
        foreach (var label in new[] { "Wi-Fi", "Pool", "Parking" }) outline.Items.Add(new Button { Content = label });

        var page = new StackPanel { Children = { grid, sizes, group, outline } };
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        using var scene = new Scene(new Border { Background = Brushes.Transparent, Child = page }, variant, width: 900, height: 760);
        scene.Window.Background = variant == ThemeVariant.Dark ? Brushes.Black : Brushes.White;
        // One pointer and one focus: hover one cell, press another, then focus a third by keyboard.
        scene.Hover(interactive.First(i => i.state == "hover").button);
        var hoverFrame = scene.Capture();
        scene.Press(interactive.First(i => i.state == "active").button);
        interactive.First(i => i.state == "focus").button.Focus(NavigationMethod.Tab);
        var frame = scene.Capture();

        Directory.CreateDirectory(folder);
        hoverFrame.Save(Path.Combine(folder, $"button-gallery-{theme.ToLowerInvariant()}-hover.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        frame.Save(Path.Combine(folder, $"button-gallery-{theme.ToLowerInvariant()}.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }
}
