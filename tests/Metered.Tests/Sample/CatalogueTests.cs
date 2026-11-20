using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Metered.Sample;
using Metered.Sample.Pages;
using Metered.Themes;

using static Metered.Tests.Sample.ShellFrames;

namespace Metered.Tests.Sample;

/// <summary>
/// The sample's catalogue (#7, from Cornflower's): one page per <see cref="SamplePageAttribute"/>, found with no
/// registration; the shell that lists them and shows one, with Back, the platform's back request and the Light/Dark
/// switch; the desktop window; and a phone's view, opened at its start page.
/// </summary>
public class CatalogueTests
{
    [SamplePage("second b", Order = 2)]
    private sealed class SecondB : Border;

    [SamplePage("second a", Order = 2)]
    private sealed class SecondA : Border;

    [SamplePage("first", Order = 1)]
    private sealed class First : Border;

    [SamplePage("no order")]
    private sealed class NoOrder : Border;

    private sealed class NotAPage : Border;

    [SamplePage("not a control")]
    private sealed class NotAControl;

    [SamplePage("needs an argument")]
    private sealed class NeedsAnArgument(int value) : Border
    {
        public int Value { get; } = value;
    }

    [SamplePage("generic")]
    private sealed class Generic<T> : Border;

    [SamplePage("first")]
    private sealed class AlsoFirst : Border;

    [SamplePage("First")]
    private sealed class FirstInCapitals : Border;

    [SamplePage("a window")]
    private sealed class AWindow : Window;

    [AvaloniaFact]
    public void Pages_Are_Found_By_Their_Attribute_By_Order_Then_Title()
    {
        var pages = SamplePageCatalog.Discover([typeof(NoOrder), typeof(SecondB), typeof(NotAPage), typeof(First), typeof(SecondA)]);

        Assert.Equal(["first", "second a", "second b", "no order"], pages.Select(page => page.Title));
        Assert.IsType<First>(pages[0].Create());
        Assert.IsType<NoOrder>(pages[3].Create());
        Assert.NotSame(pages[0].Create(), pages[0].Create());
    }

    [AvaloniaTheory]
    [InlineData(typeof(NotAControl))]
    [InlineData(typeof(NeedsAnArgument))]
    [InlineData(typeof(Generic<>))]
    [InlineData(typeof(AWindow))]
    public void A_Page_That_Cant_Be_Made_Fails_When_The_Catalogue_Is_Built(Type type)
    {
        var error = Assert.Throws<InvalidOperationException>(() => SamplePageCatalog.Discover([type]));
        Assert.Contains(type.FullName!, error.Message);
    }

    [AvaloniaFact]
    public void Two_Pages_With_One_Title_Fail_When_The_Catalogue_Is_Built()
    {
        var error = Assert.Throws<InvalidOperationException>(() => SamplePageCatalog.Discover([typeof(First), typeof(AlsoFirst)]));
        Assert.Contains("'first'", error.Message);
    }

    // A page is opened by its title in any case (App.StartPage), so two that differ only in case are one.
    [AvaloniaFact]
    public void Two_Titles_That_Differ_Only_In_Case_Fail_When_The_Catalogue_Is_Built()
    {
        Assert.Throws<InvalidOperationException>(() => SamplePageCatalog.Discover([typeof(First), typeof(FirstInCapitals)]));
    }

    // Base Web's names for its components, each control's page by its order, the theme last.
    [AvaloniaFact]
    public void The_Samples_Pages_Are_Each_Control_Then_The_Theme()
    {
        Assert.Equal(
            [
                ("Button", typeof(ButtonPage)),
                ("ButtonGroup", typeof(ButtonGroupPage)),
                ("Checkbox", typeof(CheckboxPage)),
                ("Radio", typeof(RadioPage)),
                ("Switch", typeof(SwitchPage)),
                ("ProgressBar", typeof(ProgressBarPage)),
                ("Spinner", typeof(SpinnerPage)),
                ("Theme", typeof(ThemePage)),
            ],
            SampleShell.Pages.Select(page => (page.Title, page.Type)));
    }

    // A control's page takes its issue number, which is above the pilot's 1 and 2: the theme's page still closes the list.
    [SamplePage("Slider", Order = 31)]
    private sealed class ALaterControl : Border;

    [AvaloniaFact]
    public void A_Later_Controls_Page_Sorts_Before_The_Theme_And_After_The_Pilots()
    {
        var pages = SamplePageCatalog.Discover([typeof(ThemePage), typeof(ALaterControl), typeof(ButtonPage), typeof(NoOrder)]);

        Assert.Equal(["Button", "Slider", "Theme", "no order"], pages.Select(page => page.Title));
    }

    // The preview names its pictures by the page's title, and the list's "list".
    [AvaloniaFact]
    public void Every_Page_Can_Name_Its_Pictures_And_None_Takes_The_Lists()
    {
        Assert.Equal(["button", "buttongroup", "checkbox", "radio", "switch", "progressbar", "spinner", "theme"], SampleShell.Pages.Select(page => PreviewGallery.PictureName(page.Title)));
        Assert.Throws<InvalidOperationException>(() => PreviewGallery.PictureName("List"));
        Assert.Throws<InvalidOperationException>(() => PreviewGallery.PictureName("Date/Time"));
    }

    // The preview gives the sample's window the sample's default family, by name: in the tests, which have the same
    // package's fonts, the name must find Instrument Sans itself (here at 400 and 700, whose faces report the family
    // alone), not the platform's default that a name Avalonia can't find falls back to.
    [AvaloniaFact]
    public void The_Samples_Default_Family_Finds_Instrument_Sans_In_The_Tests()
    {
        foreach (var weight in new[] { FontWeight.Normal, FontWeight.Bold })
        {
            Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface(SampleFonts.DefaultFamilyName, FontStyle.Normal, weight), out var face));
            Assert.Equal(TestApp.TextFamily, face.FamilyName);
            Assert.Equal(weight, face.Weight);
        }
    }

    [AvaloniaFact]
    public void The_Window_Opens_At_The_Phone_View_Holding_The_List_And_Can_Be_Resized()
    {
        var window = new MainWindow();
        window.Styles.Add(new BaseTheme());
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(new Size(Scene.Width, Scene.Height), window.ClientSize);
            Assert.Equal(WindowStartupLocation.CenterScreen, window.WindowStartupLocation);
            Assert.True(window.CanResize);
            var shell = Assert.IsType<SampleShell>(window.Content);
            Assert.False(shell.IsShowingPage);

            window.Width = 1024;
            window.Height = 768;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(new Size(1024, 768), shell.Bounds.Size);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void The_List_Has_One_Of_Metereds_Buttons_Per_Page_In_The_Catalogues_Order()
    {
        var shell = new SampleShell();
        using var scene = new Scene(shell);

        var buttons = ListButtons(shell);
        Assert.Equal(SampleShell.Pages.Select(page => page.Title), buttons.Select(button => button.Content));
        // Drawn by Metered's theme, not only of its type.
        Assert.All(buttons, button => Assert.NotNull(button.Part(Metered.Controls.Button.BaseButtonPart)));
    }

    [AvaloniaFact]
    public void Every_Page_Of_The_Sample_Opens_In_The_Shell_Under_Its_Title()
    {
        var shell = new SampleShell { Report = _ => { } };
        using var scene = new Scene(shell);
        var title = InBar<TextBlock>(shell, "PageTitle");

        foreach (var page in SampleShell.Pages)
        {
            shell.Open(page.Title.ToUpperInvariant());
            scene.Pump();

            Assert.IsType(page.Type, shell.Shown);
            Assert.Equal(page.Title, title.Text);
            Assert.True(shell.Shown!.IsEffectivelyVisible);
        }
    }

    [AvaloniaFact]
    public void Opening_A_Page_No_One_Has_Names_The_Pages_There_Are()
    {
        var shell = new SampleShell();

        var error = Assert.Throws<ArgumentException>(() => shell.Open("Slider"));

        Assert.Contains("'Slider'", error.Message);
        Assert.Contains("'Button', 'ButtonGroup', 'Checkbox', 'Radio', 'Switch', 'ProgressBar', 'Spinner', 'Theme'", error.Message);
        Assert.False(shell.IsShowingPage);
    }

    [AvaloniaFact]
    public void A_Click_On_The_List_Opens_Its_Page_And_Back_Shows_The_List()
    {
        var shell = new SampleShell { Report = _ => { } };
        using var scene = new Scene(shell);
        var index = shell.Shown;
        var back = InBar<Metered.Controls.Button>(shell, "BackButton");
        var title = InBar<TextBlock>(shell, "PageTitle");
        Assert.False(back.IsVisible);
        Assert.Equal(SampleShell.IndexTitle, title.Text);

        scene.Click(ListButtons(shell)[1]);
        Assert.True(shell.IsShowingPage);
        Assert.IsType<ButtonGroupPage>(shell.Shown);
        Assert.True(back.IsVisible);
        Assert.Equal("ButtonGroup", title.Text);

        scene.Click(back);
        Assert.False(shell.IsShowingPage);
        Assert.Same(index, shell.Shown);
        Assert.False(back.IsVisible);
        Assert.Equal(SampleShell.IndexTitle, title.Text);
    }

    // The bar is the shell's; the page has the rest of the view.
    [AvaloniaFact]
    public void A_Page_Has_The_View_Under_The_Bar()
    {
        var shell = new SampleShell { Report = _ => { } };
        using var scene = new Scene(shell);
        var page = new Border();
        shell.ShowPage(page, "a page");
        scene.Pump();

        var bar = InBar<Border>(shell, "Bar");
        Assert.True(bar.Bounds.Height > 0);
        Assert.Equal(new Rect(0, bar.Bounds.Height, Scene.Width, Scene.Height - bar.Bounds.Height),
            new Rect(page.TranslatePoint(default, scene.Window)!.Value, page.Bounds.Size));
    }

    [AvaloniaFact]
    public void A_Back_Request_On_A_Page_Shows_The_List_And_Is_Handled()
    {
        var shell = new SampleShell { Report = _ => { } };
        using var scene = new Scene(shell);
        shell.ShowPage(new Border(), "a page");

        Assert.True(Back(scene.Window));
        Assert.False(shell.IsShowingPage);
    }

    // Unhandled, a phone goes back from the app, and leaves it.
    [AvaloniaFact]
    public void A_Back_Request_On_The_List_Is_Left_Unhandled()
    {
        var shell = new SampleShell();
        using var scene = new Scene(shell);

        Assert.False(Back(scene.Window));
        Assert.False(shell.IsShowingPage);
    }

    // Whether a page is shown is the shell's own state, not how Back looks.
    [AvaloniaFact]
    public void A_Page_Is_Shown_Whatever_Backs_Visibility()
    {
        var shell = new SampleShell { Report = _ => { } };
        using var scene = new Scene(shell);
        shell.ShowPage(new Border(), "a page");
        InBar<Metered.Controls.Button>(shell, "BackButton").IsVisible = false;

        Assert.True(shell.IsShowingPage);
        Assert.True(Back(scene.Window));
        Assert.False(shell.IsShowingPage);
    }

    // A popup's back request is still this window's. Headless, a popup is drawn in the window's overlay layer, which
    // the Simple theme's window template has, as the sample's App does.
    [AvaloniaFact]
    public void A_Back_Request_From_A_Popup_On_The_Page_Shows_The_List()
    {
        var shell = new SampleShell { Report = _ => { } };
        using var scene = new Scene(shell, themes: [new SimpleTheme(), new BaseTheme()]);
        var inPopup = new Border { Width = 50, Height = 50 };
        var page = new Panel();
        var popup = new Popup { Child = inPopup, PlacementTarget = page };
        page.Children.Add(popup);
        shell.ShowPage(page, "a page");
        Dispatcher.UIThread.RunJobs();
        popup.IsOpen = true;
        Dispatcher.UIThread.RunJobs();

        Assert.True(Back(inPopup));
        Assert.False(shell.IsShowingPage);
    }

    // Alt+Left is Avalonia's back on desktop, through the same request as a phone's back button.
    [AvaloniaFact]
    public void Alt_Left_Goes_Back()
    {
        var shell = new SampleShell { Report = _ => { } };
        using var scene = new Scene(shell);
        shell.ShowPage(new Border(), "a page");

        scene.Window.KeyPressQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.Alt);
        scene.Window.KeyReleaseQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.Alt);
        Dispatcher.UIThread.RunJobs();

        Assert.False(shell.IsShowingPage);
    }

    // What a page shows over itself goes back first: the shell takes only what's left.
    [AvaloniaFact]
    public void A_Back_Request_A_Page_Took_Keeps_The_Page()
    {
        var shell = new SampleShell { Report = _ => { } };
        using var scene = new Scene(shell);
        var page = new Border();
        shell.ShowPage(page, "a page");
        scene.Window.AddHandler(TopLevel.BackRequestedEvent, (_, e) => e.Handled = true);

        Assert.True(Back(page));
        Assert.True(shell.IsShowingPage);
    }

    [AvaloniaFact]
    public void A_Back_Request_In_Another_Window_Is_Left_Alone()
    {
        var shell = new SampleShell { Report = _ => { } };
        using var scene = new Scene(shell);
        shell.ShowPage(new Border(), "a page");
        using var other = new Scene(new Border());

        Assert.False(Back(other.Window));
        Assert.True(shell.IsShowingPage);
    }

    [AvaloniaFact]
    public void A_Shell_That_Has_Left_Its_Window_Stops_Listening()
    {
        var shell = new SampleShell { Report = _ => { } };
        using var scene = new Scene(shell);
        shell.ShowPage(new Border(), "a page");
        scene.Window.Content = null;
        Dispatcher.UIThread.RunJobs();

        Assert.False(Back(scene.Window));
        Assert.True(shell.IsShowingPage);
    }

    // The switch is the app's own: it sets the whole window's variant, and says what it turns to.
    [AvaloniaFact]
    public void The_Switch_Turns_The_Window_Dark_And_Back()
    {
        var shell = new SampleShell { Report = _ => { } };
        using var scene = new Scene(shell);
        shell.Open("Button");
        scene.Pump();
        var variant = InBar<Metered.Controls.Button>(shell, "Variant");
        Assert.Equal("Dark", variant.Content);

        scene.Click(variant);
        Assert.Equal(ThemeVariant.Dark, scene.Window.ActualThemeVariant);
        Assert.Equal(ThemeVariant.Dark, shell.Shown!.ActualThemeVariant);
        Assert.Equal("Light", variant.Content);

        scene.Click(variant);
        Assert.Equal(ThemeVariant.Light, scene.Window.ActualThemeVariant);
        Assert.Equal("Dark", variant.Content);
    }

    // A window dark from the start, or turned by something else, is labelled for what the switch would do.
    [AvaloniaFact]
    public void The_Switch_Says_What_It_Turns_To_Whoever_Set_The_Variant()
    {
        var shell = new SampleShell();
        using var scene = new Scene(shell, ThemeVariant.Dark);
        var variant = InBar<Metered.Controls.Button>(shell, "Variant");
        Assert.Equal("Light", variant.Content);

        scene.Window.RequestedThemeVariant = ThemeVariant.Light;
        scene.Pump();
        Assert.Equal("Dark", variant.Content);
    }

    // A phone's view is the same shell, with no window of its own: on the list, or at the page its head asks for.
    [AvaloniaFact]
    public void A_Phone_Opens_On_The_List_Or_At_Its_Start_Page_In_Any_Case()
    {
        Assert.False(App.PhoneView(SampleShell.Pages, null).IsShowingPage);

        var shell = App.PhoneView(SampleShell.Pages, "buttongroup");
        shell.Report = _ => { };
        Assert.IsType<ButtonGroupPage>(shell.Shown);
        using var scene = new Scene(shell);
        Assert.Equal("ButtonGroup", InBar<TextBlock>(shell, "PageTitle").Text);
    }

    [AvaloniaFact]
    public void A_Start_Page_No_One_Has_Fails_Naming_It()
    {
        var error = Assert.Throws<ArgumentException>(() => App.PhoneView(SampleShell.Pages, "nowhere"));
        Assert.Contains("'nowhere'", error.Message);
    }
}
