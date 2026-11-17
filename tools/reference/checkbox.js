// The oracle for Checkbox (checkbox-v2): what Base Web's own style functions give, for the cases the tests build, on
// the real light and dark themes, in Avalonia's units. Kept apart from tools/tokens/checkbox.js, which writes the
// themes: the tests compare one with the other. Read by cases.js, which writes
// tests/Metered.Tests/Reference/checkbox.oracle.json.
'use strict';

const css = require('./css');
const { THEMES, evaluate, resolve } = require('./oracle');
const checkbox = require('baseui/checkbox-v2/styled-components');
const { LABEL_PLACEMENT } = require('baseui/checkbox-v2/constants');

/** checkbox.js's sharedProps, and $labelPlacement. */
const props = (s) => ({
  $checked: !!s.checked, $isIndeterminate: !!s.indeterminate, $disabled: !!s.disabled, $error: !!s.error,
  $isHovered: !!s.isHovered, $isActive: !!s.isActive, $isFocusVisible: !!s.isFocusVisible, $isFocused: !!s.isFocusVisible,
  $labelPlacement: s.labelPlacement ?? LABEL_PLACEMENT.right, $required: false, $value: '',
});

/** A part's declarations for the checkbox's props and the part's own CSS state ({ hover, active }), on a coarse pointer or not. */
function part(styled, theme, s, cssState = {}, coarse = false) {
  const style = evaluate(styled, theme, props(s));
  const { self } = resolve(style, cssState);
  if (coarse) Object.assign(self, style['@media (pointer: coarse)'] || {});
  return self;
}

/**
 * The oracle's own reading of a background-image data URL, apart from the generator's: the SVG's width, height and
 * viewBox, and its one path's d and fill. Null for no image.
 */
function glyph(backgroundImage) {
  if (backgroundImage == null) return null;
  const at = backgroundImage.indexOf('data:image/svg+xml,');
  if (at < 0) throw new Error(`Not an SVG data URL: ${backgroundImage}`);
  const svg = decodeURIComponent(backgroundImage.slice(at + 'data:image/svg+xml,'.length).replace(/'\);?\s*$/, ''));
  const attr = (tag, name) => {
    const m = new RegExp(String.raw`<${tag}\b[^>]*\s${name}="([^"]*)"`).exec(svg);
    if (!m) throw new Error(`No ${name} on <${tag}>`);
    return m[1];
  };
  if ((svg.match(/<path\b/g) || []).length !== 1) throw new Error('Not one path');
  return {
    d: attr('path', 'd'), width: Number(attr('svg', 'width')), height: Number(attr('svg', 'height')),
    viewBox: attr('svg', 'viewBox').split(/[\s,]+/).map(Number), fill: attr('path', 'fill'),
  };
}

/** '2px solid <colour>' and its offset, or null for none. */
function outline(s) {
  if (s.outline === undefined) return null;
  const m = /^(\S+) solid (.+)$/.exec(s.outline);
  if (!m) throw new Error(`outline ${s.outline}`);
  return { colour: css.color(m[2]), thickness: css.px(m[1]), offset: css.px(s.outlineOffset) };
}

/**
 * The states the tests drive with real input: the props they give and the CheckmarkContainer's own CSS state.
 * hover: a mouse over the container (over the checkbox, so $isHovered too); hoverLabel: a mouse over the label only;
 * active: a mouse pressed on the container; touchActive: a finger pressed on it (no hover: Metered's, as Base Web's
 * container hover, is a mouse's); focusVisible: focused by keyboard; keyActive: Space held with keyboard focus
 * (checkbox.js's onKeyDown sets isActive; the container, the input's sibling, isn't :active).
 */
const STATES = [
  ['rest', {}, {}],
  ['hover', { isHovered: true }, { hover: true }],
  ['hoverLabel', { isHovered: true }, {}],
  ['active', { isHovered: true, isActive: true }, { hover: true, active: true }],
  ['touchActive', { isActive: true }, { active: true }],
  ['focusVisible', { isFocusVisible: true }, {}],
  ['keyActive', { isFocusVisible: true, isActive: true }, {}],
];

function colours(shapes) {
  const cases = [];
  for (const theme of Object.keys(THEMES)) {
    for (const checked of [false, true]) {
      for (const indeterminate of [false, true]) {
        for (const error of [false, true]) {
          for (const disabled of [false, true]) {
            for (const [state, extra, cssState] of STATES) {
              // A disabled checkbox takes no input.
              if (disabled && state !== 'rest') continue;
              const s = { checked, indeterminate, error, disabled, ...extra };
              const t = THEMES[theme];
              const mark = part(checkbox.Checkmark, t, s);
              const container = part(checkbox.CheckmarkContainer, t, s, cssState);
              const g = glyph(mark.backgroundImage);
              const name = g && Object.keys(shapes).find((k) => shapes[k].d === g.d);
              if (g && !name) throw new Error(`An unknown glyph ${g.d}`);
              const border = new Set(['Left', 'Right', 'Top', 'Bottom'].map((side) => mark[`border${side}Color`]));
              if (border.size !== 1) throw new Error('Checkmark border colours differ');
              cases.push({
                theme, checked, indeterminate, error, disabled, state,
                background: css.color(mark.backgroundColor),
                border: css.color([...border][0]),
                glyph: name ?? null,
                tick: g ? css.color(g.fill) : null,
                outline: outline(mark),
                container: container.backgroundColor === undefined ? '#00000000' : css.color(container.backgroundColor),
                label: css.color(part(checkbox.Label, t, s).color),
              });
            }
          }
        }
      }
    }
  }
  return cases;
}

function layout() {
  const cases = [];
  const L = THEMES.Light;
  for (const labelPlacement of Object.values(LABEL_PLACEMENT)) {
    for (const coarse of [false, true]) {
      const s = { labelPlacement };
      const root = part(checkbox.Root, L, s, {}, coarse);
      const container = part(checkbox.CheckmarkContainer, L, s);
      const mark = part(checkbox.Checkmark, L, s);
      const label = part(checkbox.Label, L, s);
      const plate = (side) => css.px(container['padding' + side]);
      cases.push({
        labelPlacement, coarse,
        rootDirection: root.flexDirection,
        rootAlignItems: root.alignItems,
        rootPadding: root.padding === undefined ? 0 : css.px(root.padding),
        rootCursor: root.cursor,
        containerPadding: ['Left', 'Top', 'Right', 'Bottom'].map(plate),
        containerRadius: css.px(container.borderRadius),
        containerMin: [css.px(container.minWidth), css.px(container.minHeight)],
        // box-sizing border-box: the mark and the padding, or the minimum.
        containerSize: [
          Math.max(css.px(container.minWidth), css.px(mark.width) + plate('Left') + plate('Right')),
          Math.max(css.px(container.minHeight), css.px(mark.height) + plate('Top') + plate('Bottom')),
        ],
        markSize: [css.px(mark.width), css.px(mark.height)],
        markBorder: css.px(mark.borderLeftWidth),
        markRadius: css.px(mark.borderTopLeftRadius),
        labelPadding: ['Left', 'Top', 'Right', 'Bottom'].map((side) => css.px(label['padding' + side] ?? 0)),
        label: { fontSize: css.px(label.fontSize), fontWeight: css.fontWeight(label.fontWeight), lineHeight: css.px(label.lineHeight) },
      });
    }
  }
  return cases;
}

function transition() {
  const s = part(checkbox.Checkmark, THEMES.Light, {});
  return {
    properties: s.transitionProperty.split(',').map((p) => p.trim()),
    seconds: css.seconds(s.transitionDuration),
    curve: css.cubicBezier(s.transitionTimingFunction),
  };
}

function oracle(version) {
  const shapes = {
    check: glyph(part(checkbox.Checkmark, THEMES.Light, { checked: true }).backgroundImage),
    indeterminate: glyph(part(checkbox.Checkmark, THEMES.Light, { indeterminate: true }).backgroundImage),
  };
  for (const s of Object.values(shapes)) delete s.fill;
  return { baseui: version, colours: colours(shapes), layout: layout(), glyphs: shapes, transition: transition() };
}

module.exports = { oracle };
