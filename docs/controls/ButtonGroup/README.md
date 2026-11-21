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
  as an `ObservableCollection`'s `Clear` does, naming no old items: #45), has its own values back,
  as Base Web's `cloneElement` replaces a child's props without changing the child (#70). The group
  holds its kind, size, shape, selection, tab stop and role over the button's own, at Avalonia's
  `Animation` priority, the one above a local value, and lets go of them when the button leaves
  (`Hold`, which RadioGroup shares: #80). A held value something else changes with `SetCurrentValue`
  is put back at the group's next apply.
  So the button's own values stay beneath, local, styled or bound, including those set on it while
  it was in the group, and the control's defaults where it has none. The group no longer listens
  to its clicks.
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
2. `aria-checked` has no Avalonia equivalent on a Button's automation peer; the role is set. A button's own role is kept in a plain group,
   where Base Web's `child.props.role || isRadio ? 'radio' : …` reads "radio" (the developer's decision of 2026-11-21, #70).
3. Base Web reads a child's own `isSelected` of true on every render. Here the group reads the
   button's own `IsSelected` each time it applies (a change to the group, or the button's place),
   and when the button's own value changes while the group holds none over it. Avalonia raises no
   change for a value beneath a held one, so an own `IsSelected` that turns true while the group
   holds false shows at the group's next apply.
4. A `TwoWay` binding on a button's `IsSelected` writes the group's value back to its source, as
   Avalonia's bindings write back a value at any priority. The group doesn't take that for the
   button's own true: it does only when the value beneath has changed since the group last set its
   own.
5. The properties the group holds are held at the `Animation` priority, so `IsAnimating` is true
   for them, and an app's own animation of one of them on a grouped button competes with the
   group's value.
