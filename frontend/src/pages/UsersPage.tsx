import { useMemo, useState } from 'react';
import {
  Button, Card, Form, Input, Modal, Popconfirm, Select, Space, Switch, Table, Tabs, Tag, Typography,
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, UserOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { api } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { useApiData } from '../hooks/useApi';
import { useCrud } from '../hooks/useCrud';

interface UserDto {
  id: number; userName: string; fullName: string;
  organizationId?: number | null; organizationName?: string | null;
  employeeId?: number | null; employeeName?: string | null;
  isActive: boolean; isSuperAdmin: boolean; roles: string[];
}
interface OrgOpt { id: number; name: string; }
interface RoleOpt { id: number; name: string; isSuper: boolean; }
interface EmpOpt { id: number; fullName: string; departmentName: string; organizationId: number; }
interface Options { isSuperAdmin: boolean; organizations: OrgOpt[]; roles: RoleOpt[]; employees: EmpOpt[]; }

const EMPTY_OPTIONS: Options = { isSuperAdmin: false, organizations: [], roles: [], employees: [] };
const loadOptions = () => api<Options>('/api/users/options');

export default function UsersPage() {
  const { user: me } = useAuth();
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<UserDto | null>(null);
  const [form] = Form.useForm();
  const orgId = Form.useWatch('organizationId', form);
  const [tab, setTab] = useState<'active' | 'inactive'>('active');

  const { rows, loading, save, remove } = useCrud<UserDto>('/api/users');
  // Неактивні користувачі винесено на окрему вкладку, щоб не захаращувати основний список
  const activeCount = useMemo(() => rows.filter((r) => r.isActive).length, [rows]);
  const inactiveCount = rows.length - activeCount;
  const visibleRows = useMemo(
    () => rows.filter((r) => r.isActive === (tab === 'active')), [rows, tab]);
  const { data: opts } = useApiData(loadOptions, EMPTY_OPTIONS);

  const roleIdByName = useMemo(
    () => new Map(opts.roles.map((r) => [r.name, r.id])),
    [opts.roles],
  );
  const empOptions = useMemo(
    () => opts.employees
      .filter((e) => orgId == null || e.organizationId === orgId)
      .map((e) => ({ value: e.id, label: `${e.fullName} — ${e.departmentName}` })),
    [opts.employees, orgId],
  );

  const openAdd = () => {
    setEditing(null); form.resetFields();
    form.setFieldsValue({
      isActive: true, roleIds: [],
      organizationId: opts.isSuperAdmin ? null : (opts.organizations[0]?.id ?? null),
    });
    setOpen(true);
  };
  const openEdit = (u: UserDto) => {
    setEditing(u);
    form.setFieldsValue({
      userName: u.userName, password: '', fullName: u.fullName,
      organizationId: u.organizationId ?? null, employeeId: u.employeeId ?? null,
      isActive: u.isActive,
      roleIds: u.roles.map((n) => roleIdByName.get(n)).filter((x): x is number => x != null),
    });
    setOpen(true);
  };
  const submit = async () => {
    const v = await form.validateFields();
    if (await save(editing?.id, { ...v, password: v.password ? v.password : null })) setOpen(false);
  };

  const columns: ColumnsType<UserDto> = [
    { title: 'ПІБ', dataIndex: 'fullName', render: (v, r) => (
      <span><b>{v}</b>{r.id === me?.userId && <Tag color="gold" style={{ marginInlineStart: 8 }}>ви</Tag>}</span>
    ) },
    { title: 'Логін', dataIndex: 'userName', width: 140 },
    {
      title: 'Організація', dataIndex: 'organizationName', width: 180,
      render: (v, r) => (r.isSuperAdmin ? <Tag color="purple">супер-адмін</Tag> : (v || '—')),
    },
    { title: 'Працівник', dataIndex: 'employeeName', width: 180, render: (v) => v || '—' },
    {
      title: 'Ролі', dataIndex: 'roles', render: (rs: string[]) =>
        rs.length ? rs.map((r) => <Tag key={r} color="blue">{r}</Tag>) : '—',
    },
    {
      title: 'Дії', key: 'a', width: 110, fixed: 'right',
      render: (_, r) => (
        <Space>
          <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(r)} />
          <Popconfirm title="Видалити користувача?" okText="Так" cancelText="Ні"
            disabled={r.id === me?.userId} onConfirm={() => remove(r.id)}>
            <Button size="small" danger icon={<DeleteOutlined />} disabled={r.id === me?.userId} />
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <>
      <Typography.Title level={3}><UserOutlined /> Користувачі</Typography.Title>
      <Typography.Paragraph type="secondary">
        {opts.isSuperAdmin
          ? 'Ви супер-адміністратор: бачите користувачів усіх організацій і призначаєте їм організацію та ролі.'
          : 'Ви адміністратор організації: керуєте користувачами лише своєї організації.'}
      </Typography.Paragraph>
      <Card>
        <div className="toolbar">
          <Tabs activeKey={tab} onChange={(k) => setTab(k as 'active' | 'inactive')}
            tabBarStyle={{ margin: 0 }}
            items={[
              { key: 'active', label: `Активні (${activeCount})` },
              { key: 'inactive', label: `Неактивні (${inactiveCount})` },
            ]} />
          <span className="spacer" />
          <Button type="primary" icon={<PlusOutlined />} onClick={openAdd}>Додати користувача</Button>
        </div>
        <Table rowKey="id" columns={columns} dataSource={visibleRows} loading={loading} size="middle" pagination={{ pageSize: 10 }}
          locale={{ emptyText: tab === 'active' ? 'Немає активних користувачів' : 'Немає неактивних користувачів' }} />
      </Card>

      <Modal open={open} title={editing ? `Користувач: ${editing.userName}` : 'Новий користувач'}
        onCancel={() => setOpen(false)} onOk={submit} okText="Зберегти" cancelText="Скасувати" destroyOnHidden>
        <Form form={form} layout="vertical" style={{ marginTop: 12 }}>
          <Form.Item name="fullName" label="ПІБ" rules={[{ required: true, message: 'Вкажіть ПІБ' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="userName" label="Логін" rules={[{ required: true, message: 'Вкажіть логін' }]}>
            <Input autoComplete="off" />
          </Form.Item>
          <Form.Item name="password" label={editing ? 'Новий пароль (залишіть порожнім — без змін)' : 'Пароль'}
            rules={editing ? [] : [{ required: true, message: 'Вкажіть пароль' }, { min: 5, message: 'Мінімум 5 символів' }]}>
            <Input.Password autoComplete="new-password" placeholder={editing ? '••••••' : ''} />
          </Form.Item>
          <Form.Item name="organizationId" label="Організація"
            extra={opts.isSuperAdmin ? 'Порожньо = супер-адміністратор (бачить усі організації)' : undefined}>
            <Select
              allowClear={opts.isSuperAdmin}
              disabled={!opts.isSuperAdmin}
              placeholder={opts.isSuperAdmin ? '— супер-адмін —' : undefined}
              options={opts.organizations.map((o) => ({ value: o.id, label: o.name }))}
              onChange={() => form.setFieldValue('employeeId', null)}
            />
          </Form.Item>
          <Form.Item name="employeeId" label="Прив'язаний працівник">
            <Select allowClear showSearch optionFilterProp="label"
              placeholder="— не прив'язано —" options={empOptions} />
          </Form.Item>
          <Form.Item name="roleIds" label="Ролі" rules={[{ required: true, message: 'Оберіть хоча б одну роль' }]}>
            <Select mode="multiple"
              options={opts.roles.map((r) => ({ value: r.id, label: r.isSuper ? `${r.name} (супер)` : r.name }))} />
          </Form.Item>
          <Form.Item name="isActive" label="Активний" valuePropName="checked"><Switch /></Form.Item>
        </Form>
      </Modal>
    </>
  );
}
