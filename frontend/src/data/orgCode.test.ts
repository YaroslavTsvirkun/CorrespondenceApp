import { describe, expect, it } from 'vitest';
import { ORG_CODE_MAX, baseOrgCode, generateOrgCode, transliterate } from './orgCode';

describe('transliterate', () => {
  it('за офіційними правилами, з урахуванням початку слова', () => {
    expect(transliterate('Промінь')).toBe('PROMIN');
    expect(transliterate('Південь')).toBe('PIVDEN');
    expect(transliterate('Юрій')).toBe('YURII');
    expect(transliterate('Згорани')).toBe('ZGHORANY');
    expect(transliterate("п'ять")).toBe('PIAT');
  });
});

describe('baseOrgCode', () => {
  it('бере назву в лапках', () => {
    expect(baseOrgCode('Підприємство «Промінь»')).toBe('PROMIN');
    expect(baseOrgCode('Філія «Промінь-Південь»')).toBe('PROMIN-PIVDEN');
    expect(baseOrgCode('ТОВ "Вектор"')).toBe('VEKTOR');
  });

  it('інакше — абревіатура значущих слів', () => {
    expect(baseOrgCode('Міністерство оборони України')).toBe('MOU');
    expect(baseOrgCode('Генеральний штаб Збройних Сил України')).toBe('HSHZSU');
    expect(baseOrgCode('Управління з питань освіти та науки')).toBe('UPON');
    expect(baseOrgCode('Військова частина 1234')).toBe('VCH1234');
  });

  it('одне слово — транслітерація', () => {
    expect(baseOrgCode('Укртелеком')).toBe('UKRTELEKOM');
  });

  it('порожня назва — порожній код, довгий обрізається', () => {
    expect(baseOrgCode('   ')).toBe('');
    expect(baseOrgCode('«' + 'Дуже довга назва організації без кінця'.repeat(2) + '»').length)
      .toBeLessThanOrEqual(ORG_CODE_MAX);
  });
});

describe('generateOrgCode', () => {
  it('додає суфікс, якщо код зайнятий (без урахування регістру)', () => {
    expect(generateOrgCode('ТОВ «Вектор»', ['vektor'])).toBe('VEKTOR-2');
    expect(generateOrgCode('ТОВ «Вектор»', ['VEKTOR', 'VEKTOR-2', null])).toBe('VEKTOR-3');
    expect(generateOrgCode('ТОВ «Вектор»', ['PROMIN'])).toBe('VEKTOR');
  });
});
