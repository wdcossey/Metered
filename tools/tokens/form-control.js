// The themes of FormControl's parts, FormControl.Styled<Part>, generated from Base Web's form control
// (baseui/form-control/styled-components) with the theme writer the other controls share (radio.js): each style function
// evaluated on a theme whose colours are their own names, for every combination of the form control's states, written as
// the fewest state styles and checked against Base Web's own. Nothing here is a port of Base Web's branches. The caption's
// two icons are Base Web's own (icon/alert.js, icon/check.js, MIT), at the size form-control.js gives them, read from
// Base Web's FormControl rendered (tools/reference/form-control.js's render). The tests' oracle
// (tools/reference/form-control.js) checks the result against the real colours.
//
// What the elements are, from Base Web's markup:
// - LabelContainer, a flex row of the label and its end enhancer, and ControlContainer, the child and the caption in a
//   block's flow: Metered panels (FormControlLabelContainer, FormControlStack) that lay them out as a browser does, the
//   stack collapsing its children's margins with each other and its own;
// - CaptionIcon, Base Web's div round the icon's svg: one element, the div's padding round the svg's box, the svg drawn
//   in it (FormControlCaptionIcon), with the path by the icon form-control.js chooses (`:icon-alert`, `:icon-check`).
'use strict';

const React = require('react');
const sc = require('baseui/form-control/styled-components');
const { Svg } = require('baseui/icon/styled-components');
const Alert = require('baseui/icon/alert').default;
const Check = require('baseui/icon/check').default;
const css = require('../reference/css');
const { render } = require('../reference/form-control');
const { L, theme: write, colour, check } = require('./radio');
const { declare, padding, INHERITED } = require('./input');
const { markup } = require('../reference/input');

/**
 * The form control's states, each with its values (the default first) and the pseudoclass that carries it: Base Web's $
 * props, which the FormControl mirrors onto every part (form-control.js's sharedProps), and `icon`, which of Base Web's
 * icons the caption shows (chooseRenderedHintIcon), a state of the FormControl's own.
 */
const AXES = [
  { name: 'disabled', values: [false, true], pseudo: () => ':disabled' },
  { name: 'error', values: [false, true], pseudo: () => ':error' },
  { name: 'positive', values: [false, true], pseudo: () => ':positive' },
  { name: 'counterError', values: [false, true], pseudo: () => ':counter-error' },
  { name: 'icon', values: ['alert', 'check'], pseudo: (v) => `:icon-${v}` },
];

/** form-control.js's sharedProps for a combination. */
const props = (c) => ({ $disabled: c.disabled, $error: c.error, $positive: c.positive, $counterError: c.counterError });

/** A part's own declarations: a style function's, or a style object's (CaptionMessage), which styletron keeps as is. */
function own(styled, t, p) {
  const s = styled.__STYLETRON__;
  if (s.reducers.length === 0) {
    const flat = s.getInitialStyle();
    for (const [k, v] of Object.entries(flat)) if (v !== null && typeof v === 'object') throw new Error(`${styled.displayName}: a block: ${k}`);
    return { ...flat };
  }
  return declare(styled, t, p).flat;
}

/** The icons, by the name the caption's state gives: their paths as Avalonia's, and the size form-control.js renders. */
function icons() {
  const out = {};
  for (const [name, extra] of [['alert', { error: 'Error' }], ['check', { positive: 'Positive' }]]) {
    const { parts } = render({ caption: 'Caption', ...extra });
    const element = parts.CaptionIcon.children;
    if (element.type !== { alert: Alert, check: Check }[name]) throw new Error(`${name}: another icon`);
    out[name] = { size: element.props.size, ...path(element.type) };
  }
  if (out.alert.size !== out.check.size) throw new Error('the icons differ in size');
  return out;
}

/** An icon's one path, as Avalonia's path data with SVG's fill rule, and its viewBox. */
function path(Component) {
  const html = markup(React.createElement(Component, { size: '24px' }), L);
  const view = /<svg\b[^>]*\sviewBox="([^"]+)"/.exec(html);
  const paths = [...html.matchAll(/<path\b([^>]*)>/g)];
  if (!view || paths.length !== 1) throw new Error(`${Component.displayName}: not one path in a viewBox`);
  const attr = (n) => (new RegExp(String.raw`\s${n}="([^"]*)"`).exec(paths[0][1]) || [])[1];
  const rule = attr('fill-rule') ?? 'nonzero';
  // Avalonia's path data is EvenOdd unless it starts F1, NonZero; SVG's default is nonzero.
  const prefix = { evenodd: 'F0 ', nonzero: 'F1 ' }[rule];
  if (!prefix) throw new Error(`fill-rule ${rule}`);
  return { data: prefix + attr('d'), viewBox: view[1].split(/[\s,]+/).map(Number).join(',') };
}
const ICONS = icons();

const r = (key) => `{DynamicResource ${key}}`;
/** The typography token of Base Web's font250 to font100, found by its values. */
function font(s, what, n) {
  const name = Object.keys(L.typography).find((t) => /^font\d+$/.test(t)
    && ['fontFamily', 'fontSize', 'fontWeight', 'lineHeight'].every((k) => L.typography[t][k] === s[k]));
  if (!name) throw new Error(`${what}: no typography token for ${JSON.stringify(s)}`);
  return {
    [n.FontFamily]: r(`typography.${name}.fontFamily`), [n.FontSize]: r(`typography.${name}.fontSize`),
    [n.FontWeight]: r(`typography.${name}.fontWeight`), [n.LineHeight]: r(`typography.${name}.lineHeight`),
  };
}
const FONT = ['fontFamily', 'fontSize', 'fontWeight', 'lineHeight'];
/** A TemplatedControl's or a ContentPresenter's own text properties, which its text inherits. */
const OWN = { Foreground: 'Foreground', FontFamily: 'FontFamily', FontSize: 'FontSize', FontWeight: 'FontWeight', LineHeight: 'LineHeight' };
/** A Label's: its own, and the line height as TextBlock's attached property, which its presenter's text inherits. */
const LABEL = { ...OWN, LineHeight: 'TextBlock.LineHeight' };
/** Left, top, right, bottom margins as Avalonia's Thickness; a side Base Web doesn't set is 0. */
const margin = (s) => ['Left', 'Top', 'Right', 'Bottom'].map((side) => css.px(s[`margin${side}`] ?? 0)).join(',');
const MARGINS = ['marginTop', 'marginRight', 'marginBottom', 'marginLeft'];
const PADDINGS = ['paddingTop', 'paddingRight', 'paddingBottom', 'paddingLeft'];
// CSS's text wraps at a break opportunity and a word longer than the line overflows it: Avalonia's WrapWithOverflow.
const WRAP = 'WrapWithOverflow';

function parts() {
  return [
    {
      // A flex row: the label, the end enhancer at its min-content width (FormControlLabelContainer).
      part: 'LabelContainer', type: 'b:FormControlLabelContainer', fixed: { HorizontalAlignment: 'Stretch' },
      styled: (t, c) => own(sc.LabelContainer, t, props(c)),
      convert(s) {
        const what = 'LabelContainer';
        check(s, what, ['display', 'width', ...MARGINS]);
        if (s.display !== 'flex' || s.width !== '100%') throw new Error(`${what} box`);
        return { Margin: margin(s) };
      },
    },
    {
      // display: block, width 100%: the row's width less the enhancer's, its text at the top; its whole box takes the
      // pointer, as a label element's does.
      part: 'Label', type: 'b:FormControlLabel',
      fixed: { HorizontalAlignment: 'Stretch', VerticalAlignment: 'Stretch', HorizontalContentAlignment: 'Left', VerticalContentAlignment: 'Top',
        Background: 'Transparent', 'TextBlock.TextWrapping': WRAP },
      styled: (t, c) => own(sc.Label, t, props(c)),
      convert(s) {
        const what = 'Label';
        check(s, what, ['width', 'color', 'display', ...PADDINGS, ...FONT]);
        if (s.display !== 'block' || s.width !== '100%') throw new Error(`${what} box`);
        return { Padding: padding(s), Foreground: colour(s.color, what), ...font(s, what, LABEL) };
      },
    },
    {
      // flex: 0 and width 100%: its basis 0, no growing, so its min-content width (FormControlLabelContainer measures it so).
      part: 'LabelEndEnhancer', type: 'ContentPresenter',
      fixed: { VerticalAlignment: 'Stretch', VerticalContentAlignment: 'Top', HorizontalContentAlignment: 'Left', TextWrapping: WRAP },
      styled: (t, c) => own(sc.LabelEndEnhancer, t, props(c)),
      convert(s) {
        const what = 'LabelEndEnhancer';
        check(s, what, ['flex', 'width', 'color', ...FONT]);
        if (s.flex !== 0 || s.width !== '100%') throw new Error(`${what} box`);
        return { Foreground: colour(s.color, what), ...font(s, what, OWN) };
      },
    },
    {
      // A block: the child and the caption in its flow, their margins collapsed (FormControlStack).
      part: 'ControlContainer', type: 'b:FormControlStack', fixed: { HorizontalAlignment: 'Stretch' },
      styled: (t, c) => own(sc.ControlContainer, t, props(c)),
      convert(s) {
        const what = 'ControlContainer';
        check(s, what, ['width', 'marginBottom']);
        if (s.width !== '100%') throw new Error(`${what} box`);
        return { Margin: margin(s) };
      },
    },
    {
      // A flex row of the icon and the message, which inherit its font and colour.
      part: 'Caption', type: 'Border', fixed: { HorizontalAlignment: 'Stretch' },
      styled: (t, c) => own(sc.Caption, t, props(c)),
      convert(s) {
        const what = 'Caption';
        check(s, what, ['display', 'color', ...PADDINGS, ...MARGINS, ...FONT]);
        if (s.display !== 'flex') throw new Error(`${what} box`);
        return { Margin: margin(s), Padding: padding(s), [INHERITED.Foreground]: colour(s.color, what), ...font(s, what, INHERITED) };
      },
    },
    {
      // Base Web's div and the svg in it: the div's padding round the icon's box (Svg's size, form-control.js's 12), the
      // path by the icon the caption shows, in the caption's colour (currentColor). At the top of the row, unshrunk.
      part: 'CaptionIcon', type: 'b:FormControlCaptionIcon', fixed: { VerticalAlignment: 'Top', HorizontalAlignment: 'Left' },
      styled(t, c) {
        const svg = declare(Svg, t, { $size: ICONS[c.icon].size }).flat;
        return { ...own(sc.CaptionIcon, t, {}), svgWidth: svg.width, svgHeight: svg.height, svgDisplay: svg.display, svgFill: svg.fill, icon: c.icon };
      },
      convert(s) {
        const what = 'CaptionIcon';
        check(s, what, ['display', 'paddingTop', 'paddingRight', 'flexShrink', 'svgWidth', 'svgHeight', 'svgDisplay', 'svgFill', 'icon']);
        if (s.display !== 'flex' || s.flexShrink !== 0 || s.svgFill !== 'currentColor' || s.svgDisplay !== 'inline-block') throw new Error(`${what} box`);
        const pad = ['Left', 'Top', 'Right', 'Bottom'].map((side) => css.px(s[`padding${side}`] ?? 0));
        return {
          Padding: pad.join(','),
          Width: String(pad[0] + css.px(s.svgWidth) + pad[2]),
          Height: String(pad[1] + css.px(s.svgHeight) + pad[3]),
          Data: ICONS[s.icon].data,
          ViewBox: ICONS[s.icon].viewBox,
        };
      },
    },
    {
      // flexGrow 1: the rest of the row; its font and colour the caption's (inherit).
      part: 'CaptionMessage', type: 'ContentPresenter',
      fixed: { HorizontalAlignment: 'Stretch', HorizontalContentAlignment: 'Left', VerticalContentAlignment: 'Top', TextWrapping: WRAP },
      styled: (t, c) => own(sc.CaptionMessage, t, props(c)),
      convert(s) {
        const what = 'CaptionMessage';
        check(s, what, ['flexGrow', ...FONT]);
        if (s.flexGrow !== 1 || FONT.some((k) => s[k] !== 'inherit')) throw new Error(`${what} box`);
        return {};
      },
    },
  ];
}

const DESCRIBE = {
  LabelContainer: 'the row of the label and its end enhancer, between its margins.',
  Label: 'the label: its font, and its colour while disabled.',
  LabelEndEnhancer: 'the label\'s end enhancer, the counter\'s text by default: its font, and its colour while disabled or past the counter\'s maximum.',
  ControlContainer: 'the child and the caption, and the margin below them.',
  Caption: 'the caption, the error or the positive message: its font, margins, and colour by state.',
  CaptionIcon: 'Base Web\'s alert or check icon before an error or a positive message, in the caption\'s colour.',
  CaptionMessage: 'the message, in the caption\'s font and colour.',
};

function file(version) {
  const themes = parts().map((p) => {
    const { plain, blocks } = write(p, AXES, (styled, t, c) => styled(t, c), { layered: true });
    const body = [...plain, ...blocks];
    return `  <!-- ${p.part}: ${DESCRIBE[p.part]} -->
  <ControlTheme x:Key="FormControl.Styled${p.part}" TargetType="${p.type}"${body.length ? `>\n${body.join('\n')}\n  </ControlTheme>` : ' />'}`;
  });
  return `<!--
  Generated by tools/tokens/gen.js (tools/tokens/form-control.js) from baseui ${version}
  (© Uber Technologies, Inc., MIT). Do not edit.
  The themes of FormControl's parts, FormControl.Styled<Part>, from Base Web's form control: each part's look by
  the state pseudoclasses the FormControl mirrors onto it (Metered.Controls.FormControl), the colours by token,
  the lengths read from Base Web's own style functions, the caption's icons from Base Web's own icons
  (icon/alert.js, icon/check.js).
-->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:b="https://github.com/wdcossey/Metered">
${themes.join('\n')}
</ResourceDictionary>
`;
}

module.exports = { file, parts, AXES, ICONS };
