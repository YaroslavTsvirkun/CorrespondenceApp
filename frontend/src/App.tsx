import type { ComponentType } from 'react';
import { Spin } from 'antd';
import { Routes, Route, Navigate } from 'react-router-dom';
import LoginPage from './pages/LoginPage';
import ServicesPage from './pages/ServicesPage';
import CorrespondenceApp from './services/correspondence/CorrespondenceApp';
import { services } from './services/registry';
import { useAuth } from './auth/AuthContext';

// Компонент кожного сервісу з реєстру (services/registry.tsx), за його id
const serviceComponents: Record<string, ComponentType> = {
  correspondence: CorrespondenceApp,
};

// Сторінки зі старих посилань (до появи сторінки сервісів) — тепер під /correspondence
const legacyPaths = [
  'documents', 'tasks', 'correspondents', 'employees', 'departments', 'dictionaries',
  'reports', 'users', 'access', 'settings', 'organizations',
];

export default function App() {
  const { user, loading, canAny } = useAuth();

  if (loading) {
    return <div style={{ display: 'flex', height: '100vh', alignItems: 'center', justifyContent: 'center' }}><Spin size="large" /></div>;
  }
  if (!user) return <LoginPage />;

  return (
    <Routes>
      <Route path="/" element={<ServicesPage />} />
      {services.filter((s) => s.isAvailable(canAny)).map((s) => {
        const Component = serviceComponents[s.id];
        return Component && <Route key={s.id} path={`${s.path}/*`} element={<Component />} />;
      })}
      {legacyPaths.map((p) => (
        <Route key={p} path={`/${p}`} element={<Navigate to={`/correspondence/${p}`} replace />} />
      ))}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
