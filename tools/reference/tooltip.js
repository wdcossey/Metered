// The oracle for Tooltip: what Base Web's own tooltip gives (baseui/tooltip), for the cases the tests build, on the real
// light and dark themes, in Avalonia's units. Its three parts' style functions (tooltip/styled-components.js's Body,
// Arrow and Inner: the popover's getBodyStyles, getArrowStyles and getInnerStyles with the tooltip's values over them)
// evaluated with the props popover.js gives them, and its default props as StatefulTooltip and Tooltip resolve them
// (tooltip/default-props.js over StatefulPopover's container's and Popover's own). Where popper.js puts it is the
// Popover's (popover.oracle.json's tether cases). Kept apart from tools/tokens/tooltip.js, which writes the themes: the
// tests compare one with the other. Read by cases.js, which writes tests/Metered.Tests/Reference/tooltip.oracle.json.
'use strict';

const sc = require('baseui/tooltip/styled-components');
const C = require('baseui/popover/constants');
const tooltipDefaults = require('baseui/tooltip/default-props').default;
const TooltipContainer = require('baseui/tooltip/stateful-tooltip-container').default;
const Tooltip = require('baseui/tooltip/tooltip').default;
const StatefulTooltip = require('baseui/tooltip/stateful-tooltip').default;
const PopoverContainer = require('baseui/popover/stateful-container').default;
const popoverDefaults = require('baseui/popover/default-props').default;
const { getPopoverMarginStyles } = require('baseui/popover/utils');
const css = require('./css');
const { THEMES, evaluate } = require('./oracle');
const { PLACEMENTS, shared } = require('./popover');

const radius = (s) => {
  const all = new Set(['borderTopLeftRadius', 'borderTopRightRadius', 'borderBottomRightRadius', 'borderBottomLeftRadius'].map((k) => s[k]));
  if (all.size !== 1) throw new Error('radii differ');
  return css.px([...all][0]);
};

/** translate3d(xpx, ypx, 0) as [x, y]. */
function translate(v) {
  const m = /^translate3d\((-?[\d.]+)px, (-?[\d.]+)px, 0\)$/.exec(v);
  if (!m) throw new Error(`transform ${v}`);
  return [Number(m[1]), Number(m[2])];
}

const sides = (s, prefix) => ['Left', 'Top', 'Right', 'Bottom'].map((side) => (s[`${prefix}${side}`] === undefined ? 0 : css.px(s[`${prefix}${side}`])));

/** Each part's colours, radius, shadow, padding and font in each theme. */
function colours() {
  return Object.entries(THEMES).map(([theme, t]) => {
    const body = evaluate(sc.Body, t, shared({ $isOpen: true, $isAnimating: true }));
    const arrow = evaluate(sc.Arrow, t, shared());
    const inner = evaluate(sc.Inner, t, shared());
    return {
      theme,
      body: { background: css.color(body.backgroundColor), radius: radius(body), boxShadow: css.boxShadows(body.boxShadow), foreground: css.color(body.color) },
      arrow: { background: css.color(arrow.backgroundColor), boxShadow: css.boxShadows(arrow.boxShadow) },
      inner: {
        background: css.color(inner.backgroundColor), radius: radius(inner), foreground: css.color(inner.color), padding: sides(inner, 'padding'),
        font: { fontSize: css.px(inner.fontSize), fontWeight: css.fontWeight(inner.fontWeight), lineHeight: css.px(inner.lineHeight), family: css.fontFamily(inner.fontFamily) },
      },
    };
  });
}

/**
 * The body's margin and its transform closed and open, by placement, arrow and popoverMargin: the margin is
 * getPopoverMarginStyles' (the popover's), the transform getEndPosition at an offset of 0 in both (the tooltip's own:
 * no start position).
 */
function body() {
  const cases = [];
  for (const placement of PLACEMENTS) {
    for (const showArrow of [false, true]) {
      for (const popoverMargin of [tooltipDefaults.popoverMargin, C.POPOVER_MARGIN, 20]) {
        const closed = evaluate(sc.Body, THEMES.Light, shared({ $placement: placement, $showArrow: showArrow, $popoverMargin: popoverMargin }));
        const open = evaluate(sc.Body, THEMES.Light, shared({ $placement: placement, $showArrow: showArrow, $popoverMargin: popoverMargin, $isOpen: true, $isAnimating: true }));
        const m = getPopoverMarginStyles(showArrow ? C.ARROW_SIZE : 0, placement, popoverMargin);
        if (JSON.stringify(sides(m, 'margin')) !== JSON.stringify(sides(closed, 'margin'))) throw new Error('margins');
        cases.push({ placement, showArrow, popoverMargin, margin: sides(closed, 'margin'), start: translate(closed.transform), end: translate(open.transform) });
      }
    }
  }
  return cases;
}

/** The body's motion by state: its opacity, and the transition's properties, duration and curve (Body's transition*). */
function motion() {
  const cases = [];
  for (const isOpen of [false, true]) {
    for (const isAnimating of [false, true]) {
      for (const animateOutTime of [0, 300]) {
        for (const isHoverTrigger of [false, true]) {
          // popover.js: $animationDuration is animateOutTime || ANIMATE_OUT_TIME.
          const s = evaluate(sc.Body, THEMES.Light, shared({ $isOpen: isOpen, $isAnimating: isAnimating, $animationDuration: animateOutTime || C.ANIMATE_OUT_TIME, $isHoverTrigger: isHoverTrigger }));
          const hover = s.animationName ? {
            seconds: css.seconds(s.animationDuration),
            frames: Object.entries(s.animationName).map(([at, f]) => [parseFloat(at) / 100, f.pointerEvents !== 'none']),
          } : null;
          cases.push({
            isOpen, isAnimating, animateOutTime, isHoverTrigger, opacity: s.opacity, properties: s.transitionProperty.split(','),
            seconds: css.seconds(s.transitionDuration), easing: css.cubicBezier(s.transitionTimingFunction), hitTest: hover,
          });
        }
      }
    }
  }
  return cases;
}

/**
 * The props a tooltip has when none is given, as React resolves them: StatefulTooltip's defaultProps (the tooltip's),
 * then StatefulPopover's container's for what they leave undefined, then Popover's (PopoverInner's) for the rest; a
 * Tooltip's own are the tooltip's, over Popover's.
 */
function defaults() {
  if (StatefulTooltip.defaultProps !== TooltipContainer.defaultProps) throw new Error('StatefulTooltip defaults');
  const keys = ['accessibilityType', 'focusLock', 'autoFocus', 'returnFocus', 'ignoreBoundary', 'onMouseEnterDelay', 'onMouseLeaveDelay',
    'placement', 'showArrow', 'triggerType', 'dismissOnClickOutside', 'dismissOnEsc', 'popoverMargin', 'animateOutTime'];
  const pick = (o) => Object.fromEntries(keys.map((k) => [k, o[k] === undefined ? null : o[k]]));
  const stateful = { ...popoverDefaults, ...PopoverContainer.defaultProps, ...TooltipContainer.defaultProps };
  const controlled = { ...popoverDefaults, ...Tooltip.defaultProps };
  // tooltip.js renders the Popover with autoFocus false under the props: the default stands.
  if (stateful.autoFocus !== false || controlled.autoFocus !== false) throw new Error('autoFocus');
  return { stateful: pick(stateful), tooltip: pick(controlled) };
}

/**
 * The radius on a theme given its own radius300 (createTheme's overrides, deep-merged): Body's and Inner's follow it, as
 * their style functions read it, where a popover's read popoverBorderRadius, the same 8 px in Base Web's own themes.
 */
function themed() {
  const radius300 = '3px';
  const t = { ...THEMES.Light, borders: { ...THEMES.Light.borders, radius300 } };
  const body = evaluate(sc.Body, t, shared({ $isOpen: true, $isAnimating: true }));
  const inner = evaluate(sc.Inner, t, shared());
  if (css.px(THEMES.Light.borders.popoverBorderRadius) === css.px(radius300)) throw new Error('radius300 as popoverBorderRadius');
  return { radius300: css.px(radius300), body: radius(body), inner: radius(inner), popoverBorderRadius: css.px(t.borders.popoverBorderRadius) };
}

function oracle(version) {
  return {
    baseui: version,
    constants: { arrowSize: C.ARROW_SIZE, arrowWidth: C.ARROW_WIDTH, animateInTime: C.ANIMATE_IN_TIME },
    defaults: defaults(),
    colours: colours(),
    themed: themed(),
    body: body(),
    motion: motion(),
  };
}

module.exports = { oracle };
