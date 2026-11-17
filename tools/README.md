# tools/

Developer tooling. Nothing here ships in a package. All need `npm install` at the repository root
(Base Web's `baseui`, pinned, with React and Styletron, which its styled components load).

| Tool | What it does |
|---|---|
| `tokens/gen.js` | Writes `src/Metered.Themes/Tokens/*.g.axaml` from `baseui`'s LightTheme and DarkTheme (colours, sizing, borders, lighting, animation, typography, the Button's box-shadows from `tokens/button-shadows.js`), `Controls/Button.Parts.g.axaml`, the Button's part themes (`tokens/button.js`), and `Theming/ThemeDerivation.g.cs` (`tokens/theme.js`). `--check` fails if a file would change. |
| `tokens/theme.js` | What Base Web's `createTheme` and `createDarkTheme` derive, read by calling them (and the Button's style function) on sentinel values: which colours follow which foundation colour, what each Button shadow is made of, which paths are one object. Tables of names for `Metered.Themes.Theme` (ADR 0002). Run by `gen.js`. |
| `reference/oracle.js` | Calls a Base Web styled component's own style function with a theme and `$` props, and resolves the result for an interaction state by the cascade `precedence.js` measured. |
| `reference/cases.js` | Writes `tests/Metered.Tests/Reference/*.oracle.json`: the oracle's answer for every case the tests build. `--check` as above. |
| `reference/themes.js` | For `cases.js`: `theme.oracle.json`, Base Web's own `createTheme` and `createDarkTheme` on eight sets of foundation colours and overrides, every token in Metered's keys. |
| `reference/css.js` | CSS values (colours, lengths, box-shadows, weights, families) to Avalonia's. Throws on a format it doesn't know. |
| `breakage/run.js` | Breaks each behaviour in a list on its own, in a fresh clone, and checks that a test goes red ([`docs/performance/README.md`](../docs/performance/README.md#breakage-runs)). Needs git, Node and the .NET SDK, not `npm install`. From Cornflower and Avonic. On a timeout it kills what the run left running from the clone (`breakage/strays.js`): on Linux and macOS, never `run.js` or its ancestors, with the path matched literally and whole; `node tools/breakage/strays.test.js` tests it on real processes, and CI runs it on Linux. |
| `reference/fonts.js` | Measures Uber Move on baseweb.design against OFL candidates for its substitute, in one headless Chromium: widths, glyph heights, line metrics, the baseline in the ramp's line boxes ([`docs/decisions/0003-font.md`](../docs/decisions/0003-font.md)). Prints Markdown; `--write` writes `reference/fonts.g.json` (generated: don't hand-edit). Network and a Chromium, as below. Uber Move is only rendered, never saved. |
| `reference/precedence.js` | Reads computed styles on baseweb.design in headless Chromium (Playwright): which of `:hover`, `:active` and `:focus-visible` wins. Needs a Chromium for `playwright-core` (`npx playwright-core install chromium`). |
