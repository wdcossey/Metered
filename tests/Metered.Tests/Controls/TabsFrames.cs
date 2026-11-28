using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// Drawn pixels of tabs: the active tab's highlight over the border, the border beside it, a tab's fill, in Base Web's
/// colours where layout puts them. And, when METERED_FRAMES names a folder, a gallery of Base Web's tabs-motion examples,
/// light and dark, saved there for docs/controls/Tabs.
/// </summary>
public class TabsFrames
{
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void The_highlight_is_drawn_over_the_border(string theme)
    {
        var c = TabsOracle.Data.Colours.Single(x => x.Theme == theme && x.State == "rest");
        var tabs = new Tabs { ActiveKey = "1", Items = { new Tab { Title = "Robot" }, new Tab { Title = "Monster" }, new Tab { Title = "Watermelon" } } };
        using var scene = new Scene(new StackPanel { Children = { tabs } }, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        var frame = scene.Capture();
        var buttons = tabs.Elements(Tabs.TabPart).Cast<TabButton>().ToList();
        var border = (Border)tabs.Part(Tabs.TabBorderPart)!;
        Color At(Visual v, double x, double y)
        {
            var p = v.TranslatePoint(new Point(x, y), scene.Window)!.Value;
            return Frames.PixelAt(frame, (int)p.X, (int)p.Y);
        }

        // The border's middle row: the highlight under the active tab, the border under the others.
        Assert.Equal(Color.Parse(c.Highlight), At(border, buttons[1].Bounds.X + buttons[1].Bounds.Width / 2, 2));
        Assert.Equal(Color.Parse(c.Border), At(border, buttons[0].Bounds.Width / 2, 2));
        Assert.Equal(Color.Parse(c.Border), At(border, Scene.Width - 5, 2));
        Assert.Equal(Color.Parse(c.Background), At(buttons[0], 3, 3));
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Gallery(string theme)
    {
        var folder = Environment.GetEnvironmentVariable(ButtonFrames.Variable);
        if (string.IsNullOrEmpty(folder)) return;
        var dark = theme == "Dark";
        Tabs T(Action<Tabs>? set = null, string active = "0")
        {
            var t = new Tabs { ActiveKey = active };
            t.Items.Add(new Tab { Title = "First", Content = "I must not fear." });
            t.Items.Add(new Tab { Title = "Second", Content = "Fear is the mind-killer." });
            t.Items.Add(new Tab { Title = "Third", Content = "Fear is the little-death that brings total obliteration." });
            set?.Invoke(t);
            return t;
        }

        var scroll = new Tabs { ActiveKey = "8" };
        foreach (var w in new[] { "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve" }) scroll.Items.Add(new Tab { Title = w, Content = w });
        var left = new StackPanel { Width = 370, Spacing = 24, Margin = new Thickness(12) };
        var right = new StackPanel { Width = 370, Spacing = 24, Margin = new Thickness(12) };
        left.Children.Add(T());
        left.Children.Add(T(t => t.Fill = TabsFill.Fixed, "1"));
        left.Children.Add(T(t => t.Disabled = true));
        left.Children.Add(T(t => t.EndEnhancer = new Metered.Controls.Button { Size = ButtonSize.Small, Shape = ButtonShape.Pill, Content = "Button" }));
        right.Children.Add(T(t => t.Orientation = Orientation.Vertical, "2"));
        right.Children.Add(scroll);
        var focused = T(active: "1");
        right.Children.Add(focused);
        using var scene = new Scene(new StackPanel { Orientation = Orientation.Horizontal, Children = { left, right } },
            dark ? ThemeVariant.Dark : ThemeVariant.Light, width: 780, height: 700);
        scene.Window.Background = dark ? Brushes.Black : Brushes.White;
        // The panels' text is the page's colour, as Base Web's: the window's, here.
        scene.Window.Foreground = dark ? Brushes.White : Brushes.Black;
        ((TabButton)focused.Elements(Tabs.TabPart)[2]).Focus(Avalonia.Input.NavigationMethod.Directional);
        scene.Hover((Visual)focused.Elements(Tabs.TabPart)[0]);
        scene.Advance(300);
        var frame = scene.Capture();
        Directory.CreateDirectory(folder);
        frame.Save(Path.Combine(folder, $"tabs-gallery-{theme.ToLowerInvariant()}.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }
}
