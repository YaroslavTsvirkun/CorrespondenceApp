import { ContactsOutlined, IdcardOutlined } from '@ant-design/icons';
import ServiceLayout from '../../components/ServiceLayout';
import CorrespondentsPage from '../../pages/CorrespondentsPage';
import { useAuth } from '../../auth/AuthContext';
import { P } from '../../auth/permissions';

/**
 * Сервіс «Контрагенти»: реєстр юридичних і фізичних осіб, з якими працюють усі сервіси.
 * Поки що — наявний довідник кореспондентів; розширена модель (ЄДРПОУ, адреси, контакти,
 * банківські рахунки) — наступний етап.
 */
export default function PartiesApp() {
  const { can } = useAuth();

  return (
    <ServiceLayout
      base="/parties"
      icon={<IdcardOutlined />}
      siderTitle="Контрагенти"
      title="Реєстр контрагентів"
      sections={[
        { key: '', icon: <ContactsOutlined />, label: 'Контрагенти', element: <CorrespondentsPage />, show: can(P.correspondents) },
      ]}
    />
  );
}
