using Avalonia.Controls;
using Metered.Controls;

namespace Metered.Sample.Pages;

/// <summary>
/// Base Web's Select, as its documentation's examples: basic (controlled.tsx), search single and multi
/// (search-single-pick.tsx, search-multi-pick.tsx), creatable single and multi (creatable.tsx, creatable-multi.tsx), sizes
/// (sizes.tsx), grouped (grouped.tsx), not searchable (native.tsx), and its states (select-states.scenario.tsx).
/// </summary>
[SamplePage("Select", Order = 97)]
public partial class SelectPage : UserControl
{
    /// <summary>The examples' colours: their names, Base Web's labels (the ids are the names here).</summary>
    public static readonly string[] Colours = ["AliceBlue", "AntiqueWhite", "Aqua", "Aquamarine", "Azure", "Beige"];

    public SelectPage()
    {
        InitializeComponent();
        foreach (var select in new[] { Basic, Search, SearchMulti, Creatable, CreatableMulti, Mini, Compact, Default, Large, Native, Error, Positive, Disabled, Loading })
        {
            select.Options = Options();
        }

        SearchMulti.Value = [Find(SearchMulti, "Aqua"), Find(SearchMulti, "Azure")];
        CreatableMulti.Value = [Find(CreatableMulti, "AliceBlue")];
        foreach (var select in new[] { Mini, Compact, Default, Large, Native, Error, Positive, Disabled, Loading }) select.Value = [Find(select, "Aqua")];

        // grouped.tsx: Blueish and Whiteish.
        Grouped.Options = new List<object>
        {
            new SelectOption("Black"),
            Group("Blueish", "AliceBlue", "Aqua", "Aquamarine", "Azure"),
            Group("Whiteish", "AntiqueWhite", "Beige", "Bisque"),
        };
    }

    private static List<object> Options() => [.. Colours.Select(c => (object)new SelectOption(c))];

    private static SelectGroup Group(string label, params string[] colours)
    {
        var group = new SelectGroup { Label = label };
        group.Items.AddRange(colours.Select(c => (object)new SelectOption(c)));
        return group;
    }

    private static object Find(Select select, string label) => select.Options!.OfType<SelectOption>().First(o => (string?)o.Label == label);
}
