// The box-shadows Base Web's Button draws, per kind and state, from its own style function
// (baseui/button/styled-components, evaluated by tools/reference/oracle.js), as Avalonia's
// BoxShadows syntax. A BoxShadows value is parsed whole and can't hold a DynamicResource colour, so
// gen.js writes each one as a token (Tokens/ButtonBoxShadows.g.axaml), and theme.js derives the
// same shadows for a theme an app builds.
'use strict';

const css = require('../reference/css');
const { evaluate, resolve } = require('../reference/oracle');
const button = require('baseui/button/styled-components');

const KINDS = ['primary', 'secondary', 'tertiary', 'dangerPrimary', 'dangerSecondary', 'dangerTertiary', 'outline'];

// State order is Base Web's cascade (oracle.js): a later state wins.
const STATES = {
  rest: { props: {}, state: {} },
  selected: { props: { $isSelected: true }, state: {} },
  focusVisible: { props: { $isFocusVisible: true }, state: { focusVisible: true } },
  hover: { props: {}, state: { hover: true } },
  active: { props: {}, state: { active: true } },
};

/** The resource key of one shadow. */
const key = (kind, state, safe) => `button.boxShadow.${kind}.${state}${safe ? '.safe' : ''}`;

/** Every [kind, state, safe] gen.js writes, in its order. */
const ALL = KINDS.flatMap((kind) => Object.keys(STATES).flatMap((s) => [false, true].map((safe) => [kind, s, safe])));

function buttonShadow(theme, kind, stateName, safe) {
  const { props, state } = STATES[stateName];
  const style = evaluate(button.BaseButton, theme, {
    $kind: kind, $size: 'medium', $shape: 'rectangular', $isSelected: false, $isLoading: false,
    $disabled: false, $isFocusVisible: false, $backgroundSafe: safe, $widthType: 'hug', $minHitArea: 'click',
    ...props,
  });
  const { self, after } = resolve(style, state);
  // outline draws its hover and press overlay on ::after: a fill and a ring over the element's own
  // ring. As one BoxShadows list on the element, the ::after's shadows come first (on top).
  const parts = [];
  if (after.boxShadow && after.backgroundColor && after.backgroundColor !== 'transparent') {
    parts.push(`inset 999 999 0 0 ${css.color(after.backgroundColor)}`);
    parts.push(css.boxShadows(after.boxShadow));
  }
  const own = css.boxShadows(self.boxShadow ?? 'none');
  if (own !== 'none') parts.push(own);
  return parts.length ? parts.join(', ') : 'none';
}

module.exports = { KINDS, STATES, ALL, key, buttonShadow };
