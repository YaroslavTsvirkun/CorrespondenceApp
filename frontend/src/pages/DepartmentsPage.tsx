import { useCallback, useMemo, useRef, useState } from 'react';
import {
  Button, Card, Form, Input, Modal, Popconfirm, Select, Space, Switch, Table, Tabs, Typography,
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, SyncOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { api } from '../api/client';
import { DEPT_SHORT_NAME_MAX, generateDeptShortName } from '../data/deptShortName';
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
  const [tab, setTab] = useState<'active' | 'inactive'>('active');

  const { rows, loading, save, remove } = useCrud<Dto>('/api/departments');
  // Список організацій для вибору — лише супер-адміну
  const loadOrgs = useCallback(
    () => (isSuper ? api<OrgOpt[]>('/api/organizations') : Promise.resolve<OrgOpt[]>([])), [isSuper]);
  const { data: orgs } = useApiData(loadOrgs, []);
  // Неактивні підрозділи винесено на окрему вкладку, щоб не захаращувати основний список.
  // Якщо батьківський підрозділ на іншій вкладці, підрозділ показується кореневим.
  const activeCount = useMemo(() => rows.filter((r) => r.isActive).length, [rows]);
  const inactiveCount = rows.length - activeCount;
  const visibleRows = useMemo(
    () => rows.filter((r) => r.isActive === (tab === 'active')), [rows, tab]);
  const expansion = useExpandAll(useMemo(() => visibleRows.map((r) => r.id), [visibleRows]));

  const tree = useMemo(() => buildTree(visibleRows), [visibleRows]);
  const showOrg = isSuper && new Set(rows.map((r) => r.organizationId)).size > 1;

  // Кандидати в батьки: підрозділи тієї ж організації, крім самого себе
  const parentOptions = useMemo(() => {
    const targetOrg = isSuper ? formOrgId : rows[0]?.organizationId;
    return rows
      .filter((r) => r.id !== editing?.id && (targetOrg == null || r.organizationId === targetOrg))
      .map((r) => ({ value: r.id, label: r.name }));
  }, [rows, editing, isSuper, formOrgId]);

  // Скорочена назва генерується з повної: для нового підрозділу — автоматично, поки її не змінили
  // вручну; будь-коли — кнопкою «Згенерувати». Унікальність — серед підрозділів тієї ж організації.
  const shortNameTouched = useRef(false);
  const makeShortName = (name: string | undefined) => {
    const orgId = isSuper ? form.getFieldValue('organizationId') : rows[0]?.organizationId;
    const taken = rows
      .filter((r) => r.id !== editing?.id && (orgId == null || r.organizationId === orgId))
      .map((r) => r.shortName);
    return generateDeptShortName(name ?? '', taken);
  };
  const onValuesChange = (changed: Partial<Dto>) => {
    if ('shortName' in changed) shortNameTouched.current = !!changed.shortName;
    else if ('name' in changed && !editing && !shortNameTouched.current)
      form.setFieldValue('shortName', makeShortName(changed.name));
  };
  const regenerateShortName = () => {
    form.setFieldValue('shortName', makeShortName(form.getFieldValue('name')));
    shortNameTouched.current = false;
  };

  const openAdd = (parentId?: number, orgId?: number) => {
    setEditing(null); form.resetFields(); shortNameTouched.current = false;
    form.setFieldsValue({ isActive: true, parentDepartmentId: parentId ?? null, organizationId: orgId ?? null });
    setOpen(true);
  };
  const openEdit = (d: Dto) => {
    setEditing(d);
    form.resetFields();
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
        підпорядкований підрозділ. Неактивні підрозділи — на окремій вкладці.
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
          {editable && <Button type="primary" icon={<PlusOutlined />} onClick={() => openAdd()}>Додати</Button>}
        </div>
        <Table<TreeRow> rowKey="id" columns={columns} dataSource={tree} size="middle" loading={loading}
          pagination={false}
          locale={{ emptyText: tab === 'active' ? 'Немає активних підрозділів' : 'Немає неактивних підрозділів' }}
          expandable={{ ...expansion, indentSize: 24 }} />
      </Card>

      <Modal open={open} title={editing ? 'Редагування підрозділу' : 'Новий підрозділ'}
        onCancel={() => setOpen(false)} onOk={submit} okText="Зберегти" cancelText="Скасувати" forceRender>
        <Form form={form} layout="vertical" style={{ marginTop: 12 }} onValuesChange={onValuesChange}>
          {isSuper && (
            <Form.Item name="organizationId" label="Організація"
              rules={[{ required: true, message: 'Оберіть організацію' }]}>
              <Select disabled={!!editing} placeholder="Оберіть організацію"
                options={orgs.map((o) => ({ value: o.id, label: o.name }))}
                onChange={() => form.setFieldValue('parentDepartmentId', null)} />
            </Form.Item>
          )}
          <Form.Item name="name" label="Назва" rules={[{ required: true }]}><Input /></Form.Item>
          <Form.Item label="Скорочена назва"
            tooltip="Генерується з назви: абревіатура (ВК, ГОУ (J-3)) або усічення одного слова (Канц.)">
            <Space.Compact style={{ width: '100%' }}>
              <Form.Item name="shortName" noStyle><Input maxLength={DEPT_SHORT_NAME_MAX} /></Form.Item>
              <Button icon={<SyncOutlined />} onClick={regenerateShortName} title="Згенерувати скорочену назву з повної">
                Згенерувати
              </Button>
            </Space.Compact>
          </Form.Item>
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
