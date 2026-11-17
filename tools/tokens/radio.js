// The themes of Radio's parts, Radio.Styled<Part>, and RadioGroup.StyledRadioGroupRoot, generated
// from Base Web's radio-v2 (baseui/radio-v2/styled-components), by the engine the v2 controls share (parts.js):
// every combination of the radio's states, the fewest state styles, checked against Base Web's own.
'use strict';

const css = require('../reference/css');
const { evaluate } = require('../reference/oracle');
const {
  L, NAMED, engine, colour, px, cursor, check, TEXT, font, flexRoot, backplate, outline, timing,
} = require('./parts');
const radio = require('baseui/radio-v2/styled-components');
const { LABEL_PLACEMENT, ALIGN } = require('baseui/radio-v2/constants');

/**
 * The radio's states, each with its values (the default first) and the pseudoclass that carries it. `hover` and
 * `active` are CSS's own on the part (only the backplate has them); the rest are Base Web's $ props, which the Radio
 * mirrors onto every part, and `coarse` is `@media (pointer: coarse)`.
 */
const AXES = [
  { name: 'checked', values: [false, true], pseudo: () => ':checked' },
  { name: 'disabled', values: [false, true], pseudo: () => ':disabled' },
  { name: 'error', values: [false, true], pseudo: () => ':error' },
  { name: 'isHovered', values: [false, true], pseudo: () => ':is-hovered' },
  { name: 'isActive', values: [false, true], pseudo: () => ':is-active' },
  { name: 'isFocusVisible', values: [false, true], pseudo: () => ':focus-visible' },
  { name: 'align', values: [ALIGN.vertical, ALIGN.horizontal], pseudo: (v) => `:align-${v}` },
  { name: 'labelPlacement', values: [LABEL_PLACEMENT.right, LABEL_PLACEMENT.top, LABEL_PLACEMENT.bottom, LABEL_PLACEMENT.left], pseudo: (v) => `:label-placement-${v}` },
  { name: 'coarse', values: [false, true], pseudo: () => ':pointer-coarse' },
  { name: 'hover', values: [false, true], pseudo: () => ':hover' },
  { name: 'active', values: [false, true], pseudo: () => ':active' },
];

/** Base Web's $ props for a combination (radio.js's sharedProps). */
const props = (c) => ({
  $align: c.align, $checked: c.checked, $disabled: c.disabled, $hasDescription: true, $isActive: c.isActive,
  $error: c.error, $isFocused: c.isFocusVisible, $isFocusVisible: c.isFocusVisible, $isHovered: c.isHovered,
  $labelPlacement: c.labelPlacement, $required: false, $value: '',
});

const { DEFAULT, declarations, theme } = engine({ axes: AXES, props });

const RADII = ['borderTopLeftRadius', 'borderTopRightRadius', 'borderBottomRightRadius', 'borderBottomLeftRadius'];
function half(s, what) {
  for (const k of RADII) if (s[k] !== '50%') throw new Error(`${what}: ${k} ${s[k]}`);
}
/** Base Web's max3LinesStyle: three lines, then an ellipsis. */
function clamp(s, what, n) {
  if (s.WebkitLineClamp === undefined) return { [n.MaxLines]: '0', [n.TextTrimming]: 'None' };
  if (s.overflow !== 'hidden' || s.textOverflow !== 'ellipsis' || s.display !== '-webkit-box' || s.WebkitBoxOrient !== 'vertical') {
    throw new Error(`${what}: a line clamp this generator doesn't read`);
  }
  return { [n.MaxLines]: String(s.WebkitLineClamp), [n.TextTrimming]: 'CharacterEllipsis' };
}
const CLAMP = ['overflow', 'textOverflow', 'display', 'WebkitLineClamp', 'WebkitBoxOrient'];

const outerOf = (c) => declarations(radio.RadioMarkOuter, NAMED, c);

/** Each part: its Base Web styled component, its element type, and its declarations as Avalonia's properties. */
const PARTS = [
  {
    part: 'Root', type: 'b:RadioRootPanel', styled: radio.Root,
    // The label's own element: its padding takes the pointer, so the panel is drawn, transparent.
    fixed: { Background: 'Transparent' },
    // radio.js renders the label wrapper before the backplate for top and left, after it for bottom and right.
    convert: (s, c) => flexRoot(s, 'Root', c.labelPlacement === LABEL_PLACEMENT.top || c.labelPlacement === LABEL_PLACEMENT.left,
      ['flexDirection', 'display', 'alignItems', 'cursor', 'columnGap', 'rowGap', 'padding']),
  },
  {
    part: 'RadioBackplate', type: 'Border', styled: radio.RadioBackplate,
    convert: (s) => backplate(s, 'RadioBackplate', ['position', 'display', 'alignItems', 'justifyContent', 'boxSizing', 'borderRadius',
      'paddingTop', 'paddingBottom', 'paddingLeft', 'paddingRight', 'minHeight', 'minWidth', 'backgroundColor']),
  },
  {
    part: 'RadioMarkOuter', type: 'b:RadioMark', styled: radio.RadioMarkOuter,
    // Centred in the backplate: its alignItems and justifyContent.
    fixed: { HorizontalAlignment: 'Center', VerticalAlignment: 'Center' },
    convert(s) {
      check(s, 'RadioMarkOuter', ['display', 'height', 'width', 'alignItems', 'justifyContent', 'flexShrink', 'boxSizing',
        'borderWidth', 'borderStyle', 'borderColor', 'backgroundColor', ...RADII, 'verticalAlign', 'outline', 'outlineOffset']);
      if (s.boxSizing !== 'border-box' || s.borderStyle !== 'solid' || s.alignItems !== 'center' || s.justifyContent !== 'center') throw new Error('RadioMarkOuter box');
      half(s, 'RadioMarkOuter');
      return {
        Width: px(s.width),
        Height: px(s.height),
        BorderThickness: px(s.borderWidth),
        BorderBrush: colour(s.borderColor, 'RadioMarkOuter border'),
        Background: colour(s.backgroundColor, 'RadioMarkOuter'),
        CornerRadius: String(css.px(s.width) / 2),
        ...outline(s, 'RadioMarkOuter'),
      };
    },
  },
  {
    part: 'RadioMarkInner', type: 'b:RadioMark', styled: radio.RadioMarkInner,
    // Centred in the outer mark: its alignItems and justifyContent.
    fixed: { HorizontalAlignment: 'Center', VerticalAlignment: 'Center' },
    convert(s, c) {
      check(s, 'RadioMarkInner', [...RADII, 'height', 'width', 'backgroundColor', 'boxShadow', 'transitionProperty', 'transitionDuration', 'transitionTimingFunction']);
      half(s, 'RadioMarkInner');
      // 100% of the outer mark's box inside its border (box-sizing border-box).
      const o = outerOf(c);
      const full = css.px(o.width) - 2 * css.px(o.borderWidth);
      if (css.px(o.height) !== css.px(o.width)) throw new Error('RadioMarkOuter isn\'t square');
      const size = (v) => (v === '100%' ? String(full) : px(v));
      let overlay = '{x:Null}';
      if (s.boxShadow !== 'none') {
        // Base Web's overlay: inset 999px 999px 0px <colour>, which fills the box over its background.
        const m = /^inset 999px 999px 0px (@\w+)$/.exec(s.boxShadow);
        if (!m) throw new Error(`RadioMarkInner boxShadow ${s.boxShadow}`);
        overlay = colour(m[1], 'overlay');
      }
      return {
        Width: size(s.width),
        Height: size(s.height),
        Background: colour(s.backgroundColor, 'RadioMarkInner'),
        // 50%: a radius of half the largest size, which a smaller disc clamps to its own half.
        CornerRadius: String(full / 2),
        Overlay: overlay,
      };
    },
  },
  {
    part: 'LabelWrapper', type: 'StackPanel', styled: radio.LabelWrapper,
    convert(s) {
      check(s, 'LabelWrapper', ['display', 'flexDirection', 'alignItems', 'paddingTop', 'rowGap', 'maxWidth']);
      if (s.display !== 'flex' || s.flexDirection !== 'column' || s.alignItems !== 'flex-start') throw new Error('LabelWrapper box');
      // Its top padding as a margin: a StackPanel has no padding, and nothing is drawn in it.
      return {
        Orientation: 'Vertical',
        Spacing: px(s.rowGap),
        Margin: s.paddingTop === undefined ? '0' : `0,${px(s.paddingTop)},0,0`,
        MaxWidth: s.maxWidth === undefined ? 'Infinity' : px(s.maxWidth),
      };
    },
  },
  {
    // A box around the content presenter, whose text takes the box's font, colour, wrapping and clamp.
    part: 'Label', type: 'Border', styled: radio.Label,
    // In the label wrapper's column at the start (its alignItems flex-start), wrapping as a div's text does.
    fixed: { HorizontalAlignment: 'Left', [TEXT(true).TextWrapping]: 'Wrap' },
    convert(s) {
      check(s, 'Label', ['verticalAlign', 'color', 'fontFamily', 'fontSize', 'fontWeight', 'lineHeight', ...CLAMP]);
      const n = TEXT(true);
      return { [n.Foreground]: colour(s.color, 'Label'), ...font(s, 'Label', n), ...clamp(s, 'Label', n) };
    },
  },
  {
    part: 'Description', type: 'TextBlock', styled: radio.Description,
    fixed: { HorizontalAlignment: 'Left', TextWrapping: 'Wrap' },
    convert(s) {
      check(s, 'Description', ['fontFamily', 'fontSize', 'fontWeight', 'lineHeight', 'color', 'cursor', ...CLAMP]);
      const n = TEXT(false);
      return { [n.Foreground]: colour(s.color, 'Description'), Cursor: cursor(s.cursor), ...font(s, 'Description', n), ...clamp(s, 'Description', n) };
    },
  },
];

/** The inner mark's transition: width and height, timing200 on easeOutCurve, once the radio has loaded. */
function transition() {
  const s = declarations(radio.RadioMarkInner, L, DEFAULT);
  if (s.transitionProperty !== 'width, height') throw new Error(`transition ${s.transitionProperty}`);
  const { span, easing } = timing(s);
  const one = (prop) => `          <DoubleTransition Property="${prop}" Duration="${span}" Easing="${easing}" />`;
  // CSS doesn't transition a page's first paint; Avalonia would. So only once the radio has loaded (:loaded).
  return `    <Style Selector="^:loaded">
      <Setter Property="Transitions">
        <Transitions>
${one('Width')}
${one('Height')}
        </Transitions>
      </Setter>
    </Style>`;
}

const DESCRIBE = {
  Root: 'the label element: a row or a column by label placement, the label wrapper before the backplate for top and left; on a coarse pointer, padding for a 48 px target.',
  RadioBackplate: 'the round-cornered plate behind the mark, tinted under a hovering mouse and while pressed.',
  RadioMarkOuter: 'the 17 px disc, its colour in its border and fill; the focus outline when checked and focused by keyboard.',
  RadioMarkInner: 'the disc inside: the whole inside unchecked, a 5 px dot checked; the hover and press overlay.',
  LabelWrapper: 'the label and the description, in a column.',
  Label: 'the radio\'s content.',
  Description: 'the description under the label.',
};

function file(version) {
  const themes = PARTS.map((p) => {
    const { plain, blocks } = theme(p);
    const extra = p.part === 'RadioMarkInner' ? '\n' + transition() : '';
    return `  <!-- ${p.part}: ${DESCRIBE[p.part]} -->
  <ControlTheme x:Key="Radio.Styled${p.part}" TargetType="${p.type}">
${plain.join('\n')}${extra}
${blocks.join('\n')}
  </ControlTheme>`;
  });

  // RadioGroupRoot: a flex box whose direction, wrap and gaps are the group's items panel (RadioGroup.axaml), tested
  // against Base Web's; nothing else is set on the element.
  const g = evaluate(radio.RadioGroupRoot, L, { $align: ALIGN.vertical, $labelPlacement: LABEL_PLACEMENT.right });
  check(g, 'RadioGroupRoot', ['display', 'flexWrap', 'flexDirection', 'alignItems', 'columnGap', 'rowGap']);

  return `<!--
  Generated by tools/tokens/gen.js (tools/tokens/radio.js) from baseui ${version}
  (© Uber Technologies, Inc., MIT). Do not edit.
  The themes of Radio's parts, Radio.Styled<Part>, from Base Web's radio-v2: each part's look by the
  state pseudoclasses the Radio mirrors onto it (Metered.Controls.Radio), the colours by token, the
  lengths read from Base Web's own style functions.
-->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:b="https://github.com/wdcossey/Metered">
${themes.join('\n')}
  <!-- RadioGroupRoot: the group's box. Its direction, wrap and gaps are the group's items panel (RadioGroup.axaml). -->
  <ControlTheme x:Key="RadioGroup.StyledRadioGroupRoot" TargetType="Border" />
</ResourceDictionary>
`;
}

module.exports = { file, AXES, PARTS, declarations, NAMED };
