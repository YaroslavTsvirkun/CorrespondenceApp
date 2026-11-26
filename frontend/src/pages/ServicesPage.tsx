import { Card, Col, Empty, Layout, Row, Typography } from 'antd';
import { AppstoreOutlined, RightOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { services } from '../services/registry';
import UserMenu from '../components/UserMenu';

const { Header, Content } = Layout;

/** Стартова сторінка після входу: сервіси, доступні користувачеві. */
export default function ServicesPage() {
  const { user, canAny } = useAuth();
  const navigate = useNavigate();
  const available = services.filter((s) => s.isAvailable(canAny));

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header style={{ background: '#fff', padding: '0 24px', display: 'flex', alignItems: 'center', borderBottom: '1px solid #f0f0f0' }}>
        <AppstoreOutlined style={{ fontSize: 22, color: '#1d6fb8', marginInlineEnd: 12 }} />
        <Typography.Title level={4} style={{ margin: 0, flex: 1 }}>Сервіси</Typography.Title>
        <UserMenu />
      </Header>
      <Content style={{ padding: '32px 24px', maxWidth: 1200, width: '100%', margin: '0 auto' }}>
        <Typography.Title level={3} style={{ marginTop: 0 }}>
          Вітаємо, {user?.fullName}
        </Typography.Title>
        <Typography.Paragraph type="secondary">Оберіть сервіс для роботи.</Typography.Paragraph>

        {available.length === 0 ? (
          <Empty description="Немає доступних сервісів. Зверніться до адміністратора." style={{ marginTop: 48 }} />
        ) : (
          <Row gutter={[24, 24]}>
            {available.map((s) => (
              <Col key={s.id} xs={24} sm={12} lg={8}>
                <Card
                  hoverable
                  onClick={() => navigate(s.path)}
                  style={{ height: '100%', borderTop: `4px solid ${s.color}` }}
                >
                  <div style={{ display: 'flex', gap: 16, alignItems: 'flex-start' }}>
                    <div style={{
                      fontSize: 28, color: s.color, background: `${s.color}14`,
                      width: 56, height: 56, borderRadius: 12, flexShrink: 0,
                      display: 'flex', alignItems: 'center', justifyContent: 'center',
                    }}>
                      {s.icon}
                    </div>
                    <div style={{ flex: 1 }}>
                      <Typography.Title level={5} style={{ marginTop: 0 }}>{s.title}</Typography.Title>
                      <Typography.Paragraph type="secondary" style={{ marginBottom: 0 }}>{s.description}</Typography.Paragraph>
                    </div>
                    <RightOutlined style={{ color: '#bfbfbf', alignSelf: 'center' }} />
                  </div>
                </Card>
              </Col>
            ))}
          </Row>
        )}
      </Content>
    </Layout>
  );
}
