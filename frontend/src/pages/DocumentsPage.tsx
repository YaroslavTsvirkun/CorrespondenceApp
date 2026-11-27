import { useCallback, useState } from 'react';
import {
  Button, Card, DatePicker, Descriptions, Drawer, Form, Input, InputNumber,
  Modal, Popconfirm, Select, Space, Switch, Table, Tag, Tooltip, Typography,
} from 'antd';
import {
  PlusOutlined, EditOutlined, DeleteOutlined, EyeOutlined, SearchOutlined, ClearOutlined,
} from '@ant-design/icons';
import type { ColumnsType, TablePaginationConfig } from 'antd/es/table';
import type { SorterResult } from 'antd/es/table/interface';
import dayjs from 'dayjs';
import { api } from '../api/client';
import { useLookups } from '../data/lookups';
import { opt } from '../data/options';
import { useAuth } from '../auth/AuthContext';
import { P } from '../auth/permissions';
import AttachmentsPanel from '../components/AttachmentsPanel';
import { statusColor } from '../data/statusColors';
import { useApiAction, useApiData } from '../hooks/useApi';
import QuickPartyModal from '../services/parties/QuickPartyModal';

interface ListItem {
  id: number; registrationNumber: string; registrationYear: number; direction: string;
  documentType: string; title: string; correspondent?: string; documentDate: string;
  status: string; isControlled: boolean;
}
/** Картка документа (DocumentDetail у contracts/Documents/Dtos). */
interface DocDetail {
  id: number; registrationNumber: string; registrationYear: number; registeredAt: string; registrar: string;
  direction: string; documentType: string; status: string; isControlled: boolean;
  channel?: string; correspondent?: string; senderDepartment?: string; receiverDepartment?: string;
  author?: string; title: string; summary?: string; documentDate: string; incomingNumber?: string; note?: string;
  /** Поточна назва контрагента, якщо його перейменували після реєстрації (correspondent — назва на той момент) */
  correspondentCurrentName?: string;
  addressees: { partyId: number; role: number; name: string; currentName?: string }[];
  tasks: { id: number; executor: string; taskText: string; dueDate: string; status: string; completedAt?: string }[];
}
interface Paged { items: ListItem[]; total: number; page: number; pageSize: number; }
interface DocInput {
  directionId: number; documentTypeId: number; currentStatusId: number;
  deliveryChannelId?: number; correspondentId?: number; senderDepartmentId?: number;
  receiverDepartmentId?: number; authorEmployeeId?: number; title: string; summary?: string;
  documentDate: string; incomingNumber?: string; isControlled: boolean; note?: string;
  registrationNumber: string; registrationYear: number; registrarEmployeeId: number;
  /** Додаткові адресати (role 1) і копії (role 2); головний кореспондент — correspondentId */
  addressees?: { partyId: number; role: number }[];
}

const { RangePicker } = DatePicker;
const PAGE_SIZE = 8;

export default function DocumentsPage() {
  const run = useApiAction();
  const { lookups, reload: reloadLookups } = useLookups();
  const { can } = useAuth();
  const [quickParty, setQuickParty] = useState(false);

  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [fDirection, setFDirection] = useState<number>();
  const [fType, setFType] = useState<number>();
  const [fStatus, setFStatus] = useState<number>();
  const [fControlled, setFControlled] = useState<boolean>();
  const [fRange, setFRange] = useState<[dayjs.Dayjs, dayjs.Dayjs] | null>(null);
  const [sortBy, setSortBy] = useState<string>();
  const [desc, setDesc] = useState(true);

  const [open, setOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [viewing, setViewing] = useState<DocDetail | null>(null);
  const [form] = Form.useForm();

  const load = useCallback(() => {
    const p = new URLSearchParams({ page: String(page), pageSize: String(PAGE_SIZE), desc: String(desc) });
    if (search.trim()) p.set('search', search.trim());
    if (fDirection) p.set('directionId', String(fDirection));
    if (fType) p.set('documentTypeId', String(fType));
    if (fStatus) p.set('statusId', String(fStatus));
    if (fControlled !== undefined) p.set('isControlled', String(fControlled));
    if (sortBy) p.set('sortBy', sortBy);
    if (fRange) { p.set('dateFrom', fRange[0].format('YYYY-MM-DD')); p.set('dateTo', fRange[1].format('YYYY-MM-DD')); }
    return api<Paged>(`/api/documents?${p.toString()}`);
  }, [page, search, fDirection, fType, fStatus, fControlled, fRange, sortBy, desc]);
  const { data, loading, reload } = useApiData(load, { items: [], total: 0, page: 1, pageSize: PAGE_SIZE } as Paged);

  const filtersActive = !!(search || fDirection || fType || fStatus || fControlled !== undefined || fRange);
  const clearFilters = () => {
    setSearch(''); setFDirection(undefined); setFType(undefined); setFStatus(undefined);
    setFControlled(undefined); setFRange(null); setPage(1);
  };

  const openAdd = () => {
    setEditingId(null); form.resetFields();
    form.setFieldsValue({ registrationYear: dayjs().year(), documentDate: dayjs(), isControlled: false });
    setOpen(true);
  };
  const openEdit = (row: ListItem) => run(async () => {
    const input = await api<DocInput>(`/api/documents/${row.id}/edit`);
    setEditingId(row.id);
    const ids = (role: number) => (input.addressees ?? []).filter((a) => a.role === role).map((a) => a.partyId);
    form.setFieldsValue({ ...input, documentDate: dayjs(input.documentDate), addresseeIds: ids(1), copyIds: ids(2) });
    setOpen(true);
  });
  const submit = async () => {
    const { addresseeIds = [], copyIds = [], ...v } = await form.validateFields();
    const body = JSON.stringify({
      ...v,
      documentDate: (v.documentDate as dayjs.Dayjs).format('YYYY-MM-DD'),
      addressees: [
        ...(addresseeIds as number[]).map((partyId) => ({ partyId, role: 1 })),
        ...(copyIds as number[]).map((partyId) => ({ partyId, role: 2 })),
      ],
    });
    const saved = await run(
      () => editingId
        ? api(`/api/documents/${editingId}`, { method: 'PUT', body })
        : api('/api/documents', { method: 'POST', body }),
      editingId ? 'Документ оновлено' : 'Документ додано');
    if (saved) { setOpen(false); reload(); }
  };
  const remove = async (id: number) => {
    if (await run(() => api(`/api/documents/${id}`, { method: 'DELETE' }), 'Документ видалено')) reload();
  };

  // Підбір наступного реєстраційного номера за патерном організації
  const fetchNumber = async (dir?: number, type?: number, year?: number) => {
    if (!dir || !type) return;
    const y = year || dayjs().year();
    try {
      const r = await api<{ number: string }>(
        `/api/documents/next-number?directionId=${dir}&documentTypeId=${type}&year=${y}`);
      if (r.number) form.setFieldValue('registrationNumber', r.number);
    } catch { /* супер-адмін без організації — номер згенерується на сервері */ }
  };
  const genNumber = () => fetchNumber(
    form.getFieldValue('directionId'), form.getFieldValue('documentTypeId'),
    form.getFieldValue('registrationYear'));
  const openView = (row: ListItem) =>
    run(async () => setViewing(await api<DocDetail>(`/api/documents/${row.id}`)));

  const onTableChange = (
    pg: TablePaginationConfig, _f: unknown,
    sorter: SorterResult<ListItem> | SorterResult<ListItem>[],
  ) => {
    const s = Array.isArray(sorter) ? sorter[0] : sorter;
    const map: Record<string, string> = { documentDate: 'date', registrationNumber: 'number', title: 'title' };
    if (s?.order) { setSortBy(map[s.field as string]); setDesc(s.order === 'descend'); }
    else setSortBy(undefined);
    setPage(pg.current ?? 1);
  };

  const columns: ColumnsType<ListItem> = [
    { title: '№ реєстр.', dataIndex: 'registrationNumber', width: 130, sorter: true,
      render: (v) => <b>{v}</b> },
    { title: 'Напрям', dataIndex: 'direction', width: 110 },
    { title: 'Тип', dataIndex: 'documentType', width: 150 },
    { title: 'Тема', dataIndex: 'title', ellipsis: true, sorter: true },
    { title: 'Кореспондент', dataIndex: 'correspondent', width: 190, ellipsis: true, render: (v) => v || '—' },
    { title: 'Дата', dataIndex: 'documentDate', width: 120, sorter: true, defaultSortOrder: 'descend' },
    { title: 'Статус', dataIndex: 'status', width: 140, render: (v) => <Tag color={statusColor(v)}>{v}</Tag> },
    { title: 'Контроль', dataIndex: 'isControlled', width: 100, align: 'center',
      render: (v: boolean) => (v ? <Tag color="red">Так</Tag> : <span style={{ color: '#aaa' }}>—</span>) },
    {
      title: 'Дії', key: 'a', width: 140, fixed: 'right',
      render: (_, r) => (
        <Space>
          <Button size="small" icon={<EyeOutlined />} onClick={() => openView(r)} title="Картка" />
          {can(P.docsUpdate) && <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(r)} />}
          {can(P.docsDelete) && (
            <Popconfirm title="Видалити документ?" okText="Так" cancelText="Ні" onConfirm={() => remove(r.id)}>
              <Button size="small" danger icon={<DeleteOutlined />} />
            </Popconfirm>
          )}
        </Space>
      ),
    },
  ];

  return (
    <>
      <Typography.Title level={3}>Документи</Typography.Title>
      <Card>
        <div className="toolbar">
          <Input allowClear prefix={<SearchOutlined />} placeholder="Пошук за №, темою, кореспондентом" style={{ width: 270 }}
            value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }} />
          <Select allowClear placeholder="Напрям" style={{ width: 140 }} options={opt(lookups.directions)}
            value={fDirection} onChange={(v) => { setFDirection(v); setPage(1); }} />
          <Select allowClear placeholder="Тип" style={{ width: 160 }} options={opt(lookups.documentTypes)}
            value={fType} onChange={(v) => { setFType(v); setPage(1); }} />
          <Select allowClear placeholder="Статус" style={{ width: 160 }} options={opt(lookups.documentStatuses)}
            value={fStatus} onChange={(v) => { setFStatus(v); setPage(1); }} />
          <Select allowClear placeholder="Контроль" style={{ width: 130 }}
            options={[{ value: true, label: 'На контролі' }, { value: false, label: 'Без контролю' }]}
            value={fControlled} onChange={(v) => { setFControlled(v); setPage(1); }} />
          <RangePicker placeholder={['Дата від', 'Дата до']} value={fRange}
            onChange={(v) => { setFRange(v as never); setPage(1); }} />
          <Button icon={<ClearOutlined />} disabled={!filtersActive} onClick={clearFilters}>Зняти фільтр</Button>
          <span className="spacer" />
          {can(P.docsCreate) && <Button type="primary" icon={<PlusOutlined />} onClick={openAdd}>Додати документ</Button>}
        </div>

        <Table rowKey="id" columns={columns} dataSource={data.items} size="middle" loading={loading}
          scroll={{ x: 1100 }} onChange={onTableChange}
          pagination={{ current: data.page, pageSize: data.pageSize, total: data.total, showSizeChanger: false,
            showTotal: (t) => `Усього: ${t}` }} />
      </Card>

      <Modal open={open} title={editingId ? 'Редагування документа' : 'Новий документ'}
        onCancel={() => setOpen(false)} onOk={submit} okText="Зберегти" cancelText="Скасувати"
        width={760} destroyOnHidden>
        <Form form={form} layout="vertical" style={{ marginTop: 12 }}
          onValuesChange={(changed, all) => {
            if (editingId) return;
            if (('directionId' in changed || 'documentTypeId' in changed)
              && all.directionId && all.documentTypeId && !all.registrationNumber) {
              fetchNumber(all.directionId, all.documentTypeId, all.registrationYear);
            }
          }}>
          <Space align="start" wrap>
            <Form.Item name="registrationNumber" label="Реєстраційний №"
              tooltip="Залиште порожнім — номер згенерується автоматично за патерном організації">
              <Input style={{ width: 180 }} placeholder="авто"
                addonAfter={<a onClick={genNumber} title="Підібрати номер">авто</a>} />
            </Form.Item>
            <Form.Item name="registrationYear" label="Рік" rules={[{ required: true }]}>
              <InputNumber style={{ width: 100 }} min={1990} max={2100} />
            </Form.Item>
            <Form.Item name="documentDate" label="Дата документа" rules={[{ required: true }]}>
              <DatePicker style={{ width: 160 }} format="DD.MM.YYYY" />
            </Form.Item>
            <Form.Item name="isControlled" label="На контролі" valuePropName="checked"><Switch /></Form.Item>
          </Space>
          <Form.Item name="title" label="Тема документа" rules={[{ required: true, message: 'Вкажіть тему' }]}><Input /></Form.Item>
          <Form.Item name="summary" label="Короткий зміст"><Input.TextArea rows={2} /></Form.Item>
          <Space align="start" wrap>
            <Form.Item name="directionId" label="Напрям" rules={[{ required: true }]}>
              <Select style={{ width: 160 }} options={opt(lookups.directions)} /></Form.Item>
            <Form.Item name="documentTypeId" label="Тип документа" rules={[{ required: true }]}>
              <Select style={{ width: 200 }} options={opt(lookups.documentTypes)} /></Form.Item>
            <Form.Item name="currentStatusId" label="Статус" rules={[{ required: true }]}>
              <Select style={{ width: 180 }} options={opt(lookups.documentStatuses)} /></Form.Item>
            <Form.Item name="deliveryChannelId" label="Канал">
              <Select allowClear style={{ width: 180 }} options={opt(lookups.channels)} /></Form.Item>
          </Space>
          <PartyFields onQuickAdd={can(P.parties) ? () => setQuickParty(true) : undefined} />
          <Space align="start" wrap>
            <Form.Item name="senderDepartmentId" label="Підрозділ-відправник">
              <Select allowClear style={{ width: 200 }} options={opt(lookups.departments)} /></Form.Item>
            <Form.Item name="receiverDepartmentId" label="Підрозділ-одержувач">
              <Select allowClear style={{ width: 200 }} options={opt(lookups.departments)} /></Form.Item>
          </Space>
          <Space align="start" wrap>
            <Form.Item name="authorEmployeeId" label="Автор">
              <Select allowClear showSearch optionFilterProp="label" style={{ width: 260 }} options={opt(lookups.employees)} /></Form.Item>
            <Form.Item name="registrarEmployeeId" label="Реєстратор" rules={[{ required: true }]}>
              <Select showSearch optionFilterProp="label" style={{ width: 260 }} options={opt(lookups.employees)} /></Form.Item>
          </Space>
        </Form>
      </Modal>

      {quickParty && (
        <QuickPartyModal onClose={() => setQuickParty(false)}
          onCreated={(id) => { reloadLookups(); form.setFieldValue('correspondentId', id); }} />
      )}

      <Drawer open={!!viewing} size="large" onClose={() => setViewing(null)}
        title={viewing ? `Картка документа № ${viewing.registrationNumber}` : ''}>
        {viewing && <DocCard d={viewing} />}
        {viewing && <AttachmentsPanel documentId={viewing.id} canEdit={can(P.docsUpdate)} />}
      </Drawer>
    </>
  );
}

function DocCard({ d }: { d: DocDetail }) {
  return (
    <>
      <Descriptions bordered column={1} size="small">
        <Descriptions.Item label="Тема">{d.title}</Descriptions.Item>
        <Descriptions.Item label="Напрям">{d.direction}</Descriptions.Item>
        <Descriptions.Item label="Тип">{d.documentType}</Descriptions.Item>
        <Descriptions.Item label="Статус"><Tag color={statusColor(d.status)}>{d.status}</Tag></Descriptions.Item>
        <Descriptions.Item label="На контролі">{d.isControlled ? 'Так' : 'Ні'}</Descriptions.Item>
        <Descriptions.Item label="Дата документа">{dayjs(d.documentDate).format('DD.MM.YYYY')}</Descriptions.Item>
        <Descriptions.Item label="Канал">{d.channel || '—'}</Descriptions.Item>
        <Descriptions.Item label="Кореспондент">
          {d.correspondent || '—'}
          {d.correspondentCurrentName && (
            <Typography.Text type="secondary"> (зараз: {d.correspondentCurrentName})</Typography.Text>
          )}
        </Descriptions.Item>
        {d.addressees.length > 0 && (
          <Descriptions.Item label="Адресати й копії">
            {d.addressees.map((a) => (
              <div key={a.partyId}>
                <Tag color={a.role === 1 ? 'blue' : 'default'}>{a.role === 1 ? 'Адресат' : 'Копія'}</Tag>
                {a.name}
                {a.currentName && <Typography.Text type="secondary"> (зараз: {a.currentName})</Typography.Text>}
              </div>
            ))}
          </Descriptions.Item>
        )}
        <Descriptions.Item label="Підрозділ-відправник">{d.senderDepartment || '—'}</Descriptions.Item>
        <Descriptions.Item label="Підрозділ-одержувач">{d.receiverDepartment || '—'}</Descriptions.Item>
        <Descriptions.Item label="Автор">{d.author || '—'}</Descriptions.Item>
        <Descriptions.Item label="Реєстратор">{d.registrar}</Descriptions.Item>
        <Descriptions.Item label="Короткий зміст">{d.summary || '—'}</Descriptions.Item>
      </Descriptions>
      <Typography.Title level={5} style={{ marginTop: 20 }}>Доручення за документом</Typography.Title>
      <Table rowKey="id" size="small" pagination={false} dataSource={d.tasks}
        locale={{ emptyText: 'Доручень немає' }}
        columns={[
          { title: 'Виконавець', dataIndex: 'executor' },
          { title: 'Завдання', dataIndex: 'taskText', ellipsis: true },
          { title: 'Строк', dataIndex: 'dueDate', width: 110, render: (v) => dayjs(v).format('DD.MM.YYYY') },
          { title: 'Статус', dataIndex: 'status', width: 120 },
        ]} />
    </>
  );
}

/**
 * Кореспондент документа (відправник вхідного або основний адресат вихідного), додаткові адресати й копії.
 * Головного кореспондента й уже вибраних не пропонуємо вдруге.
 */
function PartyFields({ onQuickAdd }: { onQuickAdd?: () => void }) {
  const { lookups } = useLookups();
  const form = Form.useFormInstance();
  const directionId = Form.useWatch('directionId', form) as number | undefined;
  const correspondentId = Form.useWatch('correspondentId', form) as number | undefined;
  const addresseeIds = (Form.useWatch('addresseeIds', form) as number[] | undefined) ?? [];
  const copyIds = (Form.useWatch('copyIds', form) as number[] | undefined) ?? [];
  const code = lookups.directions.find((d) => d.id === directionId)?.code;
  const showAddressees = code === 'OUT' || addresseeIds.length > 0 || copyIds.length > 0;

  const options = (exclude: number[]) =>
    opt(lookups.correspondents).filter((o) => !exclude.includes(o.value));
  const label = code === 'IN' ? 'Кореспондент (відправник)' : code === 'OUT' ? 'Кореспондент (основний адресат)' : 'Кореспондент';

  return (
    <>
      <Form.Item label={label} style={{ marginBottom: 0 }}>
        <Space.Compact style={{ width: 520, marginBottom: 24 }}>
          <Form.Item name="correspondentId" noStyle>
            <Select allowClear showSearch optionFilterProp="label" style={{ width: '100%' }} placeholder="Контрагент з реєстру"
              options={options([...addresseeIds, ...copyIds])} />
          </Form.Item>
          {onQuickAdd && (
            <Tooltip title="Додати нового контрагента до реєстру">
              <Button icon={<PlusOutlined />} onClick={onQuickAdd} />
            </Tooltip>
          )}
        </Space.Compact>
      </Form.Item>
      {showAddressees && (
        <Space align="start" wrap>
          <Form.Item name="addresseeIds" label="Інші адресати">
            <Select mode="multiple" allowClear showSearch optionFilterProp="label" style={{ width: 340 }}
              options={options([correspondentId ?? 0, ...copyIds])} />
          </Form.Item>
          <Form.Item name="copyIds" label="Копії">
            <Select mode="multiple" allowClear showSearch optionFilterProp="label" style={{ width: 340 }}
              options={options([correspondentId ?? 0, ...addresseeIds])} />
          </Form.Item>
        </Space>
      )}
    </>
  );
}
