import { Card, Table, Tabs, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import LookupTab from '../components/LookupTab';
import { useLookups } from '../data/lookups';
import { P } from '../auth/permissions';

export default function DictionariesPage() {
  const { lookups } = useLookups();
  const refTable = (cols: ColumnsType<never>, data: readonly unknown[]) => (
    <Table rowKey="id" size="small" pagination={false}
      columns={cols as ColumnsType<object>} dataSource={data as object[]} />
  );
  const finalCol = { title: 'Завершальний', dataIndex: 'isFinal', width: 150,
    render: ((v: boolean) => (v ? <Tag color="green">Так</Tag> : <Tag>Ні</Tag>)) as never };

  return (
    <>
      <Typography.Title level={3}>Довідники</Typography.Title>
      <Card>
        <Tabs items={[
          { key: 't', label: 'Типи документів', children: <LookupTab kind="document-types" permission={P.dictionaries}
            code={{ label: 'Код', tooltip: 'Використовується у патерні реєстраційного номера ({type})', placeholder: 'напр. LST' }} /> },
          { key: 'c', label: 'Канали', children: <LookupTab kind="channels" permission={P.dictionaries} /> },
          { key: 'd', label: 'Напрями', children: refTable(
            [{ title: 'Код', dataIndex: 'code', width: 100 }, { title: 'Назва', dataIndex: 'name' }] as never, lookups.directions) },
          { key: 'ds', label: 'Статуси документів', children: refTable(
            [{ title: 'Назва', dataIndex: 'name' }, finalCol] as never, lookups.documentStatuses) },
          { key: 'ts', label: 'Статуси доручень', children: refTable(
            [{ title: 'Назва', dataIndex: 'name' }, finalCol] as never, lookups.taskStatuses) },
        ]} />
      </Card>
    </>
  );
}
