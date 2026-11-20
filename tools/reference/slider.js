// The oracle for Slider: what Base Web's own style functions give (baseui/slider/styled-components), and what react-range,
// which Base Web's slider drags and steps through, does with a value (react-range/lib/utils.js's normalizeValue, called
// here as it is), for the cases the tests build, in Avalonia's units. Kept apart from tools/tokens/slider.js, which
// writes the themes: the tests compare one with the other. Read by cases.js, which writes
// tests/Metered.Tests/Reference/slider.oracle.json.
'use strict';

const css = require('./css');
const { THEMES, evaluate } = require('./oracle');
const slider = require('baseui/slider/styled-components');
const path = require('path');
// react-range as Base Web's slider loads it: from baseui's own place in node_modules.
const { normalizeValue } = require(require.resolve('react-range/lib/utils', { paths: [path.dirname(require.resolve('baseui/package.json'))] }));

/** slider.js's sharedProps, and $isDragged and $thumbIndex where it passes them. */
const props = (s) => ({
  $disabled: !!s.disabled, $step: s.step ?? 1, $min: s.min ?? 0, $max: s.max ?? 100, $marks: !!s.marks,
  $value: s.value ?? [30], $isFocusVisible: !!s.isFocusVisible, $isDragged: !!s.isDragged, $thumbIndex: s.thumbIndex ?? 0,
});

/** A part's declarations: its style function's, or for a part with none (Root) its static style. */
function part(styled, theme, s) {
  const st = styled.__STYLETRON__;
  if (st.reducers.length === 0) return st.getInitialStyle();
  return evaluate(styled, theme, props(s));
}

/** The InnerTrack's gradient, read back: the colour of each run between its hard stops, and where each ends. */
function runs(background) {
  const m = /^linear-gradient\(to right, (.*)\)$/.exec(background);
  const stops = m[1].split(', ').map((stop) => {
    const [, colour, at] = /^(.+) ([\d.]+)%$/.exec(stop);
    return { colour: css.color(colour), at: Number(at) };
  });
  const out = [];
  for (let i = 0; i < stops.length; i += 2) out.push({ colour: stops[i].colour, to: stops[i + 1].at });
  return out;
}

/** '0 0 0 3px <colour>' as a ring, else the shadow in Avalonia's syntax. */
function shadow(boxShadow) {
  const ring = /^0 0 0 (\S+) (.+)$/.exec(boxShadow);
  if (ring) return { ring: { colour: css.color(ring[2]), thickness: css.px(ring[1]) }, boxShadow: null };
  return { ring: null, boxShadow: css.boxShadows(boxShadow) };
}

/**
 * The states the tests drive with real input, on the first thumb: rest; hover, a mouse over it; touch, a finger pressed
 * on it (dragged: no hover); focusVisible, focused by Tab; persistent, persistentThumb; disabled, with persistentThumb so
 * that its label shows (a disabled slider takes no input).
 */
const STATES = [
  ['rest', {}],
  ['hover', {}],
  ['touch', { isDragged: true }],
  ['focusVisible', { isFocusVisible: true }],
  ['persistent', {}],
  ['disabled', { disabled: true }],
];

function colours() {
  const cases = [];
  for (const theme of Object.keys(THEMES)) {
    const t = THEMES[theme];
    for (const value of [[30], [20, 70]]) {
      for (const [state, extra] of STATES) {
        const s = { value, ...extra };
        const thumb = part(slider.Thumb, t, s);
        const value0 = part(slider.ThumbValue, t, s);
        cases.push({
          theme, value, state,
          track: css.color(part(slider.Track, t, s).backgroundColor),
          innerTrack: runs(part(slider.InnerTrack, t, s).background),
          mark: css.color(part(slider.Mark, t, s).backgroundColor),
          thumb: css.color(thumb.backgroundColor),
          ...shadow(thumb.boxShadow),
          thumbValue: css.color(value0.backgroundColor),
          thumbValueText: css.color(value0.color),
          innerThumb: css.color(part(slider.InnerThumb, t, s).backgroundColor),
          tick: css.color(part(slider.Tick, t, s).color),
        });
      }
    }
  }
  return cases;
}

const text = (s) => ({ fontSize: css.px(s.fontSize), fontWeight: css.fontWeight(s.fontWeight), lineHeight: css.px(s.lineHeight) });
const sides = (s, prefix) => ['Left', 'Top', 'Right', 'Bottom'].map((side) => css.px(s[prefix + side] ?? '0px'));

/** Every length the parts have, from the style functions (the same on either theme). */
function layout() {
  const L = THEMES.Light;
  const track = part(slider.Track, L, {});
  const inner = part(slider.InnerTrack, L, {});
  const thumb = part(slider.Thumb, L, {});
  const value = part(slider.ThumbValue, L, {});
  const stem = part(slider.InnerThumb, L, {});
  const mark = part(slider.Mark, L, {});
  const bar = part(slider.TickBar, L, {});
  return {
    trackPadding: sides(track, 'padding'),
    innerTrackHeight: css.px(inner.height),
    innerTrackRadius: css.px(inner.borderTopLeftRadius),
    thumb: [css.px(thumb.width), css.px(thumb.height)],
    thumbRadius: css.px(thumb.borderTopLeftRadius),
    thumbValueTop: css.px(value.top),
    thumbValuePadding: sides(value, 'padding'),
    thumbValueRadius: css.px(value.borderTopLeftRadius),
    thumbValueText: text(value),
    innerThumbTop: css.px(stem.top),
    innerThumb: [css.px(stem.width), css.px(stem.height)],
    mark: [css.px(mark.width), css.px(mark.height)],
    markMarginLeft: css.px(mark.marginLeft),
    tickBarPadding: sides(bar, 'padding'),
    tick: text(part(slider.Tick, L, {})),
  };
}

/**
 * react-range's normalizeValue (allowOverlap false, as Base Web leaves it) on values a drag or a key gives: a thumb
 * held by its neighbours and the ends, and landing on a step. Its test of a neighbour is its truthiness, so a neighbour
 * at 0 holds nothing there, where Metered holds the thumb (docs/controls/Slider/): those cases are left out here and
 * tested on their own.
 */
function normalize() {
  const cases = [];
  const sets = [
    { min: 0, max: 100, step: 1, values: [[30], [20, 70]] },
    { min: 0, max: 100, step: 10, values: [[60], [20, 70]] },
    { min: -300, max: 300, step: 10, values: [[90], [-100, 200]] },
    { min: 1000, max: 8000, step: 100, values: [[4500]] },
    { min: 0, max: 1, step: 0.1, values: [[0.3], [0.2, 0.7]] },
    { min: 0, max: 10, step: 0.25, values: [[2.5]] },
    { min: 0, max: 95, step: 1, values: [[10, 70]] },
  ];
  for (const { min, max, step, values: list } of sets) {
    for (const values of list) {
      for (let index = 0; index < values.length; index++) {
        const neighbours = [values[index - 1], values[index + 1]];
        if (neighbours.includes(0)) continue;
        const span = max - min;
        const tries = [min - step, min, min + step * 0.4, min + step * 0.5, min + step * 0.6, min + span * 0.33, min + span / 2,
          min + span * 0.777, max - step * 0.5, max, max + step];
        for (const value of tries) {
          cases.push({ value, index, min, max, step, values, expected: normalizeValue(value, index, min, max, step, false, values) });
        }
      }
    }
  }
  return cases;
}

function oracle(version) {
  return { baseui: version, colours: colours(), layout: layout(), normalize: normalize() };
}

module.exports = { oracle };
