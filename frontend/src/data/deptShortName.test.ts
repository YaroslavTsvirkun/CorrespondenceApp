import { describe, expect, it } from 'vitest';
import { DEPT_SHORT_NAME_MAX, baseDeptShortName, generateDeptShortName, truncateWord } from './deptShortName';

describe('truncateWord', () => {
  it('усікає перед голосною, щонайменше 3 літери', () => {
    expect(truncateWord('Канцелярія')).toBe('Канц.');
    expect(truncateWord('Дирекція')).toBe('Дир.');
    expect(truncateWord('Адміністрація')).toBe('Адм.');
    expect(truncateWord('Бухгалтерія')).toBe('Бухг.');
  });

  it('короткі слова й абревіатури — без змін', () => {
    expect(truncateWord('Штаб')).toBe('Штаб');
    expect(truncateWord('ЗСУ')).toBe('ЗСУ');
  });
});

describe('baseDeptShortName', () => {
  it('абревіатура значущих слів', () => {
    expect(baseDeptShortName('Відділ кадрів')).toBe('ВК');
    expect(baseDeptShortName('Загальний відділ')).toBe('ЗВ');
    expect(baseDeptShortName('Відділ з питань мобілізації та обліку')).toBe('ВПМО');
  });

  it('зберігає дужки й відкидає альтернативну назву після «/»', () => {
    expect(baseDeptShortName('Головне оперативне управління (J-3)')).toBe('ГОУ (J-3)');
    expect(baseDeptShortName("Головне управління зв'язку та інформаційних систем (J-6)")).toBe('ГУЗІС (J-6)');
    expect(baseDeptShortName('Головне управління розвідки / розвідувальне управління (J-2)')).toBe('ГУР (J-2)');
  });

  it('зберігає наявні абревіатури та числа', () => {
    expect(baseDeptShortName('Служба ІТ')).toBe('СІТ');
    expect(baseDeptShortName('Відділ 12')).toBe('В12');
  });

  it('одне слово — усічення; порожня назва — порожньо; довга — обрізається', () => {
    expect(baseDeptShortName('Канцелярія')).toBe('Канц.');
    expect(baseDeptShortName('  ')).toBe('');
    expect(baseDeptShortName('Слово '.repeat(60)).length).toBeLessThanOrEqual(DEPT_SHORT_NAME_MAX);
  });
});

describe('generateDeptShortName', () => {
  it('додає суфікс за збігу (без урахування регістру)', () => {
    expect(generateDeptShortName('Відділ кадрів', ['вк'])).toBe('ВК-2');
    expect(generateDeptShortName('Відділ кадрів', ['ВК', 'ВК-2', null])).toBe('ВК-3');
    expect(generateDeptShortName('Відділ кадрів', ['ЗВ'])).toBe('ВК');
  });
});
