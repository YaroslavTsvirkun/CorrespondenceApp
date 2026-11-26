import type { ReactNode } from 'react';
import { MailOutlined } from '@ant-design/icons';
import { P } from '../auth/permissions';

// Каталог сервісів, що показуються на стартовій сторінці після входу.
// Щоб додати новий сервіс: опишіть його тут і зареєструйте маршрут `${path}/*` в App.tsx.
export interface ServiceDefinition {
  id: string;
  title: string;
  description: string;
  icon: ReactNode;
  /** Базовий шлях сервісу (усі його сторінки — під ним) */
  path: string;
  /** Колір акценту картки */
  color: string;
  /** Чи доступний сервіс користувачеві з такими пермішенами */
  isAvailable: (canAny: (...permissions: string[]) => boolean) => boolean;
}

// Усі коди пермішенів підсистеми кореспонденції (див. auth/permissions.ts)
const correspondencePermissions = Object.values(P).flat();

export const services: ServiceDefinition[] = [
  {
    id: 'correspondence',
    title: 'Реєстрація кореспонденції',
    description: 'Реєстрація вхідних і вихідних документів, доручення, контроль виконання, звіти.',
    icon: <MailOutlined />,
    path: '/correspondence',
    color: '#1d6fb8',
    isAvailable: (canAny) => canAny(...correspondencePermissions),
  },
];
