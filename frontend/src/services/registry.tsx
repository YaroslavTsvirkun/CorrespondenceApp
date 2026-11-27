import { lazy, type ComponentType, type LazyExoticComponent, type ReactNode } from 'react';
import { MailOutlined, ControlOutlined, ClusterOutlined, IdcardOutlined } from '@ant-design/icons';
import { P } from '../auth/permissions';

// Каталог сервісів, що показуються на стартовій сторінці після входу.
// Щоб додати новий сервіс: створіть його компонент (на основі ServiceLayout) і опишіть тут через
// lazyService(() => import(...)) — маршрут `${path}/*` App.tsx зареєструє сам.
// Код кожного сервісу — окремий файл збірки: він завантажується лише при вході в сервіс
// (або заздалегідь — коли користувач наводить на картку сервісу).

/** Сервіс, що завантажується на вимогу: компонент для маршруту і попереднє завантаження коду. */
interface LazyService {
  component: LazyExoticComponent<ComponentType>;
  preload: () => void;
}

function lazyService(load: () => Promise<{ default: ComponentType }>): LazyService {
  let pending: Promise<{ default: ComponentType }> | undefined;
  const once = () => (pending ??= load());
  return { component: lazy(once), preload: () => { once().catch(() => { pending = undefined; }); } };
}
export interface ServiceDefinition {
  id: string;
  title: string;
  description: string;
  icon: ReactNode;
  /** Базовий шлях сервісу (усі його сторінки — під ним) */
  path: string;
  /** Колір акценту картки */
  color: string;
  /** Пермішени сервісу: сервіс доступний, якщо користувач має хоча б один із них */
  permissions: readonly string[];
  /** Код сервісу (завантажується на вимогу) */
  app: LazyService;
}

export const services: ServiceDefinition[] = [
  {
    id: 'correspondence',
    title: 'Реєстрація кореспонденції',
    description: 'Реєстрація вхідних і вихідних документів, доручення, контроль виконання, звіти.',
    icon: <MailOutlined />,
    path: '/correspondence',
    color: '#1d6fb8',
    permissions: [
      ...P.docsReadScopes, P.docsCreate, P.docsUpdate, P.docsDelete, P.resolutionsCreate,
      P.tasksAssign, P.tasksExecute, P.dictionaries, P.reports,
      P.dashOverview, P.dashControl, P.dashMyTasks, P.settings,
    ],
    app: lazyService(() => import('./correspondence/CorrespondenceApp')),
  },
  {
    id: 'parties',
    title: 'Контрагенти',
    description: 'Реєстр юридичних і фізичних осіб, з якими листується та працює організація.',
    icon: <IdcardOutlined />,
    path: '/parties',
    color: '#13a8a8',
    permissions: [P.parties, P.partiesBank],
    app: lazyService(() => import('./parties/PartiesApp')),
  },
  {
    id: 'org',
    title: 'Оргструктура',
    description: 'Підрозділи та працівники організації.',
    icon: <ClusterOutlined />,
    path: '/org',
    color: '#389e0d',
    permissions: [P.departments, P.employees],
    app: lazyService(() => import('./org/OrgStructureApp')),
  },
  {
    id: 'admin',
    title: 'Адміністрування',
    description: 'Користувачі, ролі та права доступу, організації.',
    icon: <ControlOutlined />,
    path: '/admin',
    color: '#722ed1',
    permissions: [P.users, P.organizations],
    app: lazyService(() => import('./admin/AdminApp')),
  },
];

/** Адреси розділів до розділення на сервіси → нові адреси (щоб працювали старі закладки). */
export const legacyRedirects: Record<string, string> = {
  '/documents': '/correspondence/documents',
  '/tasks': '/correspondence/tasks',
  '/dictionaries': '/correspondence/dictionaries',
  '/reports': '/correspondence/reports',
  '/settings': '/correspondence/settings',
  '/correspondents': '/parties',
  '/employees': '/org/employees',
  '/departments': '/org/departments',
  '/users': '/admin/users',
  '/access': '/admin/access',
  '/organizations': '/admin/organizations',
  '/correspondence/correspondents': '/parties',
  '/correspondence/employees': '/org/employees',
  '/correspondence/departments': '/org/departments',
  '/correspondence/users': '/admin/users',
  '/correspondence/access': '/admin/access',
  '/correspondence/organizations': '/admin/organizations',
};
