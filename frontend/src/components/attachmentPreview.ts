/** Розширення, які можна переглянути у браузері (PDF/HTML — як є, решта — через конвертацію у PDF на сервері). */
const PREVIEWABLE = new Set(['pdf', 'html', 'htm', 'doc', 'docx', 'odt', 'rtf', 'xls', 'xlsx', 'ods', 'ppt', 'pptx', 'odp']);

export function extensionOf(fileName: string): string {
  const i = fileName.lastIndexOf('.');
  return i < 0 ? '' : fileName.slice(i + 1).toLowerCase();
}

export function canPreview(fileName: string): boolean {
  return PREVIEWABLE.has(extensionOf(fileName));
}

/** Документи Word, які можна редагувати в ONLYOFFICE (.doc зберігається як .docx). */
export function canEditInBrowser(fileName: string): boolean {
  const ext = extensionOf(fileName);
  return ext === 'doc' || ext === 'docx';
}

export function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} Б`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} КБ`;
  return `${(bytes / 1024 / 1024).toFixed(1)} МБ`;
}
