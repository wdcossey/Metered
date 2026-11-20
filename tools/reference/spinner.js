// The oracle for Spinner: what Base Web's own style function gives (baseui/spinner/styled-components), for the cases
// the tests build, in Avalonia's units. Kept apart from tools/tokens/spinner.js, which writes the theme: the tests
// compare one with the other. Read by cases.js, which writes tests/Metered.Tests/Reference/spinner.oracle.json.
'use strict';

const css = require('./css');
const { THEMES, evaluate } = require('./oracle');
const sp = require('baseui/spinner/styled-components');
const { SIZE } = require('baseui/spinner/constants');

/**
 * CSS's initial border width, `medium`, which a browser keeps when the declared one is invalid (Base Web's `NaNpx`): 3px
 * (CSS Backgrounds and Borders Level 3, §3.3 'border-width': "thin ≤ medium ≤ thick"; Level 4 and every browser: 3px).
 */
const MEDIUM = 3;

const SIDES = ['Top', 'Right', 'Bottom', 'Left'];

/** A length CSS takes for width or border-width: a number of pixels, not negative; null for one a browser drops. */
function valid(v) {
  const m = /^(\d+(?:\.\d+)?)px$/.exec(String(v));
  return m ? Number(m[1]) : null;
}

/** The style for the props, with its four borders checked equal. */
function style(theme, props) {
  const s = evaluate(sp.StyledSpinner, theme, props);
  for (const k of ['Width', 'Style']) {
    if (SIDES.some((side) => s[`border${side}${k}`] !== s[`borderTop${k}`])) throw new Error(`border ${k} differs by side`);
  }
  if (s.borderRightColor !== s.borderBottomColor || s.borderBottomColor !== s.borderLeftColor) throw new Error('the other borders differ');
  if (s.width !== s.height) throw new Error('not square');
  return s;
}

/**
 * The values the tests give $size and $borderWidth, as Metered writes them: a size by name, a sizing token's name,
 * other text, a number, or unset. The scenario's (src/spinner/__tests__/spinner.scenario.tsx at 3adb0478c9) among them:
 * the three sizes, '20px', '40px', '60px', 60, 20, scale1000, scale900, scale700, scale300, scale200, scale100.
 */
const SIZES = [undefined, SIZE.small, SIZE.medium, SIZE.large, 'scale1000', 'scale900', 'scale700', '20px', '60px', 60, '60.7px', 0, -5, '-5px', 'abc'];
const BORDERS = [undefined, SIZE.small, SIZE.medium, SIZE.large, 'scale300', 'scale200', 'scale100', 20, '12px', 0, -3, '-3px', 'abc'];

function describe(v) {
  if (v === undefined) return { kind: 'unset' };
  if (typeof v === 'number') return { kind: 'number', value: v };
  if (Object.values(SIZE).includes(v)) return { kind: 'size', value: v };
  return { kind: 'text', value: v };
}

function layout() {
  const cases = [];
  for (const size of SIZES) {
    for (const borderWidth of BORDERS) {
      const props = {};
      if (size !== undefined) props.$size = size;
      if (borderWidth !== undefined) props.$borderWidth = borderWidth;
      const s = style(THEMES.Light, props);
      const box = valid(s.width);
      const border = valid(s.borderTopWidth) ?? MEDIUM;
      cases.push({ size: describe(size), borderWidth: describe(borderWidth), box, border });
    }
  }
  return cases;
}

function colours() {
  const cases = [];
  for (const theme of Object.keys(THEMES)) {
    for (const given of [null, 'negative', 'positive']) {
      const t = THEMES[theme];
      const s = style(t, given === null ? {} : { $color: t.colors[given] });
      cases.push({
        theme, color: given,
        foreground: css.color(s.borderTopColor),
        background: css.color(s.borderLeftColor),
      });
    }
  }
  return cases;
}

/** The turn: Base Web's keyframes (`spin`), its duration, curve and count. */
function motion() {
  const s = style(THEMES.Light, {});
  const deg = (v) => Number(/^rotate\((-?[\d.]+)deg\)$/.exec(v.transform)[1]);
  return {
    from: deg(s.animationName.from),
    to: deg(s.animationName.to),
    seconds: css.seconds(s.animationDuration),
    timing: s.animationTimingFunction,
    iterations: s.animationIterationCount,
  };
}

function oracle(version) {
  const s = style(THEMES.Light, {});
  return {
    baseui: version,
    colours: colours(),
    layout: layout(),
    motion: motion(),
    cursor: s.cursor,
    display: s.display,
    borderRadius: s.borderRadius,
    borderStyle: s.borderTopStyle,
  };
}

module.exports = { oracle, MEDIUM, valid };
