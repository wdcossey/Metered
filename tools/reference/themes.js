// The oracle for Metered.Themes.Theme: Base Web's own createTheme and createDarkTheme, run on a few
// sets of foundation colours and overrides, and every token they give in Metered's keys and
// Avalonia's units. Written by cases.js into tests/Metered.Tests/Reference/theme.oracle.json.
//
// Per case: the overrides, flattened to Metered's keys; every colour token (null where Base Web gives
// no colour, ''); every Button box-shadow, from Base Web's Button style function on the built theme
// (tools/tokens/button-shadows.js); the leaves of the other groups that differ from the theme
// createTheme starts from.
'use strict';

const { isDeepStrictEqual } = require('util');
const css = require('./css');
const themes = require('baseui/themes');
const shadows = require('../tokens/button-shadows');

const CREATE = { createTheme: themes.createTheme, createLightTheme: themes.createLightTheme, createDarkTheme: themes.createDarkTheme };
const BASE = { createTheme: themes.LightTheme, createLightTheme: themes.LightTheme, createDarkTheme: themes.DarkTheme };

// The groups Metered's dictionaries hold besides colours (tools/tokens/gen.js).
const GROUPS = ['animation', 'borders', 'lighting', 'sizing', 'typography'];

const CASES = [
  { name: 'light', create: 'createTheme', overrides: {} },
  { name: 'dark', create: 'createDarkTheme', overrides: {} },
  {
    name: 'lightFoundation', create: 'createTheme',
    overrides: { colors: { primaryA: '#1F2A44', primaryB: '#FFFDF7', primary: '#1F2A44', accent: '#7356BF', negative: '#C62828', warning: '#F9A825', positive: '#2E7D32' } },
  },
  {
    name: 'darkFoundation', create: 'createDarkTheme',
    overrides: { colors: { primaryA: '#F5F1E8', primaryB: '#121826', primary: '#F5F1E8', accent: '#9F8AE0', negative: '#FF6B5B', warning: '#FFD166', positive: '#3CCB7F' } },
  },
  // A shorthand hex, as hexToRgb reads it.
  { name: 'lightShorthand', create: 'createLightTheme', overrides: { colors: { primaryA: '#123', accent: '#0A8' } } },
  // Foundation, semantic and component colours side by side, and the other groups: a semantic or
  // component colour given directly isn't derived from again.
  {
    name: 'lightOverrides', create: 'createTheme',
    overrides: {
      colors: { accent: '#00A86B', backgroundPrimary: '#FAFAFA', buttonPrimaryFill: '#0B3D2E', buttonOuterBorder: '#7356BF', buttonPrimaryHoverOverlay: 'rgba(255, 255, 255, 0.3)' },
      sizing: { scale0: '3px', scale600: '20px', scale1200: '56px' },
      lighting: { shallowBelow: '0px 2px 6px rgba(0, 0, 0, 0.3)' },
      borders: { radius300: '12px' },
      typography: { LabelMedium: { fontSize: '18px' } },
    },
  },
  // A foundation colour with an alpha isn't a hex colour to hexToRgb: the light theme falls back to
  // its own default.
  { name: 'lightTranslucent', create: 'createTheme', overrides: { colors: { primaryA: 'rgba(16, 24, 32, 0.5)', primaryB: '#FFFDF780' } } },
  // The dark theme gives no colour at all ('').
  { name: 'darkTranslucent', create: 'createDarkTheme', overrides: { colors: { primaryB: '#12182680' } } },
];

/** A leaf of a theme group to Metered's key, its Avalonia type and value (as tools/tokens/gen.js writes it). */
function leaf(group, path, value) {
  const key = [group, ...path].join('.');
  const last = path[path.length - 1];
  switch (group) {
    case 'colors':
      return /RampUnit$/.test(last) ? { key, type: 'string', value: String(value) } : { key, type: 'colour', value: value === '' ? null : css.color(value) };
    case 'sizing':
      return { key, type: 'double', value: css.px(value) };
    case 'lighting':
      return { key, type: 'boxShadows', value: css.boxShadows(value) };
    case 'borders':
      if (typeof value === 'boolean') return { key, type: 'bool', value };
      if (path.length === 1) return { key, type: 'cornerRadius', value: css.px(value) };
      if (last === 'borderWidth') return { key, type: 'thickness', value: css.px(value) };
      if (last === 'borderColor') return { key, type: 'colour', value: css.color(value) };
      break;
    case 'typography':
      if (path.length === 1) return { key, type: 'fontFamily', value: css.fontFamily(value) };
      if (last === 'fontSize' || last === 'lineHeight') return { key, type: 'double', value: css.px(value) };
      if (last === 'fontWeight') return { key, type: 'fontWeight', value: css.fontWeight(value) };
      if (last === 'fontFamily') return { key, type: 'fontFamily', value: css.fontFamily(value) };
      break;
    default:
  }
  throw new Error(`No Metered token for ${key}`);
}

/** Every leaf of an object, as [path, value]. */
function leaves(object, path = []) {
  return Object.entries(object).flatMap(([k, v]) => (v !== null && typeof v === 'object' ? leaves(v, [...path, k]) : [[[...path, k], v]]));
}

function oracle(version) {
  const cases = CASES.map(({ name, create, overrides }) => {
    const theme = CREATE[create](structuredClone(overrides));
    const base = BASE[create];
    const flatOverrides = Object.entries(overrides).flatMap(([group, v]) => leaves(v).map(([p, x]) => leaf(group, p, x)));
    const colours = Object.fromEntries(Object.entries(theme.colors).map(([k, v]) => [`colors.${k}`, leaf('colors', [k], v).value]));
    const boxShadows = Object.fromEntries(shadows.ALL.map(([kind, s, safe]) => [shadows.key(kind, s, safe), shadows.buttonShadow(theme, kind, s, safe)]));
    const changed = GROUPS.filter((g) => !isDeepStrictEqual(theme[g], base[g])).flatMap((g) => {
      const was = new Map(leaves(base[g]).map(([p, v]) => [p.join('.'), v]));
      return leaves(theme[g]).filter(([p, v]) => was.get(p.join('.')) !== v).map(([p, v]) => leaf(g, p, v));
    });
    return { name, create, themeName: theme.name, overrides: flatOverrides, colours, boxShadows, changed };
  });
  return { baseui: version, cases };
}

module.exports = { oracle };
