import { UsergroupAddOutlined, SafetyOutlined, BankOutlined, ControlOutlined } from '@ant-design/icons';
import ServiceLayout from '../../components/ServiceLayout';
import UsersPage from '../../pages/UsersPage';
import AccessPage from '../../pages/AccessPage';
import OrganizationsPage from '../../pages/OrganizationsPage';
import { useAuth } from '../../auth/AuthContext';
import { P } from '../../auth/permissions';

/** Сервіс «Адміністрування»: користувачі, ролі та доступ, організації — спільні для всіх сервісів. */
export default function AdminApp() {
  const { can } = useAuth();

  return (
    <ServiceLayout
      base="/admin"
      icon={<ControlOutlined />}
      siderTitle="Адміністрування"
      title="Адміністрування"
      sections={[
        { key: 'users', icon: <UsergroupAddOutlined />, label: 'Користувачі', element: <UsersPage />, show: can(P.users) },
        { key: 'access', icon: <SafetyOutlined />, label: 'Ролі та доступ', element: <AccessPage />, show: can(P.users) },
        { key: 'organizations', icon: <BankOutlined />, label: 'Організації', element: <OrganizationsPage />, show: can(P.organizations) },
      ]}
    />
  );
}
