// The themes of Radio's parts, Radio.Styled<Part>, and RadioGroup.StyledRadioGroupRoot, generated
// from Base Web's radio-v2 (baseui/radio-v2/styled-components).
//
// Each style function is evaluated on Base Web's light theme with its colours replaced by their own
// names ('@contentPrimary'), for every combination of the radio's states, so that the themes refer
// to the colours by token (DynamicResource, following the theme variant) and read every length as
// a number, with no table typed by hand. Each property is written as a plain setter for the radio's
// default state and a state style for each combination of the states it depends on whose value
// differs; the result is then checked, for every combination, against Base Web's own. The writer, the converters and
// the v2 parts' common conversions (a flex root, a backplate, an outline) are shared with the other v2 controls'
// generators (switch.js, checkbox.js); `layered` writes the fewest state styles instead.
'use strict';

const css = require('../reference/css');
const { THEMES, evaluate, resolve } = require('../reference/oracle');
const radio = require('baseui/radio-v2/styled-components');
const { LABEL_PLACEMENT, ALIGN } = require('baseui/radio-v2/constants');

const L = THEMES.Light;

/** Base Web's light theme with each colour its own name: '@contentPrimary'. Unknown names stay undefined, which fails below. */
const NAMED = {
  ...L,
  colors: new Proxy(L.colors, { get: (colors, name) => (typeof name === 'string' && name in colors ? '@' + name : undefined) }),
};

/**
 * The radio's states, each with its values (the default first) and the pseudoclass that carries it. `hover` and
 * `active` are CSS's own on the part (only the backplate has them); the rest are Base Web's $ props, which the Radio
 * mirrors onto every part, and `coarse` is `@media (pointer: coarse)`.
 */
const AXES = [
  { name: 'checked', values: [false, true], pseudo: () => ':checked' },
  { name: 'disabled', values: [false, true], pseudo: () => ':disabled' },
  { name: 'error', values: [false, true], pseudo: () => ':error' },
  { name: 'isHovered', values: [false, true], pseudo: () => ':is-hovered' },
  { name: 'isActive', values: [false, true], pseudo: () => ':is-active' },
  { name: 'isFocusVisible', values: [false, true], pseudo: () => ':focus-visible' },
  { name: 'align', values: [ALIGN.vertical, ALIGN.horizontal], pseudo: (v) => `:align-${v}` },
  { name: 'labelPlacement', values: [LABEL_PLACEMENT.right, LABEL_PLACEMENT.top, LABEL_PLACEMENT.bottom, LABEL_PLACEMENT.left], pseudo: (v) => `:label-placement-${v}` },
  { name: 'coarse', values: [false, true], pseudo: () => ':pointer-coarse' },
  { name: 'hover', values: [false, true], pseudo: () => ':hover' },
  { name: 'active', values: [false, true], pseudo: () => ':active' },
];

function* combinations(all = AXES, i = 0, combo = {}) {
  if (i === all.length) { yield { ...combo }; return; }
  for (const v of all[i].values) { combo[all[i].name] = v; yield* combinations(all, i + 1, combo); }
}
const COMBOS = [...combinations()];
const DEFAULT = Object.fromEntries(AXES.map((a) => [a.name, a.values[0]]));

/** Base Web's $ props for a combination (radio.js's sharedProps). */
const props = (c) => ({
  $align: c.align, $checked: c.checked, $disabled: c.disabled, $hasDescription: true, $isActive: c.isActive,
  $error: c.error, $isFocused: c.isFocusVisible, $isFocusVisible: c.isFocusVisible, $isHovered: c.isHovered,
  $labelPlacement: c.labelPlacement, $required: false, $value: '',
});

const NESTED = new Set(['@media (hover: hover)', ':active', '@media (pointer: coarse)']);

/** The declarations a browser applies to the part in that combination; `toProps` its $ props (the radio's by default). */
function declarations(styled, theme, c, toProps = props) {
  const style = evaluate(styled, theme, toProps(c));
  for (const [k, v] of Object.entries(style)) {
    if (v !== null && typeof v === 'object' && !NESTED.has(k)) throw new Error(`${styled.displayName}: a block this generator doesn't read: ${k}`);
  }
  const { self } = resolve(style, { hover: c.hover, active: c.active });
  if (c.coarse) Object.assign(self, style['@media (pointer: coarse)'] || {});
  return self;
}

// Conversions from CSS to Avalonia's setter values.
const r = (key) => `{DynamicResource ${key}}`;
function colour(v, what) {
  if (v === 'transparent') return 'Transparent';
  if (typeof v === 'string' && v.startsWith('@')) return r('colors.' + v.slice(1));
  throw new Error(`${what}: not a colour token: ${JSON.stringify(v)}`);
}
const px = (v) => String(css.px(v));
const cursor = (v) => ({ pointer: 'Hand', 'not-allowed': 'No', auto: 'Ibeam' })[v] ?? (() => { throw new Error(`cursor ${v}`); })();
function uniform(s, prefix) {
  const sides = ['Top', 'Right', 'Bottom', 'Left'].map((side) => s[prefix + side]);
  if (s[prefix] !== undefined) return px(s[prefix]);
  if (sides.every((x) => x === undefined)) return '0';
  const [t, rt, b, l] = sides.map((x) => css.px(x ?? 0));
  return `${l},${t},${rt},${b}`;
}
function check(s, what, known) {
  for (const k of Object.keys(s)) if (!known.includes(k)) throw new Error(`${what}: a declaration this generator doesn't read: ${k}`);
}
const RADII = ['borderTopLeftRadius', 'borderTopRightRadius', 'borderBottomRightRadius', 'borderBottomLeftRadius'];
function half(s, what) {
  for (const k of RADII) if (s[k] !== '50%') throw new Error(`${what}: ${k} ${s[k]}`);
}
/**
 * The text properties' names: a TextBlock's own, or (`inherited`) the attached ones an element passes down to the
 * text its content presenter makes, as Button.StyledBaseButton does.
 */
const TEXT = (inherited) => (inherited
  ? { Foreground: 'TextElement.Foreground', FontFamily: 'TextElement.FontFamily', FontSize: 'TextElement.FontSize',
    FontWeight: 'TextElement.FontWeight', LineHeight: 'TextBlock.LineHeight', TextWrapping: 'TextBlock.TextWrapping',
    MaxLines: 'TextBlock.MaxLines', TextTrimming: 'TextBlock.TextTrimming' }
  : { Foreground: 'Foreground', FontFamily: 'FontFamily', FontSize: 'FontSize', FontWeight: 'FontWeight',
    LineHeight: 'LineHeight', TextWrapping: 'TextWrapping', MaxLines: 'MaxLines', TextTrimming: 'TextTrimming' });
/** A typography token's name for the declarations' font: LabelSmall, ParagraphSmall. */
function font(s, what, n) {
  const name = Object.keys(L.typography).find((t) => /^[A-Z]/.test(t) && typeof L.typography[t] === 'object'
    && ['fontFamily', 'fontSize', 'fontWeight', 'lineHeight'].every((k) => L.typography[t][k] === s[k]));
  if (!name) throw new Error(`${what}: no typography token for ${JSON.stringify(s)}`);
  return {
    [n.FontFamily]: r(`typography.${name}.fontFamily`),
    [n.FontSize]: r(`typography.${name}.fontSize`),
    [n.FontWeight]: r(`typography.${name}.fontWeight`),
    [n.LineHeight]: r(`typography.${name}.lineHeight`),
  };
}
/** Base Web's max3LinesStyle: three lines, then an ellipsis. */
function clamp(s, what, n) {
  if (s.WebkitLineClamp === undefined) return { [n.MaxLines]: '0', [n.TextTrimming]: 'None' };
  if (s.overflow !== 'hidden' || s.textOverflow !== 'ellipsis' || s.display !== '-webkit-box' || s.WebkitBoxOrient !== 'vertical') {
    throw new Error(`${what}: a line clamp this generator doesn't read`);
  }
  return { [n.MaxLines]: String(s.WebkitLineClamp), [n.TextTrimming]: 'CharacterEllipsis' };
}
const CLAMP = ['overflow', 'textOverflow', 'display', 'WebkitLineClamp', 'WebkitBoxOrient'];

/**
 * A v2 control's Root, Base Web's label element: an inline flex box of one line, as a RadioRootPanel. `first` is
 * whether the control renders its label before its mark; `known` the declarations the part may have.
 */
function flexRoot(s, what, first, known) {
  check(s, what, known);
  if (s.display !== 'inline-flex') throw new Error(`${what} display ${s.display}`);
  const row = { row: true, column: false }[s.flexDirection];
  if (row === undefined) throw new Error(`${what} flexDirection ${s.flexDirection}`);
  // A single line: only the gap along the direction counts.
  const gap = row ? s.columnGap : s.rowGap;
  return {
    Orientation: row ? 'Horizontal' : 'Vertical',
    LabelFirst: first ? 'True' : 'False',
    AlignItems: { 'flex-start': 'Start', center: 'Center' }[s.alignItems] ?? (() => { throw new Error(`${what} alignItems ${s.alignItems}`); })(),
    Spacing: gap === undefined ? '0' : px(gap),
    Padding: s.padding === undefined ? '0' : px(s.padding),
    Cursor: cursor(s.cursor),
  };
}

/** A v2 control's backplate, the plate behind its mark that its own :hover and :active tint, as a Border. */
function backplate(s, what, known) {
  check(s, what, known);
  if (s.boxSizing !== 'border-box' || s.alignItems !== 'center' || s.justifyContent !== 'center') throw new Error(`${what} box`);
  return {
    Background: s.backgroundColor === undefined ? 'Transparent' : colour(s.backgroundColor, what),
    CornerRadius: px(s.borderRadius),
    Padding: uniform(s, 'padding'),
    MinWidth: px(s.minWidth),
    MinHeight: px(s.minHeight),
  };
}

/** CSS's outline as a RadioMark's: '<width> solid <colour>' and its offset, or none. */
function outline(s, what) {
  if (s.outline === undefined) return { OutlineBrush: '{x:Null}', OutlineThickness: '0', OutlineOffset: '0' };
  const m = /^(\S+) solid (@\w+)$/.exec(s.outline);
  if (!m) throw new Error(`${what} outline ${s.outline}`);
  return { OutlineBrush: colour(m[2], 'outline'), OutlineThickness: px(m[1]), OutlineOffset: px(s.outlineOffset) };
}

/** A transition's duration and curve as Avalonia's: { span: '00:00:00.200', easing: 'x1,y1,x2,y2' }. */
function timing(s) {
  const seconds = css.seconds(s.transitionDuration);
  const [x1, y1, x2, y2] = css.cubicBezier(s.transitionTimingFunction);
  return { span: new Date(seconds * 1000).toISOString().substr(11, 12), easing: `${x1},${y1},${x2},${y2}` };
}

const outerOf = (c) => declarations(radio.RadioMarkOuter, NAMED, c);

/** Each part: its Base Web styled component, its element type, and its declarations as Avalonia's properties. */
const PARTS = [
  {
    part: 'Root', type: 'b:RadioRootPanel', styled: radio.Root,
    // The label's own element: its padding takes the pointer, so the panel is drawn, transparent.
    fixed: { Background: 'Transparent' },
    // radio.js renders the label wrapper before the backplate for top and left, after it for bottom and right.
    convert: (s, c) => flexRoot(s, 'Root', c.labelPlacement === LABEL_PLACEMENT.top || c.labelPlacement === LABEL_PLACEMENT.left,
      ['flexDirection', 'display', 'alignItems', 'cursor', 'columnGap', 'rowGap', 'padding']),
  },
  {
    part: 'RadioBackplate', type: 'Border', styled: radio.RadioBackplate,
    convert: (s) => backplate(s, 'RadioBackplate', ['position', 'display', 'alignItems', 'justifyContent', 'boxSizing', 'borderRadius',
      'paddingTop', 'paddingBottom', 'paddingLeft', 'paddingRight', 'minHeight', 'minWidth', 'backgroundColor']),
  },
  {
    part: 'RadioMarkOuter', type: 'b:RadioMark', styled: radio.RadioMarkOuter,
    // Centred in the backplate: its alignItems and justifyContent.
    fixed: { HorizontalAlignment: 'Center', VerticalAlignment: 'Center' },
    convert(s) {
      check(s, 'RadioMarkOuter', ['display', 'height', 'width', 'alignItems', 'justifyContent', 'flexShrink', 'boxSizing',
        'borderWidth', 'borderStyle', 'borderColor', 'backgroundColor', ...RADII, 'verticalAlign', 'outline', 'outlineOffset']);
      if (s.boxSizing !== 'border-box' || s.borderStyle !== 'solid' || s.alignItems !== 'center' || s.justifyContent !== 'center') throw new Error('RadioMarkOuter box');
      half(s, 'RadioMarkOuter');
      return {
        Width: px(s.width),
        Height: px(s.height),
        BorderThickness: px(s.borderWidth),
        BorderBrush: colour(s.borderColor, 'RadioMarkOuter border'),
        Background: colour(s.backgroundColor, 'RadioMarkOuter'),
        CornerRadius: String(css.px(s.width) / 2),
        ...outline(s, 'RadioMarkOuter'),
      };
    },
  },
  {
    part: 'RadioMarkInner', type: 'b:RadioMark', styled: radio.RadioMarkInner,
    // Centred in the outer mark: its alignItems and justifyContent.
    fixed: { HorizontalAlignment: 'Center', VerticalAlignment: 'Center' },
    convert(s, c) {
      check(s, 'RadioMarkInner', [...RADII, 'height', 'width', 'backgroundColor', 'boxShadow', 'transitionProperty', 'transitionDuration', 'transitionTimingFunction']);
      half(s, 'RadioMarkInner');
      // 100% of the outer mark's box inside its border (box-sizing border-box).
      const o = outerOf(c);
      const full = css.px(o.width) - 2 * css.px(o.borderWidth);
      if (css.px(o.height) !== css.px(o.width)) throw new Error('RadioMarkOuter isn\'t square');
      const size = (v) => (v === '100%' ? String(full) : px(v));
      let overlay = '{x:Null}';
      if (s.boxShadow !== 'none') {
        // Base Web's overlay: inset 999px 999px 0px <colour>, which fills the box over its background.
        const m = /^inset 999px 999px 0px (@\w+)$/.exec(s.boxShadow);
        if (!m) throw new Error(`RadioMarkInner boxShadow ${s.boxShadow}`);
        overlay = colour(m[1], 'overlay');
      }
      return {
        Width: size(s.width),
        Height: size(s.height),
        Background: colour(s.backgroundColor, 'RadioMarkInner'),
        // 50%: a radius of half the largest size, which a smaller disc clamps to its own half.
        CornerRadius: String(full / 2),
        Overlay: overlay,
      };
    },
  },
  {
    part: 'LabelWrapper', type: 'StackPanel', styled: radio.LabelWrapper,
    convert(s) {
      check(s, 'LabelWrapper', ['display', 'flexDirection', 'alignItems', 'paddingTop', 'rowGap', 'maxWidth']);
      if (s.display !== 'flex' || s.flexDirection !== 'column' || s.alignItems !== 'flex-start') throw new Error('LabelWrapper box');
      // Its top padding as a margin: a StackPanel has no padding, and nothing is drawn in it.
      return {
        Orientation: 'Vertical',
        Spacing: px(s.rowGap),
        Margin: s.paddingTop === undefined ? '0' : `0,${px(s.paddingTop)},0,0`,
        MaxWidth: s.maxWidth === undefined ? 'Infinity' : px(s.maxWidth),
      };
    },
  },
  {
    // A box around the content presenter, whose text takes the box's font, colour, wrapping and clamp.
    part: 'Label', type: 'Border', styled: radio.Label,
    // In the label wrapper's column at the start (its alignItems flex-start), wrapping as a div's text does.
    fixed: { HorizontalAlignment: 'Left', [TEXT(true).TextWrapping]: 'Wrap' },
    convert(s) {
      check(s, 'Label', ['verticalAlign', 'color', 'fontFamily', 'fontSize', 'fontWeight', 'lineHeight', ...CLAMP]);
      const n = TEXT(true);
      return { [n.Foreground]: colour(s.color, 'Label'), ...font(s, 'Label', n), ...clamp(s, 'Label', n) };
    },
  },
  {
    part: 'Description', type: 'TextBlock', styled: radio.Description,
    fixed: { HorizontalAlignment: 'Left', TextWrapping: 'Wrap' },
    convert(s) {
      check(s, 'Description', ['fontFamily', 'fontSize', 'fontWeight', 'lineHeight', 'color', 'cursor', ...CLAMP]);
      const n = TEXT(false);
      return { [n.Foreground]: colour(s.color, 'Description'), Cursor: cursor(s.cursor), ...font(s, 'Description', n), ...clamp(s, 'Description', n) };
    },
  },
];

const selectorOf = (combo, axes, all = AXES) => '^' + axes.map((name) => {
  const a = all.find((x) => x.name === name), v = combo[name];
  if (typeof v === 'boolean') return v ? a.pseudo(v) : `:not(${a.pseudo(v)})`;
  return a.pseudo(v);
}).join('');
const keyOf = (combo, axes) => axes.map((n) => String(combo[n])).join('|');
const setter = (prop, value) => `      <Setter Property="${prop}" Value="${value}" />`;

/**
 * A part's theme: plain setters for the default state, a state style per combination that differs. Another control's
 * generator (switch.js) gives its own states and declarations; the radio's are the default.
 */
/**
 * A part's theme: plain setters for the default state, and a state style per combination that differs or, `layered`,
 * the fewest state styles (layer). `all` is the control's states, `decl` its declarations for a combination.
 */
function theme(p, all = AXES, decl = declarations, { layered = false } = {}) {
  const combos = all === AXES ? COMBOS : [...combinations(all)];
  const values = combos.map((c) => ({ c, v: { ...p.convert(decl(p.styled, NAMED, c), c) } }));
  const base = values.find(({ c }) => all.every((a) => c[a.name] === a.values[0])).v;
  const props = Object.keys(base);
  for (const { v } of values) {
    const ks = Object.keys(v);
    if (ks.length !== props.length || ks.some((k) => !props.includes(k))) throw new Error(`${p.part}: properties vary by state`);
  }

  // The states each property depends on.
  const depends = {};
  for (const prop of props) {
    depends[prop] = all.map((a) => a.name).filter((name) => {
      const byOthers = new Map();
      for (const { c, v } of values) {
        const key = all.filter((a) => a.name !== name).map((a) => String(c[a.name])).join('|');
        const seen = byOthers.get(key);
        if (seen === undefined) byOthers.set(key, v[prop]);
        else if (seen !== v[prop]) return true;
      }
      return false;
    });
  }

  const list = layered ? layer(props, depends, values, all) : expand(props, depends, values, all);

  // Avalonia's cascade, simulated: a state style wins over a plain setter, and among state styles the later.
  for (const { c, v } of values) {
    const got = { ...base };
    for (const st of list) if (st.matches(c)) Object.assign(got, st.setters);
    for (const prop of props) {
      if (got[prop] !== v[prop]) throw new Error(`${p.part}.${prop} in ${JSON.stringify(c)}: ${got[prop]} vs Base Web's ${v[prop]}`);
    }
  }
  const plain = Object.entries({ ...(p.fixed || {}), ...base }).map(([k, v]) => `    <Setter Property="${k}" Value="${v}" />`);
  const blocks = list.map((st) => `    <Style Selector="${st.selector}">\n${Object.entries(st.setters).map(([k, v]) => setter(k, v)).join('\n')}\n    </Style>`);
  return { plain, blocks };
}

/** A state style for each combination of the states a property depends on whose value differs from the default's. */
function expand(props, depends, values, all) {
  const base = values.find(({ c }) => all.every((a) => c[a.name] === a.values[0])).v;
  const styles = new Map();
  for (const prop of props) {
    const axes = depends[prop];
    if (axes.length === 0) continue;
    const done = new Set();
    for (const { c, v } of values) {
      const key = keyOf(c, axes);
      if (done.has(key)) continue;
      done.add(key);
      if (v[prop] === base[prop]) continue;
      const selector = selectorOf(c, axes, all);
      if (!styles.has(selector)) styles.set(selector, { selector, matches: (x) => axes.every((n) => c[n] === x[n]), setters: {} });
      styles.get(selector).setters[prop] = v[prop];
    }
  }
  return [...styles.values()];
}

const bits = (mask) => { let n = 0; for (let m = mask; m; m &= m - 1) n++; return n; };
/** The value the last matching layer gives a state: a layer [mask, value] matches a state holding all of mask. */
const gives = (list, state) => { let v; for (const [m, value] of list) if ((m & state) === m) v = value; return v; };

/**
 * Layered styles giving `want` (mask to value; undefined where no state has those flags): a decision list found from the
 * top, each step the most general selector (fewest flags, then the most states) whose states not yet decided all take
 * one value, written lowest first so that a later, higher layer wins; then each layer the others make redundant goes.
 * Returns [[mask, value]], checked.
 */
function layers(want) {
  const terms = [...want.keys()].sort((x, y) => bits(x) - bits(y) || x - y);
  let open = [...want.keys()].filter((m) => want.get(m) !== undefined);
  const chosen = [];
  while (open.length) {
    let best = null;
    for (const term of terms) {
      if (best && bits(term) > bits(best.term)) break;
      const hit = open.filter((m) => (m & term) === term);
      if (!hit.length || new Set(hit.map((m) => want.get(m))).size !== 1) continue;
      if (!best || hit.length > best.hit.length) best = { term, hit };
    }
    chosen.push([best.term, want.get(best.hit[0])]);
    open = open.filter((m) => !best.hit.includes(m));
  }
  let list = chosen.reverse();
  const right = (l) => [...want].every(([m, v]) => v === undefined || gives(l, m) === v);
  if (!right(list)) throw new Error('Layering wrong');
  for (let i = list.length - 1; i >= 0; i--) {
    const without = list.filter((_, j) => j !== i);
    if (right(without)) list = without;
  }
  return list;
}

/**
 * The fewest layered state styles. Each state is a flag (a boolean axis's true, another axis's every value but its
 * default) and a selector names only the flags that must be on, as Base Web's if-chains decide; a property's layers
 * share a style with another's where that keeps its own order.
 */
function layer(props, depends, values, all) {
  const flags = all.flatMap((ax) => (typeof ax.values[0] === 'boolean' ? [{ name: ax.name, value: true, pseudo: ax.pseudo(true) }]
    : ax.values.slice(1).map((v) => ({ name: ax.name, value: v, pseudo: ax.pseudo(v) }))));
  const maskOf = (c) => flags.reduce((m, fl, i) => (c[fl.name] === fl.value ? m | (1 << i) : m), 0);
  const names = all.map((ax) => ax.name);
  const DEFAULTS = Object.fromEntries(all.map((ax) => [ax.name, ax.values[0]]));
  const byKey = new Map(values.map(({ c, v }) => [keyOf(c, names), v]));
  const styles = [];
  for (const prop of props) {
    const on = depends[prop];
    if (on.length === 0) continue;
    const relevant = flags.reduce((m, fl, i) => (on.includes(fl.name) ? m | (1 << i) : m), 0);
    const want = new Map();
    for (let mask = 0; mask < 1 << flags.length; mask++) {
      if ((mask & ~relevant) !== 0) continue;
      const c = { ...DEFAULTS };
      let possible = true;
      flags.forEach((fl, i) => {
        if (!(mask & (1 << i))) return;
        if (c[fl.name] !== DEFAULTS[fl.name]) possible = false;
        c[fl.name] = fl.value;
      });
      want.set(mask, possible ? byKey.get(keyOf(c, names))[prop] : undefined);
    }
    let at = -1;
    for (const [mask, value] of layers(want)) {
      if (mask === 0) {
        if (value !== byKey.get(keyOf(DEFAULTS, names))[prop]) throw new Error(`${prop}: a bottom layer that isn't the default`);
        continue;
      }
      let i = styles.findIndex((st, j) => j > at && st.mask === mask);
      if (i < 0) {
        const selector = '^' + flags.filter((_, k) => mask & (1 << k)).map((fl) => fl.pseudo).join('');
        styles.push({ mask, selector, matches: (x) => (maskOf(x) & mask) === mask, setters: {} });
        i = styles.length - 1;
      }
      styles[i].setters[prop] = value;
      at = i;
    }
  }
  return styles;
}

/** The inner mark's transition: width and height, timing200 on easeOutCurve, once the radio has loaded. */
function transition() {
  const s = declarations(radio.RadioMarkInner, L, DEFAULT);
  if (s.transitionProperty !== 'width, height') throw new Error(`transition ${s.transitionProperty}`);
  const { span, easing } = timing(s);
  const one = (prop) => `          <DoubleTransition Property="${prop}" Duration="${span}" Easing="${easing}" />`;
  // CSS doesn't transition a page's first paint; Avalonia would. So only once the radio has loaded (:loaded).
  return `    <Style Selector="^:loaded">
      <Setter Property="Transitions">
        <Transitions>
${one('Width')}
${one('Height')}
        </Transitions>
      </Setter>
    </Style>`;
}

const DESCRIBE = {
  Root: 'the label element: a row or a column by label placement, the label wrapper before the backplate for top and left; on a coarse pointer, padding for a 48 px target.',
  RadioBackplate: 'the round-cornered plate behind the mark, tinted under a hovering mouse and while pressed.',
  RadioMarkOuter: 'the 17 px disc, its colour in its border and fill; the focus outline when checked and focused by keyboard.',
  RadioMarkInner: 'the disc inside: the whole inside unchecked, a 5 px dot checked; the hover and press overlay.',
  LabelWrapper: 'the label and the description, in a column.',
  Label: 'the radio\'s content.',
  Description: 'the description under the label.',
};

function file(version) {
  const themes = PARTS.map((p) => {
    const { plain, blocks } = theme(p);
    const extra = p.part === 'RadioMarkInner' ? '\n' + transition() : '';
    return `  <!-- ${p.part}: ${DESCRIBE[p.part]} -->
  <ControlTheme x:Key="Radio.Styled${p.part}" TargetType="${p.type}">
${plain.join('\n')}${extra}
${blocks.join('\n')}
  </ControlTheme>`;
  });

  // RadioGroupRoot: a flex box whose direction, wrap and gaps are the group's items panel (RadioGroup.axaml), tested
  // against Base Web's; nothing else is set on the element.
  const g = evaluate(radio.RadioGroupRoot, L, { $align: ALIGN.vertical, $labelPlacement: LABEL_PLACEMENT.right });
  check(g, 'RadioGroupRoot', ['display', 'flexWrap', 'flexDirection', 'alignItems', 'columnGap', 'rowGap']);

  return `<!--
  Generated by tools/tokens/gen.js (tools/tokens/radio.js) from baseui ${version}
  (© Uber Technologies, Inc., MIT). Do not edit.
  The themes of Radio's parts, Radio.Styled<Part>, from Base Web's radio-v2: each part's look by the
  state pseudoclasses the Radio mirrors onto it (Metered.Controls.Radio), the colours by token, the
  lengths read from Base Web's own style functions.
-->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:b="https://github.com/wdcossey/Metered">
${themes.join('\n')}
  <!-- RadioGroupRoot: the group's box. Its direction, wrap and gaps are the group's items panel (RadioGroup.axaml). -->
  <ControlTheme x:Key="RadioGroup.StyledRadioGroupRoot" TargetType="Border" />
</ResourceDictionary>
`;
}

module.exports = {
  file, AXES, PARTS, declarations, L, NAMED, combinations, theme, colour, px, cursor, uniform, check, font, TEXT, RADII, half,
  flexRoot, backplate, outline, timing,
};
