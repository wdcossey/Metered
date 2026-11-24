using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>Base Web's defaultFilterOptions (select/utils/default-filter-options.js) on its options, each filter's options kept (SelectOracle's filtering).</summary>
public class SelectFilterTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, SelectOracle.Data.Filtering.Length));

    [Theory]
    [MemberData(nameof(Cases))]
    public void The_options_kept_are_Base_Webs(int index)
    {
        var c = SelectOracle.Data.Filtering[index];
        var options = SelectOracle.Options("OPTIONS");
        var exclude = c.Exclude is null ? null : SelectOracle.Value(options, c.Exclude);
        var kept = Select.DefaultFilterOptions(options, c.Filter, exclude, c.IgnoreCase);
        Assert.Equal(c.Labels, kept.Select(Select.LabelText));
    }
}
