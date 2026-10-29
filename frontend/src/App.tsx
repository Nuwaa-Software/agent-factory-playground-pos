import { Navigate, Route, Routes } from 'react-router-dom';
import Layout from './components/Layout';
import RequireSession from './components/RequireSession';
import ProductsPage from './pages/ProductsPage';
import ReceiptPage from './pages/ReceiptPage';
import PosPage from './pages/PosPage';
import SalesPage from './pages/SalesPage';
import ShiftsPage from './pages/ShiftsPage';
import SignInPage from './pages/SignInPage';
import ProductCreatePage from './pages/ProductCreatePage';
import ProductEditPage from './pages/ProductEditPage';
import ReportsDailyPage from './pages/ReportsDailyPage';
import SettingsPage from './pages/SettingsPage';
import LowStockPage from './pages/LowStockPage';
import CustomersPage from './pages/CustomersPage';

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="sign-in" element={<SignInPage />} />
        <Route element={<RequireSession />}>
          <Route path="products" element={<ProductsPage />} />
          <Route path="sales" element={<SalesPage />} />
          <Route path="sales/:id/receipt" element={<ReceiptPage />} />
          <Route path="pos" element={<PosPage />} />
          <Route path="shifts" element={<ShiftsPage />} />
          <Route path="products/new" element={<ProductCreatePage />} />
          <Route path="products/:id/edit" element={<ProductEditPage />} />
          <Route path="reports/daily" element={<ReportsDailyPage />} />
          <Route path="settings" element={<SettingsPage />} />
          <Route path="low-stock" element={<LowStockPage />} />
          <Route path="customers" element={<CustomersPage />} />
        </Route>
        <Route path="*" element={<Navigate to="/products" replace />} />
      </Route>
    </Routes>
  );
}
