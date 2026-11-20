// The themes of ProgressBar's parts, ProgressBar.Styled<Part>, generated from Base Web's progress bar
// (baseui/progress-bar/styled-components).
//
// As the v2 controls' generators do, and with radio.js's theme writer: each style function is evaluated on Base Web's
// light theme with its colours replaced by their own names ('@contentAccent'), for every combination of the bar's
// states, so that the themes refer to the colours by token and read every length; written as the fewest layered state
// styles and checked against Base Web's for every combination. The motion (the fill's transition, the in-progress
// step's animation, the infinite bars' sweep) is read from the same functions and written as Avalonia's.
//
// The fill's offset in a bar of one step follows the value, which no state carries: the theme gives the empty bar's
// (the value at its minimum), and the ProgressBar sets the value's at Template priority, above a plain setter and
// below a state style or an override's setter (Metered.Controls.ProgressBar).
'use strict';

const css = require('../reference/css');
const { evaluate } = require('../reference/oracle');
const pb = require('baseui/progress-bar/styled-components');
const { SIZE, INTENT } = require('baseui/progress-bar/constants');
const { L, NAMED, theme: write, colour, px, check, font, combinations } = require('./radio');

/**
 * The bar's states, each with its values (the default first) and the pseudoclass that carries it. The ProgressBar
 * mirrors `size`, `intent`, `stepped` (more than one step) and `infinite` onto every part; `firstChild` is Avalonia's
 * own `:nth-child(1)` (Base Web's `:first-child`), `stepState` a step's fill's and `isLeft` an infinite bar's, set on
 * each element. No intent is Base Web's `intent` left out, which draws the old default colour.
 */
const SHARED = [
  { name: 'size', values: [SIZE.medium, SIZE.small, SIZE.large], pseudo: (v) => `:size-${v}` },
  { name: 'intent', values: [undefined, ...Object.values(INTENT)], pseudo: (v) => `:intent-${v}` },
  { name: 'stepped', values: [false, true], pseudo: () => ':stepped' },
  { name: 'infinite', values: [false, true], pseudo: () => ':infinite' },
];
const FIRST = { name: 'firstChild', values: [false, true], pseudo: () => ':nth-child(1)' };
const KEBAB = { awaits: 'awaits', inProgress: 'in-progress', completed: 'completed' };
const STEP = { name: 'stepState', values: ['awaits', 'inProgress', 'completed'], pseudo: (v) => `:step-${KEBAB[v]}` };
const LEFT = { name: 'isLeft', values: [false, true], pseudo: () => ':is-left' };

/**
 * progressbar.js's sharedProps for a combination: 3 steps when stepped. For a step's fill, a value and an index that
 * give its state (2 steps: the first completed and the second in progress at 50; the second awaiting at 0); otherwise
 * the value at the minimum, the empty bar.
 */
function props(c) {
  const shared = {
    $infinite: !!c.infinite, $intent: c.intent, $size: c.size, $steps: c.stepped ? 3 : 1,
    $successValue: 100, $minValue: 0, $maxValue: 100, $value: 0,
  };
  if (!c.stepped) return { ...shared, $index: 0 };
  const at = { completed: [0, 50], inProgress: [1, 50], awaits: [1, 0] }[c.stepState ?? 'awaits'];
  return { ...shared, $steps: 2, $index: at[0], $value: at[1] };
}

/** InfiniteBar's own props (progressbar.js gives it only these). */
const infiniteProps = (c) => ({ $isLeft: !!c.isLeft, $size: c.size, $intent: c.intent });

const MOTION = ['animationDuration', 'animationIterationCount', 'animationTimingFunction', 'animationName'];

/** The declarations a browser applies to the part in that combination, its keyframes left out (motion() reads them). */
function declarations(styled, t, c) {
  const style = evaluate(styled, t, styled === pb.StyledInfiniteBar ? infiniteProps(c) : props(c));
  const self = {};
  for (const [k, v] of Object.entries(style)) {
    if (k === 'animationName') continue;
    if (v !== null && typeof v === 'object') {
      if (k !== ':first-child') throw new Error(`${styled.displayName}: a block this generator doesn't read: ${k}`);
    } else if (v !== undefined) self[k] = v;
  }
  if (c.firstChild) Object.assign(self, style[':first-child'] || {});
  return self;
}

const RADII = ['borderTopLeftRadius', 'borderTopRightRadius', 'borderBottomRightRadius', 'borderBottomLeftRadius'];
/** CSS's four radii, each 0 when unset, as Avalonia's CornerRadius (top left, top right, bottom right, bottom left). */
const corners = (s) => {
  const r = RADII.map((k) => (s[k] === undefined ? 0 : css.px(s[k])));
  return new Set(r).size === 1 ? String(r[0]) : r.join(',');
};

/** 'translateX(-102%)' to -102: CSS's translateX as a percentage of the element's own width. */
function percent(v, what) {
  const m = /^translateX\((-?\d+(?:\.\d+)?)%\)$/.exec(v);
  if (!m) throw new Error(`${what} transform ${v}`);
  return String(Number(m[1]));
}

/** Base Web's infinite gradient: transparent, the colour from `start` to `end`, transparent; symmetric, so its angle doesn't matter. */
function gradient(image, what) {
  const m = /^linear-gradient\((90|270)deg, transparent 0%, (@\w+) ([\d.]+)%, (@\w+) ([\d.]+)%, transparent 100%\)$/.exec(image);
  if (!m || m[2] !== m[4]) throw new Error(`${what} backgroundImage ${image}`);
  if (Number(m[3]) + Number(m[5]) !== 100) throw new Error(`${what}: an asymmetric gradient, whose angle would matter`);
  return { colour: m[2], start: m[3], end: m[5] };
}

const PARTS = [
  {
    part: 'Root', type: 'StackPanel', styled: pb.StyledRoot, axes: SHARED,
    convert(s) {
      check(s, 'Root', ['width', 'display', 'flexDirection', 'gap']);
      // width 100%: the ProgressBar stretches across its parent, as a block does (its own HorizontalAlignment).
      if (s.width !== '100%' || s.display !== 'flex' || s.flexDirection !== 'column') throw new Error('Root box');
      return { Orientation: 'Vertical', Spacing: px(s.gap) };
    },
  },
  {
    part: 'Bar', type: 'Border', styled: pb.StyledBar, axes: [...SHARED, FIRST],
    // overflow: hidden, which clips the fill to the bar's rounded corners.
    fixed: { ClipToBounds: 'True' },
    convert(s) {
      check(s, 'Bar', [...RADII, 'backgroundColor', 'height', 'flex', 'overflow', 'marginLeft']);
      // flex: 1 is the container's: an equal share of the row after the margins (ProgressBarContainer).
      if (s.flex !== 1 || s.overflow !== 'hidden') throw new Error('Bar box');
      return {
        CornerRadius: corners(s),
        Background: colour(s.backgroundColor, 'Bar'),
        Height: px(s.height),
        Margin: s.marginLeft === undefined ? '0' : `${px(s.marginLeft)},0,0,0`,
      };
    },
  },
  {
    part: 'BarProgress', type: 'b:ProgressBarFill', styled: pb.StyledBarProgress, axes: [...SHARED, STEP],
    convert(s) {
      check(s, 'BarProgress', [...RADII, 'backgroundColor', 'height', 'width', 'transform', 'transition', ...MOTION]);
      // The bar's whole box, moved along by its transform.
      if (s.height !== '100%' || s.width !== '100%') throw new Error('BarProgress box');
      return {
        CornerRadius: corners(s),
        Background: colour(s.backgroundColor, 'BarProgress'),
        Offset: percent(s.transform, 'BarProgress'),
      };
    },
  },
  {
    part: 'InfiniteBar', type: 'b:ProgressBarInfiniteBar', styled: pb.StyledInfiniteBar, axes: [...SHARED, LEFT],
    convert(s) {
      check(s, 'InfiniteBar', [...RADII, 'height', 'display', 'flex', 'marginLeft', 'marginRight', 'transitionProperty',
        'backgroundSize', 'backgroundRepeat', 'backgroundPositionX', 'backgroundImage', ...MOTION]);
      // flex: 1, the container's (its auto margins take nothing beside a flex item that grows); a transition of the
      // background's position with no duration, which moves nothing; the image once, outside it nothing.
      if (s.display !== 'inline-block' || s.flex !== 1 || s.marginLeft !== 'auto' || s.marginRight !== 'auto') throw new Error('InfiniteBar box');
      if (s.transitionProperty !== 'background-position' || s.backgroundRepeat !== 'no-repeat') throw new Error('InfiniteBar background');
      const size = /^([\d.]+)% auto$/.exec(s.backgroundSize);
      const position = /^(-?[\d.]+)%$/.exec(s.backgroundPositionX);
      if (!size || !position) throw new Error(`InfiniteBar ${s.backgroundSize} ${s.backgroundPositionX}`);
      const g = gradient(s.backgroundImage, 'InfiniteBar');
      return {
        CornerRadius: corners(s),
        Height: px(s.height),
        ImageSize: size[1],
        Position: position[1],
        Fill: colour(g.colour, 'InfiniteBar'),
        FillStart: g.start,
        FillEnd: g.end,
      };
    },
  },
  {
    part: 'Label', type: 'TextBlock', styled: pb.StyledLabel, axes: SHARED,
    convert(s) {
      check(s, 'Label', ['textAlign', 'paddingLeft', 'paddingRight', 'color', 'fontFamily', 'fontSize', 'fontWeight', 'lineHeight']);
      if (s.textAlign !== 'center') throw new Error(`Label textAlign ${s.textAlign}`);
      return {
        TextAlignment: 'Center',
        Padding: `${px(s.paddingLeft)},0,${px(s.paddingRight)},0`,
        Foreground: colour(s.color, 'Label'),
        ...font(s, 'Label', { FontFamily: 'FontFamily', FontSize: 'FontSize', FontWeight: 'FontWeight', LineHeight: 'LineHeight' }),
      };
    },
  },
];

/**
 * The endless animations run only while the element isn't `:metered-still`, which the preview gallery sets on every
 * element of a page before each picture (tests/Metered.Tests/Sample/PreviewGallery.cs): still, the element stands at
 * its plain values, which are its animation's first keyframe (checked below), so the picture is the 0 % keyframe. An
 * app never sets it.
 */
const STILL = ':not(:metered-still)';

const span = (seconds) => new Date(seconds * 1000).toISOString().substr(11, 12);

/** CSS's animation on the part, as an Avalonia Animation of `prop` (each keyframe's value by `value`). */
function animation(s, what, prop, value, indent) {
  if (s.animationIterationCount !== 'infinite') throw new Error(`${what} animationIterationCount ${s.animationIterationCount}`);
  const easing = css.cubicBezier(s.animationTimingFunction).join(',');
  const frames = Object.entries(s.animationName).map(([cue, v]) => {
    const keys = Object.keys(v).sort().join(',');
    if (keys !== prop.keys) throw new Error(`${what} keyframe ${cue}: ${keys}`);
    return `${indent}    <KeyFrame Cue="${cue}">\n${value(v).map(([p, x]) => `${indent}      <Setter Property="${p}" Value="${x}" />`).join('\n')}\n${indent}    </KeyFrame>`;
  });
  return `${indent}<Animation Duration="${span(css.seconds(s.animationDuration))}" IterationCount="Infinite" Easing="${easing}">
${frames.join('\n')}
${indent}</Animation>`;
}

/** The fill's motion: the transform's transition, once loaded; and the step in progress, Base Web's slide and fade. */
function fillMotion() {
  const s = evaluate(pb.StyledBarProgress, L, props({ size: SIZE.medium }));
  const t = /^transform ([\d.]+m?s)$/.exec(s.transition);
  if (!t) throw new Error(`BarProgress transition ${s.transition}`);
  // No timing function: CSS's initial value, ease, cubic-bezier(0.25, 0.1, 0.25, 1) (CSS Easing Functions Level 1, §2.2).
  const transition = `    <Style Selector="^:loaded">
      <Setter Property="Transitions">
        <Transitions>
          <DoubleTransition Property="Offset" Duration="${span(css.seconds(t[1]))}" Easing="0.25,0.1,0.25,1" />
        </Transitions>
      </Setter>
    </Style>`;

  // Only a step in progress has an animation (checked for every combination).
  for (const c of combinations([...SHARED, STEP])) {
    const has = evaluate(pb.StyledBarProgress, NAMED, props(c)).animationName !== undefined;
    if (has !== (c.stepped && c.stepState === 'inProgress')) throw new Error(`BarProgress animation in ${JSON.stringify(c)}`);
  }
  const step = evaluate(pb.StyledBarProgress, L, props({ size: SIZE.medium, stepped: true, stepState: 'inProgress' }));
  const first = step.animationName['0%'];
  if (percent(first.transform, 'BarProgress') !== percent(step.transform, 'BarProgress') || first.opacity !== 1) throw new Error('BarProgress: a first keyframe that is not its plain values');
  const anim = animation(step, 'BarProgress', { keys: 'opacity,transform' },
    (v) => [['Offset', percent(v.transform, 'BarProgress keyframe')], ['Opacity', String(v.opacity)]], '        ');
  return `${transition}
    <Style Selector="^:stepped:step-in-progress${STILL}">
      <Style.Animations>
${anim}
      </Style.Animations>
    </Style>`;
}

/** The infinite bars' sweep: the background's position, left and right each its own way. */
function sweep() {
  for (const c of combinations([...SHARED, LEFT])) {
    const a = evaluate(pb.StyledInfiniteBar, NAMED, infiniteProps(c));
    const b = evaluate(pb.StyledInfiniteBar, L, infiniteProps({ size: SIZE.medium, isLeft: c.isLeft }));
    for (const k of MOTION) if (JSON.stringify(a[k]) !== JSON.stringify(b[k])) throw new Error(`InfiniteBar ${k} varies by more than isLeft`);
  }
  const one = (isLeft) => {
    const s = evaluate(pb.StyledInfiniteBar, L, infiniteProps({ size: SIZE.medium, isLeft }));
    if (s.animationName['0%'].backgroundPositionX !== s.backgroundPositionX) throw new Error('InfiniteBar: a first keyframe that is not its plain position');
    const anim = animation(s, 'InfiniteBar', { keys: 'backgroundPositionX' },
      (v) => [['Position', String(Number(/^(-?[\d.]+)%$/.exec(v.backgroundPositionX)[1]))]], '        ');
    return `    <Style Selector="${isLeft ? `^:is-left${STILL}` : `^:not(:is-left)${STILL}`}">
      <Style.Animations>
${anim}
      </Style.Animations>
    </Style>`;
  };
  return `${one(true)}\n${one(false)}`;
}

/** Base Web passes $infinite to every part, but no style function reads it: the structure changes, not a style. */
function infiniteChangesNothing() {
  for (const p of PARTS) {
    for (const c of combinations(p.axes)) {
      if (JSON.stringify(declarations(p.styled, NAMED, { ...c, infinite: false })) !== JSON.stringify(declarations(p.styled, NAMED, { ...c, infinite: true }))) {
        throw new Error(`${p.part} reads $infinite in ${JSON.stringify(c)}`);
      }
    }
  }
}

const DESCRIBE = {
  Root: 'the bar and its label, in a column.',
  Bar: 'a step\'s track, its fill clipped to its rounded corners; after the first, a margin between steps.',
  BarProgress: 'the fill: the intent\'s colour, moved along its bar by the value (the ProgressBar\'s) or by its step\'s state; the step in progress slides in and fades.',
  InfiniteBar: 'a half of the infinite bar: a gradient three times its width, swept across it, the outer corners rounded.',
  Label: 'the progress label, centred under the bar.',
};

function file(version) {
  infiniteChangesNothing();
  const themes = PARTS.map((p) => {
    const { plain, blocks } = write(p, p.axes, declarations, { layered: true });
    const extra = p.part === 'BarProgress' ? '\n' + fillMotion() : p.part === 'InfiniteBar' ? '\n' + sweep() : '';
    return `  <!-- ${p.part}: ${DESCRIBE[p.part]} -->
  <ControlTheme x:Key="ProgressBar.Styled${p.part}" TargetType="${p.type}">
${[plain.join('\n') + extra, ...blocks].join('\n')}
  </ControlTheme>`;
  });

  // BarContainer: a flex row (ProgressBarContainer lays it out), relative for its children; nothing set on the element.
  const container = evaluate(pb.StyledBarContainer, L, props({}));
  check(container, 'BarContainer', ['display', 'position']);
  if (container.display !== 'flex' || container.position !== 'relative') throw new Error('BarContainer box');

  return `<!--
  Generated by tools/tokens/gen.js (tools/tokens/progress-bar.js) from baseui ${version}
  (© Uber Technologies, Inc., MIT). Do not edit.
  The themes of ProgressBar's parts, ProgressBar.Styled<Part>, from Base Web's progress bar: each part's
  look by the state pseudoclasses the ProgressBar mirrors onto it (Metered.Controls.ProgressBar), the
  colours by token, the lengths and the motion read from Base Web's own style functions.
-->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:b="https://github.com/wdcossey/Metered">
${themes[0]}
  <!-- BarContainer: the bars in a row, an equal share each after their margins (ProgressBarContainer). -->
  <ControlTheme x:Key="ProgressBar.StyledBarContainer" TargetType="b:ProgressBarContainer" />
${themes.slice(1).join('\n')}
</ResourceDictionary>
`;
}

module.exports = { file };
