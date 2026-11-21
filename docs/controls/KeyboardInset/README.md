# KeyboardInset

`Metered.Controls.KeyboardInset`, an Avalonia `Decorator` that keeps its child clear of a phone's soft keyboard (#108).
Base Web has nothing of the kind: it relies on the browser, which shrinks its visual viewport to the space above the
keyboard and scrolls the focused field into view. An Avalonia app on a phone gets neither, so this does what the
browser does, for the content an app puts in it. Opt-in; the sample's shell (`SampleShell`) puts its page host in one.

```xml
<b:KeyboardInset>
  <ScrollViewer>
    <!-- a page of inputs -->
  </ScrollViewer>
</b:KeyboardInset>
```

## What it does

- **While the keyboard is up, the child ends above it.** The child's foot is padded by as much of the decorator as the
  keyboard covers: its own bounds against the keyboard's rect, in the top level's coordinates, so a decorator that ends
  above the keyboard pads nothing, and one with a footer below it pads only what the keyboard reaches. Its own
  `Padding` is kept, above the keyboard. A `ScrollViewer` inside ends above the keyboard, so its range grows by the
  inset and its last control can be scrolled clear. `Inset`, read-only, says by how much.
- **When the keyboard opens, the focused control is brought into view** if it is inside the decorator
  (`BringIntoView`, which the nearest `ScrollViewer` takes), and then its caret, if it takes text (the input method's
  `CursorRectangle`), so the line being typed in a text taller than the room left is above the keyboard too. Again
  whenever the room left above the keyboard changes (a rotation, a taller keyboard). A focus that moves while the
  keyboard is up is brought into view by the `ScrollViewer` itself (`BringIntoViewOnFocusChange`, on by default).
- **When the keyboard closes, the child has its height back**, and the `ScrollViewer` clamps its offset.
- **With no input pane** (Avalonia's headless platform, a desktop with no touch keyboard), it does nothing.

The pane is read when it says its state changed, and, once laid out, when the decorator is attached (a keyboard already
up), when the top level resizes, when the platform's insets change, and when the decorator moves or resizes while the
keyboard is up. On API 31 and later Android raises `StateChanged` only as the keyboard's insets start to animate; a
rotation, or a keyboard hidden while the app is in the background, changes the pane with no animation, and only the
insets manager's `SafeAreaChanged` (raised from every `OnApplyWindowInsets`) says so. Each reading lays the tree out
first, so a decorator inside another that has just made room is read in that room and pads nothing more.

The inset is taken from the room the child is given, never added to the decorator's own size, so a reading can't feed
its last inset back into the next. It is meant to be given its height (a page's or a view's root, a `Grid` row): one
whose height follows its child's (in a `StackPanel`, say) stays as high as its child would have it, and its child is
padded past its foot, overflowing it, so it keeps nothing clear.

## Why Avalonia doesn't do it

Read from Avalonia 12.1.4's Android backend (`Avalonia.Android`, `AndroidInsetsManager`, decompiled):

- **The window is never resized for the keyboard.** The sample targets API 36, and on Android 15 (API 35) and later an
  app that targets 35 or later is drawn edge to edge whatever it asks (`_isDisplayEdgeToEdgeForced`); an edge-to-edge
  window gets the keyboard as IME insets and keeps its size, so `adjustResize` changes nothing. The Android head sets no
  `WindowSoftInputMode`, and neither does Avalonia's activity.
- **`adjustPan` has nothing to pan to.** It pans the window to the focused Android view's focus rectangle, and
  Avalonia's whole UI is one full-screen view.
- **Avalonia reports the keyboard and moves nothing for it.** `TopLevel.InputPane` is the insets manager:
  `StateChanged` comes from the insets animation's `OnStart` on API 31 and later (from `OnApplyWindowInsets` on 30, and
  from the state's setter on 29 and below), with `EndRect` the keyboard's final rect, `OccludedRect` the rect now, both
  above the navigation bar's inset. Nothing in `Avalonia.Controls` reads it; `TopLevel.AutoSafeAreaPadding` pads the
  top level's content for the system bars only.

## Known differences

1. **The inset is applied at once**, at the start of the keyboard's animation, with its final rect, rather than
   following it frame by frame as a browser's visual viewport can. Opening, the content ends above where the keyboard
   will be before it gets there; closing, it has its height back while the keyboard slides away.
2. **The focused control is scrolled as little as will show it**, as Avalonia's `ScrollViewer` brings a descendant into
   view: an input below the space ends at its foot, just above the keyboard. Where Chrome on Android puts such a field
   wasn't measured.
3. **Opt-in.** An app that doesn't use it, or content outside it (an overlay in another top level, the shell's Debug
   footer), stays under the keyboard, as before.
4. **Only an edge-to-edge window is reasoned through.** On Android 14 and earlier, or an app that targets less than
   API 35, Avalonia's window isn't edge to edge (`DisplayEdgeToEdgePreference` is false), and Android chooses whether
   to pan or resize it for the keyboard (no `WindowSoftInputMode` is set, so `adjustUnspecified`). A window resized for
   the keyboard would be padded twice: Avalonia's `OccludedRect` is measured up from the bottom of the client area, which
   would already end above the keyboard. Not tried; the sample's only device is an Android 17 phone.
5. **Not yet tried on a phone.** Headless, Avalonia has no input pane, and its `IInputPane` can't be implemented outside
   Avalonia (its reference assembly carries a member user code can't write), so the tests drive the decorator through an
   internal seam (`KeyboardInset.InputPane`, a `KeyboardInset.IPane`), a fake that raises `StateChanged` as Android's
   backend does. The device run checks the rest (below).

## Tests

`tests/Metered.Tests/Controls/KeyboardInsetTests.cs`: the padding by the overlap, from the decorator's own place (under
a bar, over a footer), nothing when closed (and for a closed pane with a rect, Avalonia's floating keyboard, or an open
one with none), nothing for a keyboard below it, no more than its height, its own padding kept, the focused input at a
page's foot scrolled above the keyboard, the caret of a text taller than the room left, the page's range grown by the
inset, a focused control outside it left alone, a keyboard already up when it is attached, a resize (and the focused
input brought back into view), a keyboard hidden with no animation, a decorator that moves, one whose height follows
its child's read again and again, one inside another, detach, and no pane.
`tests/Metered.Tests/Sample/ShellKeyboardTests.cs`: the shell's host is in one and its footer isn't, and the Input
page's Clearable input, scrolled to the page's foot, is brought above the keyboard; with `METERED_FRAMES`, a picture of
the page under a stand-in keyboard before and after. Base Web has no value for any of
these: what each expects is the scene's own geometry.

## On a phone

What the Pixel run should check, on the Input, Textarea and FormControl pages:

- an input in the page's last screen (the Input page's Clearable), tapped: the page scrolls it above the keyboard, and
  the page can be scrolled by hand to its foot, which ends above the keyboard;
- the keyboard closed (Back, or Gboard's own close): the page has its height back and no gap is left at its foot;
- the focus moved to another input with the keyboard up: it is scrolled into view, and the keyboard stays;
- a rotation with the keyboard up, and Gboard's height changed (its emoji panel, or the suggestions strip), the page
  still ending at the keyboard's top;
- the bar at the top and the system bars untouched, and the ready line's `page=` the same as before with no keyboard.
