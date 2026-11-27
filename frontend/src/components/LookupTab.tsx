import { useState } from 'react';
import { Button, Form, Input, Modal, Popconfirm, Space, Table, Tag } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { useLookups } from '../data/lookups';
import { useAuth } from '../auth/AuthContext';
import { useCrud } from '../hooks/useCrud';

interface Item { id: number; name: string; description?: string; code?: string; }

interface Props {
  /** Вид довідника в /api/dictionaries/{kind} */
  kind: string;
  /** Право на зміну записів */
  permission: string;
  /** Колонка «Код» і її підказки (напр. код типу документа або скорочення правової форми) */
  code?: { label: string; tooltip?: string; placeholder?: string };
}

/** Простий довідник {назва, код?, опис}: таблиця й форма запису. */
export default function LookupTab({ kind, permission, code }: Props) {
  const { can } = useAuth();
  const { reload } = useLookups();
  const editable = can(permission);
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<Item | null>(null);
  const [form] = Form.useForm();

  // Після змін оновлюються й спільні lookups (списки у формах інших сторінок)
  const { rows, loading, save, remove } = useCrud<Item>(`/api/dictionaries/${kind}`, reload);

  const openAdd = () => { setEditing(null); form.resetFields(); setOpen(true); };
  const openEdit = (x: Item) => { setEditing(x); form.resetFields(); form.setFieldsValue(x); setOpen(true); };
  const submit = async () => {
    const v = await form.validateFields();
    if (await save(editing?.id, v)) setOpen(false);
  };

  const columns: ColumnsType<Item> = [
    { title: 'Назва', dataIndex: 'name' },
    ...(code ? [{ title: code.label, dataIndex: 'code', width: 120,
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
        onOk={submit} okText="Зберегти" cancelText="Скасувати" forceRender>
        <Form form={form} layout="vertical" style={{ marginTop: 12 }}>
          <Form.Item name="name" label="Назва" rules={[{ required: true }]}><Input /></Form.Item>
          {code && (
            <Form.Item name="code" label={code.label} tooltip={code.tooltip}>
              <Input maxLength={20} placeholder={code.placeholder} style={{ fontFamily: 'monospace' }} />
            </Form.Item>
          )}
          <Form.Item name="description" label="Опис"><Input.TextArea rows={2} /></Form.Item>
        </Form>
      </Modal>
    </>
  );
}
