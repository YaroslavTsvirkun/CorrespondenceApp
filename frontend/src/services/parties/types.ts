import dayjs from 'dayjs';

// Типи API сервісу «Контрагенти» (contracts/Parties/Dtos).

export interface PartyListItem {
  id: number; kind: number; fullName: string; shortName?: string; code?: string; legalForm?: string;
  parentPartyId?: number; parentName?: string; isActive: boolean;
  address?: string; phone?: string; email?: string; categories?: string;
  documentCount: number; bankAccountCount: number;
}

export interface PartyAddress { kind: number; country?: string; postalCode?: string; region?: string; city?: string; line?: string; }
export interface PartyContact { kind: number; value: string; isPrimary: boolean; note?: string; }
export interface PartyPerson { fullName: string; positionName?: string; phone?: string; email?: string; isSignatory: boolean; note?: string; }

export interface BankAccount {
  id: number; iban: string; bankId?: number; mfo?: string; bankName?: string; swiftBic?: string;
  currency: string; purpose?: string; accountHolderName?: string; isPrimary: boolean; closedAt?: string; note?: string;
  /** Остання зміна рахунку з журналу (UTC) і хто її зробив */
  lastChangedAt?: string; lastChangedBy?: string;
}

export interface PartyDetail {
  id: number; kind: number; fullName: string; shortName?: string; code?: string; legalFormId?: number;
  parentPartyId?: number; isActive: boolean; note?: string; documentCount: number;
  categoryIds: number[]; addresses: PartyAddress[]; contacts: PartyContact[]; persons: PartyPerson[];
  bankAccounts: BankAccount[];
}

export interface Bank {
  id: number; mfo: string; name: string; shortName?: string; edrpou?: string; swiftBic?: string;
  isActive: boolean; accountCount: number;
}

export const ADDRESS_KINDS = [
  { value: 1, label: 'Юридична' },
  { value: 2, label: 'Фактична' },
  { value: 3, label: 'Поштова' },
];

export const CONTACT_KINDS = [
  { value: 1, label: 'Телефон' },
  { value: 2, label: 'Email' },
  { value: 3, label: 'Сайт' },
  { value: 4, label: 'Обмін документами (СЕВ ОВВ тощо)' },
];

export const KIND_COLORS: Record<number, string> = { 1: 'blue', 2: 'purple', 3: 'green', 4: 'gold' };

/** Час з сервера — UTC (SYSUTCDATETIME) без позначки зони. */
export const fromUtc = (s: string) => dayjs(/[zZ]|[+-]dd:dd$/.test(s) ? s : `${s}Z`);
