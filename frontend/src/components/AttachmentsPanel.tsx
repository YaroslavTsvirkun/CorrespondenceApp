import { useCallback, useEffect, useState } from 'react';
import { App, Button, Popconfirm, Space, Table, Typography, Upload } from 'antd';
import { DeleteOutlined, DownloadOutlined, EyeOutlined, InboxOutlined, PaperClipOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import { api, apiDownload, apiUpload } from '../api/client';
import AttachmentViewer from './AttachmentViewer';
import { canPreview } from './attachmentPreview';

interface Attachment {
  id: number; fileName: string; contentType: string; sizeBytes: number;
  addedAt: string; addedBy?: string; description?: string;
}

/** Дозволені типи файлів (дзеркало FileStorage:AllowedExtensions на бекенді). */
const ACCEPT = [
  '.pdf', '.doc', '.docx', '.odt', '.rtf', '.txt', '.xls', '.xlsx', '.ods', '.csv', '.ppt', '.pptx', '.odp', '.html', '.htm',
  '.jpg', '.jpeg', '.png', '.tif', '.tiff', '.zip', '.7z', '.xml', '.eml', '.msg', '.p7s', '.asice', '.asics',
].join(',');

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} Б`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} КБ`;
  return `${(bytes / 1024 / 1024).toFixed(1)} МБ`;
}

/** Вкладення документа: список, завантаження (drag-and-drop), скачування, видалення. */
export default function AttachmentsPanel({ documentId, canEdit }: { documentId: number; canEdit: boolean }) {
  const { message } = App.useApp();
  const [items, setItems] = useState<Attachment[]>([]);
  const [loading, setLoading] = useState(false);
  const [uploading, setUploading] = useState(0);
  const [viewing, setViewing] = useState<Attachment | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setItems(await api<Attachment[]>(`/api/documents/${documentId}/attachments`));
    } catch (e) {
      message.error((e as Error).message);
    } finally {
      setLoading(false);
    }
  }, [documentId, message]);

  useEffect(() => { load(); }, [load]);

  const upload = async (file: File) => {
    setUploading((n) => n + 1);
    try {
      const form = new FormData();
      form.append('file', file);
      await apiUpload(`/api/documents/${documentId}/attachments`, form);
      message.success(`Файл «${file.name}» додано`);
      await load();
    } catch (e) {
      message.error(`«${file.name}»: ${(e as Error).message}`);
    } finally {
      setUploading((n) => n - 1);
    }
  };

  const download = async (a: Attachment) => {
    try {
      await apiDownload(`/api/attachments/${a.id}/content`, a.fileName);
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  const remove = async (a: Attachment) => {
    try {
      await api(`/api/attachments/${a.id}`, { method: 'DELETE' });
      message.success('Вкладення видалено');
      await load();
    } catch (e) {
      message.error((e as Error).message);
    }
  };

  return (
    <>
      <Typography.Title level={5} style={{ marginTop: 20 }}>
        <PaperClipOutlined /> Вкладення
      </Typography.Title>
      {canEdit && (
        // Обгортка: Dragger має height: 100% і без неї розтягується на всю висоту Drawer
        <div style={{ marginBottom: 12 }}>
          <Upload.Dragger multiple accept={ACCEPT} showUploadList={false} disabled={uploading > 0}
            beforeUpload={(file) => { upload(file); return false; }}>
            <p className="ant-upload-drag-icon"><InboxOutlined /></p>
            <p className="ant-upload-text">
              {uploading > 0 ? 'Завантаження…' : 'Перетягніть файли сюди або натисніть, щоб обрати'}
            </p>
            <p className="ant-upload-hint">PDF, документи Office, HTML, зображення, архіви, файли з КЕП (.p7s, .asice)</p>
          </Upload.Dragger>
        </div>
      )}
      <Table rowKey="id" size="small" pagination={false} loading={loading} dataSource={items}
        locale={{ emptyText: 'Вкладень немає' }}
        columns={[
          {
            title: 'Файл', dataIndex: 'fileName', ellipsis: true,
            // Клік по імені: перегляд, якщо формат підтримується, інакше — завантаження
            render: (v: string, a) => (
              <Typography.Link onClick={() => (canPreview(a.fileName) ? setViewing(a) : download(a))}>{v}</Typography.Link>
            ),
          },
          { title: 'Розмір', dataIndex: 'sizeBytes', width: 90, render: formatSize },
          { title: 'Додав', dataIndex: 'addedBy', width: 160, ellipsis: true, render: (v) => v || '—' },
          { title: 'Додано', dataIndex: 'addedAt', width: 130, render: (v) => dayjs(v).format('DD.MM.YYYY HH:mm') },
          {
            title: '', width: 110, render: (_, a) => (
              <Space size={4}>
                {canPreview(a.fileName) && (
                  <Button size="small" title="Переглянути" icon={<EyeOutlined />} onClick={() => setViewing(a)} />
                )}
                <Button size="small" title="Завантажити" icon={<DownloadOutlined />} onClick={() => download(a)} />
                {canEdit && (
                  <Popconfirm title="Видалити вкладення?" okText="Так" cancelText="Ні" onConfirm={() => remove(a)}>
                    <Button size="small" danger icon={<DeleteOutlined />} />
                  </Popconfirm>
                )}
              </Space>
            ),
          },
        ]} />
      <AttachmentViewer attachment={viewing} onClose={() => setViewing(null)}
        onDownload={() => viewing && download(viewing)} />
    </>
  );
}
