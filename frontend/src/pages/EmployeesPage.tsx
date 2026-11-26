import { useState } from 'react';
import {
  Button, Card, Form, Input, Modal, Popconfirm, Select, Space, Switch, Table, Tag, Typography,
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { useLookups } from '../data/lookups';
import { opt } from '../data/options';
import { useAuth } from '../auth/AuthContext';
import { P } from '../auth/permissions';
import { useCrud } from '../hooks/useCrud';

interface Dto {
  id: number; fullName: string; positionName: string; departmentId: number;
  departmentName: string; phone?: string; email?: string; isActive: boolean;
}

export default function EmployeesPage() {
  const { lookups } = useLookups();
  const { can } = useAuth();
  const editable = can(P.employees);
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<Dto | null>(null);
  const [form] = Form.useForm();

  const { rows, loading, save, remove } = useCrud<Dto>('/api/employees');

  const openAdd = () => { setEditing(null); form.resetFields(); form.setFieldsValue({ isActive: true }); setOpen(true); };
  const openEdit = (e: Dto) => { setEditing(e); form.setFieldsValue(e); setOpen(true); };
  const submit = async () => {
    const v = await form.validateFields();
    if (await save(editing?.id, v)) setOpen(false);
  };

  const columns: ColumnsType<Dto> = [
    { title: 'ПІБ', dataIndex: 'fullName', sorter: (a, b) => a.fullName.localeCompare(b.fullName) },
    { title: 'Посада', dataIndex: 'positionName', width: 200 },
    { title: 'Підрозділ', dataIndex: 'departmentName', width: 180 },
    { title: 'Телефон', dataIndex: 'phone', width: 150, render: (v) => v || '—' },
    { title: 'Ел. пошта', dataIndex: 'email', width: 180, render: (v) => v || '—' },
    { title: 'Активний', dataIndex: 'isActive', width: 110, align: 'center',
      render: (v: boolean) => (v ? <Tag color="green">Так</Tag> : <Tag>Ні</Tag>) },
    ...(editable ? [{
      title: 'Дії', key: 'a', width: 110, fixed: 'right' as const,
      render: (_: unknown, r: Dto) => (
        <Space>
          <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(r)} />
          <Popconfirm title="Видалити працівника?" okText="Так" cancelText="Ні" onConfirm={() => remove(r.id)}>
            <Button size="small" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        </Space>
      ),
    }] : []),
  ];

  return (
    <>
      <Typography.Title level={3}>Працівники</Typography.Title>
      <Card>
        <div className="toolbar">
          <span className="spacer" />
          {editable && <Button type="primary" icon={<PlusOutlined />} onClick={openAdd}>Додати</Button>}
        </div>
        <Table rowKey="id" columns={columns} dataSource={rows} size="middle" loading={loading}
          scroll={{ x: 1000 }} pagination={{ pageSize: 10 }} />
      </Card>

      <Modal open={open} title={editing ? 'Редагування працівника' : 'Новий працівник'}
        onCancel={() => setOpen(false)} onOk={submit} okText="Зберегти" cancelText="Скасувати" destroyOnHidden>
        <Form form={form} layout="vertical" style={{ marginTop: 12 }}>
          <Form.Item name="fullName" label="ПІБ" rules={[{ required: true }]}><Input /></Form.Item>
          <Form.Item name="positionName" label="Посада" rules={[{ required: true }]}><Input /></Form.Item>
          <Form.Item name="departmentId" label="Підрозділ" rules={[{ required: true }]}>
            <Select options={opt(lookups.departments)} />
          </Form.Item>
          <Space wrap>
            <Form.Item name="phone" label="Телефон"><Input style={{ width: 220 }} /></Form.Item>
            <Form.Item name="email" label="Електронна пошта"><Input style={{ width: 220 }} /></Form.Item>
          </Space>
          <Form.Item name="isActive" label="Активний" valuePropName="checked"><Switch /></Form.Item>
        </Form>
      </Modal>
    </>
  );
}
