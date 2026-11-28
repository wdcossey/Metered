// The oracle for Tabs: what Base Web's own tabs give (baseui/tabs-motion, the current tabs: baseui/tabs is deprecated in
// favour of it, documentation-site/pages/components/tabs.mdx at 3adb0478c9), for the cases the tests build, on the real
// light and dark themes, in Avalonia's units. Kept apart from tools/tokens/tabs.js, which writes the themes: the tests
// compare one with the other. Read by cases.js, which writes tests/Metered.Tests/Reference/tabs.oracle.json.
//
// - What it renders: each case renders Base Web's Tabs (or StatefulTabs) itself (react-dom/server) with every part
//   overridden by a component that records the props tabs.js gives it, in order. Which parts render, their order, text,
//   ARIA, tab stops, disabled and hidden states, and the content a panel holds, are Base Web's, with nothing decided here.
// - What its keys do: the onKeyDown and onClick tabs.js gives each Tab, called step by step in Node with an event whose
//   target and its parent's children are stand-ins for the tabs' buttons (each with the disabled, role, focus() and
//   click() a button has) and the highlight's div, re-rendered after each change as React would. Enter and Space are a
//   browser's: a button's activation, its click (HTML's), which tabs.js leaves to it.
// - Its look: each part's own style function, evaluated with the props tabs.js gives it, resolved for a tab's states.
'use strict';

const React = require('react');
const { Tabs, Tab, StatefulTabs, ORIENTATION, FILL } = require('baseui/tabs-motion');
const sc = require('baseui/tabs-motion/styled-components');
const css = require('./css');
const { THEMES, evaluate } = require('./oracle');
const { markup } = require('./input');

const TABS_PARTS = ['Root', 'TabBar', 'TabList', 'TabHighlight', 'TabBorder', 'EndEnhancerContainer'];
const TAB_PARTS = ['Tab', 'ArtworkContainer', 'TabPanel'];
const STYLED = {
  Root: sc.StyledRoot, TabBar: sc.StyledTabBar, TabList: sc.StyledTabList, TabHighlight: sc.StyledTabHighlight,
  TabBorder: sc.StyledTabBorder, EndEnhancerContainer: sc.StyledEndEnhancerContainer, Tab: sc.StyledTab,
  ArtworkContainer: sc.StyledArtworkContainer, TabPanel: sc.StyledTabPanel,
};

/** The artwork's props, as tab.js renders the artwork component: recorded by the case's artwork. */
let artworkProps = null;
function Artwork(p) {
  artworkProps = p;
  return React.createElement('i', null, 'artwork');
}

/**
 * Renders Base Web's Tabs (StatefulTabs when the case is stateful) and returns every part it rendered, in order:
 * { part, props }, a Tab's own parts recorded through its overrides.
 */
function render(c, theme = THEMES.Light) {
  const seen = [];
  const rec = (name) => ({
    // eslint-disable-next-line react/display-name
    component: React.forwardRef((p, ref) => { void ref; seen.push({ part: name, props: p }); return React.createElement('div', null, p.children); }),
  });
  const overrides = Object.fromEntries(TABS_PARTS.map((n) => [n, rec(n)]));
  const tabOverrides = Object.fromEntries(TAB_PARTS.map((n) => [n, rec(n)]));
  const children = c.tabs.map((t, i) => React.createElement(Tab, {
    key: t.key ?? undefined,
    title: t.title ?? undefined,
    ...(t.disabled === null || t.disabled === undefined ? {} : { disabled: t.disabled }),
    ...(t.artwork ? { artwork: Artwork } : {}),
    overrides: tabOverrides,
  }, t.content ?? 'Content ' + i));
  const props = {
    overrides,
    ...(c.disabled ? { disabled: true } : {}),
    ...(c.orientation ? { orientation: c.orientation } : {}),
    ...(c.fill ? { fill: c.fill } : {}),
    ...(c.activateOnFocus === false ? { activateOnFocus: false } : {}),
    ...(c.renderAll ? { renderAll: true } : {}),
    ...(c.endEnhancer ? { endEnhancer: c.endEnhancer } : {}),
    ...(c.onChange ? { onChange: c.onChange } : {}),
  };
  if (c.stateful) {
    if (c.initialActiveKey !== undefined && c.initialActiveKey !== null) props.initialState = { activeKey: c.initialActiveKey };
    markup(React.createElement(StatefulTabs, props, ...children), theme);
  } else {
    if (c.activeKey !== undefined && c.activeKey !== null) props.activeKey = c.activeKey;
    markup(React.createElement(Tabs, props, ...children), theme);
  }
  return seen;
}

/** The text a node renders: its strings, in order. */
function text(node) {
  if (node === null || node === undefined || typeof node === 'boolean') return '';
  if (typeof node === 'string' || typeof node === 'number') return String(node);
  if (Array.isArray(node)) return node.map(text).join('');
  if (node.type === Artwork) return '';
  return text(node.props?.children);
}

/**
 * The cases the tests build, from Base Web's scenarios (tabs-motion*.scenario.tsx at 3adb0478c9: the three tabs, disabled,
 * artwork, renderAll, vertical, fixed, the stateful one) and its docs' examples (keys, end enhancer), with the ends of
 * its props: keys of its own, a numeric active key (tabs.js compares with ==), a key no tab has, no titles, the Tabs'
 * disabled and a tab's own, which wins.
 */
const three = (extra = {}) => [{ title: 'Robot', ...extra[0] }, { title: 'Monster', ...extra[1] }, { title: 'Watermelon', ...extra[2] }];
const STRUCTURE = [
  { name: 'three', tabs: three(), activeKey: '0' },
  { name: 'second active', tabs: three(), activeKey: '1' },
  { name: 'numeric key', tabs: three(), activeKey: 2 },
  { name: 'own keys', tabs: [{ title: 'A', key: 'a' }, { title: 'B', key: 'b' }, { title: 'C', key: 'c' }], activeKey: 'b' },
  { name: 'no such key', tabs: three(), activeKey: '7' },
  { name: 'no titles', tabs: [{}, {}, { key: 'last' }], activeKey: '1' },
  { name: 'tabs disabled', tabs: three(), activeKey: '1', disabled: true },
  { name: 'a tab disabled', tabs: three({ 1: { disabled: true } }), activeKey: '0' },
  { name: 'a tab enabled in disabled tabs', tabs: three({ 2: { disabled: false } }), activeKey: '0', disabled: true },
  { name: 'the active tab disabled', tabs: three({ 0: { disabled: true } }), activeKey: '0' },
  { name: 'render all', tabs: three(), activeKey: '0', renderAll: true },
  { name: 'artwork', tabs: three({ 0: { artwork: true }, 2: { artwork: true } }), activeKey: '0' },
  { name: 'end enhancer', tabs: three(), activeKey: '0', endEnhancer: 'Enhancer' },
  { name: 'vertical', tabs: three(), activeKey: '0', orientation: ORIENTATION.vertical },
  { name: 'vertical end enhancer', tabs: three(), activeKey: '0', orientation: ORIENTATION.vertical, endEnhancer: 'Enhancer' },
  { name: 'fixed', tabs: three(), activeKey: '0', fill: FILL.fixed },
  { name: 'stateful', tabs: three(), stateful: true },
  { name: 'stateful own keys', tabs: [{ title: 'A', key: 'a' }, { title: 'B', key: 'b' }], stateful: true },
  { name: 'stateful initial', tabs: three(), stateful: true, initialActiveKey: '2' },
];

const one = (list, what) => {
  if (list.length !== 1) throw new Error(what + ': ' + list.length + ' elements');
  return list[0];
};

function structure() {
  return STRUCTURE.map((c) => {
    artworkProps = null;
    const seen = render(c);
    const of = (part) => seen.filter((s) => s.part === part).map((s) => s.props);
    const list = one(of('TabList'), 'TabList');
    const bar = one(of('TabBar'), 'TabBar');
    const tabs = of('Tab').map((p) => ({
      id: p.id,
      text: text(p.children),
      artwork: React.Children.toArray(p.children).some((ch) => ch && ch.props && ch.props['data-baseweb'] === 'artwork-container'),
      selected: p['aria-selected'],
      tabStop: p.tabIndex === '0',
      disabled: !!p.disabled,
      controls: p['aria-controls'],
    }));
    const panels = of('TabPanel').map((p) => ({ id: p.id, labelledBy: p['aria-labelledby'], hidden: p.hidden, content: text(p.children) || null }));
    const order = seen.map((s) => s.part);
    const { name, ...input } = c;
    return {
      name, input,
      parts: order,
      tabList: { role: list.role, orientation: list['aria-orientation'] },
      hasEndEnhancer: bar.$hasEndEnhancer,
      endEnhancerShown: order.includes('EndEnhancerContainer'),
      tabs: tabs.map(({ id, controls, ...t }) => ({ ...t, key: id.replace(/^tabs-\d+-tab-/, ''), controlsItsPanel: controls === id.replace('-tab-', '-tabpanel-') })),
      panels: panels.map(({ id, labelledBy, ...p }, i) => ({ ...p, labelledByItsTab: labelledBy === tabs[i].id && id === tabs[i].controls })),
      highlight: (({ role, ...h }) => ({ role, hidden: h['aria-hidden'] === 'true', length: h.$length, distance: h.$distance, animate: h.$animate }))(one(of('TabHighlight'), 'TabHighlight')),
      artworkProps: artworkProps === null ? null : { size: artworkProps.size, color: artworkProps.color },
    };
  });
}

const KEY_CODES = { ArrowLeft: 37, ArrowUp: 38, ArrowRight: 39, ArrowDown: 40, Home: 36, End: 35, Enter: 13, Space: 32 };

/**
 * Drives the tabs through the keys, as a page would: the focus on a tab, each key's keydown given to the focused tab's own
 * onKeyDown (tabs.js), Enter and Space a browser's click on the focused button unless it is disabled; after each change
 * of the active key, the tabs rendered again. Returns, after each key, the focused tab, the active key, the keys
 * onChange was given, and whether the key's default was prevented.
 */
function drive(c) {
  let active = c.activeKey ?? '0';
  let focused = c.start;
  const steps = [];
  for (const key of c.keys) {
    const changes = [];
    let prevented = false;
    const seen = render({ ...c, activeKey: active, onChange: ({ activeKey }) => { changes.push(activeKey); active = activeKey; } });
    const tabs = seen.filter((s) => s.part === 'Tab').map((s) => s.props);
    const nodes = tabs.map((p, i) => ({
      disabled: !!p.disabled,
      getAttribute: (name) => (name === 'role' ? p.role : p[name]),
      focus() { if (!p.disabled) focused = i; },
      click() { if (!p.disabled) p.onClick({}); },
    }));
    // The highlight's div is the tab list's last child.
    const highlight = { disabled: undefined, getAttribute: (name) => (name === 'role' ? 'presentation' : null) };
    const parent = { childNodes: [...nodes, highlight] };
    for (const n of [...nodes, highlight]) n.parentNode = parent;
    const target = nodes[focused];
    if (key === 'Enter' || key === 'Space') {
      tabs[focused].onKeyDown({ keyCode: KEY_CODES[key], target, preventDefault() { prevented = true; } });
      if (!prevented) target.click();
    } else {
      tabs[focused].onKeyDown({ keyCode: KEY_CODES[key], target, preventDefault() { prevented = true; } });
    }
    steps.push({ key, focused, activeKey: String(active), changes: changes.map(String), prevented });
  }
  return steps;
}

const SEQUENCES = [
  { name: 'right wraps', tabs: three(), activeKey: '0', start: 0, keys: ['ArrowRight', 'ArrowRight', 'ArrowRight', 'ArrowLeft', 'ArrowLeft'] },
  { name: 'left wraps', tabs: three(), activeKey: '0', start: 0, keys: ['ArrowLeft', 'ArrowRight'] },
  { name: 'up and down are not horizontal', tabs: three(), activeKey: '0', start: 0, keys: ['ArrowDown', 'ArrowUp', 'Home', 'End'] },
  { name: 'vertical', tabs: three(), activeKey: '0', orientation: ORIENTATION.vertical, start: 0, keys: ['ArrowDown', 'ArrowDown', 'ArrowDown', 'ArrowUp', 'ArrowRight', 'ArrowLeft', 'Home', 'End'] },
  { name: 'manual: Enter', tabs: three(), activeKey: '0', activateOnFocus: false, start: 0, keys: ['ArrowRight', 'Enter', 'ArrowRight', 'ArrowRight', 'Enter'] },
  { name: 'manual: Space', tabs: three(), activeKey: '0', activateOnFocus: false, start: 0, keys: ['ArrowLeft', 'Space', 'Space'] },
  { name: 'skips a disabled tab', tabs: three({ 1: { disabled: true } }), activeKey: '0', start: 0, keys: ['ArrowRight', 'ArrowLeft', 'ArrowLeft'] },
  { name: 'tabs disabled', tabs: three(), activeKey: '1', disabled: true, start: 1, keys: ['ArrowRight', 'ArrowLeft', 'Enter'] },
  { name: 'one enabled in disabled tabs', tabs: three({ 2: { disabled: false } }), activeKey: '0', disabled: true, start: 0, keys: ['ArrowRight', 'ArrowRight', 'ArrowLeft'] },
  { name: 'own keys', tabs: [{ title: 'A', key: 'a' }, { title: 'B', key: 'b' }, { title: 'C', key: 'c' }, { title: 'D', key: 'd' }], activeKey: 'c', start: 2, keys: ['ArrowRight', 'ArrowRight', 'ArrowLeft', 'Enter'] },
  { name: 'Enter on the active tab', tabs: three(), activeKey: '0', start: 0, keys: ['Enter', 'Space'] },
];

function keys() {
  return SEQUENCES.map(({ name, start, keys: pressed, ...input }) => ({ name, input, start, steps: drive({ ...input, start, keys: pressed }) }));
}

/** The declarations a browser applies for a tab's states: its :disabled, :hover, and :disabled:hover over them. */
function tabStyle(theme, props, { hover = false, disabled = false } = {}) {
  const s = evaluate(sc.StyledTab, theme, props);
  const self = {};
  const take = (b) => { for (const [k, v] of Object.entries(b || {})) if (v === null || typeof v !== 'object') self[k] = v; };
  take(s);
  if (disabled) take(s[':disabled']);
  if (hover) take(s[':hover']);
  if (disabled && hover) take(s[':disabled:hover']);
  return self;
}

const background = (v) => (v === 'none' ? '#00000000' : css.color(v));

/** An outline: '<width> solid <colour>', or none. */
function outline(s) {
  if (s.outline === 'none') return null;
  const m = /^(\S+) solid (#[0-9A-Fa-f]+)$/.exec(s.outline);
  if (!m) throw new Error('outline ' + s.outline);
  return { width: css.px(m[1]), colour: css.color(m[2]), offset: css.px(s.outlineOffset) };
}

const TAB_STATES = [
  ['rest', {}, {}],
  ['active', { $isActive: true }, {}],
  ['hover', {}, { hover: true }],
  ['activeHover', { $isActive: true }, { hover: true }],
  ['disabled', {}, { disabled: true }],
  ['disabledHover', {}, { disabled: true, hover: true }],
  ['focusVisible', { $focusVisible: true }, {}],
  ['focusVisibleHover', { $focusVisible: true, $isActive: true }, { hover: true }],
];

const shared = (o = ORIENTATION.horizontal, f = FILL.intrinsic) => ({ $orientation: o, $fill: f });

function colours() {
  const out = [];
  for (const theme of Object.keys(THEMES)) {
    const t = THEMES[theme];
    for (const [state, extra, cascade] of TAB_STATES) {
      const s = tabStyle(t, { ...shared(), $focusVisible: false, $isActive: false, ...extra }, cascade);
      out.push({
        theme, state,
        background: background(s.background),
        foreground: css.color(s.color),
        cursor: s.cursor,
        outline: outline(s),
        highlight: css.color(evaluate(sc.StyledTabHighlight, t, shared()).backgroundColor),
        border: css.color(evaluate(sc.StyledTabBorder, t, shared()).backgroundColor),
      });
    }
  }
  return out;
}

const sides = (s, prefix) => ['Left', 'Top', 'Right', 'Bottom'].map((side) => (s[prefix + side] === undefined ? 0 : css.px(s[prefix + side])));
const font = (s) => ({ fontSize: css.px(s.fontSize), fontWeight: css.fontWeight(s.fontWeight), lineHeight: css.px(s.lineHeight) });

function layout() {
  const out = [];
  for (const orientation of [ORIENTATION.horizontal, ORIENTATION.vertical]) {
    for (const fill of [FILL.intrinsic, FILL.fixed]) {
      const t = THEMES.Light;
      const p = shared(orientation, fill);
      const root = evaluate(sc.StyledRoot, t, p);
      const list = evaluate(sc.StyledTabList, t, p);
      const tab = evaluate(sc.StyledTab, t, { ...p, $focusVisible: false, $isActive: false });
      const art = evaluate(sc.StyledArtworkContainer, t, p);
      const end = evaluate(sc.StyledEndEnhancerContainer, t, p);
      const hl = evaluate(sc.StyledTabHighlight, t, { ...p, $length: 0, $distance: 0, $animate: false });
      const border = evaluate(sc.StyledTabBorder, t, p);
      const panel = evaluate(sc.StyledTabPanel, t, p);
      const bar = (has) => evaluate(sc.StyledTabBar, t, { $orientation: orientation, $hasEndEnhancer: has }).display ?? 'block';
      out.push({
        orientation, fill,
        rootDisplay: root.display ?? 'block',
        tabBarDisplay: bar(false), tabBarDisplayWithEnhancer: bar(true),
        tabList: {
          flexDirection: list.flexDirection, flexWrap: list.flexWrap, flexGrow: list.flexGrow,
          padding: sides(list, 'padding'), margin: sides(list, 'margin'),
          overflowX: list.overflowX ?? 'visible', overflowY: list.overflowY ?? 'visible', scrollbarWidth: list.scrollbarWidth ?? null,
        },
        tab: {
          padding: sides(tab, 'padding'), justifyContent: tab.justifyContent, alignItems: tab.alignItems,
          flexGrow: tab.flexGrow ?? 0, flexBasis: tab.flexBasis ?? 'auto', ...font(tab),
        },
        artworkMargin: sides(art, 'margin'),
        endEnhancer: { margin: sides(end, 'margin'), alignItems: end.alignItems, justifyContent: end.justifyContent },
        highlight: {
          width: hl.width === undefined ? null : css.px(hl.width), height: hl.height === undefined ? null : css.px(hl.height),
          left: hl.left ?? null, right: hl.right ?? null, bottom: hl.bottom ?? null, transform: hl.transform,
          zIndex: hl.zIndex,
        },
        border: { width: border.width === undefined ? null : css.px(border.width), height: border.height === undefined ? null : css.px(border.height) },
        panelPadding: sides(panel, 'padding'),
      });
    }
  }
  return out;
}

/** The highlight's transition, only once a key has changed ($animate), and the tab's background's. */
function motion() {
  const t = THEMES.Light;
  const still = evaluate(sc.StyledTabHighlight, t, { ...shared(), $length: 10, $distance: 20, $animate: false });
  const moving = evaluate(sc.StyledTabHighlight, t, { ...shared(), $length: 10, $distance: 20, $animate: true });
  const vertical = evaluate(sc.StyledTabHighlight, t, { ...shared(ORIENTATION.vertical), $length: 10, $distance: 20, $animate: true });
  const tab = evaluate(sc.StyledTab, t, { ...shared(), $focusVisible: false, $isActive: false });
  return {
    highlightStill: still.transitionProperty ?? null,
    highlight: { property: moving.transitionProperty, seconds: css.seconds(moving.transitionDuration), curve: css.cubicBezier(moving.transitionTimingFunction) },
    // $length and $distance: the width and translateX, or the height and translateY, in pixels.
    horizontal: { width: moving.width, transform: moving.transform },
    vertical: { height: vertical.height, transform: vertical.transform },
    tab: { property: tab.transitionProperty, seconds: css.seconds(tab.transitionDuration), curve: css.cubicBezier(tab.transitionTimingFunction) },
  };
}

function oracle(version) {
  return { baseui: version, structure: structure(), keys: keys(), colours: colours(), layout: layout(), motion: motion() };
}

module.exports = { oracle, render, STYLED, TABS_PARTS, TAB_PARTS };
