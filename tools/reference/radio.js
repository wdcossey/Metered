// The oracle for Radio (radio-v2): what Base Web's own style functions give, for the cases the tests build, in
// Avalonia's units. Kept apart from tools/tokens/radio.js, which writes the themes: the tests compare one with the
// other. Read by cases.js, which writes tests/Metered.Tests/Reference/radio.oracle.json.
'use strict';

const css = require('./css');
const { THEMES, evaluate, resolve } = require('./oracle');
const radio = require('baseui/radio-v2/styled-components');
const { LABEL_PLACEMENT, ALIGN } = require('baseui/radio-v2/constants');

/** radio.js's sharedProps, for a radio that has a label and a description. */
const props = (s) => ({
  $align: s.align ?? ALIGN.vertical, $checked: !!s.checked, $disabled: !!s.disabled, $hasDescription: true,
  $isActive: !!s.isActive, $error: !!s.error, $isFocused: !!s.isFocusVisible, $isFocusVisible: !!s.isFocusVisible,
  $isHovered: !!s.isHovered, $labelPlacement: s.labelPlacement ?? LABEL_PLACEMENT.right, $required: false, $value: '',
});

/** A part's declarations for the radio's props and the part's own CSS state ({ hover, active }), on a coarse pointer or not. */
function part(styled, theme, s, cssState = {}, coarse = false) {
  const style = evaluate(styled, theme, props(s));
  const { self } = resolve(style, cssState);
  if (coarse) Object.assign(self, style['@media (pointer: coarse)'] || {});
  return self;
}

/** 'inset 999px 999px 0px <colour>' to the colour, or null for none. */
function overlay(boxShadow) {
  if (boxShadow === 'none') return null;
  const m = /^inset 999px 999px 0px (.+)$/.exec(boxShadow);
  if (!m) throw new Error(`overlay ${boxShadow}`);
  return css.color(m[1]);
}

/** '2px solid <colour>' and its offset, or null for none. */
function outline(s) {
  if (s.outline === undefined) return null;
  const m = /^(\S+) solid (.+)$/.exec(s.outline);
  if (!m) throw new Error(`outline ${s.outline}`);
  return { colour: css.color(m[2]), thickness: css.px(m[1]), offset: css.px(s.outlineOffset) };
}

/**
 * The states the tests drive with real input: the props they give and the backplate's own CSS state.
 * hover: a mouse over the backplate (over the radio, so $isHovered too); hoverLabel: a mouse over the label only;
 * active: a mouse pressed on the backplate; touchActive: a finger pressed on it (no hover: Metered's, as Base Web's
 * backplate hover, is a mouse's); focusVisible: focused by keyboard in a group.
 */
const STATES = [
  ['rest', {}, {}],
  ['hover', { isHovered: true }, { hover: true }],
  ['hoverLabel', { isHovered: true }, {}],
  ['active', { isHovered: true, isActive: true }, { hover: true, active: true }],
  ['touchActive', { isActive: true }, { active: true }],
  ['focusVisible', { isFocusVisible: true }, {}],
];

function colours() {
  const cases = [];
  for (const theme of Object.keys(THEMES)) {
    for (const checked of [false, true]) {
      for (const error of [false, true]) {
        for (const disabled of [false, true]) {
          for (const [state, extra, cssState] of STATES) {
            // A disabled radio takes no input.
            if (disabled && state !== 'rest') continue;
            const s = { checked, error, disabled, ...extra };
            const t = THEMES[theme];
            const outer = part(radio.RadioMarkOuter, t, s);
            const inner = part(radio.RadioMarkInner, t, s);
            const backplate = part(radio.RadioBackplate, t, s, cssState);
            // box-sizing border-box: 100% is the outer box less its border on each side.
            const full = css.px(outer.width) - 2 * css.px(outer.borderWidth);
            cases.push({
              theme, checked, error, disabled, state,
              outerBackground: css.color(outer.backgroundColor),
              outerBorder: css.color(outer.borderColor),
              outline: outline(outer),
              innerBackground: css.color(inner.backgroundColor),
              innerOverlay: overlay(inner.boxShadow),
              innerSize: inner.width === '100%' ? full : css.px(inner.width),
              backplateBackground: backplate.backgroundColor === undefined ? '#00000000' : css.color(backplate.backgroundColor),
              label: css.color(part(radio.Label, t, s).color),
              description: css.color(part(radio.Description, t, s).color),
            });
          }
        }
      }
    }
  }
  return cases;
}

const lineClamp = (s) => (s.WebkitLineClamp === undefined ? 0 : s.WebkitLineClamp);
const text = (s) => ({ fontSize: css.px(s.fontSize), fontWeight: css.fontWeight(s.fontWeight), lineHeight: css.px(s.lineHeight), maxLines: lineClamp(s) });

function layout() {
  const cases = [];
  const L = THEMES.Light;
  for (const labelPlacement of Object.values(LABEL_PLACEMENT)) {
    for (const align of Object.values(ALIGN)) {
      for (const coarse of [false, true]) {
        const s = { labelPlacement, align };
        const root = part(radio.Root, L, s, {}, coarse);
        const backplate = part(radio.RadioBackplate, L, s);
        const outer = part(radio.RadioMarkOuter, L, s);
        const wrapper = part(radio.LabelWrapper, L, s);
        const px = (v) => (v === undefined ? null : css.px(v));
        const plate = (side) => css.px(backplate['padding' + side]);
        cases.push({
          labelPlacement, align, coarse,
          rootDirection: root.flexDirection,
          rootAlignItems: root.alignItems,
          rootColumnGap: px(root.columnGap),
          rootRowGap: px(root.rowGap),
          rootPadding: px(root.padding) ?? 0,
          rootCursor: root.cursor,
          backplatePadding: ['Left', 'Top', 'Right', 'Bottom'].map(plate),
          backplateRadius: css.px(backplate.borderRadius),
          backplateMin: [css.px(backplate.minWidth), css.px(backplate.minHeight)],
          // box-sizing border-box: the mark and the padding, or the minimum.
          backplateSize: [
            Math.max(css.px(backplate.minWidth), css.px(outer.width) + plate('Left') + plate('Right')),
            Math.max(css.px(backplate.minHeight), css.px(outer.height) + plate('Top') + plate('Bottom')),
          ],
          outerSize: css.px(outer.width),
          outerBorder: css.px(outer.borderWidth),
          wrapperPaddingTop: px(wrapper.paddingTop) ?? 0,
          wrapperRowGap: css.px(wrapper.rowGap),
          wrapperMaxWidth: px(wrapper.maxWidth),
          label: text(part(radio.Label, L, s)),
          description: text(part(radio.Description, L, s)),
        });
      }
    }
  }
  return cases;
}

function group() {
  const cases = [];
  for (const align of Object.values(ALIGN)) {
    for (const labelPlacement of Object.values(LABEL_PLACEMENT)) {
      const s = evaluate(radio.RadioGroupRoot, THEMES.Light, { $align: align, $labelPlacement: labelPlacement, $disabled: false, $error: false, $required: false });
      cases.push({
        align, labelPlacement,
        flexDirection: s.flexDirection, flexWrap: s.flexWrap, alignItems: s.alignItems,
        columnGap: css.px(s.columnGap), rowGap: css.px(s.rowGap),
      });
    }
  }
  return cases;
}

function transition() {
  const s = part(radio.RadioMarkInner, THEMES.Light, {});
  return {
    properties: s.transitionProperty.split(',').map((p) => p.trim()),
    seconds: css.seconds(s.transitionDuration),
    curve: css.cubicBezier(s.transitionTimingFunction),
  };
}

function oracle(version) {
  return { baseui: version, colours: colours(), layout: layout(), group: group(), transition: transition() };
}

module.exports = { oracle };
