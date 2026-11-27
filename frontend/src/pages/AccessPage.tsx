import { useMemo, useState } from 'react';
import {
  Button, Card, Checkbox, Drawer, Form, Input, Popconfirm, Space, Table, Tag, Typography,
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { api } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { P } from '../auth/permissions';
import { useApiData } from '../hooks/useApi';
import { useCrud } from '../hooks/useCrud';

interface RoleDto { id: number; name: string; description?: string; userCount: number; permissions: string[]; }
interface PermDto { code: string; description?: string; category?: string; }

const loadPermissions = () => api<PermDto[]>('/api/access/permissions');

export default function AccessPage() {
  // Ролі спільні для всіх організацій — змінювати їх може лише супер-адміністратор
  const canManageRoles = useAuth().can(P.organizations);
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<RoleDto | null>(null);
  const [selected, setSelected] = useState<string[]>([]);
  const [form] = Form.useForm();

  const { rows: roles, save: saveRole, remove: removeRole } = useCrud<RoleDto>('/api/access/roles');
  const { data: perms } = useApiData(loadPermissions, []);

  const grouped = useMemo(() => {
    const g: Record<string, PermDto[]> = {};
    for (const p of perms) (g[p.category ?? 'Інше'] ??= []).push(p);
    return g;
  }, [perms]);

  const openAdd = () => { setEditing(null); setSelected([]); form.resetFields(); setOpen(true); };
  const openEdit = (r: RoleDto) => {
    setEditing(r); setSelected(r.permissions);
    form.resetFields();
    form.setFieldsValue({ name: r.name, description: r.description });
    setOpen(true);
  };

  const save = async () => {
    const v = await form.validateFields();
    const values = { name: v.name, description: v.description, permissions: selected };
    if (await saveRole(editing?.id, values, editing ? 'Роль оновлено' : 'Роль створено')) setOpen(false);
  };

  const remove = (r: RoleDto) => removeRole(r.id, 'Роль видалено');

  const columns: ColumnsType<RoleDto> = [
    { title: 'Роль', dataIndex: 'name', width: 180, render: (v) => <b>{v}</b> },
    { title: 'Опис', dataIndex: 'description', render: (v) => v || '—' },
    { title: 'Прав', dataIndex: 'permissions', width: 90, align: 'center', render: (p: string[]) => <Tag color="blue">{p.length}</Tag> },
    { title: 'Користувачів', dataIndex: 'userCount', width: 130, align: 'center' },
    ...(!canManageRoles ? [] : [{
      title: 'Дії', key: 'a', width: 110,
      render: (_, r) => (
        <Space>
          <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(r)} />
          <Popconfirm title="Видалити роль?" okText="Так" cancelText="Ні" onConfirm={() => remove(r)}>
            <Button size="small" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        </Space>
      ),
    } as ColumnsType<RoleDto>[number]]),
  ];

  return (
    <>
      <Typography.Title level={3}>Ролі та доступ</Typography.Title>
      <Typography.Paragraph type="secondary">
        Пермішени та їх призначення ролям зберігаються в базі даних і застосовуються
        одразу (без перевидання токена).
        {!canManageRoles && ' Ролі спільні для всіх організацій, тож змінювати їх може лише супер-адміністратор.'}
      </Typography.Paragraph>
      <Card>
        {canManageRoles && (
          <div className="toolbar">
            <span className="spacer" />
            <Button type="primary" icon={<PlusOutlined />} onClick={openAdd}>Додати роль</Button>
          </div>
        )}
        <Table rowKey="id" columns={columns} dataSource={roles} pagination={false} size="middle" />
      </Card>

      <Drawer
        open={open} size="large" onClose={() => setOpen(false)} forceRender
        title={editing ? `Роль: ${editing.name}` : 'Нова роль'}
        extra={<Button type="primary" onClick={save}>Зберегти</Button>}
      >
        <Form form={form} layout="vertical">
          <Form.Item name="name" label="Назва ролі" rules={[{ required: true, message: 'Вкажіть назву' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="description" label="Опис"><Input /></Form.Item>
        </Form>
        <Typography.Title level={5}>Пермішени</Typography.Title>
        {Object.entries(grouped).map(([cat, items]) => {
          const codes = items.map((p) => p.code);
          return (
            <Card key={cat} size="small" title={cat} style={{ marginBottom: 12 }}>
              <Checkbox.Group
                style={{ display: 'flex', flexDirection: 'column', gap: 6 }}
                options={items.map((p) => ({ label: `${p.description ?? p.code}`, value: p.code }))}
                value={selected.filter((c) => codes.includes(c))}
                onChange={(vals) => setSelected([
                  ...selected.filter((c) => !codes.includes(c)),
                  ...(vals as string[]),
                ])}
              />
            </Card>
          );
        })}
      </Drawer>
    </>
  );
}
