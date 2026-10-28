import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import { useDailySales } from '../api/reports';
import { formatSar } from '../lib/money';

/** Return today as yyyy-MM-dd in the local timezone. */
function toLocalDate(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

export default function ReportsDailyPage() {
  const { t } = useTranslation();
  const [date, setDate] = useState(toLocalDate(new Date()));
  const report = useDailySales(date);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setDate(e.target.value);
  };

  return (
    <section aria-labelledby="report-title">
      <h1 id="report-title" className="mb-4 text-xl font-semibold">
        {t('report.title')}
      </h1>

      <div className="mb-4 flex items-center gap-2">
        <label htmlFor="report-date" className="min-h-11 inline-flex items-center">
          {t('report.dateLabel')}
        </label>
        <input
          id="report-date"
          type="date"
          value={date}
          onChange={handleChange}
          className="min-h-11 rounded-md border border-slate-300 px-3"
        />
      </div>

      {report.isPending ? (
        <p role="status">{t('common.loading')}</p>
      ) : report.isError ? (
        <div role="alert" className="flex flex-col items-start gap-2">
          <p>{t('report.loadError')}</p>
          <button
            type="button"
            onClick={() => void report.refetch()}
            className="min-h-11 rounded-md border border-slate-300 px-4 hover:bg-slate-100"
          >
            {t('common.retry')}
          </button>
        </div>
      ) : report.data?.saleCount === 0 ? (
        <p>{t('report.empty')}</p>
      ) : report.data ? (
        <table className="w-full border-collapse" aria-labelledby="report-title">
          <thead>
            <tr className="border-b border-slate-300">
              <th scope="col" className="p-2 text-start">{t('report.saleCount')}</th>
              <th scope="col" className="p-2 text-end">{t('report.net')}</th>
              <th scope="col" className="p-2 text-end">{t('report.vat')}</th>
              <th scope="col" className="p-2 text-end">{t('report.gross')}</th>
            </tr>
          </thead>
          <tbody>
            <tr className="border-b border-slate-100">
              <td className="p-2 text-start">{report.data.saleCount}</td>
              <td className="p-2 text-end">{t('money.sar', { amount: formatSar(report.data.netTotal) })}</td>
              <td className="p-2 text-end">{t('money.sar', { amount: formatSar(report.data.vatTotal) })}</td>
              <td className="p-2 text-end">{t('money.sar', { amount: formatSar(report.data.grossTotal) })}</td>
            </tr>
          </tbody>
        </table>
      ) : null}
    </section>
  );
}
