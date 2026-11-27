import { useState } from 'react';
import { Button, Card, Form, Input, Modal, Popconfirm, Space, Switch, Table, Tag, Typography } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, SearchOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { useAuth } from '../../auth/AuthContext';
import { P } from '../../auth/permissions';
import { useCrud } from '../../hooks/useCrud';
import { isValidEdrpou } from '../../data/partyCodes';
import type { Bank } from './types';

/** Довідник банків: за МФО з IBAN рахунок контрагента отримує назву банку. */
export default function BanksPage() {
  const { can } = useAuth();
  const editable = can(P.partiesBank);
  const [search, setSearch] = useState('');
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<Bank | null>(null);
  const [form] = Form.useForm();
  const { rows, loading, save, remove } = useCrud<Bank>('/api/banks');

  const q = search.trim().toLowerCase();
  const data = rows.filter((b) => !q || b.mfo.includes(q) || b.name.toLowerCase().includes(q)
    || (b.shortName ?? '').toLowerCase().includes(q));

  const openAdd = () => { setEditing(null); form.resetFields(); form.setFieldsValue({ isActive: true }); setOpen(true); };
  const openEdit = (b: Bank) => { setEditing(b); form.setFieldsValue(b); setOpen(true); };
  const submit = async () => {
    const v = await form.validateFields();
    if (await save(editing?.id, v)) setOpen(false);
  };

  const columns: ColumnsType<Bank> = [
    { title: 'МФО', dataIndex: 'mfo', width: 100, render: (v) => <span style={{ fontFamily: 'monospace' }}>{v}</span> },
    { title: 'Назва', dataIndex: 'name', render: (v, b) => (
      <>{b.shortName ?? v}{b.shortName && <div style={{ fontSize: 12, color: '#8c8c8c' }}>{v}</div>}</>) },
    { title: 'ЄДРПОУ', dataIndex: 'edrpou', width: 110, render: (v) => v || '—' },
    { title: 'SWIFT/BIC', dataIndex: 'swiftBic', width: 130, render: (v) => v || '—' },
    { title: 'Рахунків', dataIndex: 'accountCount', width: 100, align: 'center' },
    { title: 'Статус', dataIndex: 'isActive', width: 120,
      render: (v) => (v ? <Tag color="green">Діє</Tag> : <Tag>Неактивний</Tag>) },
    ...(editable ? [{
      title: 'Дії', key: 'a', width: 110,
      render: (_: unknown, r: Bank) => (
        <Space>
          <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(r)} />
          <Popconfirm title="Видалити банк?" okText="Так" cancelText="Ні" onConfirm={() => remove(r.id)}>
            <Button size="small" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        </Space>
      ),
    }] : []),
  ];

  return (
    <>
      <Typography.Title level={3}>Банки</Typography.Title>
      <Typography.Paragraph type="secondary">
        Банк українського рахунку визначається за МФО — шістьма цифрами IBAN після контрольних (UA·· <b>305299</b>…).
        Звіряйте довідник із довідником банків НБУ.
      </Typography.Paragraph>
      <Card>
        <div className="toolbar">
          <Input allowClear prefix={<SearchOutlined />} placeholder="Пошук за МФО або назвою" style={{ width: 280 }}
            value={search} onChange={(e) => setSearch(e.target.value)} />
          <span className="spacer" />
          {editable && <Button type="primary" icon={<PlusOutlined />} onClick={openAdd}>Додати банк</Button>}
        </div>
        <Table rowKey="id" columns={columns} dataSource={data} loading={loading} size="middle" pagination={{ pageSize: 20 }} />
      </Card>

      <Modal open={open} title={editing ? 'Редагування банку' : 'Новий банк'} onCancel={() => setOpen(false)}
        onOk={submit} okText="Зберегти" cancelText="Скасувати" destroyOnHidden>
        <Form form={form} layout="vertical" style={{ marginTop: 12 }}>
          <Form.Item name="mfo" label="МФО" rules={[{ required: true, pattern: /^\d{6}$/, message: 'МФО — 6 цифр' }]}>
            <Input maxLength={6} style={{ fontFamily: 'monospace', width: 140 }} />
          </Form.Item>
          <Form.Item name="name" label="Повна назва" rules={[{ required: true, message: 'Вкажіть назву' }]}><Input /></Form.Item>
          <Form.Item name="shortName" label="Коротка назва"><Input /></Form.Item>
          <Space wrap>
            <Form.Item name="edrpou" label="ЄДРПОУ" rules={[{
              validator: (_, v) => (!v || isValidEdrpou(v) ? Promise.resolve() : Promise.reject(new Error('Невірний код ЄДРПОУ'))),
            }]}>
              <Input maxLength={8} style={{ fontFamily: 'monospace', width: 160 }} />
            </Form.Item>
            <Form.Item name="swiftBic" label="SWIFT/BIC"><Input maxLength={11} style={{ fontFamily: 'monospace', width: 180 }} /></Form.Item>
          </Space>
          <Form.Item name="isActive" label="Діє" valuePropName="checked"><Switch /></Form.Item>
        </Form>
      </Modal>
    </>
  );
}
