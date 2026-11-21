// The oracle for FormControl: what Base Web's own form control gives (baseui/form-control), for the cases the tests
// build, on the real light and dark themes, in Avalonia's units. Each case renders Base Web's FormControl itself
// (react-dom/server) with every part overridden by a component that records the props form-control.js gives it, round a
// child that records the props cloneElement gives it (a recorder with the defaultProps of Base Web's Input, Textarea,
// Checkbox or RadioGroup, as React resolves them before FormControl reads the child's props). Then each part's own style
// function is evaluated with the props it was given. Which parts render, the hint and its icon, the counter's text, what
// the child is given and every declaration are Base Web's, with nothing decided here. Kept apart from
// tools/tokens/form-control.js, which writes the themes: the tests compare one with the other. Read by cases.js, which
// writes tests/Metered.Tests/Reference/form-control.oracle.json.
'use strict';

const React = require('react');
const { FormControl } = require('baseui/form-control');
const sc = require('baseui/form-control/styled-components');
const { Input } = require('baseui/input');
const { Textarea } = require('baseui/textarea');
const { Checkbox } = require('baseui/checkbox-v2');
const { RadioGroup } = require('baseui/radio-v2');
const Alert = require('baseui/icon/alert').default;
const Check = require('baseui/icon/check').default;
const { Svg } = require('baseui/icon/styled-components');
const css = require('./css');
const { THEMES } = require('./oracle');
const { markup, declarations, sides, colour, icon } = require('./input');

/** Base Web's override keys (types.d.ts's FormControlOverrides), in the order form-control.js renders them. */
const PARTS = ['LabelContainer', 'Label', 'LabelEndEnhancer', 'ControlContainer', 'Caption', 'CaptionIcon', 'CaptionMessage'];

/** The children Base Web's documentation and scenarios put in a FormControl, by the Metered control that is each. */
const CHILDREN = { Input, Textarea, Checkbox, RadioGroup };

/** A child that records the props it is given, with the defaultProps of the Base Web control it stands for. */
function recorder(type, seen) {
  class Child extends React.Component {
    render() { seen.push(this.props); return null; }
  }
  Child.defaultProps = CHILDREN[type].defaultProps;
  return Child;
}

/**
 * A hint, label or enhancer given as a function: `{ fn: 'text' }` in a case, a function returning the text that records
 * the props Base Web calls it with, as they are when it is called: form-control.js changes them after it calls the
 * hint's.
 */
function value(v, calls, name) {
  if (v && typeof v === 'object' && 'fn' in v) return (p) => { calls[name] = { ...p }; return v.fn; };
  return v;
}

/**
 * Renders Base Web's FormControl with these props round a child of that type with those props, and returns, for each
 * part, the props it was given (a part not rendered is missing), the child's props, and the props each function was
 * called with.
 */
function render(props, { child = 'Input', childProps = {}, theme = THEMES.Light } = {}) {
  const seen = {};
  const overrides = {};
  for (const name of PARTS) {
    // eslint-disable-next-line react/display-name
    const component = React.forwardRef((p, ref) => { (seen[name] ||= []).push(p); void ref; return React.createElement('div', null, p.children); });
    overrides[name] = { component };
  }
  const calls = {};
  const own = {};
  for (const k of ['label', 'caption', 'error', 'positive', 'labelEndEnhancer']) {
    if (k in props) own[k] = value(props[k], calls, k);
  }
  const children = [];
  const warn = console.warn;
  // form-control.js warns when error and positive are both set, and when a counter has no length: the cases do both.
  console.warn = (m, ...rest) => { if (!/both set to|`length` must either/.test(String(m))) warn(m, ...rest); };
  try {
    markup(React.createElement(FormControl, { ...props, ...own, overrides },
      React.createElement(recorder(child, children), childProps)), theme);
  } finally {
    console.warn = warn;
  }
  for (const [name, list] of Object.entries(seen)) {
    if (list.length !== 1) throw new Error(`${name}: rendered ${list.length} times`);
    seen[name] = list[0];
  }
  if (children.length !== 1) throw new Error('the child rendered more than once');
  return { parts: seen, child: children[0], calls };
}

/**
 * Each part's declarations for the props Base Web gave it. CaptionIcon and CaptionMessage are given none; CaptionMessage
 * is a style object, not a function (styletron keeps it as the initial style, with no reducer).
 */
function style(part, theme, props) {
  const s = sc[part].__STYLETRON__;
  if (s.reducers.length === 0) return { ...s.getInitialStyle() };
  return declarations(sc[part], theme, props).flat;
}

/** The text a part shows: its children, when they're text. */
const text = (p) => (p === undefined ? null : typeof p.children === 'string' ? p.children : null);

/** Which of Base Web's two icons the caption shows: the element form-control.js renders in CaptionIcon. */
function iconOf(parts) {
  if (!parts.CaptionIcon) return null;
  const c = parts.CaptionIcon.children;
  if (c.type === Alert) return 'alert';
  if (c.type === Check) return 'check';
  throw new Error('a caption icon this oracle doesn\'t know');
}

/** The $ props Base Web gives every part, as the tests' FormControlState: what a function is called with too. */
const state = (p) => (p === undefined ? null : {
  disabled: p.$disabled, error: p.$error, positive: p.$positive,
  length: p.$length ?? null, maxLength: p.$maxLength ?? null, counterError: p.$counterError ?? false,
});

/**
 * The states the colours are drawn in: Base Web's props. A counter past its maximum is an error of its own
 * ($counterError, and $error); `disabled` greys the label and the enhancer, not the caption.
 */
const STATES = [
  ['rest', {}],
  ['disabled', { disabled: true }],
  ['error', { error: true }],
  ['errorMessage', { error: 'Error message' }],
  ['positive', { positive: true }],
  ['positiveMessage', { positive: 'Positive message' }],
  ['errorPositive', { error: true, positive: true }],
  ['disabledError', { disabled: true, error: 'Error message' }],
  ['counterError', { counter: { length: 5, maxLength: 4 } }],
  ['disabledCounterError', { disabled: true, counter: { length: 5, maxLength: 4 } }],
];

/** Each part's colour in each state, with a label, a caption and an end enhancer (the counter's, where it has one). */
function colours() {
  const cases = [];
  for (const theme of Object.keys(THEMES)) {
    for (const [name, extra] of STATES) {
      const t = THEMES[theme];
      const end = extra.counter ? {} : { labelEndEnhancer: 'End' };
      const { parts: r } = render({ label: 'Label', caption: 'Caption', ...end, ...extra }, { theme: t });
      const of = (part) => (r[part] ? colour(style(part, t, r[part]).color) : null);
      cases.push({
        theme, state: name, props: extra,
        label: of('Label'), labelEndEnhancer: of('LabelEndEnhancer'), caption: of('Caption'),
        icon: iconOf(r), hint: text(r.CaptionMessage), endText: text(r.LabelEndEnhancer),
      });
    }
  }
  return cases;
}

/**
 * Which hint Base Web shows and with which icon (chooseRenderedHint, chooseRenderedHintIcon), the parts it renders, the
 * state each part is given, which of aria-describedby and aria-errormessage the child is given, and what each function
 * was called with: a caption, an error and a positive each unset, a boolean, text or a function.
 */
function hints() {
  const cases = [];
  const label = [undefined, 'Label', ''];
  const caption = [undefined, 'Caption', { fn: 'Caption fn' }];
  const error = [undefined, false, true, 'Error', { fn: 'Error fn' }, ''];
  const positive = [undefined, true, 'Positive', { fn: 'Positive fn' }];
  for (const l of label) {
    for (const c of caption) {
      for (const e of error) {
        for (const p of positive) {
          // The label's three values only with the caption alone: the label doesn't change the hint.
          if (l !== 'Label' && (e !== undefined || p !== undefined)) continue;
          const props = {};
          if (l !== undefined) props.label = l;
          if (c !== undefined) props.caption = c;
          if (e !== undefined) props.error = e;
          if (p !== undefined) props.positive = p;
          const { parts: r, child, calls } = render(props);
          cases.push({
            props,
            rendered: PARTS.filter((x) => r[x]),
            hint: text(r.CaptionMessage), icon: iconOf(r),
            iconSize: r.CaptionIcon ? r.CaptionIcon.children.props.size : null,
            iconHidden: r.CaptionIcon ? r.CaptionIcon.children.props['aria-hidden'] === 'true' : null,
            state: state(r.ControlContainer),
            describedBy: child['aria-describedby'] != null, errorMessage: child['aria-errormessage'] != null,
            calls: Object.fromEntries(Object.entries(calls).map(([k, v]) => [k, state(v)])),
          });
        }
      }
    }
  }
  return cases;
}

/**
 * The counter (form-control.js's `counter`): off, on, or an object of length, maxLength and error, round a child with a
 * value and a maxLength or without; the text the label's end enhancer shows, the state the parts and the child are given,
 * and the state the label, a function here, is called with: after the counter has changed it (the hint's function is
 * called before).
 */
function counters() {
  const cases = [];
  const COUNTERS = [false, true, { length: 50 }, { maxLength: 100 }, { length: 50, maxLength: 100 }, { length: 0 }, { maxLength: 0 },
    { error: true }, { error: false }, { length: 45, maxLength: 40 }, { length: 45, maxLength: 40, error: false }];
  const CHILD = [{ value: 'example', maxLength: 50 }, { value: 'example' }, { value: '', maxLength: 50 },
    { value: 'x'.repeat(45), maxLength: 40 }, { maxLength: 40 }, {}];
  for (const counter of COUNTERS) {
    for (const childProps of CHILD) {
      for (const labelEndEnhancer of [undefined, 'Optional']) {
        const props = { label: { fn: 'Label' }, caption: { fn: 'Caption' }, counter };
        if (labelEndEnhancer !== undefined) props.labelEndEnhancer = labelEndEnhancer;
        const { parts: r, child, calls } = render(props, { childProps });
        cases.push({
          counter, child: childProps, labelEndEnhancer: labelEndEnhancer ?? null,
          endText: text(r.LabelEndEnhancer), state: state(r.LabelEndEnhancer ?? r.ControlContainer), childError: child.error ?? null,
          labelCall: state(calls.label), captionCall: state(calls.caption),
        });
      }
    }
  }
  return cases;
}

/**
 * What cloneElement gives each kind of child: error, positive and disabled from the FormControl's, unless the child's
 * own props (its defaultProps included) have them. `own` is the child's own error or disabled, set or not.
 */
function children() {
  const cases = [];
  const FORM = [{}, { error: true }, { error: 'Error' }, { positive: true }, { disabled: true }, { error: true, positive: true }];
  const OWN = [{}, { error: false }, { error: true }, { positive: false }, { disabled: false }];
  for (const type of Object.keys(CHILDREN)) {
    for (const form of FORM) {
      for (const own of OWN) {
        const { child } = render({ label: 'Label', ...form }, { child: type, childProps: own });
        cases.push({ type, form, own, error: child.error ?? null, positive: child.positive ?? null, disabled: child.disabled ?? null });
      }
    }
  }
  return cases;
}

/** The label's htmlFor: the FormControl's, or the child's id (form-control-id.scenario.tsx). */
function labels() {
  const cases = [];
  for (const htmlFor of [undefined, 'two']) {
    for (const id of [undefined, 'one']) {
      const props = { label: 'Label' };
      if (htmlFor) props.htmlFor = htmlFor;
      const { parts: r } = render(props, { childProps: id ? { id } : {} });
      cases.push({ htmlFor: htmlFor ?? null, id: id ?? null, labelFor: r.Label.htmlFor ?? null });
    }
  }
  return cases;
}

const font = (s) => ({ fontSize: css.px(s.fontSize), fontWeight: css.fontWeight(s.fontWeight), lineHeight: css.px(s.lineHeight), family: css.fontFamily(s.fontFamily) });
const margins = (s) => sides(s, 'margin');

/** CSS's collapsed margin of adjoining margins: the largest positive one and the most negative one, added. */
const collapse = (...m) => Math.max(0, ...m) + Math.min(0, ...m);

/**
 * The lengths, the fonts, and where a browser's block flow puts each box for a child of height H: the label container's
 * row (the label's line, or the end enhancer's where it is taller: flex's stretch), then the control container with the
 * child and the caption (its row the message's line, or the icon's box), each gap the collapsed margins between them.
 * Offsets are from the top margin's edge, none collapsing with what is round the form control.
 */
function layout() {
  const t = THEMES.Light;
  const { parts: r } = render({ label: 'Label', caption: 'Caption', error: 'Error', labelEndEnhancer: 'End' });
  const lc = style('LabelContainer', t, r.LabelContainer), label = style('Label', t, r.Label);
  const end = style('LabelEndEnhancer', t, r.LabelEndEnhancer), cc = style('ControlContainer', t, r.ControlContainer);
  const cap = style('Caption', t, r.Caption), ci = style('CaptionIcon', t, {}), msg = style('CaptionMessage', t, {});
  const size = r.CaptionIcon.children.props.size;
  const svg = declarations(Svg, t, { $size: size }).flat;
  if (css.px(svg.width) !== css.px(svg.height)) throw new Error('the caption icon isn\'t square');
  for (const [what, s] of [['Label', label], ['Caption', cap]]) {
    if (sides(s, 'padding').some((x) => x !== 0)) throw new Error(`${what} padding`);
  }
  if (Object.keys(cc).some((k) => /^(padding|border)/.test(k)) || Object.keys(lc).some((k) => /^(padding|border)/.test(k))) throw new Error('a padding or border stops a margin collapsing');
  if (Object.keys(msg).some((k) => k !== 'flexGrow' && msg[k] !== 'inherit')) throw new Error('CaptionMessage has a style of its own');

  const facts = {
    labelContainer: { margin: margins(lc), display: lc.display, width: lc.width },
    label: { font: font(label), display: label.display, width: label.width },
    labelEndEnhancer: { font: font(end), flex: end.flex, width: end.width },
    controlContainer: { margin: margins(cc), width: cc.width },
    caption: { margin: margins(cap), font: font(cap), display: cap.display },
    captionIcon: { padding: sides(ci, 'padding'), flexShrink: ci.flexShrink, display: ci.display, iconSize: css.px(svg.width) },
    captionMessage: { flexGrow: msg.flexGrow },
  };

  const cases = [];
  for (const hasLabel of [false, true]) {
    for (const hasEnd of hasLabel ? [false, true] : [false]) {
      for (const hint of ['none', 'caption', 'icon']) {
        const [lt, lb] = [facts.labelContainer.margin[1], facts.labelContainer.margin[3]];
        const [ct, cb] = [facts.controlContainer.margin[1], facts.controlContainer.margin[3]];
        const [kt, kb] = [facts.caption.margin[1], facts.caption.margin[3]];
        const row = Math.max(facts.label.font.lineHeight, hasEnd ? facts.labelEndEnhancer.font.lineHeight : 0);
        const iconBox = facts.captionIcon.padding[1] + facts.captionIcon.iconSize + facts.captionIcon.padding[3];
        const capRow = Math.max(facts.caption.font.lineHeight, hint === 'icon' ? iconBox : 0);
        // The child's own margins are 0 (Input's and Textarea's Root, the tests' children).
        const labelTop = hasLabel ? lt : null;
        const controlTop = hasLabel ? lt + row + collapse(lb, ct, 0) : collapse(ct, 0);
        let captionGap = null, bottom;
        if (hint === 'none') bottom = collapse(0, cb);
        else { captionGap = collapse(0, kt); bottom = capRow + collapse(kb, cb); }
        cases.push({
          label: hasLabel, labelEndEnhancer: hasEnd, hint,
          labelTop, labelRow: hasLabel ? row : null, controlTop, captionGap, captionRow: hint === 'none' ? null : capRow,
          // The height below the child: the caption, and the margins that end the control container.
          belowChild: (captionGap ?? 0) + bottom,
        });
      }
    }
  }
  return { facts, cases };
}

function oracle(version) {
  const d = FormControl.defaultProps;
  return {
    baseui: version,
    colours: colours(),
    hints: hints(),
    counters: counters(),
    children: children(),
    labels: labels(),
    layout: layout(),
    icons: { alert: icon(Alert), check: icon(Check) },
    defaults: { label: d.label, caption: d.caption, disabled: d.disabled, counter: d.counter },
  };
}

module.exports = { oracle, render, PARTS };
