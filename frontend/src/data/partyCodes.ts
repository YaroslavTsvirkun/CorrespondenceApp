// Перевірка реквізитів контрагента — ті самі правила, що й у contracts/Parties/PartyRules.cs.

const digits = (s: string) => [...s].map(Number);
const weighted = (d: number[], w: number[]) => w.reduce((sum, wi, i) => sum + d[i] * wi, 0);

/** Код ЄДРПОУ: 8 цифр, остання — контрольна. */
export function isValidEdrpou(code: string): boolean {
  if (!/^\d{8}$/.test(code)) return false;
  const d = digits(code);
  const n = Number(code);
  const w = n < 30000000 || n > 60000000 ? [1, 2, 3, 4, 5, 6, 7] : [7, 1, 2, 3, 4, 5, 6];
  let check = weighted(d, w) % 11;
  if (check === 10) {
    check = weighted(d, w.map((x) => x + 2)) % 11;
    if (check === 10) check = 0;
  }
  return check === d[7];
}

/** РНОКПП (ідентифікаційний номер фізичної особи): 10 цифр, остання — контрольна. */
export function isValidRnokpp(code: string): boolean {
  if (!/^\d{10}$/.test(code)) return false;
  const d = digits(code);
  const sum = weighted(d, [-1, 5, 7, 9, 4, 6, 10, 5, 7]);
  return (((sum % 11) + 11) % 11) % 10 === d[9];
}

/** IBAN без пробілів, великими літерами. */
export const normalizeIban = (iban: string) => iban.replace(/\s+/g, '').toUpperCase();

/** IBAN групами по 4 символи — для показу. */
export const formatIban = (iban: string) => normalizeIban(iban).replace(/(.{4})/g, '$1 ').trim();

/** IBAN: формат, довжина для України (29 символів) і контрольна сума mod-97 (ISO 13616). */
export function isValidIban(value: string): boolean {
  const iban = normalizeIban(value);
  if (!/^[A-Z]{2}\d{2}[A-Z0-9]{11,30}$/.test(iban)) return false;
  if (iban.startsWith('UA') && !/^UA\d{27}$/.test(iban)) return false;
  const rearranged = iban.slice(4) + iban.slice(0, 4);
  let remainder = 0;
  for (const ch of rearranged) {
    const v = ch >= 'A' && ch <= 'Z' ? String(ch.charCodeAt(0) - 55) : ch;
    for (const c of v) remainder = (remainder * 10 + Number(c)) % 97;
  }
  return remainder === 1;
}

/** МФО банку з українського IBAN (символи 5–10) або null. */
export function mfoFromIban(value: string): string | null {
  const iban = normalizeIban(value);
  return /^UA\d{8}/.test(iban) ? iban.slice(4, 10) : null;
}

/** Види контрагента (dbo.Party.Kind). */
export const PARTY_KINDS = [
  { id: 1, name: 'Юридична особа', code: 'ЄДРПОУ' },
  { id: 2, name: 'ФОП', code: 'РНОКПП' },
  { id: 3, name: 'Фізична особа', code: 'РНОКПП' },
  { id: 4, name: 'Орган влади / держустанова', code: 'ЄДРПОУ' },
] as const;

/** Перевірка коду за видом контрагента; повертає текст помилки або null. */
export function partyCodeError(kind: number | undefined, code: string | undefined): string | null {
  if (!code) return null;
  const legal = kind === 1 || kind === 4;
  if (legal) return isValidEdrpou(code) ? null : 'Невірний код ЄДРПОУ (8 цифр, контрольна сума)';
  return isValidRnokpp(code) ? null : 'Невірний РНОКПП (10 цифр, контрольна сума)';
}
