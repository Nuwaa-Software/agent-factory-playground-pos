/**
 * Discount helpers — mirror the backend so the page stays clean and the math is unit-testable.
 */

export type DiscountType = 'percent' | 'fixed';

/** 0 = percent, 1 = fixed (matches DiscountType enum on the backend). */
export function discountTypeToNumber(type: DiscountType): number {
  return type === 'percent' ? 0 : 1;
}

/** Convert a percentage value to halalas for a given original amount. */
export function percentToHalalas(originalHalalas: number, pct: number): number {
  return Math.floor((originalHalalas * pct + 50) / 100);
}

/** Compute net / vat / total after applying line discounts and an optional sale discount. */
export interface TotalsResult {
  netHalalas: number;
  vatHalalas: number;
  totalHalalas: number;
  totalDiscountHalalas: number;
}

export function computeTotals(
  lines: Array<{ priceHalalas: number; quantity: number; lineDiscountHalalas?: number }>,
  saleDiscountHalalas: number,
): TotalsResult {
  const grossHalalas = lines.reduce((sum, l) => sum + l.priceHalalas * l.quantity, 0);
  const totalLineDiscount = lines.reduce(
    (sum, l) => sum + (l.lineDiscountHalalas ?? 0),
    0,
  );
  let netHalalas = grossHalalas - totalLineDiscount - saleDiscountHalalas;
  if (netHalalas < 0) netHalalas = 0;
  const vatHalalas = Math.floor((netHalalas * 15 + 50) / 100);
  return {
    netHalalas,
    vatHalalas,
    totalHalalas: netHalalas + vatHalalas,
    totalDiscountHalalas: totalLineDiscount + saleDiscountHalalas,
  };
}
