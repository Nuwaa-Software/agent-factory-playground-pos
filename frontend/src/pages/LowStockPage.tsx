import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import { useLowStock } from '../api/lowStock';
import { formatSar } from '../lib/money';

export default function LowStockPage() {
  const { t, i18n } = useTranslation();
  const [thresholdInput, setThresholdInput] = useState('');
  const [appliedThreshold, setAppliedThreshold] = useState<number | undefined>();

  const isValidPositiveInteger = /^\d+$/.test(thresholdInput) && Number(thresholdInput) > 0;

  const lowStock = useLowStock(appliedThreshold);

  const handleApply = () => {
    if (!isValidPositiveInteger) return;
    setAppliedThreshold(Number(thresholdInput));
  };

  return (
    <section aria-labelledby="lowstock-title">
      <h1 id="lowstock-title" className="mb-4 text-xl font-semibold">
        {t('lowstock.title')}
      </h1>
      <div className="mb-4 flex flex-wrap items-end gap-3">
        <label htmlFor="lowstock-threshold" className="flex flex-col text-start">
          <span className="mb-1 text-sm font-medium">{t('lowstock.thresholdLabel')}</span>
          <input
            id="lowstock-threshold"
            type="number"
            min={1}
            step={1}
            inputMode="numeric"
            dir="ltr"
            placeholder={t('lowstock.thresholdPlaceholder')}
            value={thresholdInput}
            onChange={(e) => setThresholdInput(e.target.value)}
            className="min-h-11 rounded-md border border-slate-300 px-3 text-sm"
          />
        </label>
        <button
          type="button"
          onClick={handleApply}
          disabled={!isValidPositiveInteger}
          className="min-h-11 rounded-md bg-teal-600 px-4 py-2 text-sm font-medium text-white hover:bg-teal-700 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {t('lowstock.apply')}
        </button>
      </div>
      {lowStock.isPending ? (
        <p role="status">{t('common.loading')}</p>
      ) : lowStock.isError ? (
        <div role="alert" className="flex flex-col items-start gap-2">
          <p>{t('lowstock.loadError')}</p>
          <button
            type="button"
            onClick={() => void lowStock.refetch()}
            className="min-h-11 rounded-md border border-slate-300 px-4 hover:bg-slate-100"
          >
            {t('common.retry')}
          </button>
        </div>
      ) : lowStock.data.length === 0 ? (
        <p>{t('lowstock.empty')}</p>
      ) : (
        <table className="w-full border-collapse" aria-labelledby="lowstock-title">
          <thead>
            <tr className="border-b border-slate-300">
              <th scope="col" className="p-2 text-start">{t('lowstock.product')}</th>
              <th scope="col" className="p-2 text-start">{t('lowstock.sku')}</th>
              <th scope="col" className="p-2 text-end">{t('lowstock.stock')}</th>
              <th scope="col" className="p-2 text-end">{t('lowstock.price')}</th>
            </tr>
          </thead>
          <tbody>
            {lowStock.data.map((p) => (
              <tr key={p.id} className="border-b border-slate-100">
                <td className="p-2 text-start">
                  <bdi>{i18n.language === 'ar' ? p.nameAr : p.nameEn}</bdi>
                </td>
                <td className="p-2 text-start">
                  <bdi>{p.sku}</bdi>
                </td>
                <td className="p-2 text-end">{p.stock}</td>
                <td className="p-2 text-end">
                  {t('money.sar', { amount: formatSar(p.priceHalalas) })}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
