# Metered

A faithful reproduction of Uber's [Base design system](https://baseweb.design) (Base Web, npm
`baseui`) for [AvaloniaUI](https://avaloniaui.net/), mobile first. Not affiliated with or endorsed
by Uber or the AvaloniaUI team.

> **Status: pilot.** Two controls, Button and ButtonGroup, built to test the approach: the tokens,
> the parts and states, and Base Web's overrides API. See [`docs/ROADMAP.md`](docs/ROADMAP.md).

## What "faithful" means here

- **Measured, not remembered.** The tokens are generated from the pinned `baseui` package's own
  themes, and the controls are tested against Base Web's own style functions, evaluated for each
  kind, size, shape and state (`tools/reference/`).
- **Base Web's names.** Every token under its own name (`colors.buttonPrimaryFill`,
  `sizing.scale600`); every prop, part and override key as Base Web has it.
- **The overrides API.** Each control's named parts take `Overrides`: Base Web's `style`, `props`
  and `component`, per part, nested ([ADR 0001](docs/decisions/0001-parts-states-and-overrides.md)).
- **Fonts are substituted.** Uber Move isn't shipped; Base Web's own font stack falls back.

## Packages

| Package | Contents |
|---|---|
| `Metered` | Everything: the one most apps want |
| `Metered.Controls` | The controls and the overrides API, no visual opinion |
| `Metered.Themes` | Base Web's light and dark themes: `BaseTheme` |

Targets .NET 8, Avalonia 12.

```xml
<Application.Styles>
  <b:BaseTheme xmlns:b="using:Metered.Themes" />
</Application.Styles>
```

## Building

```sh
npm install                 # Base Web's package, pinned, for the generators (not committed)
dotnet build Metered.sln
dotnet test  Metered.sln
```

## Attribution

Metered reproduces the design of Uber's Base design system and Base Web (© Uber Technologies, Inc.,
MIT licensed; the notice is in [`src/Metered.Themes/LICENSE-baseweb.txt`](src/Metered.Themes/LICENSE-baseweb.txt)).
It is an independent implementation for the AvaloniaUI ecosystem and is not affiliated with or
endorsed by Uber or the AvaloniaUI team. Uber and Uber Move are trademarks of Uber Technologies, Inc.

## Licence

Apache License 2.0; see [LICENSE](LICENSE).
