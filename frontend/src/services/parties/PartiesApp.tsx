import { ContactsOutlined, IdcardOutlined, BankOutlined, BookOutlined } from '@ant-design/icons';
import ServiceLayout from '../../components/ServiceLayout';
import PartiesPage from './PartiesPage';
import BanksPage from './BanksPage';
import PartyDictionariesPage from './PartyDictionariesPage';
import { useAuth } from '../../auth/AuthContext';
import { P } from '../../auth/permissions';

/**
 * Сервіс «Контрагенти»: реєстр юридичних і фізичних осіб (реквізити, адреси, контакти, особи,
 * банківські рахунки), довідник банків і довідники правових форм та категорій.
 */
export default function PartiesApp() {
  const { can, canAny } = useAuth();

  return (
    <ServiceLayout
      base="/parties"
      icon={<IdcardOutlined />}
      siderTitle="Контрагенти"
      title="Реєстр контрагентів"
      sections={[
        { key: '', icon: <ContactsOutlined />, label: 'Контрагенти', element: <PartiesPage />, show: canAny(P.parties, P.partiesBank) },
        { key: 'banks', icon: <BankOutlined />, label: 'Банки', element: <BanksPage />, show: canAny(P.parties, P.partiesBank) },
        { key: 'dictionaries', icon: <BookOutlined />, label: 'Довідники', element: <PartyDictionariesPage />, show: can(P.parties) },
      ]}
    />
  );
}
