# tools/

Developer tooling. Nothing here ships in a package. All need `npm install` at the repository root
(Base Web's `baseui`, pinned, with React and Styletron, which its styled components load).

| Tool | What it does |
|---|---|
| `tokens/gen.js` | Writes `src/Metered.Themes/Tokens/*.g.axaml` from `baseui`'s LightTheme and DarkTheme (colours, sizing, borders, lighting, animation, typography, the Button's box-shadows) and `Controls/Button.Parts.g.axaml`, the Button's part themes (`tokens/button.js`). `--check` fails if a file would change. |
| `reference/oracle.js` | Calls a Base Web styled component's own style function with a theme and `$` props, and resolves the result for an interaction state by the cascade `precedence.js` measured. |
| `reference/cases.js` | Writes `tests/Metered.Tests/Reference/*.oracle.json`: the oracle's answer for every case the tests build. `--check` as above. |
| `reference/css.js` | CSS values (colours, lengths, box-shadows, weights, families) to Avalonia's. Throws on a format it doesn't know. |
| `reference/precedence.js` | Reads computed styles on baseweb.design in headless Chromium (Playwright): which of `:hover`, `:active` and `:focus-visible` wins. Needs a Chromium for `playwright-core` (`npx playwright-core install chromium`). |
