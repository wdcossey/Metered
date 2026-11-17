// The Checkbox's oracle, checkbox.oracle.json (written by cases.js): what Base Web's own style
// functions for its v1 Checkbox (baseui/checkbox/styled-components) give for each case the tests
// build, on the real light and dark themes, in Avalonia's units.
'use strict';

const css = require('./css');
const { THEMES, evaluate } = require('./oracle');
const checkbox = require('baseui/checkbox/styled-components');

/**
 * The oracle's own reading of a background-image data URL, apart from the generator's: the SVG's
 * width, height and viewBox, and its one path's d and fill. Null for no image.
 */
function glyph(backgroundImage) {
  if (!backgroundImage) return null;
  const at = backgroundImage.indexOf('data:image/svg+xml,');
  if (at < 0) throw new Error(`Not an SVG data URL: ${backgroundImage}`);
  const svg = decodeURIComponent(backgroundImage.slice(at + 'data:image/svg+xml,'.length).replace(/'\);?\s*$/, ''));
  const attr = (tag, name) => {
    const m = new RegExp(String.raw`<${tag}\b[^>]*\s${name}="([^"]*)"`).exec(svg);
    if (!m) throw new Error(`No ${name} on <${tag}>`);
    return m[1];
  };
  const width = Number(attr('svg', 'width'));
  const height = Number(attr('svg', 'height'));
  if (attr('svg', 'viewBox') !== `0 0 ${width} ${height}`) throw new Error("A viewBox that isn't the SVG's size");
  if ((svg.match(/<path\b/g) || []).length !== 1) throw new Error('Not one path');
  return { d: attr('path', 'd'), width, height, fill: attr('path', 'fill') };
}

// Interaction states the tests make with real input: [name, $ props]. A mouse press is over the box.
const STATES = [
  ['rest', {}],
  ['hover', { $isHovered: true }],
  ['active', { $isHovered: true, $isActive: true }],
  ['focusVisible', { $isFocusVisible: true, $isFocused: true }],
];
const PLACEMENTS = ['top', 'right', 'bottom', 'left'];

const props = (extra) => ({
  $checked: false, $isIndeterminate: false, $error: false, $disabled: false, $isHovered: false, $isActive: false,
  $isFocusVisible: false, $isFocused: false, $required: false, $value: '', $labelPlacement: 'right', ...extra,
});
const style = (styled, theme, extra) => evaluate(styled, THEMES[theme], props(extra));
const colourOrNull = (v) => (v === undefined || v === null ? null : css.color(v));

/** Which glyph an SVG is, by its path; and its fill. */
function glyphOf(backgroundImage, shapes) {
  const g = glyph(backgroundImage);
  if (!g) return { glyph: null, tick: null };
  const name = Object.keys(shapes).find((k) => shapes[k].d === g.d);
  if (!name) throw new Error(`An unknown glyph ${g.d}`);
  return { glyph: name, tick: css.color(g.fill) };
}

/** A state's own cases: a disabled box can't be hovered, pressed or focused, so only at rest. */
function* cases(withIndeterminate) {
  for (const theme of Object.keys(THEMES)) {
    for (const checked of [false, true]) {
      for (const indeterminate of withIndeterminate ? [false, true] : [false]) {
        for (const error of [false, true]) {
          for (const disabled of [false, true]) {
            for (const [state, extra] of disabled ? [STATES[0]] : STATES) {
              yield { theme, checked, indeterminate, error, disabled, state, extra };
            }
          }
        }
      }
    }
  }
}

function oracle(version) {
  const shapes = {
    check: glyph(style(checkbox.Checkmark, 'Light', { $checked: true }).backgroundImage),
    indeterminate: glyph(style(checkbox.Checkmark, 'Light', { $isIndeterminate: true }).backgroundImage),
  };
  for (const s of Object.values(shapes)) delete s.fill;

  const checkmark = [];
  for (const c of cases(true)) {
    const p = { $checked: c.checked, $isIndeterminate: c.indeterminate, $error: c.error, $disabled: c.disabled, ...c.extra };
    const s = style(checkbox.Checkmark, c.theme, p);
    const outline = /^(\d+)px solid (.+)$/.exec(s.outline);
    if (s.outline !== 'none' && !outline) throw new Error(`outline ${s.outline}`);
    checkmark.push({
      theme: c.theme, checked: c.checked, indeterminate: c.indeterminate, error: c.error, disabled: c.disabled, state: c.state,
      background: css.color(s.backgroundColor),
      border: css.color(s.borderLeftColor),
      ...glyphOf(s.backgroundImage, shapes),
      outlineWidth: outline ? Number(outline[1]) : 0,
      outline: outline ? css.color(outline[2]) : null,
      label: css.color(style(checkbox.Label, c.theme, p).color),
    });
  }

  const toggle = [];
  for (const c of cases(false)) {
    const p = { $checked: c.checked, $error: c.error, $disabled: c.disabled, ...c.extra };
    const s = style(checkbox.Toggle, c.theme, p);
    const width = css.px(s.width);
    const pct = s.transform ? Number(/^translateX\((-?[\d.]+)%\)$/.exec(s.transform)[1]) : 0;
    toggle.push({
      theme: c.theme, checked: c.checked, error: c.error, disabled: c.disabled, state: c.state,
      background: css.color(s.backgroundColor),
      boxShadow: css.boxShadows(s.boxShadow),
      translateX: (pct / 100) * width,
      track: css.color(style(checkbox.ToggleTrack, c.theme, p).backgroundColor),
      label: css.color(style(checkbox.Label, c.theme, p).color),
    });
  }

  const sides = (s, prefix) => ['Left', 'Top', 'Right', 'Bottom'].map((side) => css.px(s[prefix + side] ?? 0));
  const layout = PLACEMENTS.map((placement) => {
    const root = style(checkbox.Root, 'Light', { $labelPlacement: placement });
    const label = style(checkbox.Label, 'Light', { $labelPlacement: placement });
    return {
      placement,
      direction: root.flexDirection,
      alignItems: root.alignItems,
      labelPadding: sides(label, 'padding'),
    };
  });

  const mark = style(checkbox.Checkmark, 'Light', {});
  const track = style(checkbox.ToggleTrack, 'Light', {});
  const knob = style(checkbox.Toggle, 'Light', {});
  const label = style(checkbox.Label, 'Light', {});
  return {
    baseui: version,
    checkmark,
    toggle,
    layout,
    // checkbox.js: labelPlacement = checkmarkType === toggle ? 'left' : 'right'.
    defaultPlacement: { default: 'right', toggle: 'left' },
    sizes: {
      checkmark: { width: css.px(mark.width), height: css.px(mark.height), margin: sides(mark, 'margin'), borderWidth: css.px(mark.borderLeftWidth), radius: css.px(mark.borderTopLeftRadius) },
      track: { width: css.px(track.width), height: css.px(track.height), margin: sides(track, 'margin'), radius: css.px(track.borderTopLeftRadius) },
      toggle: { width: css.px(knob.width), height: css.px(knob.height), radius: knob.borderTopLeftRadius === '50%' ? css.px(knob.width) / 2 : css.px(knob.borderTopLeftRadius) },
      label: { fontSize: css.px(label.fontSize), fontWeight: css.fontWeight(label.fontWeight), lineHeight: css.px(label.lineHeight) },
    },
    glyphs: shapes,
  };
}

module.exports = { oracle };
