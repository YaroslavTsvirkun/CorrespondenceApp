import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { App as AntApp, ConfigProvider, theme } from 'antd';
import ukUA from 'antd/locale/uk_UA';
import 'antd/dist/reset.css';
import './index.css';
import App from './App';
import { LookupsProvider } from './data/lookups';
import { AuthProvider } from './auth/AuthContext';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ConfigProvider
      locale={ukUA}
      theme={{
        algorithm: theme.defaultAlgorithm,
        token: { colorPrimary: '#1d6fb8', borderRadius: 6, fontSize: 14 },
      }}
    >
      <AntApp>
        <AuthProvider>
          <LookupsProvider>
            <BrowserRouter>
              <App />
            </BrowserRouter>
          </LookupsProvider>
        </AuthProvider>
      </AntApp>
    </ConfigProvider>
  </StrictMode>,
);
