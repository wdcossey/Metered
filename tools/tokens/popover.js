// The themes of Popover's parts, Popover.Styled<Part>, generated from Base Web's popover
// (baseui/popover/styled-components) with the theme writer the other controls share (radio.js): each style function
// evaluated on a theme whose colours, radii and shadows are their own names, for every combination of the states
// popover.js gives the parts (its sharedProps), written as the fewest state styles and checked against Base Web's own.
// The tests' oracle (tools/reference/popover.js) checks the result against the real colours.
//
// What the theme holds and what the Popover gives (Metered.Controls.Popover):
// - Body: its fill, radius and shadow, its opacity by state, its entrance's transition (`:is-open:is-animating`, 0.1 s on
//   easeOutCurve) and a hover popover's 0.1 s of not taking the pointer. Its margin and its transform's start depend on
//   popoverMargin, a number (getPopoverMarginStyles, getStartPosition): the Popover gives them, below an override, and
//   its exit's transition over animateOutTime. popper.js's offset is the popup's position, not a transform.
// - Arrow: its fill, shadow, 9 × 9 box turned 45°, and the side it sits 4 pixels beyond by placement
//   (getArrowPositionStyles), as CSS's absolute position in PopoverBodyPanel; its offset along that side is popper.js's,
//   which the Popover gives as its margin.
// - Inner: its fill, radius and text colour.
// - Padding: Base Web's StyledPadding, a drop-in 12 px for content, as a Border's theme.
'use strict';

const sc = require('baseui/popover/styled-components');
const C = require('baseui/popover/constants');
const css = require('../reference/css');
const { evaluate } = require('../reference/oracle');
const { L, NAMED, theme: write, colour, check, timing } = require('./radio');

/** The theme the parts are evaluated on (in place of the writer's, whose radii and shadows are values): colours, radii and shadows by their names, so they're written as tokens. */
const named = (group) => new Proxy(L[group], { get: (o, name) => (typeof name === 'string' && typeof o[name] === 'string' ? `@${group}.${name}` : o[name]) });
const T = { ...NAMED, borders: named('borders'), lighting: named('lighting') };
const r = (key) => `{DynamicResource ${key}}`;
/** A theme value by its token: '@borders.popoverBorderRadius'. */
function token(v, what) {
  if (typeof v === 'string' && /^@(borders|lighting)\./.test(v)) return r(v.slice(1));
  throw new Error(`${what}: not a token: ${JSON.stringify(v)}`);
}

const PLACEMENTS = Object.values(C.PLACEMENT).filter((p) => p !== 'auto');
const kebab = (s) => s.replace(/[A-Z]/g, (c) => `-${c.toLowerCase()}`);

/**
 * The states popover.js gives every part (sharedProps), each with its values (the default first) and the pseudoclass the
 * Popover mirrors it as. $placement is the one popper.js chose; Base Web's 'auto' is drawn only before popper.js runs,
 * while the body is still hidden, so it isn't a state here.
 */
const AXES = [
  { name: 'isOpen', values: [false, true], pseudo: () => ':is-open' },
  { name: 'isAnimating', values: [false, true], pseudo: () => ':is-animating' },
  { name: 'showArrow', values: [false, true], pseudo: () => ':show-arrow' },
  { name: 'isHoverTrigger', values: [false, true], pseudo: () => ':is-hover-trigger' },
  { name: 'placement', values: ['top', ...PLACEMENTS.filter((p) => p !== 'top')], pseudo: (v) => `:placement-${kebab(v)}` },
];

/** popover.js's sharedProps for a combination, at its defaults otherwise (an offset of 0, animateOutTime 0). */
const props = (c) => ({
  $showArrow: c.showArrow, $arrowOffset: { left: 0, top: 0 }, $popoverOffset: { left: 0, top: 0 }, $placement: c.placement,
  $isAnimating: c.isAnimating, $animationDuration: C.ANIMATE_OUT_TIME, $isOpen: c.isOpen, $popoverMargin: C.POPOVER_MARGIN,
  $isHoverTrigger: c.isHoverTrigger,
});

/** The flat declarations; a hover popover's animationName, a block, is read by hitTest(). */
function flat(styled, t, c) {
  const s = evaluate(styled, t, props(c));
  const out = {};
  for (const [k, v] of Object.entries(s)) {
    if (v !== null && typeof v === 'object') {
      if (k !== 'animationName') throw new Error(`${styled.displayName}: a block this generator doesn't read: ${k}`);
    } else out[k] = v;
  }
  return out;
}

const RADII = ['borderTopLeftRadius', 'borderTopRightRadius', 'borderBottomRightRadius', 'borderBottomLeftRadius'];
function radius(s, what) {
  const all = new Set(RADII.map((k) => s[k]));
  if (all.size !== 1) throw new Error(`${what} radii differ`);
  return token([...all][0], what);
}
const MARGINS = ['marginTop', 'marginRight', 'marginBottom', 'marginLeft'];
const SIDES = ['left', 'top', 'right', 'bottom'];

function parts() {
  return [
    {
      part: 'Body', type: 'Border',
      styled: (t, c) => flat(sc.Body, T, c),
      convert(s) {
        const what = 'Body';
        check(s, what, ['position', 'top', 'left', 'backgroundColor', ...RADII, 'boxShadow', 'transitionProperty', 'transitionDuration',
          'transitionTimingFunction', 'opacity', 'transform', ...MARGINS, 'animationDuration']);
        if (s.position !== 'absolute' || s.top !== 0 || s.left !== 0 || s.transitionProperty !== 'opacity,transform') throw new Error(`${what} box`);
        return { Background: colour(s.backgroundColor, what), CornerRadius: radius(s, what), BoxShadow: token(s.boxShadow, what), Opacity: String(s.opacity) };
      },
    },
    {
      part: 'Arrow', type: 'Border',
      styled: (t, c) => flat(sc.Arrow, T, c),
      convert(s, c) {
        const what = 'Arrow';
        check(s, what, ['backgroundColor', 'boxShadow', 'width', 'height', 'transform', 'position', ...SIDES]);
        if (s.position !== 'absolute' || s.transform !== 'rotate(45deg)') throw new Error(`${what} box`);
        // The side along the placement's side is popper.js's offset (0 here), which the Popover gives as the margin; the
        // other is the side it sits beyond.
        const along = ['top', 'bottom'].includes(c.placement.replace(/[A-Z].*/, '')) ? 'left' : 'top';
        if (s[along] !== '0px') throw new Error(`${what} ${along} ${s[along]}`);
        const at = Object.fromEntries(SIDES.map((k) => [`Canvas.${k[0].toUpperCase()}${k.slice(1)}`, k === along || s[k] === undefined ? 'NaN' : String(css.px(s[k]))]));
        return {
          Background: colour(s.backgroundColor, what), BoxShadow: token(s.boxShadow, what),
          Width: String(css.px(s.width)), Height: String(css.px(s.height)), RenderTransform: 'rotate(45deg)', ...at,
        };
      },
    },
    {
      part: 'Inner', type: 'ContentPresenter',
      styled: (t, c) => flat(sc.Inner, T, c),
      convert(s) {
        const what = 'Inner';
        check(s, what, ['backgroundColor', ...RADII, 'color', 'position']);
        if (s.position !== 'relative') throw new Error(`${what} box`);
        return { Background: colour(s.backgroundColor, what), CornerRadius: radius(s, what), Foreground: colour(s.color, what) };
      },
    },
  ];
}

const DEFAULT = Object.fromEntries(AXES.map((a) => [a.name, a.values[0]]));
const at = (x) => ({ ...DEFAULT, ...x });

/**
 * The body's entrance: while open and animating, opacity and transform over 0.1 s on easeOutCurve. Every other state's
 * duration is 0 at Base Web's default animateOutTime (0): no transition; the Popover gives the exit's for another.
 */
function entrance() {
  const on = flat(sc.Body, L, at({ isOpen: true, isAnimating: true }));
  for (const c of [at({}), at({ isOpen: true }), at({ isAnimating: true })]) {
    if (css.seconds(flat(sc.Body, L, c).transitionDuration) !== 0) throw new Error(`Body transition in ${JSON.stringify(c)}`);
  }
  const { span, easing } = timing(on);
  return `    <Style Selector="^:is-open:is-animating">
      <Setter Property="Transitions">
        <Transitions>
          <DoubleTransition Property="Opacity" Duration="${span}" Easing="${easing}" />
          <TransformOperationsTransition Property="RenderTransform" Duration="${span}" Easing="${easing}" />
        </Transitions>
      </Setter>
    </Style>`;
}

/**
 * A hover popover's body takes no pointer for its first 0.1 s (Body's animationName: pointer-events none to 99%, auto at
 * 100%), as IsHitTestVisible. CSS runs it when the body is made, each time it opens: here when it opens.
 */
function hitTest() {
  const s = evaluate(sc.Body, L, props(at({ isHoverTrigger: true, isOpen: true })));
  const frames = Object.entries(s.animationName).map(([cue, f]) => [cue, f.pointerEvents !== 'none' ? 'True' : 'False']);
  if (evaluate(sc.Body, L, props(at({}))).animationName !== undefined) throw new Error('Body animation without hover');
  const span = new Date(css.seconds(s.animationDuration) * 1000).toISOString().substr(11, 12);
  return `    <Style Selector="^:is-hover-trigger:is-open">
      <Style.Animations>
        <Animation Duration="${span}" FillMode="None">
${frames.map(([cue, v]) => `          <KeyFrame Cue="${cue}">
            <Setter Property="IsHitTestVisible" Value="${v}" />
          </KeyFrame>`).join('\n')}
        </Animation>
      </Style.Animations>
    </Style>`;
}

/** Base Web's StyledPadding: a style object of 12 px each side. */
function padding() {
  const s = sc.Padding.__STYLETRON__;
  if (s.reducers.length !== 0) throw new Error('Padding is a function');
  const p = s.getInitialStyle();
  check(p, 'Padding', ['paddingLeft', 'paddingTop', 'paddingRight', 'paddingBottom']);
  const value = ['Left', 'Top', 'Right', 'Bottom'].map((x) => css.px(p[`padding${x}`])).join(',');
  return `  <!-- Padding: Base Web's StyledPadding, the padding it recommends round a popover's content. -->
  <ControlTheme x:Key="Popover.StyledPadding" TargetType="Border">
    <Setter Property="Padding" Value="${value}" />
  </ControlTheme>`;
}

const DESCRIBE = {
  Body: 'the box placed against the anchor: its fill, radius and shadow; its opacity and entrance by state.',
  Arrow: 'the arrow, a box turned 45° behind Inner: its fill, shadow, size, and the side it sits beyond by placement.',
  Inner: 'the content\'s box over the arrow: its fill, radius and text colour.',
};

function file(version) {
  const themes = parts().map((p) => {
    const { plain, blocks } = write(p, AXES, (styled, t, c) => styled(t, c), { layered: true });
    const extra = p.part === 'Body' ? [entrance(), hitTest()] : [];
    const body = [...plain, ...extra, ...blocks];
    return `  <!-- ${p.part}: ${DESCRIBE[p.part]} -->
  <ControlTheme x:Key="Popover.Styled${p.part}" TargetType="${p.type}">\n${body.join('\n')}\n  </ControlTheme>`;
  });
  return `<!--
  Generated by tools/tokens/gen.js (tools/tokens/popover.js) from baseui ${version}
  (© Uber Technologies, Inc., MIT). Do not edit.
  The themes of Popover's parts, Popover.Styled<Part>, from Base Web's popover: each part's look by the state
  pseudoclasses the Popover mirrors onto it (Metered.Controls.Popover), the colours, radii and shadows by token,
  the lengths read from Base Web's own style functions.
-->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:b="https://github.com/wdcossey/Metered">
${themes.join('\n')}
${padding()}
</ResourceDictionary>
`;
}

module.exports = { file, parts, AXES };
