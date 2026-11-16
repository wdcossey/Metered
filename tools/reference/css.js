// CSS values as Base Web's theme and style functions write them, converted to what Avalonia's
// XAML parses. Strict: a value this file doesn't recognise throws, so a new format in a later
// baseui fails the generator instead of writing something wrong.
'use strict';

const hex2 = (n) => Math.round(n).toString(16).toUpperCase().padStart(2, '0');

/** A CSS colour to Avalonia's #AARRGGBB. */
function color(value) {
  const v = String(value).trim();
  if (v === 'transparent') return '#00000000';
  let m = /^#([0-9a-f]{3})$/i.exec(v);
  if (m) return '#FF' + [...m[1]].map((c) => c + c).join('').toUpperCase();
  m = /^#([0-9a-f]{6})$/i.exec(v);
  if (m) return '#FF' + m[1].toUpperCase();
  m = /^#([0-9a-f]{6})([0-9a-f]{2})$/i.exec(v);
  if (m) return '#' + m[2].toUpperCase() + m[1].toUpperCase();
  m = /^rgba?\(\s*([\d.]+)\s*,\s*([\d.]+)\s*,\s*([\d.]+)\s*(?:,\s*([\d.]+)\s*)?\)$/i.exec(v);
  if (m) {
    const a = m[4] === undefined ? 1 : Number(m[4]);
    return '#' + hex2(a * 255) + hex2(m[1]) + hex2(m[2]) + hex2(m[3]);
  }
  m = /^hsla?\(\s*([\d.]+)\s*,\s*([\d.]+)%\s*,\s*([\d.]+)%\s*(?:,\s*([\d.]+)\s*)?\)$/i.exec(v);
  if (m) {
    const [r, g, b] = hslToRgb(Number(m[1]), Number(m[2]) / 100, Number(m[3]) / 100);
    const a = m[4] === undefined ? 1 : Number(m[4]);
    return '#' + hex2(a * 255) + hex2(r) + hex2(g) + hex2(b);
  }
  throw new Error(`Not a colour this generator reads: ${JSON.stringify(value)}`);
}

function hslToRgb(h, s, l) {
  const k = (n) => (n + h / 30) % 12;
  const a = s * Math.min(l, 1 - l);
  const f = (n) => l - a * Math.max(-1, Math.min(k(n) - 3, Math.min(9 - k(n), 1)));
  return [f(0) * 255, f(8) * 255, f(4) * 255];
}

/** '14px' or 0 to a number of device-independent pixels. */
function px(value) {
  if (typeof value === 'number') return value;
  const m = /^(-?[\d.]+)(px)?$/.exec(String(value).trim());
  if (!m) throw new Error(`Not a pixel length: ${JSON.stringify(value)}`);
  return Number(m[1]);
}

/** '200ms' or '0' to seconds. */
function seconds(value) {
  const m = /^([\d.]+)(ms|s)?$/.exec(String(value).trim());
  if (!m) throw new Error(`Not a time: ${JSON.stringify(value)}`);
  return m[2] === 's' ? Number(m[1]) : Number(m[1]) / 1000;
}

/** 'cubic-bezier(a, b, c, d)' to [a, b, c, d]. */
function cubicBezier(value) {
  const m = /^cubic-bezier\(\s*([\d.]+)\s*,\s*([\d.]+)\s*,\s*([\d.]+)\s*,\s*([\d.]+)\s*\)$/.exec(String(value).trim());
  if (!m) throw new Error(`Not a cubic-bezier: ${JSON.stringify(value)}`);
  return m.slice(1).map(Number);
}

/** Splits on commas outside parentheses. */
function splitTop(value) {
  const parts = [];
  let depth = 0, start = 0;
  for (let i = 0; i < value.length; i++) {
    if (value[i] === '(') depth++;
    else if (value[i] === ')') depth--;
    else if (value[i] === ',' && depth === 0) { parts.push(value.slice(start, i)); start = i + 1; }
  }
  parts.push(value.slice(start));
  return parts.map((p) => p.trim()).filter((p) => p.length > 0);
}

/**
 * A CSS box-shadow to Avalonia's BoxShadows syntax ("[inset] x y blur spread #AARRGGBB", comma
 * separated), or 'none'. CSS lets the colour come first or last; Avalonia wants it last.
 */
function boxShadows(value) {
  const v = String(value).trim();
  if (v === 'none' || v === '') return 'none';
  return splitTop(v).map((one) => {
    const tokens = one.match(/rgba?\([^)]*\)|hsla?\([^)]*\)|\S+/g);
    let inset = false;
    let col = null;
    const lengths = [];
    for (const t of tokens) {
      if (t === 'inset') inset = true;
      else if (/^-?[\d.]+(px)?$/.test(t)) lengths.push(px(t));
      else col = color(t);
    }
    if (col === null || lengths.length < 2 || lengths.length > 4) {
      throw new Error(`Not a box-shadow this generator reads: ${JSON.stringify(one)}`);
    }
    while (lengths.length < 4) lengths.push(0);
    return (inset ? 'inset ' : '') + lengths.join(' ') + ' ' + col;
  }).join(', ');
}

/** CSS font-weight to Avalonia's FontWeight name. */
function fontWeight(value) {
  const names = { 100: 'Thin', 200: 'ExtraLight', 300: 'Light', 400: 'Normal', 500: 'Medium', 600: 'SemiBold', 700: 'Bold', 800: 'ExtraBold', 900: 'Black' };
  if (value === 'normal') return 'Normal';
  if (value === 'bold') return 'Bold';
  const name = names[Number(value)];
  if (!name) throw new Error(`Not a font weight: ${JSON.stringify(value)}`);
  return name;
}

/** A CSS font-family list to Avalonia's (comma separated, unquoted). */
function fontFamily(value) {
  return splitTop(String(value)).map((f) => f.replace(/^["']|["']$/g, '')).join(', ');
}

module.exports = { color, px, seconds, cubicBezier, boxShadows, fontWeight, fontFamily, splitTop };
