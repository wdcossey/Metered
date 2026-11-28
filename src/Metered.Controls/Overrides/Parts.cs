using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Styling;

namespace Metered.Controls;

/// <summary>
/// A control's hold on its template's named parts, Base Web's subcomponents: it mirrors the
/// control's state pseudoclasses onto every part (Base Web passes the same <c>$</c> props to each
/// subcomponent), and gives each part its theme, <c>{Component}.Styled{Part}</c>, with the
/// control's <see cref="Override"/> for that part merged over it.
/// </summary>
/// <remarks>
/// A part is an element of the template by its name, or the elements the control makes of a part that Base Web
/// renders some number of times (a progress bar's <c>Bar</c> for each step), given by <see cref="Place"/>: every one
/// takes the states, the theme and the override.
/// </remarks>
internal sealed class Parts
{
    private readonly TemplatedControl _owner;
    private readonly string _component;
    private readonly string[] _names;
    private readonly IReadOnlyDictionary<string, string> _aliases;
    private readonly Dictionary<string, List<StyledElement>> _found = new(StringComparer.Ordinal);
    private readonly Dictionary<StyledElement, IDisposable> _placed = [];
    private readonly Dictionary<string, bool> _states = new(StringComparer.Ordinal);
    private readonly HashSet<StyledElement> _themed = [];
    private OverrideCollection? _overrides;

    /// <param name="owner">The control.</param>
    /// <param name="component">Base Web's component name, the prefix of its parts' theme keys.</param>
    /// <param name="names">The parts, by Base Web's names.</param>
    /// <param name="aliases">Other override keys and the part each reaches (Button's <c>Root</c> is its <c>BaseButton</c>).
    /// The part's own name wins over an alias, as Base Web's <c>overrides.BaseButton || overrides.Root</c>.</param>
    public Parts(TemplatedControl owner, string component, string[] names, IReadOnlyDictionary<string, string>? aliases = null)
    {
        _owner = owner;
        _component = component;
        _names = names;
        _aliases = aliases ?? new Dictionary<string, string>();
    }

    public IReadOnlyList<string> Names => _names;

    /// <summary>The part of that name in the current template, if there is one.</summary>
    public StyledElement? this[string name] => _found.TryGetValue(name, out var list) && list.Count > 0 ? list[0] : null;

    /// <summary>Every element of the part of that name: the template's one, or the ones <see cref="Place"/> gave.</summary>
    public IReadOnlyList<StyledElement> All(string name) => _found.TryGetValue(name, out var list) ? list : [];


    /// <summary>Takes the parts of a newly applied template.</summary>
    public void Attach(INameScope scope)
    {
        foreach (var element in _themed) element.ClearValue(StyledElement.ThemeProperty);
        _themed.Clear();
        foreach (var binding in _placed.Values) binding.Dispose();
        _placed.Clear();
        _found.Clear();
        foreach (var name in _names)
        {
            if (scope.Find(name) is StyledElement element) _found[name] = [element];
        }

        foreach (var (pseudo, on) in _states)
        {
            foreach (var list in _found.Values)
            {
                foreach (var element in list) Set(element, pseudo, on);
            }
        }

        ApplyOverrides();
    }

    /// <summary>
    /// The elements the control makes of parts, each part's in place of the ones it gave before: each takes the
    /// control's states and its theme, <c>{Component}.Styled{Part}</c> (a dynamic resource, as the template's parts
    /// take theirs), with the part's override merged over it.
    /// </summary>
    public void Place(params (string Name, IReadOnlyList<StyledElement> Elements)[] parts)
    {
        foreach (var (name, elements) in parts)
        {
            foreach (var old in All(name))
            {
                if (_themed.Remove(old)) old.ClearValue(StyledElement.ThemeProperty);
                if (_placed.Remove(old, out var binding)) binding.Dispose();
            }

            _found[name] = [.. elements];
            foreach (var element in elements)
            {
                _placed[element] = element.Bind(StyledElement.ThemeProperty,
                    _owner.GetResourceObservable(ThemeKey(name), value => value as ControlTheme), BindingPriority.Template);
                foreach (var (pseudo, on) in _states) Set(element, pseudo, on);
            }
        }

        foreach (var (name, elements) in parts)
        {
            foreach (var element in elements) ApplyOverride(name, element);
        }
    }

    /// <summary>
    /// Lets every part go: the theme bindings &lt;see cref="Place"/&gt; made (to the owner's resources, which would keep the
    /// elements as long as the owner lives) and the override themes given, for elements the control no longer shows.
    /// </summary>
    public void Detach()
    {
        foreach (var element in _themed) element.ClearValue(StyledElement.ThemeProperty);
        _themed.Clear();
        foreach (var binding in _placed.Values) binding.Dispose();
        _placed.Clear();
        _found.Clear();
        SetOverrides(null);
    }

    /// <summary>Sets a state pseudoclass on every part (the control sets its own).</summary>
    public void SetState(string pseudoClass, bool on)
    {
        _states[pseudoClass] = on;
        foreach (var list in _found.Values)
        {
            foreach (var element in list) Set(element, pseudoClass, on);
        }
    }

    /// <summary>Uses a new overrides collection, following its changes.</summary>
    public void SetOverrides(OverrideCollection? overrides)
    {
        if (_overrides is not null) _overrides.CollectionChanged -= OnOverridesChanged;
        _overrides = overrides;
        if (_overrides is not null) _overrides.CollectionChanged += OnOverridesChanged;
        ApplyOverrides();
    }

    private void OnOverridesChanged(object? sender, NotifyCollectionChangedEventArgs e) => ApplyOverrides();

    /// <summary>Gives each part its theme, with its override merged over it.</summary>
    public void ApplyOverrides()
    {
        foreach (var (name, list) in _found)
        {
            foreach (var element in list) ApplyOverride(name, element);
        }
    }

    private void ApplyOverride(string name, StyledElement element)
    {
        var entry = Find(name);
        if (entry is null || (entry.Component is null && entry.Style is null && entry.Props is null))
        {
            if (_themed.Remove(element)) element.ClearValue(StyledElement.ThemeProperty);
            return;
        }

        var head = entry.Component ?? DefaultTheme(name, element);
        foreach (var layer in new[] { entry.Style, entry.Props })
        {
            if (layer is not null) head = Derive(layer, head);
        }

        element.Theme = head;
        _themed.Add(element);
    }

    /// <summary>The theme key of a part: <c>Button.StyledBaseButton</c>.</summary>
    public string ThemeKey(string part) => $"{_component}.Styled{part}";

    /// <summary>
    /// The part's own theme: <c>{Component}.Styled{Part}</c>, or for a part with none (a part that
    /// is a control with a theme of its own, as a Metered control inside another), its type's theme.
    /// </summary>
    private ControlTheme? DefaultTheme(string part, StyledElement element)
    {
        if (_owner.TryFindResource(ThemeKey(part), _owner.ActualThemeVariant, out var value) && value is ControlTheme keyed) return keyed;
        for (var type = element.GetType(); type is not null && type != typeof(StyledElement); type = type.BaseType)
        {
            if (element.TryFindResource(type, element.ActualThemeVariant, out var typed) && typed is ControlTheme byType) return byType;
        }

        return null;
    }

    private Override? Find(string part)
    {
        if (_overrides is null) return null;
        Override? viaAlias = null;
        foreach (var entry in _overrides)
        {
            if (entry.Part == part) return entry;
            if (viaAlias is null && _aliases.TryGetValue(entry.Part, out var target) && target == part) viaAlias = entry;
        }

        return viaAlias;
    }

    /// <summary>
    /// An override theme merged over another: Base Web deep-merges an override's style over the
    /// part's whole style object, so an override's plain value replaces the default's however the
    /// default reached it, by a <c>$</c> prop included.
    /// </summary>
    /// <remarks>
    /// <para>The app's theme is left as it is: the merge is a copy of it based on <paramref name="over"/>,
    /// made once for each pair and kept while the app's theme lives, so one theme serves any number
    /// of controls, parts and theme instances. A theme with a <c>BasedOn</c> of its own is used as it is.</para>
    /// <para>Here the defaults that hang on a state are the part theme's state styles, which would win
    /// over a plain setter of a theme based on it: Avalonia gives a style with a pseudoclass in its
    /// selector trigger priority, above a plain setter's. So in the copy the theme's own setters are
    /// a style of their own, ahead of its state styles, whose selector is always true but is a
    /// trigger, <c>^:not(:metered-never)</c>; among triggers a derived theme's come after its base's
    /// and win.</para>
    /// </remarks>
    private static ControlTheme Derive(ControlTheme layer, ControlTheme? over)
    {
        if (layer.BasedOn is not null) return layer;
        var copies = Copies.GetOrCreateValue(layer);
        var key = (object?)over ?? NoBase;
        lock (copies)
        {
            if (copies.TryGetValue(key, out var copy)) return copy;
            copy = new ControlTheme(layer.TargetType!) { BasedOn = over };
            if (layer.Setters.Count > 0)
            {
                var plain = new Style(x => x.Nesting().Not(y => y.Class(":metered-never")));
                foreach (var setter in layer.Setters) plain.Setters.Add(setter);
                copy.Children.Add(plain);
            }

            foreach (var child in layer.Children) copy.Children.Add(Copy(child));
            foreach (var animation in layer.Animations) copy.Animations.Add(animation);
            copies.Add(key, copy);
            return copy;
        }
    }

    private static IStyle Copy(IStyle style)
    {
        if (style is not Style s) return style;
        var copy = new Style { Selector = s.Selector };
        foreach (var setter in s.Setters) copy.Setters.Add(setter);
        foreach (var animation in s.Animations) copy.Animations.Add(animation);
        foreach (var child in s.Children) copy.Children.Add(Copy(child));
        return copy;
    }

    private static readonly object NoBase = new();

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ControlTheme, Dictionary<object, ControlTheme>> Copies = new();

    private static void Set(StyledElement element, string pseudoClass, bool on) =>
        ((IPseudoClasses)element.Classes).Set(pseudoClass, on);
}
