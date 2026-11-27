import { Card, Tabs, Typography } from 'antd';
import LookupTab from '../../components/LookupTab';
import { P } from '../../auth/permissions';

/** Довідники контрагентів: правові форми та категорії. */
export default function PartyDictionariesPage() {
  return (
    <>
      <Typography.Title level={3}>Довідники контрагентів</Typography.Title>
      <Card>
        <Tabs items={[
          { key: 'lf', label: 'Правові форми', children: <LookupTab kind="legal-forms" permission={P.parties}
            code={{ label: 'Скорочення', placeholder: 'напр. ТОВ' }} /> },
          { key: 'cat', label: 'Категорії', children: <LookupTab kind="party-categories" permission={P.parties} /> },
        ]} />
      </Card>
    </>
  );
}
