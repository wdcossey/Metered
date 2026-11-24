// The themes of Select's parts, Select.Styled<Part>, and of its tag's, SelectTag.Styled<Part>, generated from Base Web's
// select (baseui/select/styled-components) and tag (baseui/tag/styled-components) with the theme writer the other controls
// share (radio.js): each style function evaluated on a theme whose colours, radii and shadows are their own names, for
// every combination of the states select-component.js gives the part, written as the fewest state styles and checked
// against Base Web's own. The icons are Base Web's own (icon/delete-alt.js, icon/triangle-down.js, icon/search.js,
// icon/delete.js), their sizes the ones select-component.js and tag.js give them. The tests' oracle
// (tools/reference/select.js) checks the result against the real colours.
//
// What the themes hold and what the Select gives (Metered.Controls.Select):
// - Root: the font by size. ControlContainer: the 2 px border, radius, fill and colour by state, its cursor, and its
//   border and fill transition once loaded. ValueContainer: its padding by size, type, multi and emptiness, wrapping for
//   multi. Placeholder, SingleValue, InputContainer, Input: the value's line. IconsContainer and its three: the loading
//   ring, the clear icon and the arrow. SearchIconContainer and SearchIcon: the search type's icon.
// - OptionContent: an option's label in the dropdown, bold when selected and menuFontSelected unless highlighted. The
//   dropdown's other parts are the menu's (Menu.Styled<Part>), as dropdown.js gives the menu StyledList and StyledListItem.
// - The tag's Root, Text, Action and ActionIcon, at what multi-value.js gives Base Web's Tag: gray, primary, small,
//   closeable, the Root's margins its own.
'use strict';

const React = require('react');
const sc = require('baseui/select/styled-components');
const tagSc = require('baseui/tag/styled-components');
const { Svg } = require('baseui/icon/styled-components');
const DeleteAlt = require('baseui/icon/delete-alt').default;
const TriangleDown = require('baseui/icon/triangle-down').default;
const Search = require('baseui/icon/search').default;
const Delete = require('baseui/icon/delete').default;
const css = require('../reference/css');
const oracle = require('../reference/select');
const { L, NAMED, theme: write, colour, px, check, timing, TEXT } = require('./radio');
const { icon, font } = require('./input');

void React;
const r = (key) => '{DynamicResource ' + key + '}';
/** A theme group's string values by their names ('@borders.inputBorderRadius'). */
const named = (group) => new Proxy(L[group], {
  get: (o, name) => (typeof name === 'string' && typeof o[name] === 'string' ? '@' + group + '.' + name : o[name]),
});
const T = { ...NAMED, borders: named('borders'), lighting: named('lighting') };
function token(v, what) {
  if (typeof v === 'string' && /^@(borders|lighting)\./.test(v)) return r(v.slice(1));
  throw new Error(what + ': not a token: ' + JSON.stringify(v));
}
const cursor = (v) => ({ pointer: 'Hand', 'not-allowed': 'No', text: 'Ibeam', default: 'Arrow', wait: 'Wait' })[v] ?? (() => { throw new Error('cursor ' + v); })();

/** A styled component's style for the props: every reducer applied (withStyle's after its base's). */
function styleOf(styled, t, p) {
  const s = styled.__STYLETRON__;
  return s.reducers.reduceRight((acc, x) => x.reducer(acc, { $theme: t, ...p }), s.getInitialStyle());
}
/** The declarations of a style object: its flat ones, and the blocks named applied when on; any other block throws. */
function declarations(style, what, blocks = {}) {
  const out = {};
  for (const [k, v] of Object.entries(style)) {
    if (v !== null && typeof v === 'object') {
      if (!(k in blocks)) throw new Error(what + ': a block this generator does not read: ' + k);
    } else if (v !== null && v !== undefined) out[k] = v;
  }
  for (const [k, on] of Object.entries(blocks)) if (on && style[k]) Object.assign(out, style[k]);
  return out;
}
const sidesOf = (s, prefix) => ['Left', 'Top', 'Right', 'Bottom'].map((k) => oracle.length(s[prefix + k])).join(',');
const RADII = ['borderTopLeftRadius', 'borderTopRightRadius', 'borderBottomRightRadius', 'borderBottomLeftRadius'];
function same(s, keys, what) {
  const v = new Set(keys.map((k) => s[k]));
  if (v.size !== 1) throw new Error(what + ': ' + keys.join(', ') + ' differ');
  return [...v][0];
}
const FONT = ['fontFamily', 'fontSize', 'fontWeight', 'lineHeight'];
const INHERITED = TEXT(true);
const OWN = TEXT(false);

// ---- The select's states, as the Select mirrors them onto itself and every part (getSharedProps) ----

const SIZE = { name: 'size', values: ['default', 'mini', 'compact', 'large'], pseudo: (v) => ':size-' + v };
const AX = {
  size: SIZE,
  disabled: { name: 'disabled', values: [false, true], pseudo: () => ':disabled' },
  isFocused: { name: 'isFocused', values: [false, true], pseudo: () => ':is-focused' },
  isPseudoFocused: { name: 'isPseudoFocused', values: [false, true], pseudo: () => ':is-pseudo-focused' },
  error: { name: 'error', values: [false, true], pseudo: () => ':error' },
  positive: { name: 'positive', values: [false, true], pseudo: () => ':positive' },
  multi: { name: 'multi', values: [false, true], pseudo: () => ':multi' },
  hasValue: { name: 'hasValue', values: [false, true], pseudo: () => ':has-value' },
  searchable: { name: 'searchable', values: [false, true], pseudo: () => ':searchable' },
  search: { name: 'search', values: [false, true], pseudo: () => ':type-search' },
};
const axes = (...names) => names.map((n) => AX[n]);
/** select-component.js's sharedProps for a combination. */
const shared = (c) => ({
  $size: c.size ?? 'default', $disabled: !!c.disabled, $isFocused: !!c.isFocused, $isPseudoFocused: !!c.isPseudoFocused, $error: !!c.error,
  $positive: !!c.positive, $multi: !!c.multi, $isEmpty: !c.hasValue, $searchable: !!c.searchable, $type: c.search ? 'search' : 'select',
});

/** The icons' sizes select-component.js gives them, by size, read from Base Web's own Select rendered. */
function iconSizes(part, props = {}) {
  const out = {};
  for (const size of SIZE.values) {
    const seen = oracle.render({ options: oracle.OPTIONS, value: [oracle.OPTIONS[1]], size, ...props });
    out[size] = seen.find((x) => x.part === part).props.size;
  }
  return out;
}
const CLEAR_SIZES = iconSizes('ClearIcon');
const ARROW_SIZES = iconSizes('SelectArrow');
const SEARCH_SIZE = (() => { const s = new Set(Object.values(iconSizes('SearchIcon', { type: 'search' }))); if (s.size !== 1) throw new Error('search icon size'); return [...s][0]; })();
/** An icon's box: icon/styled-components.js's Svg at a size. */
function svg(size, what) {
  const s = declarations(styleOf(Svg, T, { $size: size }), what);
  check(s, what, ['display', 'fill', 'color', 'height', 'width']);
  if (s.display !== 'inline-block' || s.fill !== 'currentColor' || s.width !== s.height) throw new Error(what + ' svg');
  return { Width: px(s.width), Height: px(s.height) };
}
const CLEAR = icon(DeleteAlt), ARROW = icon(TriangleDown), SEARCH = icon(Search);
/** tag.js's ModifiedViewBoxDeleteIcon: Base Web's Delete with its viewBox moved in. */
const DELETE = (() => {
  const d = icon(Delete);
  return { data: d.data, viewBox: '5,5,13.186,13.186' };
})();

// The arrow's Svg override (select-component.js's renderArrow): its colour, read from the rendered SelectArrow's props.
const ARROW_STYLE = (() => {
  const seen = oracle.render({ options: oracle.OPTIONS, value: [] });
  return seen.find((x) => x.part === 'SelectArrow').props.overrides.Svg.style;
})();

const TRANSITION = ['transitionProperty', 'transitionDuration', 'transitionTimingFunction'];

const PARTS = [
  {
    part: 'Root', type: 'Border', axes: axes('size'),
    styled: (t, c) => declarations(styleOf(sc.StyledRoot, T, shared(c)), 'Root'),
    convert(s) {
      check(s, 'Root', [...FONT, 'boxSizing', 'position', 'width']);
      if (s.width !== '100%' || s.position !== 'relative') throw new Error('Root box');
      return { HorizontalAlignment: 'Stretch', ...font(s, 'Root', INHERITED) };
    },
  },
  {
    part: 'ControlContainer', type: 'Border', axes: axes('size', 'disabled', 'isFocused', 'isPseudoFocused', 'error', 'positive', 'searchable', 'search'),
    // overflow: hidden.
    fixed: { ClipToBounds: 'True' },
    styled: (t, c) => declarations(styleOf(sc.StyledControlContainer, T, shared(c)), 'ControlContainer'),
    convert(s) {
      const what = 'ControlContainer';
      check(s, what, [...RADII, 'boxSizing', 'overflow', 'width', 'display', 'justifyContent', 'cursor',
        'borderLeftWidth', 'borderRightWidth', 'borderTopWidth', 'borderBottomWidth', 'borderLeftStyle', 'borderRightStyle', 'borderTopStyle',
        'borderBottomStyle', ...TRANSITION, 'color', 'borderLeftColor', 'borderRightColor', 'borderTopColor', 'borderBottomColor', 'backgroundColor']);
      if (s.display !== 'flex' || s.justifyContent !== 'space-between' || s.overflow !== 'hidden') throw new Error(what + ' box');
      return {
        CornerRadius: token(same(s, RADII, what), what),
        BorderThickness: ['Left', 'Top', 'Right', 'Bottom'].map((k) => css.px(s['border' + k + 'Width'])).join(','),
        BorderBrush: colour(same(s, ['borderLeftColor', 'borderRightColor', 'borderTopColor', 'borderBottomColor'], what), what),
        Background: colour(s.backgroundColor, what),
        [INHERITED.Foreground]: colour(s.color, what),
        Cursor: cursor(s.cursor),
      };
    },
    transitionOf: 'border, box-shadow, background-color',
    transitions: [['BrushTransition', 'BorderBrush'], ['BrushTransition', 'Background']],
    raw: () => styleOf(sc.StyledControlContainer, L, shared({})),
  },
  {
    part: 'ValueContainer', type: 'b:SelectValueContainer', axes: axes('size', 'multi', 'hasValue', 'search'),
    fixed: { ClipToBounds: 'True' },
    styled: (t, c) => declarations(styleOf(sc.StyledValueContainer, T, shared(c)), 'ValueContainer'),
    convert(s) {
      const what = 'ValueContainer';
      check(s, what, ['boxSizing', 'position', 'flexGrow', 'flexShrink', 'flexBasis', 'display', 'alignItems', 'flexWrap', 'overflow',
        'paddingTop', 'paddingBottom', 'paddingLeft', 'paddingRight']);
      if (s.display !== 'flex' || s.alignItems !== 'center' || s.overflow !== 'hidden' || s.flexGrow !== 1) throw new Error(what + ' box');
      return { Padding: sidesOf(s, 'padding'), Wrap: s.flexWrap === 'wrap' ? 'True' : 'False' };
    },
  },
  {
    part: 'Placeholder', type: 'TextBlock', axes: axes('disabled'),
    fixed: { TextTrimming: 'CharacterEllipsis', TextWrapping: 'NoWrap', VerticalAlignment: 'Center' },
    styled: (t, c) => declarations(styleOf(sc.StyledPlaceholder, T, shared(c)), 'Placeholder'),
    convert(s) {
      check(s, 'Placeholder', ['color', 'maxWidth', 'overflow', 'textOverflow', 'whiteSpace']);
      if (s.maxWidth !== '100%' || s.textOverflow !== 'ellipsis' || s.whiteSpace !== 'nowrap') throw new Error('Placeholder box');
      return { Foreground: colour(s.color, 'Placeholder') };
    },
  },
  {
    part: 'SingleValue', type: 'ContentPresenter', axes: axes('size', 'searchable'),
    fixed: { [INHERITED.TextTrimming]: 'CharacterEllipsis', [INHERITED.TextWrapping]: 'NoWrap', VerticalAlignment: 'Center' },
    styled: (t, c) => declarations(styleOf(sc.StyledSingleValue, T, shared(c)), 'SingleValue'),
    convert(s, c) {
      const what = 'SingleValue';
      check(s, what, ['lineHeight', 'boxSizing', 'marginLeft', 'height', 'maxWidth', 'overflow', 'whiteSpace', 'textOverflow', 'wordWrap']);
      if (s.maxWidth !== '100%' || s.textOverflow !== 'ellipsis' || s.whiteSpace !== 'nowrap' || s.height !== '100%') throw new Error(what + ' box');
      // 'inherit' is the root's line height, getFont's for the size, as the non-searchable value's own.
      const lh = s.lineHeight === 'inherit' ? styleOf(sc.StyledRoot, T, shared(c)).lineHeight : s.lineHeight;
      const tokenName = Object.keys(L.typography).find((n) => /^font\d+$/.test(n) && L.typography[n].lineHeight === lh && L.typography[n] === ({ mini: L.typography.font100, compact: L.typography.font200, default: L.typography.font300, large: L.typography.font400 })[c.size]);
      if (!tokenName) throw new Error(what + ' line height');
      return { Margin: px(s.marginLeft) + ',0,0,0', [INHERITED.LineHeight]: r('typography.' + tokenName + '.lineHeight') };
    },
  },
  {
    part: 'InputContainer', type: 'b:SelectInputContainer', axes: axes('size', 'searchable', 'hasValue'),
    fixed: { VerticalAlignment: 'Center' },
    styled: (t, c) => declarations(styleOf(sc.StyledInputContainer, T, shared(c)), 'InputContainer'),
    convert(s, c) {
      const what = 'InputContainer';
      check(s, what, ['position', 'display', 'maxWidth', 'backgroundColor', 'borderLeftStyle', 'borderTopStyle', 'borderRightStyle', 'borderBottomStyle',
        'boxShadow', 'boxSizing', 'outline', 'marginTop', 'marginBottom', 'marginLeft', 'marginRight', 'paddingTop', 'paddingBottom', 'paddingLeft',
        'paddingRight', 'height', 'maxHeight']);
      if (s.display !== 'inline-block' || s.backgroundColor !== 'transparent' || sidesOf(s, 'padding') !== '0,0,0,0') throw new Error(what + ' box');
      const lh = (v) => {
        const n = Object.keys(L.typography).find((k) => /^font\d+$/.test(k) && L.typography[k] === ({ mini: L.typography.font100, compact: L.typography.font200, default: L.typography.font300, large: L.typography.font400 })[c.size]);
        if (L.typography[n].lineHeight !== v) throw new Error(what + ' line height ' + v);
        return r('typography.' + n + '.lineHeight');
      };
      return { Margin: sidesOf(s, 'margin'), Height: s.height === 'auto' ? 'NaN' : lh(s.height), MaxHeight: lh(s.maxHeight) };
    },
  },
  {
    part: 'Input', type: 'b:SelectInput', axes: axes('size'),
    // A bare text box: transparent, borderless, unpadded, its focus Base Web's control container's border.
    fixed: { Background: 'Transparent', BorderThickness: '0', Padding: '0', MinHeight: '0', MinWidth: '0', FocusAdorner: '{x:Null}', VerticalAlignment: 'Center',
      VerticalContentAlignment: 'Center' },
    styled: (t, c) => declarations(styleOf(sc.StyledInput, T, { ...shared(c), $searchable: true, $width: '2px' }), 'Input'),
    convert(s) {
      const what = 'Input';
      check(s, what, [...FONT, 'color', 'boxSizing', 'width', 'maxWidth', 'background', 'borderLeftStyle', 'borderTopStyle', 'borderRightStyle',
        'borderBottomStyle', 'boxShadow', 'display', 'outline', 'marginTop', 'marginBottom', 'marginLeft', 'marginRight', 'paddingTop', 'paddingBottom',
        'paddingLeft', 'paddingRight']);
      if (s.width !== '2px' || s.maxWidth !== '100%' || s.background !== 'transparent') throw new Error(what + ' box');
      // Its width is AutosizeInput's, the text's and 2 px (the Select's code); the caret is currentColor, a browser's.
      return { ...font(s, what, { ...OWN, LineHeight: 'LineHeight' }), Foreground: colour(s.color, what), CaretBrush: colour(s.color, what) };
    },
  },
  {
    part: 'IconsContainer', type: 'StackPanel', axes: axes('size'),
    fixed: { Orientation: 'Horizontal', VerticalAlignment: 'Stretch' },
    styled: (t, c) => declarations(styleOf(sc.StyledIconsContainer, T, shared(c)), 'IconsContainer'),
    convert(s) {
      check(s, 'IconsContainer', ['boxSizing', 'position', 'display', 'flexShrink', 'alignItems', 'alignSelf', 'paddingRight']);
      if (s.display !== 'flex' || s.alignItems !== 'center' || s.alignSelf !== 'stretch' || s.flexShrink !== 0) throw new Error('IconsContainer box');
      // Its padding as a margin: a StackPanel has none, and it draws nothing. Its icons centred in its height (their own).
      return { Margin: '0,0,' + px(s.paddingRight) + ',0' };
    },
  },
  {
    part: 'LoadingIndicator', type: 'b:SpinnerRing', axes: axes('size'),
    fixed: { VerticalAlignment: 'Center' },
    styled: (t, c) => declarations(styleOf(sc.StyledLoadingIndicator, T, {}), 'LoadingIndicator', { animationName: false }),
    convert(s) {
      const what = 'LoadingIndicator';
      check(s, what, ['display', 'animationName', 'animationDuration', 'animationIterationCount', 'animationTimingFunction', 'borderLeftStyle',
        'borderRightStyle', 'borderTopStyle', 'borderBottomStyle', 'borderRadius', 'borderTopColor', 'borderRightColor', 'borderBottomColor',
        'borderLeftColor', 'borderLeftWidth', 'borderRightWidth', 'borderTopWidth', 'borderBottomWidth', 'width', 'height', 'cursor']);
      if (s.width !== s.height || s.borderRadius !== '50%' || s.animationIterationCount !== 'infinite' || s.animationTimingFunction !== 'linear') throw new Error(what + ' box');
      return {
        BoxSize: px(s.width), Thickness: px(same(s, ['borderLeftWidth', 'borderRightWidth', 'borderTopWidth', 'borderBottomWidth'], what)),
        Foreground: colour(s.borderTopColor, what), Background: colour(same(s, ['borderRightColor', 'borderBottomColor', 'borderLeftColor'], what), what),
        Cursor: cursor(s.cursor),
      };
    },
    // The spinner's turn: once a turn every animationDuration, linear, unless held still.
    spin: () => styleOf(sc.StyledLoadingIndicator, L, {}).animationDuration,
  },
  {
    // The icon fills with currentColor: the control container's colour, inherited.
    part: 'ClearIcon', type: 'b:InputIcon', axes: axes('size'),
    fixed: { VerticalAlignment: 'Center', Data: CLEAR.data, ViewBox: CLEAR.viewBox },
    styled: (t, c) => svg(CLEAR_SIZES[c.size], 'ClearIcon'),
    convert: (s) => ({ Width: s.Width, Height: s.Height }),
  },
  {
    part: 'SelectArrow', type: 'b:InputIcon', axes: axes('size', 'disabled'),
    fixed: { VerticalAlignment: 'Center', Data: ARROW.data, ViewBox: ARROW.viewBox },
    styled: (t, c) => ({ ...svg(ARROW_SIZES[c.size], 'SelectArrow'), ...ARROW_STYLE({ $theme: T, ...shared(c) }) }),
    convert(s) {
      check(s, 'SelectArrow', ['Width', 'Height', 'color']);
      return { Width: s.Width, Height: s.Height, Foreground: colour(s.color, 'SelectArrow') };
    },
  },
  {
    part: 'SearchIconContainer', type: 'Border', axes: axes('disabled'),
    // position: absolute, at the root's start: the root's own overlay, its full height.
    fixed: { HorizontalAlignment: 'Left', VerticalAlignment: 'Stretch', Background: 'Transparent' },
    styled: (t, c) => declarations(styleOf(sc.StyledSearchIconContainer, T, shared(c)), 'SearchIconContainer'),
    convert(s) {
      const what = 'SearchIconContainer';
      check(s, what, ['display', 'fill', 'color', 'height', 'width', 'cursor', 'position', 'top', 'left', 'alignItems']);
      if (s.position !== 'absolute' || s.top !== 0 || s.height !== '100%' || s.display !== 'flex' || s.alignItems !== 'center') throw new Error(what + ' box');
      return { Margin: px(s.left) + ',0,0,0', Width: px(s.width), [INHERITED.Foreground]: colour(s.color, what), Cursor: cursor(s.cursor) };
    },
  },
  {
    part: 'SearchIcon', type: 'b:InputIcon', axes: axes('size'),
    fixed: { VerticalAlignment: 'Center', Data: SEARCH.data, ViewBox: SEARCH.viewBox },
    styled: () => svg(SEARCH_SIZE, 'SearchIcon'),
    convert: (s) => ({ Width: s.Width, Height: s.Height }),
  },
];

// ---- The tag, at what multi-value.js gives Base Web's Tag ----

/** tag.js's sharedProps for the props multi-value.js gives it (hierarchy primary, its defaults: gray, small, closeable). */
const tagProps = (c) => ({ $clickable: false, $closeable: true, $disabled: !!c.disabled, $isFocused: false, $isHovered: false, $kind: 'gray',
  $hierarchy: 'primary', $isFocusVisible: !!c.focusVisible, $size: 'small', $noMargin: false });
// multi-value.js's Root override style: its margins.
const TAG_ROOT_STYLE = (() => {
  const seen = oracle.render({ options: oracle.OPTIONS, value: [oracle.OPTIONS[1]], multi: true });
  const tag = seen.find((x) => x.part === 'Tag');
  for (const [k, v] of Object.entries({ hierarchy: 'primary' })) if (tag.props[k] !== v) throw new Error('Tag ' + k);
  for (const k of ['kind', 'size', 'closeable', 'color']) if (tag.props[k] !== undefined) throw new Error('Tag ' + k + ' given');
  return tag.props.overrides.Root.style;
})();
const TAG_AX = {
  disabled: { name: 'disabled', values: [false, true], pseudo: () => ':disabled' },
  focusVisible: { name: 'focusVisible', values: [false, true], pseudo: () => ':focus-visible' },
  hover: { name: 'hover', values: [false, true], pseudo: () => ':hover' },
  active: { name: 'active', values: [false, true], pseudo: () => ':active' },
};

const TAG_PARTS = [
  {
    part: 'Root', type: 'b:RadioMark', axes: Object.values(TAG_AX),
    fixed: { VerticalAlignment: 'Center', HorizontalAlignment: 'Left' },
    styled: (t, c) => {
      const s = { ...styleOf(tagSc.Root, T, tagProps(c)), ...TAG_ROOT_STYLE({ $theme: T }) };
      const media = s['@media (hover: hover)'] || {};
      const d = declarations(s, 'Tag.Root', { '::before': false, '@media (hover: hover)': false, ':active::before': false, ':focus': true });
      // The overlay (::before) over its border box: the hover's (a mouse's) under the press's.
      let overlay = 'transparent';
      if (s['::before']) overlay = s['::before'].backgroundColor;
      if (c.hover && media[':hover::before']) overlay = media[':hover::before'].backgroundColor;
      if (c.active && s[':active::before']) overlay = s[':active::before'].backgroundColor;
      return { ...d, overlay, focusShadow: c.focusVisible && s[':focus'] ? s[':focus'].boxShadow : undefined };
    },
    convert(s) {
      const what = 'Tag.Root';
      check(s, what, [...FONT, 'alignItems', 'color', 'backgroundColor', 'borderLeftColor', 'borderRightColor', 'borderTopColor', 'borderBottomColor',
        'borderLeftStyle', 'borderRightStyle', 'borderTopStyle', 'borderBottomStyle', 'borderLeftWidth', 'borderRightWidth', 'borderTopWidth',
        'borderBottomWidth', ...RADII, 'boxSizing', 'cursor', 'display', 'height', 'justifyContent', 'marginTop', 'marginBottom', 'marginLeft',
        'marginRight', 'paddingTop', 'paddingBottom', 'paddingLeft', 'paddingRight', 'outline', 'position', 'overlay', 'focusShadow', 'boxShadow']);
      if (s.display !== 'inline-flex' || s.alignItems !== 'center' || s.boxSizing !== 'border-box') throw new Error(what + ' box');
      const bw = css.px(same(s, ['borderLeftWidth', 'borderRightWidth', 'borderTopWidth', 'borderBottomWidth'], what) ?? 0);
      const border = s.borderTopColor === undefined ? '{x:Null}' : colour(same(s, ['borderLeftColor', 'borderRightColor', 'borderTopColor', 'borderBottomColor'], what), what);
      let outline = { OutlineBrush: '{x:Null}', OutlineThickness: '0' };
      const shadow = s.focusShadow ?? s.boxShadow;
      if (shadow !== undefined && shadow !== 'none') {
        // '0 0 0 3px @x': a ring as wide as its spread, outside the border box, round its radius: CSS's outline here.
        const m = /^0 0 0 (\S+) (@\w+)$/.exec(shadow);
        if (!m) throw new Error(what + ' focus ' + shadow);
        outline = { OutlineBrush: colour(m[2], what), OutlineThickness: px(m[1]) };
      }
      return {
        Height: px(s.height), Margin: sidesOf(s, 'margin'), Padding: sidesOf(s, 'padding'),
        BorderThickness: String(bw), BorderBrush: border, Background: colour(s.backgroundColor, what),
        CornerRadius: token(same(s, RADII, what), what),
        [INHERITED.Foreground]: colour(s.color, what), ...tagFont(s, what), Cursor: cursor(s.cursor),
        Overlay: colour(s.overlay, what), ...outline, OutlineOffset: '0',
      };
    },
  },
  {
    // tag.js's Text span round its children, any content: a presenter, its text cut with an ellipsis.
    part: 'Text', type: 'ContentPresenter', axes: [TAG_AX.disabled],
    fixed: { [INHERITED.TextTrimming]: 'CharacterEllipsis', [INHERITED.TextWrapping]: 'NoWrap', VerticalAlignment: 'Center' },
    styled: (t, c) => declarations(styleOf(tagSc.Text, T, { ...tagProps(c), $contentMaxWidth: undefined }), 'Tag.Text'),
    convert(s) {
      check(s, 'Tag.Text', ['overflow', 'whiteSpace', 'textOverflow', 'maxWidth', 'order']);
      if (s.textOverflow !== 'ellipsis' || s.whiteSpace !== 'nowrap') throw new Error('Tag.Text box');
      return { MaxWidth: px(s.maxWidth) };
    },
  },
  {
    part: 'Action', type: 'Border', axes: [TAG_AX.disabled],
    fixed: { VerticalAlignment: 'Stretch', Background: 'Transparent' },
    styled: (t, c) => declarations(styleOf(tagSc.Action, T, tagProps(c)), 'Tag.Action'),
    convert(s) {
      const what = 'Tag.Action';
      check(s, what, ['alignItems', 'borderBottomRightRadius', 'borderTopRightRadius', 'cursor', 'display', 'marginLeft', 'outline', ...TRANSITION]);
      if (s.display !== 'flex' || s.alignItems !== 'center') throw new Error(what + ' box');
      // transitionDuration 'background-color' is no time: CSS drops the transition.
      if (s.transitionDuration !== 'background-color') throw new Error(what + ' transition');
      const radius = token(same(s, ['borderBottomRightRadius', 'borderTopRightRadius'], what), what);
      return { Margin: px(s.marginLeft) + ',0,0,0', CornerRadius: radius.replace('DynamicResource ', 'DynamicResource ') , Cursor: cursor(s.cursor) };
    },
  },
  {
    part: 'ActionIcon', type: 'b:InputIcon', axes: [TAG_AX.disabled],
    fixed: { VerticalAlignment: 'Center', Data: DELETE.data, ViewBox: DELETE.viewBox },
    styled: () => svg(tagActionSize(), 'Tag.ActionIcon'),
    convert: (s) => ({ Width: s.Width, Height: s.Height }),
  },
];

/** The ActionIcon's size tag.js gives it at its size (small), read from Base Web's Tag rendered through the select. */
function tagActionSize() {
  const seen = oracle.render({ options: oracle.OPTIONS, value: [oracle.OPTIONS[1]], multi: true });
  return seen.find((x) => x.part === 'Tag').parts.find((y) => y.part === 'Tag.ActionIcon').props.size;
}
/** The tag's type, a Label token (LabelSmall). */
function tagFont(s, what) {
  const name = Object.keys(L.typography).find((t) => /^Label/.test(t) && FONT.every((k) => L.typography[t][k] === s[k]));
  if (!name) throw new Error(what + ': no Label token');
  return {
    [INHERITED.FontFamily]: r('typography.' + name + '.fontFamily'), [INHERITED.FontSize]: r('typography.' + name + '.fontSize'),
    [INHERITED.FontWeight]: r('typography.' + name + '.fontWeight'), [INHERITED.LineHeight]: r('typography.' + name + '.lineHeight'),
  };
}

// ---- OptionContent: written by hand from its function's three branches, then checked against it ----

function optionContent() {
  const what = 'OptionContent';
  const want = { plain: [], states: [] };
  const lines = [
    '    <Setter Property="' + INHERITED.FontWeight + '" Value="Normal" />',
    '    <Setter Property="Cursor" Value="Hand" />',
    '    <Style Selector="^:is-selected">\n      <Setter Property="' + INHERITED.FontWeight + '" Value="Bold" />\n    </Style>',
    '    <Style Selector="^:is-selected:not(:is-highlighted)">\n      <Setter Property="' + INHERITED.Foreground + '" Value="' + r('colors.menuFontSelected') + '" />\n    </Style>',
    '    <Style Selector="^:is-disabled">\n      <Setter Property="Cursor" Value="No" />\n    </Style>',
  ];
  void want;
  // Checked: every combination's colour (none: the option's, inherited), weight and cursor as Base Web's.
  for (const selected of [false, true]) for (const highlighted of [false, true]) for (const disabled of [false, true]) {
    const s = declarations(styleOf(sc.StyledOptionContent, T, { $selected: selected, $isHighlighted: highlighted, $disabled: disabled }), what);
    check(s, what, ['cursor', 'color', 'fontWeight']);
    const colourWant = selected && !highlighted ? '@menuFontSelected' : undefined;
    if (s.color !== colourWant) throw new Error(what + ' colour ' + JSON.stringify(s.color));
    if (css.fontWeight(s.fontWeight) !== (selected ? 'Bold' : 'Normal')) throw new Error(what + ' weight');
    if (cursor(s.cursor) !== (disabled ? 'No' : 'Hand')) throw new Error(what + ' cursor');
  }
  return lines;
}

/** A part's transition, once loaded (CSS doesn't transition an element's first style; Avalonia would). */
function transition(p) {
  const raw = p.raw();
  if (raw.transitionProperty !== p.transitionOf) throw new Error(p.part + ' transition ' + raw.transitionProperty);
  const { span, easing } = timing(raw);
  return '    <Style Selector="^:loaded">\n      <Setter Property="Transitions">\n        <Transitions>\n'
    + p.transitions.map(([type, prop]) => '          <' + type + ' Property="' + prop + '" Duration="' + span + '" Easing="' + easing + '" />').join('\n')
    + '\n        </Transitions>\n      </Setter>\n    </Style>';
}

/** The loading ring's turn, unless it is held still, as the Spinner's. */
function spin(p) {
  // The spinner's keyframes: one turn, from 0 to 360 degrees.
  const frames = styleOf(sc.StyledLoadingIndicator, L, {}).animationName;
  if (JSON.stringify(frames) !== JSON.stringify({ from: { transform: 'rotate(0deg)' }, to: { transform: 'rotate(360deg)' } })) throw new Error('spin ' + JSON.stringify(frames));
  const seconds = css.seconds(p.spin());
  const span = new Date(seconds * 1000).toISOString().substr(11, 12);
  return '    <Style Selector="^:not(:metered-still)">\n      <Style.Animations>\n        <Animation Duration="' + span + '" IterationCount="Infinite" Easing="LinearEasing">\n'
    + '          <KeyFrame Cue="0%"><Setter Property="RotateTransform.Angle" Value="0" /></KeyFrame>\n'
    + '          <KeyFrame Cue="100%"><Setter Property="RotateTransform.Angle" Value="360" /></KeyFrame>\n'
    + '        </Animation>\n      </Style.Animations>\n    </Style>';
}

const DESCRIBE = {
  Root: 'the select\'s box: its font by size.',
  ControlContainer: 'the control: its 2 px border, radius, fill and colour by state, its cursor.',
  ValueContainer: 'the values\' line: its padding by size, type, multi and emptiness; wrapping for multi.',
  Placeholder: 'the placeholder, while there is no value and no text.',
  SingleValue: 'a single value\'s label, while there is no text.',
  InputContainer: 'the input\'s box; focusable itself when the select isn\'t searchable.',
  Input: 'the searchable select\'s text box: the text\'s width and 2 px.',
  IconsContainer: 'the icons at the end, their padding after them.',
  LoadingIndicator: 'the loading ring: Base Web\'s spinner at 16 px with a 2 px border, turning.',
  ClearIcon: 'Base Web\'s delete-alt icon, sized by the select\'s size.',
  SelectArrow: 'Base Web\'s triangle-down icon, sized by the select\'s size.',
  SearchIconContainer: 'the search type\'s icon box, over the root\'s start, its full height.',
  SearchIcon: 'Base Web\'s search icon.',
  OptionContent: 'an option\'s label in the dropdown: bold when selected, menuFontSelected unless highlighted.',
};
const TAG_DESCRIBE = {
  Root: 'the tag: gray and primary, small, its border while it can be removed, the overlay under a hovering mouse and while pressed, its ring on keyboard focus.',
  Text: 'the tag\'s label, cut with an ellipsis.',
  Action: 'the remove action\'s box.',
  ActionIcon: 'Base Web\'s delete icon, in tag.js\'s viewBox.',
};

function themeOf(prefix, p, describe) {
  const { plain, blocks } = write(p, p.axes, (styled, t, c) => p.styled(t, c), { layered: true });
  const extra = [];
  if (p.transitions) extra.push(transition(p));
  if (p.spin) extra.push(spin(p));
  const body = [...plain, ...extra, ...blocks];
  return '  <!-- ' + p.part + ': ' + describe[p.part] + ' -->\n  <ControlTheme x:Key="' + prefix + '.Styled' + p.part + '" TargetType="' + p.type + '">\n' + body.join('\n') + '\n  </ControlTheme>';
}

function file(version) {
  const themes = PARTS.map((p) => themeOf('Select', p, DESCRIBE));
  themes.push('  <!-- OptionContent: ' + DESCRIBE.OptionContent + ' -->\n  <ControlTheme x:Key="Select.StyledOptionContent" TargetType="ContentPresenter">\n' + optionContent().join('\n') + '\n  </ControlTheme>');
  for (const p of TAG_PARTS) themes.push(themeOf('SelectTag', p, TAG_DESCRIBE));
  return '<!--\n  Generated by tools/tokens/gen.js (tools/tokens/select.js) from baseui ' + version + '\n  (© Uber Technologies, Inc., MIT). Do not edit.\n'
    + '  The themes of Select\'s parts, Select.Styled<Part>, from Base Web\'s select, and of its tag\'s, SelectTag.Styled<Part>,\n'
    + '  from Base Web\'s tag: each part\'s look by the state pseudoclasses the Select and its tags mirror onto it\n'
    + '  (Metered.Controls.Select), the colours and radii by token, the lengths read from Base Web\'s own style functions,\n'
    + '  the icons from Base Web\'s own (icon/delete-alt.js, icon/triangle-down.js, icon/search.js, icon/delete.js).\n-->\n'
    + '<ResourceDictionary xmlns="https://github.com/avaloniaui"\n                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"\n                    xmlns:b="https://github.com/wdcossey/Metered">\n'
    + themes.join('\n') + '\n</ResourceDictionary>\n';
}

module.exports = { file, PARTS, TAG_PARTS, CLEAR_SIZES, ARROW_SIZES, SEARCH_SIZE };
