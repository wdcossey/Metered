// The themes of Radio's parts, Radio.Styled<Part>, and RadioGroup.StyledRadioGroupRoot, generated
// from Base Web's radio-v2 (baseui/radio-v2/styled-components).
//
// Each style function is evaluated on Base Web's light theme with its colours replaced by their own
// names ('@contentPrimary'), for every combination of the radio's states, so that the themes refer
// to the colours by token (DynamicResource, following the theme variant) and read every length as
// a number, with no table typed by hand. Each property is written as a plain setter for the radio's
// default state and a state style for each combination of the states it depends on whose value
// differs; the result is then checked, for every combination, against Base Web's own.
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

function* combinations(i = 0, combo = {}) {
  if (i === AXES.length) { yield { ...combo }; return; }
  for (const v of AXES[i].values) { combo[AXES[i].name] = v; yield* combinations(i + 1, combo); }
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

const outerOf = (c) => declarations(radio.RadioMarkOuter, NAMED, c);

/** Each part: its Base Web styled component, its element type, and its declarations as Avalonia's properties. */
const PARTS = [
  {
    part: 'Root', type: 'b:RadioRootPanel', styled: radio.Root,
    // The label's own element: its padding takes the pointer, so the panel is drawn, transparent.
    fixed: { Background: 'Transparent' },
    convert(s, c) {
      check(s, 'Root', ['flexDirection', 'display', 'alignItems', 'cursor', 'columnGap', 'rowGap', 'padding']);
      if (s.display !== 'inline-flex') throw new Error(`Root display ${s.display}`);
      const row = { row: true, column: false }[s.flexDirection];
      if (row === undefined) throw new Error(`Root flexDirection ${s.flexDirection}`);
      // radio.js renders the label wrapper before the backplate for top and left, after it for bottom and right.
      const first = c.labelPlacement === LABEL_PLACEMENT.top || c.labelPlacement === LABEL_PLACEMENT.left;
      // A single line: only the gap along the direction counts.
      const gap = row ? s.columnGap : s.rowGap;
      return {
        Orientation: row ? 'Horizontal' : 'Vertical',
        LabelFirst: first ? 'True' : 'False',
        AlignItems: { 'flex-start': 'Start', center: 'Center' }[s.alignItems] ?? (() => { throw new Error(`Root alignItems ${s.alignItems}`); })(),
        Spacing: gap === undefined ? '0' : px(gap),
        Padding: s.padding === undefined ? '0' : px(s.padding),
        Cursor: cursor(s.cursor),
      };
    },
  },
  {
    part: 'RadioBackplate', type: 'Border', styled: radio.RadioBackplate,
    convert(s) {
      check(s, 'RadioBackplate', ['position', 'display', 'alignItems', 'justifyContent', 'boxSizing', 'borderRadius',
        'paddingTop', 'paddingBottom', 'paddingLeft', 'paddingRight', 'minHeight', 'minWidth', 'backgroundColor']);
      if (s.boxSizing !== 'border-box' || s.alignItems !== 'center' || s.justifyContent !== 'center') throw new Error('RadioBackplate box');
      return {
        Background: s.backgroundColor === undefined ? 'Transparent' : colour(s.backgroundColor, 'RadioBackplate'),
        CornerRadius: px(s.borderRadius),
        Padding: uniform(s, 'padding'),
        MinWidth: px(s.minWidth),
        MinHeight: px(s.minHeight),
      };
    },
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
      const out = {
        Width: px(s.width),
        Height: px(s.height),
        BorderThickness: px(s.borderWidth),
        BorderBrush: colour(s.borderColor, 'RadioMarkOuter border'),
        Background: colour(s.backgroundColor, 'RadioMarkOuter'),
        CornerRadius: String(css.px(s.width) / 2),
        OutlineBrush: '{x:Null}', OutlineThickness: '0', OutlineOffset: '0',
      };
      if (s.outline !== undefined) {
        const m = /^(\S+) solid (@\w+)$/.exec(s.outline);
        if (!m) throw new Error(`RadioMarkOuter outline ${s.outline}`);
        Object.assign(out, { OutlineBrush: colour(m[2], 'outline'), OutlineThickness: px(m[1]), OutlineOffset: px(s.outlineOffset) });
      }
      return out;
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

const AXIS = Object.fromEntries(AXES.map((a) => [a.name, a]));
const selectorOf = (combo, axes) => '^' + axes.map((name) => {
  const a = AXIS[name], v = combo[name];
  if (typeof v === 'boolean') return v ? a.pseudo(v) : `:not(${a.pseudo(v)})`;
  return a.pseudo(v);
}).join('');
const keyOf = (combo, axes) => axes.map((n) => String(combo[n])).join('|');
const setter = (prop, value) => `      <Setter Property="${prop}" Value="${value}" />`;

/** A part's theme: plain setters for the default state, a state style per combination that differs. */
function theme(p) {
  const values = COMBOS.map((c) => ({ c, v: { ...p.convert(declarations(p.styled, NAMED, c), c) } }));
  const base = values.find(({ c }) => AXES.every((a) => c[a.name] === DEFAULT[a.name])).v;
  const props = Object.keys(base);
  for (const { v } of values) {
    const ks = Object.keys(v);
    if (ks.length !== props.length || ks.some((k) => !props.includes(k))) throw new Error(`${p.part}: properties vary by state`);
  }

  // The states each property depends on.
  const depends = {};
  for (const prop of props) {
    depends[prop] = AXES.map((a) => a.name).filter((name) => {
      const byOthers = new Map();
      for (const { c, v } of values) {
        const key = AXES.filter((a) => a.name !== name).map((a) => String(c[a.name])).join('|');
        const seen = byOthers.get(key);
        if (seen === undefined) byOthers.set(key, v[prop]);
        else if (seen !== v[prop]) return true;
      }
      return false;
    });
  }

  // State styles, by selector, in a fixed order.
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
      const selector = selectorOf(c, axes);
      if (!styles.has(selector)) styles.set(selector, { combo: c, axes, setters: {} });
      styles.get(selector).setters[prop] = v[prop];
    }
  }

  // Avalonia's cascade, simulated: a state style wins over a plain setter, and among state styles the later.
  const list = [...styles.values()];
  for (const { c, v } of values) {
    const got = { ...base };
    for (const s of list) {
      if (s.axes.every((n) => s.combo[n] === c[n])) Object.assign(got, s.setters);
    }
    for (const prop of props) {
      if (got[prop] !== v[prop]) throw new Error(`${p.part}.${prop} in ${JSON.stringify(c)}: ${got[prop]} vs Base Web's ${v[prop]}`);
    }
  }

  const plain = Object.entries({ ...(p.fixed || {}), ...base }).map(([k, v]) => `    <Setter Property="${k}" Value="${v}" />`);
  const blocks = list.map((s) => `    <Style Selector="${selectorOf(s.combo, s.axes)}">\n${Object.entries(s.setters).map(([k, v]) => setter(k, v)).join('\n')}\n    </Style>`);
  return { plain, blocks };
}

/** The inner mark's transition: width and height, timing200 on easeOutCurve, once the radio has loaded. */
function transition() {
  const s = declarations(radio.RadioMarkInner, L, DEFAULT);
  if (s.transitionProperty !== 'width, height') throw new Error(`transition ${s.transitionProperty}`);
  const seconds = css.seconds(s.transitionDuration);
  const [x1, y1, x2, y2] = css.cubicBezier(s.transitionTimingFunction);
  const span = new Date(seconds * 1000).toISOString().substr(11, 12);
  const one = (prop) => `          <DoubleTransition Property="${prop}" Duration="${span}" Easing="${x1},${y1},${x2},${y2}" />`;
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

module.exports = { file, AXES, PARTS, declarations, NAMED };
