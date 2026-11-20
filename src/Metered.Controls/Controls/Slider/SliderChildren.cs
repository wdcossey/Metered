using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace Metered.Controls;

/// <summary>
/// A panel's children for the slider's elements that draw themselves (<see cref="SliderInnerTrack"/>,
/// <see cref="SliderThumb"/>): Avalonia's <see cref="Panel"/> seals its rendering, so they are controls that keep their
/// children in the logical and visual trees as a panel does.
/// </summary>
internal static class SliderChildren
{
    /// <summary>Keeps the trees in step with the collection.</summary>
    public static void Follow(Avalonia.Controls.Controls children, IAvaloniaList<ILogical> logical, IAvaloniaList<Visual> visual, Action changed)
    {
        children.CollectionChanged += (_, e) =>
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    logical.InsertRange(e.NewStartingIndex, e.NewItems!.OfType<Control>());
                    visual.InsertRange(e.NewStartingIndex, e.NewItems!.OfType<Visual>());
                    break;
                case NotifyCollectionChangedAction.Remove:
                    logical.RemoveAll(e.OldItems!.OfType<Control>());
                    visual.RemoveAll(e.OldItems!.OfType<Visual>());
                    break;
                default:
                    logical.Clear();
                    visual.Clear();
                    logical.AddRange(children);
                    visual.AddRange(children);
                    break;
            }

            changed();
        };
    }
}
