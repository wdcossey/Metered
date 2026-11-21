// The oracle for Textarea: what Base Web's own textarea gives (baseui/textarea), for the cases the tests build, on the
// real light and dark themes, in Avalonia's units. Each case renders Base Web's Textarea itself (react-dom/server) with
// every part overridden by a component that records the props textarea.js and base-input.js give it, then evaluates each
// part's own style function with those props: textarea.js's StyledTextAreaRoot for the root, and the components it puts
// in BaseInput's place (StyledTextareaContainer, StyledTextarea) or BaseInput's own (the clear icon's). Which parts render,
// their labels, the icon's size and every declaration are Base Web's, with nothing decided here. Kept apart from
// tools/tokens/textarea.js, which writes the themes: the tests compare one with the other. Read by cases.js, which writes
// tests/Metered.Tests/Reference/textarea.oracle.json.
'use strict';

const React = require('react');
const { Textarea, SIZE } = require('baseui/textarea');
const BaseInput = require('baseui/input/base-input').default;
const sc = require('baseui/input/styled-components');
const ta = require('baseui/textarea/styled-components');
const { Svg } = require('baseui/icon/styled-components');
const DeleteAlt = require('baseui/icon/delete-alt').default;
const css = require('./css');
const { THEMES } = require('./oracle');
// Input's oracle's helpers: rendering, declarations, lengths, colours, the outline, the border, an icon, a transition.
const { markup, declarations, sides, colour, outline, border, icon, transition } = require('./input');

/**
 * Base Web's override keys a textarea can be given (types.d.ts's TextareaOverrides: Root and BaseInput's), in the order
 * textarea.js and base-input.js render them.
 */
const PARTS = ['Root', 'InputContainer', 'Before', 'Input', 'ClearIconContainer', 'ClearIcon', 'MaskToggleButton',
  'MaskToggleShowIcon', 'MaskToggleHideIcon', 'After'];

/**
 * Renders Base Web's Textarea with these props, and returns, for each part, the props it was given; a part not rendered
 * is missing. `autoFocus` is how a case is focused: textarea.js and base-input.js both start with
 * `isFocused: this.props.autoFocus`.
 */
function render(props, theme = THEMES.Light) {
  const seen = {};
  const overrides = {};
  for (const name of PARTS) {
    // eslint-disable-next-line react/display-name
    overrides[name] = { component: React.forwardRef((p, ref) => { (seen[name] ||= []).push(p); void ref; return React.createElement('div', null, p.children); }) };
  }
  markup(React.createElement(Textarea, { ...props, overrides }), theme);
  for (const [name, list] of Object.entries(seen)) {
    if (list.length !== 1) throw new Error(`${name}: rendered ${list.length} times`);
    seen[name] = list[0];
  }
  return seen;
}

/** Each part's declarations for the props Base Web gave it: the component textarea.js or base-input.js renders there. */
function style(part, theme, props) {
  switch (part) {
    case 'Root': return declarations(ta.StyledTextAreaRoot, theme, props).flat;
    case 'InputContainer': return declarations(ta.StyledTextareaContainer, theme, props).flat;
    case 'Input': {
      const { flat, s } = declarations(ta.StyledTextarea, theme, props, ['::placeholder']);
      return { ...flat, placeholderColor: s['::placeholder'].color };
    }
    case 'ClearIconContainer': return declarations(sc.StyledClearIconContainer, theme, props).flat;
    // styled(DeleteAlt, …): the icon's Svg style, then the styled function's own.
    case 'ClearIcon': return { ...declarations(Svg, theme, { $size: props.size }).flat, ...declarations(sc.StyledClearIcon, theme, props).flat };
    default: throw new Error(part);
  }
}

const font = (s) => ({ fontSize: css.px(s.fontSize), fontWeight: css.fontWeight(s.fontWeight), lineHeight: css.px(s.lineHeight), family: css.fontFamily(s.fontFamily) });

/**
 * The states the tests drive: Base Web's props, and focus (autoFocus, which the tests give by a real Tab). Error and
 * positive together draw error (getRootColors tests $error first).
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

/** Every part's colours, clearable with a value. */
function colours() {
  const cases = [];
  for (const theme of Object.keys(THEMES)) {
    for (const [state, extra] of STATES) {
      const t = THEMES[theme];
      const r = render({ clearable: true, value: 'Value', placeholder: 'Placeholder', ...extra }, t);
      const of = (part) => (r[part] ? style(part, t, r[part]) : null);
      const root = of('Root'), input = of('Input'), container = of('InputContainer');
      if (input['-webkit-text-fill-color'] !== undefined && input['-webkit-text-fill-color'] !== input.color) throw new Error('text fill differs from colour');
      cases.push({
        theme, state,
        rootBorder: border(root), rootBackground: colour(root.backgroundColor),
        containerForeground: colour(container.color), containerBackground: colour(container.backgroundColor),
        inputForeground: colour(input.color), caret: colour(input.caretColor), placeholder: colour(input.placeholderColor),
        inputCursor: input.cursor,
        clearIconContainer: r.ClearIconContainer ? colour(of('ClearIconContainer').color) : null,
      });
    }
  }
  return cases;
}

/** The clear icon's focus outline, which its own focus-visible state draws (base-input.js's isFocusVisibleForClear). */
function outlines() {
  const cases = [];
  for (const theme of Object.keys(THEMES)) {
    const t = THEMES[theme];
    const r = render({ clearable: true, value: 'Value' }, t);
    for (const focusVisible of [false, true]) {
      const s = style('ClearIcon', t, { ...r.ClearIcon, $isFocusVisible: focusVisible });
      cases.push({ theme, focusVisible, outline: outline(s.outline) });
    }
  }
  return cases;
}

/** Base Web's resize values the tests set: unset (its default), then CSS's. */
const RESIZE = [undefined, 'none', 'both', 'horizontal', 'vertical'];

/**
 * A layout case: the lengths, the fonts, the clear icon's and the textarea's own resize, and the height a browser lays
 * out: the textarea's padding and `rows` lines of its line-height (HTML's rows; a value that isn't above 0 is HTML's 2),
 * inside the root's border. The clear icon's container, a flex item of the same row, is no taller (checked).
 */
function layoutCase(props) {
  const t = THEMES.Light;
  const r = render({ value: 'Value', clearable: true, ...props }, t);
  const root = style('Root', t, r.Root), input = style('Input', t, r.Input);
  const radii = new Set(['TopLeft', 'TopRight', 'BottomRight', 'BottomLeft'].map((c) => root[`border${c}Radius`]));
  if (radii.size !== 1) throw new Error('radii differ');
  const c = style('ClearIconContainer', t, r.ClearIconContainer), i = style('ClearIcon', t, r.ClearIcon);
  if (css.px(i.width) !== css.px(i.height)) throw new Error('ClearIcon isn\'t square');
  const clear = { padding: sides(c, 'padding', ['Left', 'Top', 'Right']), alignItems: c.alignItems, iconSize: css.px(i.width), cursor: i.cursor };
  const inputPadding = sides(input, 'padding');
  const rootBorder = ['Left', 'Top', 'Right', 'Bottom'].map((side) => css.px(root[`border${side}Width`]));
  const rows = r.Input.rows;
  const lines = Number.isInteger(rows) && rows > 0 ? rows : 2;
  const inputHeight = inputPadding[1] + lines * css.px(input.lineHeight) + inputPadding[3];
  if (clear.padding[1] + clear.iconSize > inputHeight) throw new Error('the clear icon taller than the textarea');
  if (Object.keys(root).some((k) => /^font|^lineHeight/.test(k))) throw new Error('a font on the root');
  return {
    size: r.Input.$size, resize: props.resize ?? null, rows,
    rootBorder, rootRadius: css.px([...radii][0]), rootPadding: sides(root, 'padding', ['Left', 'Right']), rootWidth: root.width,
    containerFont: font(style('InputContainer', t, r.InputContainer)),
    inputPadding, inputFont: font(input), inputResize: input.resize,
    clear,
    height: rootBorder[1] + inputHeight + rootBorder[3],
  };
}

/** Each size with each resize value, Base Web's default rows; then other rows at the default size. */
function layout() {
  const cases = [];
  for (const size of Object.values(SIZE)) {
    for (const resize of RESIZE) cases.push(layoutCase({ size, resize }));
  }
  for (const rows of [1, 2, 5, 0]) cases.push(layoutCase({ rows }));
  return cases;
}

/**
 * Which parts Base Web renders, and the labels it gives them: the clear icon by clearable, the value, disabled and
 * read-only (base-input.js's renderClear); no mask toggle, a textarea's type being "textarea".
 */
function parts() {
  const cases = [];
  for (const clearable of [false, true]) {
    for (const value of ['', 'Value']) {
      for (const disabled of [false, true]) {
        for (const readOnly of [false, true]) {
          const r = render({ clearable, value, disabled, readOnly }, THEMES.Light);
          const label = (part, key) => (r[part] ? r[part][key] ?? null : null);
          cases.push({
            clearable, value, disabled, readOnly,
            rendered: PARTS.filter((p) => r[p]),
            clearLabel: label('ClearIcon', 'aria-label'), clearTitle: label('ClearIcon', 'title'), clearRole: label('ClearIcon', 'role'),
            clearTabIndex: label('ClearIcon', 'tabIndex'), alignTop: label('ClearIconContainer', '$alignTop'),
            inputType: r.Input.type, rows: r.Input.rows,
          });
        }
      }
    }
  }
  return cases;
}

function oracle(version) {
  const t = THEMES.Light;
  const r = render({ value: 'Value' }, t);
  const d = Textarea.defaultProps;
  return {
    baseui: version,
    colours: colours(),
    outlines: outlines(),
    layout: layout(),
    parts: parts(),
    icons: { clear: icon(DeleteAlt) },
    transitions: {
      root: transition(style('Root', t, r.Root)),
      container: transition(style('InputContainer', t, r.InputContainer)),
    },
    // The defaults Metered's properties take: textarea.js's defaultProps, and base-input.js's where it has none.
    defaults: {
      rows: d.rows, size: d.size, resize: d.resize ?? null, clearable: BaseInput.defaultProps.clearable,
      clearOnEscape: BaseInput.defaultProps.clearOnEscape, error: d.error, positive: BaseInput.defaultProps.positive,
      placeholder: d.placeholder, disabled: d.disabled, readOnly: d.readOnly,
    },
  };
}

module.exports = { oracle };
