import { useState } from 'react';
import {
  Button, Card, Form, Input, Modal, Popconfirm, Space, Table, Tabs, Tag, Typography,
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { useLookups } from '../data/lookups';
import { useAuth } from '../auth/AuthContext';
import { P } from '../auth/permissions';
import { useCrud } from '../hooks/useCrud';

interface Item { id: number; name: string; description?: string; code?: string; }

function LookupTab({ kind, withCode }: { kind: string; withCode?: boolean }) {
  const { can } = useAuth();
  const { reload } = useLookups();
  const editable = can(P.dictionaries);
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<Item | null>(null);
  const [form] = Form.useForm();

  // Після змін оновлюються й спільні lookups (списки у формах інших сторінок)
  const { rows, loading, save, remove } = useCrud<Item>(`/api/dictionaries/${kind}`, reload);

  const openAdd = () => { setEditing(null); form.resetFields(); setOpen(true); };
  const openEdit = (x: Item) => { setEditing(x); form.setFieldsValue(x); setOpen(true); };
  const submit = async () => {
    const v = await form.validateFields();
    if (await save(editing?.id, v)) setOpen(false);
  };

  const columns: ColumnsType<Item> = [
    { title: 'Назва', dataIndex: 'name' },
    ...(withCode ? [{ title: 'Код', dataIndex: 'code', width: 120,
      render: ((v: string) => (v ? <Tag>{v}</Tag> : '—')) as never }] : []),
    { title: 'Опис', dataIndex: 'description', render: (v) => v || '—' },
    ...(editable ? [{
      title: 'Дії', key: 'a', width: 110,
      render: (_: unknown, r: Item) => (
        <Space>
          <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(r)} />
          <Popconfirm title="Видалити запис?" okText="Так" cancelText="Ні" onConfirm={() => remove(r.id)}>
            <Button size="small" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        </Space>
      ),
    }] : []),
  ];

  return (
    <>
      {editable && (
        <div className="toolbar"><span className="spacer" />
          <Button type="primary" icon={<PlusOutlined />} onClick={openAdd}>Додати</Button>
        </div>
      )}
      <Table rowKey="id" columns={columns} dataSource={rows} size="small" loading={loading} pagination={false} />
      <Modal open={open} title={editing ? 'Редагування' : 'Новий запис'} onCancel={() => setOpen(false)}
        onOk={submit} okText="Зберегти" cancelText="Скасувати" destroyOnHidden>
        <Form form={form} layout="vertical" style={{ marginTop: 12 }}>
          <Form.Item name="name" label="Назва" rules={[{ required: true }]}><Input /></Form.Item>
          {withCode && (
            <Form.Item name="code" label="Код" tooltip="Використовується у патерні реєстраційного номера ({type})">
              <Input maxLength={20} placeholder="напр. LST" style={{ fontFamily: 'monospace' }} />
            </Form.Item>
          )}
          <Form.Item name="description" label="Опис"><Input.TextArea rows={2} /></Form.Item>
        </Form>
      </Modal>
    </>
  );
}

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
          { key: 't', label: 'Типи документів', children: <LookupTab kind="document-types" withCode /> },
          { key: 'c', label: 'Канали', children: <LookupTab kind="channels" /> },
          { key: 'ct', label: 'Типи кореспондентів', children: <LookupTab kind="correspondent-types" /> },
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
