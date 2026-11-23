using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Metered.Controls;

/// <summary>
/// Base Web's <c>OptionProfile</c> (<c>menu/option-profile.js</c>): an option of a person's picture, title, subtitle and
/// body, chosen for a menu by its <c>Option</c> override, whose theme targets this type, as Base Web's
/// <c>overrides.Option.component: OptionProfile</c>. Its parts: <c>ListItemProfile</c> (the option's box, a row),
/// <c>ProfileImgContainer</c>, <c>ProfileImg</c>, <c>ProfileLabelsContainer</c>, <c>ProfileTitle</c>,
/// <c>ProfileSubtitle</c>, <c>ProfileBody</c>.
/// </summary>
/// <remarks>
/// <para>Its <see cref="GetProfileItemLabels"/>, <see cref="GetProfileItemImg"/> and <see cref="GetProfileItemImgText"/> are
/// Base Web's, given by the override's props; with none it shows no labels and no picture. As Base Web's, it shows neither
/// the highlight nor the menu's focus: its box is tinted only under a hovering mouse (its own <c>:hover</c>, a mouse's
/// only, as Metered's hover is).</para>
/// <para>A picture given as a string is loaded from an <c>avares://</c> resource or a file. A web address isn't fetched: a
/// control library doesn't reach the network (docs/controls/Menu). Any other <see cref="IImage"/> is drawn as it is, and a
/// control in place of the picture, as Base Web renders a component there.</para>
/// </remarks>
[PseudoClasses(":hover")]
public class MenuOptionProfile : MenuOptionBase
{
    public const string ListItemProfilePart = "ListItemProfile";
    public const string ProfileImgContainerPart = "ProfileImgContainer";
    public const string ProfileImgPart = "ProfileImg";
    public const string ProfileLabelsContainerPart = "ProfileLabelsContainer";
    public const string ProfileTitlePart = "ProfileTitle";
    public const string ProfileSubtitlePart = "ProfileSubtitle";
    public const string ProfileBodyPart = "ProfileBody";

    public static readonly StyledProperty<Func<object?, MenuProfileLabels?>?> GetProfileItemLabelsProperty =
        AvaloniaProperty.Register<MenuOptionProfile, Func<object?, MenuProfileLabels?>?>(nameof(GetProfileItemLabels));

    public static readonly StyledProperty<Func<object?, object?>?> GetProfileItemImgProperty =
        AvaloniaProperty.Register<MenuOptionProfile, Func<object?, object?>?>(nameof(GetProfileItemImg));

    public static readonly StyledProperty<Func<object?, string?>?> GetProfileItemImgTextProperty =
        AvaloniaProperty.Register<MenuOptionProfile, Func<object?, string?>?>(nameof(GetProfileItemImgText));

    private Border? _box;
    private Border? _picture;
    private Bitmap? _bitmap;

    /// <summary>Base Web's <c>getProfileItemLabels</c>: the item's title, subtitle and body.</summary>
    public Func<object?, MenuProfileLabels?>? GetProfileItemLabels { get => GetValue(GetProfileItemLabelsProperty); set => SetValue(GetProfileItemLabelsProperty, value); }

    /// <summary>Base Web's <c>getProfileItemImg</c>: the item's picture: a source (a string), an image, or a control in its place.</summary>
    public Func<object?, object?>? GetProfileItemImg { get => GetValue(GetProfileItemImgProperty); set => SetValue(GetProfileItemImgProperty, value); }

    /// <summary>Base Web's <c>getProfileItemImgText</c>: the picture's alternative text, its name to UI Automation.</summary>
    public Func<object?, string?>? GetProfileItemImgText { get => GetValue(GetProfileItemImgTextProperty); set => SetValue(GetProfileItemImgTextProperty, value); }

    protected override Type StyleKeyOverride => typeof(MenuOptionProfile);

    protected override string[] PartNames =>
        [ListItemProfilePart, ProfileImgContainerPart, ProfileImgPart, ProfileLabelsContainerPart, ProfileTitlePart, ProfileSubtitlePart, ProfileBodyPart];

    internal MenuProfileLabels Labels => GetProfileItemLabels?.Invoke(Item) ?? new MenuProfileLabels();

    protected override Control Build(INameScope scope)
    {
        var container = _picture = Themed(new Border(), ProfileImgContainerPart, scope);
        var picture = GetProfileItemImg?.Invoke(Item);
        if (picture is Control control) container.Child = control;
        else if (Source(picture, out _bitmap) is { } image)
        {
            var view = new Image { Source = image, Stretch = Stretch.Fill };
            if (GetProfileItemImgText?.Invoke(Item) is { } alt) AutomationProperties.SetName(view, alt);
            container.Child = Themed(new Border { Child = view }, ProfileImgPart, scope);
        }

        var labels = Themed(new StackPanel(), ProfileLabelsContainerPart, scope);
        var (title, subtitle, body) = Labels;
        // option-profile.js: each only when it has text.
        if (!string.IsNullOrEmpty(title)) labels.Children.Add(Themed(new TextBlock { Text = title }, ProfileTitlePart, scope));
        if (!string.IsNullOrEmpty(subtitle)) labels.Children.Add(Themed(new TextBlock { Text = subtitle }, ProfileSubtitlePart, scope));
        if (!string.IsNullOrEmpty(body)) labels.Children.Add(Themed(new TextBlock { Text = body }, ProfileBodyPart, scope));

        // display: flex; align-items: center: the picture and the labels in a row (the labels at the top: alignSelf).
        _box = Themed(new Border { Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { container, labels } } }, ListItemProfilePart, scope);
        return _box;
    }

    /// <summary>
    /// A picture given as a source: an image as it is, a string from a resource or a file (the bitmap it made, which the
    /// option disposes when its parts are made again); anything else none.
    /// </summary>
    private static IImage? Source(object? picture, out Bitmap? made)
    {
        made = null;
        switch (picture)
        {
            case IImage image:
                return image;
            case string { Length: > 0 } text when Uri.TryCreate(text, UriKind.Absolute, out var uri) && uri.Scheme == "avares" && AssetLoader.Exists(uri):
                using (var stream = AssetLoader.Open(uri)) return made = new Bitmap(stream);
            case string { Length: > 0 } text when File.Exists(text):
                return made = new Bitmap(text);
            default:
                return null;
        }
    }

    protected override void ReleaseParts()
    {
        if (_picture is not null) _picture.Child = null;
        _picture = null;
        _bitmap?.Dispose();
        _bitmap = null;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == GetProfileItemLabelsProperty || p == GetProfileItemImgProperty || p == GetProfileItemImgTextProperty) Rebuild();
    }

    /// <summary>option-profile.js keeps <c>$isHighlighted</c> from its parts: it isn't mirrored onto them.</summary>
    protected override bool Mirrors(string pseudo) => pseudo != ":is-highlighted";

    /// <summary>StyledListItemProfile's <c>:hover</c>, on its box.</summary>
    protected override void MouseOver(bool over)
    {
        PseudoClasses.Set(":hover", over);
        if (_box is not null) ((IPseudoClasses)_box.Classes).Set(":hover", over);
    }

    internal override string? AccessibleText
    {
        get
        {
            if (ChildMenu is not null) return Menu.ParentMenuItemAriaLabel;
            var (title, subtitle, body) = Labels;
            var text = string.Join(" ", new[] { title, subtitle, body }.Where(s => !string.IsNullOrEmpty(s)));
            return text.Length == 0 ? null : text;
        }
    }
}
