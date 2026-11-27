/**
 * Автоматичний код (скорочення) організації з її назви — латиницею, у стилі PROMIN / VEKTOR.
 *
 *  - Є назва в лапках («Промінь», "Вектор") → транслітерація лапкової частини: PROMIN, PROMIN-PIVDEN.
 *  - Інакше кілька слів → абревіатура з перших літер значущих слів: «Міністерство оборони України» → MOU.
 *  - Одне слово → його транслітерація.
 *
 * Якщо такий код уже зайнятий, додається суфікс -2, -3, …
 */

export const ORG_CODE_MAX = 30;

// Офіційна транслітерація (постанова КМУ № 55 від 27.01.2010), лише верхній регістр.
// [на початку слова, в інших позиціях]
const TRANSLIT: Record<string, [string, string]> = {
  А: ['A', 'A'], Б: ['B', 'B'], В: ['V', 'V'], Г: ['H', 'H'], Ґ: ['G', 'G'], Д: ['D', 'D'],
  Е: ['E', 'E'], Є: ['YE', 'IE'], Ж: ['ZH', 'ZH'], З: ['Z', 'Z'], И: ['Y', 'Y'], І: ['I', 'I'],
  Ї: ['YI', 'I'], Й: ['Y', 'I'], К: ['K', 'K'], Л: ['L', 'L'], М: ['M', 'M'], Н: ['N', 'N'],
  О: ['O', 'O'], П: ['P', 'P'], Р: ['R', 'R'], С: ['S', 'S'], Т: ['T', 'T'], У: ['U', 'U'],
  Ф: ['F', 'F'], Х: ['KH', 'KH'], Ц: ['TS', 'TS'], Ч: ['CH', 'CH'], Ш: ['SH', 'SH'],
  Щ: ['SHCH', 'SHCH'], Ь: ['', ''], Ю: ['YU', 'IU'], Я: ['YA', 'IA'],
};

// Службові слова, що не дають літеру в абревіатуру
const STOP_WORDS = new Set(['І', 'Й', 'ТА', 'А', 'В', 'У', 'З', 'ІЗ', 'ЗІ', 'ДО', 'НА', 'ПО', 'ДЛЯ', 'ВІД', 'ОБ', 'ПРИ', 'ЗА', 'О', 'ПРО']);

/** Транслітерація одного слова (лише літери/цифри; апостроф пропускається). */
export function transliterate(word: string): string {
  const w = word.toUpperCase().replace(/['’ʼ`]/g, '');
  let out = '';
  for (let i = 0; i < w.length; i++) {
    const ch = w[i];
    // «ЗГ» передається як ZGH, щоб не плутати з «Ж»
    if (ch === 'Г' && w[i - 1] === 'З') { out += 'GH'; continue; }
    const t = TRANSLIT[ch];
    if (t) out += i === 0 ? t[0] : t[1];
    else if (/[A-Z0-9]/.test(ch)) out += ch;
  }
  return out;
}

const words = (s: string) => s.split(/[^\p{L}\p{N}'’ʼ`]+/u).filter(Boolean);

/** Базовий код з назви, без перевірки унікальності. */
export function baseOrgCode(name: string): string {
  const quoted = /[«"“„]([^»"”“]+)[»"”“]/.exec(name)?.[1];
  let code: string;
  if (quoted?.trim()) {
    code = words(quoted).map(transliterate).filter(Boolean).join('-');
  } else {
    const ws = words(name);
    const significant = ws.filter((w) => !STOP_WORDS.has(w.toUpperCase()));
    code = significant.length > 1
      // Абревіатура: перша літера кожного значущого слова, числа — повністю
      ? significant.map((w) => (/^\p{N}+$/u.test(w) ? w : transliterate(w[0]))).join('')
      : transliterate(ws[0] ?? '');
  }
  return code.slice(0, ORG_CODE_MAX).replace(/-+$/, '');
}

/** Унікальний код: базовий, а за збігу з наявними — з суфіксом -2, -3, … */
export function generateOrgCode(name: string, taken: Iterable<string | null | undefined> = []): string {
  const base = baseOrgCode(name);
  if (!base) return '';
  const used = new Set<string>();
  for (const c of taken) if (c) used.add(c.trim().toUpperCase());
  if (!used.has(base)) return base;
  for (let n = 2; ; n++) {
    const suffix = `-${n}`;
    const candidate = base.slice(0, ORG_CODE_MAX - suffix.length) + suffix;
    if (!used.has(candidate)) return candidate;
  }
}
