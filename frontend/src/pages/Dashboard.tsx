import { useCallback } from 'react';
import { Card, Col, Row, Statistic, Table, Tag, Typography } from 'antd';
import { FileTextOutlined, FlagOutlined, SyncOutlined, WarningOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import { api } from '../api/client';
import { useLookups } from '../data/lookups';
import { statusColor } from '../data/statusColors';
import { useApiData } from '../hooks/useApi';

interface ListItem {
  id: number; registrationNumber: string; registrationYear: number; direction: string;
  title: string; correspondent?: string; documentDate: string; status: string;
}
interface Paged { items: ListItem[]; total: number; }
interface TaskRow { dueDate: string; statusId: number; }

interface Summary { total: number; controlled: number; onExec: number; overdue: number; recent: ListItem[]; }
const EMPTY: Summary = { total: 0, controlled: 0, onExec: 0, overdue: 0, recent: [] };

/** Кількість документів за фільтром (0, якщо немає доступу). */
const count = (qs: string) => api<Paged>(`/api/documents?${qs}`).then((r) => r.total).catch(() => 0);

export default function Dashboard() {
  const { lookups, ready } = useLookups();
  const load = useCallback(async (): Promise<Summary> => {
    if (!ready) return EMPTY; // статуси потрібні для підрахунку — чекаємо довідники
    const execId = lookups.documentStatuses.find((s) => s.name === 'На виконанні')?.id;
    const finalIds = new Set(lookups.taskStatuses.filter((s) => s.isFinal).map((s) => s.id));
    const today = dayjs();
    const [total, controlled, onExec, recent, overdue] = await Promise.all([
      count('pageSize=1'),
      count('pageSize=1&isControlled=true'),
      execId ? count(`pageSize=1&statusId=${execId}`) : Promise.resolve(0),
      api<Paged>('/api/documents?pageSize=5&sortBy=date&desc=true').then((r) => r.items),
      api<TaskRow[]>('/api/tasks')
        .then((tasks) => tasks.filter((t) => !finalIds.has(t.statusId) && dayjs(t.dueDate).isBefore(today, 'day')).length)
        .catch(() => 0), // немає доступу до доручень
    ]);
    return { total, controlled, onExec, overdue, recent };
  }, [lookups, ready]);
  const { data: { total, controlled, onExec, overdue, recent }, loading: dataLoading } = useApiData(load, EMPTY);
  // Поки немає довідників або даних — індикатор завантаження, а не нулі, схожі на справжні значення
  const loading = !ready || dataLoading;

  return (
    <>
      <Typography.Title level={3}>Огляд</Typography.Title>
      <Row gutter={16} className="stat-row">
        <Col xs={12} md={6}><Card><Statistic loading={loading} title="Усього документів" value={total} prefix={<FileTextOutlined />} /></Card></Col>
        <Col xs={12} md={6}><Card><Statistic loading={loading} title="На контролі" value={controlled} prefix={<FlagOutlined />} styles={{ content: { color: '#1d6fb8' } }} /></Card></Col>
        <Col xs={12} md={6}><Card><Statistic loading={loading} title="На виконанні" value={onExec} prefix={<SyncOutlined />} styles={{ content: { color: '#d48806' } }} /></Card></Col>
        <Col xs={12} md={6}><Card><Statistic loading={loading} title="Прострочених доручень" value={overdue} prefix={<WarningOutlined />} styles={{ content: { color: '#cf1322' } }} /></Card></Col>
      </Row>

      <Card title="Нещодавно зареєстровані документи">
        <Table rowKey="id" pagination={false} size="middle" dataSource={recent} loading={loading}
          columns={[
            { title: '№', dataIndex: 'registrationNumber', width: 80, render: (v, r) => `${v}/${r.registrationYear}` },
            { title: 'Напрям', dataIndex: 'direction', width: 110 },
            { title: 'Тема', dataIndex: 'title', ellipsis: true },
            { title: 'Кореспондент', dataIndex: 'correspondent', render: (v) => v || '—' },
            { title: 'Дата', dataIndex: 'documentDate', width: 110 },
            { title: 'Статус', dataIndex: 'status', width: 140, render: (v) => <Tag color={statusColor(v)}>{v}</Tag> },
          ]} />
      </Card>
    </>
  );
}
