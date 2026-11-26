import { Card, Col, Progress, Row, Table, Tag, Typography } from 'antd';
import dayjs from 'dayjs';
import { api } from '../api/client';
import { useApiData } from '../hooks/useApi';

interface ByType { type: string; count: number; }
interface ByDir { direction: string; count: number; percent: number; }
interface Disc { executor: string; total: number; done: number; percent: number; }
interface Overdue { taskId: number; document: string; executor: string; dueDate: string; daysOverdue: number; }

interface Reports { byType: ByType[]; byDir: ByDir[]; disc: Disc[]; overdue: Overdue[]; }
const EMPTY: Reports = { byType: [], byDir: [], disc: [], overdue: [] };

const loadReports = async (): Promise<Reports> => {
  const [byType, byDir, disc, overdue] = await Promise.all([
    api<ByType[]>('/api/reports/by-type'),
    api<ByDir[]>('/api/reports/by-direction'),
    api<Disc[]>('/api/reports/executor-discipline'),
    api<Overdue[]>('/api/reports/overdue-tasks'),
  ]);
  return { byType, byDir, disc, overdue };
};

export default function ReportsPage() {
  const { data: { byType, byDir, disc, overdue } } = useApiData(loadReports, EMPTY);

  return (
    <>
      <Typography.Title level={3}>Звіти та статистика</Typography.Title>
      <Row gutter={16}>
        <Col xs={24} lg={12} style={{ marginBottom: 16 }}>
          <Card title="1. Кількість документів за типами">
            <Table size="small" pagination={false} rowKey="type" dataSource={byType}
              columns={[
                { title: 'Тип документа', dataIndex: 'type' },
                { title: 'Кількість', dataIndex: 'count', width: 120, align: 'right' },
              ]}
              summary={() => (
                <Table.Summary.Row>
                  <Table.Summary.Cell index={0}><b>Усього</b></Table.Summary.Cell>
                  <Table.Summary.Cell index={1} align="right"><b>{byType.reduce((m, r) => m + r.count, 0)}</b></Table.Summary.Cell>
                </Table.Summary.Row>
              )} />
          </Card>
        </Col>

        <Col xs={24} lg={12} style={{ marginBottom: 16 }}>
          <Card title="2. Розподіл за напрямом кореспонденції">
            <Table size="small" pagination={false} rowKey="direction" dataSource={byDir}
              columns={[
                { title: 'Напрям', dataIndex: 'direction' },
                { title: 'Кількість', dataIndex: 'count', width: 110, align: 'right' },
                { title: 'Частка', dataIndex: 'percent', width: 160, render: (v: number) => <Progress percent={v} size="small" /> },
              ]} />
          </Card>
        </Col>

        <Col xs={24} lg={12} style={{ marginBottom: 16 }}>
          <Card title="3. Виконавська дисципліна працівників">
            <Table size="small" pagination={false} rowKey="executor" dataSource={disc}
              columns={[
                { title: 'Виконавець', dataIndex: 'executor' },
                { title: 'Усього', dataIndex: 'total', width: 90, align: 'right' },
                { title: 'Виконано', dataIndex: 'done', width: 100, align: 'right' },
                { title: '% виконання', dataIndex: 'percent', width: 140,
                  render: (v: number) => <Progress percent={v} size="small" status={v < 50 ? 'exception' : 'active'} /> },
              ]} />
          </Card>
        </Col>

        <Col xs={24} lg={12} style={{ marginBottom: 16 }}>
          <Card title="4. Прострочені доручення">
            <Table size="small" pagination={false} rowKey="taskId" dataSource={overdue}
              locale={{ emptyText: 'Прострочених доручень немає' }}
              columns={[
                { title: 'Документ', dataIndex: 'document', width: 100 },
                { title: 'Виконавець', dataIndex: 'executor' },
                { title: 'Строк', dataIndex: 'dueDate', width: 120, render: (v) => dayjs(v).format('DD.MM.YYYY') },
                { title: 'Прострочення', dataIndex: 'daysOverdue', width: 140, align: 'right',
                  render: (v: number) => <Tag color="red">{v} дн.</Tag> },
              ]} />
          </Card>
        </Col>
      </Row>
    </>
  );
}
