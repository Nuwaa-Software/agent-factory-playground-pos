import { computeTotals, discountTypeToNumber, percentToHalalas } from './discount';

describe('discount', () => {
  describe('discountTypeToNumber', () => {
    it('percent → 0', () => expect(discountTypeToNumber('percent')).toBe(0));
    it('fixed → 1', () => expect(discountTypeToNumber('fixed')).toBe(1));
  });

  describe('percentToHalalas', () => {
    it('10% of 1000 → 100', () => expect(percentToHalalas(1000, 10)).toBe(100));
    it('50% of 1000 → 500', () => expect(percentToHalalas(1000, 50)).toBe(500));
    it('3% of 1000 → 30', () => expect(percentToHalalas(1000, 3)).toBe(30));
  });

  describe('computeTotals', () => {
    const lines = [
      { priceHalalas: 1000, quantity: 1 },
    ];

    it('no discount → net=1000, vat=150, total=1150', () => {
      const r = computeTotals(lines, 0);
      expect(r.netHalalas).toBe(1000);
      expect(r.vatHalalas).toBe(Math.floor((1000 * 15 + 50) / 100)); // 150
      expect(r.totalHalalas).toBe(1150);
    });

    it('sale discount only → net reduced', () => {
      const r = computeTotals(lines, 100);
      expect(r.netHalalas).toBe(900);
      expect(r.vatHalalas).toBe(Math.floor((900 * 15 + 50) / 100)); // 135
      expect(r.totalHalalas).toBe(1035);
    });

    it('line discount only → net reduced', () => {
      const r = computeTotals([{ ...lines[0], lineDiscountHalalas: 100 }], 0);
      expect(r.netHalalas).toBe(900);
    });

    it('both discounts → net reduced by both', () => {
      const r = computeTotals([{ ...lines[0], lineDiscountHalalas: 50 }], 50);
      expect(r.netHalalas).toBe(900);
    });

    it('net floored at 0', () => {
      const r = computeTotals(lines, 2000);
      expect(r.netHalalas).toBe(0);
      expect(r.vatHalalas).toBe(0);
      expect(r.totalHalalas).toBe(0);
    });

    it('totalDiscountHalalas sums both', () => {
      const r = computeTotals([{ ...lines[0], lineDiscountHalalas: 50 }], 50);
      expect(r.totalDiscountHalalas).toBe(100);
    });
  });
});
