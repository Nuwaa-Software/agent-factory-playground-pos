import { useCallback, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import type { Product } from '../api/products';
import { createSale, type SaleResponse } from '../api/sales';
import { apiGet } from '../api/client';
import { formatSar } from '../lib/money';

interface CartLine {
  productId: string;
  nameEn: string;
  nameAr: string;
  sku: string;
  priceHalalas: number;
  stock: number;
  quantity: number;
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
    setLastSale(null);
  }, []);

  const clearCart = useCallback(() => {
    setCart([]);
    setLastSale(null);
    setSubmitError(null);
  }, []);

  // Live totals
  const netHalalas = cart.reduce((sum, l) => sum + l.priceHalalas * l.quantity, 0);
  const vatHalalas = Math.floor((netHalalas * 15 + 50) / 100);
  const totalHalalas = netHalalas + vatHalalas;

  const handleSubmit = useCallback(async () => {
    if (cart.length === 0) return;
    setSubmitting(true);
    setSubmitError(null);
    try {
      const sale = await createSale({
        lines: cart.map(l => ({ productId: l.productId, quantity: l.quantity })),
        paymentType,
      });
      setLastSale(sale);
    } catch {
      setSubmitError(t('pos.submitError'));
    } finally {
      setSubmitting(false);
    }
  }, [cart, paymentType, t]);

  const handleNewSale = useCallback(() => {
    setCart([]);
    setPaymentType(0);
    setLastSale(null);
    setSubmitError(null);
    setSearchQuery('');
    setResults([]);
  }, []);

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
                {cart.map(line => (
                  <div key={line.productId} className="flex items-center justify-between gap-2 rounded-md border border-slate-200 px-3 py-2">
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
                ))}
              </div>
            )}

            {/* Totals */}
            <div className="space-y-1 border-t border-slate-200 pt-2">
              <div className="flex justify-between text-sm">
                <span>{t('pos.net')}</span>
                <span>{formatSar(netHalalas)}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span>{t('pos.vat')}</span>
                <span>{formatSar(vatHalalas)}</span>
              </div>
              <div className="flex justify-between font-semibold">
                <span>{t('pos.total')}</span>
                <span>{formatSar(totalHalalas)}</span>
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
              <p role="alert" aria-invalid="true" aria-describedby="submit-error-desc" className="text-sm text-red-600">
                {submitError}
              </p>
            )}
          </>
        )}
      </section>
    </div>
  );
}
