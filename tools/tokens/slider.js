// The themes of Slider's parts, Slider.Styled<Part>, generated from Base Web's slider (baseui/slider/styled-components)
// with the theme writer the v2 controls share (radio.js): each style function evaluated on a theme whose colours are
// their own names, for every combination of the slider's states, written as the fewest state styles and checked against
// Base Web's own. The tests' oracle (tools/reference/slider.js) checks the result against the real colours.
//
// Also the thumb's box-shadow, a literal colour in Base Web's style function, as a token (Tokens/SliderBoxShadows.g.axaml,
// written by gen.js): CLAUDE.md's rule 5 keeps literal colours to Tokens/.
'use strict';

const css = require('../reference/css');
const { evaluate } = require('../reference/oracle');
const { L, NAMED, theme: write, colour, px, uniform, check, RADII, TEXT, font } = require('./radio');
const slider = require('baseui/slider/styled-components');

/**
 * The slider's states, each with its values (the default first) and the pseudoclass that carries it: Base Web's $ props.
 * `range` is `$value.length === 2`. `isDragged` is the Track's and InnerTrack's for any thumb, and a thumb's, its
 * ThumbValue's and its InnerThumb's for that thumb (slider.js's renderTrack and renderThumb); `isFocusVisible` is a
 * thumb's own keyboard focus on the Thumb, and the slider's on the other parts (slider.js's sharedProps).
 */
const AXES = [
  { name: 'disabled', values: [false, true], pseudo: () => ':disabled' },
  { name: 'range', values: [false, true], pseudo: () => ':range' },
  { name: 'isDragged', values: [false, true], pseudo: () => ':is-dragged' },
  { name: 'isFocusVisible', values: [false, true], pseudo: () => ':focus-visible' },
];

/** The values the generator evaluates with: one thumb at 30, or two at 20 and 70, on 0 to 100. */
const ONE = [30];
const TWO = [20, 70];

/** Base Web's $ props for a combination (slider.js's sharedProps, and $isDragged and $thumbIndex where it passes them). */
const props = (c, extra = {}) => ({
  $disabled: !!c.disabled, $step: 1, $min: 0, $max: 100, $marks: false, $value: c.range ? TWO : ONE,
  $isFocusVisible: !!c.isFocusVisible, $isDragged: !!c.isDragged, $thumbIndex: 0, ...extra,
});

/** A styled component's style object: its style function's, or for one with none (Root) its static style. */
function styleOf(styled, theme, p) {
  const s = styled.__STYLETRON__;
  if (s && Array.isArray(s.reducers) && s.reducers.length === 0) return s.getInitialStyle();
  return evaluate(styled, theme, p);
}

/** The declarations a browser applies to the part in that combination. */
function declarations(styled, theme, c, extra) {
  const style = styleOf(styled, theme, props(c, extra));
  const self = {};
  for (const [k, v] of Object.entries(style)) {
    if (v !== null && typeof v === 'object') throw new Error(`${styled.displayName}: a block this generator doesn't read: ${k}`);
    if (v !== undefined) self[k] = v;
  }
  return self;
}

/**
 * react-range's own style on each thumb (react-range/lib/Range.js, render: renderThumb's props.style), which Base Web
 * spreads onto the Thumb as an inline style, so it outranks the style function's: only its cursor is something this
 * generator reads (position and transform are the InnerTrack's layout; Base Web sets zIndex back to unset).
 */
const reactRangeThumbCursor = (c) => (c.disabled ? 'inherit' : c.isDragged ? 'grabbing' : 'grab');

/**
 * CSS's cursor as Avalonia's: Avalonia has no grab cursors, so grab and grabbing are its hand, as pointer is. inherit is
 * the parent's cursor, by a binding: CSS's computed value of the parent.
 */
const INHERIT = '{Binding $parent[InputElement].Cursor}';
function cursor(v) {
  const c = { pointer: 'Hand', grab: 'Hand', grabbing: 'Hand', 'not-allowed': 'No', inherit: INHERIT }[v];
  if (c === undefined) throw new Error(`cursor ${v}`);
  return c;
}

/** The thumb's resting box-shadow, a literal in Base Web's style function, and its token. */
const SHADOW_KEY = 'slider.boxShadow.thumb';
const restShadow = (theme) => declarations(slider.Thumb, theme, {}).boxShadow;

/**
 * react-range's getTrackBackground, as Base Web's InnerTrack writes it: `linear-gradient(to right, <c0> 0%, …)` with a
 * hard stop at each thumb. One thumb: the selection (c0) from min to it, then the rest; two: the rest, the selection
 * between them, the rest. Read back as the two colours, checked against the thumbs' places.
 */
function gradient(background, values, what) {
  const m = /^linear-gradient\(to right, (.*)\)$/.exec(background);
  if (!m) throw new Error(`${what} background ${background}`);
  const stops = m[1].split(', ').map((stop) => {
    const s = /^(\S+) ([\d.]+)%$/.exec(stop);
    if (!s) throw new Error(`${what} stop ${stop}`);
    return { colour: s[1], at: Number(s[2]) };
  });
  const at = [0, ...values.flatMap((v) => [v, v]), 100];
  if (stops.length !== at.length || stops.some((s, i) => s.at !== at[i])) throw new Error(`${what}: stops ${m[1]} for ${values}`);
  const colours = [];
  for (let i = 0; i < stops.length; i += 2) {
    if (stops[i].colour !== stops[i + 1].colour) throw new Error(`${what}: a soft stop in ${m[1]}`);
    colours.push(stops[i].colour);
  }
  if (values.length === 1) return { fill: colours[0], rest: colours[1] };
  if (colours[0] !== colours[2]) throw new Error(`${what}: the two ends differ in ${m[1]}`);
  return { fill: colours[1], rest: colours[0] };
}

const radius = (s, what) => {
  for (const k of RADII) if (s[k] !== s.borderTopLeftRadius) throw new Error(`${what} ${k} ${s[k]}`);
  return px(s.borderTopLeftRadius);
};
const FONT = ['fontFamily', 'fontSize', 'fontWeight', 'lineHeight'];
const top = (s, what) => {
  if (s.position !== 'absolute') throw new Error(`${what} position ${s.position}`);
  return px(s.top);
};

/** Each part: its Base Web styled component, its element type, and its declarations as Avalonia's properties. */
const PARTS = [
  {
    // A block of two: the track, then the tick bar.
    part: 'Root', type: 'StackPanel', styled: slider.Root,
    fixed: { Orientation: 'Vertical' },
    convert(s) {
      check(s, 'Root', ['position', 'width']);
      // position: relative is the box the thumbs' labels overflow; nothing an Avalonia panel needs.
      if (s.position !== 'relative' || s.width !== '100%') throw new Error('Root box');
      return { HorizontalAlignment: 'Stretch' };
    },
  },
  {
    part: 'Track', type: 'Border', styled: slider.Track,
    convert(s) {
      check(s, 'Track', ['paddingTop', 'paddingBottom', 'paddingRight', 'paddingLeft', 'display', 'cursor', 'backgroundColor']);
      if (s.display !== 'flex') throw new Error(`Track display ${s.display}`);
      // The InnerTrack is its one flex item, stretched along it (width: 100%) and centred across it (alignSelf).
      return { Padding: uniform(s, 'padding'), Background: colour(s.backgroundColor, 'Track'), Cursor: cursor(s.cursor) };
    },
  },
  {
    part: 'InnerTrack', type: 'b:SliderInnerTrack', styled: slider.InnerTrack,
    convert(s, c) {
      check(s, 'InnerTrack', [...RADII, 'background', 'height', 'width', 'alignSelf', 'cursor']);
      if (s.width !== '100%' || s.alignSelf !== 'center') throw new Error('InnerTrack box');
      const g = gradient(s.background, c.range ? TWO : ONE, 'InnerTrack');
      return {
        HorizontalAlignment: 'Stretch',
        VerticalAlignment: 'Center',
        Height: px(s.height),
        CornerRadius: radius(s, 'InnerTrack'),
        Background: colour(g.rest, 'InnerTrack'),
        Fill: colour(g.fill, 'InnerTrack fill'),
        Cursor: cursor(s.cursor),
      };
    },
  },
  {
    part: 'Mark', type: 'Border', styled: slider.Mark,
    convert(s) {
      check(s, 'Mark', ['width', 'height', 'backgroundColor', 'marginLeft']);
      // react-range places a mark from the root (the InnerTrack isn't positioned), and the margin brings it back by the
      // Track's padding: here it is placed from the InnerTrack, so the margin must be that padding, and is spent.
      const track = declarations(slider.Track, NAMED, {});
      if (s.marginLeft !== track.paddingLeft) throw new Error(`Mark marginLeft ${s.marginLeft} isn't the Track's padding ${track.paddingLeft}`);
      return { Width: px(s.width), Height: px(s.height), Background: colour(s.backgroundColor, 'Mark') };
    },
  },
  {
    part: 'Thumb', type: 'b:SliderThumb', styled: slider.Thumb,
    convert(s, c) {
      check(s, 'Thumb', ['height', 'width', ...RADII, 'display', 'justifyContent', 'alignItems', 'backgroundColor', 'outline', 'boxShadow', 'cursor']);
      // A flex box that centres its children: SliderThumb centres ThumbValue and InnerThumb across it.
      if (s.display !== 'flex' || s.justifyContent !== 'center' || s.alignItems !== 'center' || s.outline !== 'none') throw new Error('Thumb box');
      const ring = /^0 0 0 (\S+) (@\w+)$/.exec(s.boxShadow);
      // A spread-only shadow follows the box's corners outside it: on a round box, an outline at no offset.
      const shadow = ring
        ? { BoxShadow: 'none', OutlineBrush: colour(ring[2], 'Thumb ring'), OutlineThickness: px(ring[1]), OutlineOffset: '0' }
        : { BoxShadow: `{DynamicResource ${SHADOW_KEY}}`, OutlineBrush: '{x:Null}', OutlineThickness: '0', OutlineOffset: '0' };
      if (!ring && s.boxShadow !== restShadow(L)) throw new Error(`Thumb boxShadow ${s.boxShadow}`);
      return {
        Width: px(s.width),
        Height: px(s.height),
        CornerRadius: radius(s, 'Thumb'),
        Background: colour(s.backgroundColor, 'Thumb'),
        ...shadow,
        Cursor: cursor(reactRangeThumbCursor(c)),
      };
    },
  },
  {
    part: 'ThumbValue', type: 'ContentPresenter', styled: slider.ThumbValue,
    convert(s) {
      check(s, 'ThumbValue', ['position', 'top', ...FONT, 'backgroundColor', 'color', 'paddingLeft', 'paddingRight', 'paddingTop',
        'paddingBottom', ...RADII, 'whiteSpace']);
      if (s.whiteSpace !== 'nowrap') throw new Error(`ThumbValue whiteSpace ${s.whiteSpace}`);
      const n = TEXT(false);
      return {
        'Canvas.Top': top(s, 'ThumbValue'),
        Background: colour(s.backgroundColor, 'ThumbValue'),
        Padding: uniform(s, 'padding'),
        // 48px on a box 44 high: drawn as half its height, as CSS draws it.
        CornerRadius: radius(s, 'ThumbValue'),
        [n.Foreground]: colour(s.color, 'ThumbValue'),
        ...font(s, 'ThumbValue', n),
        [n.TextWrapping]: 'NoWrap',
      };
    },
  },
  {
    part: 'InnerThumb', type: 'Border', styled: slider.InnerThumb,
    convert(s) {
      check(s, 'InnerThumb', ['position', 'top', 'width', 'height', 'backgroundColor']);
      return { 'Canvas.Top': top(s, 'InnerThumb'), Width: px(s.width), Height: px(s.height), Background: colour(s.backgroundColor, 'InnerThumb') };
    },
  },
  {
    part: 'TickBar', type: 'Border', styled: slider.TickBar,
    convert(s) {
      check(s, 'TickBar', ['display', 'justifyContent', 'alignItems', 'paddingRight', 'paddingLeft', 'paddingBottom']);
      // Two ticks, the first at the start and the last at the end (Slider.axaml's grid), centred across (Tick's own).
      if (s.display !== 'flex' || s.justifyContent !== 'space-between' || s.alignItems !== 'center') throw new Error('TickBar box');
      return { Padding: uniform(s, 'padding') };
    },
  },
  {
    part: 'Tick', type: 'ContentPresenter', styled: slider.Tick,
    // Centred across the tick bar: its alignItems center, checked above.
    fixed: { VerticalAlignment: 'Center' },
    convert(s) {
      check(s, 'Tick', [...FONT, 'color']);
      const n = TEXT(false);
      return { [n.Foreground]: colour(s.color, 'Tick'), ...font(s, 'Tick', n) };
    },
  },
];

/**
 * What else Base Web passes the parts, which no style function may read but the InnerTrack's gradient: $marks, $step,
 * $thumbIndex, and other values, min and max (which move only the gradient's stops, the InnerTrack's own drawing).
 */
function readsNothingElse() {
  const variants = [{ $marks: true }, { $step: 5 }, { $thumbIndex: 1 }, { $min: -50, $max: 50, $value: [10] }];
  for (const p of PARTS) {
    for (const c of [{}, { disabled: true }, { range: true }, { isDragged: true }, { isFocusVisible: true }]) {
      const at = declarations(p.styled, NAMED, c);
      for (const v of variants) {
        if (c.range && v.$value) continue;
        const other = declarations(p.styled, NAMED, c, v);
        for (const k of new Set([...Object.keys(at), ...Object.keys(other)])) {
          if (at[k] === other[k]) continue;
          if (p.part === 'InnerTrack' && k === 'background' && v.$value) continue;
          throw new Error(`${p.part}.${k} reads ${Object.keys(v).join(', ')}: add it to the states`);
        }
      }
    }
  }
}

const DESCRIBE = {
  Root: 'the slider\'s box: the track, then the tick bar.',
  Track: 'the band that takes a press: padded round the inner track, the cursor by state.',
  InnerTrack: 'the 2 px line: the selection (from the start to the thumb, or between two) and the rest, by state; it places the marks and thumbs.',
  Mark: 'a gap in the line at each step, with marks.',
  Thumb: 'a round handle: its fill by state, its shadow, or on keyboard focus a ring.',
  ThumbValue: 'the value above a hovered or dragged thumb.',
  InnerThumb: 'the stem from the value down into the thumb.',
  TickBar: 'the row of the two ticks under the track.',
  Tick: 'the minimum and the maximum, as labels.',
};

function file(version) {
  readsNothingElse();
  const themes = PARTS.map((p) => {
    const { plain, blocks } = write(p, AXES, declarations, { layered: true });
    return `  <!-- ${p.part}: ${DESCRIBE[p.part]} -->
  <ControlTheme x:Key="Slider.Styled${p.part}" TargetType="${p.type}">
${plain.join('\n')}${blocks.length ? '\n' + blocks.join('\n') : ''}
  </ControlTheme>`;
  });

  return `<!--
  Generated by tools/tokens/gen.js (tools/tokens/slider.js) from baseui ${version}
  (© Uber Technologies, Inc., MIT). Do not edit.
  The themes of Slider's parts, Slider.Styled<Part>, from Base Web's slider: each part's look by the state
  pseudoclasses the Slider sets on it (Metered.Controls.Slider), the colours by token, the lengths read from
  Base Web's own style functions.
-->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:b="https://github.com/wdcossey/Metered">
${themes.join('\n')}
</ResourceDictionary>
`;
}

/** The thumb's resting shadow as a token entry for gen.js's dictionary: Base Web's literal on each theme. */
const shadows = [{ key: SHADOW_KEY, element: (t) => `<BoxShadows x:Key="${SHADOW_KEY}">${css.boxShadows(restShadow(t))}</BoxShadows>` }];

module.exports = { file, shadows, SHADOW_KEY };
