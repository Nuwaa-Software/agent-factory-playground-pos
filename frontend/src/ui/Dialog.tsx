import { useEffect, useRef, useCallback } from 'react';
import { useTranslation } from 'react-i18next';

interface DialogProps {
  open: boolean;
  onClose(): void;
  titleId: string;
  children: React.ReactNode;
}

export default function Dialog({ open, onClose, titleId, children }: DialogProps) {
  const dialogRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLElement | null>(null);
  const prevOpenRef = useRef(open);
  const { t } = useTranslation();

  // Track the element that triggered the dialog for focus restore
  useEffect(() => {
    if (open) {
      triggerRef.current = document.activeElement as HTMLElement;
    }
  }, [open]);

  // Restore focus when dialog closes via open=false transition (controlled close)
  useEffect(() => {
    if (prevOpenRef.current && !open) {
      triggerRef.current?.focus();
    }
    prevOpenRef.current = open;
  }, [open]);

  // Focus trap: Tab / Shift+Tab cycles within the dialog
  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if (e.key === 'Escape') {
        onClose();
        return;
      }
      if (e.key !== 'Tab') return;

      const container = dialogRef.current;
      if (!container) return;

      // Improved selector: includes select, textarea, and anchor links
      const focusable = Array.from(
        container.querySelectorAll<HTMLElement>(
          'button:not([disabled]), input, select, textarea, a[href]'
        )
      );
      if (focusable.length === 0) return;

      const first = focusable[0];
      const last = focusable[focusable.length - 1];

      if (e.shiftKey) {
        if (document.activeElement === first) {
          e.preventDefault();
          last.focus();
        }
      } else {
        if (document.activeElement === last) {
          e.preventDefault();
          first.focus();
        }
      }
    },
    [onClose],
  );

  // Focus restore on unmount
  useEffect(() => {
    return () => {
      triggerRef.current?.focus();
    };
  }, []);

  // Move focus into the dialog when it opens
  useEffect(() => {
    if (open) {
      const container = dialogRef.current;
      if (!container) return;
      const focusable = Array.from(
        container.querySelectorAll<HTMLElement>(
          'button:not([disabled]), input, select, textarea, a[href]',
        ),
      );
      if (focusable.length > 0) {
        focusable[0].focus();
      }
    }
  }, [open]);

  const handleOverlayClick = (e: React.MouseEvent) => {
    if (e.target === e.currentTarget) onClose();
  };

  if (!open) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
      onClick={handleOverlayClick}
      onKeyDown={handleKeyDown}
      role="presentation"
    >
      <div
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className="relative w-full max-w-md rounded-lg border border-slate-300 bg-white p-6 shadow-lg"
      >
        <button
          type="button"
          onClick={onClose}
          className="absolute top-4 start-4 rounded-md p-2 text-slate-500 hover:bg-slate-100 hover:text-slate-700"
          aria-label={t('ui.close')}
        >
          ✕
        </button>
        <div className="pe-8">{children}</div>
      </div>
    </div>
  );
}
