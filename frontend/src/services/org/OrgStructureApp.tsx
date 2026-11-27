import { ApartmentOutlined, TeamOutlined, ClusterOutlined } from '@ant-design/icons';
import ServiceLayout from '../../components/ServiceLayout';
import DepartmentsPage from '../../pages/DepartmentsPage';
import EmployeesPage from '../../pages/EmployeesPage';
import { useAuth } from '../../auth/AuthContext';
import { P } from '../../auth/permissions';

/** Сервіс «Оргструктура»: підрозділи й працівники організації — спільні для всіх сервісів. */
export default function OrgStructureApp() {
  const { can } = useAuth();

  return (
    <ServiceLayout
      base="/org"
      icon={<ClusterOutlined />}
      siderTitle="Оргструктура"
      title="Організаційна структура"
      sections={[
        { key: 'departments', icon: <ApartmentOutlined />, label: 'Підрозділи', element: <DepartmentsPage />, show: can(P.departments) },
        { key: 'employees', icon: <TeamOutlined />, label: 'Працівники', element: <EmployeesPage />, show: can(P.employees) },
      ]}
    />
  );
}
