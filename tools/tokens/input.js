// The themes of Input's parts, Input.Styled<Part>, generated from Base Web's input (baseui/input/styled-components) with
// the theme writer the other controls share (radio.js): each style function evaluated on a theme whose colours are
// their own names, for every combination of the input's states, written as the fewest state styles and checked against
// Base Web's own. Nothing here is a port of Base Web's branches. The icons' sizes are read from Base Web's own Input,
// rendered (base-input.js gives them to the icons as props), and the icons' paths from Base Web's icons rendered
// (icon/delete-alt.js, icon/show.js, icon/hide.js, MIT). The tests' oracle (tools/reference/input.js) checks the result
// against the real colours.
//
// Textarea (#85) renders the same BaseInput and reuses these style functions (getRootStyles, getInputContainerStyles,
// getInputStyles), so the parts are exported: `parts(styled)` makes them for other styled components, and `file` writes
// any control's themes from them.
'use strict';

const React = require('react');
const { renderToStaticMarkup } = require('react-dom/server');
const { Provider } = require('styletron-react');
const { Server } = require('styletron-engine-monolithic');
const { ThemeProvider } = require('baseui');
const { Input, SIZE, ADJOINED } = require('baseui/input');
const { ENHANCER_POSITION } = require('baseui/input/constants');
const BaseInput = require('baseui/input/base-input').default;
const sc = require('baseui/input/styled-components');
const { Svg } = require('baseui/icon/styled-components');
const DeleteAlt = require('baseui/icon/delete-alt').default;
const Show = require('baseui/icon/show').default;
const Hide = require('baseui/icon/hide').default;
const css = require('../reference/css');
const { evaluate } = require('../reference/oracle');
const { L, NAMED, theme: write, colour, px, check, timing } = require('./radio');

/**
 * The input's states, each with its values (the default first) and the pseudoclass that carries it: Base Web's $ props,
 * which the Input mirrors onto every part, and `isFocusVisible`, the clear icon's and the mask toggle's own keyboard
 * focus (base-input.js's isFocusVisibleForClear and isFocusVisibleForMaskToggle), Avalonia's :focus-visible on each.
 * `adjoined` is the enhancers' (input.js's getAdjoinedProp); `hasIconTrailing` clearable or a password.
 */
const AXES = [
  { name: 'size', values: [SIZE.default, SIZE.mini, SIZE.compact, SIZE.large], pseudo: (v) => `:size-${v}` },
  { name: 'adjoined', values: [ADJOINED.none, ADJOINED.left, ADJOINED.right, ADJOINED.both], pseudo: (v) => `:adjoined-${v}` },
  { name: 'hasIconTrailing', values: [false, true], pseudo: () => ':has-icon-trailing' },
  { name: 'isFocused', values: [false, true], pseudo: () => ':is-focused' },
  { name: 'disabled', values: [false, true], pseudo: () => ':disabled' },
  { name: 'error', values: [false, true], pseudo: () => ':error' },
  { name: 'positive', values: [false, true], pseudo: () => ':positive' },
  { name: 'isFocusVisible', values: [false, true], pseudo: () => ':focus-visible' },
];

/** Base Web's $ props for a combination: utils.js's getSharedProps, and what input.js and base-input.js add per part. */
const props = (c) => ({
  $isFocused: c.isFocused, $disabled: c.disabled, $error: c.error, $positive: c.positive, $adjoined: c.adjoined,
  $size: c.size, $required: false, $isReadOnly: false, $hasIconTrailing: c.hasIconTrailing, $isFocusVisible: c.isFocusVisible,
  $alignTop: false,
});

/** A styled component's declarations for a combination; its nested blocks only those it may have. */
function declare(styled, t, p, nested = []) {
  const s = evaluate(styled, t, p);
  const flat = {};
  for (const [k, v] of Object.entries(s)) {
    if (v !== null && typeof v === 'object') {
      if (!nested.includes(k)) throw new Error(`${styled.displayName}: a block this generator doesn't read: ${k}`);
    } else flat[k] = v;
  }
  return { flat, s };
}

const markup = (element) => renderToStaticMarkup(React.createElement(Provider, { value: new Server() },
  React.createElement(ThemeProvider, { theme: L }, element)));

/**
 * The size base-input.js gives each icon, for each of the input's sizes, read from Base Web's Input rendered with every
 * part recorded: clearable, a password and a value, so both icons render, and again with the mask off for the hide icon.
 */
function iconSizes() {
  const out = { ClearIcon: {}, MaskToggleShowIcon: {}, MaskToggleHideIcon: {} };
  for (const size of Object.values(SIZE)) {
    for (const unmasked of [false, true]) {
      const seen = {};
      const overrides = {};
      for (const name of Object.keys(out)) {
        // eslint-disable-next-line react/display-name
        overrides[name] = { component: React.forwardRef((p, ref) => { void ref; seen[name] = p.size; return null; }) };
      }
      const draw = BaseInput.prototype.render;
      if (unmasked) BaseInput.prototype.render = function unmaskedRender() { this.state = { ...this.state, isMasked: false }; return draw.call(this); };
      try {
        markup(React.createElement(Input, { size, clearable: true, type: 'password', value: 'x', overrides }));
      } finally {
        BaseInput.prototype.render = draw;
      }
      for (const [name, v] of Object.entries(seen)) {
        if (out[name][size] !== undefined && out[name][size] !== v) throw new Error(`${name} ${size}: two sizes`);
        out[name][size] = v;
      }
    }
  }
  for (const [name, bySize] of Object.entries(out)) {
    if (Object.keys(bySize).length !== Object.keys(SIZE).length) throw new Error(`${name}: not rendered at every size`);
  }
  return out;
}
const ICON_SIZES = iconSizes();

/** An icon of Base Web's, rendered: its viewBox and its one path, as Avalonia's path data with SVG's fill rule. */
function icon(Component) {
  const html = markup(React.createElement(Component, { size: '24px' }));
  const view = /<svg\b[^>]*\sviewBox="([^"]+)"/.exec(html);
  const paths = [...html.matchAll(/<path\b([^>]*)>/g)];
  if (!view || paths.length !== 1) throw new Error(`${Component.displayName}: not one path in a viewBox`);
  const attr = (name) => (new RegExp(String.raw`\s${name}="([^"]*)"`).exec(paths[0][1]) || [])[1];
  const rule = attr('fill-rule') ?? 'nonzero';
  // Avalonia's path data is EvenOdd unless it starts F1, NonZero; SVG's default is nonzero.
  const prefix = { evenodd: 'F0 ', nonzero: 'F1 ' }[rule];
  if (!prefix) throw new Error(`fill-rule ${rule}`);
  return { data: prefix + attr('d'), viewBox: view[1].split(/[\s,]+/).map(Number).join(',') };
}

// Conversions.
const r = (key) => `{DynamicResource ${key}}`;
const cursor = (v) => ({ pointer: 'Hand', 'not-allowed': 'No', text: 'Ibeam' })[v] ?? (() => { throw new Error(`cursor ${v}`); })();
/** The typography token Base Web's getFont gives (font100 to font400), found by its values. */
function font(s, what, n) {
  const name = Object.keys(L.typography).find((t) => /^font\d+$/.test(t)
    && ['fontFamily', 'fontSize', 'fontWeight', 'lineHeight'].every((k) => L.typography[t][k] === s[k]));
  if (!name) throw new Error(`${what}: no typography token for ${JSON.stringify(s)}`);
  return {
    [n.FontFamily]: r(`typography.${name}.fontFamily`),
    [n.FontSize]: r(`typography.${name}.fontSize`),
    [n.FontWeight]: r(`typography.${name}.fontWeight`),
    [n.LineHeight]: r(`typography.${name}.lineHeight`),
  };
}
/** The text properties an element passes down (attached, inherited), or a ContentPresenter's own. */
const INHERITED = { Foreground: 'TextElement.Foreground', FontFamily: 'TextElement.FontFamily', FontSize: 'TextElement.FontSize', FontWeight: 'TextElement.FontWeight', LineHeight: 'TextBlock.LineHeight' };
const OWN = { Foreground: 'Foreground', FontFamily: 'FontFamily', FontSize: 'FontSize', FontWeight: 'FontWeight', LineHeight: 'LineHeight' };
const FONT = ['fontFamily', 'fontSize', 'fontWeight', 'lineHeight'];
const SIDES = ['Left', 'Right', 'Top', 'Bottom'];
const RADII = ['borderTopLeftRadius', 'borderBottomLeftRadius', 'borderTopRightRadius', 'borderBottomRightRadius'];
const TRANSITION = ['transitionProperty', 'transitionDuration', 'transitionTimingFunction'];
function same(s, keys, what) {
  const v = new Set(keys.map((k) => s[k]));
  if (v.size !== 1) throw new Error(`${what}: ${keys.join(', ')} differ`);
  return [...v][0];
}
/** Left, top, right, bottom padding as Avalonia's Thickness; a side Base Web doesn't set is 0. */
const padding = (s) => ['Left', 'Top', 'Right', 'Bottom'].map((side) => css.px(s[`padding${side}`] ?? 0)).join(',');
/** CSS's outline, 'solid 3px <colour>' or none, as a RadioMark's: the offset is CSS's default, 0. */
function outline(v, what) {
  if (v === 'none') return { OutlineBrush: '{x:Null}', OutlineThickness: '0', OutlineOffset: '0' };
  const m = /^solid (\S+) (@\w+)$/.exec(v);
  if (!m) throw new Error(`${what} outline ${v}`);
  return { OutlineBrush: colour(m[2], what), OutlineThickness: px(m[1]), OutlineOffset: '0' };
}
/** An icon's box: icon/styled-components.js's Svg for the size base-input.js gives it at the input's size. */
function svgBox(part, c, what) {
  const s = declare(Svg, NAMED, { $size: ICON_SIZES[part][c.size] }).flat;
  check(s, what, ['display', 'fill', 'color', 'height', 'width']);
  if (s.display !== 'inline-block' || s.fill !== 'currentColor' || s.color !== 'currentColor' || s.width !== s.height) throw new Error(`${what} svg`);
  return { Width: px(s.width), Height: px(s.height) };
}

/**
 * Each part of BaseInput and Input, for the given styled components (Base Web's input's by default; Textarea's give its
 * own Root, InputContainer and Input): its element type, the declarations a combination gives it, and those as
 * Avalonia's properties.
 */
function parts(styled = { Root: sc.Root, InputContainer: sc.InputContainer, Input: sc.Input }) {
  const enhancer = (position) => ({
    part: position === ENHANCER_POSITION.start ? 'StartEnhancer' : 'EndEnhancer', type: 'ContentPresenter',
    // display: flex, centred both ways.
    fixed: { HorizontalContentAlignment: 'Center', VerticalContentAlignment: 'Center' },
    styled: (t, c) => declare(sc.InputEnhancer, t, { ...props(c), $position: position }).flat,
    convert(s) {
      const what = 'InputEnhancer';
      check(s, what, ['display', 'alignItems', 'justifyContent', ...TRANSITION, ...FONT, 'paddingLeft', 'paddingRight', 'color', 'backgroundColor']);
      if (s.display !== 'flex' || s.alignItems !== 'center' || s.justifyContent !== 'center') throw new Error(`${what} box`);
      return { Padding: padding(s), Foreground: colour(s.color, what), Background: colour(s.backgroundColor, what), ...font(s, what, OWN) };
    },
    transitions: [['BrushTransition', 'Foreground'], ['BrushTransition', 'Background']],
    transitionOf: 'color, background-color',
  });

  return [
    {
      part: 'Root', type: 'Border', fixed: { ClipToBounds: 'True' },
      styled: (t, c) => declare(styled.Root, t, props(c)).flat,
      convert(s) {
        const what = 'Root';
        check(s, what, ['boxSizing', 'display', 'overflow', 'width', ...SIDES.flatMap((x) => [`border${x}Width`, `border${x}Style`, `border${x}Color`]),
          ...TRANSITION, ...RADII, ...FONT, 'backgroundColor', 'paddingLeft', 'paddingRight']);
        if (s.boxSizing !== 'border-box' || s.display !== 'flex' || s.overflow !== 'hidden' || s.width !== '100%') throw new Error(`${what} box`);
        if (same(s, SIDES.map((x) => `border${x}Style`), what) !== 'solid') throw new Error(`${what} border style`);
        return {
          BorderThickness: px(same(s, SIDES.map((x) => `border${x}Width`), what)),
          BorderBrush: colour(same(s, SIDES.map((x) => `border${x}Color`), what), what),
          Background: colour(s.backgroundColor, what),
          CornerRadius: px(same(s, RADII, what)),
          Padding: padding(s),
          ...font(s, what, INHERITED),
        };
      },
      transitions: [['BrushTransition', 'BorderBrush']],
      // CSS's border: only its colour changes.
      transitionOf: 'border',
    },
    enhancer(ENHANCER_POSITION.start),
    enhancer(ENHANCER_POSITION.end),
    {
      part: 'InputContainer', type: 'Border',
      styled: (t, c) => declare(styled.InputContainer, t, props(c)).flat,
      convert(s) {
        const what = 'InputContainer';
        check(s, what, ['display', 'width', ...TRANSITION, ...FONT, 'color', 'backgroundColor']);
        if (s.display !== 'flex' || s.width !== '100%') throw new Error(`${what} box`);
        return { Background: colour(s.backgroundColor, what), [INHERITED.Foreground]: colour(s.color, what), ...font(s, what, INHERITED) };
      },
      transitions: [['BrushTransition', 'Background']],
      transitionOf: 'background-color',
    },
    {
      // The element the text is in: its padding, font and colours, the caret's and the placeholder's.
      part: 'Input', type: 'b:InputField', fixed: { Background: 'Transparent' },
      styled(t, c) {
        const { flat, s } = declare(styled.Input, t, props(c), ['::placeholder']);
        check(s['::placeholder'], 'Input ::placeholder', ['color']);
        return { ...flat, placeholderColor: s['::placeholder'].color };
      },
      convert(s) {
        const what = 'Input';
        check(s, what, ['boxSizing', 'backgroundColor', ...SIDES.flatMap((x) => [`border${x}Width`, `border${x}Style`]), 'outline', 'width',
          'minWidth', 'maxWidth', 'cursor', 'margin', 'paddingTop', 'paddingBottom', 'paddingLeft', 'paddingRight', ...FONT, 'color',
          '-webkit-text-fill-color', 'caretColor', 'placeholderColor']);
        if (s.boxSizing !== 'border-box' || s.backgroundColor !== 'transparent' || s.outline !== 'none' || s.width !== '100%'
          || String(s.margin) !== '0' || same(s, SIDES.map((x) => `border${x}Width`), what) !== 0) throw new Error(`${what} box`);
        // A disabled input's text fill is its colour (getInputColors).
        if (s['-webkit-text-fill-color'] !== undefined && s['-webkit-text-fill-color'] !== s.color) throw new Error(`${what} text fill`);
        return {
          Padding: padding(s),
          [INHERITED.Foreground]: colour(s.color, what),
          CaretBrush: colour(s.caretColor, `${what} caret`),
          PlaceholderForeground: colour(s.placeholderColor, `${what} placeholder`),
          Cursor: cursor(s.cursor),
          ...font(s, what, INHERITED),
        };
      },
    },
    // Base Web's NullComponent: an app's content (the input's InnerLeftContent and InnerRightContent).
    { part: 'Before', type: 'ContentPresenter', styled: () => ({}), convert: () => ({}) },
    { part: 'After', type: 'ContentPresenter', styled: () => ({}), convert: () => ({}) },
    {
      part: 'ClearIconContainer', type: 'Border',
      styled: (t, c) => declare(sc.StyledClearIconContainer, t, props(c)).flat,
      convert(s) {
        const what = 'ClearIconContainer';
        check(s, what, ['display', 'alignItems', 'paddingLeft', 'paddingRight', 'paddingTop', 'color']);
        // Its icon centred in its height: alignItems center, the icon's own VerticalAlignment (ClearIcon's theme).
        if (s.display !== 'flex' || s.alignItems !== 'center') throw new Error(`${what} box`);
        return { Padding: padding(s), [INHERITED.Foreground]: colour(s.color, what) };
      },
    },
    {
      // The icon fills with currentColor: the container's colour, inherited.
      part: 'ClearIcon', type: 'b:InputIcon',
      // Its focus ring is Base Web's outline, not Avalonia's adorner.
      fixed: { VerticalAlignment: 'Center', HorizontalAlignment: 'Left', Focusable: 'True', FocusAdorner: '{x:Null}', Data: CLEAR.data, ViewBox: CLEAR.viewBox },
      styled: (t, c) => ({ ...svgBox('ClearIcon', c, 'ClearIcon'), ...declare(sc.StyledClearIcon, t, props(c)).flat }),
      convert(s) {
        const what = 'ClearIcon';
        check(s, what, ['Width', 'Height', 'cursor', 'outline']);
        return { Width: s.Width, Height: s.Height, Cursor: cursor(s.cursor), ...outline(s.outline, what) };
      },
    },
    {
      // A button: borderless, no background, its icon centred in its height; the outline on keyboard focus.
      part: 'MaskToggleButton', type: 'b:InputMaskToggleButton',
      fixed: { VerticalAlignment: 'Stretch', VerticalContentAlignment: 'Center', HorizontalContentAlignment: 'Left', BorderThickness: '0', Background: 'Transparent', FocusAdorner: '{x:Null}',
        // CSS's overflow is visible: the outline is drawn outside the button (an Avalonia Button clips by default).
        ClipToBounds: 'False' },
      // base-input.js gives the toggle $size and $isFocusVisible only.
      styled: (t, c) => declare(sc.StyledMaskToggleButton, t, { $size: c.size, $isFocusVisible: c.isFocusVisible }).flat,
      convert(s) {
        const what = 'MaskToggleButton';
        check(s, what, ['display', 'alignItems', ...SIDES.map((x) => `border${x}Style`), 'background', 'paddingLeft', 'paddingRight', 'outline', 'color']);
        if (s.display !== 'flex' || s.alignItems !== 'center' || s.background !== 'none' || same(s, SIDES.map((x) => `border${x}Style`), what) !== 'none') throw new Error(`${what} box`);
        return { Padding: padding(s), Foreground: colour(s.color, what), ...outline(s.outline, what) };
      },
    },
    {
      part: 'MaskToggleShowIcon', type: 'b:InputIcon', fixed: { Data: SHOW.data, ViewBox: SHOW.viewBox },
      styled: (t, c) => svgBox('MaskToggleShowIcon', c, 'MaskToggleShowIcon'),
      convert: (s) => ({ Width: s.Width, Height: s.Height }),
    },
    {
      part: 'MaskToggleHideIcon', type: 'b:InputIcon', fixed: { Data: HIDE.data, ViewBox: HIDE.viewBox },
      styled: (t, c) => svgBox('MaskToggleHideIcon', c, 'MaskToggleHideIcon'),
      convert: (s) => ({ Width: s.Width, Height: s.Height }),
    },
  ];
}
const CLEAR = icon(DeleteAlt), SHOW = icon(Show), HIDE = icon(Hide);

/** A part's transition, once the input has loaded (CSS doesn't transition a page's first paint; Avalonia would). */
function transition(p, axes = AXES) {
  const s = p.styled(L, Object.fromEntries(axes.map((a) => [a.name, a.values[0]])));
  if (s.transitionProperty !== p.transitionOf) throw new Error(`${p.part} transition ${s.transitionProperty}`);
  const { span, easing } = timing(s);
  return `    <Style Selector="^:loaded">
      <Setter Property="Transitions">
        <Transitions>
${p.transitions.map(([type, prop]) => `          <${type} Property="${prop}" Duration="${span}" Easing="${easing}" />`).join('\n')}
        </Transitions>
      </Setter>
    </Style>`;
}

/** A part's theme: the fewest state styles that give Base Web's values in every combination of the states. */
const theme = (p, axes = AXES) => write(p, axes, (styled, t, c) => styled(t, c), { layered: true });

const DESCRIBE = {
  Root: 'the box: its 2 px border, radius, fill and font by state and size; padding beside an enhancer and before a trailing icon.',
  StartEnhancer: 'the start enhancer, the input\'s StartEnhancer: padded, centred, coloured by state.',
  EndEnhancer: 'the end enhancer, the input\'s EndEnhancer.',
  InputContainer: 'BaseInput\'s box round the text and its icons: its fill and colour by state.',
  Input: 'the text: its padding and font by size, its colour, caret and placeholder by state.',
  Before: 'Base Web\'s Before, an app\'s content before the text: the input\'s InnerLeftContent.',
  After: 'Base Web\'s After, an app\'s content after the icons: the input\'s InnerRightContent.',
  ClearIconContainer: 'the clear icon\'s box, padded by size.',
  ClearIcon: 'Base Web\'s delete-alt icon, sized by the input\'s size; the focus outline on keyboard focus.',
  MaskToggleButton: 'the password\'s show and hide button, padded by size; the focus outline on keyboard focus.',
  MaskToggleShowIcon: 'Base Web\'s show icon, while the text is masked.',
  MaskToggleHideIcon: 'Base Web\'s hide icon, while it isn\'t.',
};

/**
 * The themes of a control's parts, keyed `{component}.Styled{Part}`, for its states (`axes`, Input's by default:
 * Textarea's have no adjoined or trailing flag, and have its resize).
 */
function themes(component, list, describe = DESCRIBE, axes = AXES) {
  return list.map((p) => {
    const { plain, blocks } = theme(p, axes);
    const extra = p.transitions ? '\n' + transition(p, axes) : '';
    const body = [...plain.map((l) => l), ...(extra ? [extra.slice(1)] : []), ...blocks];
    return `  <!-- ${p.part}: ${describe[p.part]} -->
  <ControlTheme x:Key="${component}.Styled${p.part}" TargetType="${p.type}"${body.length ? `>\n${body.join('\n')}\n  </ControlTheme>` : ' />'}`;
  });
}

function file(version) {
  return `<!--
  Generated by tools/tokens/gen.js (tools/tokens/input.js) from baseui ${version}
  (© Uber Technologies, Inc., MIT). Do not edit.
  The themes of Input's parts, Input.Styled<Part>, from Base Web's input: each part's look by the state
  pseudoclasses the Input mirrors onto it (Metered.Controls.Input), the colours by token, the lengths read
  from Base Web's own style functions, the icons from Base Web's own icons (icon/delete-alt.js, icon/show.js,
  icon/hide.js).
-->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:b="https://github.com/wdcossey/Metered">
${themes('Input', parts()).join('\n')}
</ResourceDictionary>
`;
}

module.exports = { file, parts, themes, AXES, ICON_SIZES, DESCRIBE, declare, padding, INHERITED, SIDES, RADII, TRANSITION, same };
