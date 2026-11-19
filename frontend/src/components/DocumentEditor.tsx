import { useCallback, useEffect, useRef, useState } from 'react';
import { App, Button, Modal, Result, Space, Spin } from 'antd';
import { api } from '../api/client';

/** Мінімальний опис JS API ONLYOFFICE Docs (web-apps/apps/api/documents/api.js). */
interface DocEditorInstance { destroyEditor(): void }
declare global {
  interface Window {
    DocsAPI?: { DocEditor: new (elementId: string, config: Record<string, unknown>) => DocEditorInstance };
  }
}

interface EditorConfigResponse { documentServerUrl: string; config: Record<string, unknown> }

/** Завантаження api.js з Document Server (один раз на адресу). */
const scripts = new Map<string, Promise<void>>();
function loadDocsApi(serverUrl: string): Promise<void> {
  const src = `${serverUrl}/web-apps/apps/api/documents/api.js`;
  let p = scripts.get(src);
  if (!p) {
    p = new Promise<void>((resolve, reject) => {
      const s = document.createElement('script');
      s.src = src;
      s.async = true;
      s.onload = () => resolve();
      s.onerror = () => {
        scripts.delete(src);
        s.remove();
        reject(new Error('Сервер редактора документів недоступний'));
      };
      document.head.appendChild(s);
    });
    scripts.set(src, p);
  }
  return p;
}

let counter = 0;

interface Props {
  attachment: { id: number; fileName: string } | null;
  /** Викликається після закриття: збереження відбувається на сервері із затримкою. */
  onClose: (changed: boolean) => void;
}

/** Редактор документа Word (ONLYOFFICE) у модальному вікні на весь екран. */
export default function DocumentEditor({ attachment, onClose }: Props) {
  const changed = useRef(false);
  // Стабільний колбек: інакше ефект редактора перезапускався б при кожному рендері батька
  const markChanged = useCallback(() => { changed.current = true; }, []);
  const close = () => {
    const wasChanged = changed.current;
    changed.current = false;
    onClose(wasChanged);
  };
  return (
    <Modal
      open={!!attachment}
      onCancel={close}
      destroyOnHidden
      centered
      mask={{ closable: false }}
      width="98vw"
      title={attachment?.fileName}
      footer={null}
      styles={{ body: { height: '88vh', padding: 0, overflow: 'hidden' } }}
    >
      {attachment && (
        <EditorBody key={attachment.id} attachmentId={attachment.id} onChanged={markChanged} />
      )}
    </Modal>
  );
}

/** Якщо документ не відкрився за цей час — показуємо повідомлення з можливістю повторити. */
const LOAD_TIMEOUT_MS = 90_000;

type Stage = 'config' | 'editor' | 'document' | 'ready' | 'timeout';

const STAGE_TEXT: Record<'config' | 'editor' | 'document', string> = {
  config: 'Підготовка документа…',
  editor: 'Завантаження редактора…',
  document: 'Відкриття документа…',
};

function EditorBody({ attachmentId, onChanged }: { attachmentId: number; onChanged: () => void }) {
  const [attempt, setAttempt] = useState(0);
  // key: «Спробувати ще раз» створює редактор заново
  return <EditorInstance key={attempt} attachmentId={attachmentId} onChanged={onChanged}
    onRetry={() => setAttempt((a) => a + 1)} />;
}

function EditorInstance({ attachmentId, onChanged, onRetry }: {
  attachmentId: number; onChanged: () => void; onRetry: () => void;
}) {
  const { message } = App.useApp();
  const [elementId] = useState(() => `onlyoffice-editor-${++counter}`);
  const [stage, setStage] = useState<Stage>('config');
  const [error, setError] = useState<string>();

  useEffect(() => {
    let editor: DocEditorInstance | null = null;
    let cancelled = false;
    let ready = false;
    const watchdog = setTimeout(() => { if (!ready && !cancelled) setStage('timeout'); }, LOAD_TIMEOUT_MS);

    (async () => {
      try {
        const { documentServerUrl, config } = await api<EditorConfigResponse>(`/api/attachments/${attachmentId}/editor`);
        if (cancelled) return;
        setStage('editor');
        await loadDocsApi(documentServerUrl);
        if (cancelled || !window.DocsAPI) return;
        editor = new window.DocsAPI.DocEditor(elementId, {
          ...config,
          width: '100%',
          height: '100%',
          type: 'desktop',
          events: {
            onAppReady: () => { if (!cancelled) setStage((s) => (s === 'ready' ? s : 'document')); },
            onDocumentReady: () => { ready = true; if (!cancelled) setStage('ready'); },
            onDocumentStateChange: (e: { data: boolean }) => { if (e.data) onChanged(); },
            onError: (e: { data: { errorDescription?: string } }) =>
              message.error(e.data?.errorDescription ?? 'Помилка редактора документів'),
          },
        });
      } catch (e) {
        if (!cancelled) setError((e as Error).message);
      }
    })();

    return () => {
      cancelled = true;
      clearTimeout(watchdog);
      editor?.destroyEditor();
    };
  }, [attachmentId, elementId, message, onChanged]);

  if (error)
    return (
      <Result status="warning" title="Не вдалося відкрити редактор" subTitle={error}
        extra={<Button type="primary" onClick={onRetry}>Спробувати ще раз</Button>} />
    );

  return (
    <div style={{ position: 'relative', height: '100%' }}>
      {stage === 'timeout' ? (
        <div style={overlay}>
          <Result status="warning" title="Редактор не відповідає"
            subTitle="Сервер редактора документів може ще запускатися (після старту — до хвилини). Спробуйте ще раз."
            extra={<Button type="primary" onClick={onRetry}>Спробувати ще раз</Button>} />
        </div>
      ) : stage !== 'ready' && (
        <div style={{ ...overlay, pointerEvents: 'none', background: 'transparent', alignItems: 'end', paddingBottom: 48 }}>
          <Space style={{ background: 'rgba(255,255,255,0.95)', padding: '8px 16px', borderRadius: 8,
            boxShadow: '0 2px 8px rgba(0,0,0,0.15)' }}>
            <Spin size="small" />
            <span>{STAGE_TEXT[stage]}</span>
          </Space>
        </div>
      )}
      <div id={elementId} />
    </div>
  );
}

const overlay: React.CSSProperties = {
  position: 'absolute', inset: 0, display: 'grid', placeItems: 'center', zIndex: 1, background: '#fff',
};
