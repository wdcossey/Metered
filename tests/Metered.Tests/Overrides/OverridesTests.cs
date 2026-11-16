using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Metered.Controls;
using Button = Metered.Controls.Button;

namespace Metered.Tests.Overrides;

/// <summary>
/// Base Web's overrides API (guides/understanding-overrides): style, props and component per part,
/// state styles, the Root alias, merging, removal, reuse and nesting.
/// </summary>
public class OverridesTests
{
    private static readonly IBrush Red = new ImmutableSolidColorBrush(Colors.Red);
    private static readonly IBrush Blue = new ImmutableSolidColorBrush(Colors.Blue);

    private static ControlTheme Theme<T>(params (AvaloniaProperty property, object? value)[] setters)
    {
        var theme = new ControlTheme(typeof(T));
        foreach (var (property, value) in setters) theme.Setters.Add(new Setter(property, value));
        return theme;
    }

    private static Border BaseButton(Button button) => (Border)button.Part("BaseButton")!;

    /// <summary>The part's background without its transition (Base Web's background animates too).</summary>
    private static IBrush? Background(Button button) => BaseButton(button).GetBaseValue(Border.BackgroundProperty).GetValueOrDefault();

    [AvaloniaFact]
    public void Style_is_merged_over_the_part_theme()
    {
        var button = new Button
        {
            Content = "Button",
            Overrides = [new Override { Part = "BaseButton", Style = Theme<Border>((Border.BackgroundProperty, Red)) }],
        };
        using var scene = new Scene(button);
        var part = BaseButton(button);
        Assert.Same(Red, part.Background);
        // The rest of Base Web's style is still there.
        Assert.Equal(new Thickness(16, 14), part.Padding);
        Assert.Equal(new CornerRadius(8), part.CornerRadius);
    }

    [AvaloniaFact]
    public void Root_reaches_BaseButton_and_BaseButton_wins_over_it()
    {
        var button = new Button
        {
            Content = "Button",
            Overrides = [new Override { Part = "Root", Style = Theme<Border>((Border.BackgroundProperty, Red)) }],
        };
        using var scene = new Scene(button);
        Assert.Same(Red, Background(button));

        button.Overrides = [
            new Override { Part = "Root", Style = Theme<Border>((Border.BackgroundProperty, Red)) },
            new Override { Part = "BaseButton", Style = Theme<Border>((Border.BackgroundProperty, Blue)) },
        ];
        scene.Pump();
        Assert.Same(Blue, Background(button));
    }

    [AvaloniaFact]
    public void Props_are_merged_over_style()
    {
        var button = new Button
        {
            Content = "Button",
            Overrides =
            [
                new Override
                {
                    Part = "BaseButton",
                    Style = Theme<Border>((Border.BackgroundProperty, Red), (Border.PaddingProperty, new Thickness(2))),
                    Props = Theme<Border>((Border.BackgroundProperty, Blue)),
                },
            ],
        };
        using var scene = new Scene(button);
        Assert.Same(Blue, Background(button));
        Assert.Equal(new Thickness(2), BaseButton(button).Padding);
    }

    [AvaloniaFact]
    public void Component_replaces_the_part_theme()
    {
        var button = new Button
        {
            Content = "Button",
            Overrides = [new Override { Part = "BaseButton", Component = Theme<Border>((Border.BackgroundProperty, Red)) }],
        };
        using var scene = new Scene(button);
        Assert.Same(Red, Background(button));
        Assert.Equal(default, BaseButton(button).Padding);
    }

    [AvaloniaFact]
    public void A_state_style_follows_the_buttons_state()
    {
        // Base Web's style function of $isSelected, as a ^:is-selected style.
        var style = new ControlTheme(typeof(Border));
        var selected = new Style(x => x.Nesting().Class(":is-selected"));
        selected.Setters.Add(new Setter(Border.BackgroundProperty, Red));
        style.Children.Add(selected);
        var button = new Button { Content = "Button", Overrides = [new Override { Part = "BaseButton", Style = style }] };
        using var scene = new Scene(button);
        Assert.NotSame(Red, Background(button));
        button.IsSelected = true;
        scene.Pump();
        Assert.Same(Red, Background(button));
    }

    [AvaloniaFact]
    public void A_style_reaches_a_part_inside_another_part()
    {
        var button = new Button
        {
            Content = "Go",
            IsLoading = true,
            Overrides = [new Override { Part = "LoadingSpinner", Style = Theme<ButtonLoadingSpinner>((ButtonLoadingSpinner.ForegroundProperty, Red)) }],
        };
        using var scene = new Scene(button);
        Assert.Same(Red, ((ButtonLoadingSpinner)button.Part("LoadingSpinner")!).Foreground);
    }

    [AvaloniaFact]
    public void Removing_an_override_restores_the_part()
    {
        var button = new Button
        {
            Content = "Button",
            Overrides = [new Override { Part = "BaseButton", Style = Theme<Border>((Border.BackgroundProperty, Red)) }],
        };
        using var scene = new Scene(button);
        button.Overrides!.Clear();
        scene.Pump();
        Assert.Equal("#FF000000", Controls.ButtonColourTests.Colour(Background(button)));
    }

    [AvaloniaFact]
    public void One_style_serves_many_buttons()
    {
        var style = Theme<Border>((Border.BackgroundProperty, Red));
        var a = new Button { Content = "A", Overrides = [new Override { Part = "BaseButton", Style = style }] };
        var b = new Button { Content = "B", Overrides = [new Override { Part = "BaseButton", Style = style }] };
        using var scene = new Scene(new StackPanel { Children = { a, b } });
        Assert.Same(Red, Background(a));
        Assert.Same(Red, Background(b));
    }

    [AvaloniaFact]
    public void One_style_serves_different_parts_and_is_left_as_it_is()
    {
        var style = new ControlTheme(typeof(Control));
        style.Setters.Add(new Setter(Visual.OpacityProperty, 0.5));
        var button = new Button
        {
            Content = "A",
            StartEnhancer = "x",
            Overrides =
            [
                new Override { Part = "BaseButton", Style = style },
                new Override { Part = "StartEnhancer", Style = style },
            ],
        };
        using var scene = new Scene(button);
        Assert.Equal(0.5, BaseButton(button).Opacity);
        Assert.Equal(0.5, button.Part("StartEnhancer")!.GetValue(Visual.OpacityProperty));
        // Base Web's padding is still under the BaseButton, and the app's theme is untouched.
        Assert.Equal(new Thickness(16, 14), BaseButton(button).Padding);
        Assert.Null(style.BasedOn);
        Assert.Single(style.Setters);
    }

    [AvaloniaFact]
    public void One_style_serves_two_theme_instances()
    {
        var style = Theme<Border>((Border.BackgroundProperty, Red));
        foreach (var _ in Enumerable.Range(0, 2))
        {
            var button = new Button { Content = "A", Overrides = [new Override { Part = "BaseButton", Style = style }] };
            using var scene = new Scene(button); // a new BaseTheme each time
            Assert.Same(Red, Background(button));
            Assert.Equal(new Thickness(16, 14), BaseButton(button).Padding);
        }
    }

    [AvaloniaFact]
    public void Nested_overrides_reach_a_part_that_is_a_Metered_control()
    {
        // Base Web's ComponentA > overrides > ComponentB > props > overrides.
        var inner = new OverrideCollection { new Override { Part = "BaseButton", Style = Theme<Border>((Border.BackgroundProperty, Red)) } };
        var card = new TestCard
        {
            Overrides = [new Override { Part = "Action", Props = Theme<Button>((Button.OverridesProperty, inner)) }],
        };
        using var scene = new Scene(card);
        var action = (Button)card.PartSet["Action"]!;
        Assert.Same(Red, Background(action));
    }

    /// <summary>A control whose part is a Button, as Base Web's Select's Tag is a Tag.</summary>
    private sealed class TestCard : TemplatedControl
    {
        public static readonly StyledProperty<OverrideCollection?> OverridesProperty =
            AvaloniaProperty.Register<TestCard, OverrideCollection?>(nameof(Overrides));

        public TestCard()
        {
            PartSet = new Parts(this, "TestCard", ["Action"]);
            Template = new FuncControlTemplate<TestCard>((_, scope) =>
                new Button { Name = "Action", Content = "Act" }.RegisterInNameScope(scope));
        }

        public Parts PartSet { get; }

        public OverrideCollection? Overrides { get => GetValue(OverridesProperty); set => SetValue(OverridesProperty, value); }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            PartSet.Attach(e.NameScope);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == OverridesProperty) PartSet.SetOverrides(Overrides);
        }
    }
}
