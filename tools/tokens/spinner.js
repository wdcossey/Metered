// The theme of Spinner's part, Spinner.StyledSpinner, generated from Base Web's spinner
// (baseui/spinner/styled-components).
//
// Base Web's style function reads three props, each into one property: $size into the box (width and height),
// $borderWidth into the border's width, $color into the top border's colour. So the theme is a plain setter for the
// default, and a state style for each value of each prop the Spinner sets as a pseudoclass (Metered.Controls.Spinner):
// a size's name, a sizing token or a length (bound to the length the spinner reads), and a value a browser drops. The
// style function is evaluated on Base Web's light theme with its colours replaced by their own names ('@contentAccent',
// radio.js's NAMED), so the theme refers to them by token, and each state style's value is read from it.
'use strict';

const css = require('../reference/css');
const { evaluate } = require('../reference/oracle');
const { MEDIUM, valid } = require('../reference/spinner');
const sp = require('baseui/spinner/styled-components');
const { SIZE } = require('baseui/spinner/constants');
const { L, NAMED, colour, check } = require('./radio');

const SIDES = ['Top', 'Right', 'Bottom', 'Left'];
const KNOWN = ['display', 'animationName', 'animationDuration', 'animationIterationCount', 'animationTimingFunction',
  ...SIDES.flatMap((s) => [`border${s}Style`, `border${s}Color`, `border${s}Width`]), 'borderRadius', 'width', 'height', 'cursor'];

/** The style for the props, checked for what this generator reads. */
function style(props) {
  const s = evaluate(sp.StyledSpinner, NAMED, props);
  check(s, 'StyledSpinner', KNOWN);
  if (s.display !== 'block' || s.borderRadius !== '50%' || s.width !== s.height) throw new Error('StyledSpinner box');
  for (const side of SIDES) if (s[`border${side}Style`] !== 'solid' || s[`border${side}Width`] !== s.borderTopWidth) throw new Error(`border ${side}`);
  for (const side of ['Right', 'Bottom', 'Left']) if (s[`border${side}Color`] !== s.borderRightColor) throw new Error(`border ${side} colour`);
  return s;
}

/** A length a browser takes, as Avalonia's; for one it drops, `dropped`. */
const length = (v, dropped) => { const n = valid(v); return n === null ? dropped : String(n); };

/** A number no style of Base Web's has, to see the style function pass a length through. */
const PROBE = 37;

const setter = (prop, value) => `      <Setter Property="${prop}" Value="${value}" />`;
const block = (selector, prop, value) => `    <Style Selector="${selector}">\n${setter(prop, value)}\n    </Style>`;

/**
 * Each state of one prop: [pseudoclass, value]. `read` gives the style's value for the prop's value; `bound` the binding
 * to the length the Spinner resolves for a value that isn't a size's name; `dropped` the value for one a browser drops;
 * `invalid` a value of the prop the style function makes so.
 */
function states(axis, read, bound, dropped, invalid) {
  const out = [];
  for (const name of [SIZE.small, SIZE.medium, SIZE.large]) out.push([`:${axis}-${name}`, length(read(name), dropped)]);
  // Not a name: a sizing token's value from the theme ($theme.sizing[value]), else parseInt's length. The Spinner reads
  // both, the token from the theme it sees, so the theme binds to what it reads.
  for (const token of Object.keys(L.sizing)) {
    if (Object.values(SIZE).includes(token)) throw new Error(`a sizing token named as a size: ${token}`);
    if (read(token) !== L.sizing[token]) throw new Error(`${axis}: ${token} isn't the theme's sizing.${token}`);
  }
  if (read(PROBE) !== `${PROBE}px`) throw new Error(`${axis}: a length isn't passed through: ${read(PROBE)}`);
  out.push([`:${axis}-custom`, bound]);
  const bad = read(invalid);
  if (bad !== 'NaNpx') throw new Error(`${axis}: ${JSON.stringify(invalid)} gives ${bad}, not NaNpx`);
  out.push([`:${axis}-invalid`, dropped]);
  return out;
}

const Box = (v) => style({ $size: v }).width;
// The border by its own value, with a size that is a name: Base Web's `$borderWidth || $size`, for a $borderWidth that is
// something; the Spinner itself sets the size's name for one that isn't.
const Border = (v) => style({ $borderWidth: v, $size: SIZE.medium }).borderTopWidth;

/** The box and the border are independent: each prop moves only its own property (for props that are something). */
function independent() {
  const sizes = [SIZE.small, SIZE.large, 'scale1000', PROBE], borders = [SIZE.small, SIZE.large, 'scale300', PROBE];
  for (const $size of sizes) {
    for (const $borderWidth of borders) {
      const s = style({ $size, $borderWidth });
      if (s.width !== Box($size) || s.borderTopWidth !== Border($borderWidth)) throw new Error(`${$size} × ${$borderWidth} aren't independent`);
    }
  }
  // An unset border with a size that isn't a name: NaNpx, which the Spinner reads as :border-width-invalid.
  if (style({ $size: PROBE }).borderTopWidth !== 'NaNpx') throw new Error('an unset border with a length isn\'t NaNpx');
}

/** The turn, Base Web's `spin` keyframes, as an animation of the angle, which Avalonia turns the element by. */
function turn() {
  const s = style({});
  const deg = (v) => Number(/^rotate\((-?[\d.]+)deg\)$/.exec(v.transform)[1]);
  if (s.animationIterationCount !== 'infinite' || s.animationTimingFunction !== 'linear') throw new Error('spin');
  const span = new Date(css.seconds(s.animationDuration) * 1000).toISOString().substr(11, 12);
  const frame = (cue, v) => `          <KeyFrame Cue="${cue}"><Setter Property="RotateTransform.Angle" Value="${deg(v)}" /></KeyFrame>`;
  // Every turn is the same: a picture of the page can't wait for it to end. So it runs unless :metered-still, which only
  // the tests' PreviewGallery sets (Avalonia 12's animation clock is internal: #13); the ring then stands at 0deg.
  return `    <Style Selector="^:not(:metered-still)">
      <Style.Animations>
        <Animation Duration="${span}" IterationCount="Infinite" Easing="LinearEasing">
${frame('0%', s.animationName.from)}
${frame('100%', s.animationName.to)}
        </Animation>
      </Style.Animations>
    </Style>`;
}

function file(version) {
  independent();
  const s = style({});
  if (s.cursor !== 'wait') throw new Error(`cursor ${s.cursor}`);
  if (style({ $color: '@negative' }).borderTopColor !== '@negative') throw new Error('$color isn\'t the top border');

  const box = states('size', Box, '{Binding $parent[b:Spinner].CustomBoxSize}', 'NaN', 'invalid');
  const border = states('border-width', Border, '{Binding $parent[b:Spinner].CustomBorderSize}', String(MEDIUM), 'invalid');
  const plainBox = length(s.width), plainBorder = length(s.borderTopWidth);
  const blocks = [
    ...box.filter(([, v]) => v !== plainBox).map(([sel, v]) => block('^' + sel, 'BoxSize', v)),
    ...border.filter(([, v]) => v !== plainBorder).map(([sel, v]) => block('^' + sel, 'Thickness', v)),
    block('^:has-color', 'Foreground', '{Binding $parent[b:Spinner].Color}'),
  ];

  return `<!--
  Generated by tools/tokens/gen.js (tools/tokens/spinner.js) from baseui ${version}
  (© Uber Technologies, Inc., MIT). Do not edit.
  The theme of Spinner's part, Spinner.StyledSpinner, from Base Web's spinner: the box, the border and the top's
  colour by the state pseudoclasses the Spinner sets on it (Metered.Controls.Spinner), the colours by token, the
  lengths read from Base Web's own style function.
-->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:b="https://github.com/wdcossey/Metered">
  <!-- Spinner: the ring, its border outside its box (CSS's content box: Base Web sets no box-sizing), turning once a second. -->
  <ControlTheme x:Key="Spinner.StyledSpinner" TargetType="b:SpinnerRing">
    <!-- display: block with a width of its own: at the start of the spinner's box, however the spinner is aligned. -->
    <Setter Property="HorizontalAlignment" Value="Left" />
    <Setter Property="VerticalAlignment" Value="Top" />
    <Setter Property="BoxSize" Value="${plainBox}" />
    <Setter Property="Thickness" Value="${plainBorder}" />
    <Setter Property="Foreground" Value="${colour(s.borderTopColor, 'top')}" />
    <Setter Property="Background" Value="${colour(s.borderRightColor, 'rest')}" />
    <Setter Property="Cursor" Value="Wait" />
${turn()}
${blocks.join('\n')}
  </ControlTheme>
</ResourceDictionary>
`;
}

module.exports = { file };
