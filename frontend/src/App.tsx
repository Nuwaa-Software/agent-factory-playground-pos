import { Navigate, Route, Routes } from 'react-router-dom';
import Layout from './components/Layout';
import RequireSession from './components/RequireSession';
import ProductsPage from './pages/ProductsPage';
import SalesPage from './pages/SalesPage';
import ShiftsPage from './pages/ShiftsPage';
import SignInPage from './pages/SignInPage';

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="sign-in" element={<SignInPage />} />
        <Route element={<RequireSession />}>
          <Route path="products" element={<ProductsPage />} />
          <Route path="sales" element={<SalesPage />} />
          <Route path="shifts" element={<ShiftsPage />} />
        </Route>
        <Route path="*" element={<Navigate to="/products" replace />} />
      </Route>
    </Routes>
  );
}
