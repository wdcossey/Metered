// Where Base Web's Popover puts its body and arrow: popper.js itself (the version baseui 18.2.0 depends on, from
// node_modules/popper.js), run in headless Chromium on plain boxes, with the options Base Web's TetherBehavior
// (layer/tether.js) and Popover (popover/popover.js) give it. Each case is an anchor's rectangle in a 390 × 844 page, a
// body of a size with the margin Base Web's Body has before popper.js first runs (getPopoverMarginStyles at the
// requested placement: none for auto, whose $placement is still 'auto'), an arrow or none, and a placement. popper.js's
// first update gives the placement it chose, the body's offset and the arrow's (parsePopperOffset floors them), which is
// what Base Web renders: its Tether schedules another update only when the anchor or the body changes size, and a new
// margin doesn't change the body's border box.
//
//   node tools/reference/tether.js           print a summary
//   node tools/reference/tether.js --write   write tools/reference/tether.g.json (generated: never hand-edit it; run
//                                            this again)
//
// Needs a Chromium for playwright-core (`npx playwright-core install chromium`); no network: the page is blank and
// popper.js is read from node_modules. cases.js reads tether.g.json into popover.oracle.json, so CI checks it with no
// browser.
'use strict';

const fs = require('fs');
const path = require('path');
const { chromium } = require('playwright-core');
const { getPopoverMarginStyles } = require('baseui/popover/utils');
const { ARROW_SIZE, ARROW_WIDTH, PLACEMENT, POPOVER_MARGIN } = require('baseui/popover/constants');
const { toPopperPlacement } = require('baseui/layer/utils');

const outFile = path.join(__dirname, 'tether.g.json');
const write = process.argv.includes('--write');
const popperFile = require.resolve('popper.js');
const popperVersion = require('popper.js/package.json').version;

const VIEW = { width: 390, height: 844 };

// Anchors across the page: its middle, each corner and edge, one as wide as the page, one off its top.
const ANCHORS = [
  { name: 'middle', x: 150, y: 400, width: 90, height: 40 },
  { name: 'topLeft', x: 8, y: 8, width: 60, height: 32 },
  { name: 'topRight', x: 322, y: 8, width: 60, height: 32 },
  { name: 'bottomLeft', x: 8, y: 804, width: 60, height: 32 },
  { name: 'bottomRight', x: 322, y: 804, width: 60, height: 32 },
  { name: 'left', x: 0, y: 400, width: 40, height: 40 },
  { name: 'right', x: 350, y: 400, width: 40, height: 40 },
  { name: 'wide', x: 0, y: 300, width: 390, height: 48 },
  { name: 'fractional', x: 100.5, y: 200.25, width: 75.5, height: 33.75 },
];
// Bodies: small, wider than half the page, taller than the space on either side of the middle.
const BODIES = [
  { name: 'small', width: 120, height: 60 },
  { name: 'wide', width: 300, height: 80 },
  { name: 'tall', width: 200, height: 500 },
];
const PLACEMENTS = Object.values(PLACEMENT);
// The anchors measured with ignoreBoundary too: where preventOverflow would move the body.
const IGNORE = ['topLeft', 'bottomRight', 'right'];

function cases() {
  const list = [];
  for (const anchor of ANCHORS) {
    for (const body of BODIES) {
      for (const placement of PLACEMENTS) {
        // The default margin with an arrow and without, no margin (Select's and Tooltip's), and no boundary.
        list.push({ anchor, body, placement, showArrow: false, popoverMargin: POPOVER_MARGIN, ignoreBoundary: false });
        list.push({ anchor, body, placement, showArrow: true, popoverMargin: POPOVER_MARGIN, ignoreBoundary: false });
        list.push({ anchor, body, placement, showArrow: false, popoverMargin: 0, ignoreBoundary: false });
        if (IGNORE.includes(anchor.name)) list.push({ anchor, body, placement, showArrow: false, popoverMargin: POPOVER_MARGIN, ignoreBoundary: true });
      }
    }
  }
  return list;
}

/** The margins Base Web's Body has before popper.js runs: getPopoverMarginStyles at the requested placement. */
function margins(c) {
  const m = getPopoverMarginStyles(c.showArrow ? ARROW_SIZE : 0, c.placement, c.popoverMargin) || {};
  const px = (v) => (v === undefined ? 0 : parseFloat(v));
  return { top: px(m.marginTop), right: px(m.marginRight), bottom: px(m.marginBottom), left: px(m.marginLeft) };
}

async function measure() {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage({ viewport: VIEW });
    await page.setContent('<!DOCTYPE html><html><head><style>html,body{margin:0;padding:0}</style></head><body></body></html>');
    await page.addScriptTag({ path: popperFile });
    const input = cases().map((c) => ({ ...c, margin: margins(c), popperPlacement: toPopperPlacement(c.placement) }));
    const results = await page.evaluate(async ({ list, arrowWidth }) => {
      const out = [];
      for (const c of list) {
        document.body.innerHTML = '';
        const ref = document.createElement('div');
        Object.assign(ref.style, { position: 'absolute', left: `${c.anchor.x}px`, top: `${c.anchor.y}px`, width: `${c.anchor.width}px`, height: `${c.anchor.height}px` });
        const pop = document.createElement('div');
        Object.assign(pop.style, {
          position: 'absolute', top: '0', left: '0', width: `${c.body.width}px`, height: `${c.body.height}px`,
          marginTop: `${c.margin.top}px`, marginRight: `${c.margin.right}px`, marginBottom: `${c.margin.bottom}px`, marginLeft: `${c.margin.left}px`,
        });
        let arrow = null;
        if (c.showArrow) {
          arrow = document.createElement('div');
          Object.assign(arrow.style, { position: 'absolute', width: `${arrowWidth}px`, height: `${arrowWidth}px`, transform: 'rotate(45deg)' });
          pop.appendChild(arrow);
        }
        document.body.append(ref, pop);
        // eslint-disable-next-line no-undef
        const data = await new Promise((done) => {
          // TetherBehavior's options, with Popover's defaultPopperOptions merged over its modifiers.
          // eslint-disable-next-line no-new, no-undef
          new Popper(ref, pop, {
            placement: c.popperPlacement,
            modifiers: {
              arrow: { element: arrow, enabled: !!arrow },
              computeStyle: { gpuAcceleration: false },
              applyStyle: { enabled: false },
              applyReactStyle: { enabled: true, fn: (d) => { done(d); return d; }, order: 900 },
              preventOverflow: { enabled: !c.ignoreBoundary, padding: 0 },
            },
          });
        });
        out.push({ placement: data.placement, popper: { top: data.offsets.popper.top, left: data.offsets.popper.left }, arrow: data.offsets.arrow ? { ...data.offsets.arrow } : null });
      }
      return out;
    }, { list: input, arrowWidth: ARROW_WIDTH });
    return input.map((c, i) => ({ c, r: results[i] }));
  } finally {
    await browser.close();
  }
}

/** Base Web's fromPopperPlacement and parsePopperOffset, applied as popover.js and tether.js apply them. */
function placementOf(p) {
  const s = p.replace(/(top|bottom)-start$/, '$1Left').replace(/(top|bottom)-end$/, '$1Right')
    .replace(/(left|right)-start$/, '$1Top').replace(/(left|right)-end$/, '$1Bottom');
  return PLACEMENT[s] || PLACEMENT.top;
}
const floor = (o) => ({ top: Math.floor(o.top || 0), left: Math.floor(o.left || 0) });

(async () => {
  const measured = await measure();
  const json = {
    popper: popperVersion,
    view: VIEW,
    arrowWidth: ARROW_WIDTH,
    cases: measured.map(({ c, r }) => ({
      anchor: c.anchor.name, rect: [c.anchor.x, c.anchor.y, c.anchor.width, c.anchor.height],
      body: [c.body.width, c.body.height], placement: c.placement, showArrow: c.showArrow, popoverMargin: c.popoverMargin,
      ignoreBoundary: c.ignoreBoundary, margin: [c.margin.left, c.margin.top, c.margin.right, c.margin.bottom],
      result: placementOf(r.placement), offset: floor(r.popper), arrowOffset: c.showArrow ? floor(r.arrow || {}) : null,
    })),
  };
  const flips = json.cases.filter((x) => x.placement !== 'auto' && x.result !== x.placement).length;
  console.log(`${json.cases.length} cases from popper.js ${popperVersion}; ${flips} flipped`);
  if (write) {
    // A case to a line: the file is read by cases.js, not by people.
    const { cases: list, ...head } = json;
    const text = `${JSON.stringify(head, null, 1).replace(/\n}$/, ',\n "cases": [\n')}${list.map((x) => `  ${JSON.stringify(x)}`).join(',\n')}\n ]\n}\n`;
    JSON.parse(text);
    fs.writeFileSync(outFile, text);
    console.log('wrote', path.relative(path.resolve(__dirname, '../..'), outFile));
  }
})().catch((e) => { console.error(e); process.exit(1); });
