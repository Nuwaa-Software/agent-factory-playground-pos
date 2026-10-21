import { Navigate, Outlet } from 'react-router-dom';
import { loadSession } from '../session';

/** Pages that call the API need a company and a role; without them, go to sign-in. */
export default function RequireSession() {
  return loadSession() ? <Outlet /> : <Navigate to="/sign-in" replace />;
}
