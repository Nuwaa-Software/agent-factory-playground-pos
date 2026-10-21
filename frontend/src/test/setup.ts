import '@testing-library/jest-dom/vitest';
import { configure } from '@testing-library/react';
import * as axeMatchers from 'vitest-axe/matchers';
import type { AxeMatchers } from 'vitest-axe/matchers';
import i18n from '../i18n';

// vitest-axe's own `extend-expect` types the old `Vi.Assertion` global; vitest 4 types matchers on the
// `vitest` module instead.
declare module 'vitest' {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/no-empty-object-type, @typescript-eslint/no-unused-vars
  interface Assertion<T = any> extends AxeMatchers {}
  // eslint-disable-next-line @typescript-eslint/no-empty-object-type
  interface AsymmetricMatchersContaining extends AxeMatchers {}
}

expect.extend(axeMatchers);

// findBy*/waitFor default to 1 s; the sandbox is shared with Postgres and the .NET build.
configure({ asyncUtilTimeout: 5000 });

// Every test starts signed out, in Arabic (the default language), with no fetch stub left over.
beforeEach(async () => {
  localStorage.clear();
  await i18n.changeLanguage('ar');
});

afterEach(() => {
  vi.unstubAllGlobals();
});
