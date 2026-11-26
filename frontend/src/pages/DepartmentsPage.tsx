import { useCallback, useMemo, useState } from 'react';
import {
  Button, Card, Form, Input, Modal, Popconfirm, Select, Space, Switch, Table, Tag, Typography,
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { api } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { P } from '../auth/permissions';
import { useApiData } from '../hooks/useApi';
import { useCrud } from '../hooks/useCrud';
import { useExpandAll } from '../hooks/useExpandAll';

interface Dto {
  id: number; name: string; shortName?: string; description?: string; isActive: boolean;
  organizationId: number; organizationName: string; parentDepartmentId?: number | null; employeeCount: number;
}
interface TreeRow extends Dto { children?: TreeRow[]; }
interface OrgOpt { id: number; name: string; }

function buildTree(rows: Dto[]): TreeRow[] {
  const byId = new Map<number, TreeRow>(rows.map((r) => [r.id, { ...r }]));
  const roots: TreeRow[] = [];
  for (const r of byId.values()) {
    const parent = r.parentDepartmentId != null ? byId.get(r.parentDepartmentId) : undefined;
    if (parent) (parent.children ??= []).push(r);
    else roots.push(r);
  }
  return roots;
}

export default function DepartmentsPage() {
  const { can } = useAuth();
  const editable = can(P.departments);
  const isSuper = can(P.organizations); // супер-адмін бачить усі організації
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<Dto | null>(null);
  const [form] = Form.useForm();
  const formOrgId = Form.useWatch('organizationId', form);

  const { rows, loading, save, remove } = useCrud<Dto>('/api/departments');
  // Список організацій для вибору — лише супер-адміну
  const loadOrgs = useCallback(
    () => (isSuper ? api<OrgOpt[]>('/api/organizations') : Promise.resolve<OrgOpt[]>([])), [isSuper]);
  const { data: orgs } = useApiData(loadOrgs, []);
  const expansion = useExpandAll(useMemo(() => rows.map((r) => r.id), [rows]));

  const tree = useMemo(() => buildTree(rows), [rows]);
  const showOrg = isSuper && new Set(rows.map((r) => r.organizationId)).size > 1;

  // Кандидати в батьки: підрозділи тієї ж організації, крім самого себе
  const parentOptions = useMemo(() => {
    const targetOrg = isSuper ? formOrgId : rows[0]?.organizationId;
    return rows
      .filter((r) => r.id !== editing?.id && (targetOrg == null || r.organizationId === targetOrg))
      .map((r) => ({ value: r.id, label: r.name }));
  }, [rows, editing, isSuper, formOrgId]);

  const openAdd = (parentId?: number, orgId?: number) => {
    setEditing(null); form.resetFields();
    form.setFieldsValue({ isActive: true, parentDepartmentId: parentId ?? null, organizationId: orgId ?? null });
    setOpen(true);
  };
  const openEdit = (d: Dto) => {
    setEditing(d);
    form.setFieldsValue({
      name: d.name, shortName: d.shortName, description: d.description, isActive: d.isActive,
      parentDepartmentId: d.parentDepartmentId ?? null, organizationId: d.organizationId,
    });
    setOpen(true);
  };
  const submit = async () => {
    const v = await form.validateFields();
    if (await save(editing?.id, v)) setOpen(false);
  };

  const columns: ColumnsType<TreeRow> = [
    { title: 'Назва', dataIndex: 'name', render: (v) => <b>{v}</b> },
    { title: 'Скорочено', dataIndex: 'shortName', width: 130, render: (v) => v || '—' },
    ...(showOrg ? [{ title: 'Організація', dataIndex: 'organizationName', width: 170 } as const] : []),
    { title: 'Працівників', dataIndex: 'employeeCount', width: 120, align: 'center' as const },
    {
      title: 'Активний', dataIndex: 'isActive', width: 100, align: 'center' as const,
      render: (v: boolean) => (v ? <Tag color="green">Так</Tag> : <Tag>Ні</Tag>),
    },
    ...(editable ? [{
      title: 'Дії', key: 'a', width: 150, fixed: 'right' as const,
      render: (_: unknown, r: TreeRow) => (
        <Space>
          <Button size="small" icon={<PlusOutlined />} title="Додати підпорядкований"
            onClick={() => openAdd(r.id, r.organizationId)} />
          <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(r)} />
          <Popconfirm title="Видалити підрозділ?" okText="Так" cancelText="Ні" onConfirm={() => remove(r.id)}>
            <Button size="small" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        </Space>
      ),
    }] : []),
  ];

  return (
    <>
      <Typography.Title level={3}>Підрозділи</Typography.Title>
      <Typography.Paragraph type="secondary">
        Деревовидна структура підпорядкування підрозділів. Кнопка «+» у рядку створює
        підпорядкований підрозділ.
      </Typography.Paragraph>
      <Card>
        <div className="toolbar">
          <span className="spacer" />
          {editable && <Button type="primary" icon={<PlusOutlined />} onClick={() => openAdd()}>Додати</Button>}
        </div>
        <Table<TreeRow> rowKey="id" columns={columns} dataSource={tree} size="middle" loading={loading}
          pagination={false}
          expandable={{ ...expansion, indentSize: 24 }} />
      </Card>

      <Modal open={open} title={editing ? 'Редагування підрозділу' : 'Новий підрозділ'}
        onCancel={() => setOpen(false)} onOk={submit} okText="Зберегти" cancelText="Скасувати" destroyOnHidden>
        <Form form={form} layout="vertical" style={{ marginTop: 12 }}>
          {isSuper && (
            <Form.Item name="organizationId" label="Організація"
              rules={[{ required: true, message: 'Оберіть організацію' }]}>
              <Select disabled={!!editing} placeholder="Оберіть організацію"
                options={orgs.map((o) => ({ value: o.id, label: o.name }))}
                onChange={() => form.setFieldValue('parentDepartmentId', null)} />
            </Form.Item>
          )}
          <Form.Item name="name" label="Назва" rules={[{ required: true }]}><Input /></Form.Item>
          <Form.Item name="shortName" label="Скорочена назва"><Input /></Form.Item>
          <Form.Item name="parentDepartmentId" label="Підпорядкований підрозділу">
            <Select allowClear showSearch optionFilterProp="label"
              placeholder="— кореневий —" options={parentOptions} />
          </Form.Item>
          <Form.Item name="description" label="Опис"><Input.TextArea rows={2} /></Form.Item>
          <Form.Item name="isActive" label="Активний" valuePropName="checked"><Switch /></Form.Item>
        </Form>
      </Modal>
    </>
  );
}
