import { Avatar, Dropdown, Space, Tag } from 'antd';
import { BankOutlined, LogoutOutlined, UserOutlined } from '@ant-design/icons';
import { useAuth } from '../auth/AuthContext';

/** Організація, ім'я й ролі користувача та меню «Вийти» — спільне для шапок усіх сервісів. */
export default function UserMenu() {
  const { user, logout } = useAuth();
  if (!user) return null;

  return (
    <>
      {user.organizationId == null
        ? <Tag icon={<BankOutlined />} color="purple" style={{ marginInlineEnd: 16 }}>Усі організації</Tag>
        : user.organizationName && <Tag icon={<BankOutlined />} color="geekblue" style={{ marginInlineEnd: 16 }}>{user.organizationName}</Tag>}
      <Dropdown
        menu={{ items: [{ key: 'logout', icon: <LogoutOutlined />, label: 'Вийти', onClick: logout }] }}
      >
        <Space style={{ cursor: 'pointer' }}>
          <Avatar icon={<UserOutlined />} />
          <span>
            {user.fullName}{' '}
            {user.roles.map((r) => <Tag key={r} color="blue" style={{ marginInlineEnd: 0 }}>{r}</Tag>)}
          </span>
        </Space>
      </Dropdown>
    </>
  );
}
