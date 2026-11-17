// The oracle for Switch: what Base Web's own style functions give (baseui/switch/styled-components), for the cases the
// tests build, in Avalonia's units. Kept apart from tools/tokens/switch.js, which writes the themes: the tests compare
// one with the other. Read by cases.js, which writes tests/Metered.Tests/Reference/switch.oracle.json.
'use strict';

const css = require('./css');
const { THEMES, evaluate } = require('./oracle');
const sw = require('baseui/switch/styled-components');
const { SIZE, LABEL_PLACEMENT } = require('baseui/switch/constants');

/** switch.js's sharedProps (and the root's and label's $labelPlacement). */
const props = (s) => ({
  $isFocused: !!s.isFocusVisible, $isFocusVisible: !!s.isFocusVisible, $isHovered: !!s.isHovered, $isActive: !!s.isActive,
  $checked: !!s.checked, $required: false, $disabled: !!s.disabled, $value: '', $showIcon: !!s.showIcon,
  $size: s.size ?? SIZE.default, $labelPlacement: s.labelPlacement ?? LABEL_PLACEMENT.right,
});

/** A part's declarations, on a coarse pointer or not. */
function part(styled, theme, s, coarse = false) {
  const style = evaluate(styled, theme, props(s));
  const self = {};
  for (const [k, v] of Object.entries(style)) if (v !== null && typeof v !== 'object' && v !== undefined) self[k] = v;
  if (coarse) Object.assign(self, style['@media (pointer: coarse)'] || {});
  return self;
}

/** 'inset 0 0 0 999px <colour>' to the colour, or null for none. */
function overlay(boxShadow) {
  if (boxShadow === 'none') return null;
  const m = /^inset 0 0 0 999px (.+)$/.exec(boxShadow);
  if (!m) throw new Error(`overlay ${boxShadow}`);
  return css.color(m[1]);
}

/** '2px solid <colour>' and its offset, or null for none. */
function outline(s) {
  if (s.outline === 'none') return null;
  const m = /^(\S+) solid (.+)$/.exec(s.outline);
  if (!m) throw new Error(`outline ${s.outline}`);
  return { colour: css.color(m[2]), thickness: css.px(m[1]), offset: css.px(s.outlineOffset) };
}

/** The checkmark in the toggle's background image: its colour and drawn size, or null for none. */
function icon(s) {
  if (s.backgroundImage === undefined) return null;
  const svg = decodeURIComponent(/^url\('data:image\/svg\+xml,(.*)'\);?$/.exec(s.backgroundImage)[1]);
  return {
    colour: css.color(/ fill="([^"]+)"\/>/.exec(svg)[1]),
    size: Number(/<svg width="(\d+)"/.exec(svg)[1]),
    path: /<path d="([^"]+)"/.exec(svg)[1],
  };
}

/**
 * The states the tests drive with real input: rest; hover, a mouse over the switch; touch, a finger pressed on it,
 * which doesn't hover (Metered's hover is a mouse's); focusVisible, focused by keyboard.
 */
const STATES = [
  ['rest', {}],
  ['hover', { isHovered: true }],
  ['touch', { isActive: true }],
  ['focusVisible', { isFocusVisible: true }],
];

function colours() {
  const cases = [];
  for (const theme of Object.keys(THEMES)) {
    for (const checked of [false, true]) {
      for (const disabled of [false, true]) {
        for (const showIcon of [false, true]) {
          for (const [state, extra] of STATES) {
            // A disabled switch takes no input (Avalonia doesn't hit-test it, nor give it the focus).
            if (disabled && state !== 'rest') continue;
            const s = { checked, disabled, showIcon, ...extra };
            const t = THEMES[theme];
            const track = part(sw.ToggleTrack, t, s);
            const toggle = part(sw.Toggle, t, s);
            cases.push({
              theme, checked, disabled, showIcon, state,
              trackBackground: css.color(track.backgroundColor),
              trackBorder: css.color(track.borderColor),
              trackBorderWidth: track.borderStyle === 'none' ? 0 : css.px(track.borderWidth),
              trackOverlay: overlay(track.boxShadow),
              outline: outline(track),
              toggleBackground: css.color(toggle.backgroundColor),
              toggleOverlay: overlay(toggle.boxShadow),
              icon: icon(toggle),
              label: css.color(part(sw.Label, t, s).color),
            });
          }
        }
      }
    }
  }
  return cases;
}

const text = (s) => ({ fontSize: css.px(s.fontSize), fontWeight: css.fontWeight(s.fontWeight), lineHeight: css.px(s.lineHeight) });

function layout() {
  const cases = [];
  const L = THEMES.Light;
  for (const size of Object.values(SIZE)) {
    for (const labelPlacement of Object.values(LABEL_PLACEMENT)) {
      for (const coarse of [false, true]) {
        for (const checked of [false, true]) {
          for (const disabled of [false, true]) {
            const s = { size, labelPlacement, checked, disabled };
            const root = part(sw.Root, L, s, coarse);
            const track = part(sw.ToggleTrack, L, s);
            const toggle = part(sw.Toggle, L, s);
            const label = part(sw.Label, L, s);
            const px = (v) => (v === undefined ? 0 : css.px(v));
            const translate = toggle.transform === undefined ? 0 : css.px(/^translateX\((.+)\)$/.exec(toggle.transform)[1]);
            cases.push({
              size, labelPlacement, coarse, checked, disabled,
              rootDirection: root.flexDirection,
              rootAlignItems: root.alignItems,
              rootPadding: px(root.padding),
              track: [css.px(track.width), css.px(track.height)],
              trackBorder: track.borderStyle === 'none' ? 0 : css.px(track.borderWidth),
              trackPadding: ['Left', 'Top', 'Right', 'Bottom'].map((side) => css.px(track['padding' + side])),
              trackRadius: css.px(track.borderTopLeftRadius),
              trackAlignItems: track.alignItems,
              toggle: css.px(toggle.width),
              toggleTranslate: translate,
              labelPadding: ['Left', 'Top', 'Right'].map((side) => px(label['padding' + side])),
              label: text(label),
            });
          }
        }
      }
    }
  }
  return cases;
}

function transition() {
  const s = part(sw.Toggle, THEMES.Light, {});
  const m = /^(\S+) (\S+) (cubic-bezier\(.*\))$/.exec(s.transition);
  return { property: m[1], seconds: css.seconds(m[2]), curve: css.cubicBezier(m[3]) };
}

function oracle(version) {
  return { baseui: version, colours: colours(), layout: layout(), transition: transition() };
}

module.exports = { oracle };
