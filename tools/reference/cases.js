// Writes tests/Metered.Tests/Reference/*.oracle.json: what Base Web's own style functions give for
// each case the tests build, resolved for its interaction state (oracle.js) and converted to
// Avalonia's units. The tests build the same case in Avalonia and compare. theme.oracle.json is
// Base Web's createTheme and createDarkTheme (themes.js).
//
//   node tools/reference/cases.js          write the files
//   node tools/reference/cases.js --check  fail if a file would change
'use strict';

const fs = require('fs');
const path = require('path');
const css = require('./css');
const { THEMES, evaluate, resolve } = require('./oracle');
const button = require('baseui/button/styled-components');
const group = require('baseui/button-group/styled-components');
const version = require('baseui/package.json').version;

const root = path.resolve(__dirname, '../..');
const outDir = path.join(root, 'tests/Metered.Tests/Reference');
const check = process.argv.includes('--check');

const KINDS = ['primary', 'secondary', 'tertiary', 'dangerPrimary', 'dangerSecondary', 'dangerTertiary', 'outline'];
const props = (extra) => ({
  $kind: 'primary', $size: 'medium', $shape: 'rectangular', $isSelected: false, $isLoading: false,
  $disabled: false, $isFocusVisible: false, $backgroundSafe: false, $widthType: 'hug', $minHitArea: 'click', ...extra,
});

// Interaction states: [name, extra props, cascade state].
const STATES = [
  ['rest', {}, {}],
  ['hover', {}, { hover: true }],
  ['active', {}, { hover: true, active: true }],
  ['focusVisible', { $isFocusVisible: true }, { focusVisible: true }],
  ['selected', { $isSelected: true }, {}],
  ['selectedHover', { $isSelected: true }, { hover: true }],
  ['selectedFocusVisible', { $isSelected: true, $isFocusVisible: true }, { focusVisible: true }],
  ['loading', { $isLoading: true }, {}],
  ['loadingHover', { $isLoading: true }, { hover: true }],
  ['disabled', { $disabled: true }, { disabled: true }],
  ['disabledSelected', { $disabled: true, $isSelected: true }, { disabled: true }],
];

/** The box-shadows on the element, with an outline's ::after overlay drawn as tools/tokens/gen.js draws it. */
function shadows(self, after) {
  const parts = [];
  if (after.boxShadow && after.backgroundColor && after.backgroundColor !== 'transparent') {
    parts.push(`inset 999 999 0 0 ${css.color(after.backgroundColor)}`);
    parts.push(css.boxShadows(after.boxShadow));
  }
  const own = css.boxShadows(self.boxShadow ?? 'none');
  if (own !== 'none') parts.push(own);
  return parts.length ? parts.join(', ') : 'none';
}

const colours = [];
for (const theme of Object.keys(THEMES)) {
  for (const kind of KINDS) {
    for (const [state, extra, cascade] of STATES) {
      for (const safe of [false, true]) {
        const style = evaluate(button.BaseButton, THEMES[theme], props({ $kind: kind, $backgroundSafe: safe, ...extra }));
        const { self, after } = resolve(style, cascade);
        colours.push({
          theme, kind, state, backgroundSafe: safe,
          background: css.color(self.backgroundColor),
          // Base Web sets no text colour on a loading button: the content isn't drawn.
          foreground: self.color === undefined ? null : css.color(self.color),
          boxShadow: shadows(self, after),
        });
      }
    }
  }
}

const SIZES = ['xSmall', 'small', 'medium', 'large'];
const SHAPES = ['rectangular', 'pill', 'round', 'circle', 'square'];
const layout = [];
for (const size of SIZES) {
  for (const shape of SHAPES) {
    for (const widthType of ['hug', 'fill']) {
      const s = resolve(evaluate(button.BaseButton, THEMES.Light, props({ $size: size, $shape: shape, $widthType: widthType })), {}).self;
      const radius = s.borderTopLeftRadius === '50%' ? 'half' : css.px(s.borderTopLeftRadius);
      layout.push({
        size, shape, widthType,
        padding: [s.paddingLeft, s.paddingTop, s.paddingRight, s.paddingBottom].map(css.px),
        radius,
        width: s.width === undefined || s.width === '100%' ? null : css.px(s.width),
        height: s.height === undefined ? null : css.px(s.height),
        minWidth: s.minWidth === undefined ? null : css.px(s.minWidth),
        minHeight: s.minHeight === undefined ? null : css.px(s.minHeight),
        fontSize: css.px(s.fontSize),
        fontWeight: css.fontWeight(s.fontWeight),
        lineHeight: css.px(s.lineHeight),
      });
    }
  }
}

const spinner = [];
for (const theme of Object.keys(THEMES)) {
  for (const kind of KINDS) {
    for (const size of SIZES) {
      for (const disabled of [false, true]) {
        const s = resolve(evaluate(button.LoadingSpinner, THEMES[theme], props({ $kind: kind, $size: size, $disabled: disabled })), {}).self;
        const c = resolve(evaluate(button.LoadingSpinnerContainer, THEMES[theme], props({ $kind: kind, $size: size })), {}).self;
        spinner.push({
          theme, kind, size, disabled,
          diameter: css.px(s.width), thickness: css.px(s.borderTopWidth),
          foreground: css.color(s.borderTopColor), background: css.color(s.borderLeftColor),
          containerMargin: ['Left', 'Top', 'Right', 'Bottom'].map((side) => css.px(c['margin' + side] ?? 0)),
        });
      }
    }
  }
}

const enhancers = {
  startMarginRight: css.px(resolve(evaluate(button.StartEnhancer, THEMES.Light, props({})), {}).self.marginRight),
  endMarginLeft: css.px(resolve(evaluate(button.EndEnhancer, THEMES.Light, props({})), {}).self.marginLeft),
  artworkSize: Object.fromEntries(SIZES.map((size) => [size, css.px(button.getArtworkSize({ $theme: THEMES.Light, $size: size }))])),
};

const groupRoot = [];
for (const size of SIZES) {
  for (const padding of ['default', 'none', 'custom']) {
    for (const wrap of [null, true, false]) {
      const s = resolve(evaluate(group.StyledRoot, THEMES.Light, { $size: size, $padding: padding, $wrap: wrap ?? undefined }), {}).self;
      groupRoot.push({
        size, padding, wrap,
        columnGap: css.px(s.columnGap), rowGap: css.px(s.rowGap),
        paddingLeft: s.paddingLeft === undefined ? null : css.px(s.paddingLeft),
        paddingRight: s.paddingRight === undefined ? null : css.px(s.paddingRight),
        flexWrap: s.flexWrap ?? null, overflowX: s.overflowX ?? null,
      });
    }
  }
}

const files = {
  // Base Web's radio-v2 Radio and RadioGroup (radio.js).
  'radio.oracle.json': require('./radio').oracle(version),
  'button.oracle.json': { baseui: version, colours, layout, spinner, enhancers },
  'button-group.oracle.json': { baseui: version, root: groupRoot },
  // Base Web's createTheme and createDarkTheme on a few foundations and overrides (themes.js).
  'theme.oracle.json': require('./themes').oracle(version),
};

fs.mkdirSync(outDir, { recursive: true });
const stale = [];
for (const [name, data] of Object.entries(files)) {
  const file = path.join(outDir, name);
  const text = JSON.stringify(data, null, 1) + '\n';
  if (fs.existsSync(file) && fs.readFileSync(file, 'utf8') === text) continue;
  if (check) stale.push(name);
  else { fs.writeFileSync(file, text); console.log('wrote', path.relative(root, file)); }
}
if (check && stale.length) { console.error('Out of date:', stale.join(', ')); process.exit(1); }
