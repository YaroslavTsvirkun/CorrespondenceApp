import { useState } from 'react';
import { App, Button, Card, Form, Input, Typography } from 'antd';
import { LockOutlined, MailOutlined, UserOutlined } from '@ant-design/icons';
import { useAuth } from '../auth/AuthContext';

export default function LoginPage() {
  const { login } = useAuth();
  const { message } = App.useApp();
  const [loading, setLoading] = useState(false);

  const onFinish = async (v: { userName: string; password: string }) => {
    setLoading(true);
    try {
      await login(v.userName, v.password);
      message.success('Вхід виконано');
    } catch (e) {
      message.error((e as Error).message || 'Помилка входу');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{
      minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center',
      background: '#f0f2f5', padding: 16,
    }}>
      <Card style={{ width: 380, boxShadow: '0 4px 20px rgba(0,0,0,0.08)' }}>
        <div style={{ textAlign: 'center', marginBottom: 20 }}>
          <MailOutlined style={{ fontSize: 36, color: '#1d6fb8' }} />
          <Typography.Title level={4} style={{ marginTop: 8, marginBottom: 0 }}>
            Реєстрація кореспонденції
          </Typography.Title>
          <Typography.Text type="secondary">Вхід до системи</Typography.Text>
        </div>
        <Form layout="vertical" onFinish={onFinish} initialValues={{ userName: '', password: '' }}>
          <Form.Item name="userName" label="Логін" rules={[{ required: true, message: 'Введіть логін' }]}>
            <Input prefix={<UserOutlined />} placeholder="напр. registrar" autoFocus />
          </Form.Item>
          <Form.Item name="password" label="Пароль" rules={[{ required: true, message: 'Введіть пароль' }]}>
            <Input.Password prefix={<LockOutlined />} placeholder="••••••" />
          </Form.Item>
          <Button type="primary" htmlType="submit" block loading={loading}>
            Увійти
          </Button>
        </Form>
        <Typography.Paragraph type="secondary" style={{ fontSize: 12, marginTop: 16, marginBottom: 0 }}>
          Демо-облікові записи: <b>admin</b>/admin123, <b>registrar</b>/registrar123,
          {' '}<b>manager</b>/manager123, <b>executor</b>/executor123.
        </Typography.Paragraph>
      </Card>
    </div>
  );
}
