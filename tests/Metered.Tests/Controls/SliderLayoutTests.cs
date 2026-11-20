using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Metered.Controls;
using Slider = Metered.Controls.Slider;

namespace Metered.Tests.Controls;

/// <summary>
/// Where a slider's parts are and what size, against the lengths of Base Web's style functions (SliderOracle) and where
/// react-range places the thumbs and marks (react-range/lib/Range.js: getOffsets and calculateMarkOffsets).
/// </summary>
public class SliderLayoutTests
{
    private static SliderLayout L => SliderOracle.Data.Layout;

    /// <summary>A slider 350 wide: a phone's 390, less 20 each side.</summary>
    private static (Slider Slider, Scene Scene) Show(Slider slider) =>
        (slider, new Scene(new StackPanel { Margin = new Thickness(20, 80, 20, 20), Children = { slider } }));

    private static Rect In(Visual part, Visual of) =>
        new(part.TranslatePoint(default, of)!.Value, part.Bounds.Size);

    [AvaloniaFact]
    public void The_track_pads_a_line_and_the_tick_bar_follows()
    {
        var (slider, scene) = Show(new Slider());
        using var _ = scene;
        var track = (Border)slider.Part(Slider.TrackPart)!;
        var inner = (SliderInnerTrack)slider.Part(Slider.InnerTrackPart)!;
        var bar = (Border)slider.Part(Slider.TickBarPart)!;
        var p = L.TrackPadding;

        Assert.Equal(new Thickness(p[0], p[1], p[2], p[3]), track.Padding);
        Assert.Equal(new Rect(p[0], p[1], 350 - p[0] - p[2], L.InnerTrackHeight), In(inner, slider));
        Assert.Equal(new CornerRadius(L.InnerTrackRadius), inner.CornerRadius);
        Assert.Equal(new Rect(0, 0, 350, p[1] + L.InnerTrackHeight + p[3]), In(track, slider));

        var b = L.TickBarPadding;
        Assert.Equal(new Thickness(b[0], b[1], b[2], b[3]), bar.Padding);
        Assert.Equal(track.Bounds.Bottom, In(bar, slider).Y);
        var ticks = slider.Elements(Slider.TickPart).Cast<ContentPresenter>().ToList();
        Assert.Equal(2, ticks.Count);
        // justify-content: space-between: the minimum at the start, the maximum at the end; align-items: center.
        Assert.Equal(b[0], In(ticks[0], bar).X);
        Assert.Equal(350 - b[2], In(ticks[1], bar).Right, 3);
        foreach (var tick in ticks)
        {
            Assert.Equal(L.Tick.FontSize, tick.FontSize);
            Assert.Equal(L.Tick.LineHeight, tick.LineHeight);
            Assert.Equal(Enum.Parse<FontWeight>(L.Tick.FontWeight), tick.FontWeight);
            Assert.Equal(L.Tick.LineHeight, tick.Bounds.Height);
        }

        Assert.Equal(L.Tick.LineHeight + b[1] + b[3], bar.Bounds.Height);
        Assert.Equal(track.Bounds.Height + bar.Bounds.Height, slider.Bounds.Height);
    }

    public static TheoryData<double[]> Values => new([0], [30], [100], [20, 70], [0, 100], [45.5]);

    [AvaloniaTheory]
    [MemberData(nameof(Values))]
    public void Each_thumb_is_centred_on_its_value_and_on_the_line(double[] value)
    {
        var (slider, scene) = Show(new Slider { Value = value, Step = 0.5 });
        using var _ = scene;
        var inner = (SliderInnerTrack)slider.Part(Slider.InnerTrackPart)!;
        var width = inner.Bounds.Width;
        Assert.Equal(value.Length, slider.Thumbs.Count);
        for (var i = 0; i < value.Length; i++)
        {
            var thumb = slider.Thumbs[i];
            Assert.Equal(new Size(L.Thumb[0], L.Thumb[1]), thumb.Bounds.Size);
            Assert.Equal(new CornerRadius(L.ThumbRadius), thumb.CornerRadius);
            // getOffsets, Direction.Right: the line's width × relativeValue − half the thumb; across, half the difference
            // of the heights above the line. Avalonia lays out on whole pixels: within half of one.
            Assert.Equal(width * value[i] / 100 - L.Thumb[0] / 2, thumb.Bounds.X, 0.5);
            Assert.Equal(-(L.Thumb[1] - L.InnerTrackHeight) / 2, thumb.Bounds.Y, 0.5);
        }
    }

    [AvaloniaFact]
    public void The_label_sits_above_its_thumb_with_its_stem()
    {
        var (slider, scene) = Show(new Slider { Value = [40], PersistentThumb = true });
        using var _ = scene;
        var thumb = slider.Thumbs[0];
        var value = (ContentPresenter)slider.Elements(Slider.ThumbValuePart)[0];
        var stem = (Border)slider.Elements(Slider.InnerThumbPart)[0];
        var p = L.ThumbValuePadding;

        Assert.Equal(new Thickness(p[0], p[1], p[2], p[3]), value.Padding);
        Assert.Equal(new CornerRadius(L.ThumbValueRadius), value.CornerRadius);
        Assert.Equal(L.ThumbValueText.FontSize, value.FontSize);
        Assert.Equal(L.ThumbValueText.LineHeight, value.LineHeight);
        Assert.Equal(Enum.Parse<FontWeight>(L.ThumbValueText.FontWeight), value.FontWeight);
        Assert.Equal(TextWrapping.NoWrap, value.TextWrapping);
        // position: absolute; top: −56px, in a flex box that centres it across: its static place.
        Assert.Equal(L.ThumbValueTop, value.Bounds.Y);
        Assert.Equal(p[1] + L.ThumbValueText.LineHeight + p[3], value.Bounds.Height);
        Assert.Equal((thumb.Bounds.Width - value.Bounds.Width) / 2, value.Bounds.X, 0.5);

        Assert.Equal(new Rect((thumb.Bounds.Width - L.InnerThumb[0]) / 2, L.InnerThumbTop, L.InnerThumb[0], L.InnerThumb[1]), stem.Bounds);
        // Drawn after the value, so the stem joins the two.
        Assert.Equal([value, stem], thumb.Children);
    }

    public static TheoryData<double, double, double> Steps => new()
    {
        { 0, 100, 10 },
        { 0, 100, 7 },
        { -300, 300, 50 },
    };

    [AvaloniaTheory]
    [MemberData(nameof(Steps))]
    public void A_mark_at_each_step(double min, double max, double step)
    {
        var (slider, scene) = Show(new Slider { Min = min, Max = max, Step = step, Value = [min], Marks = true });
        using var _ = scene;
        var inner = (SliderInnerTrack)slider.Part(Slider.InnerTrackPart)!;
        var marks = slider.Elements(Slider.MarkPart).Cast<Border>().ToList();
        // react-range: numOfMarks = (max − min) / step, and a mark for each i < numOfMarks + 1.
        var steps = (max - min) / step;
        Assert.Equal((int)Math.Ceiling(steps + 1), marks.Count);
        // The mark's margin brings it from the root to the line: here it is placed from the line.
        Assert.Equal(L.TrackPadding[0], L.MarkMarginLeft);
        for (var i = 0; i < marks.Count; i++)
        {
            var mark = marks[i];
            Assert.Equal(new Size(L.Mark[0], L.Mark[1]), mark.Bounds.Size);
            // calculateMarkOffsets: Math.round(parseInt(width) / numOfMarks × i + paddingLeft − markWidth / 2), and
            // −Math.round((markHeight − parseInt(height)) / 2) down.
            Assert.Equal(Math.Floor(Math.Floor(inner.Bounds.Width) / steps * i - L.Mark[0] / 2 + 0.5), mark.Bounds.X);
            Assert.Equal(-Math.Floor((L.Mark[1] - L.InnerTrackHeight) / 2 + 0.5), mark.Bounds.Y);
        }

        // Under the thumbs.
        Assert.True(inner.Children.IndexOf(marks[^1]) < inner.Children.IndexOf(slider.Thumbs[0]));
    }

    [AvaloniaFact]
    public void No_marks_without_marks()
    {
        var (slider, scene) = Show(new Slider { Step = 10 });
        using var _ = scene;
        Assert.Empty(slider.Elements(Slider.MarkPart));
        slider.Marks = true;
        Assert.Equal(11, slider.Elements(Slider.MarkPart).Count);
        slider.Step = 25;
        Assert.Equal(5, slider.Elements(Slider.MarkPart).Count);
        slider.Marks = false;
        Assert.Empty(slider.Elements(Slider.MarkPart));
        Assert.Single(((SliderInnerTrack)slider.Part(Slider.InnerTrackPart)!).Children);
    }
}
