// The themes of Textarea's parts, Textarea.Styled<Part>, generated from Base Web's textarea (baseui/textarea/
// styled-components) and the parts of Input's BaseInput it renders, with Input's generator (input.js) and the theme writer
// the other controls share (radio.js): each style function evaluated on a theme whose colours are their own names, for
// every combination of the textarea's states, written as the fewest state styles and checked against Base Web's own.
//
// textarea.js renders StyledTextAreaRoot round BaseInput with type "textarea" and its Input and InputContainer overridden
// by StyledTextarea and StyledTextareaContainer, which call Input's getInputStyles and getInputContainerStyles. What
// differs from Input, each from Base Web's source:
// - the root is given only its own props (textarea.js): no $size, so no font and the default radius at every size, no
//   $adjoined, and $hasIconTrailing false, so no padding; its width fit-content once $resize is set (to anything);
// - the textarea's CSS resize is $resize || 'none' (StyledTextarea);
// - base-input.js gives the clear icon's container $alignTop for a textarea: its icon at the top, padded above;
// - no enhancers and no mask toggle (a textarea isn't a password).
// The tests' oracle (tools/reference/textarea.js) checks the result against the real colours.
'use strict';

const { SIZE, ADJOINED } = require('baseui/input');
const ta = require('baseui/textarea/styled-components');
const sc = require('baseui/input/styled-components');
const { px, check, colour } = require('./radio');
const input = require('./input');

/**
 * The textarea's states, each with its values (the default first) and the pseudoclass that carries it: Base Web's $
 * props, which the Textarea mirrors onto every part, `resize` (unset, Base Web's default, then CSS's values) and
 * `isFocusVisible`, the clear icon's own keyboard focus (base-input.js's isFocusVisibleForClear).
 */
const AXES = [
  { name: 'size', values: [SIZE.default, SIZE.mini, SIZE.compact, SIZE.large], pseudo: (v) => `:size-${v}` },
  { name: 'resize', values: [undefined, 'none', 'both', 'horizontal', 'vertical'], pseudo: (v) => `:resize-${v}` },
  { name: 'isFocused', values: [false, true], pseudo: () => ':is-focused' },
  { name: 'disabled', values: [false, true], pseudo: () => ':disabled' },
  { name: 'error', values: [false, true], pseudo: () => ':error' },
  { name: 'positive', values: [false, true], pseudo: () => ':positive' },
  { name: 'isFocusVisible', values: [false, true], pseudo: () => ':focus-visible' },
];

/** textarea.js's own props on its root. */
const rootProps = (c) => ({
  $isFocused: c.isFocused, $isReadOnly: false, $disabled: c.disabled, $error: c.error, $positive: c.positive, $required: false,
  $resize: c.resize,
});
/** What BaseInput gives its parts: utils.js's getSharedProps of the props textarea.js passes on. */
const shared = (c) => ({
  $isFocused: c.isFocused, $disabled: c.disabled, $error: c.error, $positive: c.positive, $adjoined: ADJOINED.none,
  $size: c.size, $required: false, $resize: c.resize, $isReadOnly: false,
});

/** CSS's resize as the TextareaField's Resize: which way its grip drags, or none. */
const RESIZE = { none: 'None', both: 'Both', horizontal: 'Horizontal', vertical: 'Vertical' };

function parts() {
  // Input's parts: their conversions, and Before, After and the clear icon as they are; the others' declarations are the
  // textarea's own components' with its own props.
  const base = Object.fromEntries(input.parts().map((p) => [p.part, p]));
  const { SIDES, RADII, TRANSITION, same, padding, declare } = input;
  return [
    {
      ...base.Root,
      styled: (t, c) => declare(ta.StyledTextAreaRoot, t, rootProps(c)).flat,
      convert(s) {
        const what = 'Root';
        check(s, what, ['boxSizing', 'display', 'overflow', 'width', ...SIDES.flatMap((x) => [`border${x}Width`, `border${x}Style`, `border${x}Color`]),
          ...TRANSITION, ...RADII, 'backgroundColor', 'paddingLeft', 'paddingRight']);
        if (s.boxSizing !== 'border-box' || s.display !== 'flex' || s.overflow !== 'hidden') throw new Error(`${what} box`);
        if (same(s, SIDES.map((x) => `border${x}Style`), what) !== 'solid') throw new Error(`${what} border style`);
        // width: 100% stretches it; fit-content is the textarea's own width, at the start.
        const align = { '100%': 'Stretch', 'fit-content': 'Left' }[s.width];
        if (!align) throw new Error(`${what} width ${s.width}`);
        return {
          HorizontalAlignment: align,
          BorderThickness: px(same(s, SIDES.map((x) => `border${x}Width`), what)),
          BorderBrush: colour(same(s, SIDES.map((x) => `border${x}Color`), what), what),
          Background: colour(s.backgroundColor, what),
          CornerRadius: px(same(s, RADII, what)),
          Padding: padding(s),
        };
      },
    },
    { ...base.InputContainer, styled: (t, c) => declare(ta.StyledTextareaContainer, t, shared(c)).flat },
    {
      ...base.Input,
      type: 'b:TextareaField',
      styled(t, c) {
        const { flat, s } = declare(ta.StyledTextarea, t, shared(c), ['::placeholder']);
        check(s['::placeholder'], 'Input ::placeholder', ['color']);
        return { ...flat, placeholderColor: s['::placeholder'].color };
      },
      convert(s) {
        const { resize, ...rest } = s;
        if (!RESIZE[resize]) throw new Error(`Input resize ${resize}`);
        return { ...base.Input.convert(rest), Resize: RESIZE[resize] };
      },
    },
    base.Before,
    base.After,
    {
      part: 'ClearIconContainer', type: 'Border',
      styled: (t, c) => declare(sc.StyledClearIconContainer, t, { $alignTop: true, ...shared(c) }).flat,
      convert(s) {
        const what = 'ClearIconContainer';
        check(s, what, ['display', 'alignItems', 'paddingLeft', 'paddingRight', 'paddingTop', 'color']);
        // Its icon at the top (alignItems flex-start): the icon's own VerticalAlignment, in ClearIcon's theme.
        if (s.display !== 'flex' || s.alignItems !== 'flex-start') throw new Error(`${what} box`);
        return { Padding: padding(s), [input.INHERITED.Foreground]: colour(s.color, what) };
      },
    },
    // Input's clear icon, at the top of its container.
    { ...base.ClearIcon, fixed: { ...base.ClearIcon.fixed, VerticalAlignment: 'Top' } },
  ];
}

const DESCRIBE = {
  ...input.DESCRIBE,
  Root: 'the box: its 2 px border, radius and fill by state; the textarea\'s own width once it can be resized.',
  Input: 'the textarea: its padding and font by size, its colour, caret and placeholder by state, and which way it resizes.',
  ClearIconContainer: 'the clear icon\'s box, padded by size, its icon at the top.',
};

function file(version) {
  return `<!--
  Generated by tools/tokens/gen.js (tools/tokens/textarea.js) from baseui ${version}
  (© Uber Technologies, Inc., MIT). Do not edit.
  The themes of Textarea's parts, Textarea.Styled<Part>, from Base Web's textarea and the parts of Input's
  BaseInput it renders: each part's look by the state pseudoclasses the Textarea mirrors onto it
  (Metered.Controls.Textarea), the colours by token, the lengths read from Base Web's own style functions, the
  clear icon from Base Web's own icon (icon/delete-alt.js).
-->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:b="https://github.com/wdcossey/Metered">
${input.themes('Textarea', parts(), DESCRIBE, AXES).join('\n')}
</ResourceDictionary>
`;
}

module.exports = { file, parts, AXES };
