// The themes of Menu's parts, Menu.Styled<Part>, generated from Base Web's menu (baseui/menu/styled-components) with the
// theme writer the other controls share (radio.js): each style function evaluated on a theme whose colours, radii,
// shadows and border colours are their own names, for every combination of the states menu.js and option-list.js give
// the part, written as the fewest state styles and checked against Base Web's own. The tests' oracle
// (tools/reference/menu.js) checks the result against the real colours.
//
// What the themes hold and what the Menu gives (Metered.Controls.Menu):
// - List: the box (menuFill, popoverBorderRadius, shadow600), its 8 px top and bottom padding, inside its scroller, and its
//   3 px borderAccent outline while focused by keyboard (its own :focus-visible, Base Web's $isFocusVisible on :focus).
// - ListItem: each option's box by $size (:size-compact), $isHighlighted, $isFocused (the menu's) and $disabled
//   (:is-highlighted, :is-focused, :is-disabled), and its colour and background transition once loaded.
// - ListItemAnchor: an href option's label: Base Web gives it none of the option's states, so its colour is always the
//   default; a browser's own stylesheet underlines a link (a:any-link), which a descendant style gives its text.
// - OptgroupHeader, MenuDivider, EmptyState: a group's header, a divider, the empty state.
// - The profile option's parts: ListItemProfile (tinted under a hovering mouse: its plain :hover), ProfileImgContainer,
//   ProfileImg, ProfileLabelsContainer, ProfileTitle, ProfileSubtitle, ProfileBody.
'use strict';

const sc = require('baseui/menu/styled-components');
const css = require('../reference/css');
const { evaluate } = require('../reference/oracle');
const { L, NAMED, theme: write, colour, px, check, font, cursor, timing, TEXT } = require('./radio');

const r = (key) => '{DynamicResource ' + key + '}';
/** A theme group's string values by their names ('@borders.popoverBorderRadius'), its objects' too ('@borders.border300.borderColor'). */
const named = (group) => new Proxy(L[group], {
  get: (o, name) => {
    if (typeof name !== 'string') return o[name];
    if (typeof o[name] === 'string') return '@' + group + '.' + name;
    if (o[name] && typeof o[name] === 'object') return new Proxy(o[name], { get: (b, k) => (typeof k === 'string' && typeof b[k] === 'string' ? '@' + group + '.' + name + '.' + k : b[k]) });
    return o[name];
  },
});
const T = { ...NAMED, borders: named('borders'), lighting: named('lighting') };
/** A theme value by its token. */
function token(v, what) {
  if (typeof v === 'string' && /^@(borders|lighting)\./.test(v)) return r(v.slice(1));
  throw new Error(what + ': not a token: ' + JSON.stringify(v));
}

/** The option's states, as the Menu mirrors them onto each option and its parts (its $ props); size from the Option's props. */
const OPTION = [
  { name: 'size', values: ['default', 'compact'], pseudo: (v) => ':size-' + v },
  { name: 'isHighlighted', values: [false, true], pseudo: () => ':is-highlighted' },
  { name: 'isFocused', values: [false, true], pseudo: () => ':is-focused' },
  { name: 'disabled', values: [false, true], pseudo: () => ':is-disabled' },
];
const optionProps = (c) => ({ $size: c.size, $isHighlighted: c.isHighlighted, $isFocused: c.isFocused, $disabled: c.disabled });

const LIST = [{ name: 'isFocusVisible', values: [false, true], pseudo: () => ':focus-visible' }];
const HOVER = [{ name: 'hover', values: [false, true], pseudo: () => ':hover' }];
const NONE = [{ name: 'none', values: [false], pseudo: () => ':metered-never' }];

/** The declarations of a style object with its nested blocks: those named are applied when \`on\`, any other throws. */
function declarations(style, what, blocks = {}) {
  const out = {};
  for (const [k, v] of Object.entries(style)) {
    if (v !== null && typeof v === 'object') {
      if (!(k in blocks)) throw new Error(what + ': a block this generator does not read: ' + k);
    } else out[k] = v;
  }
  for (const [k, on] of Object.entries(blocks)) if (on && style[k]) Object.assign(out, style[k]);
  return out;
}

const styleOf = (styled, t, p) => {
  const s = styled.__STYLETRON__;
  return s.reducers.length === 0 ? { ...s.getInitialStyle() } : evaluate(styled, t, p);
};

const RADII = ['borderTopLeftRadius', 'borderTopRightRadius', 'borderBottomRightRadius', 'borderBottomLeftRadius'];
function same(s, keys, what) {
  const v = new Set(keys.map((k) => s[k]));
  if (v.size !== 1) throw new Error(what + ': ' + keys.join(', ') + ' differ');
  return [...v][0];
}
const sides = (s, prefix) => ['Left', 'Top', 'Right', 'Bottom'].map((k) => css.px(s[prefix + k] ?? 0)).join(',');
const OWN = TEXT(false);
const TRANSITION = ['transitionProperty', 'transitionDuration', 'transitionTimingFunction'];
const FONT = ['fontFamily', 'fontSize', 'fontWeight', 'lineHeight'];

const PARTS = [
  {
    part: 'List', type: 'b:MenuList', axes: LIST,
    styled: (t, c) => {
      const s = styleOf(sc.StyledList, T, { $isFocusVisible: c.isFocusVisible });
      // Base Web's outline is under :focus, by $isFocusVisible: a focus by keyboard. The element's own :focus-visible is both.
      return declarations(s, 'List', { ':focus': c.isFocusVisible });
    },
    convert(s) {
      const what = 'List';
      check(s, what, ['backgroundColor', 'position', 'marginTop', 'marginBottom', 'marginLeft', 'marginRight', 'paddingTop', 'paddingBottom',
        'paddingLeft', 'paddingRight', ...RADII, 'boxShadow', 'overflow', 'outline']);
      if (s.position !== 'relative' || s.overflow !== 'auto' || sides(s, 'margin') !== '0,0,0,0') throw new Error(what + ' box');
      let outline = { OutlineBrush: '{x:Null}', OutlineThickness: '0' };
      if (s.outline !== undefined && s.outline !== 'none') {
        const m = /^(\S+) solid (@\w+)$/.exec(s.outline);
        if (!m) throw new Error(what + ' outline ' + s.outline);
        outline = { OutlineBrush: colour(m[2], what), OutlineThickness: px(m[1]) };
      }
      return {
        Background: colour(s.backgroundColor, what),
        CornerRadius: token(same(s, RADII, what), what),
        BoxShadow: token(s.boxShadow, what),
        // Inside the scroller: an overflow box's padding scrolls with its content.
        Padding: sides(s, 'padding'),
        ...outline,
      };
    },
  },
  {
    part: 'ListItem', type: 'ContentPresenter', axes: OPTION,
    // A block: as wide as the list, its text wrapping.
    fixed: { HorizontalAlignment: 'Stretch', TextWrapping: 'Wrap' },
    styled: (t, c) => declarations(styleOf(sc.StyledListItemElement, T, optionProps(c)), 'ListItem', { ':focus': false }),
    convert(s) {
      const what = 'ListItem';
      check(s, what, [...FONT, 'boxSizing', 'position', 'display', 'color', 'cursor', 'backgroundColor', ...TRANSITION, 'marginBottom',
        'paddingTop', 'paddingBottom', 'paddingRight', 'paddingLeft']);
      if (s.boxSizing !== 'border-box' || s.position !== 'relative' || s.display !== 'block' || s.marginBottom !== 0) throw new Error(what + ' box');
      return {
        Padding: sides(s, 'padding'),
        Background: colour(s.backgroundColor, what),
        Foreground: colour(s.color, what),
        ...font(s, what, OWN),
        Cursor: cursor(s.cursor),
      };
    },
    transitionOf: 'color, background-color',
    transitions: [['BrushTransition', 'Foreground'], ['BrushTransition', 'Background']],
  },
  {
    part: 'ListItemAnchor', type: 'ContentPresenter', axes: NONE,
    fixed: { HorizontalAlignment: 'Stretch', TextWrapping: 'Wrap' },
    // option-list.js gives the anchor only $item and href: getFontColor reads none of the option's states.
    styled: () => declarations(styleOf(sc.StyledListItemAnchor, T, {}), 'ListItemAnchor'),
    convert(s) {
      const what = 'ListItemAnchor';
      check(s, what, ['display', 'color']);
      if (s.display !== 'block') throw new Error(what + ' box');
      return { Foreground: colour(s.color, what) };
    },
  },
  {
    part: 'OptgroupHeader', type: 'TextBlock', axes: NONE,
    fixed: { TextWrapping: 'Wrap' },
    styled: () => declarations(styleOf(sc.StyledOptgroupHeader, T, {}), 'OptgroupHeader'),
    convert(s) {
      const what = 'OptgroupHeader';
      check(s, what, [...FONT, 'color', 'paddingTop', 'paddingBottom', 'paddingRight', 'paddingLeft']);
      return { Padding: sides(s, 'padding'), Foreground: colour(s.color, what), ...font(s, what, OWN) };
    },
  },
  {
    part: 'MenuDivider', type: 'Border', axes: NONE,
    styled: () => declarations(styleOf(sc.StyledMenuDivider, T, {}), 'MenuDivider'),
    convert(s) {
      const what = 'MenuDivider';
      check(s, what, ['color', 'borderBottomWidth', 'borderBottomStyle', 'borderBottomColor', 'marginTop', 'marginBottom', 'marginLeft',
        'marginRight', 'listStyle', 'height']);
      // Its height 0, its border below it: a line of the border's width. Its colour is for text it doesn't have.
      if (s.borderBottomStyle !== '@borders.border300.borderStyle' && s.borderBottomStyle !== 'solid') throw new Error(what + ' border style ' + s.borderBottomStyle);
      if (s.height !== 0 || s.listStyle !== 'none') throw new Error(what + ' box');
      const width = /^@borders\.(\w+)\.borderWidth$/.exec(s.borderBottomWidth);
      const w = width ? css.px(L.borders[width[1]].borderWidth) : css.px(s.borderBottomWidth);
      return {
        BorderBrush: token(s.borderBottomColor, what),
        BorderThickness: '0,0,0,' + w,
        Margin: sides(s, 'margin'),
      };
    },
  },
  {
    part: 'EmptyState', type: 'ContentPresenter', axes: NONE,
    styled: () => declarations(styleOf(sc.StyledEmptyState, T, {}), 'EmptyState'),
    convert(s) {
      const what = 'EmptyState';
      check(s, what, [...FONT, 'position', 'display', 'color', 'textAlign', 'cursor', 'backgroundColor', 'marginBottom', 'paddingTop',
        'paddingBottom', 'paddingRight', 'paddingLeft']);
      if (s.display !== 'block' || s.textAlign !== 'center' || s.marginBottom !== 0) throw new Error(what + ' box');
      return {
        HorizontalAlignment: 'Stretch',
        HorizontalContentAlignment: 'Center',
        TextAlignment: 'Center',
        TextWrapping: 'Wrap',
        Padding: sides(s, 'padding'),
        Background: colour(s.backgroundColor, what),
        Foreground: colour(s.color, what),
        ...font(s, what, OWN),
        Cursor: cursor(s.cursor),
      };
    },
  },
  {
    part: 'ListItemProfile', type: 'Border', axes: HOVER,
    // Its padding takes the pointer: drawn, transparent.
    styled: (t, c) => {
      const s = declarations(styleOf(sc.StyledListItemProfile, T, {}), 'ListItemProfile', { ':hover': c.hover });
      return { backgroundColor: 'transparent', ...s };
    },
    convert(s) {
      const what = 'ListItemProfile';
      check(s, what, ['position', 'display', 'alignItems', 'cursor', 'paddingTop', 'paddingBottom', 'paddingRight', 'paddingLeft', ...TRANSITION, 'backgroundColor']);
      if (s.display !== 'flex' || s.alignItems !== 'center') throw new Error(what + ' box');
      return { Padding: sides(s, 'padding'), Background: colour(s.backgroundColor, what), Cursor: cursor(s.cursor) };
    },
    transitionOf: 'color, background-color',
    transitions: [['BrushTransition', 'Background']],
  },
  {
    part: 'ProfileImgContainer', type: 'Border', axes: NONE,
    styled: () => declarations(styleOf(sc.StyledProfileImgContainer, T, {}), 'ProfileImgContainer'),
    convert(s) {
      const what = 'ProfileImgContainer';
      check(s, what, ['width', 'height', 'display', 'justifyContent', 'alignItems']);
      if (s.display !== 'flex' || s.justifyContent !== 'center' || s.alignItems !== 'center') throw new Error(what + ' box');
      // A flex item in the row, centred in it (the row's alignItems).
      return { Width: px(s.width), Height: px(s.height), VerticalAlignment: 'Center' };
    },
  },
  {
    part: 'ProfileImg', type: 'Border', axes: NONE,
    // The image fills its box, clipped to its round corners.
    fixed: { ClipToBounds: 'True' },
    styled: () => declarations(styleOf(sc.StyledProfileImg, T, {}), 'ProfileImg'),
    convert(s) {
      const what = 'ProfileImg';
      check(s, what, ['width', 'height', ...RADII]);
      if (s.width !== '100%' || s.height !== '100%' || same(s, RADII, what) !== '50%') throw new Error(what + ' box');
      // 100% of its container, round: half the container's side.
      const box = styleOf(sc.StyledProfileImgContainer, T, {});
      if (box.width !== box.height) throw new Error(what + ': a container that is not square');
      return { HorizontalAlignment: 'Stretch', VerticalAlignment: 'Stretch', CornerRadius: String(css.px(box.width) / 2) };
    },
  },
  {
    part: 'ProfileLabelsContainer', type: 'StackPanel', axes: NONE,
    styled: () => declarations(styleOf(sc.StyledProfileLabelsContainer, T, {}), 'ProfileLabelsContainer'),
    convert(s) {
      const what = 'ProfileLabelsContainer';
      check(s, what, ['alignSelf', 'display', 'flexDirection', 'marginLeft']);
      if (s.alignSelf !== 'flex-start' || s.display !== 'flex' || s.flexDirection !== 'column') throw new Error(what + ' box');
      return { Orientation: 'Vertical', VerticalAlignment: 'Top', Margin: px(s.marginLeft) + ',0,0,0' };
    },
  },
  ...[['ProfileTitle', sc.StyledProfileTitle], ['ProfileSubtitle', sc.StyledProfileSubtitle], ['ProfileBody', sc.StyledProfileBody]].map(([part, styled]) => ({
    part, type: 'TextBlock', axes: NONE,
    fixed: { TextWrapping: 'Wrap' },
    styled: () => declarations(styleOf(styled, T, {}), part),
    convert(s) {
      check(s, part, [...FONT, 'color', 'marginTop', 'marginBottom', 'marginLeft', 'marginRight']);
      if (sides(s, 'margin') !== '0,0,0,0') throw new Error(part + ' margin');
      return { Foreground: colour(s.color, part), ...font(s, part, OWN) };
    },
  })),
];

/** A part's transition, once loaded (CSS doesn't transition an element's first style; Avalonia would). */
function transition(p) {
  const s = p.styled(L, Object.fromEntries(p.axes.map((a) => [a.name, a.values[0]])));
  if (s.transitionProperty !== p.transitionOf) throw new Error(p.part + ' transition ' + s.transitionProperty);
  const raw = p.part === 'ListItem' ? styleOf(sc.StyledListItemElement, L, optionProps({ size: 'default' })) : styleOf(sc.StyledListItemProfile, L, {});
  const { span, easing } = timing(raw);
  return '    <Style Selector="^:loaded">\n      <Setter Property="Transitions">\n        <Transitions>\n'
    + p.transitions.map(([type, prop]) => '          <' + type + ' Property="' + prop + '" Duration="' + span + '" Easing="' + easing + '" />').join('\n')
    + '\n        </Transitions>\n      </Setter>\n    </Style>';
}

/**
 * A browser's own stylesheet underlines a link (a:any-link { text-decoration: underline }), which its text takes: the
 * decorations Avalonia's text inherits, set on the anchor.
 */
const UNDERLINE = '    <Setter Property="TextBlock.TextDecorations" Value="Underline" />';

const DESCRIBE = {
  List: 'the list\'s box: its fill, radius and shadow, its padding inside its scroller, its outline while focused by keyboard.',
  ListItem: 'an option: its padding and type by size, its colours by the highlight, the menu\'s focus and disabled.',
  ListItemAnchor: 'an href option\'s label, as a link: the default colour whatever the option\'s state, underlined.',
  OptgroupHeader: 'a group\'s header.',
  MenuDivider: 'a divider: a line of the theme\'s border300, inset.',
  EmptyState: 'the message of an empty menu.',
  ListItemProfile: 'a profile option: its padding, tinted under a hovering mouse.',
  ProfileImgContainer: 'the profile\'s image box.',
  ProfileImg: 'the profile\'s image, round.',
  ProfileLabelsContainer: 'the profile\'s title, subtitle and body, in a column.',
  ProfileTitle: 'the profile\'s title.',
  ProfileSubtitle: 'the profile\'s subtitle.',
  ProfileBody: 'the profile\'s body.',
};

function file(version) {
  const themes = PARTS.map((p) => {
    const { plain, blocks } = write(p, p.axes, (styled, t, c) => p.styled(t, c), { layered: true });
    const extra = [];
    if (p.transitions) extra.push(transition(p));
    if (p.part === 'ListItemAnchor') extra.push(UNDERLINE);
    const body = [...plain, ...extra, ...blocks];
    return '  <!-- ' + p.part + ': ' + DESCRIBE[p.part] + ' -->\n  <ControlTheme x:Key="Menu.Styled' + p.part + '" TargetType="' + p.type + '">\n' + body.join('\n') + '\n  </ControlTheme>';
  });
  return '<!--\n  Generated by tools/tokens/gen.js (tools/tokens/menu.js) from baseui ' + version + '\n  (© Uber Technologies, Inc., MIT). Do not edit.\n'
    + '  The themes of Menu\'s parts, Menu.Styled<Part>, from Base Web\'s menu: each part\'s look by the state\n'
    + '  pseudoclasses the Menu and its options set on it (Metered.Controls.Menu), the colours, radii, shadows and\n'
    + '  border colours by token, the lengths read from Base Web\'s own style functions.\n-->\n'
    + '<ResourceDictionary xmlns="https://github.com/avaloniaui"\n                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"\n                    xmlns:b="https://github.com/wdcossey/Metered">\n'
    + themes.join('\n') + '\n</ResourceDictionary>\n';
}

module.exports = { file, PARTS };
