import { useTranslation } from 'react-i18next';

interface PaginationProps {
  currentPage: number;
  totalPages: number;
  onPageChange(page: number): void;
  className?: string;
}

export default function Pagination({
  currentPage,
  totalPages,
  onPageChange,
  className = '',
}: PaginationProps) {
  const { t } = useTranslation();
  const pages: (number | 'ellipsis')[] = [];

  if (totalPages <= 1) return null;

  for (let i = 1; i <= totalPages; i++) {
    if (i === 1 || i === totalPages || Math.abs(i - currentPage) <= 1) {
      pages.push(i);
    } else if (pages[pages.length - 1] !== 'ellipsis') {
      pages.push('ellipsis');
    }
  }

  return (
    <nav
      aria-label={t('ui.pagination')}
      className={`flex items-center gap-2 ${className}`}
    >
      <button
        onClick={() => onPageChange(currentPage - 1)}
        disabled={currentPage === 1}
        className="min-h-11 rounded-md border border-slate-300 bg-white px-3 py-2 text-sm disabled:opacity-50 disabled:cursor-not-allowed"
        aria-label={t('ui.prevPage')}
      >
        ‹
      </button>

      {pages.map((page, idx) =>
        page === 'ellipsis' ? (
          <span key={`e-${idx}`} className="px-2 text-slate-400">
            …
          </span>
        ) : (
          <button
            key={page}
            onClick={() => onPageChange(page)}
            aria-current={page === currentPage ? 'page' : undefined}
            className={`min-h-11 rounded-md px-3 py-2 text-sm ${
              page === currentPage
                ? 'bg-teal-700 font-medium text-white'
                : 'border border-slate-300 bg-white hover:bg-slate-50'
            }`}
          >
            {page}
          </button>
        )
      )}

      <button
        onClick={() => onPageChange(currentPage + 1)}
        disabled={currentPage === totalPages}
        className="min-h-11 rounded-md border border-slate-300 bg-white px-3 py-2 text-sm disabled:opacity-50 disabled:cursor-not-allowed"
        aria-label={t('ui.nextPage')}
      >
        ›
      </button>
    </nav>
  );
}
