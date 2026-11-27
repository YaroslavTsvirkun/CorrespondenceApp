import { useMemo, useState } from 'react';
import {
  Alert, AutoComplete, Button, Checkbox, DatePicker, Form, Input, Modal, Popconfirm, Select, Space, Table, Tag, Tooltip, Typography,
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, StarFilled, HistoryOutlined, WarningOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import dayjs from 'dayjs';
import { api } from '../../api/client';
import { useAuth } from '../../auth/AuthContext';
import { P } from '../../auth/permissions';
import { useApiAction, useApiData } from '../../hooks/useApi';
import { formatIban, isValidIban, mfoFromIban, normalizeIban } from '../../data/partyCodes';
import BankAccountHistory from './BankAccountHistory';
import { fromUtc, type Bank, type BankAccount } from './types';

/** Скільки днів після зміни реквізити вважаються «свіжими» — перед оплатою їх варто звірити з контрагентом. */
const RECENT_DAYS = 30;

const CURRENCIES = ['UAH', 'USD', 'EUR', 'GBP', 'PLN', 'CHF'].map((c) => ({ value: c, label: c }));
const PURPOSES = ['Поточний', 'Бюджетний', 'Депозитний', 'Зарплатний', 'Поточний валютний', 'Поточний ФОП']
  .map((value) => ({ value }));

const loadBanks = () => api<Bank[]>('/api/banks');

interface Props {
  partyId: number;
  accounts: BankAccount[];
  /** Перечитати картку контрагента після зміни */
  onChanged: () => void;
}

/** Банківські рахунки контрагента. Змінювати їх може лише користувач із parties.bank.manage. */
export default function BankAccountsTab({ partyId, accounts, onChanged }: Props) {
  const { can } = useAuth();
  const editable = can(P.partiesBank);
  const run = useApiAction();
  const { data: banks } = useApiData(loadBanks, []);
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<BankAccount | null>(null);
  const [historyOpen, setHistoryOpen] = useState(false);
  const [form] = Form.useForm();

  const iban = Form.useWatch('iban', form) as string | undefined;
  const mfo = iban ? mfoFromIban(iban) : null;
  const bank = useMemo(() => banks.find((b) => b.mfo === mfo), [banks, mfo]);
  // Назву банку й SWIFT вводять вручну лише для іноземного рахунку або банку, якого немає в довіднику
  const manualBank = !!iban && isValidIban(iban) && !bank;

  const openAdd = () => {
    setEditing(null); form.resetFields();
    form.setFieldsValue({ currency: 'UAH', isPrimary: accounts.every((a) => a.closedAt || a.currency !== 'UAH' || !a.isPrimary) });
    setOpen(true);
  };
  const openEdit = (a: BankAccount) => {
    setEditing(a);
    form.setFieldsValue({ ...a, iban: formatIban(a.iban), closedAt: a.closedAt ? dayjs(a.closedAt) : undefined });
    setOpen(true);
  };

  const submit = async () => {
    const v = await form.validateFields();
    const body = JSON.stringify({
      ...v,
      iban: normalizeIban(v.iban),
      bankName: manualBank ? v.bankName : null,
      swiftBic: manualBank ? v.swiftBic : null,
      closedAt: v.closedAt ? (v.closedAt as dayjs.Dayjs).format('YYYY-MM-DD') : null,
    });
    const url = `/api/parties/${partyId}/bank-accounts`;
    const ok = await run(() => (editing
      ? api(`${url}/${editing.id}`, { method: 'PUT', body })
      : api(url, { method: 'POST', body })), 'Рахунок збережено');
    if (ok) { setOpen(false); onChanged(); }
  };

  const remove = async (a: BankAccount) => {
    if (await run(() => api(`/api/parties/${partyId}/bank-accounts/${a.id}`, { method: 'DELETE' }), 'Рахунок видалено')) onChanged();
  };

  const columns: ColumnsType<BankAccount> = [
    { title: 'IBAN', dataIndex: 'iban', render: (v, a) => (
      <Space>
        {a.isPrimary && !a.closedAt && <StarFilled style={{ color: '#faad14' }} title="Основний рахунок" />}
        <span style={{ fontFamily: 'monospace', whiteSpace: 'nowrap', textDecoration: a.closedAt ? 'line-through' : undefined }}>{formatIban(v)}</span>
      </Space>) },
    { title: 'Банк', dataIndex: 'bankName', render: (v, a) => (
      <>{v ?? <Typography.Text type="warning">Банк не знайдено в довіднику</Typography.Text>}
        {a.mfo && <div style={{ fontSize: 12, color: '#8c8c8c' }}>МФО {a.mfo}{a.swiftBic ? ` · ${a.swiftBic}` : ''}</div>}
        {!a.mfo && a.swiftBic && <div style={{ fontSize: 12, color: '#8c8c8c' }}>{a.swiftBic}</div>}</>) },
    { title: 'Валюта', dataIndex: 'currency', width: 90, render: (v) => <Tag>{v}</Tag> },
    { title: 'Призначення', dataIndex: 'purpose', width: 160, render: (v) => v || '—' },
    { title: 'Статус', dataIndex: 'closedAt', width: 170,
      render: (v, a) => (
        <Space direction="vertical" size={2}>
          {v ? <Tag>Закрито {dayjs(v).format('DD.MM.YYYY')}</Tag> : <Tag color="green">Відкритий</Tag>}
          {a.lastChangedAt && !v && dayjs().diff(fromUtc(a.lastChangedAt), 'day') < RECENT_DAYS && (
            <Tooltip title={`Рахунок додано або змінено ${fromUtc(a.lastChangedAt).format('DD.MM.YYYY HH:mm')}`
              + `${a.lastChangedBy ? ` (${a.lastChangedBy})` : ''}. Перед оплатою звірте рахунок з контрагентом.`}>
              <Tag color="warning" icon={<WarningOutlined />}>Змінено нещодавно</Tag>
            </Tooltip>
          )}
        </Space>
      ) },
    ...(editable ? [{
      title: '', key: 'a', width: 90,
      render: (_: unknown, r: BankAccount) => (
        <Space>
          <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(r)} />
          <Popconfirm title="Видалити рахунок?" description="Закритий рахунок краще позначити датою закриття — він лишиться в історії."
            okText="Видалити" cancelText="Ні" onConfirm={() => remove(r)}>
            <Button size="small" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        </Space>
      ),
    }] : []),
  ];

  return (
    <>
      {!editable && (
        <Alert type="info" showIcon style={{ marginBottom: 12 }}
          message="Банківські реквізити змінює лише користувач із правом «Банківські рахунки контрагентів»." />
      )}
      <div className="toolbar"><span className="spacer" />
        <Button icon={<HistoryOutlined />} onClick={() => setHistoryOpen(true)}>Журнал змін</Button>
        {editable && <Button type="primary" icon={<PlusOutlined />} onClick={openAdd}>Додати рахунок</Button>}
      </div>
      <Table rowKey="id" columns={columns} dataSource={accounts} size="small" pagination={false}
        locale={{ emptyText: 'Рахунків немає' }} />

      {historyOpen && <BankAccountHistory partyId={partyId} onClose={() => setHistoryOpen(false)} />}

      <Modal open={open} title={editing ? 'Редагування рахунку' : 'Новий рахунок'} width={560}
        onCancel={() => setOpen(false)} onOk={submit} okText="Зберегти" cancelText="Скасувати" destroyOnHidden>
        <Form form={form} layout="vertical" style={{ marginTop: 12 }}>
          <Form.Item name="iban" label="IBAN" rules={[{
            validator: (_, v) => (v && isValidIban(v) ? Promise.resolve()
              : Promise.reject(new Error('Невірний IBAN (формат або контрольна сума)'))),
          }]} extra={bank ? `${bank.shortName ?? bank.name} · МФО ${bank.mfo}`
            : mfo ? `Банку з МФО ${mfo} немає в довіднику — вкажіть назву вручну або додайте банк у розділі «Банки»` : undefined}>
            <Input placeholder="UA00 0000 0000 0000 0000 0000 0000 0" style={{ fontFamily: 'monospace' }}
              onBlur={(e) => e.target.value && form.setFieldValue('iban', formatIban(e.target.value))} />
          </Form.Item>
          {manualBank && (
            <Space wrap>
              <Form.Item name="bankName" label="Назва банку"><Input style={{ width: 300 }} /></Form.Item>
              <Form.Item name="swiftBic" label="SWIFT/BIC"><Input maxLength={11} style={{ width: 160, fontFamily: 'monospace' }} /></Form.Item>
            </Space>
          )}
          <Space wrap>
            <Form.Item name="currency" label="Валюта" rules={[{ required: true }]}>
              <Select options={CURRENCIES} style={{ width: 120 }} />
            </Form.Item>
            <Form.Item name="purpose" label="Призначення">
              <AutoComplete options={PURPOSES} style={{ width: 240 }} filterOption />
            </Form.Item>
          </Space>
          <Form.Item name="accountHolderName" label="Власник рахунку" tooltip="Якщо відрізняється від назви контрагента">
            <Input />
          </Form.Item>
          <Space wrap align="start">
            <Form.Item name="isPrimary" valuePropName="checked"><Checkbox>Основний рахунок у цій валюті</Checkbox></Form.Item>
            <Form.Item name="closedAt" label="Дата закриття"><DatePicker format="DD.MM.YYYY" /></Form.Item>
          </Space>
          <Form.Item name="note" label="Примітка"><Input /></Form.Item>
        </Form>
      </Modal>
    </>
  );
}
