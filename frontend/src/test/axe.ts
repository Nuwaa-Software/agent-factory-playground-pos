import { configureAxe } from 'vitest-axe';

/**
 * axe for component tests. Colour contrast is off: jsdom has no canvas, so axe can't measure it there
 * (it only printed "Not implemented: HTMLCanvasElement's getContext()" and checked nothing).
 */
export const axe = configureAxe({ rules: { 'color-contrast': { enabled: false } } });
