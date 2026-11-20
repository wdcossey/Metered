// The oracle for ProgressBar: what Base Web's own progress bar gives (baseui/progress-bar), for the cases the tests
// build, in Avalonia's units. Each case renders Base Web's ProgressBar itself (react-dom/server) with every part
// overridden by a component that records the props progressbar.js gives it, then evaluates each part's own style
// function with those props: the bars it renders, their step states and transforms, the label and the accessible name
// are all Base Web's, with nothing computed here. Kept apart from tools/tokens/progress-bar.js, which writes the
// themes: the tests compare one with the other. Read by cases.js, which writes
// tests/Metered.Tests/Reference/progress-bar.oracle.json.
'use strict';

const React = require('react');
const { renderToStaticMarkup } = require('react-dom/server');
const { Provider } = require('styletron-react');
const { Server } = require('styletron-engine-monolithic');
const { ThemeProvider } = require('baseui');
const { ProgressBar, SIZE, INTENT } = require('baseui/progress-bar');
const pb = require('baseui/progress-bar/styled-components');
const css = require('./css');
const { THEMES, evaluate } = require('./oracle');

const PARTS = { Root: pb.StyledRoot, BarContainer: pb.StyledBarContainer, Bar: pb.StyledBar, BarProgress: pb.StyledBarProgress, InfiniteBar: pb.StyledInfiniteBar, Label: pb.StyledLabel };

/**
 * Renders Base Web's ProgressBar with these props on a theme, and returns, for each part, the props progressbar.js gave
 * each element of it, in order, and the style its own function gives for them.
 */
function render(props, theme = THEMES.Light) {
  const seen = Object.fromEntries(Object.keys(PARTS).map((p) => [p, []]));
  const overrides = {};
  for (const name of Object.keys(PARTS)) {
    // eslint-disable-next-line react/display-name
    const Capture = React.forwardRef((p, ref) => {
      seen[name].push({ props: p, style: evaluate(PARTS[name], theme, p) });
      return React.createElement('div', null, p.children);
    });
    overrides[name] = { component: Capture };
  }
  const engine = new Server();
  renderToStaticMarkup(React.createElement(Provider, { value: engine },
    React.createElement(ThemeProvider, { theme }, React.createElement(ProgressBar, { ...props, overrides }))));
  return seen;
}

const one = (list, what) => {
  if (list.length !== 1) throw new Error(`${what}: ${list.length} elements`);
  return list[0];
};

/**
 * 'translateX(-80%)' to -80, a percentage of the element's own width, as a browser applies it. Over the maximum,
 * progressbar's `translateX(-${width})` with a negative width is 'translateX(--50%)', which isn't CSS: a browser drops
 * the declaration and draws no transform, 0.
 */
function translate(v) {
  if (/^translateX\(--[\d.]+(e[+-]?\d+)?%\)$/.test(v)) return 0;
  const m = /^translateX\((-?\d+(?:\.\d+)?(?:e[+-]?\d+)?)%\)$/.exec(v);
  if (!m) throw new Error(`transform ${v}`);
  return Number(m[1]);
}

/** A bar's step state as Base Web decides it: in progress has the animation, completed sits at 0 %, awaiting at -102 %. */
function stepState(style, steps) {
  if (steps <= 1) return null;
  if (style.animationName !== undefined) return 'inProgress';
  const t = translate(style.transform);
  if (t === 0) return 'completed';
  if (t === -102) return 'awaits';
  throw new Error(`a step at ${style.transform}`);
}

/**
 * The values the tests build, from Base Web's scenarios (progressbar.scenario.tsx, progressbar-intent.scenario.tsx),
 * its unit tests (progressbar.test.tsx), its examples (steps.tsx: 50 of 5 steps) and the ends: empty, full, over and
 * under the range, halves that round, and numbers JavaScript writes whole and in its exponent form.
 */
const VALUES = [
  { value: 20 },
  { value: 20, showLabel: true },
  { value: 120, minValue: 100, maxValue: 200, showLabel: true },
  { value: 20, steps: 5 },
  { value: 120, minValue: 100, maxValue: 200, steps: 10 },
  { value: 75, steps: 4 },
  { value: 50, steps: 5 },
  { value: 60, steps: 5 },
  { value: 0, steps: 3 },
  { value: 100, steps: 3 },
  { value: 0 },
  { value: 100 },
  { value: 150, showLabel: true },
  { value: -10, showLabel: true },
  { value: -10, steps: 4 },
  { value: 12.5, showLabel: true },
  { value: 2.5, minValue: 0, maxValue: 50, showLabel: true },
  { value: 33.3, steps: 3 },
  { value: 1e15, showLabel: true },
  { value: 1.2345678901234568e18, showLabel: true },
  { value: 1e21, showLabel: true },
  { infinite: true },
  { infinite: true, steps: 3 },
  { infinite: true, showLabel: true },
  { value: 75, 'aria-label': 'test', steps: 2 },
  { value: 40, 'aria-label': 'Uploading' },
];

function values() {
  return VALUES.map((p) => {
    const r = render(p);
    const root = one(r.Root, 'Root').props;
    const steps = p.steps ?? 1;
    const label = r.Label.length ? String(one(r.Label, 'Label').props.children) : null;
    return {
      value: p.value ?? 0, minValue: p.minValue ?? 0, maxValue: p.maxValue ?? 100, steps,
      infinite: !!p.infinite, showLabel: !!p.showLabel, ariaLabel: p['aria-label'] ?? null,
      name: root['aria-label'],
      busy: root['aria-busy'] === true,
      label,
      bars: r.BarProgress.map(({ style }) => ({ state: stepState(style, steps), offset: translate(style.transform) })),
      infiniteBars: r.InfiniteBar.length,
    };
  });
}

/** A gradient Base Web draws on an infinite bar: '<deg>deg, transparent 0%, <c> 25%, <c> 75%, transparent 100%'. */
function gradient(image) {
  const m = /^linear-gradient\((\d+)deg, transparent 0%, (#[0-9A-Fa-f]+) ([\d.]+)%, (#[0-9A-Fa-f]+) ([\d.]+)%, transparent 100%\)$/.exec(image);
  if (!m || m[2] !== m[4]) throw new Error(`gradient ${image}`);
  return { angle: Number(m[1]), colour: css.color(m[2]), start: Number(m[3]), end: Number(m[5]) };
}

const INTENTS = [null, ...Object.values(INTENT)];

/** By theme and intent: the track, the fill, the infinite bars' colour and the label. */
function colours() {
  const cases = [];
  for (const theme of Object.keys(THEMES)) {
    for (const intent of INTENTS) {
      const t = THEMES[theme];
      const p = intent === null ? {} : { intent };
      const det = render({ ...p, value: 40, showLabel: true }, t);
      const inf = render({ ...p, infinite: true }, t);
      const [left, right] = inf.InfiniteBar.map((x) => gradient(x.style.backgroundImage));
      if (left.colour !== right.colour) throw new Error('the infinite bars differ in colour');
      cases.push({
        theme, intent,
        bar: css.color(one(det.Bar, 'Bar').style.backgroundColor),
        barProgress: css.color(one(det.BarProgress, 'BarProgress').style.backgroundColor),
        infinite: left.colour,
        label: css.color(one(det.Label, 'Label').style.color),
      });
    }
  }
  return cases;
}

const text = (s) => ({ fontSize: css.px(s.fontSize), fontWeight: css.fontWeight(s.fontWeight), lineHeight: css.px(s.lineHeight) });
const radii = (s) => ['borderTopLeftRadius', 'borderTopRightRadius', 'borderBottomRightRadius', 'borderBottomLeftRadius'].map((k) => (s[k] === undefined ? 0 : css.px(s[k])));

/** By size, stepped (3 steps) or not: the root's gap, the bars' height, radii and margins, the label, the infinite bars. */
function layout() {
  const cases = [];
  for (const size of Object.values(SIZE)) {
    for (const steps of [1, 3]) {
      const det = render({ size, steps, value: 40, showLabel: true });
      const inf = render({ size, infinite: true });
      const root = one(det.Root, 'Root').style;
      const label = one(det.Label, 'Label').style;
      cases.push({
        size, steps,
        rootGap: css.px(root.gap),
        bars: det.Bar.map(({ style }, i) => {
          // :first-child resets the margin: the bar's own, by its place in the container.
          const first = style[':first-child'] ?? {};
          const marginLeft = i === 0 && first.marginLeft !== undefined ? first.marginLeft : style.marginLeft;
          return { height: css.px(style.height), radii: radii(style), marginLeft: marginLeft === undefined ? 0 : css.px(marginLeft) };
        }),
        barProgressRadii: radii(det.BarProgress[0].style),
        labelPadding: [css.px(label.paddingLeft), css.px(label.paddingRight)],
        labelAlign: label.textAlign,
        label: text(label),
        infiniteBars: inf.InfiniteBar.map(({ style }) => ({ height: css.px(style.height), radii: radii(style) })),
      });
    }
  }
  return cases;
}

/** The motion: the fill's transition, the in-progress step's animation and the infinite bars' sweep. */
function motion() {
  const det = one(render({ value: 40 }).BarProgress, 'BarProgress').style;
  const m = /^transform ([\d.]+m?s)$/.exec(det.transition);
  if (!m) throw new Error(`transition ${det.transition}`);
  const step = render({ value: 50, steps: 2 }).BarProgress.find((x) => x.style.animationName !== undefined).style;
  const inf = render({ infinite: true }).InfiniteBar;
  const frames = (name, f) => Object.entries(name).map(([cue, v]) => ({ cue: Number(cue.replace('%', '')), ...f(v) }));
  const animation = (s) => ({
    seconds: css.seconds(s.animationDuration),
    iterations: s.animationIterationCount,
    curve: css.cubicBezier(s.animationTimingFunction),
  });
  return {
    transition: {
      property: 'transform',
      seconds: css.seconds(m[1]),
      // No timing function: CSS's initial value, ease, which CSS Easing Functions Level 1 (§2.2) defines as
      // cubic-bezier(0.25, 0.1, 0.25, 1).
      curve: [0.25, 0.1, 0.25, 1],
    },
    inProgress: {
      ...animation(step),
      frames: frames(step.animationName, (v) => ({ offset: translate(v.transform), opacity: v.opacity })),
    },
    infinite: inf.map(({ props, style }) => ({
      isLeft: !!props.$isLeft,
      ...animation(style),
      position: Number(style.backgroundPositionX.replace('%', '')),
      imageSize: Number(/^([\d.]+)% auto$/.exec(style.backgroundSize)[1]),
      ...(({ angle, start, end }) => ({ angle, start, end }))(gradient(style.backgroundImage)),
      frames: frames(style.animationName, (v) => ({ position: Number(v.backgroundPositionX.replace('%', '')) })),
    })),
  };
}

function oracle(version) {
  return { baseui: version, colours: colours(), layout: layout(), values: values(), motion: motion() };
}

module.exports = { oracle, render };
