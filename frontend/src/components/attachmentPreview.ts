/** Розширення, які можна переглянути у браузері (PDF/HTML — як є, решта — через конвертацію у PDF на сервері). */
const PREVIEWABLE = new Set(['pdf', 'html', 'htm', 'doc', 'docx', 'odt', 'rtf', 'xls', 'xlsx', 'ods', 'ppt', 'pptx', 'odp']);

export function extensionOf(fileName: string): string {
  const i = fileName.lastIndexOf('.');
  return i < 0 ? '' : fileName.slice(i + 1).toLowerCase();
}

export function canPreview(fileName: string): boolean {
  return PREVIEWABLE.has(extensionOf(fileName));
}
