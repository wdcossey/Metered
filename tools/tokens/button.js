// The themes of Button's parts, Button.Styled<Part>, generated from Base Web's own Button
// (baseui/button/styled-components). Two sources, each checked against the other by the tests'
// oracle:
//  - which colour token each kind and state uses: a table, ported line by line from
//    getColorStyles, getDisabledStyles and getLoadingSpinnerColors, so the theme refers to the
//    tokens by name (DynamicResource) and follows the theme variant;
//  - every length (padding, sizes, margins, radii): read from Base Web's style functions, evaluated
//    for each size and shape (oracle.js), so no number here is typed by hand.
'use strict';

const css = require('../reference/css');
const { THEMES, evaluate, resolve } = require('../reference/oracle');
const button = require('baseui/button/styled-components');

const L = THEMES.Light;

// getColorStyles / getLoadingSpinnerColors, per kind: [fill, text, selectedFill, selectedText, loadingFill, spinnerFg, spinnerBg].
const KINDS = {
  primary: ['buttonPrimaryFill', 'buttonPrimaryText', 'buttonPrimarySelectedFill', 'buttonPrimarySelectedText', 'buttonPrimaryLoadingFill', 'buttonPrimarySpinnerForeground', 'buttonPrimarySpinnerBackground'],
  // A selected secondary button is drawn as a primary one.
  secondary: ['buttonSecondaryFill', 'buttonSecondaryText', 'buttonPrimaryFill', 'buttonPrimaryText', 'buttonSecondaryLoadingFill', 'buttonSecondarySpinnerForeground', 'buttonSecondarySpinnerBackground'],
  tertiary: ['buttonTertiaryFill', 'buttonTertiaryText', 'buttonTertiarySelectedFill', 'buttonTertiarySelectedText', 'buttonTertiaryLoadingFill', 'buttonTertiarySpinnerForeground', 'buttonTertiarySpinnerBackground'],
  dangerPrimary: ['buttonDangerPrimaryFill', 'buttonDangerPrimaryText', 'buttonDangerPrimarySelectedFill', 'buttonDangerPrimarySelectedText', 'buttonDangerPrimaryLoadingFill', 'buttonDangerPrimarySpinnerForeground', 'buttonDangerPrimarySpinnerBackground'],
  dangerSecondary: ['buttonDangerSecondaryFill', 'buttonDangerSecondaryText', 'buttonDangerSecondarySelectedFill', 'buttonDangerSecondarySelectedText', 'buttonDangerSecondaryLoadingFill', 'buttonDangerSecondarySpinnerForeground', 'buttonDangerSecondarySpinnerBackground'],
  dangerTertiary: ['buttonDangerTertiaryFill', 'buttonDangerTertiaryText', 'buttonDangerTertiarySelectedFill', 'buttonDangerTertiarySelectedText', 'buttonDangerTertiaryLoadingFill', 'buttonDangerTertiarySpinnerForeground', 'buttonDangerTertiarySpinnerBackground'],
  outline: ['buttonOutlineFill', 'buttonOutlineText', 'buttonOutlineSelectedFill', 'buttonOutlineSelectedText', 'buttonOutlineLoadingFill', 'buttonOutlineSpinnerForeground', 'buttonOutlineSpinnerBackground'],
};
// getDisabledStyles: disabled and selected, for these kinds only.
const DISABLED_SELECTED = {
  primary: ['buttonDisabledActiveFill', 'buttonDisabledActiveText'],
  secondary: ['buttonDisabledActiveFill', 'buttonDisabledActiveText'],
  tertiary: ['buttonTertiaryDisabledActiveFill', 'buttonTertiaryDisabledActiveText'],
};
// getFontStyles.
const FONTS = { xsmall: 'LabelXSmall', small: 'LabelSmall', medium: 'LabelMedium', large: 'LabelLarge' };
const SIZES = { xsmall: 'xSmall', small: 'small', medium: 'medium', large: 'large' };

for (const names of Object.values(KINDS).concat(Object.values(DISABLED_SELECTED))) {
  for (const n of names) if (!(n in L.colors)) throw new Error(`No colour token ${n}`);
}
for (const n of ['buttonDisabledFill', 'buttonDisabledText', 'buttonDisabledSpinnerForeground', 'buttonDisabledSpinnerBackground']) {
  if (!(n in L.colors)) throw new Error(`No colour token ${n}`);
}

const props = (extra) => ({
  $kind: 'primary', $size: 'medium', $shape: 'rectangular', $isSelected: false, $isLoading: false,
  $disabled: false, $isFocusVisible: false, $backgroundSafe: false, $widthType: 'hug', $minHitArea: 'click', ...extra,
});
const base = (extra) => resolve(evaluate(button.BaseButton, L, props(extra)), {}).self;

const r = (key) => `{DynamicResource ${key}}`;
const setter = (prop, value) => `      <Setter Property="${prop}" Value="${value}" />`;
const style = (selector, setters) => `    <Style Selector="${selector}">\n${setters.join('\n')}\n    </Style>`;
const thickness = (s, prefix) => {
  const [t, rt, b, l] = ['Top', 'Right', 'Bottom', 'Left'].map((side) => css.px(s[prefix + side] ?? 0));
  return `${l},${t},${rt},${b}`;
};
const same = (a, b, what) => { if (a !== b) throw new Error(`${what}: ${a} vs ${b}`); return a; };

function baseButtonTheme() {
  const blocks = [];
  const k = (kind) => `:kind-${kind}`;

  // Layout: getDimensionStyles, getPaddingStyles, getShapeStyles, getBorderRadiusStyles, getFontStyles.
  blocks.push(style('^', [
    setter('CornerRadius', r('borders.buttonBorderRadius')),
    setter('TextElement.FontFamily', r('typography.LabelMedium.fontFamily')),
    setter('Cursor', 'Hand'),
  ]));
  for (const [cls, size] of Object.entries(SIZES)) {
    const font = FONTS[cls];
    const plain = base({ $size: size });
    const icon = base({ $size: size, $shape: 'round' });
    const fixed = base({ $size: size, $shape: 'square' });
    const fill = base({ $size: size, $widthType: 'fill' });
    blocks.push(style(`^:size-${cls}`, [
      setter('Padding', thickness(plain, 'padding')),
      setter('TextElement.FontSize', r(`typography.${font}.fontSize`)),
      setter('TextElement.FontWeight', r(`typography.${font}.fontWeight`)),
      setter('TextBlock.LineHeight', r(`typography.${font}.lineHeight`)),
    ]));
    blocks.push(style(`^:size-${cls}:icon-shape`, [setter('Padding', thickness(icon, 'padding'))]));
    blocks.push(style(`^:size-${cls}:fixed-shape`, [
      setter('Padding', thickness(fixed, 'padding')),
      setter('Width', css.px(fixed.width)),
      setter('Height', same(css.px(fixed.height), css.px(fixed.width), 'square')),
    ]));
    blocks.push(style(`^:size-${cls}:width-fill`, [
      setter('MinWidth', css.px(fill.minWidth)),
      setter('MinHeight', css.px(fill.minHeight)),
    ]));
  }
  const pill = css.px(base({ $shape: 'pill' }).borderTopLeftRadius);
  blocks.push(style('^:shape-pill', [setter('CornerRadius', pill)]));
  // circle and round are 50%: on a square box, a radius of half its side, which a larger radius clamps to.
  same(base({ $shape: 'circle' }).borderTopLeftRadius, '50%', 'circle radius');
  blocks.push(style('^:shape-circle', [setter('CornerRadius', pill)]));
  blocks.push(style('^:shape-round', [setter('CornerRadius', pill)]));

  // Colours, in Base Web's cascade order: a later style wins.
  for (const [kind, [fill, text]] of Object.entries(KINDS)) {
    blocks.push(style(`^${k(kind)}`, [
      setter('Background', r(`colors.${fill}`)),
      setter('TextElement.Foreground', r(`colors.${text}`)),
      setter('BoxShadow', r(`button.boxShadow.${kind}.rest`)),
    ]));
    blocks.push(style(`^${k(kind)}:background-safe`, [setter('BoxShadow', r(`button.boxShadow.${kind}.rest.safe`))]));
  }
  for (const [kind, [, , , , loading]] of Object.entries(KINDS)) {
    blocks.push(style(`^${k(kind)}:is-loading`, [setter('Background', r(`colors.${loading}`))]));
  }
  for (const [kind, [, , selFill, selText]] of Object.entries(KINDS)) {
    blocks.push(style(`^${k(kind)}:is-selected`, [
      setter('Background', r(`colors.${selFill}`)),
      setter('TextElement.Foreground', r(`colors.${selText}`)),
      setter('BoxShadow', r(`button.boxShadow.${kind}.selected`)),
    ]));
    blocks.push(style(`^${k(kind)}:is-selected:background-safe`, [setter('BoxShadow', r(`button.boxShadow.${kind}.selected.safe`))]));
  }
  for (const state of ['focusVisible', 'hover', 'active']) {
    const pseudo = { focusVisible: ':focus-visible', hover: ':hover', active: ':active' }[state];
    // Hover and press overlays come from getColorStyles, which gives a selected or loading button none.
    const gate = state === 'focusVisible' ? '' : ':not(:is-selected):not(:is-loading)';
    for (const kind of Object.keys(KINDS)) {
      blocks.push(style(`^${k(kind)}${pseudo}${gate}`, [setter('BoxShadow', r(`button.boxShadow.${kind}.${state}`))]));
      blocks.push(style(`^${k(kind)}${pseudo}${gate}:background-safe`, [setter('BoxShadow', r(`button.boxShadow.${kind}.${state}.safe`))]));
    }
  }
  // :disabled: getDisabledStyles; the double border and the overlays are gone, the safe shadow stays.
  blocks.push(style('^:disabled', [
    setter('Background', r('colors.buttonDisabledFill')),
    setter('TextElement.Foreground', r('colors.buttonDisabledText')),
    setter('BoxShadow', 'none'),
    setter('Cursor', 'No'),
  ]));
  blocks.push(style('^:disabled:background-safe', [setter('BoxShadow', r('lighting.shallowBelow'))]));
  for (const [kind, [fill, text]] of Object.entries(DISABLED_SELECTED)) {
    blocks.push(style(`^${k(kind)}:disabled:is-selected`, [
      setter('Background', r(`colors.${fill}`)),
      setter('TextElement.Foreground', r(`colors.${text}`)),
    ]));
  }

  // transition: background 200ms linear.
  same(base({}).transitionProperty, 'background', 'transition');
  // CSS doesn't transition a page's first paint; Avalonia would, from no background. So the
  // transition is set only once the button has loaded (:loaded, Metered.Controls.Button).
  const transitions = `    <Style Selector="^:loaded">
      <Setter Property="Transitions">
        <Transitions>
          <BrushTransition Property="Background" Duration="0:0:0.2" Easing="0,0,1,1" />
        </Transitions>
      </Setter>
    </Style>`;
  same(base({}).transitionDuration, '200ms', 'transition duration');
  same(base({}).transitionTimingFunction, 'cubic-bezier(0, 0, 1, 1)', 'transition curve');

  return `  <!-- BaseButton: the button itself (Base Web's StyledBaseButton). Root is the same part. -->
  <ControlTheme x:Key="Button.StyledBaseButton" TargetType="Border">
${transitions}
${blocks.join('\n')}
  </ControlTheme>`;
}

function enhancerThemes() {
  const start = resolve(evaluate(button.StartEnhancer, L, props({})), {}).self;
  const end = resolve(evaluate(button.EndEnhancer, L, props({})), {}).self;
  const container = resolve(evaluate(button.StartEnhancerButtonContentContainer, L, props({})), {}).self;
  same(container.flexGrow, 1, 'content container grows');
  return `  <!-- StartEnhancer and EndEnhancer: flex boxes beside the content. -->
  <ControlTheme x:Key="Button.StyledStartEnhancer" TargetType="ContentPresenter">
    <Setter Property="Margin" Value="0,0,${css.px(start.marginRight)},0" />
    <Setter Property="VerticalAlignment" Value="Center" />
  </ControlTheme>
  <ControlTheme x:Key="Button.StyledEndEnhancer" TargetType="ContentPresenter">
    <Setter Property="Margin" Value="${css.px(end.marginLeft)},0,0,0" />
    <Setter Property="VerticalAlignment" Value="Center" />
  </ControlTheme>
  <!-- StartEnhancerButtonContentContainer: the start enhancer and the content, centred, taking the room the end enhancer leaves. -->
  <ControlTheme x:Key="Button.StyledStartEnhancerButtonContentContainer" TargetType="StackPanel">
    <Setter Property="Orientation" Value="Horizontal" />
    <Setter Property="HorizontalAlignment" Value="Center" />
    <Setter Property="VerticalAlignment" Value="Center" />
  </ControlTheme>`;
}

function spinnerThemes() {
  const blocks = [];
  for (const [cls, size] of Object.entries(SIZES)) {
    const s = resolve(evaluate(button.LoadingSpinner, L, props({ $size: size })), {}).self;
    blocks.push(style(`^:size-${cls}`, [
      setter('Width', css.px(s.width)),
      setter('Height', same(css.px(s.height), css.px(s.width), 'spinner')),
      setter('Thickness', css.px(s.borderTopWidth)),
    ]));
  }
  for (const [kind, names] of Object.entries(KINDS)) {
    blocks.push(style(`^:kind-${kind}`, [
      setter('Foreground', r(`colors.${names[5]}`)),
      setter('Background', r(`colors.${names[6]}`)),
    ]));
  }
  blocks.push(style('^:disabled', [
    setter('Foreground', r('colors.buttonDisabledSpinnerForeground')),
    setter('Background', r('colors.buttonDisabledSpinnerBackground')),
  ]));
  const spin = resolve(evaluate(button.LoadingSpinner, L, props({})), {}).self;
  same(spin.animationDuration, '700ms', 'spin duration');
  same(spin.animationIterationCount, 'infinite', 'spin count');
  same(spin.animationTimingFunction, 'linear', 'spin curve');
  // The keyframes are an object, which resolve() leaves out: read from the style function's own output.
  const keyframes = evaluate(button.LoadingSpinner, L, props({})).animationName;
  same(keyframes.from.transform, 'rotate(0deg)', 'spin from');
  same(keyframes.to.transform, 'rotate(360deg)', 'spin to');
  // Every turn is the same: a picture of the page can't wait for it to end. So it runs unless :metered-still, which only
  // the tests' PreviewGallery sets (Avalonia 12's animation clock is internal: #13); the ring then stands at 0deg, as
  // Spinner's does (tools/tokens/spinner.js).
  blocks.push(`    <Style Selector="^:is-loading:not(:metered-still)">
      <Style.Animations>
        <Animation Duration="0:0:0.7" IterationCount="Infinite">
          <KeyFrame Cue="0%"><Setter Property="RotateTransform.Angle" Value="0" /></KeyFrame>
          <KeyFrame Cue="100%"><Setter Property="RotateTransform.Angle" Value="360" /></KeyFrame>
        </Animation>
      </Style.Animations>
    </Style>`);

  const containers = [];
  for (const [cls, size] of Object.entries(SIZES)) {
    const c = resolve(evaluate(button.LoadingSpinnerContainer, L, props({ $size: size })), {}).self;
    containers.push(style(`^:size-${cls}`, [setter('Margin', thickness(c, 'margin'))]));
  }

  return `  <!-- LoadingSpinnerContainer: holds the spinner, in place of the content while loading. -->
  <ControlTheme x:Key="Button.StyledLoadingSpinnerContainer" TargetType="Panel">
    <Setter Property="IsVisible" Value="False" />
    <Setter Property="HorizontalAlignment" Value="Center" />
    <Setter Property="VerticalAlignment" Value="Center" />
${containers.join('\n')}
    <Style Selector="^:is-loading">
      <Setter Property="IsVisible" Value="True" />
    </Style>
  </ControlTheme>
  <!-- LoadingSpinner: the ring, turning once each 700 ms. -->
  <ControlTheme x:Key="Button.StyledLoadingSpinner" TargetType="b:ButtonLoadingSpinner">
    <Setter Property="RenderTransform" Value="rotate(0deg)" />
${blocks.join('\n')}
  </ControlTheme>`;
}

function file(version) {
  return `<!--
  Generated by tools/tokens/gen.js (tools/tokens/button.js) from baseui ${version}
  (© Uber Technologies, Inc., MIT). Do not edit.
  The themes of Button's parts, Button.Styled<Part>: each part's look by the state pseudoclasses
  the Button mirrors onto it (Metered.Controls.Button), the colours by token, the lengths read from
  Base Web's own style functions.
-->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:b="https://github.com/wdcossey/Metered">
${baseButtonTheme()}
${enhancerThemes()}
${spinnerThemes()}
</ResourceDictionary>
`;
}

module.exports = { file, KINDS, DISABLED_SELECTED, FONTS };
