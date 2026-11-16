// Writes src/Metered.Themes/Theming/ThemeDerivation.g.cs: what Base Web's createTheme (=
// createLightTheme) and createDarkTheme derive, read from Base Web's own functions rather than
// ported (ADR 0002). Three tables:
//
//  - The colour tokens that follow a foundation colour (themes/utils.js's getFoundationColorOverrides:
//    primaryA, primaryB, primary, accent, negative, warning, positive), found by calling
//    createLightTheme and createDarkTheme with sentinel foundations and seeing which tokens change
//    and how: to the foundation itself, or to it at an alpha (hexToRgb), with what Base Web falls
//    back to when the foundation isn't a hex colour. Every other colour is the theme's own constant.
//  - What each of the Button's box-shadows (button-shadows.js) is made of: which colour, lighting and
//    sizing tokens, found by evaluating Base Web's Button style function on a theme whose every
//    colour, lighting and sizing value is a sentinel.
//  - The paths that are one object in Base Web's theme (typography.font350 is LabelMedium), which
//    createTheme's deep merge changes together.
//
// Strict, like css.js: a token that changes in a way this file doesn't recognise throws, and both
// tables are checked against Base Web's own LightTheme and DarkTheme before anything is written.
// Names only: no colour is written (rule 5); the constants come from Tokens/*.g.axaml at run time.
'use strict';

const { isDeepStrictEqual } = require('util');
const css = require('../reference/css');
const themes = require('baseui/themes');
const { getFoundationColorOverrides } = require('baseui/themes/utils');
const { hexToRgb } = require('baseui/styles/util');
const shadows = require('./button-shadows');

const CREATE = { Light: themes.createLightTheme, Dark: themes.createDarkTheme };
const BASE = { Light: themes.LightTheme, Dark: themes.DarkTheme };
const hex2 = (n) => n.toString(16).toUpperCase().padStart(2, '0');

// The foundation: what getFoundationColorOverrides keeps of a colors object that has every token.
const FOUNDATION = Object.keys(getFoundationColorOverrides(
  Object.fromEntries(Object.keys(themes.LightTheme.colors).map((k) => [k, '#000000']))));

// createX({}) must be the theme Metered's dictionaries are generated from, or the constants read
// from them at run time would be the wrong ones.
for (const v of Object.keys(CREATE)) {
  if (!isDeepStrictEqual(CREATE[v]({}), BASE[v])) throw new Error(`create${v}Theme({}) isn't ${v}Theme`);
}

/** Two sentinel foundations: a distinct hex colour for each key, different in each run. */
const foundationSentinels = (run) => Object.fromEntries(FOUNDATION.map((f, i) => [f, '#' + hex2(run) + hex2(i + 1) + '5A']));

function colourRules(variant) {
  const create = CREATE[variant];
  const base = create({}).colors;
  const runs = [1, 2].map((run) => {
    const s = foundationSentinels(run);
    return { s, colors: create({ colors: s }).colors };
  });
  // Every foundation not a hex colour: what Base Web falls back to where it takes an alpha.
  const nonHex = create({ colors: Object.fromEntries(FOUNDATION.map((f) => [f, 'rgb(1, 2, 3)'])) }).colors;

  const rules = [];
  for (const k of Object.keys(base)) {
    if (runs.every((r) => r.colors[k] === base[k])) continue; // a constant
    const alias = FOUNDATION.find((f) => runs.every((r) => r.colors[k] === r.s[f]));
    if (alias) { rules.push({ key: k, from: alias }); continue; }
    let rule = null;
    for (const f of FOUNDATION) {
      const ms = runs.map((r) => /^rgba\(\d+, \d+, \d+, ([\d.]+)\)$/.exec(r.colors[k]));
      if (!ms.every((m, i) => m && m[1] === ms[0][1] && hexToRgb(runs[i].s[f], m[1]) === runs[i].colors[k])) continue;
      const alpha = ms[0][1];
      let fallback;
      if (nonHex[k] === '') fallback = 'None';
      else if (nonHex[k] === hexToRgb(base[f], alpha)) fallback = 'Default';
      else throw new Error(`${variant} ${k}: falls back to ${JSON.stringify(nonHex[k])}, not a rule this file knows`);
      rule = { key: k, from: f, alpha, fallback };
      break;
    }
    if (!rule) throw new Error(`${variant} ${k}: derived as ${JSON.stringify(runs[0].colors[k])}, not a rule this file knows`);
    rules.push(rule);
  }
  return rules;
}

/** Applies colour rules to a foundation, as the C# builder does: for the self-check. */
function applyColourRules(variant, rules, colors) {
  const base = BASE[variant].colors;
  const out = { ...base };
  for (const r of rules) {
    if (!r.alpha) out[r.key] = colors[r.from];
    else out[r.key] = hexToRgb(colors[r.from], r.alpha) ?? (r.fallback === 'Default' ? hexToRgb(base[r.from], r.alpha) : '');
  }
  return out;
}

// --- The Button's box-shadows -------------------------------------------------------------------

const colourKeys = Object.keys(themes.LightTheme.colors).filter((k) => !/RampUnit$/.test(k));
const lightingKeys = Object.keys(themes.LightTheme.lighting);
const sizingKeys = Object.keys(themes.LightTheme.sizing);
const colourSentinel = (run, i) => '#' + hex2(0x10 + run) + hex2(i >> 8) + hex2(i & 255);
const lightingSentinel = (run, j) => '#' + hex2(0x30 + run) + '00' + hex2(j);
const sizingSentinel = (run, j) => 100000 + run * 1000 + j;

// One per variant and run, built once, frozen whole: Base Web's style function only reads it.
const deepFreeze = (o) => { Object.values(o).forEach((v) => { if (v && typeof v === 'object') deepFreeze(v); }); return Object.freeze(o); };
const sentinelThemes = new Map();
function sentinelTheme(variant, run) {
  const id = `${variant} ${run}`;
  if (!sentinelThemes.has(id)) sentinelThemes.set(id, deepFreeze(buildSentinelTheme(variant, run)));
  return sentinelThemes.get(id);
}

function buildSentinelTheme(variant, run) {
  const t = structuredClone(BASE[variant]);
  colourKeys.forEach((k, i) => { t.colors[k] = colourSentinel(run, i); });
  lightingKeys.forEach((k, j) => { t.lighting[k] = `0px 0px 0px 0px ${lightingSentinel(run, j)}`; });
  sizingKeys.forEach((k, j) => { t.sizing[k] = `${sizingSentinel(run, j)}px`; });
  return t;
}

/** "inset 0 0 0 2 #FF000000" to its parts. */
function parseTerm(term) {
  const m = /^(inset )?(-?[\d.]+) (-?[\d.]+) (-?[\d.]+) (-?[\d.]+) (#[0-9A-F]{8})$/.exec(term);
  if (!m) throw new Error(`Not a shadow: ${term}`);
  return { inset: !!m[1], lengths: m.slice(2, 6).map(Number), colour: m[6] };
}

function shadowTemplate(variant, kind, state, safe) {
  const strings = [1, 2].map((run) => shadows.buttonShadow(sentinelTheme(variant, run), kind, state, safe));
  const lists = strings.map((s) => (s === 'none' ? [] : css.splitTop(s).map(parseTerm)));
  if (lists[0].length !== lists[1].length) throw new Error(`${kind} ${state}: the shadow's length depends on the theme`);
  return lists[0].map((a, n) => {
    const b = lists[1][n];
    const lj = lightingKeys.findIndex((_, j) => a.colour === css.color(lightingSentinel(1, j)) && b.colour === css.color(lightingSentinel(2, j)));
    if (lj >= 0) {
      if (a.inset || a.lengths.some((x) => x !== 0)) throw new Error(`${kind} ${state}: a lighting token, changed`);
      return { lighting: `lighting.${lightingKeys[lj]}` };
    }
    if (a.inset !== b.inset) throw new Error(`${kind} ${state}: inset depends on the theme`);
    const lengths = a.lengths.map((x, i) => {
      if (x === b.lengths[i]) return x;
      const j = sizingKeys.findIndex((_, jj) => x === sizingSentinel(1, jj) && b.lengths[i] === sizingSentinel(2, jj));
      if (j < 0) throw new Error(`${kind} ${state}: a length derived as ${x}, not a token`);
      return `sizing.${sizingKeys[j]}`;
    });
    const ci = colourKeys.findIndex((_, i) => a.colour === css.color(colourSentinel(1, i)) && b.colour === css.color(colourSentinel(2, i)));
    if (ci < 0) throw new Error(`${kind} ${state}: the colour ${a.colour} isn't a token`);
    return { inset: a.inset, lengths, colour: `colors.${colourKeys[ci]}` };
  });
}

/** A template filled from a Base Web theme, in gen.js's syntax: for the self-check. */
function fillTemplate(terms, theme) {
  const parts = terms.map((t) => {
    if (t.lighting) return css.boxShadows(theme.lighting[t.lighting.slice('lighting.'.length)]);
    const lengths = t.lengths.map((x) => (typeof x === 'number' ? x : css.px(theme.sizing[x.slice('sizing.'.length)])));
    return (t.inset ? 'inset ' : '') + lengths.join(' ') + ' ' + css.color(theme.colors[t.colour.slice('colors.'.length)]);
  }).filter((p) => p !== 'none');
  return parts.length ? parts.join(', ') : 'none';
}

// --- Shared objects ----------------------------------------------------------------------------

// The groups Metered's dictionaries hold, keyed as `<group>.<path>` (gen.js).
const GROUPS = ['colors', 'animation', 'borders', 'lighting', 'sizing', 'typography'];

/**
 * The objects a theme reaches by more than one path (typography.font350 is typography.LabelMedium).
 * createTheme deep-merges the overrides into a structuredClone of the theme, which keeps them one
 * object: overriding a leaf under one path changes it under every other.
 */
function sharedObjects(theme) {
  const seen = new Map();
  const walk = (o, path) => {
    for (const [k, v] of Object.entries(o)) {
      if (v === null || typeof v !== 'object') continue;
      const p = [...path, k];
      if (seen.has(v)) seen.get(v).push(p.join('.'));
      else { seen.set(v, [p.join('.')]); walk(v, p); }
    }
  };
  for (const g of GROUPS) walk(theme[g], [g]);
  return [...seen.values()].filter((paths) => paths.length > 1);
}

function derive() {
  const shared = sharedObjects(BASE.Light);
  if (!isDeepStrictEqual(shared, sharedObjects(BASE.Dark))) throw new Error('The themes share other objects');

  const colours = {};
  for (const v of Object.keys(CREATE)) {
    colours[v] = colourRules(v);
    // Self-check: the rules, applied to Base Web's own sentinel run, give what createX gives.
    const s = foundationSentinels(3);
    const want = CREATE[v]({ colors: s }).colors;
    const got = applyColourRules(v, colours[v], { ...BASE[v].colors, ...s });
    for (const k of Object.keys(want)) if (want[k] !== got[k]) throw new Error(`${v} ${k}: the rules give ${got[k]}, Base Web ${want[k]}`);
  }

  const shadowRules = shadows.ALL.map(([kind, state, safe]) => {
    const terms = shadowTemplate('Light', kind, state, safe);
    if (!isDeepStrictEqual(terms, shadowTemplate('Dark', kind, state, safe))) {
      throw new Error(`${kind} ${state}: Base Web's Button reads other tokens in the dark theme`);
    }
    for (const v of Object.keys(BASE)) {
      const want = shadows.buttonShadow(BASE[v], kind, state, safe);
      const got = fillTemplate(terms, BASE[v]);
      if (want !== got) throw new Error(`${v} ${kind} ${state}: the template gives ${got}, Base Web ${want}`);
    }
    return { key: shadows.key(kind, state, safe), terms };
  });
  return { foundation: FOUNDATION, colours, shadows: shadowRules, shared };
}

const q = (s) => JSON.stringify(s);
const len = (x) => (typeof x === 'number' ? String(x) : q(x));

function file(version) {
  const d = derive();
  const colourTable = (rules) => rules.map((r) => (r.alpha
    ? `        new(${q('colors.' + r.key)}, ${q(r.from)}, ${r.alpha}, AlphaFallback.${r.fallback}),`
    : `        new(${q('colors.' + r.key)}, ${q(r.from)}),`)).join('\n');
  const term = (t) => (t.lighting
    ? `ShadowTerm.Lighting(${q(t.lighting)})`
    : `new(${t.inset ? 'true' : 'false'}, ${t.lengths.map(len).join(', ')}, ${q(t.colour)})`);
  const shadowTable = d.shadows.map((s) => `        new(${q(s.key)}, [${s.terms.map(term).join(', ')}]),`).join('\n');
  return `// <auto-generated />
// Generated by tools/tokens/theme.js from baseui ${version} (© Uber Technologies, Inc., MIT). Do not edit.
// What Base Web's createTheme and createDarkTheme derive, read from Base Web's own functions (ADR 0002).

namespace Metered.Themes;

internal static partial class ThemeDerivation
{
    /// <summary>The foundation colours: the keys of <c>overrides.colors</c> the semantic and component colours derive from (themes/utils.js).</summary>
    internal static readonly string[] FoundationColors = [${d.foundation.map(q).join(', ')}];

    /// <summary>createTheme (createLightTheme): each colour token that follows a foundation colour. Every other colour is LightTheme's.</summary>
    internal static readonly ColourRule[] Light =
    [
${colourTable(d.colours.Light)}
    ];

    /// <summary>createDarkTheme: each colour token that follows a foundation colour. Every other colour is DarkTheme's.</summary>
    internal static readonly ColourRule[] Dark =
    [
${colourTable(d.colours.Dark)}
    ];

    /// <summary>What each of the Button's box-shadows is made of, in either theme (Base Web's BaseButton style function).</summary>
    internal static readonly ShadowRule[] Shadows =
    [
${shadowTable}
    ];

    /// <summary>
    /// The paths that are one object in either theme: an override of a token under one is an override of
    /// the same token under the others, as createTheme's deepMerge into one structuredClone makes it.
    /// </summary>
    internal static readonly string[][] SharedObjects =
    [
${d.shared.map((paths) => `        [${paths.map(q).join(', ')}],`).join('\n')}
    ];
}
`;
}

module.exports = { file, derive, FOUNDATION };
