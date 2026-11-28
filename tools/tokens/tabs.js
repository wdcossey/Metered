// The themes of Tabs' parts, Tabs.Styled<Part>, generated from Base Web's tabs-motion
// (baseui/tabs-motion/styled-components), the current tabs (baseui/tabs is deprecated in its favour).
//
// As the v2 controls' generators do, and with radio.js's theme writer: each style function is evaluated on Base Web's
// light theme with its colours replaced by their own names ('@contentPrimary'), for every combination of the states it
// reads, so that the themes refer to the colours by token and read every length; written as the fewest layered state
// styles and checked against Base Web's for every combination. The tab's :hover, :disabled and :disabled:hover are its
// own pseudo-classes, resolved here as a browser's cascade does (:disabled:hover, the more specific, over both).
//
// The highlight's length and distance follow the active tab's layout, which no state carries: the theme gives Base
// Web's defaults ($length and $distance 0), and the Tabs sets the active tab's at Template priority, above a plain
// setter and below a state style or an override's setter (Metered.Controls.Tabs). The artwork's size, which tab.js
// gives the artwork it renders, is written as the resource Tabs.ArtworkSize.
'use strict';

const React = require('react');
const css = require('../reference/css');
const { evaluate } = require('../reference/oracle');
const sc = require('baseui/tabs-motion/styled-components');
const { ORIENTATION, FILL } = require('baseui/tabs-motion/constants');
const { Tabs, Tab } = require('baseui/tabs-motion');
const { markup } = require('../reference/input');
const { L, NAMED, theme: write, colour, px, check, font, TEXT, cursor } = require('./radio');

/**
 * The states, each with its values (the default first) and the pseudoclass that carries it. The Tabs mirrors
 * \`orientation\` and \`fill\` (Base Web's sharedStylingProps) onto every part; a tab's own are its \`$isActive\`, Avalonia's
 * \`:focus-visible\` (Base Web's $focusVisible, a keyboard's focus), \`:disabled\` (the button's disabled), and
 * \`:is-hovered\` for CSS's :hover, a mouse's only; the highlight's \`$animate\` and the bar's \`$hasEndEnhancer\`.
 */
const ORIENT = { name: 'orientation', values: [ORIENTATION.horizontal, ORIENTATION.vertical], pseudo: (v) => ':orientation-' + v };
const FILLS = { name: 'fill', values: [FILL.intrinsic, FILL.fixed], pseudo: (v) => ':fill-' + v };
const SHARED = [ORIENT, FILLS];
const ACTIVE = { name: 'isActive', values: [false, true], pseudo: () => ':is-active' };
const FOCUS = { name: 'focusVisible', values: [false, true], pseudo: () => ':focus-visible' };
const DISABLED = { name: 'disabled', values: [false, true], pseudo: () => ':disabled' };
const HOVER = { name: 'hover', values: [false, true], pseudo: () => ':is-hovered' };
const ANIMATE = { name: 'animate', values: [false, true], pseudo: () => ':animate' };
const END = { name: 'hasEndEnhancer', values: [false, true], pseudo: () => ':has-end-enhancer' };

/** tabs.js's props for a part in a combination. */
function props(c) {
  return {
    $orientation: c.orientation, $fill: c.fill, $isActive: !!c.isActive, $focusVisible: !!c.focusVisible,
    $length: 0, $distance: 0, $animate: !!c.animate, $hasEndEnhancer: !!c.hasEndEnhancer,
  };
}

/** The pseudo-element blocks a part may have, which draw nothing an Avalonia scroller draws: its scroll bar's. */
const BARLESS = new Set(['::-webkit-scrollbar']);

/** The declarations a browser applies to the part in that combination: a tab's :disabled, :hover and :disabled:hover over its own. */
function declarations(styled, t, c) {
  const style = evaluate(styled, t, props(c));
  const self = {};
  const take = (b) => { for (const [k, v] of Object.entries(b || {})) if (v === null || typeof v !== 'object') self[k] = v; };
  for (const [k, v] of Object.entries(style)) {
    if (v !== null && typeof v === 'object' && !BARLESS.has(k) && ![':disabled', ':hover', ':disabled:hover'].includes(k)) {
      throw new Error(styled.displayName + ': a block this generator doesn\'t read: ' + k);
    }
  }
  take(style);
  if (c.disabled) take(style[':disabled']);
  if (c.hover) take(style[':hover']);
  if (c.disabled && c.hover) take(style[':disabled:hover']);
  for (const k of Object.keys(self)) if (self[k] === undefined) delete self[k];
  return self;
}

const sides = (s, prefix) => {
  const [l, t, r, b] = ['Left', 'Top', 'Right', 'Bottom'].map((side) => (s[prefix + side] === undefined ? 0 : css.px(s[prefix + side])));
  return l === t && t === r && r === b ? String(l) : l + ',' + t + ',' + r + ',' + b;
};
const SIDES = (prefix) => ['Left', 'Top', 'Right', 'Bottom'].map((side) => prefix + side);

/** CSS's overflow on one axis as Avalonia's scroll bar: scroll with no bar drawn (scrollbar-width: none) is Hidden, visible Disabled. */
function overflow(v, s, what) {
  if (v === undefined || v === 'visible') return 'Disabled';
  if (v === 'scroll' && s.scrollbarWidth === 'none' && s['-ms-overflow-style'] === 'none') return 'Hidden';
  throw new Error(what + ' overflow ' + v);
}

const PARTS = [
  {
    part: 'Root', type: 'b:TabsRootPanel', styled: sc.StyledRoot, axes: SHARED,
    convert(s) {
      // transform: scale(1) makes a stacking context for the highlight's z-index: the TabBar is drawn over the TabBorder
      // (Tabs.axaml), which the highlight overlaps.
      check(s, 'Root', ['transform', 'display']);
      if (s.transform !== 'scale(1)') throw new Error('Root transform ' + s.transform);
      // A block stacks its children, a flex box (its row) lays them side by side, the last taking the rest.
      const d = s.display ?? 'block';
      if (d !== 'block' && d !== 'flex') throw new Error('Root display ' + d);
      return { Orientation: d === 'flex' ? 'Horizontal' : 'Vertical' };
    },
  },
  {
    part: 'TabBar', type: 'Grid', styled: sc.StyledTabBar, axes: [ORIENT, END],
    convert(s) {
      // A block, or a flex row of the tab list (flex-grow 1) and the end enhancer: both a grid of the list's column, which
      // takes the rest, and the enhancer's, at its own width (Tabs.axaml).
      check(s, 'TabBar', ['display']);
      if (s.display !== undefined && s.display !== 'flex') throw new Error('TabBar display ' + s.display);
      // The highlight's z-index, in the root's stacking context (its transform: scale(1)), lifts it over the border, which
      // comes after the bar: the bar, which holds it, is drawn over the border.
      const z = evaluate(sc.StyledTabHighlight, L, props({ orientation: ORIENTATION.horizontal, fill: FILL.intrinsic })).zIndex;
      if (typeof z !== 'number') throw new Error('TabHighlight zIndex ' + z);
      return { ZIndex: String(z) };
    },
  },
  {
    part: 'TabList', type: 'b:TabList', styled: sc.StyledTabList, axes: SHARED,
    convert(s) {
      check(s, 'TabList', ['position', 'display', 'flexWrap', 'flexGrow', 'flexDirection', ...SIDES('padding'), ...SIDES('margin'),
        '-ms-overflow-style', 'scrollbarWidth', 'overflowX', 'overflowY']);
      // A flex line that doesn't wrap, growing in the bar (the dock panel's last child fills it), relative for the highlight.
      if (s.position !== 'relative' || s.display !== 'flex' || s.flexWrap !== 'nowrap' || s.flexGrow !== 1) throw new Error('TabList box');
      const row = { row: true, column: false }[s.flexDirection];
      if (row === undefined) throw new Error('TabList flexDirection ' + s.flexDirection);
      return {
        Orientation: row ? 'Horizontal' : 'Vertical',
        Padding: sides(s, 'padding'),
        Margin: sides(s, 'margin'),
        HorizontalScrollBarVisibility: overflow(s.overflowX, s, 'TabList'),
        VerticalScrollBarVisibility: overflow(s.overflowY, s, 'TabList'),
      };
    },
  },
  {
    part: 'Tab', type: 'b:TabButton', styled: sc.StyledTab, axes: [...SHARED, ACTIVE, FOCUS, DISABLED, HOVER],
    // A button's text wraps where it may, and a word longer than the line overflows it (CSS's white-space and overflow-wrap normal).
    fixed: { [TEXT(true).TextWrapping]: 'WrapWithOverflow' },
    convert(s) {
      check(s, 'Tab', ['cursor', 'WebkitAppearance', ...SIDES('margin'), 'boxSizing', 'display', 'alignItems', ...SIDES('padding'),
        ...['Left', 'Top', 'Right', 'Bottom'].flatMap((x) => ['border' + x + 'Width', 'border' + x + 'Style']), 'color', 'background',
        'transitionProperty', 'transitionDuration', 'transitionTimingFunction', 'outline', 'outlineOffset', 'fontFamily', 'fontSize',
        'fontWeight', 'lineHeight', 'flexGrow', 'flexBasis', 'justifyContent']);
      if (s.boxSizing !== 'border-box' || s.display !== 'inline-flex' || s.alignItems !== 'center' || s.WebkitAppearance !== 'none') throw new Error('Tab box');
      for (const x of ['Left', 'Top', 'Right', 'Bottom']) {
        if (css.px(s['margin' + x]) !== 0 || css.px(s['border' + x + 'Width']) !== 0 || s['border' + x + 'Style'] !== 'none') throw new Error('Tab ' + x);
      }
      let out = { OutlineBrush: '{x:Null}', OutlineThickness: '0' };
      if (s.outline !== 'none') {
        const m = /^(\S+) solid (@\w+)$/.exec(s.outline);
        if (!m) throw new Error('Tab outline ' + s.outline);
        out = { OutlineBrush: colour(m[2], 'Tab outline'), OutlineThickness: px(m[1]) };
      }
      const n = TEXT(true);
      return {
        Cursor: cursor(s.cursor),
        Padding: sides(s, 'padding'),
        Background: s.background === 'none' ? 'Transparent' : colour(s.background, 'Tab'),
        [n.Foreground]: colour(s.color, 'Tab'),
        ...font(s, 'Tab', n),
        ...out,
        OutlineOffset: px(s.outlineOffset),
        // The flex item in the tab list's line: its growth and basis (auto, NaN, when unset).
        FlexGrow: String(s.flexGrow ?? 0),
        FlexBasis: s.flexBasis === undefined ? 'NaN' : px(s.flexBasis),
        // Its content along its own row (an inline flex box's justify-content), centred across it (align-items).
        HorizontalContentAlignment: { center: 'Center', 'flex-end': 'Right' }[s.justifyContent] ?? (() => { throw new Error('Tab justifyContent ' + s.justifyContent); })(),
        VerticalContentAlignment: 'Center',
      };
    },
  },
  {
    part: 'ArtworkContainer', type: 'ContentPresenter', styled: sc.StyledArtworkContainer, axes: SHARED,
    convert(s) {
      check(s, 'ArtworkContainer', ['display', 'marginRight', 'marginLeft']);
      if (s.display !== 'flex') throw new Error('ArtworkContainer display');
      // tab.js renders the artwork with the colour it names: the content's (an icon's) colour.
      return { Margin: sides(s, 'margin'), [TEXT(true).Foreground]: colour('@' + ARTWORK.color, 'artwork') };
    },
  },
  {
    part: 'TabHighlight', type: 'b:TabHighlight', styled: sc.StyledTabHighlight, axes: [...SHARED, ANIMATE],
    convert(s, c) {
      check(s, 'TabHighlight', ['backgroundColor', 'position', 'zIndex', 'bottom', 'left', 'right', 'height', 'width', 'transform',
        'transitionProperty', 'transitionDuration', 'transitionTimingFunction']);
      if (s.position !== 'absolute') throw new Error('TabHighlight position');
      const horizontal = c.orientation === ORIENTATION.horizontal;
      // Along the line: $length its width (height), $distance its translate; across it, at the bottom (the right), its thickness.
      const along = horizontal ? s.width : s.height;
      const translate = new RegExp('^translate' + (horizontal ? 'X' : 'Y') + '\\((-?[\\d.]+)px\\)$').exec(s.transform);
      if (along !== '0px' || !translate || Number(translate[1]) !== 0) throw new Error('TabHighlight ' + along + ' ' + s.transform);
      if (horizontal ? s.bottom !== '0px' || s.left !== '0px' : s.right !== '0px' || s.left !== undefined) throw new Error('TabHighlight place');
      return {
        Background: colour(s.backgroundColor, 'TabHighlight'),
        ZIndex: String(s.zIndex),
        Width: horizontal ? 'NaN' : px(s.width),
        Height: horizontal ? px(s.height) : 'NaN',
        // left: 0 and its translate (top and translateY): its line places it at its distance, as long as its length, so it
        // stretches along the line; across it, at the bottom (the right).
        HorizontalAlignment: horizontal ? 'Stretch' : 'Right',
        VerticalAlignment: horizontal ? 'Bottom' : 'Stretch',
        Length: '0',
        Distance: '0',
      };
    },
  },
  {
    part: 'TabBorder', type: 'Border', styled: sc.StyledTabBorder, axes: SHARED,
    convert(s, c) {
      check(s, 'TabBorder', ['backgroundColor', 'position', 'height', 'width']);
      const horizontal = c.orientation === ORIENTATION.horizontal;
      return { Background: colour(s.backgroundColor, 'TabBorder'), Width: horizontal ? 'NaN' : px(s.width), Height: horizontal ? px(s.height) : 'NaN' };
    },
  },
  {
    part: 'EndEnhancerContainer', type: 'ContentPresenter', styled: sc.StyledEndEnhancerContainer, axes: SHARED,
    convert(s) {
      check(s, 'EndEnhancerContainer', ['display', 'alignItems', 'justifyContent', 'marginLeft', 'marginRight']);
      if (s.display !== 'flex' || s.alignItems !== 'center' || s.justifyContent !== 'flex-end') throw new Error('EndEnhancerContainer box');
      return { Margin: sides(s, 'margin'), HorizontalContentAlignment: 'Right', VerticalContentAlignment: 'Center' };
    },
  },
  {
    part: 'TabPanel', type: 'ContentPresenter', styled: sc.StyledTabPanel, axes: SHARED,
    // A div's text wraps where it may, a word longer than the line overflowing it.
    fixed: { TextWrapping: 'WrapWithOverflow' },
    convert(s) {
      // flex-grow 1: the panel takes the rest of a vertical root's row (the panels' place in Tabs.axaml); outline none.
      check(s, 'TabPanel', ['flexGrow', 'outline', ...SIDES('padding')]);
      if (s.flexGrow !== 1 || s.outline !== 'none') throw new Error('TabPanel box');
      return { Padding: sides(s, 'padding') };
    },
  },
];

/** What tab.js gives the artwork it renders: its size and colour, read from Base Web's Tabs rendered. */
const ARTWORK = (() => {
  let seen = null;
  const Artwork = (p) => { seen = p; return null; };
  markup(React.createElement(Tabs, { activeKey: '0' }, React.createElement(Tab, { title: 'T', artwork: Artwork }, 'x')), L);
  if (!seen || typeof seen.size !== 'number' || typeof seen.color !== 'string') throw new Error('artwork props ' + JSON.stringify(seen));
  return seen;
})();

const span = (seconds) => new Date(seconds * 1000).toISOString().substr(11, 12);

/** A transition's duration and curve on the part in a combination. */
function timing(s, what) {
  const [x1, y1, x2, y2] = css.cubicBezier(s.transitionTimingFunction);
  return { span: span(css.seconds(s.transitionDuration)), easing: x1 + ',' + y1 + ',' + x2 + ',' + y2, what };
}

/** The tab's background transition, once loaded (CSS doesn't transition a page's first paint). */
function tabMotion() {
  const s = declarations(sc.StyledTab, L, { orientation: ORIENTATION.horizontal, fill: FILL.intrinsic });
  if (s.transitionProperty !== 'background') throw new Error('Tab transition ' + s.transitionProperty);
  const t = timing(s, 'Tab');
  return '    <Style Selector="^:loaded">\n      <Setter Property="Transitions">\n        <Transitions>\n' +
    '          <BrushTransition Property="Background" Duration="' + t.span + '" Easing="' + t.easing + '" />\n' +
    '        </Transitions>\n      </Setter>\n    </Style>';
}

/**
 * The highlight's transition: \`all\` once a key has changed ($animate), none before. What moves is its length and its
 * distance (and its colour, with the theme): those are written.
 */
function highlightMotion() {
  for (const orientation of ORIENT.values) {
    const still = declarations(sc.StyledTabHighlight, L, { orientation, fill: FILL.intrinsic });
    if (still.transitionProperty !== undefined) throw new Error('TabHighlight: a transition before $animate');
  }
  const s = declarations(sc.StyledTabHighlight, L, { orientation: ORIENTATION.horizontal, fill: FILL.intrinsic, animate: true });
  const v = declarations(sc.StyledTabHighlight, L, { orientation: ORIENTATION.vertical, fill: FILL.intrinsic, animate: true });
  if (s.transitionProperty !== 'all' || JSON.stringify(timing(s, 'h')) !== JSON.stringify(timing(v, 'h'))) throw new Error('TabHighlight transition');
  const t = timing(s, 'TabHighlight');
  const one = (k, p) => '          <' + k + ' Property="' + p + '" Duration="' + t.span + '" Easing="' + t.easing + '" />';
  return '    <Style Selector="^:animate">\n      <Setter Property="Transitions">\n        <Transitions>\n' +
    [one('DoubleTransition', 'Length'), one('DoubleTransition', 'Distance'), one('BrushTransition', 'Background')].join('\n') +
    '\n        </Transitions>\n      </Setter>\n    </Style>';
}

const DESCRIBE = {
  Root: 'the tabs: a block of the bar, the border and the panels; a row, the panels taking the rest, when vertical.',
  TabBar: 'the tab list and the end enhancer, side by side.',
  TabList: 'the line of tabs: a scroller with no bar while intrinsic, its padding where the highlight is and a margin as much under it, so the border meets it.',
  Tab: 'a tab: its padding and type; the fill under a hovering mouse, the disabled colour (and no fill hovered), the inset focus ring; equal shares when fixed; its content at the end when vertical.',
  ArtworkContainer: 'a tab\'s artwork, before its title.',
  TabHighlight: 'the bar under the active tab (beside it when vertical): its colour and thickness; its length and place the tab\'s, moving once a key has changed.',
  TabBorder: 'the line under the tabs (beside them when vertical).',
  EndEnhancerContainer: 'the end enhancer, after the tab list, centred on its line.',
  TabPanel: 'a tab\'s panel, padded; only the active tab\'s is shown.',
};

function file(version) {
  const themes = PARTS.map((p) => {
    const { plain, blocks } = write(p, p.axes, declarations, { layered: true });
    const extra = p.part === 'Tab' ? '\n' + tabMotion() : p.part === 'TabHighlight' ? '\n' + highlightMotion() : '';
    const body = [plain.join('\n') + extra, ...blocks].filter((x) => x.length);
    const head = '  <!-- ' + p.part + ': ' + DESCRIBE[p.part] + ' -->\n  <ControlTheme x:Key="Tabs.Styled' + p.part + '" TargetType="' + p.type + '"';
    return body.length ? head + '>\n' + body.join('\n') + '\n  </ControlTheme>' : head + ' />';
  });

  return '<!--\n  Generated by tools/tokens/gen.js (tools/tokens/tabs.js) from baseui ' + version + '\n' +
    '  (© Uber Technologies, Inc., MIT). Do not edit.\n' +
    '  The themes of Tabs\' parts, Tabs.Styled<Part>, from Base Web\'s tabs-motion: each part\'s look by the state\n' +
    '  pseudoclasses the Tabs mirrors onto it (Metered.Controls.Tabs), the colours by token, the lengths and the motion\n' +
    '  read from Base Web\'s own style functions.\n-->\n' +
    '<ResourceDictionary xmlns="https://github.com/avaloniaui"\n                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"\n' +
    '                    xmlns:b="https://github.com/wdcossey/Metered">\n' +
    '  <!-- The size tab.js gives a tab\'s artwork: an icon\'s width and height. -->\n' +
    '  <x:Double x:Key="Tabs.ArtworkSize">' + ARTWORK.size + '</x:Double>\n' +
    themes.join('\n') + '\n</ResourceDictionary>\n';
}

module.exports = { file, PARTS, ARTWORK };
