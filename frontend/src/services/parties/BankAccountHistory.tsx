import { useCallback } from 'react';
import { Empty, Modal, Spin, Tag, Timeline, Typography } from 'antd';
import dayjs from 'dayjs';
import { api } from '../../api/client';
import { useApiData } from '../../hooks/useApi';
import { formatIban } from '../../data/partyCodes';
import { fromUtc } from './types';

/** Запис журналу (contracts/Parties/Dtos/BankAccountHistoryItem): значення — JSON рахунку до й після. */
interface HistoryItem {
  id: number; accountId: number; action: number; changedAt: string; changedBy?: string;
  oldValues?: string; newValues?: string;
}
type Values = Record<string, string | boolean | null>;

const ACTIONS: Record<number, { label: string; color: string }> = {
  1: { label: 'Додано рахунок', color: 'green' },
  2: { label: 'Змінено рахунок', color: 'orange' },
  3: { label: 'Видалено рахунок', color: 'red' },
};

// Поля JSON (імена колонок dbo.PartyBankAccount) у порядку показу
const FIELDS: [string, string][] = [
  ['Iban', 'IBAN'], ['BankName', 'Банк'], ['SwiftBic', 'SWIFT/BIC'], ['Currency', 'Валюта'],
  ['Purpose', 'Призначення'], ['AccountHolderName', 'Власник рахунку'], ['IsPrimary', 'Основний'],
  ['ClosedAt', 'Дата закриття'], ['Note', 'Примітка'],
];

function show(field: string, v: Values[string] | undefined): string {
  if (v === null || v === undefined || v === '') return '—';
  if (field === 'Iban') return formatIban(String(v));
  if (field === 'IsPrimary') return v ? 'так' : 'ні';
  if (field === 'ClosedAt') return dayjs(String(v)).format('DD.MM.YYYY');
  return String(v);
}

const parse = (json?: string): Values | null => (json ? JSON.parse(json) as Values : null);

/** Що змінилося: для зміни — лише різні поля (старе → нове), для додавання / видалення — усі заповнені. */
function Changes({ item }: { item: HistoryItem }) {
  const before = parse(item.oldValues);
  const after = parse(item.newValues);
  const rows = FIELDS.filter(([f]) => (before && after
    ? show(f, before[f]) !== show(f, after[f])
    : show(f, (after ?? before)?.[f]) !== '—'));

  return (
    <div style={{ marginTop: 4 }}>
      {rows.map(([f, label]) => (
        <div key={f} style={{ fontSize: 13 }}>
          <Typography.Text type="secondary">{label}: </Typography.Text>
          {before && after ? (
            <>
              <Typography.Text delete type="secondary" style={{ fontFamily: f === 'Iban' ? 'monospace' : undefined }}>
                {show(f, before[f])}
              </Typography.Text>
              {' → '}
              <Typography.Text strong style={{ fontFamily: f === 'Iban' ? 'monospace' : undefined }}>{show(f, after[f])}</Typography.Text>
            </>
          ) : (
            <span style={{ fontFamily: f === 'Iban' ? 'monospace' : undefined }}>{show(f, (after ?? before)?.[f])}</span>
          )}
        </div>
      ))}
    </div>
  );
}

/** Журнал змін банківських рахунків контрагента: хто, коли і що змінив (нові записи першими). */
export default function BankAccountHistory({ partyId, onClose }: { partyId: number; onClose: () => void }) {
  const load = useCallback(() => api<HistoryItem[]>(`/api/parties/${partyId}/bank-accounts/history`), [partyId]);
  const { data, loading } = useApiData(load, []);

  return (
    <Modal open title="Журнал змін банківських рахунків" onCancel={onClose} footer={null} width={640}>
      <Spin spinning={loading}>
        {data.length === 0 && !loading
          ? <Empty description="Змін ще не було" image={Empty.PRESENTED_IMAGE_SIMPLE} />
          : (
            <Timeline style={{ marginTop: 16 }} items={data.map((h) => ({
              color: ACTIONS[h.action]?.color,
              content: (
                <>
                  <Tag color={ACTIONS[h.action]?.color}>{ACTIONS[h.action]?.label}</Tag>
                  <Typography.Text type="secondary">
                    {fromUtc(h.changedAt).format('DD.MM.YYYY HH:mm')} · {h.changedBy ?? 'система (поза застосунком)'}
                  </Typography.Text>
                  <Changes item={h} />
                </>
              ),
            }))} />
          )}
      </Spin>
    </Modal>
  );
}
