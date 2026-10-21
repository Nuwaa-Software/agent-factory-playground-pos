/**
 * The UI is right-to-left in Arabic: spacing and alignment must use logical classes (`ms-`, `pe-`,
 * `text-start`, `border-s`), which flip with `dir`. A physical one (`ml-`, `text-left`) looks fine in
 * English and wrong in Arabic, and jsdom has no layout to catch it, so the source is checked instead.
 */
const sources = import.meta.glob(['./**/*.tsx', '!./**/*.test.tsx'], {
  query: '?raw',
  import: 'default',
  eager: true,
}) as Record<string, string>;

const PHYSICAL =
  /^(?:[\w-]+:)*-?(?:(?:m|p|scroll-m|scroll-p)[lr]-|(?:left|right)-|rounded-(?:[lr]|[tb][lr])(?:-|$)|border-[lr](?:-|$)|(?:text|float|clear)-(?:left|right)$)/;

function classNames(source: string): string[] {
  const attributes = source.matchAll(/className=(?:"([^"]*)"|\{\s*[`'"]([^`'"]*)[`'"]\s*\})/g);
  return [...attributes].flatMap((m) => (m[1] ?? m[2]).split(/\s+/)).filter(Boolean);
}

describe('right-to-left layout', () => {
  it('finds the components to check', () => {
    expect(Object.keys(sources).length).toBeGreaterThan(3);
  });

  it.each(Object.entries(sources))('%s uses logical directions only', (_, source) => {
    expect(classNames(source).filter((c) => PHYSICAL.test(c))).toEqual([]);
  });

  it('recognises physical classes', () => {
    const bad = ['ml-2', 'md:pr-4', '-mr-1', 'text-left', 'left-0', 'rounded-l-md', 'border-r', 'rounded-tr'];
    const good = ['ms-auto', 'pe-4', 'text-start', 'start-0', 'rounded-s-md', 'border-e', 'px-3', 'border-slate-300'];
    expect(bad.filter((c) => !PHYSICAL.test(c))).toEqual([]);
    expect(good.filter((c) => PHYSICAL.test(c))).toEqual([]);
  });
});
