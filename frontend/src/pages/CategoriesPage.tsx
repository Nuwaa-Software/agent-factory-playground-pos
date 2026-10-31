import { useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQueryClient } from '@tanstack/react-query';
import { apiPost, ApiError } from '../api/client';
import { useCategories } from '../api/categories';

type View = 'list' | 'create';

export default function CategoriesPage() {
  const { t, i18n } = useTranslation();
  const categories = useCategories();
  const queryClient = useQueryClient();
  const [view, setView] = useState<View>('list');
  const [nameEn, setNameEn] = useState('');
  const [nameAr, setNameAr] = useState('');
  const [description, setDescription] = useState('');
  const [parentId, setParentId] = useState('');
  const [inFlight, setInFlight] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');
  const [nameEnError, setNameEnError] = useState(false);
  const [nameArError, setNameArError] = useState(false);
  const [createdFlag, setCreatedFlag] = useState(false);
  const nameEnRef = useRef<HTMLInputElement>(null);

  const resetForm = () => {
    setNameEn('');
    setNameAr('');
    setDescription('');
    setParentId('');
    setInFlight(false);
    setErrorMsg('');
    setNameEnError(false);
    setNameArError(false);
    setCreatedFlag(false);
  };

  const goList = () => {
    setNameEn('');
    setNameAr('');
    setDescription('');
    setParentId('');
    setInFlight(false);
    setErrorMsg('');
    setNameEnError(false);
    setNameArError(false);
    setView('list');
  };

  const openCreate = () => {
    resetForm();
    setView('create');
    // Focus after render
    setTimeout(() => nameEnRef.current?.focus(), 0);
  };

  const validate = (): boolean => {
    setNameEnError(false);
    setNameArError(false);
    let valid = true;
    if (nameEn.trim() === '') {
      setNameEnError(true);
      valid = false;
    }
    if (nameAr.trim() === '') {
      setNameArError(true);
      valid = false;
    }
    return valid;
  };

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMsg('');
    if (!validate()) return;
    setInFlight(true);
    try {
      const body: Record<string, unknown> = {
        nameEn: nameEn.trim(),
        nameAr: nameAr.trim(),
      };
      if (description.trim()) body.description = description.trim();
      if (parentId) body.parentId = parentId;

      const created = await apiPost<import('../api/categories').Category>('/api/inventory/categories', body);

      queryClient.setQueryData<import('../api/categories').Category[]>(
        ['categories'],
        (prev) => [created, ...(prev ?? [])],
      );
      setCreatedFlag(true);
      goList();
    } catch (err) {
      if (err instanceof ApiError) {
        setErrorMsg(t('categories.error'));
      } else {
        setErrorMsg(t('categories.error'));
      }
    } finally {
      setInFlight(false);
    }
  };

  // ── List view ───────────────────────────────────────────────────────

  if (view === 'list') {
    return (
      <section aria-labelledby="categories-title">
        <h1 id="categories-title" className="mb-4 text-xl font-semibold">
          {t('categories.title')}
        </h1>
        {createdFlag && (
          <p role="status" className="mb-4 text-sm text-green-700">
            {t('categories.created')}
          </p>
        )}
        <button
          type="button"
          onClick={openCreate}
          className="mb-4 min-h-11 self-start rounded-md bg-teal-700 px-4 text-white hover:bg-teal-800 disabled:opacity-50"
        >
          {t('categories.create')}
        </button>
        {categories.isPending ? (
          <p role="status">{t('common.loading')}</p>
        ) : categories.isError ? (
          <div role="alert" className="flex flex-col items-start gap-2">
            <p>{t('categories.loadError')}</p>
            <button
              type="button"
              onClick={() => void categories.refetch()}
              className="min-h-11 rounded-md border border-slate-300 px-4 hover:bg-slate-100"
            >
              {t('common.retry')}
            </button>
          </div>
        ) : !categories.data || categories.data.length === 0 ? (
          <p>{t('categories.empty')}</p>
        ) : (
          <table className="w-full border-collapse">
            <thead>
              <tr className="border-b border-slate-300">
                <th scope="col" className="p-2 text-start">{t('categories.name')}</th>
                <th scope="col" className="p-2 text-start">{t('categories.description')}</th>
                <th scope="col" className="p-2 text-start">{t('categories.parent')}</th>
              </tr>
            </thead>
            <tbody>
              {categories.data.map((c) => {
                const parent = c.parentId ? categories.data.find((p) => p.id === c.parentId) : undefined;
                return (
                  <tr key={c.id} className="border-b border-slate-100">
                    <td className="p-2">
                      <bdi>{i18n.language === 'ar' ? c.nameAr : c.nameEn}</bdi>
                    </td>
                    <td className="p-2">{c.description ?? ''}</td>
                    <td className="p-2">
                      {parent ? (
                        <bdi>{i18n.language === 'ar' ? parent.nameAr : parent.nameEn}</bdi>
                      ) : (
                        t('categories.noParent')
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </section>
    );
  }

  // ── Create form view ────────────────────────────────────────────────

  const submitLabel = inFlight ? t('categories.saving') : t('categories.create');

  return (
    <section aria-labelledby="category-form-title" className="max-w-md">
      <h1 id="category-form-title" className="mb-4 text-xl font-semibold">
        {t('categories.createTitle')}
      </h1>
      {errorMsg && (
        <div role="alert" className="mb-4 text-sm text-red-700">
          {errorMsg}
        </div>
      )}
      <form onSubmit={handleCreate} noValidate className="flex flex-col gap-4">
        {/* nameEn */}
        <div className="flex flex-col gap-1">
          <label htmlFor="cat-name-en">{t('categories.nameEn')}</label>
          <input
            ref={nameEnRef}
            id="cat-name-en"
            value={nameEn}
            onChange={(e) => { setNameEn(e.target.value); setNameEnError(false); }}
            aria-invalid={nameEnError}
            aria-describedby={nameEnError ? 'cat-name-en-error' : undefined}
            className="min-h-11 rounded-md border border-slate-300 px-3"
          />
          {nameEnError && (
            <p id="cat-name-en-error" role="alert" className="text-sm text-red-700">
              {t('product.errRequired')}
            </p>
          )}
        </div>

        {/* nameAr */}
        <div className="flex flex-col gap-1">
          <label htmlFor="cat-name-ar">{t('categories.nameAr')}</label>
          <input
            id="cat-name-ar"
            dir="rtl"
            value={nameAr}
            onChange={(e) => { setNameAr(e.target.value); setNameArError(false); }}
            aria-invalid={nameArError}
            aria-describedby={nameArError ? 'cat-name-ar-error' : undefined}
            className="min-h-11 rounded-md border border-slate-300 px-3"
          />
          {nameArError && (
            <p id="cat-name-ar-error" role="alert" className="text-sm text-red-700">
              {t('product.errRequired')}
            </p>
          )}
        </div>

        {/* description */}
        <div className="flex flex-col gap-1">
          <label htmlFor="cat-description">{t('categories.description')}</label>
          <textarea
            id="cat-description"
            rows={3}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            className="min-h-11 rounded-md border border-slate-300 px-3"
          />
        </div>

        {/* parent */}
        <div className="flex flex-col gap-1">
          <label htmlFor="cat-parent">{t('categories.parent')}</label>
          <select
            id="cat-parent"
            value={parentId}
            onChange={(e) => setParentId(e.target.value)}
            className="min-h-11 rounded-md border border-slate-300 px-3"
          >
            <option value="">{t('categories.noParent')}</option>
            {categories.data?.map((c) => (
              <option key={c.id} value={c.id}>
                {i18n.language === 'ar' ? c.nameAr : c.nameEn}
              </option>
            ))}
          </select>
        </div>

        {/* Submit */}
        <button
          type="submit"
          disabled={inFlight}
          className="min-h-11 self-start rounded-md bg-teal-700 px-4 text-white hover:bg-teal-800 disabled:opacity-50"
        >
          {submitLabel}
        </button>
      </form>
    </section>
  );
}
