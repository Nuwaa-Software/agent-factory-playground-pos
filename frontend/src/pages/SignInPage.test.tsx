import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Route, Routes } from 'react-router-dom';
import { axe } from '../test/axe';
import { loadSession } from '../session';
import { COMPANY_ID, renderWithProviders } from '../test/render';
import SignInPage from './SignInPage';

function renderSignIn() {
  return renderWithProviders(
    <Routes>
      <Route path="/sign-in" element={<SignInPage />} />
      <Route path="/products" element={<p>products page</p>} />
    </Routes>,
    { route: '/sign-in' },
  );
}

describe('SignInPage', () => {
  it('rejects a company ID that is not a GUID', async () => {
    renderSignIn();
    await userEvent.type(screen.getByLabelText('معرّف الشركة'), 'acme');
    await userEvent.click(screen.getByRole('button', { name: 'دخول' }));
    expect(screen.getByRole('alert')).toHaveTextContent('أدخل معرّف شركة صحيحًا (GUID).');
    expect(screen.getByLabelText('معرّف الشركة')).toHaveAttribute('aria-invalid', 'true');
    expect(loadSession()).toBeNull();
  });

  it('remembers the company and role and opens the products', async () => {
    renderSignIn();
    await userEvent.type(screen.getByLabelText('معرّف الشركة'), ` ${COMPANY_ID} `);
    await userEvent.selectOptions(screen.getByLabelText('الدور'), 'مدير');
    await userEvent.click(screen.getByRole('button', { name: 'دخول' }));
    expect(await screen.findByText('products page')).toBeInTheDocument();
    expect(loadSession()).toEqual({ companyId: COMPANY_ID, role: 'Admin' });
  });

  it('has no accessibility violations, also with an error shown', async () => {
    const { container } = renderSignIn();
    expect(await axe(container)).toHaveNoViolations();
    await userEvent.click(screen.getByRole('button', { name: 'دخول' }));
    expect(await axe(container)).toHaveNoViolations();
  });
});
