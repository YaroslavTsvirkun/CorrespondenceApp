// Коди пермішенів (відповідають Permissions у contracts/Common/Permissions.cs).
// Композитні політики backend (напр. documents.read) тут не перелічено: у user.permissions їх
// ніколи немає — для «будь-який із» використовуйте canAny(...P.docsReadScopes).
export const P = {
  docsCreate: 'documents.create',
  docsUpdate: 'documents.update',
  docsDelete: 'documents.delete',
  resolutionsCreate: 'resolutions.create',
  tasksAssign: 'tasks.assign',
  tasksExecute: 'tasks.execute',
  parties: 'parties.manage',
  partiesBank: 'parties.bank.manage',
  dictionaries: 'dictionaries.manage',
  employees: 'employees.manage',
  departments: 'departments.manage',
  reports: 'reports.view',
  dashOverview: 'dashboard.overview',
  dashControl: 'dashboard.control',
  dashMyTasks: 'dashboard.my-tasks',
  users: 'users.manage',
  organizations: 'organizations.manage',
  settings: 'settings.manage',
  // будь-який скоуп перегляду (для відображення пункту меню «Документи»)
  docsReadScopes: ['documents.read.all', 'documents.read.department', 'documents.read.involved'],
} as const;
