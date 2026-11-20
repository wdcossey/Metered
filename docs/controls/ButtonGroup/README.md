# ButtonGroup

Base Web's `ButtonGroup` and `StatefulButtonGroup` (baseui 18.2.0, `button-group/`), as one
control, `Metered.Controls.ButtonGroup`, an Avalonia `ItemsControl` of `Button`s. See the Button's
gallery for two groups: radio, and checkbox with the outline kind.

## API

| Base Web | Metered |
|---|---|
| `mode` | `Mode` (`None`, `Radio`, `Checkbox`) |
| `selected`, `initialState.selected` | `Selected`, the selected indices, two-way |
| `kind`, `size`, `shape` | `Kind` (secondary by default), `Size`, `Shape`: they replace each button's own |
| `padding` | `PaddingType` |
| `wrap` | `Wrap` (`bool?`) |
| `disabled` | `IsEnabled` |
| `onClick(event, index)` | `Click` (`ButtonGroupClickEventArgs.Index`), after the button's own |
| `overrides` | `Overrides`: `Root` |
| `stateReducer` | not yet |

## Behaviour, as Base Web's

- A click in radio mode selects the button, or clears it if it is the one selected; in checkbox
  mode it toggles it. A group with no mode and no selection selects nothing.
- A button's own `IsSelected` of true is kept; otherwise the group sets it from `Selected`, and
  takes it back when the button leaves.
- `Selected` is by place, as Base Web's `isIndexSelected(selected, index)` is, with
  `React.Children.map`'s index: when a button comes or goes before another, that one is selected,
  and a tab stop, by its new place (#70).
- A button that leaves, however it leaves (removed, the items cleared, or its `ItemsSource` reset,
  as an `ObservableCollection`'s `Clear` does, naming no old items: #45), has its own values back
  as they were when it joined, as Base Web's `cloneElement` leaves the child unchanged: its kind,
  size, shape, selection, tab stop and role, each one it had set, and the control's defaults for
  the rest (#70). The group no longer listens to its clicks.
- Radio: only the selected button (or the first, with none) is a tab stop; the arrow keys move the
  selection and the focus, wrapping; the buttons are announced as radio buttons (checkbox mode: as
  check boxes).
- `Root`: a row with Base Web's gaps (8 between buttons; between rows when wrapping, 12 for the
  extra-small size and 8 otherwise), its padding, and overflow visible, unless `Wrap` is false,
  which scrolls the row sideways with no bar.

## Held by

`Controls/ButtonGroupTests`: the root against Base Web's `StyledRoot` for every size, padding and
wrap (36 cases), and each behaviour above.

## Known differences

1. Base Web's controlled and stateful groups are one: `Selected` is set by a click whatever bound it.
2. `aria-checked` has no Avalonia equivalent on a Button's automation peer; the role is set.
3. A button's own values are those it had when it joined. A value set on the button while it is in
   the group isn't recorded: when it leaves, it gets back what it had when it joined. In Base Web a
   child re-rendered with new props has those outside the group. `RadioGroup` does the same.
