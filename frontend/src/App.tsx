import { Spin } from 'antd';
import { Routes, Route, Navigate } from 'react-router-dom';
import LoginPage from './pages/LoginPage';
import ServicesPage from './pages/ServicesPage';
import { legacyRedirects, services } from './services/registry';
import { useAuth } from './auth/AuthContext';

export default function App() {
  const { user, loading, canAny } = useAuth();

  if (loading) {
    return <div style={{ display: 'flex', height: '100vh', alignItems: 'center', justifyContent: 'center' }}><Spin size="large" /></div>;
  }
  if (!user) return <LoginPage />;

  return (
    <Routes>
      <Route path="/" element={<ServicesPage />} />
      {Object.entries(legacyRedirects).map(([from, to]) => (
        <Route key={from} path={from} element={<Navigate to={to} replace />} />
      ))}
      {services.filter((s) => canAny(...s.permissions)).map(({ id, path, component: Component }) => (
        <Route key={id} path={`${path}/*`} element={<Component />} />
      ))}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
