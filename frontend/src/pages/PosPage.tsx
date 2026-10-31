import { useCallback, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import type { Product } from '../api/products';
import { createSale, type DiscountRequest, type SaleResponse } from '../api/sales';
import { apiGet } from '../api/client';
import { formatSar } from '../lib/money';
import { computeTotals } from '../lib/discount';
import Button from '../ui/Button';
import TextField from '../ui/TextField';

interface CartLine {
  productId: string;
  nameEn: string;
  nameAr: string;
  sku: string;
  priceHalalas: number;
  stock: number;
  quantity: number;
}

interface LineDiscountState {
  type: 'percent' | 'fixed';
  value: string; // keep as string in the input
  reason: string;
}

interface SaleDiscountState {
  type: 'percent' | 'fixed';
  value: string;
  reason: string;
}

function emptyLineDiscount(): LineDiscountState {
  return { type: 'percent', value: '', reason: '' };
}

function emptySaleDiscount(): SaleDiscountState {
  return { type: 'percent', value: '', reason: '' };
}

/** Build a DiscountRequest from a line discount state, or undefined if no discount. */
function buildLineDiscount(
  _line: CartLine,
  state: LineDiscountState,
): DiscountRequest | undefined {
  const v = parseFloat(state.value);
  if (!state.value.trim() || isNaN(v) || v <= 0) return undefined;
  let type: number;
  let value: number;
  if (state.type === 'percent') {
    if (v > 100) return undefined; // validation error, don't submit
    type = 0;
    value = v;
  } else {
    type = 1;
    value = Math.round(v * 100); // SAR → halalas
  }
  return { type, value, reason: state.reason };
}

/** Build a DiscountRequest from the sale discount state, or undefined if no discount. */
function buildSaleDiscount(state: SaleDiscountState): DiscountRequest | undefined {
  const v = parseFloat(state.value);
  if (!state.value.trim() || isNaN(v) || v <= 0) return undefined;
  let type: number;
  let value: number;
  if (state.type === 'percent') {
    if (v > 100) return undefined;
    type = 0;
    value = v;
  } else {
    type = 1;
    value = Math.round(v * 100);
  }
  return { type, value, reason: state.reason };
}

export default function PosPage() {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const lang = i18n.language as 'ar' | 'en';

  const [searchQuery, setSearchQuery] = useState('');
  const [results, setResults] = useState<Product[]>([]);
  const [searching, setSearching] = useState(false);
  const [searchError, setSearchError] = useState<string | null>(null);
  const [cart, setCart] = useState<CartLine[]>([]);
  const [paymentType, setPaymentType] = useState(0);
  const [submitting, setSubmitting] = useState(false);
  const [lastSale, setLastSale] = useState<SaleResponse | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);

  // Discount state
  const [saleDiscountState, setSaleDiscountState] = useState<SaleDiscountState>(emptySaleDiscount());
  const [saleDiscountExpanded, setSaleDiscountExpanded] = useState(false);
  const [lineDiscountStates, setLineDiscountStates] = useState<Record<string, LineDiscountState>>({});
  const [expandedLines, setExpandedLines] = useState<Record<string, boolean>>({});
  const [appliedLineDiscounts, setAppliedLineDiscounts] = useState<Record<string, number>>({}); // halalas
  const [lineDiscountError, setLineDiscountError] = useState<string | null>(null);
  const [saleDiscountError, setSaleDiscountError] = useState<string | null>(null);

  const productLabel = (p: { nameEn: string; nameAr: string }) => (lang === 'ar' ? p.nameAr : p.nameEn);

  const handleSearch = useCallback(async () => {
    if (!searchQuery.trim()) return;
    setSearching(true);
    setSearchError(null);
    try {
      const data = await apiGet<Product[]>(`/api/inventory/products?q=${encodeURIComponent(searchQuery)}`);
      setResults(data);
    } catch {
      setSearchError(t('pos.searchError'));
      setResults([]);
    } finally {
      setSearching(false);
    }
  }, [searchQuery, t]);

  const handleAddProduct = useCallback((product: Product) => {
    setCart(prev => {
      const idx = prev.findIndex(l => l.productId === product.id);
      if (idx >= 0) {
        const updated = [...prev];
        updated[idx] = { ...updated[idx], quantity: Math.min(updated[idx].quantity + 1, 999) };
        return updated;
      }
      return [
        ...prev,
        {
          productId: product.id,
          nameEn: product.nameEn,
          nameAr: product.nameAr,
          sku: product.sku,
          priceHalalas: product.priceHalalas,
          stock: product.stock,
          quantity: 1,
        },
      ];
    });
    setLastSale(null);
    setSubmitError(null);
  }, []);

  const updateQuantity = useCallback((productId: string, delta: number) => {
    setCart(prev =>
      prev.map(line =>
        line.productId === productId
          ? { ...line, quantity: Math.max(1, Math.min(999, line.quantity + delta)) }
          : line,
      ),
    );
    setLastSale(null);
  }, []);

  const removeLine = useCallback((productId: string) => {
    setCart(prev => prev.filter(l => l.productId !== productId));
    setAppliedLineDiscounts(prev => {
      const copy = { ...prev };
      delete copy[productId];
      return copy;
    });
    setLastSale(null);
  }, []);

  const clearCart = useCallback(() => {
    setCart([]);
    setSaleDiscountState(emptySaleDiscount());
    setSaleDiscountExpanded(false);
    setLineDiscountStates({});
    setExpandedLines({});
    setAppliedLineDiscounts({});
    setLastSale(null);
    setSubmitError(null);
    setSearchQuery('');
    setResults([]);
  }, []);

  // Live totals (discount-aware)
  const lineDiscountHalalas = useMemo(() => {
    const result: Record<string, number> = {};
    for (const line of cart) {
      const applied = appliedLineDiscounts[line.productId];
      if (applied) result[line.productId] = applied;
    }
    return result;
  }, [cart, appliedLineDiscounts]);

  const totals = useMemo(() => {
    const linesWithDiscounts = cart.map(line => ({
      priceHalalas: line.priceHalalas,
      quantity: line.quantity,
      lineDiscountHalalas: lineDiscountHalalas[line.productId],
    }));
    // For sale discount in percent, we need to compute halalas from the gross
    let sdHalalas = 0;
    if (saleDiscountState.value.trim()) {
      const v = parseFloat(saleDiscountState.value);
      if (!isNaN(v) && v > 0) {
        if (saleDiscountState.type === 'percent') {
          const gross = linesWithDiscounts.reduce((s, l) => s + l.priceHalalas * l.quantity, 0);
          sdHalalas = Math.floor((gross * v + 50) / 100);
        } else {
          sdHalalas = Math.round(v * 100);
        }
      }
    }
    return computeTotals(linesWithDiscounts, sdHalalas);
  }, [cart, lineDiscountHalalas, saleDiscountState]);

  const handleSubmit = useCallback(async () => {
    if (cart.length === 0) return;
    setSubmitting(true);
    setSubmitError(null);
    try {
      // Validate reason non-empty for any applied discount
      if (saleDiscountState.value.trim()) {
        const v = parseInt(saleDiscountState.value, 10);
        if (!isNaN(v) && v > 0 && saleDiscountState.type === 'percent' && v > 100) {
          setSubmitError(t('discount.invalid'));
          setSubmitting(false);
          return;
        }
        if (!saleDiscountState.reason.trim()) {
          setSaleDiscountError(t('discount.reasonRequired'));
          setSubmitting(false);
          return;
        }
      }

      // Validate line discount reasons at submit time
      for (const line of cart) {
        const ld = buildLineDiscount(line, lineDiscountStates[line.productId] ?? emptyLineDiscount());
        if (ld && !ld.reason.trim()) {
          setSubmitError(t('discount.reasonRequired'));
          setSubmitting(false);
          return;
        }
      }

      const sale = await createSale({
        lines: cart.map(l => {
          const ld = buildLineDiscount(l, lineDiscountStates[l.productId] ?? emptyLineDiscount());
          return ld ? { productId: l.productId, quantity: l.quantity, discount: ld } : { productId: l.productId, quantity: l.quantity };
        }),
        paymentType,
        ...(saleDiscountState.value.trim() && { saleDiscount: buildSaleDiscount(saleDiscountState) }),
      });
      setLastSale(sale);
    } catch (err: unknown) {
      if (err instanceof Error && 'status' in err && 'code' in err) {
        const apiErr = err as { status: number; code?: string };
        if (apiErr.status === 400) {
          switch (apiErr.code) {
            case 'discount.exceeds_limits':
              setSubmitError(t('discount.exceedsLimits'));
              break;
            case 'discount.value_must_be_positive':
              setSubmitError(t('discount.valueMustBePositive'));
              break;
            case 'discount.invalid':
              setSubmitError(t('discount.invalid'));
              break;
            default:
              setSubmitError(t('pos.submitError'));
          }
        } else {
          setSubmitError(t('pos.submitError'));
        }
      } else {
        setSubmitError(t('pos.submitError'));
      }
    } finally {
      setSubmitting(false);
    }
  }, [cart, paymentType, saleDiscountState, lineDiscountStates, t]);

  const handleNewSale = useCallback(() => {
    setCart([]);
    setPaymentType(0);
    setLastSale(null);
    setSubmitError(null);
    setSearchQuery('');
    setResults([]);
  }, []);

  // Line discount helpers
  const toggleLineDiscount = useCallback((productId: string) => {
    setExpandedLines(prev => ({ ...prev, [productId]: !prev[productId] }));
    if (!expandedLines[productId]) {
      setLineDiscountStates(prev => ({
        ...prev,
        [productId]: prev[productId] ?? emptyLineDiscount(),
      }));
    }
  }, [expandedLines]);

  const updateLineDiscountState = useCallback((productId: string, updates: Partial<LineDiscountState>) => {
    setLineDiscountStates(prev => ({
      ...prev,
      [productId]: { ...(prev[productId] ?? emptyLineDiscount()), ...updates },
    }));
  }, []);

  const applyLineDiscount = useCallback((productId: string) => {
    const state = lineDiscountStates[productId];
    if (!state) return;
    const v = parseInt(state.value, 10);
    if (isNaN(v) || v <= 0) {
      setLineDiscountError(t('discount.valueMustBePositive'));
      return;
    }
    if (state.type === 'percent' && v > 100) {
      setLineDiscountError(t('discount.invalid'));
      return;
    }
    if (!state.reason.trim()) {
      setLineDiscountError(t('discount.reasonRequired'));
      return;
    }
    // Compute halalas
    let discountHalalas: number;
    const line = cart.find(l => l.productId === productId);
    if (!line) return;
    if (state.type === 'percent') {
      discountHalalas = Math.floor((line.priceHalalas * line.quantity * v + 50) / 100);
    } else {
      discountHalalas = Math.round(v * 100);
    }
    setAppliedLineDiscounts(prev => ({ ...prev, [productId]: discountHalalas }));
    setExpandedLines(prev => ({ ...prev, [productId]: false }));
    setLineDiscountError(null);
  }, [lineDiscountStates, cart, t]);

  const removeLineDiscount = useCallback((productId: string) => {
    setAppliedLineDiscounts(prev => {
      const copy = { ...prev };
      delete copy[productId];
      return copy;
    });
    setExpandedLines(prev => ({ ...prev, [productId]: false }));
  }, []);

  // Sale discount helpers
  const applySaleDiscount = useCallback(() => {
    const v = parseInt(saleDiscountState.value, 10);
    if (isNaN(v) || v <= 0) {
      setSaleDiscountError(t('discount.valueMustBePositive'));
      return;
    }
    if (saleDiscountState.type === 'percent' && v > 100) {
      setSaleDiscountError(t('discount.invalid'));
      return;
    }
    if (!saleDiscountState.reason.trim()) {
      setSaleDiscountError(t('discount.reasonRequired'));
      return;
    }
    setSaleDiscountExpanded(false);
    setSaleDiscountError(null);
  }, [saleDiscountState, t]);

  const removeSaleDiscount = useCallback(() => {
    setSaleDiscountState(emptySaleDiscount());
    setSaleDiscountExpanded(false);
    setSaleDiscountError(null);
  }, []);

  // Check if any line has an applied discount
  const hasLineDiscounts = Object.keys(appliedLineDiscounts).length > 0;
  const hasSaleDiscount = saleDiscountState.value.trim() !== '';

  return (
    <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
      {/* Left pane: search + results */}
      <section aria-labelledby="pos-search-title" className="space-y-3">
        <h2 id="pos-search-title" className="text-lg font-semibold">{t('pos.title')}</h2>

        <div className="flex gap-2">
          <label htmlFor="pos-search-input" className="sr-only">{t('pos.searchLabel')}</label>
          <input
            id="pos-search-input"
            type="search"
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
            onKeyDown={e => { if (e.key === 'Enter') handleSearch(); }}
            placeholder={t('pos.searchPlaceholder')}
            className="min-h-11 flex-1 rounded-md border border-slate-300 px-3"
          />
          <button
            type="button"
            onClick={handleSearch}
            disabled={searching || !searchQuery.trim()}
            className="min-h-11 rounded-md bg-teal-600 px-4 text-white hover:bg-teal-700 disabled:opacity-50"
          >
            {searching ? t('pos.searching') : t('pos.search')}
          </button>
        </div>

        {/* Search error */}
        {searchError && (
          <p role="alert" className="text-sm text-red-600">{searchError}</p>
        )}

        {/* Results */}
        {!searching && results.length === 0 && searchQuery.trim() && !searchError && (
          <p className="text-sm text-slate-500">{t('pos.noResults')}</p>
        )}

        <div className="space-y-2">
          {results.map(product => (
            <button
              key={product.id}
              type="button"
              onClick={() => handleAddProduct(product)}
              disabled={product.stock <= 0}
              className="flex w-full items-center justify-between rounded-md border border-slate-200 px-3 py-2 text-start hover:bg-slate-50 disabled:opacity-40"
            >
              <span>
                <bdi>{productLabel(product)}</bdi>
                {' '}
                <span className="text-sm text-slate-500">
                  ({t('pos.inStock', { count: product.stock })})
                </span>
              </span>
              <span className="font-medium">{formatSar(product.priceHalalas)}</span>
            </button>
          ))}
        </div>
      </section>

      {/* Right pane: cart + totals + payment + submit */}
      <section aria-labelledby="pos-cart-title" className="space-y-3">
        <h2 id="pos-cart-title" className="text-lg font-semibold">{t('pos.cartTitle')}</h2>

        {lastSale ? (
          /* Success view */
          <div className="space-y-2 rounded-md border border-green-200 bg-green-50 p-4">
            <p className="font-semibold text-green-800">{t('pos.successTitle')}</p>
            <p>{t('pos.successSale', { number: lastSale.number })}</p>
            <p>{t('pos.successTotal', { amount: formatSar(lastSale.totalHalalas) })}</p>
            {/* Sale discount in success view */}
            {lastSale.saleDiscountHalalas && lastSale.saleDiscountHalalas > 0 && (
              <div className="flex justify-between text-sm text-slate-600">
                <span>{t('discount.saleDiscount')}:</span>
                <span>-{formatSar(lastSale.saleDiscountHalalas)}</span>
              </div>
            )}
            <div className="flex gap-2">
              <button
                type="button"
                onClick={() => navigate(`/sales/${lastSale.id}/receipt`)}
                className="min-h-11 rounded-md bg-teal-600 px-4 text-white hover:bg-teal-700"
              >
                {t('pos.viewReceipt')}
              </button>
              <button
                type="button"
                onClick={handleNewSale}
                className="min-h-11 rounded-md border border-slate-300 px-4 hover:bg-slate-100"
              >
                {t('pos.newSale')}
              </button>
            </div>
          </div>
        ) : (
          <>
            {/* Cart lines */}
            {cart.length === 0 ? (
              <p className="text-sm text-slate-500">{t('pos.cartEmpty')}</p>
            ) : (
              <div className="space-y-2">
                {cart.map(line => {
                  const isExpanded = expandedLines[line.productId];
                  const appliedDiscount = appliedLineDiscounts[line.productId];
                  const ls = lineDiscountStates[line.productId] ?? emptyLineDiscount();
                  return (
                    <div key={line.productId} className="rounded-md border border-slate-200">
                      {/* Main row */}
                      <div className="flex items-center justify-between gap-2 px-3 py-2">
                        <div className="min-w-0 flex-1">
                          <p className="truncate font-medium">
                            <bdi>{productLabel(line)}</bdi>
                          </p>
                          <p className="text-sm text-slate-500">{formatSar(line.priceHalalas)} × {line.quantity}</p>
                        </div>
                        <div className="flex items-center gap-1">
                          <button
                            type="button"
                            onClick={() => updateQuantity(line.productId, -1)}
                            disabled={line.quantity <= 1}
                            className="min-h-11 min-w-11 rounded-md border border-slate-300 px-2 text-center hover:bg-slate-100 disabled:opacity-40"
                            aria-label={`${t('pos.qty')}: ${line.quantity}`}
                          >
                            −
                          </button>
                          <span className="min-w-8 text-center">{line.quantity}</span>
                          <button
                            type="button"
                            onClick={() => updateQuantity(line.productId, 1)}
                            disabled={line.quantity >= 999}
                            className="min-h-11 min-w-11 rounded-md border border-slate-300 px-2 text-center hover:bg-slate-100 disabled:opacity-40"
                          >
                            +
                          </button>
                          <button
                            type="button"
                            onClick={() => removeLine(line.productId)}
                            className="min-h-11 rounded-md border border-red-300 px-2 text-sm text-red-600 hover:bg-red-50 ms-1"
                          >
                            {t('pos.remove')}
                          </button>
                        </div>
                      </div>

                      {/* Applied line discount summary */}
                      {appliedDiscount && (
                        <div className="flex items-center justify-between border-s border-slate-200 ps-3 text-sm text-slate-600">
                          <span>{t('discount.lineDiscount')}: {formatSar(appliedDiscount)}</span>
                          <Button variant="ghost" onClick={() => removeLineDiscount(line.productId)}>
                            {t('discount.remove')}
                          </Button>
                        </div>
                      )}

                      {/* Discount toggle */}
                      <button
                        type="button"
                        onClick={() => toggleLineDiscount(line.productId)}
                        className="flex w-full items-center justify-between min-h-11 px-3 text-sm text-slate-700 hover:bg-slate-50"
                        aria-expanded={isExpanded}
                      >
                        <span>{t('discount.lineDiscount')}</span>
                        <span className={`transition-transform ${isExpanded ? 'rotate-180' : ''}`}>▾</span>
                      </button>

                      {/* Expanded discount form */}
                      {isExpanded && (
                        <div className="space-y-2 border-s border-slate-200 ps-3 p-3">
                          {/* Type radios */}
                          <fieldset>
                            <legend className="mb-1 text-sm font-medium">{t('discount.type')}</legend>
                            <div role="radiogroup" className="flex gap-4">
                              <label className="flex items-center gap-1">
                                <input
                                  type="radio"
                                  name={`line-discount-type-${line.productId}`}
                                  value="percent"
                                  checked={ls.type === 'percent'}
                                  onChange={() => updateLineDiscountState(line.productId, { type: 'percent' })}
                                  className="min-h-11"
                                />
                                {t('discount.percent')}
                              </label>
                              <label className="flex items-center gap-1">
                                <input
                                  type="radio"
                                  name={`line-discount-type-${line.productId}`}
                                  value="fixed"
                                  checked={ls.type === 'fixed'}
                                  onChange={() => updateLineDiscountState(line.productId, { type: 'fixed' })}
                                  className="min-h-11"
                                />
                                {t('discount.fixed')}
                              </label>
                            </div>
                          </fieldset>

                          {/* Value */}
                          <TextField
                            label={t('discount.value')}
                            type="text"
                            inputMode="decimal"
                            dir="ltr"
                            className="text-start"
                            placeholder={ls.type === 'percent' ? '1–100' : 'e.g. 5.00'}
                            value={ls.value}
                            onChange={e => updateLineDiscountState(line.productId, { value: e.target.value })}
                            error={lineDiscountError ? t('discount.valueMustBePositive') : null}
                          />

                          {/* Reason */}
                          <TextField
                            label={t('discount.reason')}
                            type="text"
                            placeholder={t('discount.reasonPlaceholder')}
                            value={ls.reason}
                            onChange={e => updateLineDiscountState(line.productId, { reason: e.target.value })}
                            error={lineDiscountError ? t('discount.reasonRequired') : null}
                          />

                          {/* Actions */}
                          <div className="flex gap-2">
                            <Button variant="primary" onClick={() => applyLineDiscount(line.productId)}>
                              {t('discount.apply')}
                            </Button>
                            <Button variant="secondary" onClick={() => removeLineDiscount(line.productId)}>
                              {t('discount.remove')}
                            </Button>
                          </div>

                          {/* Inline error */}
                          {lineDiscountError && (
                            <p role="alert" className="text-sm text-red-600">
                              {lineDiscountError}
                            </p>
                          )}
                        </div>
                      )}
                    </div>
                  );
                })}

                {/* Whole-sale discount control */}
                <div className="rounded-md border border-slate-200 p-3">
                  {/* Header with toggle button */}
                  <button
                    type="button"
                    onClick={() => setSaleDiscountExpanded(prev => !prev)}
                    className="flex w-full items-center justify-between min-h-11 px-0 text-sm font-medium text-slate-700 hover:bg-slate-50"
                    aria-expanded={saleDiscountExpanded}
                  >
                    <span>{t('discount.saleDiscount')}</span>
                    <span className={`transition-transform ${saleDiscountExpanded ? 'rotate-180' : ''}`}>▾</span>
                  </button>
                  {hasSaleDiscount && !saleDiscountExpanded && (
                    <div className="flex items-center justify-between border-s border-slate-200 ps-3 text-sm text-slate-600">
                      <span>{t('discount.saleDiscount')}: {formatSar(totals.netHalalas > 0 ? totals.totalDiscountHalalas : 0)}</span>
                      <Button variant="ghost" onClick={removeSaleDiscount}>
                        {t('discount.remove')}
                      </Button>
                    </div>
                  )}

                  {/* Sale discount form */}
                  {saleDiscountExpanded && (
                  <div className="space-y-2">
                    {/* Type radios */}
                    <fieldset>
                      <legend className="mb-1 text-sm font-medium">{t('discount.type')}</legend>
                      <div role="radiogroup" className="flex gap-4">
                        <label className="flex items-center gap-1">
                          <input
                            type="radio"
                            name="sale-discount-type"
                            value="percent"
                            checked={saleDiscountState.type === 'percent'}
                            onChange={() => setSaleDiscountState(prev => ({ ...prev, type: 'percent' }))}
                            className="min-h-11"
                          />
                          {t('discount.percent')}
                        </label>
                        <label className="flex items-center gap-1">
                          <input
                            type="radio"
                            name="sale-discount-type"
                            value="fixed"
                            checked={saleDiscountState.type === 'fixed'}
                            onChange={() => setSaleDiscountState(prev => ({ ...prev, type: 'fixed' }))}
                            className="min-h-11"
                          />
                          {t('discount.fixed')}
                        </label>
                      </div>
                    </fieldset>

                    {/* Value */}
                    <TextField
                      label={t('discount.value')}
                      type="text"
                      inputMode="decimal"
                      dir="ltr"
                      className="text-start"
                      placeholder={saleDiscountState.type === 'percent' ? '1–100' : 'e.g. 5.00'}
                      value={saleDiscountState.value}
                      onChange={e => setSaleDiscountState(prev => ({ ...prev, value: e.target.value }))}
                      error={saleDiscountError ? t('discount.valueMustBePositive') : null}
                    />

                    {/* Reason */}
                    <TextField
                      label={t('discount.reason')}
                      type="text"
                      placeholder={t('discount.reasonPlaceholder')}
                      value={saleDiscountState.reason}
                      onChange={e => setSaleDiscountState(prev => ({ ...prev, reason: e.target.value }))}
                      error={saleDiscountError ? t('discount.reasonRequired') : null}
                    />

                    {/* Actions */}
                    <div className="flex gap-2">
                      <Button variant="primary" onClick={applySaleDiscount}>
                        {t('discount.apply')}
                      </Button>
                      <Button variant="secondary" onClick={removeSaleDiscount}>
                        {t('discount.remove')}
                      </Button>
                    </div>

                    {/* Inline error (TextField already renders its own error via aria-describedby) */}
                  </div>
                  )}
                </div>

                {/* Discount applied confirmation */}
                {hasSaleDiscount && !saleDiscountError && (
                  <p role="status" className="text-sm text-green-700">
                    {t('discount.applied')}
                  </p>
                )}
              </div>
            )}

            {/* Totals */}
            <div className="space-y-1 border-t border-slate-200 pt-2">
              {/* Discount row (if any discount applied) */}
              {totals.totalDiscountHalalas > 0 && (
                <div className="flex justify-between text-sm text-slate-600">
                  <span>
                    {hasSaleDiscount && !hasLineDiscounts
                      ? t('discount.saleDiscount')
                      : hasLineDiscounts && !hasSaleDiscount
                        ? t('discount.lineDiscount')
                        : t('discount.discountLabel')}
                  </span>
                  <span className="text-end">-{formatSar(totals.totalDiscountHalalas)}</span>
                </div>
              )}
              <div className="flex justify-between text-sm">
                <span>{t('pos.net')}</span>
                <span className="text-end">{formatSar(totals.netHalalas)}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span>{t('pos.vat')}</span>
                <span className="text-end">{formatSar(totals.vatHalalas)}</span>
              </div>
              <div className="flex justify-between font-semibold">
                <span>{t('pos.total')}</span>
                <span className="text-end">{formatSar(totals.totalHalalas)}</span>
              </div>
            </div>

            {/* Payment type */}
            <fieldset>
              <legend className="mb-1 text-sm font-medium">{t('pos.payment')}</legend>
              <div role="radiogroup" className="flex gap-4">
                <label className="flex items-center gap-1">
                  <input
                    type="radio"
                    name="paymentType"
                    value={0}
                    checked={paymentType === 0}
                    onChange={() => setPaymentType(0)}
                    className="min-h-11"
                  />
                  {t('pos.paymentCash')}
                </label>
                <label className="flex items-center gap-1">
                  <input
                    type="radio"
                    name="paymentType"
                    value={1}
                    checked={paymentType === 1}
                    onChange={() => setPaymentType(1)}
                    className="min-h-11"
                  />
                  {t('pos.paymentCard')}
                </label>
              </div>
            </fieldset>

            {/* Actions */}
            <div className="flex gap-2">
              <button
                type="button"
                onClick={handleSubmit}
                disabled={cart.length === 0 || submitting}
                className="min-h-11 flex-1 rounded-md bg-teal-600 px-4 text-white hover:bg-teal-700 disabled:opacity-50"
              >
                {submitting ? t('pos.submitting') : t('pos.submit')}
              </button>
              <button
                type="button"
                onClick={clearCart}
                disabled={cart.length === 0}
                className="min-h-11 rounded-md border border-slate-300 px-4 hover:bg-slate-100 disabled:opacity-50"
              >
                {t('pos.clear')}
              </button>
            </div>

            {/* Submit error */}
            {submitError && (
              <p role="alert" aria-invalid="true" className="text-sm text-red-600">
                {submitError}
              </p>
            )}
          </>
        )}
      </section>
    </div>
  );
}
