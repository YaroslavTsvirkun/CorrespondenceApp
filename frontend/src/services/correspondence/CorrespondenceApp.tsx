import { Layout, Menu, Typography, Button, Tooltip } from 'antd';
import {
  DashboardOutlined, FileTextOutlined, CheckSquareOutlined, ContactsOutlined,
  TeamOutlined, ApartmentOutlined, BookOutlined, BarChartOutlined, MailOutlined,
  SafetyOutlined, BankOutlined, UsergroupAddOutlined, SettingOutlined, ArrowLeftOutlined,
} from '@ant-design/icons';
import { Routes, Route, useNavigate, useLocation, Navigate } from 'react-router-dom';
import Dashboard from '../../pages/Dashboard';
import DocumentsPage from '../../pages/DocumentsPage';
import TasksPage from '../../pages/TasksPage';
import CorrespondentsPage from '../../pages/CorrespondentsPage';
import EmployeesPage from '../../pages/EmployeesPage';
import DepartmentsPage from '../../pages/DepartmentsPage';
import DictionariesPage from '../../pages/DictionariesPage';
import ReportsPage from '../../pages/ReportsPage';
import AccessPage from '../../pages/AccessPage';
import OrganizationsPage from '../../pages/OrganizationsPage';
import UsersPage from '../../pages/UsersPage';
import SettingsPage from '../../pages/SettingsPage';
import UserMenu from '../../components/UserMenu';
import { useAuth } from '../../auth/AuthContext';
import { P } from '../../auth/permissions';

const { Sider, Header, Content } = Layout;

/** Базовий шлях сервісу; має збігатися з path у services/registry.tsx */
const BASE = '/correspondence';

/** Сервіс «Реєстрація кореспонденції»: бокове меню, шапка й сторінки під /correspondence/*. */
export default function CorrespondenceApp() {
  const { can, canAny } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  // key — шлях відносно BASE ('' — дашборд)
  const allItems = [
    { key: '', icon: <DashboardOutlined />, label: 'Дашборд', show: canAny(P.dashOverview, P.dashControl, P.dashMyTasks) },
    { key: 'documents', icon: <FileTextOutlined />, label: 'Документи', show: canAny(...P.docsReadScopes) },
    { key: 'tasks', icon: <CheckSquareOutlined />, label: 'Доручення', show: canAny(...P.docsReadScopes, P.tasksAssign, P.tasksExecute) },
    { key: 'correspondents', icon: <ContactsOutlined />, label: 'Кореспонденти', show: can(P.correspondents) },
    { key: 'employees', icon: <TeamOutlined />, label: 'Працівники', show: can(P.employees) },
    { key: 'departments', icon: <ApartmentOutlined />, label: 'Підрозділи', show: can(P.departments) },
    { key: 'dictionaries', icon: <BookOutlined />, label: 'Довідники', show: can(P.dictionaries) },
    { key: 'reports', icon: <BarChartOutlined />, label: 'Звіти та статистика', show: can(P.reports) },
    { key: 'users', icon: <UsergroupAddOutlined />, label: 'Користувачі', show: can(P.users) },
    { key: 'access', icon: <SafetyOutlined />, label: 'Ролі та доступ', show: can(P.users) },
    { key: 'settings', icon: <SettingOutlined />, label: 'Налаштування', show: can(P.settings) },
    { key: 'organizations', icon: <BankOutlined />, label: 'Організації', show: can(P.organizations) },
  ];
  const menu = allItems.filter((i) => i.show).map(({ key, icon, label }) => ({ key, icon, label }));
  const subPath = location.pathname.slice(BASE.length).replace(/^\//, '');
  const selected = menu.find((m) => m.key !== '' && subPath.startsWith(m.key))?.key ?? '';

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider theme="dark" width={250} breakpoint="lg" collapsedWidth={0}>
        <div style={{ color: '#fff', display: 'flex', alignItems: 'center', gap: 10, padding: '16px 20px', fontSize: 16, fontWeight: 600 }}>
          <MailOutlined style={{ fontSize: 22 }} />
          <span>Реєстрація<br />кореспонденції</span>
        </div>
        <Menu
          theme="dark" mode="inline" selectedKeys={[selected]} items={menu}
          onClick={(e) => navigate(e.key ? `${BASE}/${e.key}` : BASE)}
        />
      </Sider>
      <Layout>
        <Header style={{ background: '#fff', padding: '0 24px', display: 'flex', alignItems: 'center', borderBottom: '1px solid #f0f0f0' }}>
          <Tooltip title="Повернутися до списку доступних сервісів">
            <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/')} style={{ marginInlineEnd: 16 }}>
              Сервіси
            </Button>
          </Tooltip>
          <Typography.Title level={4} style={{ margin: 0, flex: 1 }}>
            Інформаційна система відділу реєстрації кореспонденції
          </Typography.Title>
          <UserMenu />
        </Header>
        <Content style={{ margin: 24 }}>
          <Routes>
            <Route index element={<Dashboard />} />
            <Route path="documents" element={<DocumentsPage />} />
            <Route path="tasks" element={<TasksPage />} />
            <Route path="correspondents" element={<CorrespondentsPage />} />
            <Route path="employees" element={<EmployeesPage />} />
            <Route path="departments" element={<DepartmentsPage />} />
            <Route path="dictionaries" element={<DictionariesPage />} />
            <Route path="reports" element={<ReportsPage />} />
            <Route path="users" element={<UsersPage />} />
            <Route path="access" element={<AccessPage />} />
            <Route path="settings" element={<SettingsPage />} />
            <Route path="organizations" element={<OrganizationsPage />} />
            <Route path="*" element={<Navigate to={BASE} replace />} />
          </Routes>
        </Content>
      </Layout>
    </Layout>
  );
}
