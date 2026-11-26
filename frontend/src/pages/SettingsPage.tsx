import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { Alert, App, Button, Card, Form, Input, Space, Tag, Typography } from 'antd';
import { SaveOutlined } from '@ant-design/icons';
import { api } from '../api/client';
import { useApiAction, useApiData } from '../hooks/useApi';

interface RegSetting { organizationId: number; organizationName: string; pattern: string; preview: string; }

const TOKENS: [string, string][] = [
  ['{seq}', 'порядковий номер (лічильник за типом і роком)'],
  ['{seq:000}', 'те саме з доповненням нулями (001, 002, …)'],
  ['{year}', 'рік, 4 цифри (2026)'],
  ['{yy}', 'рік, 2 цифри (26)'],
  ['{type}', 'код типу документа (LST, NAK, …)'],
  ['{dir}', 'код напряму (IN, OUT, INT)'],
];

// Клієнтський приклад номера за патерном (seq=1, тип LST, напрям IN)
function previewOf(pattern: string): string {
  const year = new Date().getFullYear();
  return pattern
    .replace(/\{seq:(0+)\}/gi, (_, z: string) => String(1).padStart(z.length, '0'))
    .replace(/\{seq\}/gi, '1')
    .replace(/\{year\}/gi, String(year))
    .replace(/\{yy\}/gi, String(year % 100).padStart(2, '0'))
    .replace(/\{type\}/gi, 'LST')
    .replace(/\{dir\}/gi, 'IN');
}

const loadSetting = () => api<RegSetting | null>('/api/settings/registration');

export default function SettingsPage() {
  const { message } = App.useApp();
  const run = useApiAction();
  const { data, loading, reload } = useApiData(loadSetting, null);
  // Патерн у полі: чернетка користувача або поточний із сервера
  const [draft, setDraft] = useState<string | null>(null);
  const pattern = draft ?? data?.pattern ?? '';
  const [saving, setSaving] = useState(false);

  const livePreview = useMemo(() => previewOf(pattern || ''), [pattern]);

  const save = async () => {
    if (!pattern.trim()) { message.warning('Вкажіть патерн'); return; }
    setSaving(true);
    const saved = await run(
      () => api('/api/settings/registration', { method: 'PUT', body: JSON.stringify({ pattern: pattern.trim() }) }),
      'Налаштування збережено');
    setSaving(false);
    if (saved) { setDraft(null); reload(); }
  };

  // Супер-адмін не прив'язаний до організації — патерн задається в картці кожної організації
  if (!loading && !data) {
    return (
      <>
        <Typography.Title level={3}>Налаштування</Typography.Title>
        <Alert type="info" showIcon style={{ maxWidth: 720 }}
          message="Ваш обліковий запис не прив'язаний до організації"
          description={<>Патерн реєстраційного номера задається окремо для кожної організації —
            у її картці в розділі <Link to="/correspondence/organizations">«Організації»</Link>.</>} />
      </>
    );
  }

  return (
    <>
      <Typography.Title level={3}>Налаштування</Typography.Title>
      <Typography.Paragraph type="secondary">
        Налаштування автоматичної генерації реєстраційного номера документів
        {data && <> для організації <b>{data.organizationName}</b></>}.
      </Typography.Paragraph>

      <Card title="Патерн реєстраційного номера" style={{ maxWidth: 720 }}>
        <Form layout="vertical">
          <Form.Item label="Патерн"
            help="Лічильник зростає окремо для кожного типу документа в межах року.">
            <Input value={pattern} onChange={(e) => setDraft(e.target.value)}
              placeholder="{type}-{seq:000}/{year}" style={{ fontFamily: 'monospace' }} />
          </Form.Item>
          <Form.Item label="Приклад">
            <Tag color="geekblue" style={{ fontSize: 14, padding: '4px 10px' }}>{livePreview || '—'}</Tag>
          </Form.Item>
          <Space>
            <Button type="primary" icon={<SaveOutlined />} loading={saving} onClick={save}>Зберегти</Button>
            {data && <Typography.Text type="secondary">поточний: <code>{data.pattern}</code></Typography.Text>}
          </Space>
        </Form>

        <Typography.Title level={5} style={{ marginTop: 24 }}>Доступні токени</Typography.Title>
        <ul style={{ marginBottom: 0 }}>
          {TOKENS.map(([t, d]) => (
            <li key={t}><code>{t}</code> — {d}</li>
          ))}
        </ul>
      </Card>
    </>
  );
}
