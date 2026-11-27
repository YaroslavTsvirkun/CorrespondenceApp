import {
  DashboardOutlined, FileTextOutlined, CheckSquareOutlined, BookOutlined, BarChartOutlined,
  MailOutlined, SettingOutlined,
} from '@ant-design/icons';
import ServiceLayout from '../../components/ServiceLayout';
import Dashboard from '../../pages/Dashboard';
import DocumentsPage from '../../pages/DocumentsPage';
import TasksPage from '../../pages/TasksPage';
import DictionariesPage from '../../pages/DictionariesPage';
import ReportsPage from '../../pages/ReportsPage';
import SettingsPage from '../../pages/SettingsPage';
import { useAuth } from '../../auth/AuthContext';
import { P } from '../../auth/permissions';

/** Сервіс «Реєстрація кореспонденції»: документи, доручення, довідники, звіти, нумерація. */
export default function CorrespondenceApp() {
  const { can, canAny } = useAuth();

  return (
    <ServiceLayout
      base="/correspondence"
      icon={<MailOutlined />}
      siderTitle={<>Реєстрація<br />кореспонденції</>}
      title="Інформаційна система відділу реєстрації кореспонденції"
      sections={[
        { key: '', icon: <DashboardOutlined />, label: 'Дашборд', element: <Dashboard />, show: canAny(P.dashOverview, P.dashControl, P.dashMyTasks) },
        { key: 'documents', icon: <FileTextOutlined />, label: 'Документи', element: <DocumentsPage />, show: canAny(...P.docsReadScopes) },
        { key: 'tasks', icon: <CheckSquareOutlined />, label: 'Доручення', element: <TasksPage />, show: canAny(...P.docsReadScopes, P.tasksAssign, P.tasksExecute) },
        { key: 'dictionaries', icon: <BookOutlined />, label: 'Довідники', element: <DictionariesPage />, show: can(P.dictionaries) },
        { key: 'reports', icon: <BarChartOutlined />, label: 'Звіти та статистика', element: <ReportsPage />, show: can(P.reports) },
        { key: 'settings', icon: <SettingOutlined />, label: 'Налаштування', element: <SettingsPage />, show: can(P.settings) },
      ]}
    />
  );
}
