import ar from './ar.json';
import en from './en.json';

function keys(value: unknown, prefix = ''): string[] {
  if (typeof value !== 'object' || value === null) return [prefix];
  return Object.entries(value).flatMap(([k, v]) => keys(v, prefix ? `${prefix}.${k}` : k));
}

function text(value: unknown, key: string): unknown {
  return key.split('.').reduce<unknown>((node, k) => (node as Record<string, unknown>)[k], value);
}

describe('translations', () => {
  it('every text exists in both Arabic and English', () => {
    expect(keys(ar).sort()).toEqual(keys(en).sort());
  });

  it('no text is empty', () => {
    for (const [name, file] of [['ar', ar], ['en', en]] as const) {
      for (const key of keys(file)) expect(text(file, key), `${name}: ${key}`).toMatch(/\S/);
    }
  });
});
