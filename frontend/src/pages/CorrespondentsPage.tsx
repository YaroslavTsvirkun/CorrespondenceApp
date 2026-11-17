import { useCallback, useEffect, useState } from 'react';
import {
  App, Button, Card, Form, Input, Modal, Popconfirm, Select, Space, Table, Typography,
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, SearchOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { api } from '../api/client';
import { useLookups, opt } from '../data/lookups';
import { useAuth, P } from '../auth/AuthContext';

interface Dto {
  id: number; name: string; correspondentTypeId: number; typeName: string;
  address?: string; phone?: string; email?: string; contactPerson?: string; note?: string;
}

export default function CorrespondentsPage() {
  const { message } = App.useApp();
  const { lookups } = useLookups();
  const { can } = useAuth();
  const editable = can(P.correspondents);
  const [rows, setRows] = useState<Dto[]>([]);
  const [loading, setLoading] = useState(false);
  const [search, setSearch] = useState('');
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<Dto | null>(null);
  const [form] = Form.useForm();

  const load = useCallback(async () => {
    setLoading(true);
    try { setRows(await api<Dto[]>('/api/correspondents')); }
    catch (e) { message.error((e as Error).message); }
    finally { setLoading(false); }
  }, [message]);
  useEffect(() => { load(); }, [load]);

  const data = rows.filter((c) => c.name.toLowerCase().includes(search.trim().toLowerCase()));

  const openAdd = () => { setEditing(null); form.resetFields(); setOpen(true); };
  const openEdit = (c: Dto) => { setEditing(c); form.setFieldsValue(c); setOpen(true); };
  const submit = async () => {
    const v = await form.validateFields();
    try {
      const body = JSON.stringify(v);
      if (editing) await api(`/api/correspondents/${editing.id}`, { method: 'PUT', body });
      else await api('/api/correspondents', { method: 'POST', body });
      setOpen(false); message.success('Збережено'); load();
    } catch (e) { message.error((e as Error).message); }
  };
  const remove = async (id: number) => {
    try { await api(`/api/correspondents/${id}`, { method: 'DELETE' }); message.success('Видалено'); load(); }
    catch (e) { message.error((e as Error).message); }
  };

  const columns: ColumnsType<Dto> = [
    { title: 'Назва / ПІБ', dataIndex: 'name', sorter: (a, b) => a.name.localeCompare(b.name) },
    { title: 'Тип', dataIndex: 'typeName', width: 170 },
    { title: 'Адреса', dataIndex: 'address', ellipsis: true, render: (v) => v || '—' },
    { title: 'Телефон', dataIndex: 'phone', width: 160, render: (v) => v || '—' },
    { title: 'Контактна особа', dataIndex: 'contactPerson', width: 180, render: (v) => v || '—' },
    ...(editable ? [{
      title: 'Дії', key: 'a', width: 110, fixed: 'right' as const,
      render: (_: unknown, r: Dto) => (
        <Space>
          <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(r)} />
          <Popconfirm title="Видалити кореспондента?" okText="Так" cancelText="Ні" onConfirm={() => remove(r.id)}>
            <Button size="small" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        </Space>
      ),
    }] : []),
  ];

  return (
    <>
      <Typography.Title level={3}>Кореспонденти</Typography.Title>
      <Card>
        <div className="toolbar">
          <Input allowClear prefix={<SearchOutlined />} placeholder="Пошук за назвою" style={{ width: 260 }}
            value={search} onChange={(e) => setSearch(e.target.value)} />
          <span className="spacer" />
          {editable && <Button type="primary" icon={<PlusOutlined />} onClick={openAdd}>Додати</Button>}
        </div>
        <Table rowKey="id" columns={columns} dataSource={data} size="middle" loading={loading}
          scroll={{ x: 900 }} pagination={{ pageSize: 10 }} />
      </Card>

      <Modal open={open} title={editing ? 'Редагування кореспондента' : 'Новий кореспондент'}
        onCancel={() => setOpen(false)} onOk={submit} okText="Зберегти" cancelText="Скасувати" destroyOnHidden>
        <Form form={form} layout="vertical" style={{ marginTop: 12 }}>
          <Form.Item name="name" label="Назва або ПІБ" rules={[{ required: true }]}><Input /></Form.Item>
          <Form.Item name="correspondentTypeId" label="Тип кореспондента" rules={[{ required: true }]}>
            <Select options={opt(lookups.correspondentTypes)} />
          </Form.Item>
          <Form.Item name="address" label="Адреса"><Input /></Form.Item>
          <Space wrap>
            <Form.Item name="phone" label="Телефон"><Input style={{ width: 220 }} /></Form.Item>
            <Form.Item name="email" label="Електронна пошта"><Input style={{ width: 220 }} /></Form.Item>
          </Space>
          <Form.Item name="contactPerson" label="Контактна особа"><Input /></Form.Item>
          <Form.Item name="note" label="Примітка"><Input.TextArea rows={2} /></Form.Item>
        </Form>
      </Modal>
    </>
  );
}
