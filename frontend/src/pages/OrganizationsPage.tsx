import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  App, Button, Card, Form, Input, Modal, Popconfirm, Select, Space, Switch, Table, Tag, Typography,
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, ApartmentOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { api } from '../api/client';

interface Dto {
  id: number; name: string; code?: string; parentOrganizationId?: number | null;
  isActive: boolean; registrationNumberPattern: string; departmentCount: number; userCount: number;
}
interface TreeRow extends Dto { children?: TreeRow[]; }

// Побудова дерева з плаского списку за parentOrganizationId
function buildTree(rows: Dto[]): TreeRow[] {
  const byId = new Map<number, TreeRow>(rows.map((r) => [r.id, { ...r }]));
  const roots: TreeRow[] = [];
  for (const r of byId.values()) {
    const parent = r.parentOrganizationId != null ? byId.get(r.parentOrganizationId) : undefined;
    if (parent) (parent.children ??= []).push(r);
    else roots.push(r);
  }
  return roots;
}

export default function OrganizationsPage() {
  const { message } = App.useApp();
  const [rows, setRows] = useState<Dto[]>([]);
  const [expandedKeys, setExpandedKeys] = useState<number[]>([]);
  const [loading, setLoading] = useState(false);
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<Dto | null>(null);
  const [form] = Form.useForm();

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const data = await api<Dto[]>('/api/organizations');
      setRows(data);
      setExpandedKeys(data.map((d) => d.id)); // дерево розгорнуте за замовчуванням
    } catch (e) { message.error((e as Error).message); }
    finally { setLoading(false); }
  }, [message]);
  useEffect(() => { load(); }, [load]);

  const tree = useMemo(() => buildTree(rows), [rows]);
  // Кандидати в батьки: усі, крім самого себе (захист від циклів — на бекенді)
  const parentOptions = useMemo(
    () => rows.filter((r) => r.id !== editing?.id).map((r) => ({ value: r.id, label: r.name })),
    [rows, editing],
  );

  const openAdd = (parentId?: number) => {
    setEditing(null); form.resetFields();
    form.setFieldsValue({ isActive: true, parentOrganizationId: parentId ?? null });
    setOpen(true);
  };
  const openEdit = (d: Dto) => {
    setEditing(d);
    form.setFieldsValue({ name: d.name, code: d.code, parentOrganizationId: d.parentOrganizationId ?? null,
      isActive: d.isActive, registrationNumberPattern: d.registrationNumberPattern });
    setOpen(true);
  };
  const submit = async () => {
    const v = await form.validateFields();
    try {
      const body = JSON.stringify(v);
      if (editing) await api(`/api/organizations/${editing.id}`, { method: 'PUT', body });
      else await api('/api/organizations', { method: 'POST', body });
      setOpen(false); message.success('Збережено'); load();
    } catch (e) { message.error((e as Error).message); }
  };
  const remove = async (id: number) => {
    try { await api(`/api/organizations/${id}`, { method: 'DELETE' }); message.success('Видалено'); load(); }
    catch (e) { message.error((e as Error).message); }
  };

  const columns: ColumnsType<TreeRow> = [
    { title: 'Організація', dataIndex: 'name', render: (v) => <b>{v}</b> },
    { title: 'Код', dataIndex: 'code', width: 130, render: (v) => (v ? <Tag>{v}</Tag> : '—') },
    { title: 'Підрозділів', dataIndex: 'departmentCount', width: 120, align: 'center' },
    { title: 'Користувачів', dataIndex: 'userCount', width: 120, align: 'center' },
    {
      title: 'Активна', dataIndex: 'isActive', width: 100, align: 'center',
      render: (v: boolean) => (v ? <Tag color="green">Так</Tag> : <Tag>Ні</Tag>),
    },
    {
      title: 'Дії', key: 'a', width: 150, fixed: 'right',
      render: (_, r) => (
        <Space>
          <Button size="small" icon={<PlusOutlined />} title="Додати дочірню" onClick={() => openAdd(r.id)} />
          <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(r)} />
          <Popconfirm title="Видалити організацію?" okText="Так" cancelText="Ні" onConfirm={() => remove(r.id)}>
            <Button size="small" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <>
      <Typography.Title level={3}><ApartmentOutlined /> Організації</Typography.Title>
      <Typography.Paragraph type="secondary">
        Деревовидна структура організацій. Кожна організація ізольована: її користувачі бачать
        лише власні дані. Адміністратор організації керує лише своєю організацією.
      </Typography.Paragraph>
      <Card>
        <div className="toolbar">
          <span className="spacer" />
          <Button type="primary" icon={<PlusOutlined />} onClick={() => openAdd()}>Додати організацію</Button>
        </div>
        <Table<TreeRow>
          rowKey="id" columns={columns} dataSource={tree} loading={loading}
          pagination={false} size="middle"
          expandable={{ expandedRowKeys: expandedKeys, onExpandedRowsChange: (k) => setExpandedKeys(k as number[]), indentSize: 24 }}
        />
      </Card>

      <Modal open={open} title={editing ? `Організація: ${editing.name}` : 'Нова організація'}
        onCancel={() => setOpen(false)} onOk={submit} okText="Зберегти" cancelText="Скасувати" destroyOnHidden>
        <Form form={form} layout="vertical" style={{ marginTop: 12 }}>
          <Form.Item name="name" label="Назва" rules={[{ required: true, message: 'Вкажіть назву' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="code" label="Код (скорочення)"><Input maxLength={30} /></Form.Item>
          <Form.Item name="parentOrganizationId" label="Материнська організація">
            <Select allowClear placeholder="— коренева —" options={parentOptions} />
          </Form.Item>
          <Form.Item name="registrationNumberPattern" label="Патерн реєстраційного номера"
            tooltip="Токени: {seq}/{seq:000}, {year}, {yy}, {type}, {dir}">
            <Input placeholder="{type}-{seq:000}/{year}" style={{ fontFamily: 'monospace' }} />
          </Form.Item>
          <Form.Item name="isActive" label="Активна" valuePropName="checked"><Switch /></Form.Item>
        </Form>
      </Modal>
    </>
  );
}
