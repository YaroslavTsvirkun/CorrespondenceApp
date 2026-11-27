import { useState } from 'react';
import { Form, Input, Modal, Select, Typography } from 'antd';
import { api } from '../../api/client';
import { useApiAction } from '../../hooks/useApi';
import { PARTY_KINDS, partyCodeError } from '../../data/partyCodes';

interface Props {
  onClose: () => void;
  /** Контрагента створено — його Id (напр. щоб одразу вибрати в документі) */
  onCreated: (id: number) => void;
}

/**
 * Швидке додавання контрагента з іншої форми (напр. реєстрації документа): лише основні реквізити.
 * Адреси, контакти й рахунки дописують потім у картці в сервісі «Контрагенти».
 * Монтуйте лише на час показу (`{open && <QuickPartyModal … />}`): так вікно з’являється поверх
 * форми, з якої його відкрили, а форма щоразу порожня.
 */
export default function QuickPartyModal({ onClose, onCreated }: Props) {
  const run = useApiAction();
  const [form] = Form.useForm();
  const [saving, setSaving] = useState(false);
  const kind = Form.useWatch('kind', form) as number | undefined;

  const submit = async () => {
    const v = await form.validateFields();
    setSaving(true);
    let id: number | undefined;
    const ok = await run(async () => {
      id = (await api<{ id: number }>('/api/parties', {
        method: 'POST',
        body: JSON.stringify({ ...v, code: v.code?.trim() || null, isActive: true }),
      })).id;
    }, 'Контрагента додано');
    setSaving(false);
    if (ok && id) { onCreated(id); onClose(); }
  };

  return (
    <Modal open title="Новий контрагент" onCancel={onClose} onOk={submit} confirmLoading={saving}
      okText="Додати" cancelText="Скасувати">
      <Form form={form} layout="vertical" initialValues={{ kind: 1 }} style={{ marginTop: 12 }}>
        <Form.Item name="kind" label="Вид контрагента" rules={[{ required: true }]}>
          <Select options={PARTY_KINDS.map((k) => ({ value: k.id, label: k.name }))} />
        </Form.Item>
        <Form.Item name="fullName" label="Повна назва або ПІБ" rules={[{ required: true, message: 'Вкажіть назву' }]}>
          <Input />
        </Form.Item>
        <Form.Item name="shortName" label="Коротка назва" tooltip="Так контрагент показується в документах">
          <Input />
        </Form.Item>
        <Form.Item name="code" label={PARTY_KINDS.find((k) => k.id === kind)?.code ?? 'Код'} dependencies={['kind']}
          rules={[{ validator: (_, v) => { const e = partyCodeError(kind, v?.trim()); return e ? Promise.reject(new Error(e)) : Promise.resolve(); } }]}>
          <Input maxLength={10} style={{ fontFamily: 'monospace', width: 200 }} />
        </Form.Item>
        <Typography.Text type="secondary">
          Адреси, контакти й банківські рахунки можна додати пізніше в сервісі «Контрагенти».
        </Typography.Text>
      </Form>
    </Modal>
  );
}
