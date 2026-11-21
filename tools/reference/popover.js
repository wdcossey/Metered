// The oracle for Popover: what Base Web's own popover gives (baseui/popover), for the cases the tests build, on the real
// light and dark themes, in Avalonia's units. Its three parts' style functions (styled-components.js's Body, Arrow and
// Inner) evaluated with the props popover.js gives them, the helpers they call (utils.js), its constants and its default
// props; and where popper.js puts the body and the arrow (tether.g.json, which tools/reference/tether.js measures by
// running popper.js in headless Chromium). Kept apart from tools/tokens/popover.js, which writes the themes: the tests
// compare one with the other. Read by cases.js, which writes tests/Metered.Tests/Reference/popover.oracle.json.
'use strict';

const fs = require('fs');
const path = require('path');
const sc = require('baseui/popover/styled-components');
const C = require('baseui/popover/constants');
const defaults = require('baseui/popover/default-props').default;
const Stateful = require('baseui/popover/stateful-container').default;
const { getPopoverMarginStyles, getStartPosition, getEndPosition } = require('baseui/popover/utils');
const css = require('./css');
const { THEMES, evaluate } = require('./oracle');

/** The placements popper.js can choose: Base Web's PLACEMENT less auto, which Body is drawn with only before it runs. */
const PLACEMENTS = Object.values(C.PLACEMENT).filter((p) => p !== 'auto');

/** popover.js's sharedProps for these. */
const shared = (x = {}) => ({
  $showArrow: false, $arrowOffset: { left: 0, top: 0 }, $popoverOffset: { left: 0, top: 0 }, $placement: 'top',
  $isAnimating: false, $animationDuration: C.ANIMATE_OUT_TIME, $isOpen: false, $popoverMargin: C.POPOVER_MARGIN, $isHoverTrigger: false, ...x,
});

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

const margins = (s) => ['Left', 'Top', 'Right', 'Bottom'].map((side) => (s[`margin${side}`] === undefined ? 0 : css.px(s[`margin${side}`])));

/** Each part's colours, radius and shadow in each theme. */
function colours() {
  return Object.entries(THEMES).map(([theme, t]) => {
    const body = evaluate(sc.Body, t, shared({ $isOpen: true, $isAnimating: true }));
    const arrow = evaluate(sc.Arrow, t, shared());
    const inner = evaluate(sc.Inner, t, shared());
    return {
      theme,
      body: { background: css.color(body.backgroundColor), radius: radius(body), boxShadow: css.boxShadows(body.boxShadow) },
      arrow: { background: css.color(arrow.backgroundColor), boxShadow: css.boxShadows(arrow.boxShadow) },
      inner: { background: css.color(inner.backgroundColor), radius: radius(inner), foreground: css.color(inner.color) },
    };
  });
}

/**
 * The body's margin and where its entrance starts, by placement, arrow and popoverMargin (Body's getPopoverMarginStyles
 * and getStartPosition, at an offset of 0: the rest of the transform is popper.js's offset, which Metered gives the
 * popup's position instead).
 */
function body() {
  const cases = [];
  for (const placement of PLACEMENTS) {
    for (const showArrow of [false, true]) {
      for (const popoverMargin of [C.POPOVER_MARGIN, 0, 13, 30]) {
        const s = evaluate(sc.Body, THEMES.Light, shared({ $placement: placement, $showArrow: showArrow, $popoverMargin: popoverMargin }));
        const end = evaluate(sc.Body, THEMES.Light, shared({ $placement: placement, $showArrow: showArrow, $popoverMargin: popoverMargin, $isOpen: true, $isAnimating: true }));
        // The helpers agree with the style function (the margin and transform come from them alone).
        const m = getPopoverMarginStyles(showArrow ? C.ARROW_SIZE : 0, placement, popoverMargin);
        if (JSON.stringify(margins(m)) !== JSON.stringify(margins(s))) throw new Error('margins');
        if (s.transform !== getStartPosition({ left: 0, top: 0 }, placement, showArrow ? C.ARROW_SIZE : 0, popoverMargin)) throw new Error('start');
        if (end.transform !== getEndPosition({ left: 0, top: 0 })) throw new Error('end');
        cases.push({ placement, showArrow, popoverMargin, margin: margins(s), start: translate(s.transform), end: translate(end.transform) });
      }
    }
  }
  return cases;
}

/** The body's motion by state: its opacity, and the transition's duration and curve (Body's transition*). */
function motion() {
  const cases = [];
  for (const isOpen of [false, true]) {
    for (const isAnimating of [false, true]) {
      for (const animateOutTime of [0, 300]) {
        for (const isHoverTrigger of [false, true]) {
          // popover.js: $animationDuration is animateOutTime || ANIMATE_OUT_TIME.
          const s = evaluate(sc.Body, THEMES.Light, shared({ $isOpen: isOpen, $isAnimating: isAnimating, $animationDuration: animateOutTime || C.ANIMATE_OUT_TIME, $isHoverTrigger: isHoverTrigger }));
          if (s.transitionProperty !== 'opacity,transform') throw new Error(`transition ${s.transitionProperty}`);
          const hover = s.animationName ? {
            seconds: css.seconds(s.animationDuration),
            frames: Object.entries(s.animationName).map(([at, f]) => [parseFloat(at) / 100, f.pointerEvents !== 'none']),
          } : null;
          cases.push({
            isOpen, isAnimating, animateOutTime, isHoverTrigger, opacity: s.opacity,
            seconds: css.seconds(s.transitionDuration), easing: css.cubicBezier(s.transitionTimingFunction), hitTest: hover,
          });
        }
      }
    }
  }
  return cases;
}

/** The arrow's box and where it is put by placement (getArrowPositionStyles), with an offset of 17 on its side. */
function arrow() {
  const cases = PLACEMENTS.map((placement) => {
    const s = evaluate(sc.Arrow, THEMES.Light, shared({ $placement: placement, $arrowOffset: { left: 17, top: 17 } }));
    const at = (k) => (s[k] === undefined ? null : css.px(s[k]));
    return { placement, left: at('left'), top: at('top'), right: at('right'), bottom: at('bottom') };
  });
  const s = evaluate(sc.Arrow, THEMES.Light, shared({ $placement: 'top' }));
  if (s.transform !== 'rotate(45deg)' || s.position !== 'absolute') throw new Error('arrow box');
  return { width: css.px(s.width), height: css.px(s.height), rotate: 45, cases };
}

/** Where popper.js puts the body and the arrow, on its first update and on its next (tether.g.json). */
function tether() {
  const file = path.join(__dirname, 'tether.g.json');
  const g = JSON.parse(fs.readFileSync(file, 'utf8'));
  return {
    popper: g.popper, view: g.view, arrowWidth: g.arrowWidth,
    // The margin the body had (getPopoverMarginStyles at the placement asked for) and has at the placement chosen are
    // the body cases'.
    cases: g.cases.map((c) => {
      const m = getPopoverMarginStyles(c.showArrow ? C.ARROW_SIZE : 0, c.placement, c.popoverMargin);
      if (JSON.stringify(margins(m || {})) !== JSON.stringify(c.margin)) throw new Error('tether.g.json is stale: run node tools/reference/tether.js --write');
      return {
        anchor: c.anchor, x: c.rect[0], y: c.rect[1], width: c.rect[2], height: c.rect[3], bodyWidth: c.body[0], bodyHeight: c.body[1],
        placement: c.placement, showArrow: c.showArrow, popoverMargin: c.popoverMargin, ignoreBoundary: c.ignoreBoundary,
        result: c.result, left: c.offset.left, top: c.offset.top,
        arrowLeft: c.arrowOffset ? c.arrowOffset.left : null, arrowTop: c.arrowOffset ? c.arrowOffset.top : null,
        // popper.js's next update, with the margin at the placement chosen: what Metered shows.
        steadyResult: c.steady.result, steadyLeft: c.steady.offset.left, steadyTop: c.steady.offset.top,
        steadyArrowLeft: c.steady.arrowOffset ? c.steady.arrowOffset.left : null, steadyArrowTop: c.steady.arrowOffset ? c.steady.arrowOffset.top : null,
      };
    }),
  };
}

function oracle(version) {
  const s = Stateful.defaultProps;
  return {
    baseui: version,
    constants: {
      popoverMargin: C.POPOVER_MARGIN, arrowSize: C.ARROW_SIZE, arrowWidth: C.ARROW_WIDTH,
      animateInTime: C.ANIMATE_IN_TIME, animateOutTime: C.ANIMATE_OUT_TIME,
    },
    defaults: {
      accessibilityType: defaults.accessibilityType, focusLock: defaults.focusLock, autoFocus: defaults.autoFocus,
      returnFocus: defaults.returnFocus, ignoreBoundary: defaults.ignoreBoundary, onMouseEnterDelay: defaults.onMouseEnterDelay,
      onMouseLeaveDelay: defaults.onMouseLeaveDelay, placement: defaults.placement, showArrow: defaults.showArrow,
      triggerType: defaults.triggerType, dismissOnClickOutside: s.dismissOnClickOutside, dismissOnEsc: s.dismissOnEsc,
      popoverMargin: s.popoverMargin,
    },
    padding: margins(Object.fromEntries(Object.entries(sc.Padding.__STYLETRON__.getInitialStyle()).map(([k, v]) => [k.replace('padding', 'margin'), v]))),
    colours: colours(),
    body: body(),
    motion: motion(),
    arrow: arrow(),
    tether: tether(),
  };
}

module.exports = { oracle, PLACEMENTS, shared };
