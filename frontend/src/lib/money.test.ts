import { formatSar } from './money';

describe('formatSar', () => {
  it.each([
    [0, '0.00'],
    [5, '0.05'],
    [1250, '12.50'],
    [100000, '1000.00'],
    [-1725, '-17.25'],
  ])('shows %i halalas as %s', (halalas, text) => {
    expect(formatSar(halalas)).toBe(text);
  });

  it('refuses a fraction of a halala', () => {
    expect(() => formatSar(12.5)).toThrow(RangeError);
  });
});
