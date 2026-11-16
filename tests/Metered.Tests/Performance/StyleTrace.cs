using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Metered.Tests.Performance;

/// <summary>
/// Which global style rules an element is checked against, from Avalonia's own diagnostics. See
/// docs/performance/README.md. Counts are exact, so CI can rely on them. The time of each check
/// isn't: tracing costs more than the check does; the cost spike (<c>Spikes/StyleCostSpike</c>)
/// measures time, with tracing off. From Cornflower's and Avonic's StyleTrace.
/// </summary>
internal static class StyleTrace
{
    private const string Source = "Avalonia.Diagnostic.Source";
    private const string AttachingStyle = "Avalonia.AttachingStyle";

    /// <summary>Avalonia's switch for its diagnostics. It reads it once, so <see cref="TestApp"/> sets it as the assembly loads.</summary>
    internal const string DiagnosticsSwitch = "Avalonia.Diagnostics.Diagnostic.IsEnabled";

    /// <summary>The cost spike's variable: set, the diagnostics stay off, as an app has them, and the trace refuses to run.</summary>
    internal const string BenchVariable = "METERED_BENCH";

    /// <summary>One rule checked against one element.</summary>
    internal readonly record struct Check(string Selector, string Result)
    {
        /// <summary>The check got past the element's type, which costs several times what a rejected type does.</summary>
        public bool ReachedElement => Result != "NeverThisType";

        /// <summary>A comma makes a rule cost as much as that many rules.</summary>
        public IReadOnlyList<string> Alternatives => StyleCensus.SplitAlternatives(Selector);
    }

    /// <summary>
    /// The checks of <paramref name="themes"/>' global rules against one element, attached to a window
    /// that's already open with <paramref name="themes"/> as its only <c>Styles</c>. Only the element's
    /// own: it's styled as it joins the tree, and its template's parts later, in layout, after the
    /// listening stops. A <c>ControlTheme</c>'s checks, which Avalonia reports through the same
    /// activity, aren't global, and aren't counted.
    /// </summary>
    public static IReadOnlyList<Check> ChecksFor(Control element, params IStyle[] themes)
    {
        if (!AppContext.TryGetSwitch(DiagnosticsSwitch, out var enabled) || !enabled)
        {
            throw new InvalidOperationException(
                $"Avalonia's diagnostics are off, so the trace would see nothing and every budget would pass. Is {BenchVariable} set? The cost spike runs on its own.");
        }

        using var scene = new Scene(new Panel(), themes: themes);
        var host = (Panel)scene.Window.Content!;
        var global = themes.SelectMany(StyleCensus.Rules).ToHashSet<object>(ReferenceEqualityComparer.Instance);
        var checks = new List<Check>();
        using (var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == Source,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.OperationName == AttachingStyle && activity.GetTagItem("Style") is Style rule && global.Contains(rule))
                {
                    checks.Add(new Check(rule.ToString(), activity.GetTagItem("SelectorResult")?.ToString() ?? string.Empty));
                }
            },
        })
        {
            ActivitySource.AddActivityListener(listener);
            host.Children.Add(element);
        }

        Dispatcher.UIThread.RunJobs();
        return checks;
    }

    private static string WithoutSpacedCombinators(string alternative) =>
        alternative.Replace(" > ", ">", StringComparison.Ordinal).Replace(" /template/ ", ">", StringComparison.Ordinal);

    /// <summary>The index of the last of these outside brackets, or -1.</summary>
    private static int LastOutsideBrackets(string text, params char[] wanted)
    {
        var depth = 0;
        var inValue = false;
        for (var i = text.Length - 1; i >= 0; i--)
        {
            switch (text[i])
            {
                case ']':
                    inValue = true;
                    break;
                case '[':
                    inValue = false;
                    break;
                case ')' when !inValue:
                    depth++;
                    break;
                case '(' when !inValue:
                    depth--;
                    break;
                default:
                    if (depth == 0 && !inValue && wanted.Contains(text[i]))
                    {
                        return i;
                    }

                    break;
            }
        }

        return -1;
    }

    /// <summary>
    /// Whether a common element can get as far as a descendant combinator, which then looks at
    /// every ancestor. A name on the element stops that, because it can't change. Cornflower also
    /// lets one of its own controls after the combinator stop it; Metered doesn't, as its
    /// <c>Button</c> shares its name with Avalonia's, and the parts' looks are in
    /// <c>ControlTheme</c>s (ADR 0001), so no global rule needs one.
    /// </summary>
    public static bool WalksAncestors(string alternative)
    {
        var parts = WithoutSpacedCombinators(alternative);
        var space = LastOutsideBrackets(parts, ' ');
        if (space < 0)
        {
            return false;
        }

        var after = parts[(space + 1)..];
        var subject = after[(LastOutsideBrackets(after, '>') + 1)..];
        return LastOutsideBrackets(subject, '#') < 0;
    }

    /// <summary>The type a part of a selector names, without its namespace prefix, classes, pseudoclasses, name or properties.</summary>
    private static string TypeName(string part)
    {
        var end = part.IndexOfAny(['.', ':', '#', '[']);
        var type = end < 0 ? part : part[..end];
        return type[(type.IndexOf('|') + 1)..];
    }

    /// <summary>
    /// Whether an alternative gets past the type of an element, judged alone: a rule reports one result for all its commas.
    /// </summary>
    public static bool Reaches(string alternative, Type element)
    {
        var parts = WithoutSpacedCombinators(alternative);
        var subject = parts[(LastOutsideBrackets(parts, '>', ' ') + 1)..];
        const string Is = ":is(";
        if (subject.StartsWith(Is, StringComparison.Ordinal))
        {
            var name = subject[Is.Length..subject.IndexOf(')', StringComparison.Ordinal)];
            for (var type = element; type is not null; type = type.BaseType)
            {
                if (type.Name == name)
                {
                    return true;
                }
            }

            return false;
        }

        var target = TypeName(subject);
        return target.Length == 0 || target == element.Name;
    }
}
