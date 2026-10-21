import { useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { isGuid, ROLES, saveSession, type Role } from '../session';

export default function SignInPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [companyId, setCompanyId] = useState('');
  const [role, setRole] = useState<Role>('Cashier');
  const [invalid, setInvalid] = useState(false);

  const submit = (event: FormEvent) => {
    event.preventDefault();
    if (!isGuid(companyId)) {
      setInvalid(true);
      return;
    }
    saveSession({ companyId, role });
    navigate('/products');
  };

  return (
    <section aria-labelledby="sign-in-title" className="max-w-md">
      <h1 id="sign-in-title" className="mb-4 text-xl font-semibold">
        {t('signIn.title')}
      </h1>
      <form onSubmit={submit} noValidate className="flex flex-col gap-4">
        <div className="flex flex-col gap-1">
          <label htmlFor="company-id">{t('signIn.companyId')}</label>
          <input
            id="company-id"
            dir="ltr"
            autoComplete="off"
            value={companyId}
            onChange={(e) => {
              setCompanyId(e.target.value);
              setInvalid(false);
            }}
            aria-invalid={invalid}
            aria-describedby={invalid ? 'company-id-hint company-id-error' : 'company-id-hint'}
            className="min-h-11 rounded-md border border-slate-300 px-3"
          />
          <p id="company-id-hint" className="text-sm text-slate-600">
            {t('signIn.companyIdHint')}
          </p>
          {invalid && (
            <p id="company-id-error" role="alert" className="text-sm text-red-700">
              {t('signIn.companyIdInvalid')}
            </p>
          )}
        </div>
        <div className="flex flex-col gap-1">
          <label htmlFor="role">{t('signIn.role')}</label>
          <select
            id="role"
            value={role}
            onChange={(e) => setRole(e.target.value as Role)}
            className="min-h-11 rounded-md border border-slate-300 px-3"
          >
            {ROLES.map((r) => (
              <option key={r} value={r}>
                {t(`roles.${r}`)}
              </option>
            ))}
          </select>
        </div>
        <button type="submit" className="min-h-11 self-start rounded-md bg-teal-700 px-4 text-white hover:bg-teal-800">
          {t('signIn.submit')}
        </button>
      </form>
    </section>
  );
}
