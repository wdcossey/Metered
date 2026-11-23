// The oracle for Menu: what Base Web's own menu gives (baseui/menu), for the cases the tests build, on the real light and
// dark themes, in Avalonia's units. Kept apart from tools/tokens/menu.js, which writes the themes: the tests compare one
// with the other. Read by cases.js, which writes tests/Metered.Tests/Reference/menu.oracle.json.
//
// - What it renders: each case renders Base Web's StatefulMenu itself (react-dom/server) with each part overridden by a
//   component that records the props menu.js and option-list.js give it, in order, then evaluates the part's own style
//   function with those props. Which parts render, in what order, their text, states and ARIA, and every declaration,
//   are Base Web's, with nothing decided here.
// - What it does: its keyboard, pointer and focus state, from Base Web's own MenuStatefulContainer (stateful-container.js)
//   driven step by step in Node, as React would drive it: each handler's setState applied, then componentDidUpdate. The
//   DOM it reads is absent (typeof document is 'undefined' here), so its scrolling is left to the next part.
// - How it scrolls: Base Web's own scrollItemIntoView (utils.js), called on the geometry its styles give a list of
//   items (each option's padding and line height, the list's padding), for every key that moves the highlight.
'use strict';

const React = require('react');
const { StatefulMenu, OptionProfile } = require('baseui/menu');
const sc = require('baseui/menu/styled-components');
const { scrollItemIntoView } = require('baseui/menu/utils');
const locale = require('baseui/menu/locale').default;
const css = require('./css');
const { THEMES, evaluate, resolve } = require('./oracle');
const { markup: draw } = require('./input');

/** React's warning that a function component's defaultProps will go (stateful-container.js has them) is Base Web's, not the case's. */
function markup(element, theme) {
  const error = console.error;
  console.error = (m, ...rest) => { if (!/Support for defaultProps will be removed/.test(String(m))) error(m, ...rest); };
  try {
    return draw(element, theme);
  } finally {
    console.error = error;
  }
}

/** The parts recorded, by Base Web's override key, with the styled component whose function draws each. */
const STYLED = {
  List: sc.StyledList,
  OptgroupHeader: sc.StyledOptgroupHeader,
  ListItem: sc.StyledListItemElement,
  ListItemAnchor: sc.StyledListItemAnchor,
  MenuDivider: sc.StyledMenuDivider,
  EmptyState: sc.StyledEmptyState,
  ListItemProfile: sc.StyledListItemProfile,
  ProfileImgContainer: sc.StyledProfileImgContainer,
  ProfileImg: sc.StyledProfileImg,
  ProfileLabelsContainer: sc.StyledProfileLabelsContainer,
  ProfileTitle: sc.StyledProfileTitle,
  ProfileSubtitle: sc.StyledProfileSubtitle,
  ProfileBody: sc.StyledProfileBody,
};

/** A styled component's style for the props: its function's, or its style object (ProfileImgContainer, ProfileImg). */
function styleOf(part, theme, props) {
  const s = STYLED[part].__STYLETRON__;
  if (s.reducers.length === 0) return { ...s.getInitialStyle() };
  return evaluate(STYLED[part], theme, props);
}

/** The flat declarations, nested blocks left out: a browser's for an element no pseudo-class matches. */
const flat = (s) => Object.fromEntries(Object.entries(s).filter(([, v]) => v === null || typeof v !== 'object'));

/**
 * Renders Base Web's StatefulMenu with these props and returns every part it rendered, in order: { part, props }. The
 * Option override, when the case gives one, is kept and its own overrides get the recorders too (option-list.js and
 * option-profile.js read theirs from the Option's props, which menu.js fills with the menu's overrides).
 */
function render(props, theme = THEMES.Light) {
  const seen = [];
  const recorders = {};
  for (const name of Object.keys(STYLED)) {
    // eslint-disable-next-line react/display-name
    recorders[name] = { component: React.forwardRef((p, ref) => { seen.push({ part: name, props: p }); void ref; return React.createElement('div', null, p.children); }) };
  }
  const option = props.overrides?.Option;
  const overrides = { ...recorders, ...(option ? { Option: { ...option, props: { ...option.props, overrides: recorders } } } : {}) };
  markup(React.createElement(StatefulMenu, { ...props, overrides }), theme);
  return seen;
}

/** The text of a part's children: a string, a fragment's, or the anchor's label (recorded on its own). */
function text(children) {
  if (children === undefined || children === null || typeof children === 'boolean') return '';
  if (typeof children === 'string' || typeof children === 'number') return String(children);
  if (Array.isArray(children)) return children.map(text).join('');
  if (children.type === React.Fragment) return text(children.props.children);
  return text(children.props?.children);
}

const BASIC = [
  { label: 'Item One' }, { label: 'Item Two' }, { label: 'Item Three', disabled: true }, { label: 'Item Four', disabled: true },
  { label: 'Item Five' }, { label: 'Item Six' }, { label: 'Item Seven' }, { label: 'Item Eight' }, { label: 'Item Nine' },
  { label: 'Item Ten' }, { label: 'Item Eleven' }, { label: 'Item Twelve' },
];
const DIVIDERS = [
  { label: 'Menu option A' }, { label: 'Menu option B' }, { divider: true }, { label: 'Menu option X' }, { label: 'Menu option Y' },
  { label: 'Menu option Z' }, { divider: true }, { label: 'Menu option 1' }, { label: 'Menu option 2' }, { label: 'Menu option 3' },
];
// grouped.tsx: the items are { id, color }, labelled by getItemLabel: item.id.
const GROUPED = {
  __ungrouped: [{ id: 'Black', color: '#000000' }],
  Blueish: [{ id: 'AliceBlue', color: '#F0F8FF' }, { id: 'Aqua', color: '#00FFFF' }, { id: 'Aquamarine', color: '#7FFFD4' }],
  Whiteish: [{ id: 'AntiqueWhite', color: '#FAEBD7' }, { id: 'Azure', color: '#F0FFFF' }, { id: 'Beige', color: '#F5F5DC' }],
};
const HREF = [
  { label: 'Apple', href: '//www.example.com/apple' }, { label: 'Orange', href: '//www.example.com/orange' },
  { label: 'Strawberry', href: '//www.example.com/strawberry' },
];
const FRUIT = [{ label: 'Apple' }, { label: 'Apricot' }, { label: 'Banana' }, { label: 'Blueberry' }, { label: 'Cherry' }, { label: 'Pineapple' }];
const PROFILE = Array.from({ length: 4 }, () => ({ title: 'David Smith', subtitle: 'Senior Engineering Manager', body: 'Uber Everything' }));

const LISTS = { BASIC, DIVIDERS, GROUPED, HREF, FRUIT, PROFILE };

/** A getItemLabel the tests give by name: grouped.tsx's. */
const LABELS = { id: (item) => item.id };

/** Option props by name, as the tests' Option override gives them. */
function optionOf(c) {
  const p = {};
  if (c.size) p.size = c.size;
  if (c.label) p.getItemLabel = LABELS[c.label];
  if (c.renderHrefAsAnchor === false) p.renderHrefAsAnchor = false;
  return Object.keys(p).length ? { Option: { props: p } } : {};
}

/** Profile options, as menu-profile-menu.scenario.tsx gives them (no image). */
const profileOption = () => ({
  Option: {
    component: OptionProfile,
    props: {
      getProfileItemLabels: ({ title, subtitle, body }) => ({ title, subtitle, body }),
      getProfileItemImg: () => undefined,
      getProfileItemImgText: (item) => item.title,
    },
  },
});

/** The cases whose structure the tests check: Base Web's scenarios and examples, and the states they show. */
const STRUCTURE = [
  { name: 'basic', items: 'BASIC' },
  { name: 'basic highlighted', items: 'BASIC', initialState: { highlightedIndex: 4 } },
  { name: 'basic highlighted and focused', items: 'BASIC', initialState: { highlightedIndex: 4, isFocused: true } },
  { name: 'basic, a disabled item highlighted and focused', items: 'BASIC', initialState: { highlightedIndex: 2, isFocused: true } },
  { name: 'compact', items: 'BASIC', size: 'compact' },
  { name: 'dividers', items: 'DIVIDERS', initialState: { highlightedIndex: 3, isFocused: true } },
  { name: 'grouped', items: 'GROUPED', label: 'id', initialState: { highlightedIndex: 1, isFocused: true } },
  { name: 'empty', items: [] },
  { name: 'empty groups', items: { __ungrouped: [], Blueish: [] } },
  { name: 'empty, with a message', items: [], noResultsMsg: 'Nothing here' },
  { name: 'href', items: 'HREF', initialState: { highlightedIndex: 0, isFocused: true } },
  { name: 'href, not as an anchor', items: 'HREF', renderHrefAsAnchor: false },
  { name: 'named', items: 'FRUIT', 'aria-label': 'Fruit' },
  { name: 'profile', items: 'PROFILE', profile: true, initialState: { highlightedIndex: 1, isFocused: true } },
];

const itemsOf = (v) => (typeof v === 'string' ? LISTS[v] : v);

function structure() {
  return STRUCTURE.map((c) => {
    const props = {
      items: itemsOf(c.items),
      overrides: c.profile ? profileOption() : optionOf(c),
      ...(c.initialState ? { initialState: c.initialState } : {}),
      ...(c.noResultsMsg ? { noResultsMsg: c.noResultsMsg } : {}),
      ...(c['aria-label'] ? { 'aria-label': c['aria-label'] } : {}),
    };
    const seen = render(props);
    const list = seen.filter((x) => x.part === 'List');
    if (list.length !== 1) throw new Error(c.name + ': ' + list.length + ' lists');
    const L = list[0].props;
    const elements = [];
    for (const { part, props: p } of seen) {
      if (part === 'List' || part === 'ListItemAnchor' || part.startsWith('Profile')) continue;
      const e = { part };
      if (part === 'OptgroupHeader' || part === 'EmptyState') e.text = text(p.children);
      if (part === 'ListItem' || part === 'ListItemProfile') {
        e.text = text(p.children);
        e.size = p.$size ?? null;
        e.highlighted = !!p.$isHighlighted;
        e.focused = !!p.$isFocused;
        e.disabled = !!p.$disabled;
        e.ariaSelected = !!p['aria-selected'];
        e.ariaDisabled = !!p['aria-disabled'];
        e.role = p.role ?? null;
        const anchor = seen.find((x) => x.part === 'ListItemAnchor' && x.props.$item?.label === p.item.label && x.props.href);
        e.href = anchor && p.item.href ? anchor.props.href : null;
      }
      if (part === 'EmptyState') { e.ariaLive = p['aria-live']; }
      elements.push(e);
    }
    const profile = c.profile ? seen.filter((x) => x.part === 'ProfileTitle' || x.part === 'ProfileSubtitle' || x.part === 'ProfileBody')
      .map((x) => ({ part: x.part, text: text(x.props.children) })) : [];
    return {
      name: c.name,
      // What the case gives: its items (by name, or as they are), its Option props, its state and its message.
      items: typeof c.items === 'string' ? c.items : null, itemsGiven: typeof c.items === 'string' ? null : c.items,
      size: c.size ?? null, label: c.label ?? null, renderHrefAsAnchor: c.renderHrefAsAnchor ?? true, profile: !!c.profile,
      initialState: c.initialState ?? null, noResultsMsg: c.noResultsMsg ?? null, ariaLabelGiven: c['aria-label'] ?? null,
      role: L.role, ariaLabel: L['aria-label'], tabIndex: L.tabIndex, elements, profileText: profile,
    };
  });
}

// ---- Colours ----

/** The states an option is drawn in, by what menu.js gives it: the item highlighted (by index), the menu focused. */
const OPTION_STATES = [
  ['rest', { index: 1 }],
  ['highlighted', { index: 0, highlighted: true }],
  ['highlightedFocused', { index: 0, highlighted: true, focused: true }],
  ['disabled', { index: 2 }],
  ['disabledHighlighted', { index: 2, highlighted: true }],
  ['disabledHighlightedFocused', { index: 2, highlighted: true, focused: true }],
];

const colour = (v) => css.color(v);

function colours() {
  const cases = [];
  for (const theme of Object.keys(THEMES)) {
    const t = THEMES[theme];
    for (const [state, s] of OPTION_STATES) {
      const initialState = { highlightedIndex: s.highlighted ? s.index : -1, isFocused: !!s.focused };
      const items = [{ label: 'A', href: '#a' }, { label: 'B' }, { label: 'C', disabled: true }];
      const seen = render({ items, initialState }, t);
      const options = seen.filter((x) => x.part === 'ListItem');
      const o = options[s.index].props;
      const decl = flat(styleOf('ListItem', t, o));
      if (!!o.$isHighlighted !== !!s.highlighted || !!o.$isFocused !== !!s.focused || !!o.$disabled !== (s.index === 2)) throw new Error(state + ': props');
      const anchors = seen.filter((x) => x.part === 'ListItemAnchor');
      cases.push({
        theme, state,
        foreground: colour(decl.color), background: colour(decl.backgroundColor), cursor: decl.cursor,
        // The first item's anchor, in the state the first item is in (rest, or highlighted).
        anchor: s.index === 0 || state === 'rest' ? colour(flat(styleOf('ListItemAnchor', t, anchors[0].props)).color) : null,
      });
    }
  }
  return cases;
}

/** The list, the header, the divider, the empty state and the profile's parts, by theme. */
function surfaces() {
  return Object.keys(THEMES).map((theme) => {
    const t = THEMES[theme];
    const grouped = render({ items: { __ungrouped: [], G: [{ label: 'a' }, { divider: true }] } }, t);
    const list = styleOf('List', t, grouped.find((x) => x.part === 'List').props);
    const focusList = evaluate(sc.StyledList, t, { $isFocusVisible: true });
    const outline = /^(\S+) solid (\S+)$/.exec(focusList[':focus'].outline);
    if (!outline || list[':focus'].outline !== 'none') throw new Error('List outline');
    const header = flat(styleOf('OptgroupHeader', t, grouped.find((x) => x.part === 'OptgroupHeader').props));
    const divider = flat(styleOf('MenuDivider', t, grouped.find((x) => x.part === 'MenuDivider').props));
    const empty = flat(styleOf('EmptyState', t, render({ items: [] }, t).find((x) => x.part === 'EmptyState').props));
    const profile = render({ items: PROFILE.slice(0, 1), overrides: profileOption() }, t);
    const of = (part) => styleOf(part, t, profile.find((x) => x.part === part).props);
    const item = of('ListItemProfile');
    return {
      theme,
      list: colour(list.backgroundColor), listShadow: css.boxShadows(list.boxShadow), listFocusOutline: colour(outline[2]),
      header: colour(header.color), divider: colour(divider.borderBottomColor), empty: colour(empty.color),
      profileHover: colour(resolve(item, {}).self.backgroundColor ?? 'transparent') === '#00000000' ? colour(item[':hover'].backgroundColor) : null,
      profileTitle: colour(of('ProfileTitle').color), profileSubtitle: colour(of('ProfileSubtitle').color), profileBody: colour(of('ProfileBody').color),
    };
  });
}

// ---- Layout ----

const font = (s) => ({ fontSize: css.px(s.fontSize), fontWeight: css.fontWeight(s.fontWeight), lineHeight: css.px(s.lineHeight) });
const pad = (s) => ['Left', 'Top', 'Right', 'Bottom'].map((k) => css.px(s['padding' + k] ?? 0));
const margin = (s) => ['Left', 'Top', 'Right', 'Bottom'].map((k) => css.px(s['margin' + k] ?? 0));

function layout() {
  const t = THEMES.Light;
  const sizes = ['default', 'compact'].map((size) => {
    const seen = render({ items: [{ label: 'a' }], overrides: optionOf({ size }) });
    const o = flat(styleOf('ListItem', t, seen.find((x) => x.part === 'ListItem').props));
    return { size, padding: pad(o), font: font(o) };
  });
  const g = render({ items: { __ungrouped: [], G: [{ label: 'a' }, { divider: true }] } });
  const list = flat(styleOf('List', t, g.find((x) => x.part === 'List').props));
  const header = flat(styleOf('OptgroupHeader', t, g.find((x) => x.part === 'OptgroupHeader').props));
  const divider = flat(styleOf('MenuDivider', t, g.find((x) => x.part === 'MenuDivider').props));
  const empty = flat(styleOf('EmptyState', t, render({ items: [] }).find((x) => x.part === 'EmptyState').props));
  const focusList = evaluate(sc.StyledList, t, { $isFocusVisible: true });
  const p = render({ items: PROFILE.slice(0, 1), overrides: profileOption() });
  const of = (part) => flat(styleOf(part, t, p.find((x) => x.part === part)?.props ?? {}));
  const img = flat(styleOf('ProfileImg', t, {}));
  return {
    options: sizes,
    list: {
      padding: pad(list), radius: css.px(list.borderTopLeftRadius),
      focusOutlineWidth: css.px(/^(\S+) solid/.exec(focusList[':focus'].outline)[1]),
    },
    header: { padding: pad(header), font: font(header) },
    divider: { margin: margin(divider), borderWidth: css.px(divider.borderBottomWidth), height: css.px(divider.height) },
    empty: { padding: pad(empty), font: font(empty), textAlign: empty.textAlign },
    profile: {
      padding: pad(of('ListItemProfile')),
      imgContainer: [css.px(of('ProfileImgContainer').width), css.px(of('ProfileImgContainer').height)],
      imgRadius: img.borderTopLeftRadius,
      labelsMarginLeft: css.px(of('ProfileLabelsContainer').marginLeft),
      title: font(of('ProfileTitle')), subtitle: font(of('ProfileSubtitle')), body: font(of('ProfileBody')),
    },
    transition: (() => {
      const o = flat(styleOf('ListItem', t, render({ items: [{ label: 'a' }] }).find((x) => x.part === 'ListItem').props));
      return { property: o.transitionProperty, seconds: css.seconds(o.transitionDuration), curve: css.cubicBezier(o.transitionTimingFunction) };
    })(),
    locale,
  };
}

// ---- Behaviour ----

/**
 * Base Web's MenuStatefulContainerInner, the class stateful-container.js keeps to itself: a fresh copy of the module,
 * loaded with React.createElement watched, renders it once, which names its class.
 */
function containerClass() {
  const file = require.resolve('baseui/menu/stateful-container');
  const cached = require.cache[file];
  delete require.cache[file];
  const create = React.createElement;
  let Inner = null;
  React.createElement = function watched(type, ...rest) {
    if (typeof type === 'function' && type.name === 'MenuStatefulContainerInner') Inner = type;
    return create.call(this, type, ...rest);
  };
  try {
    const SC = require('baseui/menu/stateful-container').default;
    markup(React.createElement(SC, { items: [] }), THEMES.Light);
  } finally {
    React.createElement = create;
    if (cached) require.cache[file] = cached; else delete require.cache[file];
  }
  if (!Inner) throw new Error('MenuStatefulContainerInner not found');
  return Inner;
}

/** Fake timers, for type-ahead's 500 ms: time moves only by a step's wait. */
function clock() {
  let now = 0, id = 0;
  const timers = new Map();
  return {
    setTimeout: (fn, ms) => { timers.set(++id, { at: now + ms, fn }); return id; },
    clearTimeout: (i) => { timers.delete(i); },
    advance(ms) {
      const end = now + ms;
      for (;;) {
        const due = [...timers.entries()].filter(([, t]) => t.at <= end).sort((a, b) => a[1].at - b[1].at)[0];
        if (!due) break;
        timers.delete(due[0]);
        now = due[1].at;
        due[1].fn();
      }
      now = end;
    },
  };
}

/** Reducers the tests give by name. */
const REDUCERS = {
  // Type-ahead ignored: its change refused, the state kept.
  noCharacter: (type, changes, state) => (type === 'character' ? state : changes),
};

/**
 * Runs a case: Base Web's container made with the case's props, mounted, then each step, as a browser and React would
 * run it. Each event's handlers run, their setState calls merged and applied when the event is over (React batches them),
 * then componentDidUpdate, again while it sets state. The list's DOM focus is followed too: focusMenu calls
 * rootRef.focus(), whose focus event runs onFocus (focusMenu again, on the state not yet applied); a key reaches the
 * container only when the list has the focus and the menu is focused (menu.js's onKeyDown); a blur only when it has it.
 * The steps:
 * - focus: the list focused by keyboard (onFocus); blur: the focus moved elsewhere (onBlur);
 * - key: a key pressed on the focused list (its DOM name);
 * - over: a mouse onto the list, not onto an option (mouseover: focusMenu);
 * - enter: a mouse onto an option (mouseover, which bubbles to the list: focusMenu; then mouseenter);
 * - click: a click on the option the mouse is on, with no move (mousedown, which focuses the list; then click);
 * - wait: time passing (type-ahead's 500 ms); items: the menu given other items.
 * Each step's result: the highlighted index, whether the menu is focused, the items selected and the change types the
 * reducer saw.
 */
function run(Inner, c) {
  const items = itemsOf(c.items);
  const time = clock();
  const realSet = global.setTimeout, realClear = global.clearTimeout;
  global.setTimeout = time.setTimeout;
  global.clearTimeout = time.clearTimeout;
  const selected = [];
  const types = [];
  const reducer = c.reducer ? REDUCERS[c.reducer] : (type, changes) => changes;
  let domFocus = false;
  let inst = null;
  let props = {
    ...Inner.defaultProps,
    items,
    uidSeed: (i) => 'item-' + i,
    initialState: { ...Inner.defaultProps.initialState, ...(c.initialState || {}) },
    typeAhead: c.typeAhead ?? true,
    forceHighlight: !!c.forceHighlight,
    stateReducer: (type, changes, state) => { types.push(type); return reducer(type, changes, state); },
    onItemSelect: ({ item }) => { selected.push(flatItems(props.items).indexOf(item)); },
    // focus() on the list: its focus event, when it didn't have the focus.
    rootRef: { current: { contains: () => true, focus() { if (!domFocus) { domFocus = true; inst.focusMenu({ target: {} }); } } } },
  };
  inst = new Inner(props);
  let pending = null;
  inst.setState = (u) => { const next = typeof u === 'function' ? u({ ...inst.state, ...(pending || {}) }, inst.props) : u; pending = { ...(pending || {}), ...next }; };
  const settle = (prevProps) => {
    let before = prevProps;
    for (let guard = 0; pending; guard++) {
      if (guard > 10) throw new Error('componentDidUpdate does not settle');
      const prevState = inst.state;
      inst.state = { ...inst.state, ...pending };
      pending = null;
      inst.componentDidUpdate(before, prevState);
      before = inst.props;
    }
  };
  // React renders, then mounts, before any step; getRequiredItemProps runs for every option on each render.
  const renderItems = () => flatItems(props.items).forEach((item, i) => { if (!item.divider) inst.getRequiredItemProps(item, i); });
  /** One DOM event: its handler, then React's update. */
  const event = (handler) => { const prevProps = inst.props; handler(); settle(prevProps); renderItems(); };
  const option = (i) => inst.getRequiredItemProps(flatItems(props.items)[i], i);
  renderItems();
  inst.componentDidMount();
  const out = [];
  try {
    for (const step of c.steps) {
      types.length = 0;
      const before = selected.length;
      const ev = { preventDefault() {}, keyCode: 0, target: {} };
      if (step.key !== undefined) {
        if (domFocus) event(() => { if (inst.state.isFocused) inst.onKeyDown({ ...ev, key: step.key }); });
      } else if (step.focus) {
        if (!domFocus) { domFocus = true; event(() => inst.focusMenu(ev)); }
      } else if (step.blur) {
        if (domFocus) { domFocus = false; event(() => inst.unfocusMenu()); }
      } else if (step.over) event(() => inst.focusMenu(ev));
      else if (step.enter !== undefined) {
        event(() => inst.focusMenu(ev));
        // getRequiredItemProps attaches onMouseEnter only to an item that isn't disabled.
        event(() => { const p = option(step.enter); if (p.onMouseEnter) p.onMouseEnter(ev); });
      } else if (step.click !== undefined) {
        if (!domFocus) { domFocus = true; event(() => inst.focusMenu(ev)); }
        event(() => { const p = option(step.click); if (p.onClick) p.onClick(ev); });
      } else if (step.wait !== undefined) time.advance(step.wait);
      else if (step.items !== undefined) {
        const prevProps = inst.props;
        props = { ...props, items: itemsOf(step.items) };
        inst.props = props;
        // A new prop is a render, and componentDidUpdate after it.
        inst.componentDidUpdate(prevProps, inst.state);
        settle(props);
        renderItems();
      } else throw new Error('a step this oracle does not know: ' + JSON.stringify(step));
      out.push({ ...step, highlightedIndex: inst.state.highlightedIndex, isFocused: inst.state.isFocused, selected: selected.slice(before), changes: [...types] });
    }
  } finally {
    global.setTimeout = realSet;
    global.clearTimeout = realClear;
  }
  return out;
}

function flatItems(items) {
  return Array.isArray(items) ? items : Object.keys(items).reduce((o, k) => o.concat(items[k]), []);
}

const k = (key) => ({ key });
const keys = (...ks) => ks.map(k);

/** The sequences the tests replay on Metered's menu: keys, focus, pointer, type-ahead, the props that change the range. */
const BEHAVIOUR = [
  { name: 'arrows, Home and End', items: 'BASIC', steps: [{ focus: true }, ...keys('ArrowDown', 'ArrowDown', 'ArrowDown', 'ArrowUp', 'End', 'ArrowDown', 'Home', 'ArrowUp', 'ArrowLeft', 'ArrowRight')] },
  { name: 'keys before focus do nothing', items: 'BASIC', steps: [...keys('ArrowDown', 'End'), { focus: true }, k('ArrowDown')] },
  { name: 'Enter selects, not a disabled item', items: 'BASIC', steps: [{ focus: true }, k('Enter'), k('ArrowDown'), k('ArrowDown'), k('Enter'), k('ArrowDown'), k('Enter'), k('End'), k('Enter')] },
  { name: 'blur keeps the highlight, focus again keeps it', items: 'BASIC', steps: [{ focus: true }, k('ArrowDown'), { blur: true }, k('ArrowDown'), { focus: true }, k('ArrowDown')] },
  { name: 'dividers take an index', items: 'DIVIDERS', steps: [{ focus: true }, k('ArrowDown'), k('ArrowDown'), k('Enter'), k('ArrowDown'), k('End'), k('ArrowUp'), k('ArrowUp'), k('ArrowUp'), k('ArrowUp')] },
  { name: 'grouped: Tab then ArrowDown highlights AliceBlue', items: 'GROUPED', steps: [{ focus: true }, k('ArrowDown'), k('End'), k('Enter')] },
  { name: 'type-ahead: prefix, then infix', items: 'FRUIT', steps: [{ focus: true }, k('b'), k('l'), { wait: 700 }, k('c'), { wait: 700 }, k('a'), k('p'), { wait: 700 }, k('n'), k('e'), { wait: 700 }, k('z'), { wait: 700 }, k('P')] },
  { name: 'type-ahead: letters less than 500 ms apart add up', items: 'FRUIT', steps: [{ focus: true }, k('a'), { wait: 300 }, k('p'), { wait: 300 }, k('r'), { wait: 700 }, k('b')] },
  { name: 'type-ahead: a key name joins the letters', items: 'FRUIT', steps: [{ focus: true }, k('Shift'), k('B'), { wait: 700 }, k('C'), { wait: 700 }, k('Escape'), k('a'), { wait: 700 }, k('Control'), { wait: 700 }, k('Backspace'), k('b')] },
  { name: 'type-ahead: a space joins the words', items: 'BASIC', steps: [{ focus: true }, ...keys('i', 't', 'e', 'm', ' ', 't', 'h')] },
  { name: 'type-ahead: none without labels', items: 'GROUPED', steps: [{ focus: true }, k('a'), k('ArrowDown'), k('b')] },
  { name: 'type-ahead off', items: 'FRUIT', typeAhead: false, steps: [{ focus: true }, k('c'), k('ArrowDown')] },
  { name: 'a mouse over the menu, over options, clicks', items: 'BASIC', steps: [{ over: true }, { enter: 5 }, { enter: 2 }, { click: 2 }, { enter: 6 }, { click: 6 }, { enter: 8 }, { blur: true }, { enter: 9 }, { click: 9 }, k('ArrowDown')] },
  { name: 'a click on the list blurred focuses it, then chooses', items: 'BASIC', initialState: { highlightedIndex: 3 }, steps: [{ enter: 4 }, { click: 4 }, k('ArrowDown'), { blur: true }, { click: 4 }, k('ArrowUp')] },
  { name: 'initial highlight, focused but not the list', items: 'BASIC', initialState: { highlightedIndex: 5, isFocused: true }, steps: [k('ArrowDown'), { over: true }, k('ArrowDown'), { focus: true }, k('ArrowDown'), k('Home'), { blur: true }] },
  { name: 'initial highlight, unfocused', items: 'BASIC', initialState: { highlightedIndex: 5 }, steps: [k('ArrowDown'), { focus: true }, k('ArrowDown')] },
  { name: 'forceHighlight, focused at first', items: 'FRUIT', initialState: { isFocused: true }, forceHighlight: true, steps: [{ focus: true }, k('z'), k('ArrowDown')] },
  { name: 'no forceHighlight, focused at first', items: 'FRUIT', initialState: { isFocused: true }, steps: [{ focus: true }, k('z'), k('ArrowDown')] },
  { name: 'fewer items: the highlight back to the first', items: 'BASIC', steps: [{ focus: true }, k('End'), { items: 'FRUIT' }, k('ArrowDown'), { items: [] }, k('ArrowDown'), { items: 'FRUIT' }, k('ArrowDown')] },
  { name: 'an empty menu', items: [], steps: [{ focus: true }, k('ArrowDown'), k('End'), k('Enter')] },
  { name: 'a reducer that refuses type-ahead', items: 'FRUIT', reducer: 'noCharacter', steps: [{ focus: true }, k('c'), k('ArrowDown'), { enter: 4 }, { click: 4 }, { blur: true }] },
];

function behaviour() {
  const Inner = containerClass();
  return BEHAVIOUR.map((c) => ({
    name: c.name, items: typeof c.items === 'string' ? c.items : null, itemsGiven: typeof c.items === 'string' ? null : c.items,
    initialState: c.initialState ?? null, typeAhead: c.typeAhead ?? true, forceHighlight: !!c.forceHighlight, reducer: c.reducer ?? null,
    steps: run(Inner, c),
  }));
}

// ---- Scrolling ----

/**
 * The geometry Base Web's styles give a list of n options of one size in a list whose CSS height (an override's, as
 * basic.tsx's) is given: the ul's box is content-box, so its box is that height and its padding, which scrolls with its
 * options as a browser scrolls an overflow box's padding. Each option's top and height, the list's box (its client
 * height: it has no border) and its scroll height.
 */
function geometry(n, size, cssHeight) {
  const t = THEMES.Light;
  const o = flat(styleOf('ListItem', t, render({ items: [{ label: 'a' }], overrides: optionOf({ size }) }).find((x) => x.part === 'ListItem').props));
  const list = flat(styleOf('List', t, render({ items: [{ label: 'a' }] }).find((x) => x.part === 'List').props));
  const item = css.px(o.paddingTop) + css.px(o.lineHeight) + css.px(o.paddingBottom);
  const top = css.px(list.paddingTop);
  if (list.boxSizing !== undefined) throw new Error('List box-sizing');
  const height = cssHeight + top + css.px(list.paddingBottom);
  return { item, top, height, scrollHeight: top + n * item + css.px(list.paddingBottom) };
}

/** Base Web's scrollItemIntoView on that geometry, the list scrolled to scrollTop: the new scrollTop. */
function scrolled(g, index, n, scrollTop, align) {
  const parent = { scrollTop, scrollHeight: g.scrollHeight, getBoundingClientRect: () => ({ top: 0, bottom: g.height, height: g.height }) };
  const offsetTop = g.top + index * g.item;
  const child = { offsetTop, getBoundingClientRect: () => ({ top: offsetTop - parent.scrollTop, bottom: offsetTop - parent.scrollTop + g.item, height: g.item }) };
  scrollItemIntoView(child, parent, index === 0, index === n - 1, align);
  return parent.scrollTop;
}

/**
 * The scroll after each key, from a list scrolled to the top (or, with an initial highlight, centred on it as
 * componentDidMount does): a list of 12 at 250 px (basic.tsx), and of 12 at 300 px with item 5 highlighted
 * (menu-stateful.scenario.tsx), the keys stepping as the container steps them.
 */
const SCROLLS = [
  { name: 'basic.tsx: down to the end and back', n: 12, size: 'default', height: 250, start: -1, keys: [...Array(12).fill('ArrowDown'), ...Array(12).fill('ArrowUp')] },
  { name: 'compact: End, Home', n: 30, size: 'compact', height: 200, start: -1, keys: ['End', 'ArrowUp', 'Home', 'ArrowDown'] },
  { name: 'menu-stateful.scenario.tsx: item 5 in view, then down', n: 12, size: 'default', height: 300, start: 5, keys: ['ArrowDown', 'ArrowDown', 'ArrowDown', 'End', 'Home'] },
  { name: 'item 8 centred in 150 px, then up and down', n: 12, size: 'default', height: 150, start: 8, keys: ['ArrowUp', 'ArrowUp', 'ArrowUp', 'ArrowDown', 'ArrowDown', 'ArrowDown', 'ArrowDown'] },
];

function scroll() {
  return SCROLLS.map((c) => {
    const g = geometry(c.n, c.size, c.height);
    let index = c.start, top = 0;
    const initial = c.start >= 0 ? (top = scrolled(g, c.start, c.n, 0, 'center')) : 0;
    const steps = c.keys.map((key) => {
      if (key === 'ArrowDown') index = Math.min(index + 1, c.n - 1);
      else if (key === 'ArrowUp') index = Math.max(0, index - 1);
      else if (key === 'Home') index = 0;
      else if (key === 'End') index = c.n - 1;
      top = scrolled(g, index, c.n, top, 'auto');
      return { key, highlightedIndex: index, scrollTop: top };
    });
    return { name: c.name, count: c.n, size: c.size, cssHeight: c.height, boxHeight: g.height, start: c.start, item: g.item, initialScrollTop: initial, steps };
  });
}

function oracle(version) {
  return {
    baseui: version, lists: LISTS, colours: colours(), surfaces: surfaces(), layout: layout(), structure: structure(),
    behaviour: behaviour(), scroll: scroll(),
  };
}

module.exports = { oracle, render, STYLED, styleOf };
