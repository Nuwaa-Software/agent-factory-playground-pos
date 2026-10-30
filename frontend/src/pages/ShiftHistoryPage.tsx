import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import { useShiftHistory } from '../api/shifts';
import { formatSar } from '../lib/money';

const PAGE_SIZE = 10;

export default function ShiftHistoryPage() {
  const { t, i18n } = useTranslation();
  const shifts = useShiftHistory();

  // Draft filter state (not applied until Apply)
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [status, setStatus] = useState('all');

  // Committed filter state
  const [appliedFrom, setAppliedFrom] = useState<string | undefined>();
  const [appliedTo, setAppliedTo] = useState<string | undefined>();
  const [appliedStatus, setAppliedStatus] = useState('all');

  // Pagination
  const [page, setPage] = useState(1);

  const isRangeInvalid = Boolean(from && to && from > to);

  const handleApply = () => {
    setAppliedFrom(from ? `${from}T00:00:00Z` : undefined);
    setAppliedTo(to ? `${to}T23:59:59Z` : undefined);
    setAppliedStatus(status);
    setPage(1);
  };

  const handleClear = () => {
    setFrom('');
    setTo('');
    setStatus('all');
    setAppliedFrom(undefined);
    setAppliedTo(undefined);
    setAppliedStatus('all');
    setPage(1);
  };

  // Client-side filtering
  const filtered = (shifts.data ?? []).filter((shift) => {
    // Status filter
    if (appliedStatus === 'open') {
      if (shift.closedAt != null) return false;
    } else if (appliedStatus === 'closed') {
      if (shift.closedAt == null) return false;
    }

    // Date range filters
    const openedDate = new Date(shift.openedAt);
    if (appliedFrom) {
      if (openedDate < new Date(appliedFrom)) return false;
    }
    if (appliedTo) {
      if (openedDate > new Date(appliedTo)) return false;
    }

    return true;
  });

  // Client-side pagination
  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const clampedPage = Math.min(Math.max(1, page), totalPages);
  const startIdx = (clampedPage - 1) * PAGE_SIZE;
  const paged = filtered.slice(startIdx, startIdx + PAGE_SIZE);

  const lang = i18n.language === 'ar' ? 'ar-SA' : 'en-SA';

  return (
    <section aria-labelledby="shift-history-title">
      <h1 id="shift-history-title" className="mb-4 text-xl font-semibold">
        {t('shiftHistory.title')}
      </h1>

      {/* Filter bar */}
      <div
        role="group"
        aria-label={t('shiftHistory.filterLabel')}
        className="mb-4 flex flex-wrap items-end gap-3"
      >
        <label htmlFor="shift-from" className="flex flex-col text-start">
          <span className="mb-1 text-sm font-medium">{t('sales.filterFrom')}</span>
          <input
            id="shift-from"
            type="date"
            dir="ltr"
            value={from}
            onChange={(e) => setFrom(e.target.value)}
            className="min-h-11 rounded-md border border-slate-300 px-3 text-sm"
          />
        </label>

        <label htmlFor="shift-to" className="flex flex-col text-start">
          <span className="mb-1 text-sm font-medium">{t('sales.filterTo')}</span>
          <input
            id="shift-to"
            type="date"
            dir="ltr"
            value={to}
            onChange={(e) => setTo(e.target.value)}
            className="min-h-11 rounded-md border border-slate-300 px-3 text-sm"
          />
        </label>

        <label htmlFor="shift-status" className="flex flex-col text-start">
          <span className="mb-1 text-sm font-medium">{t('shiftHistory.statusFilter')}</span>
          <select
            id="shift-status"
            value={status}
            onChange={(e) => setStatus(e.target.value)}
            className="min-h-11 rounded-md border border-slate-300 px-3 text-sm"
          >
            <option value="all">{t('shiftHistory.statusAll')}</option>
            <option value="open">{t('shiftHistory.statusOpen')}</option>
            <option value="closed">{t('shiftHistory.statusClosed')}</option>
          </select>
        </label>

        <button
          type="button"
          onClick={handleApply}
          disabled={isRangeInvalid}
          className="min-h-11 rounded-md bg-teal-700 px-4 py-2 text-sm font-medium text-white hover:bg-teal-800 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {t('sales.filterApply')}
        </button>

        <button
          type="button"
          onClick={handleClear}
          className="min-h-11 rounded-md border border-slate-300 px-4 py-2 text-sm font-medium hover:bg-slate-100"
        >
          {t('sales.filterClear')}
        </button>
      </div>

      {/* Loading state */}
      {shifts.isPending ? (
        <p role="status">{t('common.loading')}</p>
      ) : shifts.isError ? (
        /* Error state */
        <div role="alert" className="flex flex-col items-start gap-2">
          <p>{t('shiftHistory.loadError')}</p>
          <button
            type="button"
            onClick={() => void shifts.refetch()}
            className="min-h-11 rounded-md border border-slate-300 px-4 hover:bg-slate-100"
          >
            {t('common.retry')}
          </button>
        </div>
      ) : (
        /* Data state */
        <>
          {/* Empty: no shifts at all */}
          {shifts.data.length === 0 ? (
            <p role="status">{t('shiftHistory.empty')}</p>
          ) : filtered.length === 0 ? (
            /* No match: data exists but filter yields nothing */
            <p role="status">{t('shiftHistory.noMatch')}</p>
          ) : (
            <>
              {/* Table */}
              <table className="w-full border-collapse" aria-labelledby="shift-history-title">
                <thead>
                  <tr className="border-b border-slate-300">
                    <th scope="col" className="p-2 text-start">{t('shiftHistory.date')}</th>
                    <th scope="col" className="p-2 text-start">{t('shift.cashier')}</th>
                    <th scope="col" className="p-2 text-start">{t('shiftHistory.status')}</th>
                    <th scope="col" className="p-2 text-end">{t('shift.starting')}</th>
                    <th scope="col" className="p-2 text-end">{t('shift.actual')}</th>
                    <th scope="col" className="p-2 text-end">{t('shiftHistory.expected')}</th>
                    <th scope="col" className="p-2 text-end">{t('shiftHistory.discrepancy')}</th>
                  </tr>
                </thead>
                <tbody>
                  {paged.map((shift) => {
                    const dateStr = new Date(shift.openedAt).toLocaleDateString(lang, {
                      year: 'numeric',
                      month: 'long',
                      day: 'numeric',
                    });
                    const isOpen = shift.closedAt == null;

                    return (
                      <tr key={shift.id} className="border-b border-slate-100">
                        <td className="p-2 text-start">{dateStr}</td>
                        <td className="p-2 text-start"><bdi>{shift.cashierName}</bdi></td>
                        <td className="p-2 text-start">
                          {isOpen ? t('shiftHistory.statusOpen') : t('shiftHistory.statusClosed')}
                        </td>
                        <td className="p-2 text-end">
                          {t('money.sar', { amount: formatSar(shift.startingCashHalalas) })}
                        </td>
                        <td className="p-2 text-end">
                          {shift.actualCashHalalas != null
                            ? t('money.sar', { amount: formatSar(shift.actualCashHalalas) })
                            : '—'}
                        </td>
                        <td className="p-2 text-end">
                          {shift.expectedCashHalalas != null
                            ? t('money.sar', { amount: formatSar(shift.expectedCashHalalas) })
                            : '—'}
                        </td>
                        <td className="p-2 text-end">
                          {shift.discrepancyHalalas != null
                            ? t('money.sar', { amount: formatSar(shift.discrepancyHalalas) })
                            : '—'}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>

              {/* Pagination */}
              <div className="flex items-center justify-center gap-4 mt-4">
                <button
                  type="button"
                  disabled={clampedPage === 1}
                  onClick={() => setPage(clampedPage - 1)}
                  className="min-h-11 rounded-md border border-slate-300 px-4 hover:bg-slate-100 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {t('sales.prevPage')}
                </button>
                <span role="status" aria-live="polite">
                  {t('sales.pageOf', { current: clampedPage, total: totalPages })}
                </span>
                <button
                  type="button"
                  disabled={clampedPage >= totalPages}
                  onClick={() => setPage(clampedPage + 1)}
                  className="min-h-11 rounded-md border border-slate-300 px-4 hover:bg-slate-100 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {t('sales.nextPage')}
                </button>
              </div>
            </>
          )}
        </>
      )}
    </section>
  );
}
