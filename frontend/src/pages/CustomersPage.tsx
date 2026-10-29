import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQueryClient } from '@tanstack/react-query';
import { apiPatch, apiPost, ApiError } from '../api/client';
import { useCustomers } from '../api/customers';
import { formatSar } from '../lib/money';

type View = 'list' | 'create' | 'edit' | 'success';

interface SuccessData {
  type: 'created' | 'updated';
  customer: import('../api/customers').Customer;
}

export default function CustomersPage() {
  const { t } = useTranslation();
  const customers = useCustomers();
  const queryClient = useQueryClient();
  const [view, setView] = useState<View>('list');
  const [editingId, setEditingId] = useState<string | null>(null);
  const [name, setName] = useState('');
  const [phone, setPhone] = useState('');
  const [inFlight, setInFlight] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');
  const [nameError, setNameError] = useState(false);
  const [successData, setSuccessData] = useState<SuccessData | null>(null);

  // ── Helpers ────────────────────────────────────────────────────────

  const resetForm = () => {
    setName('');
    setPhone('');
    setNameError(false);
    setErrorMsg('');
    setSuccessData(null);
  };

  const goList = () => {
    setView('list');
    resetForm();
  };

  const validate = (): boolean => {
    setNameError(false);
    if (name.trim() === '') {
      setNameError(true);
      return false;
    }
    return true;
  };

  // ── Create handler ─────────────────────────────────────────────────

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMsg('');
    if (!validate()) return;
    setInFlight(true);
    try {
      const created = await apiPost<import('../api/customers').Customer>('/api/customers', {
        name: name.trim(),
        phone: phone.trim() || undefined,
      });
      queryClient.setQueryData<import('../api/customers').Customer[]>(['customers'], (prev) =>
        prev ? [created, ...prev] : [created],
      );
      setSuccessData({ type: 'created', customer: created });
      setView('success');
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        setErrorMsg(t('customers.notFound'));
      } else {
        setErrorMsg(t('customers.error'));
      }
    } finally {
      setInFlight(false);
    }
  };

  // ── Edit handler ───────────────────────────────────────────────────

  const handleEdit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMsg('');
    if (!validate()) return;
    setInFlight(true);
    try {
      const updated = await apiPatch<import('../api/customers').Customer>(`/api/customers/${editingId}`, {
        name: name.trim(),
        phone: phone.trim() || undefined,
      });
      queryClient.setQueryData<import('../api/customers').Customer[]>(['customers'], (prev) =>
        prev ? prev.map((c) => (c.id === editingId ? updated : c)) : prev,
      );
      setSuccessData({ type: 'updated', customer: updated });
      setView('success');
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        setErrorMsg(t('customers.notFound'));
      } else {
        setErrorMsg(t('customers.error'));
      }
    } finally {
      setInFlight(false);
    }
  };

  // ── Edit: pre-fill from list data ──────────────────────────────────

  const openEdit = (id: string) => {
    resetForm();
    const customer = customers.data?.find((c) => c.id === id);
    if (!customer) return;
    setName(customer.name);
    setPhone(customer.phone ?? '');
    setEditingId(id);
    setView('edit');
  };

  // ── Render: list ───────────────────────────────────────────────────

  if (view === 'list') {
    return (
      <section aria-labelledby="customers-title">
        <h1 id="customers-title" className="mb-4 text-xl font-semibold">
          {t('customers.title')}
        </h1>
        <button
          type="button"
          onClick={() => { setView('create'); resetForm(); }}
          className="mb-4 min-h-11 self-start rounded-md bg-teal-700 px-4 text-white hover:bg-teal-800 disabled:opacity-50"
        >
          {t('customers.create')}
        </button>
        {customers.isPending ? (
          <p role="status">{t('common.loading')}</p>
        ) : customers.isError ? (
          <div role="alert" className="flex flex-col items-start gap-2">
            <p>{t('customers.loadError')}</p>
            <button
              type="button"
              onClick={() => void customers.refetch()}
              className="min-h-11 rounded-md border border-slate-300 px-4 hover:bg-slate-100"
            >
              {t('common.retry')}
            </button>
          </div>
        ) : !customers.data || customers.data.length === 0 ? (
          <p>{t('customers.empty')}</p>
        ) : (
          <table className="w-full border-collapse">
            <thead>
              <tr className="border-b border-slate-300">
                <th scope="col" className="p-2 text-start">{t('customers.name')}</th>
                <th scope="col" className="p-2 text-start">{t('customers.phone')}</th>
                <th scope="col" className="p-2 text-end">{t('customers.credit')}</th>
                <th scope="col" className="p-2 text-end sr-only">Actions</th>
              </tr>
            </thead>
            <tbody>
              {customers.data.map((c) => (
                <tr key={c.id} className="border-b border-slate-100">
                  <td className="p-2">
                    <bdi>{c.name}</bdi>
                  </td>
                  <td className="p-2">
                    {c.phone ? <bdi>{c.phone}</bdi> : ''}
                  </td>
                  <td className="p-2 text-end">
                    {t('money.sar', { amount: formatSar(c.creditBalanceHalalas) })}
                  </td>
                  <td className="p-2 text-end">
                    <button
                      type="button"
                      onClick={() => openEdit(c.id)}
                      className="min-h-11 rounded-md border border-slate-300 px-4 hover:bg-slate-100"
                    >
                      {t('customers.editTitle')}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    );
  }

  // ── Render: success state ──────────────────────────────────────────

  if (view === 'success' && successData) {
    const c = successData.customer;
    return (
      <section aria-labelledby="customer-success-title" className="max-w-md">
        <h1 id="customer-success-title" tabIndex={-1} role="status" className="mb-4 text-xl font-semibold text-green-700">
          {successData.type === 'created' ? t('customers.created') : t('customers.updated')}
        </h1>
        <div className="mb-6 flex flex-col gap-2 rounded-md border border-slate-200 p-3">
          <p>
            <span className="font-medium">{t('customers.name')}:</span>{' '}
            {c.name}
          </p>
          <p>
            <span className="font-medium">{t('customers.phone')}:</span>{' '}
            {c.phone ? <bdi>{c.phone}</bdi> : ''}
          </p>
          <p>
            <span className="font-medium">{t('customers.credit')}:</span>{' '}
            {t('money.sar', { amount: formatSar(c.creditBalanceHalalas) })}
          </p>
        </div>
        <div className="flex flex-col gap-2">
          <button
            type="button"
            onClick={goList}
            className="min-h-11 self-start rounded-md bg-teal-700 px-4 text-white hover:bg-teal-800 disabled:opacity-50"
          >
            {t('product.backToList')}
          </button>
          <button
            type="button"
            onClick={() => { setView('create'); resetForm(); }}
            className="min-h-11 self-start rounded-md border border-slate-300 px-4 hover:bg-slate-100"
          >
            {t('customers.create')}
          </button>
        </div>
      </section>
    );
  }

  // ── Render: create / edit form ─────────────────────────────────────

  const title = view === 'create' ? t('customers.createTitle') : t('customers.editTitle');
  const submitLabel = inFlight ? t('customers.saving') : t('product.save');

  return (
    <section aria-labelledby="customer-form-title" className="max-w-md">
      <h1 id="customer-form-title" className="mb-4 text-xl font-semibold">
        {title}
      </h1>
      {errorMsg && (
        <div role="alert" className="mb-4 text-sm text-red-700">
          {errorMsg}
        </div>
      )}
      <form onSubmit={view === 'create' ? handleCreate : handleEdit} noValidate className="flex flex-col gap-4">
        {/* Name */}
        <div className="flex flex-col gap-1">
          <label htmlFor="customer-name">{t('customers.name')}</label>
          <input
            id="customer-name"
            required
            value={name}
            onChange={(e) => { setName(e.target.value); setNameError(false); }}
            aria-invalid={nameError}
            aria-describedby={nameError ? 'customer-name-error' : undefined}
            className="min-h-11 rounded-md border border-slate-300 px-3"
          />
          {nameError && (
            <p id="customer-name-error" role="alert" className="text-sm text-red-700">
              {t('product.errRequired')}
            </p>
          )}
        </div>

        {/* Phone */}
        <div className="flex flex-col gap-1">
          <label htmlFor="customer-phone">{t('customers.phone')}</label>
          <input
            id="customer-phone"
            type="tel"
            dir="ltr"
            value={phone}
            onChange={(e) => setPhone(e.target.value)}
            className="min-h-11 rounded-md border border-slate-300 px-3"
          />
        </div>

        {/* Submit */}
        <button
          type="submit"
          disabled={inFlight}
          className="min-h-11 self-start rounded-md bg-teal-700 px-4 text-white hover:bg-teal-800 disabled:opacity-50"
        >
          {submitLabel}
        </button>

        {/* Back */}
        <button
          type="button"
          onClick={goList}
          className="min-h-11 self-start rounded-md border border-slate-300 px-4 hover:bg-slate-100"
        >
          {t('product.backToList')}
        </button>
      </form>
    </section>
  );
}
