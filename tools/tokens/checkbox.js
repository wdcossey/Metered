// The themes of Checkbox's parts, Checkbox.Styled<Part>, generated from Base Web's own Checkbox
// (baseui/checkbox/styled-components: the v1 checkbox, with its toggle style). Nothing here is a
// port of Base Web's branches:
//  - which colour token each part uses in each state is read by evaluating the style function with
//    a probe theme whose colours are their own token names, for every combination of the state
//    props; the states are then written as the fewest layered styles that give the same token in
//    every combination, a result this file checks before it writes anything;
//  - every length is read from the style function evaluated with Base Web's light theme.
// The tests' oracle (tools/reference/cases.js) checks the result against the real colours.
'use strict';

const css = require('../reference/css');
const { THEMES, evaluate } = require('../reference/oracle');
const checkbox = require('baseui/checkbox/styled-components');

const L = THEMES.Light;

/** Light, with each colour its own name ('@tickFill') and each lighting value its own key. */
const PROBE = {
  ...L,
  colors: Object.fromEntries(Object.keys(L.colors).map((k) => [k, '@' + k])),
  lighting: Object.fromEntries(Object.keys(L.lighting).map((k) => [k, '@lighting.' + k])),
};

// Base Web's state props, each a pseudoclass the Checkbox mirrors onto its parts (ADR 0001).
// :disabled is Avalonia's own; the rest are set by Metered.Controls.Checkbox.
const FLAGS = [
  ['$disabled', ':disabled'],
  ['$error', ':error'],
  ['$checked', ':checked'],
  ['$isIndeterminate', ':is-indeterminate'],
  ['$isHovered', ':hover'],
  ['$isActive', ':active'],
  ['$isFocusVisible', ':focus-visible'],
];
const ALL = (1 << FLAGS.length) - 1;
const bits = (mask) => { let n = 0; for (let m = mask; m; m &= m - 1) n++; return n; };
// The order every layer is written in: fewer pseudoclasses first, so a fuller match comes later and wins.
const ORDER = [...Array(ALL + 1).keys()].sort((a, b) => bits(a) - bits(b) || a - b);

const PLACEMENTS = ['top', 'right', 'bottom', 'left'];

/** The $ props for a mask of FLAGS. */
function stateProps(mask, extra) {
  const p = { $labelPlacement: 'right', $required: false, $isFocused: false, $value: '' };
  FLAGS.forEach(([prop], i) => { p[prop] = (mask & (1 << i)) !== 0; });
  return { ...p, ...extra };
}

/** A style function evaluated on the probe theme for a mask of states. */
const probe = (styled, mask, extra) => evaluate(styled, PROBE, stateProps(mask, extra));
/** The same on Base Web's light theme, for lengths. */
const light = (styled, extra) => evaluate(styled, L, stateProps(0, extra));

/** The value the last matching layer gives a state: a layer [mask, value] matches a state holding all of mask. */
function gives(list, state) {
  let v;
  for (const [m, value] of list) if ((m & state) === m) v = value;
  return v;
}

/**
 * Layered styles giving `value(mask)` in every combination of the flags in `relevant`, as Base
 * Web's if-chains give them: a decision list found from the top, each step the most general
 * selector (fewest pseudoclasses, then the most states) whose states not yet decided all take one
 * value, written lowest first so that a later, higher layer wins. `value` returns undefined where
 * the property doesn't matter. Returns [[mask, value]], checked against every combination.
 */
function layers(relevant, value) {
  const want = new Map();
  for (let mask = 0; mask <= ALL; mask++) {
    if ((mask & ~relevant) === 0) want.set(mask, value(mask));
  }
  const terms = ORDER.filter((m) => (m & ~relevant) === 0);
  let open = [...want.keys()].filter((s) => want.get(s) !== undefined);
  const chosen = [];
  while (open.length) {
    let best = null;
    for (const term of terms) {
      if (best && bits(term) > bits(best.term)) break;
      const hit = open.filter((s) => (s & term) === term);
      if (!hit.length || new Set(hit.map((s) => want.get(s))).size !== 1) continue;
      if (!best || hit.length > best.hit.length) best = { term, hit };
    }
    chosen.push([best.term, want.get(best.hit[0])]);
    open = open.filter((s) => !best.hit.includes(s));
  }
  const list = chosen.reverse();
  for (const [state, v] of want) {
    if (v !== undefined && gives(list, state) !== v) throw new Error(`Layering wrong at ${selector(state)}`);
  }
  return list;
}

const flagMask = (...props) => props.reduce((m, p) => m | (1 << FLAGS.findIndex(([name]) => name === p)), 0);
const selector = (mask) => '^' + FLAGS.filter((_, i) => mask & (1 << i)).map(([, pseudo]) => pseudo).join('');

/** A probed colour ('@tickFill', 'transparent') as a setter value. */
function brush(v) {
  if (v === 'transparent') return 'Transparent';
  if (typeof v === 'string' && v.startsWith('@')) return `{DynamicResource colors.${v.slice(1)}}`;
  throw new Error(`Not a probed colour: ${JSON.stringify(v)}`);
}

/** A probed box-shadow: a lighting token, or Base Web's focus ring (a spread with no offset or blur). */
function shadow(v) {
  if (v === 'none' || v === undefined) return { boxShadow: 'none', ring: null };
  if (/^@lighting\.\w+$/.test(v)) return { boxShadow: `{DynamicResource lighting.${v.slice('@lighting.'.length)}}`, ring: null };
  const m = /^0 0 0 (\d+)px (@\w+)$/.exec(v);
  if (m) return { boxShadow: 'none', ring: { width: Number(m[1]), brush: brush(m[2]) } };
  throw new Error(`Not a box-shadow this generator reads: ${JSON.stringify(v)}`);
}

/** CSS's outline: 'none' or '<width>px solid <colour>'. */
function outline(v) {
  if (v === 'none') return null;
  const m = /^(\d+)px solid (@\w+)$/.exec(v);
  if (!m) throw new Error(`Not an outline this generator reads: ${JSON.stringify(v)}`);
  return { width: Number(m[1]), brush: brush(m[2]) };
}

/** The SVG of a background-image data URL: its size, its one path and that path's fill. */
function glyph(backgroundImage) {
  if (!backgroundImage) return null;
  const m = /^url\('data:image\/svg\+xml,(.*)'\);?$/s.exec(backgroundImage);
  if (!m) throw new Error(`Not an SVG data URL: ${backgroundImage}`);
  const svg = decodeURIComponent(m[1]);
  const w = Number(/<svg[^>]*\swidth="([\d.]+)"/.exec(svg)[1]);
  const h = Number(/<svg[^>]*\sheight="([\d.]+)"/.exec(svg)[1]);
  const box = /viewBox="([^"]+)"/.exec(svg)[1];
  if (box !== `0 0 ${w} ${h}`) throw new Error(`A viewBox that isn't the SVG's size: ${box}`);
  const paths = [...svg.matchAll(/<path d="([^"]+)" fill="([^"]+)"\/>/g)];
  if (paths.length !== 1) throw new Error('Not one path');
  return { d: paths[0][1], width: w, height: h, fill: paths[0][2] };
}

const setter = (prop, value) => `    <Setter Property="${prop}" Value="${value}" />`;
const nested = (prop, value) => `      <Setter Property="${prop}" Value="${value}" />`;
const same = (a, b, what) => { if (a !== b) throw new Error(`${what}: ${a} vs ${b}`); return a; };
const thickness = (s, prefix) => {
  const [t, rt, b, l] = ['Top', 'Right', 'Bottom', 'Left'].map((side) => css.px(s[prefix + side] ?? 0));
  return `${l},${t},${rt},${b}`;
};
const uniform = (s, prefix, suffix = '') => {
  const v = ['Top', 'Right', 'Bottom', 'Left'].map((side) => s[prefix + side + suffix]);
  if (new Set(v).size !== 1) throw new Error(`${prefix}*${suffix} isn't uniform: ${v}`);
  return v[0];
};
const radii = (s) => {
  const v = ['TopLeft', 'TopRight', 'BottomRight', 'BottomLeft'].map((c) => s[`border${c}Radius`]);
  if (new Set(v).size !== 1) throw new Error(`Radii aren't uniform: ${v}`);
  return v[0];
};

/**
 * A theme's state styles from per-property layers: { property: [[mask, value]] }. The plain values
 * (mask 0) are the theme's own setters; the rest are styles, a property's layers sharing a style
 * with another's where that keeps each property's own order, which is all its layering needs.
 */
function stateStyles(byProperty) {
  const plain = [];
  const styles = [];
  for (const [prop, list] of Object.entries(byProperty)) {
    let at = -1;
    for (const [mask, value] of list) {
      if (mask === 0) { plain.push(setter(prop, value)); continue; }
      let i = styles.findIndex((s, j) => j > at && s.mask === mask);
      if (i < 0) { styles.push({ mask, setters: [] }); i = styles.length - 1; }
      styles[i].setters.push(nested(prop, value));
      at = i;
    }
  }
  const blocks = styles.map((s) => `    <Style Selector="${selector(s.mask)}">\n${s.setters.join('\n')}\n    </Style>`);
  return { plain, blocks };
}

/** CSS's transition timing to Avalonia's Easing text. */
function easing(timing) {
  const named = { ease: [0.25, 0.1, 0.25, 1], linear: [0, 0, 1, 1] };
  return (named[timing] ?? css.cubicBezier(timing)).join(',');
}
const duration = (v) => `0:0:${css.seconds(v)}`;

function rootTheme() {
  const by = {};
  by.Cursor = layers(flagMask('$disabled'), (m) => {
    const c = probe(checkbox.Root, m).cursor;
    return { pointer: 'Hand', 'not-allowed': 'No' }[c] ?? (() => { throw new Error(`cursor ${c}`); })();
  });
  const { plain, blocks } = stateStyles(by);
  const placement = PLACEMENTS.map((p) => {
    const s = light(checkbox.Root, { $labelPlacement: p });
    const orientation = { row: 'Horizontal', column: 'Vertical' }[s.flexDirection];
    const align = { 'flex-start': 'Start', center: 'Center' }[s.alignItems];
    if (!orientation || !align) throw new Error(`Root ${p}: ${s.flexDirection} ${s.alignItems}`);
    return `    <Style Selector="^:label-${p}">\n${nested('Orientation', orientation)}\n${nested('AlignItems', align)}\n    </Style>`;
  });
  same(light(checkbox.Root).display, 'flex', 'Root display');
  return `  <!-- Root: Base Web's label element, a flex line of the checkmark (or the toggle) and the label.
       Its whole box takes the pointer, as a label's does: the tap area is the row. -->
  <ControlTheme x:Key="Checkbox.StyledRoot" TargetType="b:LabelPlacementPanel">
    <Setter Property="Background" Value="Transparent" />
${plain.join('\n')}
${placement.join('\n')}
${blocks.join('\n')}
  </ControlTheme>`;
}

function checkmarkTheme() {
  const states = flagMask('$disabled', '$error', '$checked', '$isIndeterminate', '$isHovered', '$isActive', '$isFocusVisible');
  const by = {};
  by.Background = layers(states, (m) => brush(probe(checkbox.Checkmark, m).backgroundColor));
  by.BorderBrush = layers(states, (m) => brush(uniform(probe(checkbox.Checkmark, m), 'border', 'Color')));
  const glyphOf = (m) => glyph(probe(checkbox.Checkmark, m).backgroundImage);
  const shapes = {};
  by.Glyph = layers(states, (m) => {
    const g = glyphOf(m);
    if (!g) return '{x:Null}';
    shapes[g.d] = g;
    return g.d;
  });
  by.GlyphSize = layers(states, (m) => { const g = glyphOf(m); return g ? `${g.width},${g.height}` : undefined; });
  by.Foreground = layers(states, (m) => { const g = glyphOf(m); return g ? brush(g.fill) : undefined; });
  const ring = (m) => outline(probe(checkbox.Checkmark, m).outline);
  by.OutlineThickness = layers(states, (m) => String(ring(m)?.width ?? 0));
  by.OutlineBrush = layers(states, (m) => ring(m)?.brush);
  if (Object.keys(shapes).length !== 2) throw new Error('Expected two glyphs');

  const s = light(checkbox.Checkmark);
  same(s.boxSizing, 'border-box', 'Checkmark box-sizing');
  same(s.backgroundSize, 'contain', 'glyph size');
  same(s.backgroundPosition, 'center', 'glyph position');
  same(uniform(s, 'border', 'Style'), 'solid', 'border style');
  // Its radius is the theme's checkboxBorderRadius, by token.
  same(radii(s), L.borders.checkboxBorderRadius, 'Checkmark radius');
  same(s.transitionProperty, 'background-image, border-color, background-color', 'Checkmark transition');
  const { plain, blocks } = stateStyles(by);
  return `  <!-- Checkmark: the box, its tick or dash drawn as Base Web's background image (contained in the
       box inside its border, centred), and the outline keyboard focus gives a checked box. -->
  <ControlTheme x:Key="Checkbox.StyledCheckmark" TargetType="b:CheckboxMark">
${setter('Width', css.px(s.width))}
${setter('Height', css.px(s.height))}
${setter('Margin', thickness(s, 'margin'))}
${setter('BorderThickness', css.px(uniform(s, 'border', 'Width')))}
${setter('CornerRadius', '{DynamicResource borders.checkboxBorderRadius}')}
${plain.join('\n')}
    <!-- The colours change over ${s.transitionDuration}, once the box has loaded: CSS doesn't transition a first paint. -->
    <Style Selector="^:loaded">
      <Setter Property="Transitions">
        <Transitions>
          <BrushTransition Property="Background" Duration="${duration(s.transitionDuration)}" Easing="${easing(s.transitionTimingFunction)}" />
          <BrushTransition Property="BorderBrush" Duration="${duration(s.transitionDuration)}" Easing="${easing(s.transitionTimingFunction)}" />
        </Transitions>
      </Setter>
    </Style>
${blocks.join('\n')}
  </ControlTheme>`;
}

function toggleTheme() {
  const states = flagMask('$disabled', '$error', '$checked', '$isHovered', '$isFocusVisible');
  const by = {};
  by.Background = layers(states, (m) => brush(probe(checkbox.Toggle, m).backgroundColor));
  const sh = (m) => shadow(probe(checkbox.Toggle, m).boxShadow);
  by.BoxShadow = layers(states, (m) => sh(m).boxShadow);
  by.OutlineThickness = layers(states, (m) => String(sh(m).ring?.width ?? 0));
  by.OutlineBrush = layers(states, (m) => sh(m).ring?.brush);
  const s = light(checkbox.Toggle);
  const width = css.px(s.width);
  by.RenderTransform = layers(states, (m) => {
    const t = probe(checkbox.Toggle, m).transform;
    if (t == null) return 'translateX(0px)';
    const pct = /^translateX\((-?[\d.]+)%\)$/.exec(t);
    if (!pct) throw new Error(`transform ${t}`);
    return `translateX(${(Number(pct[1]) / 100) * width}px)`;
  });
  same(radii(s), '50%', 'Toggle radius');
  same(css.px(s.height), width, 'Toggle is round');
  const transition = /^transform (\S+)$/.exec(s.transition);
  if (!transition) throw new Error(`Toggle transition ${s.transition}`);
  const track = light(checkbox.ToggleTrack);
  same(track.alignItems, 'center', 'ToggleTrack alignItems');
  const { plain, blocks } = stateStyles(by);
  return `  <!-- Toggle: the knob, moved by its own width when checked. Its keyboard focus ring (Base Web's
       0 0 0 3px box-shadow) is drawn as an outline, which takes the theme's colour. Centred
       across the track and at its start: the track's alignItems: center. -->
  <ControlTheme x:Key="Checkbox.StyledToggle" TargetType="b:CheckboxMark">
${setter('Width', width)}
${setter('Height', width)}
${setter('CornerRadius', width / 2)}
${setter('HorizontalAlignment', 'Left')}
${setter('VerticalAlignment', 'Center')}
${plain.join('\n')}
    <!-- transform ${transition[1]}, CSS's default ease; once loaded. -->
    <Style Selector="^:loaded">
      <Setter Property="Transitions">
        <Transitions>
          <TransformOperationsTransition Property="RenderTransform" Duration="${duration(transition[1])}" Easing="${easing('ease')}" />
        </Transitions>
      </Setter>
    </Style>
${blocks.join('\n')}
  </ControlTheme>`;
}

function toggleTrackTheme() {
  const by = {};
  by.Background = layers(flagMask('$disabled'), (m) => brush(probe(checkbox.ToggleTrack, m).backgroundColor));
  const s = light(checkbox.ToggleTrack);
  same(s.flex, '0 0 auto', 'ToggleTrack flex');
  const { plain, blocks } = stateStyles(by);
  return `  <!-- ToggleTrack: the rail the toggle runs on. -->
  <ControlTheme x:Key="Checkbox.StyledToggleTrack" TargetType="Border">
${setter('Width', css.px(s.width))}
${setter('Height', css.px(s.height))}
${setter('Margin', thickness(s, 'margin'))}
${setter('CornerRadius', css.px(radii(s)))}
${plain.join('\n')}
${blocks.join('\n')}
  </ControlTheme>`;
}

function labelTheme() {
  const by = {};
  by.Foreground = layers(flagMask('$disabled'), (m) => brush(probe(checkbox.Label, m).color));
  const s = light(checkbox.Label);
  const font = L.typography.LabelMedium;
  for (const k of ['fontFamily', 'fontSize', 'fontWeight']) same(s[k], font[k], `Label ${k}`);
  const padding = PLACEMENTS.map((p) => {
    const t = thickness(light(checkbox.Label, { $labelPlacement: p }), 'padding');
    return `    <Style Selector="^:label-${p}">\n${nested('Padding', t)}\n    </Style>`;
  });
  const { plain, blocks } = stateStyles(by);
  return `  <!-- Label: LabelMedium on a ${css.px(s.lineHeight)} px line, padded on the side of the checkmark. -->
  <ControlTheme x:Key="Checkbox.StyledLabel" TargetType="ContentPresenter">
${setter('FontFamily', '{DynamicResource typography.LabelMedium.fontFamily}')}
${setter('FontSize', '{DynamicResource typography.LabelMedium.fontSize}')}
${setter('FontWeight', '{DynamicResource typography.LabelMedium.fontWeight}')}
${setter('LineHeight', css.px(s.lineHeight))}
${setter('TextWrapping', 'Wrap')}
${plain.join('\n')}
${padding.join('\n')}
${blocks.join('\n')}
  </ControlTheme>`;
}

function file(version) {
  return `<!--
  Generated by tools/tokens/gen.js (tools/tokens/checkbox.js) from baseui ${version}
  (© Uber Technologies, Inc., MIT). Do not edit.
  The themes of Checkbox's parts, Checkbox.Styled<Part>: each part's look by the state pseudoclasses
  the Checkbox mirrors onto it (Metered.Controls.Checkbox), the colours by token as Base Web's own
  style functions choose them, the lengths read from those functions.
-->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:b="https://github.com/wdcossey/Metered">
${rootTheme()}
${checkmarkTheme()}
${toggleTrackTheme()}
${toggleTheme()}
${labelTheme()}
</ResourceDictionary>
`;
}

module.exports = { file, glyph };
