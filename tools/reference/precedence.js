// Which of Base Web's competing box-shadow rules wins in a real browser (hover, :active,
// :focus-visible, selected)? Reads computed styles on baseweb.design's own Button page.
const { chromium } = require('playwright-core');
(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1280, height: 900 }, hasTouch: false });
  await page.goto('https://baseweb.design/components/button/', { waitUntil: 'networkidle' });
  const btn = page.locator('main [data-baseweb="button"]').first();
  const read = () => btn.evaluate(el => { const s = getComputedStyle(el); return { bg: s.backgroundColor, shadow: s.boxShadow, text: s.color, kind: el.textContent }; });
  const out = { rest: await read() };
  const box = await btn.boundingBox();
  await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2); await page.waitForTimeout(300);
  out.hover = await read();
  await page.mouse.down(); await page.waitForTimeout(300);
  out.hoverActive = await read();
  await page.mouse.up(); await page.mouse.move(0, 0); await page.waitForTimeout(300);
  await btn.focus(); await page.keyboard.press('Shift+Tab'); await page.keyboard.press('Tab'); await page.waitForTimeout(300);
  out.focusVisible = await read();
  await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2); await page.waitForTimeout(300);
  out.focusVisibleHover = await read();
  console.log(JSON.stringify(out, null, 1));
  await browser.close();
})();
