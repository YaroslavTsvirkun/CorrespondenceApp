import { useEffect, useState } from 'react';
import { App, Button, Modal, Popconfirm, Space, Table } from 'antd';
import { DownloadOutlined, RollbackOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import { api, apiDownload } from '../api/client';
import { formatSize } from './attachmentPreview';

interface Version {
  id: number; versionNo: number; fileName: string; sizeBytes: number; createdAt: string; createdBy?: string;
}

interface FileRef { id: number; fileName: string }

interface Props {
  attachment: FileRef | null;
  canEdit: boolean;
  onClose: () => void;
  onRestored: () => void;
}

/** Попередні версії вмісту вкладення: завантаження та відновлення. */
export default function AttachmentVersions({ attachment, canEdit, onClose, onRestored }: Props) {
  return (
    <Modal open={!!attachment} onCancel={onClose} footer={null} width={760} destroyOnHidden
      title={`Попередні версії: ${attachment?.fileName ?? ''}`}>
      {/* key: для іншого вкладення — новий екземпляр із чистим станом */}
      {attachment && <VersionsTable key={attachment.id} attachment={attachment} canEdit={canEdit} onRestored={onRestored} />}
    </Modal>
  );
}

function VersionsTable({ attachment, canEdit, onRestored }: { attachment: FileRef; canEdit: boolean; onRestored: () => void }) {
  const { message } = App.useApp();
  const [items, setItems] = useState<Version[]>([]);
  const [loading, setLoading] = useState(true);
  const [revision, setRevision] = useState(0); // збільшується після відновлення -> перезавантаження списку

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const list = await api<Version[]>(`/api/attachments/${attachment.id}/versions`);
        if (!cancelled) setItems(list);
      } catch (e) {
        if (!cancelled) message.error((e as Error).message);
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => { cancelled = true; };
  }, [attachment.id, message, revision]);

  const restore = async (v: Version) => {
    try {
      await api(`/api/attachments/${attachment.id}/versions/${v.id}/restore`, { method: 'POST' });
      message.success(`Відновлено версію ${v.versionNo}`);
      onRestored();
      setLoading(true);
      setRevision((r) => r + 1);
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  const download = async (v: Version) => {
    try {
      await apiDownload(`/api/attachments/${attachment.id}/versions/${v.id}/content`, v.fileName);
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  return (
    <Table rowKey="id" size="small" pagination={false} loading={loading} dataSource={items}
      locale={{ emptyText: 'Попередніх версій немає' }}
      columns={[
        { title: '№', dataIndex: 'versionNo', width: 50 },
        { title: 'Файл', dataIndex: 'fileName', ellipsis: true },
        { title: 'Розмір', dataIndex: 'sizeBytes', width: 90, render: formatSize },
        { title: 'Автор', dataIndex: 'createdBy', width: 170, ellipsis: true, render: (v) => v || '—' },
        { title: 'Створено', dataIndex: 'createdAt', width: 130, render: (v) => dayjs(v).format('DD.MM.YYYY HH:mm') },
        {
          title: '', width: 80, render: (_, v) => (
            <Space size={4}>
              <Button size="small" title="Завантажити" icon={<DownloadOutlined />} onClick={() => download(v)} />
              {canEdit && (
                <Popconfirm title="Відновити цю версію?" description="Поточний вміст збережеться як нова версія."
                  okText="Так" cancelText="Ні" onConfirm={() => restore(v)}>
                  <Button size="small" title="Відновити" icon={<RollbackOutlined />} />
                </Popconfirm>
              )}
            </Space>
          ),
        },
      ]} />
  );
}
