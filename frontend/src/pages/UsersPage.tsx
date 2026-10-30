import { useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { apiPatch, ApiError } from '../api/client';
import { useUsers } from '../api/users';
import type { User } from '../api/users';

export default function UsersPage() {
  const { t, i18n } = useTranslation();
  const users = useUsers();
  const queryClient = useQueryClient();

  const [successMsg, setSuccessMsg] = useState('');
  const [errorMsg, setErrorMsg] = useState('');
  const [inFlight, setInFlight] = useState<Set<string>>(new Set());

  if (users.isPending) {
    return <p role="status">{t('common.loading')}</p>;
  }

  if (users.isError) {
    if (users.error instanceof ApiError && users.error.status === 403) {
      return (
        <div role="alert" className="flex flex-col items-start gap-2">
          <p>{t('users.forbidden')}</p>
        </div>
      );
    }
    return (
      <div role="alert" className="flex flex-col items-start gap-2">
        <p>{t('users.error')}</p>
        <button
          type="button"
          onClick={() => void users.refetch()}
          className="min-h-11 self-start rounded-md border border-slate-300 px-4 hover:bg-slate-100"
        >
          {t('common.retry')}
        </button>
      </div>
    );
  }

  if (users.data.length === 0) {
    return <p>{t('users.empty')}</p>;
  }

  const updateRow = (userId: string, result: User) => {
    queryClient.setQueryData<User[]>(['users'], (old) =>
      old?.map((u) => (u.id === userId ? result : u)),
    );
  };

  const handleRoleChange = async (userId: string, newRole: number) => {
    setSuccessMsg('');
    setErrorMsg('');
    setInFlight((prev) => new Set(prev).add(userId));
    try {
      const result = await apiPatch<User>(`/api/users/${userId}`, { role: newRole });
      updateRow(userId, result);
      setSuccessMsg(t('users.updated'));
    } catch (err) {
      if (err instanceof ApiError && err.status === 403) {
        setErrorMsg(t('users.forbidden'));
      } else if (err instanceof ApiError && err.status === 404) {
        setErrorMsg(t('users.notFound'));
      } else {
        setErrorMsg(t('users.error'));
      }
    } finally {
      setInFlight((prev) => {
        const next = new Set(prev);
        next.delete(userId);
        return next;
      });
    }
  };

  const handleStatusToggle = async (userId: string, currentActive: boolean) => {
    setSuccessMsg('');
    setErrorMsg('');
    setInFlight((prev) => new Set(prev).add(userId));
    try {
      const result = await apiPatch<User>(`/api/users/${userId}`, { isActive: !currentActive });
      updateRow(userId, result);
      setSuccessMsg(t('users.updated'));
    } catch (err) {
      if (err instanceof ApiError && err.status === 403) {
        setErrorMsg(t('users.forbidden'));
      } else if (err instanceof ApiError && err.status === 404) {
        setErrorMsg(t('users.notFound'));
      } else {
        setErrorMsg(t('users.error'));
      }
    } finally {
      setInFlight((prev) => {
        const next = new Set(prev);
        next.delete(userId);
        return next;
      });
    }
  };

  return (
    <section aria-labelledby="users-title">
      <h1 id="users-title" className="mb-4 text-xl font-semibold">
        {t('users.title')}
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
      <table className="w-full border-collapse">
        <thead>
          <tr className="border-b border-slate-300">
            <th scope="col" className="p-2 text-start">{t('users.name')}</th>
            <th scope="col" className="p-2 text-start">{t('users.role')}</th>
            <th scope="col" className="p-2 text-end">{t('users.status')}</th>
          </tr>
        </thead>
        <tbody>
          {users.data.map((u) => (
            <tr key={u.id} className="border-b border-slate-100">
              <td className="p-2">
                <bdi>{i18n.language === 'ar' ? u.nameAr : u.nameEn}</bdi>
              </td>
              <td className="p-2">
                <label htmlFor={`role-${u.id}`} className="sr-only">{t('users.role')}</label>
                <select
                  id={`role-${u.id}`}
                  value={u.role}
                  onChange={(e) => handleRoleChange(u.id, Number(e.target.value))}
                  disabled={inFlight.has(u.id)}
                  className="min-h-11 rounded-md border border-slate-300 px-3"
                >
                  <option value={0}>{t('users.roleAdmin')}</option>
                  <option value={1}>{t('users.roleCashier')}</option>
                  <option value={2}>{t('users.roleViewer')}</option>
                </select>
              </td>
              <td className="p-2 text-end">
                <span>{u.isActive ? t('users.active') : t('users.inactive')}</span>
                {' '}
                <button
                  type="button"
                  aria-pressed={u.isActive}
                  aria-label={`${i18n.language === 'ar' ? u.nameAr : u.nameEn} ${u.isActive ? t('users.deactivate') : t('users.activate')}`}
                  onClick={() => handleStatusToggle(u.id, u.isActive)}
                  disabled={inFlight.has(u.id)}
                  className="min-h-11 rounded-md border border-slate-300 px-3 hover:bg-slate-100"
                >
                  {inFlight.has(u.id) ? t('users.saving') : u.isActive ? t('users.deactivate') : t('users.activate')}
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
