import { Navigate, Route, Routes } from 'react-router-dom';
import Layout from './components/Layout';
import RequireSession from './components/RequireSession';
import ProductsPage from './pages/ProductsPage';
import PosPage from './pages/PosPage';
import SalesPage from './pages/SalesPage';
import SignInPage from './pages/SignInPage';

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="sign-in" element={<SignInPage />} />
        <Route element={<RequireSession />}>
          <Route path="products" element={<ProductsPage />} />
          <Route path="sales" element={<SalesPage />} />
          <Route path="pos" element={<PosPage />} />
        </Route>
        <Route path="*" element={<Navigate to="/products" replace />} />
      </Route>
    </Routes>
  );
}
