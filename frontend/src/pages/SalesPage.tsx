import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import { useSales } from '../api/sales';
import { formatSar } from '../lib/money';
import VoidSaleDialog from '../components/VoidSaleDialog';

export default function SalesPage() {
  const { t, i18n } = useTranslation();
  const [page, setPage] = useState(1);
  const sales = useSales(page);
  const queryClient = useQueryClient();

  const [voidingSale, setVoidingSale] = useState<{
    id: string;
    number: number;
    createdAt: string;
    totalHalalas: number;
  } | null>(null);
  const [triggerElement, setTriggerElement] = useState<HTMLElement | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const handlePrevious = () => {
    if (page > 1) setPage(page - 1);
  };

  const handleNext = () => {
    if (sales.data && page < sales.data.totalPages) setPage(page + 1);
  };

  const handleVoidSuccess = () => {
    setVoidingSale(null);
    setSuccessMessage(t('void.success'));
    void queryClient.invalidateQueries({ queryKey: ['sales'] });
  };

  return (
    <section aria-labelledby="sales-title">
      <h1 id="sales-title" className="mb-4 text-xl font-semibold">
        {t('sales.title')}
      </h1>
      {sales.isPending ? (
        <p role="status">{t('common.loading')}</p>
      ) : sales.isError ? (
        <div role="alert" className="flex flex-col items-start gap-2">
          <p>{t('sales.loadError')}</p>
          <button
            type="button"
            onClick={() => void sales.refetch()}
            className="min-h-11 rounded-md border border-slate-300 px-4 hover:bg-slate-100"
          >
            {t('common.retry')}
          </button>
        </div>
      ) : sales.data.sales.length === 0 ? (
        <p>{t('sales.empty')}</p>
      ) : (
        <>
          <table className="w-full border-collapse" aria-labelledby="sales-title">
            <thead>
              <tr className="border-b border-slate-300">
                <th scope="col" className="p-2 text-start">{t('sales.number')}</th>
                <th scope="col" className="p-2 text-start">{t('sales.date')}</th>
                <th scope="col" className="p-2 text-end">{t('sales.net')}</th>
                <th scope="col" className="p-2 text-end">{t('sales.vat')}</th>
                <th scope="col" className="p-2 text-end">{t('sales.total')}</th>
                <th scope="col" className="p-2 text-start">{t('receipt.title')}</th>
                <th scope="col" className="p-2 text-start">{t('void.title')}</th>
              </tr>
            </thead>
            <tbody>
              {sales.data.sales.map((sale) => {
                const formattedDate = new Date(sale.createdAt).toLocaleDateString(
                  i18n.language === 'ar' ? 'ar-SA' : 'en-SA'
                );
                return (
                  <tr key={sale.id} className="border-b border-slate-100">
                    <td className="p-2 text-start">{sale.number}</td>
                    <td className="p-2 text-start">{formattedDate}</td>
                    <td className="p-2 text-end">{t('money.sar', { amount: formatSar(sale.netHalalas) })}</td>
                    <td className="p-2 text-end">{t('money.sar', { amount: formatSar(sale.vatHalalas) })}</td>
                    <td className="p-2 text-end">{t('money.sar', { amount: formatSar(sale.totalHalalas) })}</td>
                    <td className="p-2 text-start">
                      <Link to={`/sales/${sale.id}/receipt`}>{t('receipt.title')}</Link>
                    </td>
                    <td className="p-2 text-start">
                      {sale.voided ? (
                        <span className="text-sm text-slate-500">{t('void.alreadyVoided')}</span>
                      ) : (
                        <button
                          type="button"
                          onClick={() => {
                            setTriggerElement(document.activeElement as HTMLElement | null);
                            setVoidingSale({
                              id: sale.id,
                              number: sale.number,
                              createdAt: sale.createdAt,
                              totalHalalas: sale.totalHalalas,
                            });
                          }}
                          className="min-h-11 rounded-md border border-slate-300 px-4 hover:bg-slate-100"
                        >
                          {t('void.title')}
                        </button>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
          <div className="flex items-center justify-center gap-4 mt-4">
            <button
              type="button"
              disabled={page === 1}
              onClick={handlePrevious}
              className="min-h-11 rounded-md border border-slate-300 px-4 hover:bg-slate-100 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {t('sales.prevPage')}
            </button>
            <span role="status" aria-live="polite">
              {t('sales.pageOf', { current: page, total: sales.data.totalPages })}
            </span>
            <button
              type="button"
              disabled={page >= sales.data.totalPages}
              onClick={handleNext}
              className="min-h-11 rounded-md border border-slate-300 px-4 hover:bg-slate-100 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {t('sales.nextPage')}
            </button>
          </div>
        </>
      )}

      {voidingSale && (
        <VoidSaleDialog
          saleId={voidingSale.id}
          saleNumber={voidingSale.number}
          createdAt={voidingSale.createdAt}
          totalHalalas={voidingSale.totalHalalas}
          onSuccess={handleVoidSuccess}
          onClose={() => setVoidingSale(null)}
          triggerElement={triggerElement}
        />
      )}

      <div className="mt-4">
        {successMessage && (
          <p role="status" className="text-sm text-green-700">
            {successMessage}
          </p>
        )}
      </div>
    </section>
  );
}
