// The oracle for Select: what Base Web's own select gives (baseui/select), for the cases the tests build, on the real light
// and dark themes, in Avalonia's units. Kept apart from tools/tokens/select.js, which writes the themes: the tests compare
// one with the other. Read by cases.js, which writes tests/Metered.Tests/Reference/select.oracle.json.
//
// - What it renders: each case renders Base Web's Select itself (react-dom/server) with each of its parts overridden by a
//   component that records the props select-component.js gives it, in order, then evaluates the part's own style function
//   with those props. The dropdown is the Popover's content, which select-component.js gives the Popover as a function:
//   called and rendered the same way, its menu Base Web's own StatefulMenu. A multi value's tag is Base Web's own Tag,
//   rendered with the props multi-value.js gives it, its parts recorded too. Which parts render, in what order, their
//   text, ARIA and sizes, and every declaration, are Base Web's, with nothing decided here.
// - What it does: its state (isOpen, isFocused, isPseudoFocused, inputValue) and its value, from Base Web's own Select
//   class (select-component.js) driven step by step in Node, as React and a browser would drive it: each handler's setState
//   applied (its callback after), then componentDidUpdate; focus by the input's focus(), as a browser fires focus; the
//   document's click listener added after the focus that adds it (its setTimeout); the value kept as StatefulSelect keeps it.
'use strict';

const React = require('react');
const { Select, StatefulSelect } = require('baseui/select');
const SelectComponent = require('baseui/select/select-component').default;
const defaultProps = require('baseui/select/default-props').default;
const { normalizeOptions } = require('baseui/select/utils');
const defaultFilterOptions = require('baseui/select/utils/default-filter-options').default;
const sc = require('baseui/select/styled-components');
const { Tag } = require('baseui/tag');
const tagSc = require('baseui/tag/styled-components');
const menuSc = require('baseui/menu/styled-components');
const { Svg } = require('baseui/icon/styled-components');
const locale = require('baseui/select/locale').default;
const css = require('./css');
const { THEMES, evaluate, resolve } = require('./oracle');
const { markup: draw } = require('./input');

void StatefulSelect;

/** React's warning that a function component's defaultProps will go is Base Web's, not the case's. */
function markup(element, theme) {
  const error = console.error;
  console.error = (m, ...rest) => { if (!/Support for defaultProps will be removed/.test(String(m))) error(m, ...rest); };
  try {
    return draw(element, theme);
  } finally {
    console.error = error;
  }
}

/** Base Web's override keys for the select itself (types.d.ts's OverridesT), in the order select-component.js renders them. */
const SELECT_PARTS = ['Popover', 'Root', 'ControlContainer', 'SearchIconContainer', 'SearchIcon', 'ValueContainer', 'SingleValue', 'Tag',
  'InputContainer', 'Input', 'Placeholder', 'IconsContainer', 'LoadingIndicator', 'ClearIcon', 'SelectArrow'];
/** The dropdown's, and the menu's own that a select's dropdown shows (its group headers and empty state). */
const DROPDOWN_PARTS = ['DropdownContainer', 'Dropdown', 'DropdownListItem', 'OptionContent'];
const MENU_PARTS = ['OptgroupHeader', 'EmptyState'];
const TAG_PARTS = ['Root', 'Text', 'Action', 'ActionIcon'];

const recorder = (seen, name) => ({
  // eslint-disable-next-line react/display-name
  component: React.forwardRef((p, ref) => { seen.push({ part: name, props: p }); void ref; return React.createElement('div', null, p.children); }),
});

/** The text of a part's children: a string, a fragment's, an element's. */
function text(children) {
  if (children === undefined || children === null || typeof children === 'boolean') return '';
  if (typeof children === 'string' || typeof children === 'number') return String(children);
  if (Array.isArray(children)) return children.map(text).join('');
  if (children.type === React.Fragment) return text(children.props.children);
  return text(children.props?.children);
}

/**
 * Renders Base Web's Select with these props: every part it rendered, in order ({ part, props }), the dropdown's after
 * (rendered from the Popover's content, as the Popover renders it once open), and each tag's own parts ({ part: 'Tag.Root' }).
 */
function render(props, theme = THEMES.Light) {
  const seen = [];
  const overrides = {};
  for (const name of [...SELECT_PARTS, ...DROPDOWN_PARTS]) overrides[name] = recorder(seen, name);
  const menu = {};
  for (const name of MENU_PARTS) menu[name] = recorder(seen, name);
  overrides.StatefulMenu = { props: { overrides: menu } };
  const warn = console.warn;
  console.warn = (m, ...rest) => { if (!/both set to/.test(String(m))) warn(m, ...rest); };
  try {
    markup(React.createElement(Select, { ...props, overrides }), theme);
    const popover = seen.find((x) => x.part === 'Popover');
    if (props.dropdown) markup(popover.props.content(), theme);
    for (const t of seen.filter((x) => x.part === 'Tag')) {
      const tagSeen = [];
      const o = {};
      for (const name of TAG_PARTS) o[name] = { ...(t.props.overrides?.[name] || {}), ...recorder(tagSeen, 'Tag.' + name) };
      markup(React.createElement(Tag, { ...t.props, overrides: o }), theme);
      t.parts = tagSeen;
    }
  } finally {
    console.warn = warn;
  }
  return seen;
}

/** A styled component's style for its props, an override's $style merged over (Base Web's mergeOverride of style). */
function styleOf(styled, theme, props) {
  const s = styled.__STYLETRON__;
  // Every reducer, the base's first (styletron-react applies them last to first): withStyle's after the Spinner's own.
  let style = s.reducers.reduceRight((acc, r) => r.reducer(acc, { $theme: theme, ...props }), s.getInitialStyle());
  const extra = typeof props.$style === 'function' ? props.$style({ $theme: theme, ...props }) : props.$style;
  if (extra) style = merge(style, extra);
  return style;
}
const merge = (a, b) => {
  const out = { ...a };
  for (const [k, v] of Object.entries(b)) out[k] = v && typeof v === 'object' && a[k] && typeof a[k] === 'object' ? merge(a[k], v) : v;
  return out;
};
const flat = (s) => Object.fromEntries(Object.entries(s).filter(([, v]) => v === null || typeof v !== 'object'));

/** CSS's calc() of pixel lengths, as Base Web's padding writes it ('calc(12px - 2px)'), or a plain length. */
function length(v) {
  if (v === undefined || v === null) return 0;
  const s = String(v);
  if (!s.startsWith('calc(')) return css.px(v);
  // Sums and differences of pixel lengths, calc() inside calc() too ('calc(calc(40px - 2px) + 2px)').
  const tokens = s.replace(/calc\(/g, '(').match(/\(|\)|[-+]|\d+(?:\.\d+)?px/g);
  let i = 0;
  const term = () => {
    const t = tokens[i++];
    if (t === '(') { const x = sum(); if (tokens[i++] !== ')') throw new Error('calc ' + s); return x; }
    if (/px$/.test(t)) return Number(t.slice(0, -2));
    throw new Error('calc ' + s);
  };
  const sum = () => {
    let x = term();
    while (tokens[i] === '+' || tokens[i] === '-') { const op = tokens[i++]; const y = term(); x = op === '+' ? x + y : x - y; }
    return x;
  };
  const x = sum();
  if (i !== tokens.length) throw new Error('calc ' + s);
  return x;
}
const sides = (s, prefix) => ['Left', 'Top', 'Right', 'Bottom'].map((k) => length(s[prefix + k]));
const font = (s) => ({ fontSize: css.px(s.fontSize), fontWeight: css.fontWeight(s.fontWeight), lineHeight: css.px(s.lineHeight) });
const colour = (v) => (v === undefined || v === null ? null : css.color(v));

/** The parts of a render, by name: the select's first of each (a multi value's tags all). */
const first = (seen, part) => seen.find((x) => x.part === part)?.props;

const OPTIONS = [
  { id: 'AliceBlue', label: 'AliceBlue' }, { id: 'Aqua', label: 'Aqua' }, { id: 'Aquamarine', label: 'Aquamarine' },
  { id: 'Azure', label: 'Azure' }, { id: 'Beige', label: 'Beige', disabled: true }, { id: 'Bisque', label: 'Bisque' },
  { id: 'Black', label: 'Black' }, { id: 'BlanchedAlmond', label: 'BlanchedAlmond' },
];
// select-option-group.scenario.tsx's shape: groups by key, __ungrouped without a header.
const GROUPED = {
  __ungrouped: [{ id: 'Black', label: 'Black' }, { id: 'White', label: 'White' }],
  Blueish: [{ id: 'AliceBlue', label: 'AliceBlue' }, { id: 'Aqua', label: 'Aqua' }],
  Reddish: [{ id: 'Crimson', label: 'Crimson' }, { id: 'Red', label: 'Red' }],
};
const LISTS = { OPTIONS, GROUPED };
const optionsOf = (v) => (typeof v === 'string' ? LISTS[v] : v);
const pick = (labels) => (labels || []).map((l) => ({ ...OPTIONS.find((o) => o.label === l) || { id: l, label: l } }));

// ---- Structure ----

/** The cases whose parts the tests check: Base Web's scenarios and examples, and the states they show. */
const STRUCTURE = [
  { name: 'empty' },
  { name: 'a value', value: ['Aqua'] },
  { name: 'a value, not clearable', value: ['Aqua'], clearable: false },
  { name: 'a value, disabled', value: ['Aqua'], disabled: true },
  { name: 'loading', value: ['Aqua'], isLoading: true },
  { name: 'not searchable', value: ['Aqua'], searchable: false },
  { name: 'not searchable, empty', searchable: false },
  { name: 'search', type: 'search' },
  { name: 'search with a value', type: 'search', value: ['Aqua'] },
  { name: 'multi, empty', multi: true },
  { name: 'multi', multi: true, value: ['Aqua', 'Azure'] },
  { name: 'multi, a value not clearable', multi: true, value: ['Aqua', 'Azure'], unclearable: 'Azure' },
  { name: 'multi, disabled', multi: true, value: ['Aqua'], disabled: true },
  { name: 'a placeholder', placeholder: 'Choose a colour' },
  { name: 'named', 'aria-label': 'Colour', value: ['Aqua', 'Azure'], multi: true },
  { name: 'required, empty', required: true },
];

function selectProps(c) {
  const value = pick(c.value).map((v) => (v.label === c.unclearable ? { ...v, clearableValue: false } : v));
  const p = { options: optionsOf(c.options || 'OPTIONS'), value };
  for (const k of ['clearable', 'disabled', 'isLoading', 'searchable', 'type', 'multi', 'placeholder', 'aria-label', 'required', 'creatable',
    'size', 'error', 'positive', 'noResultsMsg', 'filterOutSelected', 'maxDropdownHeight', 'startOpen']) if (c[k] !== undefined) p[k] = c[k];
  return p;
}

function element(x) {
  const p = x.props;
  const e = { part: x.part };
  if (['SingleValue', 'Placeholder', 'OptgroupHeader', 'EmptyState', 'OptionContent'].includes(x.part)) e.text = text(p.children);
  if (x.part === 'Tag') {
    e.text = text(p.children);
    e.disabled = !!p.disabled;
    const root = x.parts.find((y) => y.part === 'Tag.Root').props;
    e.ariaLabel = root['aria-label'] ?? null;
    e.role = root.role ?? null;
    e.tabIndex = root.tabIndex ?? null;
    e.action = x.parts.some((y) => y.part === 'Tag.Action');
    e.actionIconSize = x.parts.find((y) => y.part === 'Tag.ActionIcon')?.props.size ?? null;
  }
  if (x.part === 'Input' || x.part === 'InputContainer') {
    for (const k of ['role', 'aria-label', 'aria-expanded', 'aria-haspopup', 'aria-autocomplete', 'aria-required', 'aria-disabled', 'tabIndex', 'value']) {
      if (p[k] !== undefined && p[k] !== null) e[k.replace(/-(\w)/g, (_, a) => a.toUpperCase())] = p[k];
    }
  }
  if (['ClearIcon', 'SelectArrow', 'SearchIcon'].includes(x.part)) {
    e.size = p.size ?? null;
    e.title = p.title ?? null;
    e.ariaLabel = p['aria-label'] ?? null;
    e.role = p.role ?? null;
  }
  if (x.part === 'LoadingIndicator') e.role = p.role;
  if (x.part === 'DropdownListItem') {
    e.text = text(p.children);
    e.role = p.role ?? null;
    e.highlighted = !!p.$isHighlighted;
    e.disabled = !!p.$disabled;
  }
  if (x.part === 'OptionContent') {
    e.selected = !!p.$selected;
    e.disabled = !!p.$disabled;
    e.highlighted = !!p.$isHighlighted;
    e.ariaSelected = !!p['aria-selected'];
  }
  if (x.part === 'Dropdown') {
    e.role = p.role ?? null;
    e.ariaMultiselectable = !!p['aria-multiselectable'];
  }
  return e;
}

function structure() {
  return STRUCTURE.map((c) => {
    const seen = render(selectProps(c));
    const pop = first(seen, 'Popover');
    return {
      ...c, value: c.value ?? [],
      popover: { accessibilityType: pop.accessibilityType, autoFocus: pop.autoFocus, focusLock: pop.focusLock, popoverMargin: pop.popoverMargin, placement: pop.placement },
      elements: seen.filter((x) => x.part !== 'Popover').map(element),
    };
  });
}

/** The dropdown's cases, open: what it lists, which option is highlighted first, which are selected. */
const DROPDOWN = [
  { name: 'options', value: [] },
  { name: 'a value: its option highlighted and selected', value: ['Azure'] },
  { name: 'a value not among the options: the first highlighted', value: ['Crimson'] },
  { name: 'multi: the chosen left out', multi: true, value: ['Aqua', 'Azure'] },
  { name: 'multi, filterOutSelected off: the first chosen highlighted', multi: true, value: ['Azure', 'Aqua'], filterOutSelected: false },
  { name: 'grouped', options: 'GROUPED', value: ['Aqua'] },
  { name: 'no options', options: [] },
  { name: 'no options, a message', options: [], noResultsMsg: 'Nothing here' },
  { name: 'compact', size: 'compact', value: ['Aqua'] },
  { name: 'a max height', maxDropdownHeight: '120px' },
];

function dropdown() {
  return DROPDOWN.map((c) => {
    const seen = render({ ...selectProps(c), dropdown: true });
    const list = first(seen, 'Dropdown');
    const container = first(seen, 'DropdownContainer');
    const lt = THEMES.Light;
    const listStyle = flat(styleOf(menuSc.StyledList, lt, list));
    return {
      ...c, options: typeof c.options === 'string' ? c.options : c.options === undefined ? 'OPTIONS' : null,
      optionsGiven: Array.isArray(c.options) ? c.options : null, value: c.value ?? [],
      maxHeight: listStyle.maxHeight ?? null, listId: list.id !== undefined && list.id !== null,
      // The container's width is the anchor's clientWidth once it is mounted (null in a server render): the select's.
      containerWidth: container.$width === null ? 'anchor' : container.$width,
      elements: seen.filter((x) => [...DROPDOWN_PARTS, ...MENU_PARTS].includes(x.part) && x.part !== 'DropdownContainer').map(element),
    };
  });
}

// ---- Colours ----

/** The control's states, by what select-component.js gives its parts (getSharedProps). */
const STATES = [
  ['rest', {}], ['focused', { $isFocused: true }], ['pseudoFocused', { $isPseudoFocused: true }], ['error', { $error: true }],
  ['positive', { $positive: true }], ['errorFocused', { $error: true, $isFocused: true }], ['disabled', { $disabled: true }],
  ['disabledError', { $disabled: true, $error: true }],
];

function colours() {
  const out = [];
  for (const theme of Object.keys(THEMES)) {
    const t = THEMES[theme];
    const seen = render(selectProps({ value: ['Aqua'], isLoading: true }), t);
    const shared = first(seen, 'ControlContainer');
    for (const [state, s] of STATES) {
      const p = { ...shared, ...s };
      const control = flat(styleOf(sc.StyledControlContainer, t, p));
      const placeholder = flat(styleOf(sc.StyledPlaceholder, t, p));
      const input = flat(styleOf(sc.StyledInput, t, p));
      // The arrow is Base Web's TriangleDown, its Svg override's colour select-component.js's (renderArrow).
      const arrowSeen = render(selectProps({ value: ['Aqua'], disabled: !!s.$disabled }), t);
      const arrowStyle = first(arrowSeen, 'SelectArrow').overrides.Svg.style({ $theme: t, ...p });
      const searchContainer = flat(styleOf(sc.StyledSearchIconContainer, t, p));
      out.push({
        theme, state,
        foreground: colour(control.color), border: colour(control.borderTopColor), background: colour(control.backgroundColor),
        placeholder: colour(placeholder.color), input: colour(input.color), arrow: colour(arrowStyle.color), search: colour(searchContainer.color),
        cursor: control.cursor,
      });
    }
  }
  return out;
}

/** The tag, the loading ring, the option's content, and the clear icon's fill, by theme. */
function surfaces() {
  return Object.keys(THEMES).map((theme) => {
    const t = THEMES[theme];
    const tags = (disabled) => {
      const seen = render(selectProps({ multi: true, value: ['Aqua'], disabled }), t);
      const tag = seen.find((x) => x.part === 'Tag');
      const root = tag.parts.find((y) => y.part === 'Tag.Root').props;
      const s = styleOf(tagSc.Root, t, root);
      const f = flat(s);
      const hover = s['@media (hover: hover)']?.[':hover::before'] ?? null;
      const active = s[':active::before'] ?? null;
      const focus = s[':focus'] ?? {};
      return {
        foreground: colour(f.color), background: colour(f.backgroundColor), border: colour(f.borderTopColor), borderWidth: css.px(f.borderTopWidth ?? 0),
        hover: hover ? colour(hover.backgroundColor) : null, active: active ? colour(active.backgroundColor) : null,
        focusRing: focus.boxShadow ? colour(/(#[0-9a-f]+|rgba?\([^)]*\))$/i.exec(evaluate(tagSc.Root, t, { ...root, $isFocusVisible: true })[':focus'].boxShadow)[1]) : null,
        cursor: f.cursor, actionCursor: flat(styleOf(tagSc.Action, t, tag.parts.find((y) => y.part === 'Tag.Action').props)).cursor,
      };
    };
    const seen = render(selectProps({ value: ['Aqua'], isLoading: true }), t);
    const ring = flat(styleOf(sc.StyledLoadingIndicator, t, first(seen, 'LoadingIndicator')));
    const content = (s) => flat(styleOf(sc.StyledOptionContent, t, s));
    const menuItem = (s) => flat(evaluate(menuSc.StyledListItemElement, t, { $size: 'default', ...s }));
    return {
      theme,
      tag: tags(false), tagDisabled: tags(true),
      ring: { top: colour(ring.borderTopColor), track: colour(ring.borderRightColor) },
      option: {
        selected: colour(content({ $selected: true }).color), selectedHighlighted: colour(content({ $selected: true, $isHighlighted: true }).color),
        plain: colour(content({}).color), listItem: colour(menuItem({}).color), listItemHighlighted: colour(menuItem({ $isHighlighted: true }).color),
        selectedWeight: css.fontWeight(content({ $selected: true }).fontWeight), plainWeight: css.fontWeight(content({}).fontWeight),
        disabledCursor: content({ $disabled: true }).cursor, cursor: content({}).cursor,
      },
    };
  });
}

// ---- Layout ----

const SIZES = ['mini', 'compact', 'default', 'large'];

/** Each size, single and multi, empty or not, select or search: the parts' boxes and fonts, and the height a browser gives it. */
function layout() {
  const t = THEMES.Light;
  const out = [];
  for (const size of SIZES) {
    for (const [kind, c] of [['single', {}], ['single, a value', { value: ['Aqua'] }], ['multi', { multi: true }], ['multi, values', { multi: true, value: ['Aqua', 'Azure'] }],
      ['search', { type: 'search' }], ['search, a value', { type: 'search', value: ['Aqua'] }], ['not searchable, a value', { searchable: false, value: ['Aqua'] }]]) {
      const seen = render(selectProps({ ...c, size, isLoading: true }), t);
      const p = (part) => first(seen, part);
      const root = flat(styleOf(sc.StyledRoot, t, p('Root')));
      const control = flat(styleOf(sc.StyledControlContainer, t, p('ControlContainer')));
      const value = flat(styleOf(sc.StyledValueContainer, t, p('ValueContainer')));
      const icons = flat(styleOf(sc.StyledIconsContainer, t, p('IconsContainer')));
      const inputContainer = flat(styleOf(sc.StyledInputContainer, t, p('InputContainer')));
      const input = p('Input') ? flat(styleOf(sc.StyledInput, t, p('Input'))) : null;
      const single = p('SingleValue') ? flat(styleOf(sc.StyledSingleValue, t, p('SingleValue'))) : null;
      const placeholder = p('Placeholder') ? flat(styleOf(sc.StyledPlaceholder, t, p('Placeholder'))) : null;
      const search = p('SearchIconContainer') ? flat(styleOf(sc.StyledSearchIconContainer, t, p('SearchIconContainer'))) : null;
      const ring = flat(styleOf(sc.StyledLoadingIndicator, t, p('LoadingIndicator')));
      const svg = (part) => (p(part) ? css.px(flat(evaluate(Svg, t, { $size: p(part).size })).width) : null);
      // The tag: Base Web's own, as multi-value.js gives it its props and its Root's margins.
      const tag = seen.find((x) => x.part === 'Tag');
      let tagBox = null;
      if (tag) {
        const tp = (part) => tag.parts.find((y) => y.part === 'Tag.' + part).props;
        const r = flat(styleOf(tagSc.Root, t, tp('Root')));
        const tx = flat(styleOf(tagSc.Text, t, tp('Text')));
        const a = flat(styleOf(tagSc.Action, t, tp('Action')));
        tagBox = {
          height: css.px(r.height), margin: sides(r, 'margin'), padding: sides(r, 'padding'), border: css.px(r.borderTopWidth ?? 0),
          radius: css.px(r.borderTopLeftRadius), font: font(r), textMaxWidth: css.px(tx.maxWidth), actionMargin: length(a.marginLeft),
          actionRadius: css.px(a.borderTopRightRadius), actionIcon: tp('ActionIcon').size,
          actionIconViewBox: '5 5 13.186 13.186',
        };
      }
      const pad = sides(value, 'padding');
      const border = sides(control, 'border').map((_, i) => css.px(control['border' + ['Left', 'Top', 'Right', 'Bottom'][i] + 'Width']));
      // The value container's line: its tallest item (the tags with their margins, or a line of text), centred.
      const lineHeight = css.px(root.lineHeight);
      const content = tagBox ? tagBox.height + tagBox.margin[1] + tagBox.margin[3] : lineHeight;
      out.push({
        size, kind, multi: !!c.multi, values: (c.value || []).length, type: c.type || 'select', searchable: c.searchable ?? true,
        font: font(root), radius: css.px(control.borderTopLeftRadius), border, padding: pad,
        iconsPadding: css.px(icons.paddingRight),
        inputContainer: { marginLeft: css.px(inputContainer.marginLeft), height: inputContainer.height === 'auto' ? null : css.px(inputContainer.height), maxHeight: css.px(inputContainer.maxHeight) },
        input: input ? { width: input.width, font: font(input) } : null,
        singleValue: single ? { marginLeft: css.px(single.marginLeft), lineHeight: single.lineHeight === 'inherit' ? null : css.px(single.lineHeight) } : null,
        placeholder: !!placeholder,
        search: search ? { left: css.px(search.left), size: css.px(search.width), icon: p('SearchIcon').size } : null,
        ring: { box: css.px(ring.width), border: css.px(ring.borderTopWidth) },
        clearIcon: svg('ClearIcon'), arrow: svg('SelectArrow'),
        tag: tagBox,
        height: border[1] + border[3] + pad[1] + pad[3] + content,
      });
    }
  }
  return out;
}

// ---- Behaviour ----

/**
 * Drives Base Web's Select (select-component.js) through a case's steps, as a browser and React would. Its DOM is a few
 * stand-ins: the input (focus() fires its focus event, as a browser's does, once), the anchor and the dropdown (what
 * contains what), the document (its click listener). Returns each step with the state after it and the value's changes.
 */
function run(c) {
  const anchorEls = new Set(['control', 'input', 'clear', 'tag']);
  const dropdownEls = new Set(['option']);
  // The DOM's Element, for isInteractive: an element's role and its parent.
  class Element {
    constructor(name, role = null) { this.name = name; this.role = role; this.tagName = 'DIV'; this.parentElement = null; }
    getAttribute(k) { return k === 'role' ? this.role : null; }
  }
  const el = (name, role) => new Element(name, role);
  const outside = el('outside');
  const listeners = {};
  global.Element = Element;
  global.document = {
    addEventListener: (type, fn) => { (listeners[type] ||= new Set()).add(fn); },
    removeEventListener: (type, fn) => { listeners[type]?.delete(fn); },
  };
  const realSet = global.setTimeout;
  const later = [];
  global.setTimeout = (fn) => { later.push(fn); return later.length; };
  const changes = [];
  const opens = [];
  let domFocus = false;
  let props = { ...defaultProps, options: optionsOf(c.options || 'OPTIONS'), value: pick(c.value), ...(c.props || {}) };
  props.onChange = (params) => {
    changes.push({ type: params.type, option: params.option ? params.option.label : null, value: params.value.map((v) => v.label) });
    // StatefulSelect's container: the value is the new one.
    setProps({ value: params.value });
  };
  props.onOpen = () => opens.push('open');
  props.onClose = () => opens.push('close');
  const inst = new SelectComponent(props);
  inst.props = props;
  let pending = [];
  inst.setState = (u, cb) => { pending.push([u, cb]); };
  const contains = (set) => ({ contains: (child) => !!child && set.has(child.name) });
  inst.anchor = { current: contains(anchorEls) };
  inst.dropdown = { current: contains(dropdownEls) };
  const input = { name: 'input', value: '', tagName: 'INPUT', focus() { if (!domFocus) { domFocus = true; event(() => inst.handleInputFocus({})); } }, blur() { if (domFocus) { domFocus = false; event(() => inst.handleBlur({ relatedTarget: null, target: input })); } } };
  inst.handleInputRef(input);
  const settle = () => {
    for (let guard = 0; pending.length; guard++) {
      if (guard > 20) throw new Error('setState does not settle');
      const prevState = inst.state;
      const prevProps = inst.props;
      const batch = pending;
      pending = [];
      let next = { ...inst.state };
      for (const [u] of batch) next = { ...next, ...(typeof u === 'function' ? u(next, inst.props) : u) };
      inst.state = next;
      inst.componentDidUpdate(prevProps, prevState);
      for (const [, cb] of batch) if (cb) cb();
    }
  };
  function setProps(p) {
    const prevProps = inst.props;
    props = { ...props, ...p };
    inst.props = props;
    inst.componentDidUpdate(prevProps, inst.state);
  }
  // A handler's updates are batched, a nested event's (focus() inside a click) with them, as React batches them.
  let depth = 0;
  function event(handler) {
    depth++;
    try {
      handler();
    } finally {
      depth--;
    }
    if (depth === 0) {
      settle();
      // The input is controlled: its value is the state's.
      input.value = inst.state.inputValue;
    }
  }
  /** The document's click listener after a click, as the click bubbles to it. */
  const documentClick = (target) => { for (const fn of [...(listeners.click || [])]) event(() => fn({ target, type: 'click' })); };
  const tick = () => { while (later.length) later.shift()(); };
  inst.componentDidMount();
  const out = [];
  try {
    for (const step of c.steps) {
      changes.length = 0;
      opens.length = 0;
      const click = (target) => ({ type: 'click', button: 0, target, preventDefault() {}, stopPropagation() {} });
      // A press on the control's box but the input moves a browser's focus off the input (to the page: no related target),
      // which handleBlur lets go as the input is in the anchor. A press on a tag focuses the tag, in the anchor too.
      const pressOff = () => { if (domFocus) { domFocus = false; event(() => inst.handleBlur({ relatedTarget: null, target: input })); } };
      if (step.click === 'control') {
        pressOff();
        event(() => inst.handleClick(click(el('control'))));
        documentClick(el('control'));
      } else if (step.click === 'input') {
        input.focus();
        event(() => inst.handleClick(click(input)));
        documentClick(input);
      } else if (step.tap === 'control') {
        // touchstart and touchend (handleTouchEnd's handleClick takes no touchend), then the mouse events and the click a
        // phone's browser sends after a tap.
        event(() => inst.handleTouchStart());
        event(() => inst.handleTouchEnd({ type: 'touchend', target: el('control') }));
        pressOff();
        event(() => inst.handleClick(click(el('control'))));
        documentClick(el('control'));
      } else if (step.drag === 'control') {
        // A drag: no click follows.
        event(() => inst.handleTouchStart());
        event(() => inst.handleTouchMove());
        event(() => inst.handleTouchEnd({ type: 'touchend', target: el('control') }));
      } else if (step.clear) {
        pressOff();
        event(() => inst.clearValue(click(el('clear'))));
        // The click bubbles on to the control, whose handleClick takes a role="button" as interactive, and to the document.
        event(() => inst.handleClick(click(el('clear', 'button'))));
        documentClick(el('clear'));
      } else if (step.choose !== undefined) {
        // An option clicked: the menu's onItemSelect, then the click on the document. The dropdown isn't the anchor.
        const options = inst.filterOptions(props.multi && props.filterOutSelected ? inst.getValueArray(props.value) : null);
        const item = options.find((o) => o.label === step.choose || (o.isCreatable && step.choose === 'Create ' + o.label));
        if (!item) throw new Error(c.name + ': no option ' + step.choose);
        // The press focuses the menu's list (its tabIndex), in the dropdown: handleBlur lets it go.
        if (domFocus) { domFocus = false; event(() => inst.handleBlur({ relatedTarget: el('option'), target: input })); }
        event(() => inst.selectValue({ item }));
        documentClick(el('option'));
      } else if (step.remove !== undefined) {
        const v = inst.getValueArray(props.value).find((x) => x.label === step.remove);
        // The press focuses the tag (its tabIndex), in the anchor; its action's click goes no further than the tag.
        if (domFocus) { domFocus = false; event(() => inst.handleBlur({ relatedTarget: el('tag'), target: input })); }
        event(() => inst.removeValue(v));
      } else if (step.clickOutside) {
        if (domFocus) { domFocus = false; event(() => inst.handleBlur({ relatedTarget: null, target: input })); }
        documentClick(outside);
      } else if (step.tapOutside) {
        // A finger's touchstart outside (the document's listener while open), then the blur and the click a tap makes.
        for (const fn of [...(listeners.touchstart || [])]) event(() => fn({ target: outside }));
        if (domFocus) { domFocus = false; event(() => inst.handleBlur({ relatedTarget: null, target: input })); }
        documentClick(outside);
      } else if (step.key !== undefined) {
        const codes = { Backspace: 8, Tab: 9, Escape: 27, ' ': 32, ArrowUp: 38, ArrowDown: 40, PageUp: 33, PageDown: 34, End: 35, Home: 36, Delete: 46, Enter: 13 };
        const open = !!inst.state.isOpen;
        let prevented = false;
        event(() => inst.handleKeyDown({ keyCode: codes[step.key], key: step.key, shiftKey: !!step.shift, preventDefault() { prevented = true; } }));
        // The input's own action, unless prevented: Backspace deletes the letter before the caret (at the end), a space
        // is typed. Delete at the end deletes nothing.
        if (!prevented && (props.searchable ?? true) && domFocus) {
          let typed = null;
          if (step.key === 'Backspace' && input.value) typed = input.value.slice(0, -1);
          else if (step.key === ' ') typed = input.value + ' ';
          if (typed !== null) event(() => inst.handleInputChange({ target: { value: typed } }));
        }
        // Escape's release goes to the popover's layer while it is open: onEsc, closeMenu.
        if (step.key === 'Escape' && open) event(() => inst.closeMenu());
        if (step.key === 'Tab' && domFocus) { domFocus = false; event(() => inst.handleBlur({ relatedTarget: outside, target: input })); }
      } else if (step.type !== undefined) {
        input.value = step.type;
        event(() => inst.handleInputChange({ target: { value: step.type } }));
      } else if (step.focus) {
        input.focus();
      } else if (step.blur) {
        input.blur();
      } else throw new Error('a step this oracle does not know: ' + JSON.stringify(step));
      tick();
      const valueArray = inst.getValueArray(props.value);
      inst.options = normalizeOptions(props.options);
      const options = inst.filterOptions(props.multi && props.filterOutSelected ? valueArray : null);
      out.push({
        ...step,
        isOpen: !!inst.state.isOpen, isFocused: !!inst.state.isFocused, isPseudoFocused: !!inst.state.isPseudoFocused, inputValue: inst.state.inputValue,
        value: valueArray.map((v) => v.label), changes: changes.map((x) => ({ ...x })), opens: [...opens],
        options: options.map((o) => (o.isCreatable ? 'Create “' + o.label + '”' : o.label)),
        placeholder: inst.shouldShowPlaceholder(), showValue: inst.shouldShowValue(),
      });
    }
  } finally {
    delete global.document;
    delete global.Element;
    global.setTimeout = realSet;
  }
  return out;
}

const k = (key) => ({ key });

/** The sequences the tests replay on Metered's select. */
const BEHAVIOUR = [
  { name: 'a click opens, a click again closes', steps: [{ click: 'control' }, { click: 'control' }, { click: 'control' }] },
  { name: 'a click on the input opens, and again keeps it open', steps: [{ click: 'input' }, { click: 'input' }] },
  { name: 'not searchable: a click toggles', props: { searchable: false }, steps: [{ click: 'control' }, { click: 'control' }, { click: 'control' }] },
  { name: 'openOnClick off: focus only', props: { openOnClick: false }, steps: [{ click: 'control' }, k('ArrowDown')] },
  { name: 'choose closes, and resets the input', steps: [{ click: 'control' }, { type: 'aq' }, { choose: 'Aquamarine' }, { click: 'control' }] },
  { name: 'closeOnSelect off keeps it open', props: { closeOnSelect: false }, steps: [{ click: 'control' }, { choose: 'Aqua' }, { choose: 'Azure' }] },
  { name: 'typing filters and opens', steps: [{ focus: true }, { type: 'a' }, { type: 'aqua' }, { type: 'zz' }, { type: '' }] },
  { name: 'typing, ignoreCase off', props: { ignoreCase: false }, steps: [{ focus: true }, { type: 'a' }, { type: 'A' }] },
  { name: 'Escape closes, pseudo-focused; Escape again clears', value: ['Aqua'], steps: [{ click: 'control' }, k('Escape'), k('Escape'), k('ArrowDown')] },
  { name: 'escapeClearsValue off', value: ['Aqua'], props: { escapeClearsValue: false }, steps: [{ focus: true }, k('Escape')] },
  { name: 'the keys that open', props: { searchable: false }, steps: [{ focus: true }, k(' '), k('Escape'), k('ArrowUp'), k('Escape'), k('PageDown'), k('Escape'), k('Home'), k('Escape'), { key: 'End', shift: true }, k('End')] },
  { name: 'Space types when searchable', steps: [{ focus: true }, k(' ')] },
  { name: 'Tab closes and blurs, the input kept for a reset off', props: { onBlurResetsInput: false }, steps: [{ focus: true }, { type: 'aq' }, k('Tab')] },
  { name: 'Tab closes and resets the input', steps: [{ focus: true }, { type: 'aq' }, k('Tab')] },
  { name: 'blur closes and resets', steps: [{ click: 'control' }, { type: 'b' }, { clickOutside: true }] },
  { name: 'a tap opens, a drag does not', steps: [{ drag: 'control' }, { tap: 'control' }, { tapOutside: true }] },
  { name: 'clear: the value and the input', value: ['Aqua'], steps: [{ click: 'control' }, { clear: true }, { click: 'control' }] },
  { name: 'Backspace takes the last value back to the input', multi: true, value: ['Aqua', 'Azure'], props: { multi: true }, steps: [{ focus: true }, k('Backspace'), k('Backspace'), k('Backspace')] },
  { name: 'backspaceClearsInputValue', multi: true, value: ['Aqua', 'Azure'], props: { multi: true, backspaceClearsInputValue: true }, steps: [{ focus: true }, k('Backspace')] },
  { name: 'Backspace with text deletes a letter', value: ['Aqua'], steps: [{ focus: true }, { type: 'b' }, k('Backspace')] },
  { name: 'Delete removes the last value', multi: true, value: ['Aqua', 'Azure'], props: { multi: true }, steps: [{ focus: true }, k('Delete'), k('Delete')] },
  { name: 'a value not clearable stays', multi: true, value: ['Aqua', 'Azure'], props: { multi: true }, unclearable: 'Azure', steps: [{ focus: true }, k('Backspace'), { clear: true }] },
  { name: 'multi: choose adds, again removes', props: { multi: true, closeOnSelect: false, filterOutSelected: false }, steps: [{ click: 'control' }, { choose: 'Aqua' }, { choose: 'Azure' }, { choose: 'Aqua' }] },
  { name: 'multi: a tag removed', multi: true, value: ['Aqua', 'Azure'], props: { multi: true }, steps: [{ remove: 'Aqua' }] },
  { name: 'creatable', props: { creatable: true }, steps: [{ focus: true }, { type: 'Teal' }, { choose: 'Create Teal' }, { type: 'aqua' }, { type: 'Aqua' }] },
  { name: 'creatable multi', props: { creatable: true, multi: true }, multi: true, steps: [{ focus: true }, { type: 'Teal' }, { choose: 'Create Teal' }, { type: 'Teal' }] },
  { name: 'a disabled option is not chosen', steps: [{ click: 'control' }, { choose: 'Beige' }] },
  { name: 'disabled: nothing opens', props: { disabled: true }, steps: [{ click: 'control' }, { focus: true }, k('ArrowDown')] },
  { name: 'onCloseResetsInput off keeps the text on Escape', props: { onCloseResetsInput: false }, steps: [{ focus: true }, { type: 'aq' }, k('Escape')] },
  { name: 'onSelectResetsInput off keeps the text', props: { onSelectResetsInput: false, closeOnSelect: false }, steps: [{ focus: true }, { type: 'aq' }, { choose: 'Aqua' }] },
];

function behaviour() {
  return BEHAVIOUR.map((c) => {
    const value = pick(c.value).map((v) => (v.label === c.unclearable ? { ...v, clearableValue: false } : v));
    const steps = run({ ...c, value: undefined, props: { ...(c.props || {}), value } });
    return { name: c.name, value: c.value ?? [], unclearable: c.unclearable ?? null, props: c.props ?? {}, steps };
  });
}

// ---- Filtering ----

/** defaultFilterOptions on Base Web's own: the labels it keeps for each filter. */
function filtering() {
  const cases = [
    ['', null], ['a', null], ['A', null], ['  aq ', null], ['zz', null], ['qua', null], ['.', null], ['Aqua', ['Aqua']], ['b', ['Bisque']],
  ];
  const out = cases.map(([filter, exclude]) => ({
    filter, exclude, ignoreCase: true,
    labels: defaultFilterOptions(OPTIONS, filter, exclude ? pick(exclude) : null, { valueKey: 'id', labelKey: 'label' }).map((o) => o.label),
  }));
  for (const filter of ['a', 'A', 'aq']) {
    out.push({ filter, exclude: null, ignoreCase: false,
      labels: defaultFilterOptions(OPTIONS, filter, null, { valueKey: 'id', labelKey: 'label', ignoreCase: false }).map((o) => o.label) });
  }
  return out;
}

function oracle(version) {
  return {
    baseui: version, lists: LISTS, locale, structure: structure(), dropdown: dropdown(), colours: colours(), surfaces: surfaces(),
    layout: layout(), behaviour: behaviour(), filtering: filtering(),
    transition: (() => {
      const s = flat(evaluate(sc.StyledControlContainer, THEMES.Light, { $size: 'default' }));
      return { property: s.transitionProperty, seconds: css.seconds(s.transitionDuration), curve: css.cubicBezier(s.transitionTimingFunction) };
    })(),
    clearSizes: (() => Object.fromEntries(SIZES.map((size) => [size, first(render(selectProps({ size, value: ['Aqua'] })), 'ClearIcon').size])))(),
  };
}

module.exports = { oracle, render, styleOf, SELECT_PARTS, DROPDOWN_PARTS, TAG_PARTS, length, sides, OPTIONS };
