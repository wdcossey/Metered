// Chooses the OFL substitute for Uber Move by measurement. Uber Move is measured as baseweb.design
// serves it, and each candidate in the same page, so both sides go through one engine (headless
// Chromium, Playwright): advance widths over representative strings, glyph heights, the engine's
// line metrics, and where the baseline sits in the line box at each size of Base Web's ramp. For
// the candidates it also reads the vertical metrics Avalonia uses (GlyphTypeface: OS/2 typo when
// USE_TYPO_METRICS is set, else hhea) and predicts Avalonia's baseline from them.
//
//   node tools/reference/fonts.js           print the tables (Markdown)
//   node tools/reference/fonts.js --write   also write tools/reference/fonts.g.json (generated:
//                                            never hand-edit it; run this again)
//
// Needs network access and a Chromium for playwright-core (`npx playwright-core install chromium`).
// The incumbent, Inter, is read out of Avalonia.Fonts.Inter at the pinned Avalonia version.
//
// Uber Move is proprietary and is only rendered. The page loads it through baseweb.design's own
// @font-face rules, in an off-the-record browser context (its HTTP cache lives in memory) with the
// cache disabled besides; only measurements of laid-out text leave the page. Nothing of Uber Move
// is written to disk, here or anywhere. The candidates are fetched at pinned versions into memory
// and handed to the page as FontFace data; they aren't written to disk either.
'use strict';

const fs = require('fs');
const os = require('os');
const path = require('path');
const zlib = require('zlib');
const crypto = require('crypto');
const { chromium } = require('playwright-core');
const typography = require('baseui/themes/shared/typography').default;
const { fontTokens } = require('baseui/themes/shared/typography');

const root = path.resolve(__dirname, '../..');
const outFile = path.join(__dirname, 'fonts.g.json');
const write = process.argv.includes('--write');

const PAGE = 'https://baseweb.design/components/button/';
// Static instances per weight (Latin subset, WOFF2), as Google Fonts' API cuts them from the
// family's own font, published on npm by Fontsource; fetched from jsDelivr at a pinned version.
const FONTSOURCE = 'https://cdn.jsdelivr.net/npm/@fontsource';
// The licence of each family, read from google/fonts at a pinned commit (METADATA.pb, OFL.txt).
const GOOGLE_FONTS = 'https://raw.githubusercontent.com/google/fonts/7af0d90b4d532ffa4b041937b560c6df4e7b1e95/ofl';

const WEIGHTS = [400, 500, 600, 700];

// The reference faces. Base Web's ramp asks UberMoveText at 400 ('normal') and 500, and UberMove at
// 700 (shared/typography.js; move-theme/typography.js changes only the families). baseweb.design
// registers UberMove's Bold file at 600, so 700 draws it (face: the weight of the rule it matches).
const REFERENCE = [
  { role: 'text400', family: 'UberMoveText', weight: 400, ramp: true },
  { role: 'text500', family: 'UberMoveText', weight: 500, ramp: true },
  { role: 'display700', family: 'UberMove', weight: 700, face: 600, ramp: true },
  // Not in the ramp; measured to describe the two cuts and the weight mapping.
  { role: 'text600', family: 'UberMoveText', weight: 600 },
  { role: 'display600', family: 'UberMove', weight: 600 },
  { role: 'display400', family: 'UberMove', weight: 400 },
  { role: 'display500', family: 'UberMove', weight: 500 },
];
const ROLES = ['text400', 'text500', 'display700'];

// id: the Fontsource package; gf: the google/fonts directory. The reasons are in docs/decisions/0003-font.md.
const CANDIDATES = [
  { id: 'inter-avalonia', name: 'Inter 3 (Avalonia.Fonts.Inter)', source: 'avalonia' },
  { id: 'inter', name: 'Inter 4', gf: 'inter' },
  { id: 'inter-tight', name: 'Inter Tight', gf: 'intertight' },
  { id: 'geist', name: 'Geist', gf: 'geist' },
  { id: 'public-sans', name: 'Public Sans', gf: 'publicsans' },
  { id: 'ibm-plex-sans', name: 'IBM Plex Sans', gf: 'ibmplexsans' },
  { id: 'roboto', name: 'Roboto', gf: 'roboto' },
  { id: 'dm-sans', name: 'DM Sans', gf: 'dmsans' },
  { id: 'figtree', name: 'Figtree', gf: 'figtree' },
  { id: 'manrope', name: 'Manrope', gf: 'manrope' },
  { id: 'plus-jakarta-sans', name: 'Plus Jakarta Sans', gf: 'plusjakartasans' },
  { id: 'instrument-sans', name: 'Instrument Sans', gf: 'instrumentsans' },
  { id: 'hanken-grotesk', name: 'Hanken Grotesk', gf: 'hankengrotesk' },
  { id: 'albert-sans', name: 'Albert Sans', gf: 'albertsans' },
  { id: 'schibsted-grotesk', name: 'Schibsted Grotesk', gf: 'schibstedgrotesk' },
  { id: 'work-sans', name: 'Work Sans', gf: 'worksans' },
  { id: 'archivo', name: 'Archivo', gf: 'archivo' },
  { id: 'be-vietnam-pro', name: 'Be Vietnam Pro', gf: 'bevietnampro' },
  { id: 'onest', name: 'Onest', gf: 'onest', version: '5.3.1' },
  { id: 'golos-text', name: 'Golos Text', gf: 'golostext' },
  { id: 'mona-sans', name: 'Mona Sans', gf: 'monasans' },
  { id: 'libre-franklin', name: 'Libre Franklin', gf: 'librefranklin' },
  { id: 'karla', name: 'Karla', gf: 'karla' },
  { id: 'outfit', name: 'Outfit', gf: 'outfit' },
  { id: 'rethink-sans', name: 'Rethink Sans', gf: 'rethinksans' },
  { id: 'host-grotesk', name: 'Host Grotesk', gf: 'hostgrotesk' },
  // Families cut as a text and a display pair, as Uber Move is.
  { id: 'red-hat-text', name: 'Red Hat Text', gf: 'redhattext' },
  { id: 'red-hat-display', name: 'Red Hat Display', gf: 'redhatdisplay' },
  { id: 'wix-madefor-text', name: 'Wix Madefor Text', gf: 'wixmadefortext' },
  { id: 'wix-madefor-display', name: 'Wix Madefor Display', gf: 'wixmadefordisplay' },
  { id: 'funnel-sans', name: 'Funnel Sans', gf: 'funnelsans' },
  { id: 'funnel-display', name: 'Funnel Display', gf: 'funneldisplay' },
  // The static TTFs of the leaders' own repositories, at a pinned commit: the files an app would
  // embed (Avalonia reads neither WOFF2 nor a variable font's axes), and Instrument Sans's
  // narrower static widths, for the display cut.
  ...['', 'SemiCondensed', 'Condensed'].map(w => ({
    id: `instrument-sans${w ? '-' + w.toLowerCase() : ''}-ttf`, name: `Instrument Sans${w ? ' ' + w : ''} (static TTF)`,
    repo: 'Instrument/instrument-sans', commit: '7fa22308a3d0c94ee2b3cd537a1196b65db34a3e',
    file: x => `fonts/ttf/InstrumentSans${w}-${x}.ttf`,
  })),
  {
    id: 'figtree-ttf', name: 'Figtree (static TTF)',
    repo: 'erikdkennedy/figtree', commit: '032dfa7fe219ef3a02890d6d3add84eacc9aebfe',
    file: x => `fonts/ttf/Figtree-${x}.ttf`,
  },
];
const FONTSOURCE_VERSION = '5.3.0';
const STYLE_NAMES = { 400: 'Regular', 500: 'Medium', 600: 'SemiBold', 700: 'Bold' };

// Local fonts, for context only (not candidates): what Base Web's own fallback stack draws here.
const LOCAL = [{ id: 'system-ui', name: 'system-ui (fallback)', css: 'system-ui' }, { id: 'arial', name: 'Arial (fallback)', css: 'Arial' }];

const STRINGS = {
  labels: ['Button', 'Primary', 'Secondary', 'Tertiary', 'Start Enhancer', 'End Enhancer', 'Selected', 'Cancel', 'Continue', 'Learn more', 'Sign up', 'Get started'],
  sentences: ['The quick brown fox jumps over the lazy dog.', 'Pick up your order at the door in 5 minutes.', 'Your account settings, payment methods and saved places.'],
  digits: ['0123456789', '$1,234.56', '12:45 PM'],
  capitals: ['THE QUICK BROWN FOX JUMPS OVER THE LAZY DOG', 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'],
  lowercase: ['abcdefghijklmnopqrstuvwxyz'],
};
const ALL = Object.values(STRINGS).flat();
const AVG = ['abcdefghijklmnopqrstuvwxyz', 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', '0123456789'];

// The ramp: font100 to font1450, each with its role.
const RAMP = Object.entries(typography).filter(([k]) => /^font\d+$/.test(k)).map(([name, t]) => {
  const weight = t.fontWeight === 'normal' ? 400 : Number(t.fontWeight);
  const role = t.fontFamily === fontTokens.secondaryFontFamily ? `display${weight}` : `text${weight}`;
  return { name, role, size: parseFloat(t.fontSize), lineHeight: parseFloat(t.lineHeight) };
});
for (const r of RAMP) if (!ROLES.includes(r.role)) throw new Error(`ramp role not measured: ${r.name} ${r.role}`);

// The Button's label: LabelXSmall to LabelLarge by size (baseui/button/styled-components.js), all
// UberMoveText 500, measured at their own pixel sizes too.
const LABEL_STYLES = ['LabelXSmall', 'LabelSmall', 'LabelMedium', 'LabelLarge'];
const LABEL_SIZES = LABEL_STYLES.map(n => parseFloat(typography[n].fontSize));
for (const n of LABEL_STYLES) if (Number(typography[n].fontWeight) !== 500) throw new Error(`${n} is not 500`);
const labelsAt = weight => (weight === 500 ? { labels: STRINGS.labels, labelSizes: LABEL_SIZES } : {});

// ---- font files as data -------------------------------------------------------------------

const WOFF2_TAGS = ['cmap', 'head', 'hhea', 'hmtx', 'maxp', 'name', 'OS/2', 'post', 'cvt ', 'fpgm', 'glyf', 'loca', 'prep', 'CFF ', 'VORG', 'EBDT', 'EBLC', 'gasp', 'hdmx', 'kern', 'LTSH', 'PCLT', 'VDMX', 'vhea', 'vmtx', 'BASE', 'GDEF', 'GPOS', 'GSUB', 'EBSC', 'JSTF', 'MATH', 'CBDT', 'CBLC', 'COLR', 'CPAL', 'SVG ', 'sbix', 'acnt', 'avar', 'bdat', 'bloc', 'bsln', 'cvar', 'fdsc', 'feat', 'fmtx', 'fvar', 'gvar', 'hsty', 'just', 'lcar', 'mort', 'morx', 'opbd', 'prop', 'trak', 'Zapf', 'Silf', 'Glat', 'Gloc', 'Feat', 'Sill'];

function tablesOf(buf) {
  const sig = buf.toString('latin1', 0, 4);
  const tables = {};
  if (sig === 'wOF2') {
    const numTables = buf.readUInt16BE(12);
    const compressed = buf.readUInt32BE(20);
    let p = 48;
    const base128 = () => {
      let v = 0;
      for (let i = 0; i < 5; i++) { const b = buf[p++]; v = v * 128 + (b & 0x7f); if (!(b & 0x80)) return v; }
      throw new Error('bad UIntBase128');
    };
    const dir = [];
    for (let i = 0; i < numTables; i++) {
      const flags = buf[p++];
      let tag = WOFF2_TAGS[flags & 0x3f];
      if ((flags & 0x3f) === 0x3f) { tag = buf.toString('latin1', p, p + 4); p += 4; }
      const version = flags >> 6;
      const orig = base128();
      const transformed = tag === 'glyf' || tag === 'loca' ? version === 0 : version !== 0;
      dir.push({ tag, length: transformed ? base128() : orig });
    }
    const data = zlib.brotliDecompressSync(buf.subarray(p, p + compressed));
    let off = 0;
    for (const t of dir) { tables[t.tag] = data.subarray(off, off + t.length); off += t.length; }
  } else if (buf.readUInt32BE(0) === 0x00010000 || sig === 'OTTO') {
    const n = buf.readUInt16BE(4);
    for (let i = 0; i < n; i++) {
      const r = 12 + i * 16;
      const off = buf.readUInt32BE(r + 8);
      tables[buf.toString('latin1', r, r + 4)] = buf.subarray(off, off + buf.readUInt32BE(r + 12));
    }
  } else {
    throw new Error(`not a font: ${sig}`);
  }
  return tables;
}

function nameOf(t, id) {
  const count = t.readUInt16BE(2), strings = t.readUInt16BE(4);
  let mac;
  for (let i = 0; i < count; i++) {
    const r = 6 + i * 12;
    const [platform, , , nameId, length, offset] = [0, 2, 4, 6, 8, 10].map(o => t.readUInt16BE(r + o));
    if (nameId !== id) continue;
    const s = t.subarray(strings + offset, strings + offset + length);
    if (platform === 3 || platform === 0) {
      let out = '';
      for (let j = 0; j + 1 < s.length; j += 2) out += String.fromCharCode(s.readUInt16BE(j));
      return out;
    }
    if (platform === 1) mac = s.toString('latin1');
  }
  return mac;
}

function metricsOf(buf) {
  const t = tablesOf(buf);
  const upem = t.head.readUInt16BE(18);
  const em = v => v / upem;
  const os2 = t['OS/2'], hhea = t.hhea;
  const useTypo = (os2.readUInt16BE(62) & 0x80) !== 0;
  const m = {
    family: nameOf(t.name, 16) || nameOf(t.name, 1),
    subfamily: nameOf(t.name, 17) || nameOf(t.name, 2),
    version: nameOf(t.name, 5),
    licence: nameOf(t.name, 13),
    licenceUrl: nameOf(t.name, 14),
    weightClass: os2.readUInt16BE(4),
    unitsPerEm: upem,
    xAvgCharWidth: em(os2.readInt16BE(2)),
    hhea: { ascent: em(hhea.readInt16BE(4)), descent: em(-hhea.readInt16BE(6)), lineGap: em(hhea.readInt16BE(8)) },
    typo: { ascent: em(os2.readInt16BE(68)), descent: em(-os2.readInt16BE(70)), lineGap: em(os2.readInt16BE(72)) },
    win: { ascent: em(os2.readUInt16BE(74)), descent: em(os2.readUInt16BE(76)) },
    useTypoMetrics: useTypo,
    xHeight: os2.readUInt16BE(0) >= 2 ? em(os2.readInt16BE(86)) : null,
    capHeight: os2.readUInt16BE(0) >= 2 ? em(os2.readInt16BE(88)) : null,
  };
  // What Avalonia 12's GlyphTypeface takes as ascent and descent.
  m.avalonia = useTypo ? { ...m.typo, from: 'typo' } : { ...m.hhea, from: 'hhea' };
  return m;
}

// One file out of a zip (a .nupkg), in memory.
function unzip(zip, name) {
  let eocd = zip.lastIndexOf(Buffer.from([0x50, 0x4b, 0x05, 0x06]));
  if (eocd < 0) throw new Error('not a zip');
  const entries = zip.readUInt16LE(eocd + 10);
  let p = zip.readUInt32LE(eocd + 16);
  for (let i = 0; i < entries; i++) {
    const method = zip.readUInt16LE(p + 10), size = zip.readUInt32LE(p + 20);
    const nameLen = zip.readUInt16LE(p + 28), extra = zip.readUInt16LE(p + 30), comment = zip.readUInt16LE(p + 32);
    const local = zip.readUInt32LE(p + 42);
    const entry = zip.toString('utf8', p + 46, p + 46 + nameLen);
    p += 46 + nameLen + extra + comment;
    if (entry !== name) continue;
    const start = local + 30 + zip.readUInt16LE(local + 26) + zip.readUInt16LE(local + 28);
    const data = zip.subarray(start, start + size);
    return method === 8 ? zlib.inflateRawSync(data) : data;
  }
  throw new Error(`${name} not in the zip`);
}

// The Inter that Metered drew before this choice (the incumbent): Avalonia.Fonts.Inter's embedded
// files, at the pinned Avalonia version, from the NuGet cache or else nuget.org, read out of the
// assembly's Avalonia resources (an index of path, offset, size, then the data) into memory.
async function avaloniaInter() {
  const props = fs.readFileSync(path.join(root, 'Directory.Packages.props'), 'utf8');
  const version = props.match(/<AvaloniaVersion>([^<]+)</)[1];
  const nuget = process.env.NUGET_PACKAGES || path.join(os.homedir(), '.nuget', 'packages');
  const dll = path.join(nuget, 'avalonia.fonts.inter', version, 'lib', 'net8.0', 'Avalonia.Fonts.Inter.dll');
  const b = fs.existsSync(dll)
    ? fs.readFileSync(dll)
    : unzip(await get(`https://api.nuget.org/v3-flatcontainer/avalonia.fonts.inter/${version}/avalonia.fonts.inter.${version}.nupkg`), 'lib/net8.0/Avalonia.Fonts.Inter.dll');
  // The index: its size, version 2, the count, then (7-bit length, path, offset, size) per file.
  // The first path is the first '/Assets/' in the assembly, after the 13 bytes of size, version,
  // count and its one-byte length.
  const start = b.indexOf(Buffer.from('/Assets/')) - 13;
  const count = start >= 0 && b.readInt32LE(start + 4) === 2 ? b.readInt32LE(start + 8) : 0;
  if (count < 1 || count > 64) throw new Error(`no Avalonia resource index in Avalonia.Fonts.Inter ${version}`);
  const dataStart = start + 4 + b.readInt32LE(start);
  let p = start + 12;
  const files = {};
  for (let i = 0; i < count; i++) {
    const len = b[p++];
    const name = b.toString('utf8', p, p + len); p += len;
    const offset = b.readInt32LE(p), size = b.readInt32LE(p + 4); p += 8;
    files[name] = b.subarray(dataStart + offset, dataStart + offset + size);
  }
  const out = {};
  for (const w of WEIGHTS) {
    const f = files[`/Assets/Inter-${STYLE_NAMES[w]}.ttf`];
    if (!f) throw new Error(`Inter-${STYLE_NAMES[w]}.ttf not in Avalonia.Fonts.Inter ${version}`);
    out[w] = { bytes: f, url: `nuget:avalonia.fonts.inter/${version}/Assets/Inter-${STYLE_NAMES[w]}.ttf` };
  }
  return { files: out, licence: { source: `Avalonia.Fonts.Inter ${version}, the font's own name table` } };
}

async function get(url, as = 'buffer') {
  for (let attempt = 1; ; attempt++) {
    try {
      const r = await fetch(url);
      if (!r.ok) throw new Error(`${r.status} ${url}`);
      return as === 'text' ? await r.text() : Buffer.from(await r.arrayBuffer());
    } catch (e) {
      if (attempt >= 3) throw e;
    }
  }
}

async function loadCandidate(c) {
  if (c.source === 'avalonia') return avaloniaInter();
  if (c.repo) {
    const base = `https://raw.githubusercontent.com/${c.repo}/${c.commit}`;
    const files = {};
    for (const w of WEIGHTS) {
      const url = `${base}/${c.file(STYLE_NAMES[w])}`;
      files[w] = { bytes: await get(url), url };
    }
    const ofl = await get(`${base}/OFL.txt`, 'text');
    const licence = {
      source: `${base}/OFL.txt`,
      oflTxt: /SIL OPEN FONT LICENSE Version 1\.1/i.test(ofl),
      copyright: ofl.split(/\r?\n/).find(l => /copyright/i.test(l)),
    };
    if (!licence.oflTxt) throw new Error(`${c.name}: not OFL (${JSON.stringify(licence)})`);
    return { files, licence };
  }
  const version = c.version || FONTSOURCE_VERSION;
  const files = {};
  for (const w of WEIGHTS) {
    const url = `${FONTSOURCE}/${c.id}@${version}/files/${c.id}-latin-${w}-normal.woff2`;
    files[w] = { bytes: await get(url), url };
  }
  const metadata = await get(`${GOOGLE_FONTS}/${c.gf}/METADATA.pb`, 'text');
  const ofl = await get(`${GOOGLE_FONTS}/${c.gf}/OFL.txt`, 'text');
  const licence = {
    source: `${GOOGLE_FONTS}/${c.gf}/`,
    metadata: (metadata.match(/^license: "([^"]+)"/m) || [])[1],
    oflTxt: /SIL OPEN FONT LICENSE Version 1\.1/i.test(ofl),
    copyright: ofl.split(/\r?\n/).find(l => /copyright/i.test(l)),
  };
  if (licence.metadata !== 'OFL' || !licence.oflTxt) throw new Error(`${c.name}: not OFL (${JSON.stringify(licence)})`);
  return { files, licence };
}

// ---- in the page ----------------------------------------------------------------------------

// Runs in the page: measures one face. family is a CSS family list.
// Widths and heights are laid out at EM px, so Chromium's whole-pixel rounding of ascent, descent
// and ink is a thousandth of an em; labels also at the Button's own sizes, as it draws them.
function measureInPage({ family, weight, strings, ramp, labels = [], labelSizes = [] }) {
  const EM = 1000;
  let host = document.getElementById('__fonts');
  if (!host) {
    host = document.createElement('div');
    host.id = '__fonts';
    host.style.cssText = 'all: initial; position: absolute; left: 0; top: 0; visibility: hidden; white-space: pre; contain: layout;';
    document.body.appendChild(host);
  }
  const font = (size, lineHeight) => `font-family: ${family}; font-weight: ${weight}; font-size: ${size}px; line-height: ${lineHeight};` +
    ' font-style: normal; font-stretch: normal; font-kerning: auto; font-variant: normal; font-feature-settings: normal;' +
    ' letter-spacing: normal; word-spacing: normal; text-transform: none; text-rendering: auto;';
  const span = document.createElement('span');
  span.style.cssText = font(EM, 'normal');
  host.appendChild(span);
  const widths = {};
  for (const s of strings) { span.textContent = s; widths[s] = span.getBoundingClientRect().width / EM; }
  // The engine's content area and its baseline.
  span.textContent = 'Hxgp';
  const marker = () => { const m = document.createElement('span'); m.style.cssText = 'display: inline-block; width: 0; height: 0; vertical-align: baseline;'; return m; };
  const mk = marker();
  span.appendChild(mk);
  const box = span.getBoundingClientRect();
  const engine = { ascent: (mk.getBoundingClientRect().top - box.top) / EM, height: box.height / EM };
  engine.descent = engine.height - engine.ascent;
  const labelWidths = {};
  for (const size of labelSizes) {
    span.style.fontSize = `${size}px`;
    labelWidths[size] = {};
    for (const s of labels) { span.textContent = s; labelWidths[size][s] = span.getBoundingClientRect().width; }
  }
  span.remove();
  // Glyph heights, from the glyphs' ink: drawn on a canvas at EM px and scanned for the rows at
  // least half covered (measureText's actualBoundingBox is too coarse at this size).
  const canvas = document.createElement('canvas');
  canvas.width = 1400; canvas.height = 1600;
  const ctx = canvas.getContext('2d', { willReadFrequently: true });
  const BASE = 1200;
  const ink = t => {
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.font = `${weight} ${EM}px ${family}`;
    ctx.textBaseline = 'alphabetic';
    ctx.fillStyle = 'black';
    ctx.fillText(t, 100, BASE);
    const { data } = ctx.getImageData(0, 0, canvas.width, canvas.height);
    let top = -1, bottom = -1;
    for (let y = 0; y < canvas.height; y++) {
      let row = false;
      for (let x = 0; x < canvas.width && !row; x++) row = data[(y * canvas.width + x) * 4 + 3] >= 128;
      if (row) { if (top < 0) top = y; bottom = y; }
    }
    if (top < 0) throw new Error(`no ink for "${t}" in ${family} ${weight}`);
    return { ascent: (BASE - top) / EM, descent: (bottom + 1 - BASE) / EM };
  };
  const glyphs = {
    capHeight: ink('H').ascent,
    xHeight: ink('x').ascent,
    ascender: ink('d').ascent,
    descender: ink('p').descent,
  };
  // The baseline in the line box at each size of the ramp.
  const lines = ramp.map(r => {
    const div = document.createElement('div');
    div.style.cssText = font(r.size, `${r.lineHeight}px`) + ' display: block;';
    div.textContent = 'Button';
    const m = marker();
    div.appendChild(m);
    host.appendChild(div);
    const top = div.getBoundingClientRect().top;
    const out = { name: r.name, size: r.size, lineHeight: r.lineHeight, boxHeight: div.getBoundingClientRect().height, baseline: m.getBoundingClientRect().top - top };
    div.remove();
    return out;
  });
  return { widths, labelWidths, engine, glyphs, lines };
}

async function platformFonts(page, cdp, family, weight, text) {
  await page.evaluate(({ family, weight, text }) => {
    let el = document.getElementById('__fontcheck');
    if (!el) { el = document.createElement('span'); el.id = '__fontcheck'; document.body.appendChild(el); }
    el.style.cssText = `all: initial; position: absolute; left: 0; top: 0; visibility: hidden; white-space: pre; font-family: ${family}; font-weight: ${weight}; font-size: 20px;`;
    el.textContent = text;
    el.getBoundingClientRect();
    return new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done)));
  }, { family, weight, text });
  const { root: doc } = await cdp.send('DOM.getDocument', { depth: -1 });
  const { nodeId } = await cdp.send('DOM.querySelector', { nodeId: doc.nodeId, selector: '#__fontcheck' });
  const { fonts } = await cdp.send('CSS.getPlatformFontsForNode', { nodeId });
  return fonts;
}

// ---- comparison ---------------------------------------------------------------------------

const pct = v => (v == null ? '' : `${v >= 0 ? '+' : ''}${(v * 100).toFixed(1)}%`);
const fixed = (v, n = 3) => (v == null ? '' : v.toFixed(n));
const mean = a => a.reduce((s, v) => s + v, 0) / a.length;
const sum = (w, list) => list.reduce((s, k) => s + w[k], 0);

function compareWidths(c, r) {
  const errors = ALL.map(s => ({ s, e: c.widths[s] / r.widths[s] - 1 }));
  const worst = errors.reduce((a, b) => (Math.abs(b.e) > Math.abs(a.e) ? b : a));
  const groups = {};
  for (const [g, list] of Object.entries(STRINGS)) groups[g] = sum(c.widths, list) / sum(r.widths, list) - 1;
  return {
    meanAbs: mean(errors.map(x => Math.abs(x.e))),
    signed: mean(errors.map(x => x.e)),
    worst: worst.e,
    worstString: worst.s,
    groups,
    avgCharWidth: sum(c.widths, AVG) / sum(r.widths, AVG) - 1,
  };
}

function compareVertical(c, cm, r, rampRefs) {
  const g = k => c.glyphs[k] / r.glyphs[k] - 1;
  const lines = rampRefs.map(ref => {
    const mine = c.lines.find(l => l.name === ref.name);
    // Avalonia: the line height's extra leading split evenly about the font's own ascent + descent.
    const a = cm ? cm.avalonia : null;
    const place = (ascent, descent) => (ref.lineHeight - (ascent + descent) * ref.size) / 2 + ascent * ref.size;
    const avalonia = a ? place(a.ascent, a.descent) : null;
    // Chromium places baseweb.design's baseline with ascent and descent rounded to whole pixels;
    // Uber Move placed as Avalonia places text, with the metrics Chromium used for it, unrounded,
    // leaves the font's own share of the difference.
    const uberMove = place(r.engine.ascent, r.engine.descent);
    return {
      name: ref.name, size: ref.size, reference: ref.baseline, chromium: mine.baseline - ref.baseline,
      avalonia: avalonia == null ? null : avalonia - ref.baseline,
      fontOnly: avalonia == null ? null : avalonia - uberMove,
    };
  });
  return {
    capHeight: g('capHeight'), xHeight: g('xHeight'), ascender: g('ascender'), descender: g('descender'),
    glyphMeanAbs: mean(['capHeight', 'xHeight'].map(k => Math.abs(g(k)))),
    baselineChromium: mean(lines.map(l => Math.abs(l.chromium))),
    baselineAvalonia: lines[0].avalonia == null ? null : mean(lines.map(l => Math.abs(l.avalonia))),
    baselineFontOnly: lines[0].fontOnly == null ? null : mean(lines.map(l => Math.abs(l.fontOnly))),
    lines,
  };
}

// ---- main ---------------------------------------------------------------------------------

(async () => {
  const loaded = [];
  for (const c of CANDIDATES) {
    const { files, licence } = await loadCandidate(c);
    const metrics = {};
    const sha256 = {};
    for (const w of WEIGHTS) {
      metrics[w] = metricsOf(files[w].bytes);
      sha256[w] = crypto.createHash('sha256').update(files[w].bytes).digest('hex');
    }
    if (c.source === 'avalonia') {
      licence.nameTable = metrics[400].licence;
      if (!/Open Font License/i.test(licence.nameTable || '')) throw new Error(`${c.name}: no OFL in its name table`);
    }
    loaded.push({ ...c, files, licence, metrics, sha256 });
    process.stderr.write(`fetched ${c.name}\n`);
  }

  const browser = await chromium.launch();
  try {
    // A new context is off the record: its HTTP cache is in memory. The cache is also disabled.
    const context = await browser.newContext({ viewport: { width: 1280, height: 900 } });
    const page = await context.newPage();
    const cdp = await context.newCDPSession(page);
    await cdp.send('Network.enable');
    await cdp.send('Network.setCacheDisabled', { cacheDisabled: true });

    let faces;
    for (let attempt = 1; ; attempt++) {
      try {
        await page.goto(PAGE, { waitUntil: 'networkidle', timeout: 60000 });
        // load() gives the faces CSS matches for each family and weight (and rejects if one fails):
        // each must be loaded and registered at that weight (or `face`), so a weight the page no
        // longer registers can't pass on the nearest one.
        let matched;
        ({ faces, matched } = await page.evaluate(async refs => {
          await document.fonts.ready;
          const matched = [];
          for (const r of refs) {
            const list = await document.fonts.load(`${r.weight} 20px ${r.family}`, 'Button');
            matched.push({ ...r, faces: list.map(f => ({ weight: f.weight, status: f.status })) });
          }
          const faces = [...document.fonts].filter(f => /^"?UberMove/.test(f.family))
            .map(f => ({ family: f.family.replace(/"/g, ''), weight: f.weight, status: f.status }));
          return { faces, matched };
        }, REFERENCE));
        const missing = matched.filter(r => !r.faces.length || r.faces.some(f => f.status !== 'loaded' || Number(f.weight) !== (r.face || r.weight)));
        if (missing.length) throw new Error(`Uber Move not loaded: ${missing.map(m => `${m.family} ${m.weight}`).join(', ')}`);
        break;
      } catch (e) {
        process.stderr.write(`attempt ${attempt}: ${e.message}\n`);
        if (attempt >= 3) throw new Error(`baseweb.design did not serve Uber Move after ${attempt} attempts: ${e.message}`);
      }
    }
    const fontFaceRules = await page.evaluate(() => {
      const out = [];
      for (const s of document.styleSheets) {
        let rules;
        try { rules = s.cssRules; } catch { continue; }
        for (const r of rules) {
          if (!(r instanceof CSSFontFaceRule) || !/UberMove(Text)?\b/.test(r.style.fontFamily) || /Mono/.test(r.style.fontFamily)) continue;
          out.push({ family: r.style.fontFamily.replace(/"/g, ''), weight: r.style.fontWeight, file: ((r.style.getPropertyValue('src').match(/url\("?([^")]+)/) || [])[1] || '').split('/').pop() });
        }
      }
      return out;
    });

    await cdp.send('DOM.enable');
    await cdp.send('CSS.enable');
    const text = ALL.join(' ');

    // The reference, with the proof that Uber Move drew it: the platform font Chromium used for
    // every glyph, and the same widths whatever comes after it in the stack.
    const reference = {};
    for (const r of REFERENCE) {
      const css = `"${r.family}"`;
      const m = await page.evaluate(measureInPage, { family: css, weight: r.weight, strings: ALL, ramp: RAMP.filter(x => x.role === r.role), ...labelsAt(r.weight) });
      const used = await platformFonts(page, cdp, css, r.weight, text);
      const viaMono = await page.evaluate(measureInPage, { family: `${css}, monospace`, weight: r.weight, strings: ALL, ramp: [] });
      const viaSerif = await page.evaluate(measureInPage, { family: `${css}, serif`, weight: r.weight, strings: ALL, ramp: [] });
      const fallbackFree = ALL.every(s => viaMono.widths[s] === m.widths[s] && viaSerif.widths[s] === m.widths[s]);
      reference[r.role] = { ...r, ...m, platformFonts: used, fallbackFree };
      // The one platform font is a web font of this family (UberMoveText-Medium, UberMove-Bold), not
      // the other cut's.
      if (!fallbackFree || used.length !== 1 || !used[0].isCustomFont || !used[0].postScriptName.startsWith(`${r.family}-`)) {
        throw new Error(`${r.family} ${r.weight} did not draw every glyph in its own face: ${JSON.stringify(used)}`);
      }
    }

    // The candidates, each face loaded from its bytes into the same page.
    const results = [];
    for (const c of loaded) {
      const family = `"Metered-${c.id}"`;
      const faces = {};
      for (const w of WEIGHTS) {
        const status = await page.evaluate(async ({ name, weight, b64 }) => {
          const bin = Uint8Array.from(atob(b64), ch => ch.charCodeAt(0));
          const face = new FontFace(name, bin.buffer, { weight: String(weight) });
          await face.load();
          document.fonts.add(face);
          return face.status;
        }, { name: `Metered-${c.id}`, weight: w, b64: c.files[w].bytes.toString('base64') });
        if (status !== 'loaded') throw new Error(`${c.name} ${w}: ${status}`);
      }
      for (const w of WEIGHTS) {
        const role = ROLES.find(x => reference[x].weight === w);
        const m = await page.evaluate(measureInPage, { family, weight: w, strings: ALL, ramp: role ? RAMP.filter(x => x.role === role) : [], ...labelsAt(w) });
        const used = await platformFonts(page, cdp, family, w, text);
        if (used.length !== 1 || !used[0].isCustomFont) throw new Error(`${c.name} ${w} did not draw every glyph: ${JSON.stringify(used)}`);
        faces[w] = { ...m, platformFonts: used };
      }
      const cmp = {};
      for (const role of ROLES) {
        const r = reference[role];
        const face = faces[r.weight];
        cmp[role] = { width: compareWidths(face, r), vertical: compareVertical(face, c.metrics[r.weight], r, r.lines) };
      }
      // Which of the candidate's weights draws each reference face's widths best.
      const nearest = {};
      for (const [role, r] of Object.entries(reference)) {
        const scored = WEIGHTS.map(w => ({ w, e: compareWidths(faces[w], r).meanAbs }));
        nearest[role] = scored.reduce((a, b) => (b.e < a.e ? b : a)).w;
      }
      results.push({
        id: c.id, name: c.name, files: Object.fromEntries(WEIGHTS.map(w => [w, { url: c.files[w].url, sha256: c.sha256[w] }])),
        licence: c.licence, metrics: c.metrics, faces, compare: cmp, nearest,
        widthScore: mean(ROLES.map(role => cmp[role].width.meanAbs)),
        verticalScore: mean(ROLES.map(role => cmp[role].vertical.glyphMeanAbs)),
        baselineScore: mean(ROLES.map(role => cmp[role].vertical.baselineAvalonia)),
        baselineFontOnly: mean(ROLES.map(role => cmp[role].vertical.baselineFontOnly)),
      });
      process.stderr.write(`measured ${c.name}\n`);
    }

    // Context: the local fallbacks of Base Web's own stack.
    const local = [];
    for (const l of LOCAL) {
      const cmp = {};
      for (const role of ROLES) {
        const r = reference[role];
        const m = await page.evaluate(measureInPage, { family: l.css, weight: r.weight, strings: ALL, ramp: RAMP.filter(x => x.role === role) });
        const used = await platformFonts(page, cdp, l.css, r.weight, text);
        cmp[role] = { width: compareWidths(m, r), vertical: compareVertical(m, null, r, r.lines), platformFonts: used };
      }
      local.push({ ...l, compare: cmp, widthScore: mean(ROLES.map(role => cmp[role].width.meanAbs)), verticalScore: mean(ROLES.map(role => cmp[role].vertical.glyphMeanAbs)) });
    }

    results.sort((a, b) => a.widthScore - b.widthScore);
    report({ browser: browser.version(), faces, fontFaceRules, reference, results, local });

    if (write) {
      const json = {
        generated: 'by tools/reference/fonts.js --write; do not hand-edit',
        measured: new Date().toISOString().slice(0, 10),
        page: PAGE,
        browser: `Chromium ${browser.version()} (playwright-core ${require('playwright-core/package.json').version}), ${os.platform()}`,
        baseui: require('baseui/package.json').version,
        strings: STRINGS,
        ramp: RAMP,
        units: 'widths, glyphs and engine metrics in em; lines in px; errors as candidate / reference - 1',
        uberMove: { fontFaceRules, faces, reference },
        candidates: results,
        local,
      };
      // Five decimals: a hundred-thousandth of an em, well under what the page resolves.
      // Widths as arrays, in the order of `strings` (all of them) and of `strings.labels`.
      const round = (k, v) => {
        if (k === 'widths' && v && !Array.isArray(v)) return ALL.map(s => v[s]);
        if (k === 'labelWidths' && v) return Object.fromEntries(Object.entries(v).map(([z, w]) => [z, STRINGS.labels.map(s => w[s])]));
        return typeof v === 'number' && !Number.isInteger(v) ? Number(v.toFixed(5)) : v;
      };
      // Arrays of numbers, and objects of plain values, on one line each.
      const text = JSON.stringify(json, round, 1)
        .replace(/\[\s*(-?[\d.e+-]+(?:,\s*-?[\d.e+-]+)*)\s*\]/g, (m, list) => `[${list.split(/,\s*/).join(', ')}]`)
        .replace(/\{\n((?: *"[^"\n]*": (?:-?[\d.e+-]+|"[^"\n]*"|true|false|null),?\n)+) *\}/g, m => m.replace(/\n */g, ' '));
      fs.writeFileSync(outFile, text + '\n');
      process.stderr.write(`wrote ${path.relative(root, outFile)}\n`);
    }
  } finally {
    await browser.close();
  }
})().catch(e => { console.error(e.stack || e.message); process.exit(1); });

function report({ browser, faces, fontFaceRules, reference, results, local }) {
  const out = [];
  const p = s => out.push(s);
  p(`# Uber Move against its candidates\n`);
  p(`Measured in headless Chromium ${browser} on ${PAGE}. Errors are candidate / Uber Move - 1.\n`);
  p(`## The reference\n`);
  p('| Role | Face | @font-face weight, file | Platform font Chromium drew | Glyphs | Same with a monospace or serif fallback |');
  p('|---|---|---|---|---|---|');
  for (const r of Object.values(reference)) {
    const pf = r.platformFonts[0];
    const rules = fontFaceRules.filter(f => f.family === r.family).map(f => `${f.weight} ${f.file}`).join('; ');
    p(`| ${r.role} | ${r.family} ${r.weight} | ${rules} | ${pf.familyName} (${pf.postScriptName}), custom ${pf.isCustomFont} | ${pf.glyphCount} | ${r.fallbackFree ? 'yes' : 'no'} |`);
  }
  const d6 = reference.display600, d7 = reference.display700;
  p(`\nUberMove at 700 and at 600 draw the same widths: ${ALL.every(s => d6.widths[s] === d7.widths[s]) ? 'yes' : 'no'}; the same cap height: ${d6.glyphs.capHeight === d7.glyphs.capHeight ? 'yes' : 'no'}.`);
  const cut = compareWidths(reference.display400, reference.text400);
  p(`UberMove against UberMoveText at 400 (the display cut against the text cut): widths ${pct(cut.signed)} on average (worst ${pct(cut.worst)}, "${cut.worstString}").\n`);

  p(`## Ranking, by width error\n`);
  p('Mean and worst absolute width error over the strings, per ramp face; glyph height error (cap height and x-height); and the baseline in the ramp\'s line boxes as Avalonia would place it, against baseweb.design\'s.\n');
  p('| # | Candidate | UberMoveText 400 mean / worst | UberMoveText 500 mean / worst | UberMove 700 mean / worst | Width score | Cap height 400 / 700 | x-height 400 / 700 | Baseline, Avalonia: against baseweb.design / the font\'s share (mean px) |');
  p('|---|---|---|---|---|---|---|---|---|');
  results.forEach((c, i) => {
    const w = role => `${(c.compare[role].width.meanAbs * 100).toFixed(1)}% / ${pct(c.compare[role].width.worst)}`;
    const v = c.compare;
    p(`| ${i + 1} | ${c.name} | ${w('text400')} | ${w('text500')} | ${w('display700')} | ${(c.widthScore * 100).toFixed(2)}% | ${pct(v.text400.vertical.capHeight)} / ${pct(v.display700.vertical.capHeight)} | ${pct(v.text400.vertical.xHeight)} / ${pct(v.display700.vertical.xHeight)} | ${fixed(c.baselineScore, 2)} / ${fixed(c.baselineFontOnly, 2)} |`);
  });
  for (const l of local) {
    const w = role => `${(l.compare[role].width.meanAbs * 100).toFixed(1)}% / ${pct(l.compare[role].width.worst)}`;
    p(`| - | ${l.name}: ${l.compare.text400.platformFonts.map(f => f.familyName).join(', ')} | ${w('text400')} | ${w('text500')} | ${w('display700')} | ${(l.widthScore * 100).toFixed(2)}% | ${pct(l.compare.text400.vertical.capHeight)} / ${pct(l.compare.display700.vertical.capHeight)} | ${pct(l.compare.text400.vertical.xHeight)} / ${pct(l.compare.display700.vertical.xHeight)} | |`);
  }

  // Base Web names two families, UberMoveText (primaryFontFamily) and UberMove (secondary), so a
  // substitute may be one family or a pair.
  const textScore = c => mean([c.compare.text400.width.meanAbs, c.compare.text500.width.meanAbs]);
  const displayScore = c => c.compare.display700.width.meanAbs;
  const byText = [...results].sort((a, b) => textScore(a) - textScore(b));
  const byDisplay = [...results].sort((a, b) => displayScore(a) - displayScore(b));
  p(`\n## Text and display apart\n`);
  p('| # | For UberMoveText (400, 500): mean width error | For UberMove (700): mean width error |');
  p('|---|---|---|');
  for (let i = 0; i < 8; i++) p(`| ${i + 1} | ${byText[i].name}: ${(textScore(byText[i]) * 100).toFixed(2)}% | ${byDisplay[i].name}: ${(displayScore(byDisplay[i]) * 100).toFixed(2)}% |`);
  const pair = mean([textScore(byText[0]), textScore(byText[0]), displayScore(byDisplay[0])]);
  p(`\nThe best pair, ${byText[0].name} with ${byDisplay[0].name}, scores ${(pair * 100).toFixed(2)}% on the three ramp faces, against ${(results[0].widthScore * 100).toFixed(2)}% for the best single family, ${results[0].name}.`);
  const incumbent = results.find(c => c.id === 'inter-avalonia');
  if (!incumbent) throw new Error('the incumbent, inter-avalonia, was not measured');
  const shown = [...new Set([...results.slice(0, 6), ...byText.slice(0, 4), ...byDisplay.slice(0, 4), incumbent])];

  p(`\n## By kind of string (signed width error)\n`);
  p('| Candidate | Face | Labels | Sentences | Digits | Capitals | Lowercase | Average character |');
  p('|---|---|---|---|---|---|---|---|');
  for (const c of shown) for (const role of ROLES) {
    const g = c.compare[role].width.groups;
    p(`| ${c.name} | ${role} | ${pct(g.labels)} | ${pct(g.sentences)} | ${pct(g.digits)} | ${pct(g.capitals)} | ${pct(g.lowercase)} | ${pct(c.compare[role].width.avgCharWidth)} |`);
  }

  p(`\n## Vertical metrics (em)\n`);
  p('Glyph heights from the ink; the engine\'s ascent and descent from the laid-out content area; Avalonia\'s from the font\'s tables.\n');
  p('| Font | Weight | Cap height | x-height | Ascender (d) | Descender (p) | Chromium ascent / descent | Avalonia ascent / descent (table) | hhea | win |');
  p('|---|---|---|---|---|---|---|---|---|---|');
  for (const role of ROLES) {
    const r = reference[role];
    p(`| **${r.family}** | ${r.weight} | ${fixed(r.glyphs.capHeight)} | ${fixed(r.glyphs.xHeight)} | ${fixed(r.glyphs.ascender)} | ${fixed(r.glyphs.descender)} | ${fixed(r.engine.ascent)} / ${fixed(r.engine.descent)} | | | |`);
  }
  for (const c of shown) for (const role of ROLES) {
    const w = reference[role].weight, f = c.faces[w], m = c.metrics[w];
    p(`| ${c.name} | ${w} | ${fixed(f.glyphs.capHeight)} | ${fixed(f.glyphs.xHeight)} | ${fixed(f.glyphs.ascender)} | ${fixed(f.glyphs.descender)} | ${fixed(f.engine.ascent)} / ${fixed(f.engine.descent)} | ${fixed(m.avalonia.ascent)} / ${fixed(m.avalonia.descent)} (${m.avalonia.from}) | ${fixed(m.hhea.ascent)} / ${fixed(m.hhea.descent)} | ${fixed(m.win.ascent)} / ${fixed(m.win.descent)} |`);
  }

  p(`\n## Baseline in the ramp's line boxes (px, candidate - baseweb.design)\n`);
  p('avalonia: the candidate as Avalonia places it, against baseweb.design as Chromium drew it (whole pixels). fontOnly: against Uber Move placed the same way as the candidate, unrounded: the font\'s own share. chromium: the candidate in the same page.\n');
  const names = RAMP.map(r => r.name);
  p(`| Candidate | Engine | ${names.join(' | ')} |`);
  p(`|---|---|${names.map(() => '---').join('|')}|`);
  const refLine = n => { for (const role of ROLES) { const l = reference[role].lines.find(x => x.name === n); if (l) return l; } };
  p(`| **baseweb.design** | baseline | ${names.map(n => fixed(refLine(n).baseline, 2)).join(' | ')} |`);
  for (const c of shown) for (const engine of ['avalonia', 'fontOnly', 'chromium']) {
    const at = n => { for (const role of ROLES) { const l = c.compare[role].vertical.lines.find(x => x.name === n); if (l) return l[engine]; } };
    p(`| ${c.name} | ${engine} | ${names.map(n => fixed(at(n), 2)).join(' | ')} |`);
  }

  p(`\n## Button labels at the Button's sizes (px, candidate - baseweb.design, laid out at that size)\n`);
  const r5 = reference.text500;
  p(`| Candidate | ${STRINGS.labels.slice(0, 6).map(s => `"${s}" at 16`).join(' | ')} | "Button" at ${LABEL_SIZES.join(' / ')} | Mean / worst over all labels and sizes |`);
  p(`|---|${STRINGS.labels.slice(0, 6).map(() => '---').join('|')}|---|---|`);
  p(`| **baseweb.design** (px) | ${STRINGS.labels.slice(0, 6).map(s => r5.labelWidths[16][s].toFixed(2)).join(' | ')} | ${LABEL_SIZES.map(z => r5.labelWidths[z].Button.toFixed(2)).join(' / ')} | |`);
  for (const c of results) {
    const f = c.faces[500];
    const d = (s, size) => f.labelWidths[size][s] - r5.labelWidths[size][s];
    const all = LABEL_SIZES.flatMap(z => STRINGS.labels.map(s => d(s, z)));
    const worst = all.reduce((a, b) => (Math.abs(b) > Math.abs(a) ? b : a));
    p(`| ${c.name} | ${STRINGS.labels.slice(0, 6).map(s => d(s, 16).toFixed(2)).join(' | ')} | ${LABEL_SIZES.map(z => d('Button', z).toFixed(2)).join(' / ')} | ${mean(all.map(Math.abs)).toFixed(2)} / ${worst.toFixed(2)} |`);
  }

  p(`\n## Nearest weight to each Uber Move face, by width\n`);
  p(`| Candidate | ${Object.keys(reference).join(' | ')} |`);
  p(`|---|${Object.keys(reference).map(() => '---').join('|')}|`);
  for (const c of results) p(`| ${c.name} | ${Object.keys(reference).map(k => c.nearest[k]).join(' | ')} |`);

  p(`\n## Files and licences\n`);
  p('| Candidate | Version (name table) | Licence | Source of the licence |');
  p('|---|---|---|---|');
  for (const c of results) {
    const l = c.licence;
    p(`| ${c.name} | ${(c.metrics[400].version || '').split(';')[0]} | ${l.metadata ? `METADATA.pb: ${l.metadata}; OFL.txt: ${l.oflTxt ? 'OFL 1.1' : '?'}` : l.oflTxt ? 'OFL.txt: OFL 1.1' : `name table: ${(l.nameTable || '').slice(0, 72)}`} | ${l.source} |`);
  }
  console.log(out.join('\n'));
}
