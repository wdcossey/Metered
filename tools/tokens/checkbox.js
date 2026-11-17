// The themes of Checkbox's parts, Checkbox.Styled<Part>, generated from Base Web's checkbox-v2
// (baseui/checkbox-v2/styled-components) by the engine the v2 controls share (parts.js): each style function
// evaluated on a theme whose colours are their own names, for every combination of the checkbox's states, written as
// the fewest state styles and checked against Base Web's own. Nothing here is a port of Base Web's branches. The tests'
// oracle (tools/reference/checkbox.js) checks the result against the real colours.
'use strict';

const css = require('../reference/css');
const {
  L, NAMED, engine, colour, px, check, TEXT, font, flexRoot, backplate, outline, timing,
} = require('./parts');
const checkbox = require('baseui/checkbox-v2/styled-components');
const { LABEL_PLACEMENT } = require('baseui/checkbox-v2/constants');

/**
 * The checkbox's states, each with its values (the default first) and the pseudoclass that carries it. `hover` and
 * `active` are CSS's own on the part (only the CheckmarkContainer has them); the rest are Base Web's $ props, which the
 * Checkbox mirrors onto every part, and `coarse` is `@media (pointer: coarse)`. checkbox-v2's placements are right and
 * left only (constants.js: top and bottom are deprecated).
 */
const AXES = [
  { name: 'checked', values: [false, true], pseudo: () => ':checked' },
  { name: 'isIndeterminate', values: [false, true], pseudo: () => ':is-indeterminate' },
  { name: 'disabled', values: [false, true], pseudo: () => ':disabled' },
  { name: 'error', values: [false, true], pseudo: () => ':error' },
  { name: 'isHovered', values: [false, true], pseudo: () => ':is-hovered' },
  { name: 'isActive', values: [false, true], pseudo: () => ':is-active' },
  { name: 'isFocusVisible', values: [false, true], pseudo: () => ':focus-visible' },
  { name: 'labelPlacement', values: [LABEL_PLACEMENT.right, LABEL_PLACEMENT.left], pseudo: (v) => `:label-placement-${v}` },
  { name: 'coarse', values: [false, true], pseudo: () => ':pointer-coarse' },
  { name: 'hover', values: [false, true], pseudo: () => ':hover' },
  { name: 'active', values: [false, true], pseudo: () => ':active' },
];

/**
 * Base Web's $ props for a combination (checkbox.js's sharedProps, and $labelPlacement). Its $isFocused is the
 * input's focus, which a keyboard's focus has too; a mouse's focus changes nothing these functions draw.
 */
const props = (c) => ({
  $checked: c.checked, $isIndeterminate: c.isIndeterminate, $disabled: c.disabled, $error: c.error,
  $isHovered: c.isHovered, $isActive: c.isActive, $isFocusVisible: c.isFocusVisible, $isFocused: c.isFocusVisible,
  $labelPlacement: c.labelPlacement, $required: false, $value: '',
});

const { DEFAULT, declarations, theme } = engine({ axes: AXES, props, layered: true });

/** The SVG of a background-image data URL: its size, its viewBox, its one path and that path's fill. */
function glyph(backgroundImage) {
  if (backgroundImage == null) return null;
  const m = /^url\('data:image\/svg\+xml,(.*)'\);?$/s.exec(backgroundImage);
  if (!m) throw new Error(`Not an SVG data URL: ${backgroundImage}`);
  const svg = decodeURIComponent(m[1]);
  const w = Number(/<svg[^>]*\swidth="([\d.]+)"/.exec(svg)[1]);
  const h = Number(/<svg[^>]*\sheight="([\d.]+)"/.exec(svg)[1]);
  const box = /viewBox="([^"]+)"/.exec(svg)[1].split(/[\s,]+/).map(Number);
  const paths = [...svg.matchAll(/<path d="([^"]+)" fill="([^"]+)"\/>/g)];
  if (paths.length !== 1) throw new Error('Not one path');
  return { d: paths[0][1], width: w, height: h, viewBox: box, fill: paths[0][2] };
}

const SIDES = ['Left', 'Right', 'Top', 'Bottom'];
const RADII = ['borderTopLeftRadius', 'borderTopRightRadius', 'borderBottomRightRadius', 'borderBottomLeftRadius'];
/** The same value on every side: border{Side}{suffix}. */
function same(s, suffix, what) {
  const v = SIDES.map((side) => s[`border${side}${suffix}`]);
  if (new Set(v).size !== 1) throw new Error(`${what}: border*${suffix} differ: ${v}`);
  return v[0];
}

// The glyph's size, viewBox and fill, the same for the tick and the dash and in every state (checked here); a box
// with no glyph keeps them, so they don't vary by state.
const TICK = glyph(declarations(checkbox.Checkmark, NAMED, { ...DEFAULT, checked: true }).backgroundImage);

/** Each part: its Base Web styled component, its element type, and its declarations as Avalonia's properties. */
const PARTS = [
  {
    part: 'Root', type: 'b:RadioRootPanel', styled: checkbox.Root,
    // The label's own element: its padding takes the pointer, so the panel is drawn, transparent.
    fixed: { Background: 'Transparent' },
    // checkbox.js renders the label before the CheckmarkContainer for left, after it for right.
    convert: (s, c) => flexRoot(s, 'Root', c.labelPlacement === LABEL_PLACEMENT.left,
      ['flexDirection', 'display', 'verticalAlign', 'alignItems', 'cursor', 'userSelect', 'padding']),
  },
  {
    part: 'CheckmarkContainer', type: 'Border', styled: checkbox.CheckmarkContainer,
    convert: (s) => backplate(s, 'CheckmarkContainer', ['display', 'alignItems', 'justifyContent', 'boxSizing', 'minHeight',
      'minWidth', 'borderRadius', 'paddingTop', 'paddingBottom', 'paddingLeft', 'paddingRight', 'backgroundColor']),
  },
  {
    part: 'Checkmark', type: 'b:CheckboxMark', styled: checkbox.Checkmark,
    // Centred in the CheckmarkContainer: its alignItems and justifyContent.
    fixed: { HorizontalAlignment: 'Center', VerticalAlignment: 'Center' },
    convert(s) {
      check(s, 'Checkmark', ['flex', 'transitionDuration', 'transitionTimingFunction', 'transitionProperty', 'width', 'height',
        'boxSizing', ...SIDES.flatMap((side) => ['Style', 'Width', 'Color'].map((k) => `border${side}${k}`)), ...RADII,
        'outline', 'outlineOffset', 'display', 'verticalAlign', 'backgroundImage', 'backgroundColor', 'backgroundRepeat',
        'backgroundPosition']);
      if (s.boxSizing !== 'border-box' || same(s, 'Style', 'Checkmark') !== 'solid') throw new Error('Checkmark box');
      // The image at its own size (no background-size), centred, once.
      if (s.backgroundPosition !== 'center' || s.backgroundRepeat !== 'no-repeat') throw new Error('Checkmark background');
      const radius = new Set(RADII.map((k) => s[k]));
      if (radius.size !== 1) throw new Error('Checkmark radii differ');
      const g = glyph(s.backgroundImage);
      if (g && (g.width !== TICK.width || g.height !== TICK.height || String(g.viewBox) !== String(TICK.viewBox) || g.fill !== TICK.fill)) {
        throw new Error(`A glyph unlike the tick's size, viewBox or fill: ${JSON.stringify(g)}`);
      }
      return {
        Width: px(s.width),
        Height: px(s.height),
        BorderThickness: px(same(s, 'Width', 'Checkmark')),
        BorderBrush: colour(same(s, 'Color', 'Checkmark'), 'Checkmark border'),
        Background: colour(s.backgroundColor, 'Checkmark'),
        CornerRadius: px([...radius][0]),
        ...outline(s, 'Checkmark'),
        Glyph: g ? g.d : '{x:Null}',
        GlyphSize: `${TICK.width},${TICK.height}`,
        GlyphViewBox: TICK.viewBox.join(','),
        Foreground: colour(TICK.fill, 'Checkmark glyph'),
      };
    },
  },
  {
    // The content presenter, whose text takes its font, colour and wrapping.
    part: 'Label', type: 'ContentPresenter', styled: checkbox.Label,
    // A div's text wraps.
    fixed: { TextWrapping: 'Wrap' },
    convert(s) {
      check(s, 'Label', ['verticalAlign', 'paddingTop', 'paddingRight', 'paddingBottom', 'paddingLeft', 'color',
        'fontFamily', 'fontSize', 'fontWeight', 'lineHeight']);
      const n = TEXT(false);
      const [t, rt, b, l] = ['Top', 'Right', 'Bottom', 'Left'].map((side) => css.px(s['padding' + side] ?? 0));
      return { Padding: `${l},${t},${rt},${b}`, [n.Foreground]: colour(s.color, 'Label'), ...font(s, 'Label', n) };
    },
  },
];

/** The Checkmark's transition: its border and fill, timing200 on easeOutCurve, once the checkbox has loaded. */
function transition() {
  const s = declarations(checkbox.Checkmark, L, DEFAULT);
  // background-image is discrete in CSS: it changes at once, as the glyph does here.
  if (s.transitionProperty !== 'background-image, border-color, background-color') throw new Error(`transition ${s.transitionProperty}`);
  const { span, easing } = timing(s);
  const one = (prop) => `          <BrushTransition Property="${prop}" Duration="${span}" Easing="${easing}" />`;
  // CSS doesn't transition a page's first paint; Avalonia would. So only once the checkbox has loaded (:loaded).
  return `    <Style Selector="^:loaded">
      <Setter Property="Transitions">
        <Transitions>
${one('Background')}
${one('BorderBrush')}
        </Transitions>
      </Setter>
    </Style>`;
}

const DESCRIBE = {
  Root: 'the label element: the mark and the label in a row, the label first for the left placement; on a coarse pointer, padding for a 48 px target.',
  CheckmarkContainer: 'the round-cornered plate behind the mark, tinted under a hovering mouse and while pressed.',
  Checkmark: 'the 17 px box, its colour in its border and fill; the tick or the dash, Base Web\'s background image; the focus outline on keyboard focus.',
  Label: 'the checkbox\'s content, beside the mark.',
};

function file(version) {
  const themes = PARTS.map((p) => {
    const { plain, blocks } = theme(p);
    const extra = p.part === 'Checkmark' ? '\n' + transition() : '';
    return `  <!-- ${p.part}: ${DESCRIBE[p.part]} -->
  <ControlTheme x:Key="Checkbox.Styled${p.part}" TargetType="${p.type}">
${plain.join('\n')}${extra}
${blocks.join('\n')}
  </ControlTheme>`;
  });

  return `<!--
  Generated by tools/tokens/gen.js (tools/tokens/checkbox.js) from baseui ${version}
  (© Uber Technologies, Inc., MIT). Do not edit.
  The themes of Checkbox's parts, Checkbox.Styled<Part>, from Base Web's checkbox-v2: each part's look by
  the state pseudoclasses the Checkbox mirrors onto it (Metered.Controls.Checkbox), the colours by token,
  the lengths read from Base Web's own style functions.
-->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:b="https://github.com/wdcossey/Metered">
${themes.join('\n')}
</ResourceDictionary>
`;
}

module.exports = { file, glyph };
