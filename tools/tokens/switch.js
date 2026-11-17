// The themes of Switch's parts, Switch.Styled<Part>, generated from Base Web's switch
// (baseui/switch/styled-components).
//
// As radio.js does for the radio, and with its theme writer: each style function is evaluated on Base Web's light
// theme with its colours replaced by their own names ('@contentPrimary'), for every combination of the switch's
// states, so that the themes refer to the colours by token and read every length; each property is a plain setter
// for the default state and a state style for each combination it differs in, checked against Base Web's for every
// combination.
'use strict';

const css = require('../reference/css');
const { THEMES, evaluate } = require('../reference/oracle');
const sw = require('baseui/switch/styled-components');
const { SIZE, LABEL_PLACEMENT } = require('baseui/switch/constants');
const { theme, NAMED, colour, px, cursor, check, font, TEXT, RADII } = require('./radio');

const L = THEMES.Light;

/**
 * The switch's states, each with its values (the default first) and the pseudoclass that carries it: Base Web's $ props,
 * which the Switch mirrors onto every part, and `coarse`, `@media (pointer: coarse)`. $isActive is left out: no style
 * function reads it (checked below).
 */
const AXES = [
  { name: 'checked', values: [false, true], pseudo: () => ':checked' },
  { name: 'disabled', values: [false, true], pseudo: () => ':disabled' },
  { name: 'isHovered', values: [false, true], pseudo: () => ':is-hovered' },
  { name: 'isFocusVisible', values: [false, true], pseudo: () => ':focus-visible' },
  { name: 'size', values: [SIZE.default, SIZE.small], pseudo: (v) => `:size-${v}` },
  { name: 'labelPlacement', values: [LABEL_PLACEMENT.right, LABEL_PLACEMENT.left], pseudo: (v) => `:label-placement-${v}` },
  { name: 'showIcon', values: [false, true], pseudo: () => ':show-icon' },
  { name: 'coarse', values: [false, true], pseudo: () => ':pointer-coarse' },
];

/** Base Web's $ props for a combination (switch.js's sharedProps, and the root's and label's $labelPlacement). */
const props = (c) => ({
  $isFocused: !!c.isFocusVisible, $isFocusVisible: !!c.isFocusVisible, $isHovered: !!c.isHovered, $isActive: !!c.isActive,
  $checked: !!c.checked, $required: false, $disabled: !!c.disabled, $value: '', $showIcon: !!c.showIcon,
  $size: c.size ?? SIZE.default, $labelPlacement: c.labelPlacement ?? LABEL_PLACEMENT.right,
});

/** The declarations a browser applies to the part in that combination. */
function declarations(styled, t, c) {
  const style = evaluate(styled, t, props(c));
  const self = {};
  for (const [k, v] of Object.entries(style)) {
    if (v !== null && typeof v === 'object') {
      if (k !== '@media (pointer: coarse)') throw new Error(`${styled.displayName}: a block this generator doesn't read: ${k}`);
    } else if (v !== undefined) self[k] = v;
  }
  if (c.coarse) Object.assign(self, style['@media (pointer: coarse)'] || {});
  return self;
}

/** 'inset 0 0 0 999px <colour>', Base Web's hover overlay, to its colour (as written), or null for none. */
function overlay(boxShadow, what) {
  if (boxShadow === 'none') return null;
  const m = /^inset 0 0 0 999px (\S+)$/.exec(boxShadow);
  if (!m) throw new Error(`${what} boxShadow ${boxShadow}`);
  return m[1];
}

/** The checkmark Base Web draws as the toggle's background image: its path, colour (as written) and size; null for none. */
function icon(s, what) {
  if (s.backgroundRepeat !== 'no-repeat' || s.backgroundPosition !== 'center') throw new Error(`${what} background ${s.backgroundRepeat} ${s.backgroundPosition}`);
  if (s.backgroundImage === undefined) return null;
  const m = /^url\('data:image\/svg\+xml,(.*)'\);?$/.exec(s.backgroundImage);
  if (!m) throw new Error(`${what} backgroundImage ${s.backgroundImage}`);
  const svg = decodeURIComponent(m[1]);
  const size = /<svg width="(\d+)" height="(\d+)" viewBox="0 0 24 24"/.exec(svg);
  const paths = [...svg.matchAll(/<path d="([^"]+)" fill="([^"]+)"\/>/g)];
  if (!size || size[1] !== size[2] || paths.length !== 1) throw new Error(`${what}: an icon this generator doesn't read: ${svg}`);
  return { path: paths[0][1], fill: paths[0][2], size: Number(size[1]) };
}

/** 'translateX(20px)' as Avalonia's transform text; none is translateX(0px), so the transition has two ends. */
function transform(v, what) {
  if (v === undefined) return 'translateX(0px)';
  const m = /^translateX\((-?[\d.]+)px\)$/.exec(v);
  if (!m) throw new Error(`${what} transform ${v}`);
  return `translateX(${m[1]}px)`;
}

const PARTS = [
  {
    part: 'Root', type: 'b:RadioRootPanel', styled: sw.Root,
    // The label element: its padding takes the pointer, so the panel is drawn, transparent.
    fixed: { Background: 'Transparent' },
    convert(s, c) {
      // verticalAlign is the inline box's place in a line of text; the control's own alignment is the app's.
      check(s, 'Root', ['flexDirection', 'display', 'verticalAlign', 'alignItems', 'cursor', 'userSelect', 'padding']);
      if (s.display !== 'inline-flex' || s.flexDirection !== 'row' || s.alignItems !== 'flex-start') throw new Error('Root box');
      return {
        Orientation: 'Horizontal',
        // switch.js renders the label before the track for the left placement, after it for the right.
        LabelFirst: c.labelPlacement === LABEL_PLACEMENT.left ? 'True' : 'False',
        AlignItems: 'Start',
        Spacing: '0',
        Padding: s.padding === undefined ? '0' : px(s.padding),
        Cursor: cursor(s.cursor),
      };
    },
  },
  {
    part: 'ToggleTrack', type: 'b:RadioMark', styled: sw.ToggleTrack,
    convert(s) {
      check(s, 'ToggleTrack', ['alignItems', 'backgroundColor', ...RADII, 'borderStyle', 'borderWidth', 'borderColor', 'display',
        'flex', 'height', 'width', 'outline', 'outlineOffset', 'paddingTop', 'paddingBottom', 'paddingLeft', 'paddingRight',
        'boxSizing', 'boxShadow']);
      if (s.boxSizing !== 'border-box' || s.display !== 'flex' || s.alignItems !== 'center' || s.flex !== '0 0 auto') throw new Error('ToggleTrack box');
      if (!['solid', 'none'].includes(s.borderStyle)) throw new Error(`ToggleTrack borderStyle ${s.borderStyle}`);
      const radius = s.borderTopLeftRadius;
      for (const k of RADII) if (s[k] !== radius) throw new Error(`ToggleTrack ${k} ${s[k]}`);
      const pads = ['Top', 'Right', 'Bottom', 'Left'].map((side) => s['padding' + side]);
      if (pads.some((x) => x !== pads[0])) throw new Error('ToggleTrack padding');
      const over = overlay(s.boxShadow, 'ToggleTrack');
      const out = {
        Width: px(s.width),
        Height: px(s.height),
        Background: colour(s.backgroundColor, 'ToggleTrack'),
        BorderBrush: colour(s.borderColor, 'ToggleTrack border'),
        BorderThickness: s.borderStyle === 'none' ? '0' : px(s.borderWidth),
        // 999px: RadioMark draws a radius larger than half the box as half the box, as CSS does.
        CornerRadius: px(radius),
        Padding: px(pads[0]),
        Overlay: over === null ? '{x:Null}' : colour(over, 'ToggleTrack overlay'),
        OutlineBrush: '{x:Null}', OutlineThickness: '0', OutlineOffset: '0',
      };
      if (s.outline !== 'none') {
        const m = /^(\S+) solid (@\w+)$/.exec(s.outline);
        if (!m) throw new Error(`ToggleTrack outline ${s.outline}`);
        Object.assign(out, { OutlineBrush: colour(m[2], 'outline'), OutlineThickness: px(m[1]), OutlineOffset: px(s.outlineOffset) });
      }
      return out;
    },
  },
  {
    part: 'Toggle', type: 'b:SwitchToggle', styled: sw.Toggle,
    // At the start of the track's flex row, centred across it (its alignItems center, checked above).
    fixed: { HorizontalAlignment: 'Left', VerticalAlignment: 'Center' },
    convert(s) {
      check(s, 'Toggle', ['backgroundColor', ...RADII, 'boxShadow', 'outline', 'height', 'width', 'transform', 'transition',
        'backgroundImage', 'backgroundRepeat', 'backgroundPosition']);
      for (const k of RADII) if (s[k] !== '50%') throw new Error(`Toggle ${k} ${s[k]}`);
      if (s.outline !== 'none') throw new Error(`Toggle outline ${s.outline}`);
      if (s.width !== s.height) throw new Error('Toggle isn\'t square');
      const over = overlay(s.boxShadow, 'Toggle');
      const mark = icon(s, 'Toggle');
      return {
        Width: px(s.width),
        Height: px(s.height),
        Background: colour(s.backgroundColor, 'Toggle'),
        // 50% of a square.
        CornerRadius: String(css.px(s.width) / 2),
        Overlay: over === null ? '{x:Null}' : colour(over, 'Toggle overlay'),
        RenderTransform: transform(s.transform, 'Toggle'),
        Icon: mark === null ? '{x:Null}' : mark.path,
        IconBrush: mark === null ? '{x:Null}' : colour(mark.fill, 'Toggle icon'),
        IconSize: mark === null ? '0' : String(mark.size),
      };
    },
  },
  {
    // A box around the content presenter, whose text takes the box's font and colour, and wraps.
    part: 'Label', type: 'Border', styled: sw.Label,
    fixed: { [TEXT(true).TextWrapping]: 'Wrap' },
    convert(s) {
      check(s, 'Label', ['verticalAlign', 'paddingLeft', 'paddingRight', 'paddingTop', 'color', 'fontFamily', 'fontSize', 'fontWeight', 'lineHeight']);
      const p = (v) => (v === undefined ? 0 : css.px(v));
      const n = TEXT(true);
      return {
        Padding: `${p(s.paddingLeft)},${p(s.paddingTop)},${p(s.paddingRight)},0`,
        [n.Foreground]: colour(s.color, 'Label'),
        ...font(s, 'Label', n),
      };
    },
  },
];

/** The toggle's transition: its transform, 350ms on Base Web's curve, once the switch has loaded. */
function transition() {
  const s = declarations(sw.Toggle, L, {});
  const m = /^transform (\S+) (cubic-bezier\(.*\))$/.exec(s.transition);
  if (!m) throw new Error(`Toggle transition ${s.transition}`);
  const span = new Date(css.seconds(m[1]) * 1000).toISOString().substr(11, 12);
  const easing = css.cubicBezier(m[2]).join(',');
  // CSS doesn't transition a page's first paint; Avalonia would. So only once the switch has loaded (:loaded).
  return `    <Style Selector="^:loaded">
      <Setter Property="Transitions">
        <Transitions>
          <TransformOperationsTransition Property="RenderTransform" Duration="${span}" Easing="${easing}" />
        </Transitions>
      </Setter>
    </Style>`;
}

/** Base Web passes $isActive to every part, but none reads it: a press changes no style. */
function activeChangesNothing() {
  for (const p of PARTS) {
    for (const checked of [false, true]) {
      const a = JSON.stringify(declarations(p.styled, NAMED, { checked }));
      const b = JSON.stringify(declarations(p.styled, NAMED, { checked, isActive: true }));
      if (a !== b) throw new Error(`${p.part} reads $isActive: add it to the states`);
    }
  }
}

const DESCRIBE = {
  Root: 'the label element: a row, the label before the track for the left placement; on a coarse pointer, padding for a 48 px target.',
  ToggleTrack: 'the rounded track: its fill, its border (none when checked and enabled), the hover overlay and the focus outline.',
  Toggle: 'the knob: its size and fill by state, moved along the track when checked, the hover overlay, and the checkmark with showIcon.',
  Label: 'the switch\'s content, beside the track.',
};

function file(version) {
  activeChangesNothing();
  const themes = PARTS.map((p) => {
    const { plain, blocks } = theme(p, AXES, declarations);
    const extra = p.part === 'Toggle' ? '\n' + transition() : '';
    return `  <!-- ${p.part}: ${DESCRIBE[p.part]} -->
  <ControlTheme x:Key="Switch.Styled${p.part}" TargetType="${p.type}">
${plain.join('\n')}${extra}
${blocks.join('\n')}
  </ControlTheme>`;
  });

  return `<!--
  Generated by tools/tokens/gen.js (tools/tokens/switch.js) from baseui ${version}
  (© Uber Technologies, Inc., MIT). Do not edit.
  The themes of Switch's parts, Switch.Styled<Part>, from Base Web's switch: each part's look by the
  state pseudoclasses the Switch mirrors onto it (Metered.Controls.Switch), the colours by token, the
  lengths read from Base Web's own style functions.
-->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:b="https://github.com/wdcossey/Metered">
${themes.join('\n')}
</ResourceDictionary>
`;
}

module.exports = { file, AXES, PARTS, declarations, props, overlay, icon };
