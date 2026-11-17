import { useEffect, useState } from 'react';
import { App, Card, Col, Row, Statistic, Table, Tag, Typography } from 'antd';
import { FileTextOutlined, FlagOutlined, SyncOutlined, WarningOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import { api } from '../api/client';
import { useLookups } from '../data/lookups';
import { statusColor } from './DocumentsPage';

interface ListItem {
  id: number; registrationNumber: string; registrationYear: number; direction: string;
  title: string; correspondent?: string; documentDate: string; status: string;
}
interface Paged { items: ListItem[]; total: number; }
interface TaskRow { dueDate: string; statusId: number; }

export default function Dashboard() {
  const { message } = App.useApp();
  const { lookups } = useLookups();
  const [total, setTotal] = useState(0);
  const [controlled, setControlled] = useState(0);
  const [onExec, setOnExec] = useState(0);
  const [overdue, setOverdue] = useState(0);
  const [recent, setRecent] = useState<ListItem[]>([]);

  useEffect(() => {
    if (lookups.documentStatuses.length === 0) return;
    const execId = lookups.documentStatuses.find((s) => s.name === 'На виконанні')?.id;
    const today = dayjs();
    const num = async (qs: string) => { try { return (await api<Paged>(`/api/documents?${qs}`)).total; } catch { return 0; } };
    (async () => {
      try {
        setTotal(await num('pageSize=1'));
        setControlled(await num('pageSize=1&isControlled=true'));
        if (execId) setOnExec(await num(`pageSize=1&statusId=${execId}`));
        const r = await api<Paged>('/api/documents?pageSize=5&sortBy=date&desc=true');
        setRecent(r.items);
        try {
          const tasks = await api<TaskRow[]>('/api/tasks');
          const finalIds = new Set(lookups.taskStatuses.filter((s) => s.isFinal).map((s) => s.id));
          setOverdue(tasks.filter((t) => !finalIds.has(t.statusId) && dayjs(t.dueDate).isBefore(today, 'day')).length);
        } catch { /* немає доступу до доручень */ }
      } catch (e) { message.error((e as Error).message); }
    })();
  }, [lookups, message]);

  return (
    <>
      <Typography.Title level={3}>Огляд</Typography.Title>
      <Row gutter={16} className="stat-row">
        <Col xs={12} md={6}><Card><Statistic title="Усього документів" value={total} prefix={<FileTextOutlined />} /></Card></Col>
        <Col xs={12} md={6}><Card><Statistic title="На контролі" value={controlled} prefix={<FlagOutlined />} styles={{ content: { color: '#1d6fb8' } }} /></Card></Col>
        <Col xs={12} md={6}><Card><Statistic title="На виконанні" value={onExec} prefix={<SyncOutlined />} styles={{ content: { color: '#d48806' } }} /></Card></Col>
        <Col xs={12} md={6}><Card><Statistic title="Прострочених доручень" value={overdue} prefix={<WarningOutlined />} styles={{ content: { color: '#cf1322' } }} /></Card></Col>
      </Row>

      <Card title="Нещодавно зареєстровані документи">
        <Table rowKey="id" pagination={false} size="middle" dataSource={recent}
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
