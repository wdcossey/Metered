// Base Web's own style functions, evaluated. Each styled component in the baseui package keeps
// its style function (styletron's reducer); calling it with a theme and the $ props gives the
// exact style object Base Web renders. This file resolves that object for one interaction state
// into the flat set of declarations a browser would apply, using the cascade measured on
// baseweb.design by precedence.js (2026-11-15): :active beats :hover, :hover beats
// :focus-visible, and :focus-visible beats the plain declarations. :hover is inside
// @media (hover: hover), so it applies only to a pointer that hovers (a mouse), never to touch.
'use strict';

const { LightTheme, DarkTheme } = require('baseui/themes');

const THEMES = { Light: LightTheme, Dark: DarkTheme };

/** Calls a styled component's style function with the theme and props. */
function evaluate(styled, theme, props) {
  const s = styled.__STYLETRON__;
  if (!s || !s.reducers || s.reducers.length !== 1) {
    throw new Error(`${styled.displayName}: not a styletron component with one style function`);
  }
  return s.reducers[0].reducer(s.getInitialStyle(), { $theme: theme, ...props });
}

/**
 * Resolves a style object for one state. `state` has: hover (a mouse over it), active (pressed),
 * focusVisible, disabled. Returns { self, before, after }: the declarations on the element and on
 * its ::before and ::after.
 */
function resolve(style, state) {
  const self = {}, before = {}, after = {};
  const take = (target, block) => {
    for (const [k, v] of Object.entries(block || {})) {
      if (v === null || typeof v !== 'object') target[k] = v;
    }
  };
  take(self, style);
  take(before, style['::before']);
  take(after, style['::after']);
  if (state.focusVisible) take(self, style[':focus-visible']);
  if (state.hover) {
    const media = style['@media (hover: hover)'] || {};
    take(self, media[':hover']);
    take(after, media[':hover::after']);
  }
  if (state.active) {
    take(self, style[':active']);
    take(after, style[':active::after']);
  }
  if (state.disabled) take(self, style[':disabled']);
  return { self, before, after };
}

module.exports = { THEMES, evaluate, resolve };
