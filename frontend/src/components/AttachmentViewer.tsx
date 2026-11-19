import { useEffect, useState } from 'react';
import { Button, Modal, Result, Space, Spin, Typography } from 'antd';
import { DownloadOutlined } from '@ant-design/icons';
import { apiBlob } from '../api/client';
import { extensionOf } from './attachmentPreview';

/**
 * Ізольований HTML: без скриптів (iframe sandbox) і без зовнішніх ресурсів (CSP у документі),
 * щоб вкладення не могло виконати код чи «подзвонити додому».
 */
const HTML_CSP = `<meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'unsafe-inline'; img-src data:; font-src data:">`;

/** Декодує HTML з урахуванням <meta charset> (старі документи бувають у windows-1251). */
async function decodeHtml(blob: Blob): Promise<string> {
  const bytes = await blob.arrayBuffer();
  const head = new TextDecoder('latin1').decode(bytes.slice(0, 2048));
  const charset = /<meta[^>]+charset\s*=\s*["']?([\w-]+)/i.exec(head)?.[1] ?? 'utf-8';
  try {
    return new TextDecoder(charset).decode(bytes);
  } catch {
    return new TextDecoder('utf-8').decode(bytes);
  }
}

function isolateHtml(html: string): string {
  return /<head[^>]*>/i.test(html)
    ? html.replace(/<head[^>]*>/i, (m) => m + HTML_CSP)
    : HTML_CSP + html;
}

type View =
  | { kind: 'loading' }
  | { kind: 'pdf'; url: string }
  | { kind: 'html'; html: string }
  | { kind: 'error'; message: string };

interface FileRef { id: number; fileName: string }

interface Props {
  attachment: FileRef | null;
  onClose: () => void;
  onDownload: () => void;
}

/** Переглядач вкладення у модальному вікні: PDF, DOC/DOCX, XLS/XLSX (як PDF), HTML (ізольовано). */
export default function AttachmentViewer({ attachment, onClose, onDownload }: Props) {
  return (
    <Modal
      open={!!attachment}
      onCancel={onClose}
      destroyOnHidden
      centered
      width="min(1200px, 95vw)"
      title={
        <Space>
          <Typography.Text ellipsis style={{ maxWidth: '60vw' }}>{attachment?.fileName}</Typography.Text>
          <Button size="small" icon={<DownloadOutlined />} onClick={onDownload}>Завантажити</Button>
        </Space>
      }
      footer={null}
      styles={{ body: { height: '80vh', padding: 0, overflow: 'hidden' } }}
    >
      {/* key: для іншого файлу — новий екземпляр із чистим станом */}
      {attachment && <ViewerBody key={attachment.id} attachment={attachment} onDownload={onDownload} />}
    </Modal>
  );
}

function ViewerBody({ attachment, onDownload }: { attachment: FileRef; onDownload: () => void }) {
  const [view, setView] = useState<View>({ kind: 'loading' });

  useEffect(() => {
    const ctrl = new AbortController();
    let url: string | null = null;

    (async () => {
      try {
        const blob = await apiBlob(`/api/attachments/${attachment.id}/preview`, ctrl.signal);
        const ext = extensionOf(attachment.fileName);
        if (ext === 'html' || ext === 'htm') {
          setView({ kind: 'html', html: isolateHtml(await decodeHtml(blob)) });
        } else {
          url = URL.createObjectURL(new Blob([blob], { type: 'application/pdf' }));
          setView({ kind: 'pdf', url });
        }
      } catch (e) {
        if (!ctrl.signal.aborted) setView({ kind: 'error', message: (e as Error).message });
      }
    })();

    return () => {
      ctrl.abort();
      if (url) URL.revokeObjectURL(url);
    };
  }, [attachment]);

  const frameStyle: React.CSSProperties = { width: '100%', height: '100%', border: 0, background: '#fff' };

  return (
    <>
      {view.kind === 'loading' && (
        <div style={{ height: '100%', display: 'grid', placeItems: 'center' }}>
          <Spin size="large" tip="Підготовка документа…"><div style={{ width: 200, height: 80 }} /></Spin>
        </div>
      )}
      {view.kind === 'error' && (
        <Result status="warning" title="Не вдалося відкрити документ" subTitle={view.message}
          extra={<Button type="primary" icon={<DownloadOutlined />} onClick={onDownload}>Завантажити файл</Button>} />
      )}
      {view.kind === 'pdf' && <iframe title={attachment.fileName} src={view.url} style={frameStyle} />}
      {view.kind === 'html' && (
        // sandbox без allow-scripts / allow-same-origin: скрипти, форми й доступ до сторінки заборонені
        <iframe title={attachment.fileName} sandbox="" srcDoc={view.html} style={frameStyle} />
      )}
    </>
  );
}
