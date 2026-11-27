import { useCallback, useState } from 'react';
import {
  Button, Card, Input, Popconfirm, Segmented, Select, Space, Table, Tag, Tooltip, Typography,
} from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, SearchOutlined, EyeOutlined, BankOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { useAuth } from '../../auth/AuthContext';
import { P } from '../../auth/permissions';
import { useLookups } from '../../data/lookups';
import { useCrud } from '../../hooks/useCrud';
import { PARTY_KINDS } from '../../data/partyCodes';
import PartyDrawer from './PartyDrawer';
import { KIND_COLORS, type PartyListItem } from './types';

const kindName = (k: number) => PARTY_KINDS.find((x) => x.id === k)?.name ?? '—';

/** Реєстр контрагентів: пошук за назвою або кодом, фільтр за видом і статусом, картка в бічній панелі. */
export default function PartiesPage() {
  const { can } = useAuth();
  const editable = can(P.parties);
  const { reload: reloadLookups } = useLookups();
  const [search, setSearch] = useState('');
  const [kind, setKind] = useState<number | undefined>();
  const [status, setStatus] = useState<'active' | 'all' | 'inactive'>('active');
  const [openId, setOpenId] = useState<number | 'new' | null>(null);

  // Список кореспондентів у формі документа (lookups) теж оновлюється після змін
  const { rows, loading, reload, remove } = useCrud<PartyListItem>('/api/parties?includeInactive=true', reloadLookups);

  const q = search.trim().toLowerCase();
  const data = rows.filter((p) =>
    (!q || p.fullName.toLowerCase().includes(q) || (p.shortName ?? '').toLowerCase().includes(q) || (p.code ?? '').includes(q))
    && (kind == null || p.kind === kind)
    && (status === 'all' || p.isActive === (status === 'active')));

  const close = useCallback(() => setOpenId(null), []);
  // Новий контрагент після збереження лишається відкритим (картка сама перечитує себе) — щоб додати рахунки
  const saved = useCallback(() => { reload(); reloadLookups(); }, [reload, reloadLookups]);

  const columns: ColumnsType<PartyListItem> = [
    {
      title: 'Контрагент', dataIndex: 'fullName', width: 280, sorter: (a, b) => (a.shortName ?? a.fullName).localeCompare(b.shortName ?? b.fullName),
      render: (_, p) => (
        <>
          <Typography.Link onClick={() => setOpenId(p.id)} style={{ fontWeight: 500 }}>{p.shortName ?? p.fullName}</Typography.Link>
          {!p.isActive && <Tag style={{ marginInlineStart: 8 }}>Неактивний</Tag>}
          {p.shortName && <div style={{ fontSize: 12, color: '#8c8c8c' }}>{p.fullName}</div>}
          {p.parentName && <div style={{ fontSize: 12, color: '#8c8c8c' }}>Філія: {p.parentName}</div>}
        </>
      ),
    },
    { title: 'Вид', dataIndex: 'kind', width: 170, render: (k: number) => <Tag color={KIND_COLORS[k]}>{kindName(k)}</Tag> },
    { title: 'Код', dataIndex: 'code', width: 120, render: (v) => (v ? <span style={{ fontFamily: 'monospace' }}>{v}</span> : '—') },
    { title: 'Адреса', dataIndex: 'address', ellipsis: true, render: (v) => v || '—' },
    { title: 'Контакти', key: 'contacts', width: 200, render: (_, p) => (
      <>{p.phone && <div>{p.phone}</div>}{p.email && <div style={{ fontSize: 12 }}>{p.email}</div>}{!p.phone && !p.email && '—'}</>) },
    { title: 'Категорії', dataIndex: 'categories', width: 160,
      render: (v?: string) => (v ? v.split(', ').map((c) => <Tag key={c}>{c}</Tag>) : '—') },
    { title: <Tooltip title="Документів / відкритих рахунків">Док. / рах.</Tooltip>, key: 'counts', width: 100, align: 'center',
      render: (_, p) => (
        <Space size={4}>
          <span>{p.documentCount}</span>/
          <span>{p.bankAccountCount > 0 ? <><BankOutlined /> {p.bankAccountCount}</> : 0}</span>
        </Space>) },
    {
      title: 'Дії', key: 'a', width: 110, fixed: 'right',
      render: (_, r) => (
        <Space>
          <Button size="small" icon={editable ? <EditOutlined /> : <EyeOutlined />} onClick={() => setOpenId(r.id)} />
          {editable && (
            <Popconfirm title="Видалити контрагента?"
              description={r.documentCount > 0 ? 'Контрагент є в документах — його можна лише зробити неактивним.' : undefined}
              okText="Так" cancelText="Ні" onConfirm={() => remove(r.id)}>
              <Button size="small" danger icon={<DeleteOutlined />} />
            </Popconfirm>
          )}
        </Space>
      ),
    },
  ];

  return (
    <>
      <Typography.Title level={3}>Контрагенти</Typography.Title>
      <Card>
        <div className="toolbar">
          <Input allowClear prefix={<SearchOutlined />} placeholder="Назва, ПІБ або код ЄДРПОУ / РНОКПП" style={{ width: 300 }}
            value={search} onChange={(e) => setSearch(e.target.value)} />
          <Select allowClear placeholder="Усі види" style={{ width: 230 }} value={kind} onChange={setKind}
            options={PARTY_KINDS.map((k) => ({ value: k.id, label: k.name }))} />
          <Segmented value={status} onChange={(v) => setStatus(v as typeof status)}
            options={[{ value: 'active', label: 'Діючі' }, { value: 'inactive', label: 'Неактивні' }, { value: 'all', label: 'Усі' }]} />
          <span className="spacer" />
          {editable && <Button type="primary" icon={<PlusOutlined />} onClick={() => setOpenId('new')}>Додати</Button>}
        </div>
        <Table rowKey="id" columns={columns} dataSource={data} size="middle" loading={loading}
          scroll={{ x: 1100 }} pagination={{ pageSize: 15 }} />
      </Card>

      <PartyDrawer partyId={openId} parties={rows} onClose={close} onSaved={saved} />
    </>
  );
}
