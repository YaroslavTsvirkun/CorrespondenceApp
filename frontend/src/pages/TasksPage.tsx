import { useCallback, useEffect, useState } from 'react';
import {
  App, Button, Card, DatePicker, Form, Input, Modal, Popconfirm, Select, Space, Table, Tag, Typography,
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import dayjs from 'dayjs';
import { api } from '../api/client';
import { useLookups, opt } from '../data/lookups';
import { useAuth, P } from '../auth/AuthContext';

interface Dto {
  id: number; documentId: number; documentNumber: string; documentTitle: string;
  executorEmployeeId: number; executorName: string; statusId: number; statusName: string;
  taskText: string; resolutionText: string; dueDate: string; completedAt?: string; resultText?: string;
}
interface DocOpt { value: number; label: string; }

function taskColor(name: string): string {
  switch (name) {
    case 'Призначено': return 'blue';
    case 'У роботі': return 'gold';
    case 'Виконано': return 'green';
    case 'Прострочено': return 'red';
    case 'Скасовано': return 'default';
    default: return 'default';
  }
}

export default function TasksPage() {
  const { message } = App.useApp();
  const { lookups } = useLookups();
  const { can, canAny } = useAuth();
  const canEdit = canAny(P.tasksAssign, P.tasksExecute);
  const canAssign = can(P.tasksAssign);
  const today = dayjs();

  const [rows, setRows] = useState<Dto[]>([]);
  const [docs, setDocs] = useState<DocOpt[]>([]);
  const [loading, setLoading] = useState(false);
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<Dto | null>(null);
  const [form] = Form.useForm();

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setRows(await api<Dto[]>('/api/tasks'));
      const dd = await api<{ items: { id: number; registrationNumber: string; registrationYear: number; title: string }[] }>('/api/documents?pageSize=200');
      setDocs(dd.items.map((d) => ({ value: d.id, label: `${d.registrationNumber}/${d.registrationYear} — ${d.title}` })));
    } catch (e) { message.error((e as Error).message); }
    finally { setLoading(false); }
  }, [message]);
  useEffect(() => { load(); }, [load]);

  const isFinal = (statusId: number) => lookups.taskStatuses.find((s) => s.id === statusId)?.isFinal ?? false;
  const isOverdue = (t: Dto) => !isFinal(t.statusId) && dayjs(t.dueDate).isBefore(today, 'day');

  const openAdd = () => {
    setEditing(null); form.resetFields();
    form.setFieldsValue({ statusId: lookups.taskStatuses[0]?.id, dueDate: dayjs().add(10, 'day') });
    setOpen(true);
  };
  const openEdit = (t: Dto) => {
    setEditing(t);
    form.setFieldsValue({ ...t, dueDate: dayjs(t.dueDate), completedAt: t.completedAt ? dayjs(t.completedAt) : undefined });
    setOpen(true);
  };
  const submit = async () => {
    const v = await form.validateFields();
    const body = JSON.stringify({
      ...v,
      dueDate: (v.dueDate as dayjs.Dayjs).format('YYYY-MM-DD'),
      completedAt: v.completedAt ? (v.completedAt as dayjs.Dayjs).format('YYYY-MM-DD') : null,
    });
    try {
      if (editing) await api(`/api/tasks/${editing.id}`, { method: 'PUT', body });
      else await api('/api/tasks', { method: 'POST', body });
      setOpen(false); message.success('Збережено'); load();
    } catch (e) { message.error((e as Error).message); }
  };
  const remove = async (id: number) => {
    try { await api(`/api/tasks/${id}`, { method: 'DELETE' }); message.success('Видалено'); load(); }
    catch (e) { message.error((e as Error).message); }
  };

  const columns: ColumnsType<Dto> = [
    { title: 'Документ', key: 'doc', ellipsis: true, render: (_, r) => `${r.documentNumber} — ${r.documentTitle}` },
    { title: 'Завдання', dataIndex: 'taskText', ellipsis: true },
    { title: 'Виконавець', dataIndex: 'executorName', width: 200 },
    { title: 'Строк', dataIndex: 'dueDate', width: 130, sorter: (a, b) => a.dueDate.localeCompare(b.dueDate),
      render: (v, r) => <span style={{ color: isOverdue(r) ? '#cf1322' : undefined, fontWeight: isOverdue(r) ? 600 : 400 }}>{dayjs(v).format('DD.MM.YYYY')}</span> },
    { title: 'Статус', dataIndex: 'statusName', width: 140,
      render: (v, r) => { const n = isOverdue(r) ? 'Прострочено' : v; return <Tag color={taskColor(n)}>{n}</Tag>; } },
    ...(canEdit ? [{
      title: 'Дії', key: 'a', width: 110, fixed: 'right' as const,
      render: (_: unknown, r: Dto) => (
        <Space>
          <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(r)} />
          {canAssign && (
            <Popconfirm title="Видалити доручення?" okText="Так" cancelText="Ні" onConfirm={() => remove(r.id)}>
              <Button size="small" danger icon={<DeleteOutlined />} />
            </Popconfirm>
          )}
        </Space>
      ),
    }] : []),
  ];

  return (
    <>
      <Typography.Title level={3}>Доручення</Typography.Title>
      <Card>
        <div className="toolbar">
          <span className="spacer" />
          {canAssign && <Button type="primary" icon={<PlusOutlined />} onClick={openAdd}>Додати доручення</Button>}
        </div>
        <Table rowKey="id" columns={columns} dataSource={rows} size="middle" loading={loading}
          scroll={{ x: 900 }} pagination={{ pageSize: 8 }} />
      </Card>

      <Modal open={open} title={editing ? 'Редагування доручення' : 'Нове доручення'}
        onCancel={() => setOpen(false)} onOk={submit} okText="Зберегти" cancelText="Скасувати" width={680} destroyOnHidden>
        <Form form={form} layout="vertical" style={{ marginTop: 12 }}>
          <Form.Item name="documentId" label="Документ" rules={[{ required: true }]}>
            <Select showSearch optionFilterProp="label" options={docs} disabled={!!editing} />
          </Form.Item>
          <Form.Item name="resolutionText" label="Текст резолюції" rules={[{ required: true }]}><Input /></Form.Item>
          <Form.Item name="taskText" label="Завдання виконавцю" rules={[{ required: true }]}><Input.TextArea rows={2} /></Form.Item>
          <Space wrap>
            <Form.Item name="executorEmployeeId" label="Виконавець" rules={[{ required: true }]}>
              <Select showSearch optionFilterProp="label" style={{ width: 260 }} options={opt(lookups.employees)} />
            </Form.Item>
            <Form.Item name="statusId" label="Статус" rules={[{ required: true }]}>
              <Select style={{ width: 160 }} options={opt(lookups.taskStatuses)} />
            </Form.Item>
          </Space>
          <Space wrap>
            <Form.Item name="dueDate" label="Строк виконання" rules={[{ required: true }]}>
              <DatePicker format="DD.MM.YYYY" /></Form.Item>
            <Form.Item name="completedAt" label="Дата виконання"><DatePicker format="DD.MM.YYYY" /></Form.Item>
          </Space>
          <Form.Item name="resultText" label="Результат виконання"><Input.TextArea rows={2} /></Form.Item>
        </Form>
      </Modal>
    </>
  );
}
