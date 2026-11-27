import type { ReactNode } from 'react';
import { Layout, Menu, Typography, Button, Tooltip } from 'antd';
import { ArrowLeftOutlined } from '@ant-design/icons';
import { Routes, Route, useNavigate, useLocation, Navigate } from 'react-router-dom';
import UserMenu from './UserMenu';

const { Sider, Header, Content } = Layout;

/** Розділ сервісу: пункт бокового меню й сторінка. */
export interface ServiceSection {
  /** Шлях відносно базового шляху сервісу ('' — головна сторінка сервісу) */
  key: string;
  icon: ReactNode;
  label: string;
  element: ReactNode;
  /** Чи має користувач доступ до розділу */
  show: boolean;
}

interface Props {
  /** Базовий шлях сервісу; має збігатися з path у services/registry.tsx */
  base: string;
  icon: ReactNode;
  /** Назва в боковій панелі (можна з переносом рядка) */
  siderTitle: ReactNode;
  /** Заголовок у шапці */
  title: string;
  sections: ServiceSection[];
}

/** Спільний каркас сервісу: бокове меню розділів, шапка з поверненням до сервісів і маршрути розділів. */
export default function ServiceLayout({ base, icon, siderTitle, title, sections }: Props) {
  const navigate = useNavigate();
  const location = useLocation();

  const visible = sections.filter((s) => s.show);
  const menu = visible.map(({ key, icon, label }) => ({ key, icon, label }));
  const subPath = location.pathname.slice(base.length).replace(/^\//, '');
  const selected = visible.find((s) => s.key !== '' && subPath.startsWith(s.key))?.key ?? '';
  // Немає головної сторінки ('') — відкриваємо перший доступний розділ
  const home = visible.find((s) => s.key === '') ? base : visible[0] ? `${base}/${visible[0].key}` : '/';

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider theme="dark" width={250} breakpoint="lg" collapsedWidth={0}>
        <div style={{ color: '#fff', display: 'flex', alignItems: 'center', gap: 10, padding: '16px 20px', fontSize: 16, fontWeight: 600 }}>
          <span style={{ fontSize: 22 }}>{icon}</span>
          <span>{siderTitle}</span>
        </div>
        <Menu
          theme="dark" mode="inline" selectedKeys={[selected]} items={menu}
          onClick={(e) => navigate(e.key ? `${base}/${e.key}` : base)}
        />
      </Sider>
      <Layout>
        <Header style={{ background: '#fff', padding: '0 24px', display: 'flex', alignItems: 'center', borderBottom: '1px solid #f0f0f0' }}>
          <Tooltip title="Повернутися до списку доступних сервісів">
            <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/')} style={{ marginInlineEnd: 16 }}>
              Сервіси
            </Button>
          </Tooltip>
          <Typography.Title level={4} style={{ margin: 0, flex: 1 }}>{title}</Typography.Title>
          <UserMenu />
        </Header>
        <Content style={{ margin: 24 }}>
          <Routes>
            {visible.map((s) => (s.key === ''
              ? <Route key="index" index element={s.element} />
              : <Route key={s.key} path={s.key} element={s.element} />))}
            <Route path="*" element={<Navigate to={home} replace />} />
          </Routes>
        </Content>
      </Layout>
    </Layout>
  );
}
