/**
 * Автоматична скорочена назва підрозділу з повної — кирилицею, у стилі «Канц.», «ВК», «ГОУ (J-3)».
 *
 *  - Кілька значущих слів → абревіатура з великих перших літер: «Відділ кадрів» → ВК.
 *  - Одне слово → усічення перед голосною (щонайменше 3 літери) з крапкою: «Канцелярія» → Канц.
 *  - Текст у дужках зберігається: «Головне оперативне управління (J-3)» → ГОУ (J-3).
 *  - Частина після «/» (альтернативна назва) відкидається.
 *  - Слова, що вже є абревіатурами (ІТ, ЗСУ) і числа, беруться повністю.
 *
 * Якщо така назва вже є в організації, додається суфікс -2, -3, …
 */

export const DEPT_SHORT_NAME_MAX = 50;

const STOP_WORDS = new Set(['і', 'й', 'та', 'а', 'в', 'у', 'з', 'із', 'зі', 'до', 'на', 'по', 'для', 'від', 'об', 'при', 'за', 'о', 'про']);
const VOWELS = /[аеєиіїоуюяaeiouy]/i;

const words = (s: string) => s.split(/[^\p{L}\p{N}'’ʼ`-]+/u).filter(Boolean);
const isAbbr = (w: string) => w.length > 1 && w === w.toUpperCase() && /\p{L}/u.test(w);

/** Усічення одного слова: «Канцелярія» → «Канц.», короткі слова — без змін. */
export function truncateWord(word: string): string {
  if (word.length <= 4 || isAbbr(word)) return word;
  for (let i = 3; i < word.length; i++) {
    if (VOWELS.test(word[i])) {
      const stem = word.slice(0, i).replace(/[ь'’ʼ`-]+$/u, '');
      return i >= word.length - 1 ? word : `${stem}.`;
    }
  }
  return word;
}

/** Базова скорочена назва, без перевірки унікальності. */
export function baseDeptShortName(name: string): string {
  const parens = [...name.matchAll(/\(([^)]+)\)/g)].map((m) => m[1].trim()).filter(Boolean);
  const main = name.replace(/\([^)]*\)/g, ' ').split('/')[0];

  const significant = words(main).filter((w) => !STOP_WORDS.has(w.toLowerCase()));
  let short: string;
  if (significant.length > 1) {
    short = significant
      .map((w) => (/^\p{N}+$/u.test(w) || isAbbr(w) ? w : w[0].toUpperCase()))
      .join('');
  } else {
    short = significant[0] ? truncateWord(significant[0]) : '';
  }

  const tail = parens.map((p) => `(${p})`).join(' ');
  short = [short, tail].filter(Boolean).join(' ');
  return short.slice(0, DEPT_SHORT_NAME_MAX).trim();
}

/** Унікальна в межах організації скорочена назва: базова, а за збігу — з суфіксом -2, -3, … */
export function generateDeptShortName(name: string, taken: Iterable<string | null | undefined> = []): string {
  const base = baseDeptShortName(name);
  if (!base) return '';
  const used = new Set<string>();
  for (const s of taken) if (s) used.add(s.trim().toLowerCase());
  if (!used.has(base.toLowerCase())) return base;
  for (let n = 2; ; n++) {
    const suffix = `-${n}`;
    const candidate = base.slice(0, DEPT_SHORT_NAME_MAX - suffix.length) + suffix;
    if (!used.has(candidate.toLowerCase())) return candidate;
  }
}
