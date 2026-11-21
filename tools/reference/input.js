// The oracle for Input: what Base Web's own input gives (baseui/input), for the cases the tests build, on the real light
// and dark themes, in Avalonia's units. Each case renders Base Web's Input itself (react-dom/server) with every part
// overridden by a component that records the props input.js and base-input.js give it, then evaluates each part's own
// style function with those props: which parts render, their labels, the icons' sizes and every declaration are Base
// Web's, with nothing decided here. The icons' paths are read from Base Web's own icons rendered (icon/delete-alt.js,
// icon/show.js, icon/hide.js). Kept apart from tools/tokens/input.js, which writes the themes: the tests compare one with
// the other. Read by cases.js, which writes tests/Metered.Tests/Reference/input.oracle.json.
'use strict';

const React = require('react');
const { renderToStaticMarkup } = require('react-dom/server');
const { Provider } = require('styletron-react');
const { Server } = require('styletron-engine-monolithic');
const { ThemeProvider } = require('baseui');
const { Input, SIZE } = require('baseui/input');
const BaseInput = require('baseui/input/base-input').default;
const sc = require('baseui/input/styled-components');
const { Svg } = require('baseui/icon/styled-components');
const DeleteAlt = require('baseui/icon/delete-alt').default;
const Show = require('baseui/icon/show').default;
const Hide = require('baseui/icon/hide').default;
const css = require('./css');
const { THEMES, evaluate } = require('./oracle');

/** Base Web's override keys (types.d.ts's InputOverrides), in the order input.js renders them. */
const PARTS = ['Root', 'StartEnhancer', 'InputContainer', 'Before', 'Input', 'ClearIconContainer', 'ClearIcon',
  'MaskToggleButton', 'MaskToggleShowIcon', 'MaskToggleHideIcon', 'After', 'EndEnhancer'];

/** Renders an element of Base Web's in a theme, to static markup. */
const markup = (element, theme) => renderToStaticMarkup(React.createElement(Provider, { value: new Server() },
  React.createElement(ThemeProvider, { theme }, element)));

/**
 * Renders Base Web's Input with these props, and returns, for each part, the props it was given; a part not rendered is
 * missing. `autoFocus` is how a case is focused: input.js and base-input.js both start with
 * `isFocused: this.props.autoFocus`. `unmasked` renders it as the mask toggle's click leaves it: base-input.js's
 * BaseInput, which Input renders, drawn with its state's `isMasked` false.
 */
function render(props, theme = THEMES.Light, { unmasked = false } = {}) {
  const seen = {};
  const overrides = {};
  for (const name of PARTS) {
    // eslint-disable-next-line react/display-name
    overrides[name] = { component: React.forwardRef((p, ref) => { (seen[name] ||= []).push(p); void ref; return React.createElement('div', null, p.children); }) };
  }
  const draw = BaseInput.prototype.render;
  const warn = console.warn;
  if (unmasked) BaseInput.prototype.render = function unmaskedRender() { this.state = { ...this.state, isMasked: false }; return draw.call(this); };
  // input.js warns when error and positive are both set; the cases set both on purpose.
  console.warn = (m, ...rest) => { if (!/`error` and `positive` are both set/.test(String(m))) warn(m, ...rest); };
  try {
    markup(React.createElement(Input, { ...props, overrides }), theme);
  } finally {
    BaseInput.prototype.render = draw;
    console.warn = warn;
  }
  for (const [name, list] of Object.entries(seen)) {
    if (list.length !== 1) throw new Error(`${name}: rendered ${list.length} times`);
    seen[name] = list[0];
  }
  return seen;
}

/** A styled component's flat declarations for the props, and its nested blocks checked for what this file reads. */
function declarations(styled, theme, props, nested = []) {
  const s = evaluate(styled, theme, props);
  const flat = {};
  for (const [k, v] of Object.entries(s)) {
    if (v !== null && typeof v === 'object') {
      if (!nested.includes(k)) throw new Error(`${styled.displayName}: a block this oracle doesn't read: ${k}`);
    } else flat[k] = v;
  }
  return { flat, s };
}

/** An icon's own style: icon/styled-components.js's Svg for the size it was given (the colour is currentColor). */
const svg = (theme, size) => declarations(Svg, theme, { $size: size }).flat;

/** Each part's declarations for the props Base Web gave it. */
function style(part, theme, props) {
  switch (part) {
    case 'Root': return declarations(sc.Root, theme, props).flat;
    case 'StartEnhancer': case 'EndEnhancer': return declarations(sc.InputEnhancer, theme, props).flat;
    case 'InputContainer': return declarations(sc.InputContainer, theme, props).flat;
    case 'Input': {
      const { flat, s } = declarations(sc.Input, theme, props, ['::placeholder']);
      return { ...flat, placeholderColor: s['::placeholder'].color };
    }
    case 'ClearIconContainer': return declarations(sc.StyledClearIconContainer, theme, props).flat;
    // styled(DeleteAlt, …): the icon's Svg style, then the styled function's own.
    case 'ClearIcon': return { ...svg(theme, props.size), ...declarations(sc.StyledClearIcon, theme, props).flat };
    case 'MaskToggleButton': return declarations(sc.StyledMaskToggleButton, theme, props).flat;
    case 'MaskToggleShowIcon': case 'MaskToggleHideIcon': return svg(theme, props.size);
    default: throw new Error(part);
  }
}

const sides = (s, prefix, names = ['Left', 'Top', 'Right', 'Bottom']) => names.map((side) => css.px(s[prefix + side] ?? 0));
const colour = (v) => (v === undefined ? null : css.color(v));

/** 'solid 3px <colour>' as { colour, thickness }, or null for none. */
function outline(v) {
  if (v === undefined || v === 'none') return null;
  const m = /^solid (\S+) (.+)$/.exec(v);
  if (!m) throw new Error(`outline ${v}`);
  return { colour: css.color(m[2]), thickness: css.px(m[1]) };
}

/** The four border colours as one: they're the same in every state (getRootColors). */
function border(s) {
  const all = new Set(['Left', 'Right', 'Top', 'Bottom'].map((side) => s[`border${side}Color`]));
  if (all.size !== 1) throw new Error('border colours differ');
  return css.color([...all][0]);
}

/**
 * The states the tests drive: Base Web's props, and focus (autoFocus, which the tests give by a real Tab). Error and
 * positive together draw error (input.js warns of it; getRootColors tests $error first).
 */
const STATES = [
  ['rest', {}],
  ['focused', { autoFocus: true }],
  ['error', { error: true }],
  ['positive', { positive: true }],
  ['errorPositive', { error: true, positive: true }],
  ['focusedError', { autoFocus: true, error: true }],
  ['focusedPositive', { autoFocus: true, positive: true }],
  ['disabled', { disabled: true }],
  ['disabledError', { disabled: true, error: true }],
  ['readOnly', { readOnly: true }],
  ['readOnlyFocused', { readOnly: true, autoFocus: true }],
];

/** Every part's colours, with both enhancers, the clear icon and the mask toggle. */
function colours() {
  const cases = [];
  for (const theme of Object.keys(THEMES)) {
    for (const [state, extra] of STATES) {
      const t = THEMES[theme];
      const r = render({ startEnhancer: '@', endEnhancer: '.com', clearable: true, type: 'password', value: 'Value', placeholder: 'Placeholder', ...extra }, t);
      const of = (part) => (r[part] ? style(part, t, r[part]) : null);
      const root = of('Root'), input = of('Input'), start = of('StartEnhancer'), end = of('EndEnhancer'), container = of('InputContainer');
      if (input.WebkitTextFillColor !== undefined && input.WebkitTextFillColor !== input.color) throw new Error('text fill differs from colour');
      cases.push({
        theme, state,
        rootBorder: border(root), rootBackground: colour(root.backgroundColor),
        startForeground: colour(start.color), startBackground: colour(start.backgroundColor),
        endForeground: colour(end.color), endBackground: colour(end.backgroundColor),
        containerForeground: colour(container.color), containerBackground: colour(container.backgroundColor),
        inputForeground: colour(input.color), caret: colour(input.caretColor), placeholder: colour(input.placeholderColor),
        inputCursor: input.cursor,
        clearIconContainer: r.ClearIconContainer ? colour(of('ClearIconContainer').color) : null,
        maskToggleButton: r.MaskToggleButton ? colour(of('MaskToggleButton').color) : null,
      });
    }
  }
  return cases;
}

/**
 * The clear icon's and the mask toggle's focus outline, which their own focus-visible state draws (base-input.js's
 * isFocusVisibleForClear and isFocusVisibleForMaskToggle, set by the focus-visible check on a keyboard's focus): the
 * props each was rendered with, that state on and off.
 */
function outlines() {
  const cases = [];
  for (const theme of Object.keys(THEMES)) {
    const t = THEMES[theme];
    const r = render({ clearable: true, type: 'password', value: 'Value' }, t);
    for (const part of ['ClearIcon', 'MaskToggleButton']) {
      for (const focusVisible of [false, true]) {
        const s = style(part, t, { ...r[part], $isFocusVisible: focusVisible });
        cases.push({ theme, part, focusVisible, outline: outline(s.outline) });
      }
    }
  }
  return cases;
}

const ENHANCERS = { none: {}, start: { startEnhancer: '@' }, end: { endEnhancer: '.com' }, both: { startEnhancer: '$', endEnhancer: '.00' } };
const TRAILING = { none: {}, clearable: { clearable: true }, password: { type: 'password' } };

/**
 * Each size with each set of enhancers and each trailing icon (none, the clear icon, the mask toggle): the lengths, the
 * fonts, the icons' sizes, and the height they make.
 */
function layout() {
  const cases = [];
  const t = THEMES.Light;
  for (const size of Object.values(SIZE)) {
    for (const [enhancers, e] of Object.entries(ENHANCERS)) {
      for (const [trailing, tr] of Object.entries(TRAILING)) {
        const props = { size, value: 'Value', ...e, ...tr };
        const r = render(props, t);
        const root = style('Root', t, r.Root), input = style('Input', t, r.Input);
        const font = (s) => ({ fontSize: css.px(s.fontSize), fontWeight: css.fontWeight(s.fontWeight), lineHeight: css.px(s.lineHeight), family: css.fontFamily(s.fontFamily) });
        const enhancer = (part) => (r[part] ? { padding: sides(style(part, t, r[part]), 'padding', ['Left', 'Right']), font: font(style(part, t, r[part])) } : null);
        const radii = new Set(['TopLeft', 'TopRight', 'BottomRight', 'BottomLeft'].map((c) => root[`border${c}Radius`]));
        if (radii.size !== 1) throw new Error('radii differ');
        const icon = (part, s) => { const w = css.px(s.width), h = css.px(s.height); if (w !== h) throw new Error(`${part} isn't square`); return w; };
        let clear = null, toggle = null;
        if (r.ClearIconContainer) {
          const c = style('ClearIconContainer', t, r.ClearIconContainer), i = style('ClearIcon', t, r.ClearIcon);
          clear = { padding: sides(c, 'padding', ['Left', 'Top', 'Right']), alignItems: c.alignItems, iconSize: icon('ClearIcon', i), cursor: i.cursor };
        }
        if (r.MaskToggleButton) {
          const b = style('MaskToggleButton', t, r.MaskToggleButton);
          // The hide icon renders once the toggle has unmasked the text: the same input with its mask off.
          const u = render(props, t, { unmasked: true });
          toggle = {
            padding: sides(b, 'padding', ['Left', 'Right']), alignItems: b.alignItems,
            showIconSize: icon('MaskToggleShowIcon', style('MaskToggleShowIcon', t, r.MaskToggleShowIcon)),
            hideIconSize: icon('MaskToggleHideIcon', style('MaskToggleHideIcon', t, u.MaskToggleHideIcon)),
          };
        }
        const inputPadding = sides(input, 'padding');
        const rootBorder = ['Left', 'Top', 'Right', 'Bottom'].map((side) => css.px(root[`border${side}Width`]));
        // The height a browser lays out: the root's flex row stretches every item to the tallest, the input: its
        // padding and its line (an input's text box is its line-height). The others are no taller (checked).
        const line = css.px(input.lineHeight);
        const inputHeight = inputPadding[1] + line + inputPadding[3];
        const others = [enhancer('StartEnhancer'), enhancer('EndEnhancer')].filter(Boolean).map((x) => x.font.lineHeight)
          .concat(clear ? [clear.iconSize + clear.padding[1]] : [], toggle ? [toggle.showIconSize, toggle.hideIconSize] : []);
        if (others.some((h) => h > inputHeight)) throw new Error(`${size}: an item taller than the input`);
        cases.push({
          size, enhancers, trailing,
          adjoined: r.Root.$adjoined, hasIconTrailing: r.Root.$hasIconTrailing,
          rootBorder, rootRadius: css.px([...radii][0]), rootPadding: sides(root, 'padding', ['Left', 'Right']), rootFont: font(root),
          startEnhancer: enhancer('StartEnhancer'), endEnhancer: enhancer('EndEnhancer'),
          containerFont: font(style('InputContainer', t, r.InputContainer)),
          inputPadding, inputFont: font(input),
          clear, toggle,
          height: rootBorder[1] + inputHeight + rootBorder[3],
        });
      }
    }
  }
  return cases;
}

/**
 * Which parts Base Web renders, and the labels it gives them: the clear icon by clearable, the value, disabled and
 * read-only (base-input.js's renderClear); the toggle by type, its icon and label by the mask state (renderMaskToggle).
 */
function parts() {
  const cases = [];
  for (const clearable of [false, true]) {
    for (const value of ['', 'Value']) {
      for (const disabled of [false, true]) {
        for (const readOnly of [false, true]) {
          for (const type of ['text', 'password']) {
            for (const masked of type === 'password' ? [true, false] : [true]) {
              const props = { clearable, value, disabled, readOnly, type };
              const r = render(props, THEMES.Light, { unmasked: !masked });
              const label = (part, key) => (r[part] ? r[part][key] ?? null : null);
              cases.push({
                clearable, value, disabled, readOnly, type, masked,
                rendered: PARTS.filter((p) => r[p]),
                clearLabel: label('ClearIcon', 'aria-label'), clearTitle: label('ClearIcon', 'title'), clearRole: label('ClearIcon', 'role'),
                clearTabIndex: label('ClearIcon', 'tabIndex'),
                toggleLabel: label('MaskToggleButton', 'aria-label'), toggleTitle: label('MaskToggleButton', 'title'),
                inputType: r.Input.type,
              });
            }
          }
        }
      }
    }
  }
  return cases;
}

/** An icon of Base Web's, rendered: its viewBox, its one path and the path's fill rule (SVG's default, nonzero, unless set). */
function icon(Component) {
  const html = markup(React.createElement(Component, { size: '24px' }), THEMES.Light);
  const view = /<svg\b[^>]*\sviewBox="([^"]+)"/.exec(html);
  const paths = [...html.matchAll(/<path\b([^>]*)>/g)];
  if (!view || paths.length !== 1) throw new Error(`${Component.displayName}: not one path in a viewBox`);
  const attr = (name) => (new RegExp(String.raw`\s${name}="([^"]*)"`).exec(paths[0][1]) || [])[1];
  return { d: attr('d'), viewBox: view[1].split(/[\s,]+/).map(Number), fillRule: attr('fill-rule') ?? 'nonzero' };
}

/** A part's transition: its CSS properties, seconds and curve. */
function transition(s) {
  return { properties: s.transitionProperty.split(',').map((p) => p.trim()), seconds: css.seconds(s.transitionDuration), curve: css.cubicBezier(s.transitionTimingFunction) };
}

function oracle(version) {
  const t = THEMES.Light;
  const r = render({ startEnhancer: '@', value: 'Value' }, t);
  return {
    baseui: version,
    colours: colours(),
    outlines: outlines(),
    layout: layout(),
    parts: parts(),
    icons: { clear: icon(DeleteAlt), show: icon(Show), hide: icon(Hide) },
    transitions: {
      root: transition(style('Root', t, r.Root)),
      enhancer: transition(style('StartEnhancer', t, r.StartEnhancer)),
      container: transition(style('InputContainer', t, r.InputContainer)),
    },
    // The defaults Metered's properties take: input.js's and base-input.js's defaultProps.
    defaults: {
      size: Input.defaultProps.size, type: Input.defaultProps.type, clearable: Input.defaultProps.clearable,
      clearOnEscape: BaseInput.defaultProps.clearOnEscape, error: BaseInput.defaultProps.error, positive: BaseInput.defaultProps.positive,
      placeholder: BaseInput.defaultProps.placeholder, disabled: Input.defaultProps.disabled, readOnly: Input.defaultProps.readOnly,
    },
  };
}

module.exports = { oracle };
