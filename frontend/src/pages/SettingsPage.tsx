import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { apiPatch, ApiError } from '../api/client';
import { useCompanySettings } from '../api/settings';
import type { CompanySettings } from '../api/settings';

export default function SettingsPage() {
  const { t } = useTranslation();
  const { data: settings, isLoading, isError, error } = useCompanySettings();

  const [taxRate, setTaxRate] = useState('');
  const [lowStock, setLowStock] = useState('');
  const [receiptFooter, setReceiptFooter] = useState('');
  const [inFlight, setInFlight] = useState(false);
  const [successMsg, setSuccessMsg] = useState('');
  const [errorMsg, setErrorMsg] = useState('');

  // Validation errors
  const [taxRateError, setTaxRateError] = useState(false);
  const [lowStockError, setLowStockError] = useState(false);
  const [footerError, setFooterError] = useState(false);

  // Populate form from loaded settings (only when settings first becomes available)
  /* eslint-disable react-hooks/set-state-in-effect, react-hooks/exhaustive-deps */
  useEffect(() => {
    if (settings && taxRate === '' && lowStock === '' && receiptFooter === '') {
      setTaxRate(String(settings.taxRatePercent));
      setLowStock(String(settings.lowStockThreshold));
      setReceiptFooter(settings.receiptFooterText);
    }
  }, [settings]);
  /* eslint-enable react-hooks/set-state-in-effect, react-hooks/exhaustive-deps */

  if (isLoading) {
    return <p role="status">{t('common.loading')}</p>;
  }

  if (isError) {
    if (error instanceof ApiError && error.status === 403) {
      return (
        <div role="alert" className="flex flex-col items-start gap-2">
          <p>{t('settings.forbidden')}</p>
        </div>
      );
    }
    return (
      <div role="alert" className="flex flex-col items-start gap-2">
        <p>{t('settings.error')}</p>
        <button
          type="button"
          onClick={() => window.location.reload()}
          className="min-h-11 self-start rounded-md border border-slate-300 px-4 hover:bg-slate-100"
        >
          {t('common.retry')}
        </button>
      </div>
    );
  }

  if (!settings) return null;

  const validate = (): boolean => {
    let valid = true;
    setTaxRateError(false);
    setLowStockError(false);
    setFooterError(false);

    const taxTrimmed = taxRate.trim();
    if (taxTrimmed === '' || !/^\d+(\.\d+)?$/.test(taxTrimmed) || parseFloat(taxTrimmed) < 0 || parseFloat(taxTrimmed) > 100) {
      setTaxRateError(true);
      valid = false;
    }

    const stockTrimmed = lowStock.trim();
    if (stockTrimmed === '' || !/^\d+$/.test(stockTrimmed) || parseInt(stockTrimmed, 10) < 0 || parseInt(stockTrimmed, 10) > 10000) {
      setLowStockError(true);
      valid = false;
    }

    if (receiptFooter.length > 500) {
      setFooterError(true);
      valid = false;
    }

    return valid;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSuccessMsg('');
    setErrorMsg('');

    if (!validate()) return;

    // Build PATCH body with only changed fields
    const body: Record<string, unknown> = {};
    const taxParsed = parseFloat(taxRate.trim());
    const stockParsed = parseInt(lowStock.trim(), 10);

    if (taxParsed !== settings.taxRatePercent) {
      body.taxRatePercent = taxParsed;
    }
    if (stockParsed !== settings.lowStockThreshold) {
      body.lowStockThreshold = stockParsed;
    }
    if (receiptFooter !== settings.receiptFooterText) {
      body.receiptFooterText = receiptFooter;
    }

    setInFlight(true);
    try {
      const result = await apiPatch<CompanySettings>('/api/company/settings', body);
      setSuccessMsg(t('settings.saved'));
      // Update local values from response
      if (result.taxRatePercent != null) setTaxRate(String(result.taxRatePercent));
      if (result.lowStockThreshold != null) setLowStock(String(result.lowStockThreshold));
      if (result.receiptFooterText != null) setReceiptFooter(result.receiptFooterText);
    } catch (err) {
      if (err instanceof ApiError && err.status === 403) {
        setErrorMsg(t('settings.forbidden'));
      } else {
        setErrorMsg(t('settings.error'));
      }
    } finally {
      setInFlight(false);
    }
  };

  return (
    <section aria-labelledby="settings-title" className="max-w-md">
      <h1 id="settings-title" className="mb-4 text-xl font-semibold">
        {t('settings.title')}
      </h1>
      {successMsg && (
        <div role="status" className="mb-4 text-sm text-green-700">
          {successMsg}
        </div>
      )}
      {errorMsg && (
        <div role="alert" className="mb-4 text-sm text-red-700">
          {errorMsg}
        </div>
      )}
      <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-4">
        {/* Tax rate */}
        <div className="flex flex-col gap-1">
          <label htmlFor="tax-rate">{t('settings.taxRate')}</label>
          <input
            id="tax-rate"
            type="number"
            min={0}
            max={100}
            step={0.5}
            dir="ltr"
            value={taxRate}
            onChange={(e) => { setTaxRate(e.target.value); setTaxRateError(false); }}
            aria-invalid={taxRateError}
            aria-describedby={taxRateError ? 'tax-rate-error' : undefined}
            className="min-h-11 rounded-md border border-slate-300 px-3"
          />
          {taxRateError && (
            <p id="tax-rate-error" role="alert" className="text-sm text-red-700">
              {t('product.errMoney')}
            </p>
          )}
        </div>

        {/* Low stock threshold */}
        <div className="flex flex-col gap-1">
          <label htmlFor="low-stock">{t('settings.lowStockThreshold')}</label>
          <input
            id="low-stock"
            type="number"
            min={0}
            max={10000}
            step={1}
            dir="ltr"
            value={lowStock}
            onChange={(e) => { setLowStock(e.target.value); setLowStockError(false); }}
            aria-invalid={lowStockError}
            aria-describedby={lowStockError ? 'low-stock-error' : undefined}
            className="min-h-11 rounded-md border border-slate-300 px-3"
          />
          {lowStockError && (
            <p id="low-stock-error" role="alert" className="text-sm text-red-700">
              {t('product.errStock')}
            </p>
          )}
        </div>

        {/* Receipt footer */}
        <div className="flex flex-col gap-1">
          <label htmlFor="receipt-footer">{t('settings.receiptFooter')}</label>
          <input
            id="receipt-footer"
            type="text"
            maxLength={500}
            value={receiptFooter}
            onChange={(e) => { setReceiptFooter(e.target.value); setFooterError(false); }}
            aria-invalid={footerError}
            aria-describedby={footerError ? 'receipt-footer-error' : 'receipt-footer-hint'}
            className="min-h-11 rounded-md border border-slate-300 px-3"
          />
          <p id="receipt-footer-hint" className="text-sm text-slate-500">
            {t('settings.footerMax')}
          </p>
          {footerError && (
            <p id="receipt-footer-error" role="alert" className="text-sm text-red-700">
              {t('settings.footerMax')}
            </p>
          )}
        </div>

        {/* Submit */}
        <button
          type="submit"
          disabled={inFlight}
          className="min-h-11 self-start rounded-md bg-teal-700 px-4 text-white hover:bg-teal-800 disabled:opacity-50"
        >
          {inFlight ? t('settings.saving') : t('settings.save')}
        </button>
      </form>
    </section>
  );
}
