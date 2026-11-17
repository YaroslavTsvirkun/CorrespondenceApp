// Доменні типи відповідно до схеми бази даних відділу реєстрації кореспонденції

export interface Lookup {
  id: number;
  name: string;
  description?: string;
}

export interface DocumentDirection extends Lookup {
  code: string; // IN / OUT / INT
}

export interface DocumentStatus extends Lookup {
  isFinal: boolean;
}

export interface TaskStatus extends Lookup {
  isFinal: boolean;
}

export interface Department {
  id: number;
  name: string;
  shortName?: string;
  description?: string;
  isActive: boolean;
}

export interface Employee {
  id: number;
  departmentId: number;
  fullName: string;
  positionName: string;
  phone?: string;
  email?: string;
  isActive: boolean;
}

export interface Correspondent {
  id: number;
  typeId: number;
  name: string;
  address?: string;
  phone?: string;
  email?: string;
  contactPerson?: string;
  note?: string;
}

export interface DocumentItem {
  id: number;
  directionId: number;
  typeId: number;
  statusId: number;
  channelId?: number;
  correspondentId?: number;
  senderDepartmentId?: number;
  receiverDepartmentId?: number;
  authorEmployeeId?: number;
  title: string;
  summary?: string;
  documentDate: string; // ISO yyyy-mm-dd
  isControlled: boolean;
  note?: string;
  // реєстраційні дані (1:1)
  registrationNumber: string;
  registrationYear: number;
  registeredAt: string; // ISO datetime
  registrarEmployeeId: number;
}

export interface ExecutionTask {
  id: number;
  documentId: number;
  resolutionText: string;
  executorEmployeeId: number;
  statusId: number;
  taskText: string;
  assignedAt: string; // ISO date
  dueDate: string; // ISO date
  completedAt?: string;
  resultText?: string;
}
