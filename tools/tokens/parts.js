// The engine of the v2 controls' part themes (radio.js, checkbox.js), and the conversions they share.
//
// A control's states are axes, each with its values (the default first) and the pseudoclass that carries it. Each
// part's style function is evaluated on Base Web's light theme with its colours replaced by their own names
// ('@contentPrimary'), for every combination of the states, so that the themes refer to the colours by token
// (DynamicResource, following the theme variant) and read every length as a number, with no table typed by hand. Each
// property is written as a plain setter for the default state and a state style for each combination of the states it
// depends on whose value differs; the result is then checked, for every combination, against Base Web's own.
'use strict';

const css = require('../reference/css');
const { THEMES, evaluate, resolve } = require('../reference/oracle');

const L = THEMES.Light;

/** Base Web's light theme with each colour its own name: '@contentPrimary'. Unknown names stay undefined, which fails. */
const NAMED = {
  ...L,
  colors: new Proxy(L.colors, { get: (colors, name) => (typeof name === 'string' && name in colors ? '@' + name : undefined) }),
};

// Conversions from CSS to Avalonia's setter values.
const r = (key) => `{DynamicResource ${key}}`;
function colour(v, what) {
  if (v === 'transparent') return 'Transparent';
  if (typeof v === 'string' && v.startsWith('@')) return r('colors.' + v.slice(1));
  throw new Error(`${what}: not a colour token: ${JSON.stringify(v)}`);
}
const px = (v) => String(css.px(v));
const cursor = (v) => ({ pointer: 'Hand', 'not-allowed': 'No', auto: 'Ibeam' })[v] ?? (() => { throw new Error(`cursor ${v}`); })();
/** A padding or margin's four sides as Avalonia's Thickness (left, top, right, bottom), or its shorthand. */
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

const NESTED = new Set(['@media (hover: hover)', ':active', '@media (pointer: coarse)']);

/**
 * The engine for one control: `axes` its states (a `hover` and an `active` axis are CSS's own on the part; a `coarse`
 * axis is `@media (pointer: coarse)`), `props` Base Web's $ props for a combination of them. `layered` writes the fewest
 * state styles, each naming only the states that must be on (the default writes one per combination that differs).
 */
function engine({ axes, props, layered = false }) {
  function* combinations(i = 0, combo = {}) {
    if (i === axes.length) { yield { ...combo }; return; }
    for (const v of axes[i].values) { combo[axes[i].name] = v; yield* combinations(i + 1, combo); }
  }
  const COMBOS = [...combinations()];
  const DEFAULT = Object.fromEntries(axes.map((a) => [a.name, a.values[0]]));

  /** The declarations a browser applies to the part in that combination. */
  function declarations(styled, theme, c) {
    const style = evaluate(styled, theme, props(c));
    for (const [k, v] of Object.entries(style)) {
      if (v !== null && typeof v === 'object' && !NESTED.has(k)) throw new Error(`${styled.displayName}: a block this generator doesn't read: ${k}`);
    }
    const { self } = resolve(style, { hover: c.hover, active: c.active });
    if (c.coarse) Object.assign(self, style['@media (pointer: coarse)'] || {});
    return self;
  }

  const AXIS = Object.fromEntries(axes.map((a) => [a.name, a]));
  const selectorOf = (combo, names) => '^' + names.map((name) => {
    const a = AXIS[name], v = combo[name];
    if (typeof v === 'boolean') return v ? a.pseudo(v) : `:not(${a.pseudo(v)})`;
    return a.pseudo(v);
  }).join('');
  const keyOf = (combo, names) => names.map((n) => String(combo[n])).join('|');
  const setter = (prop, value) => `      <Setter Property="${prop}" Value="${value}" />`;

  /** A part's theme: plain setters for the default state, a state style per combination that differs. */
  function theme(p) {
    const values = COMBOS.map((c) => ({ c, v: { ...p.convert(declarations(p.styled, NAMED, c), c) } }));
    const base = values.find(({ c }) => axes.every((a) => c[a.name] === DEFAULT[a.name])).v;
    const names = Object.keys(base);
    for (const { v } of values) {
      const ks = Object.keys(v);
      if (ks.length !== names.length || ks.some((k) => !names.includes(k))) throw new Error(`${p.part}: properties vary by state`);
    }

    // The states each property depends on.
    const depends = {};
    for (const prop of names) {
      depends[prop] = axes.map((a) => a.name).filter((name) => {
        const byOthers = new Map();
        for (const { c, v } of values) {
          const key = axes.filter((a) => a.name !== name).map((a) => String(c[a.name])).join('|');
          const seen = byOthers.get(key);
          if (seen === undefined) byOthers.set(key, v[prop]);
          else if (seen !== v[prop]) return true;
        }
        return false;
      });
    }

    const list = layered ? layer(names, depends, values, base) : expand(names, depends, values, base);

    // Avalonia's cascade, simulated: a state style wins over a plain setter, and among state styles the later.
    for (const { c, v } of values) {
      const got = { ...base };
      for (const st of list) if (st.matches(c)) Object.assign(got, st.setters);
      for (const prop of names) {
        if (got[prop] !== v[prop]) throw new Error(`${p.part}.${prop} in ${JSON.stringify(c)}: ${got[prop]} vs Base Web's ${v[prop]}`);
      }
    }

    const plain = Object.entries({ ...(p.fixed || {}), ...base }).map(([k, v]) => `    <Setter Property="${k}" Value="${v}" />`);
    const blocks = list.map((st) => `    <Style Selector="${st.selector}">\n${Object.entries(st.setters).map(([k, v]) => setter(k, v)).join('\n')}\n    </Style>`);
    return { plain, blocks };
  }

  /** A state style for each combination of the states a property depends on whose value differs from the default's. */
  function expand(names, depends, values, base) {
    const styles = new Map();
    for (const prop of names) {
      const on = depends[prop];
      if (on.length === 0) continue;
      const done = new Set();
      for (const { c, v } of values) {
        const key = keyOf(c, on);
        if (done.has(key)) continue;
        done.add(key);
        if (v[prop] === base[prop]) continue;
        const selector = selectorOf(c, on);
        if (!styles.has(selector)) {
          const combo = c;
          styles.set(selector, { selector, matches: (x) => on.every((n) => combo[n] === x[n]), setters: {} });
        }
        styles.get(selector).setters[prop] = v[prop];
      }
    }
    return [...styles.values()];
  }

  // Layering: each state a flag (a boolean axis's true, an other axis's every value but its default), a selector only
  // the flags that must be on, as Base Web's if-chains decide; a later selector with more flags wins.
  const FLAGS = axes.flatMap((ax) => (typeof ax.values[0] === 'boolean' ? [{ name: ax.name, value: true, pseudo: ax.pseudo(true) }]
    : ax.values.slice(1).map((v) => ({ name: ax.name, value: v, pseudo: ax.pseudo(v) }))));
  const maskOf = (c) => FLAGS.reduce((m, fl, i) => (c[fl.name] === fl.value ? m | (1 << i) : m), 0);
  const bits = (mask) => { let n = 0; for (let m = mask; m; m &= m - 1) n++; return n; };
  const flagSelector = (mask) => '^' + FLAGS.filter((_, i) => mask & (1 << i)).map((fl) => fl.pseudo).join('');
  /** The value the last matching layer gives a state: a layer [mask, value] matches a state holding all of mask. */
  const gives = (list, state) => { let v; for (const [m, value] of list) if ((m & state) === m) v = value; return v; };

  /**
   * Layered styles giving `want` (mask to value; undefined where no state has those flags) in every combination of the
   * flags in `relevant`: a decision list found from the top, each step the most general selector (fewest flags, then
   * the most states) whose states not yet decided all take one value, written lowest first so that a later, higher
   * layer wins; then each layer the others make redundant goes. Returns [[mask, value]], checked.
   */
  function layers(relevant, want) {
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

  /** The fewest layered state styles: each property's layers, sharing a style with another's where that keeps its order. */
  function layer(names, depends, values) {
    const byKey = new Map(values.map(({ c, v }) => [keyOf(c, axes.map((ax) => ax.name)), v]));
    const styles = [];
    for (const prop of names) {
      const on = depends[prop];
      if (on.length === 0) continue;
      const relevant = FLAGS.reduce((m, fl, i) => (on.includes(fl.name) ? m | (1 << i) : m), 0);
      const want = new Map();
      for (let mask = 0; mask < 1 << FLAGS.length; mask++) {
        if ((mask & ~relevant) !== 0) continue;
        const c = { ...DEFAULT };
        let possible = true;
        FLAGS.forEach((fl, i) => {
          if (!(mask & (1 << i))) return;
          if (c[fl.name] !== DEFAULT[fl.name]) possible = false;
          c[fl.name] = fl.value;
        });
        want.set(mask, possible ? byKey.get(keyOf(c, axes.map((ax) => ax.name)))[prop] : undefined);
      }
      let at = -1;
      for (const [mask, value] of layers(relevant, want)) {
        if (mask === 0) {
          if (value !== byKey.get(keyOf(DEFAULT, axes.map((ax) => ax.name)))[prop]) throw new Error(`${prop}: a bottom layer that isn't the default`);
          continue;
        }
        let i = styles.findIndex((st, j) => j > at && st.mask === mask);
        if (i < 0) {
          styles.push({ mask, selector: flagSelector(mask), matches: (x) => (maskOf(x) & mask) === mask, setters: {} });
          i = styles.length - 1;
        }
        styles[i].setters[prop] = value;
        at = i;
      }
    }
    return styles;
  }

  return { COMBOS, DEFAULT, declarations, theme };
}

module.exports = {
  L, NAMED, engine, r, colour, px, cursor, uniform, check, TEXT, font, flexRoot, backplate, outline, timing,
};
