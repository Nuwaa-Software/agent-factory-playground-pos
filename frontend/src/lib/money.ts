/**
 * Halalas (1 SAR = 100 halalas, always a whole number) as "12.50", like the backend's
 * `InvoiceQr.FormatSar`. Integer maths only: in floating point 0.1 + 0.2 is not 0.3.
 */
export function formatSar(halalas: number): string {
  if (!Number.isSafeInteger(halalas)) throw new RangeError(`halalas must be a whole number, got ${halalas}`);
  const sign = halalas < 0 ? '-' : '';
  const abs = Math.abs(halalas);
  return `${sign}${Math.floor(abs / 100)}.${String(abs % 100).padStart(2, '0')}`;
}
