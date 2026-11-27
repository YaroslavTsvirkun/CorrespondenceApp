import { describe, expect, it } from 'vitest';
import {
  formatIban, isValidEdrpou, isValidIban, isValidRnokpp, mfoFromIban, normalizeIban, partyCodeError,
} from './partyCodes';

// Ті самі приклади, що й у тестах C# (PartiesTests): правила мають збігатися

describe('ЄДРПОУ', () => {
  it.each([['32145678', true], ['02142632', true], ['40123454', true], ['32145679', false], ['3214567', false], ['3214567a', false]])(
    '%s → %s', (code, valid) => expect(isValidEdrpou(code)).toBe(valid));
});

describe('РНОКПП', () => {
  it.each([['3124567896', true], ['2987654320', true], ['3124567895', false], ['312456789', false]])(
    '%s → %s', (code, valid) => expect(isValidRnokpp(code)).toBe(valid));
});

describe('IBAN', () => {
  it.each([
    ['UA183052990000026007052612345', true],
    ['ua18 3052 9900 0002 6007 0526 1234 5', true],
    ['UA183052990000026007052612346', false],
    ['UA1830529900000260070526123', false],
    ['DE89370400440532013000', true],
    ['DE89370400440532013001', false],
  ])('%s → %s', (iban, valid) => expect(isValidIban(iban)).toBe(valid));

  it('нормалізує, форматує й дістає МФО', () => {
    expect(normalizeIban(' ua18 3052 99 ')).toBe('UA18305299');
    expect(formatIban('UA183052990000026007052612345')).toBe('UA18 3052 9900 0002 6007 0526 1234 5');
    expect(mfoFromIban('UA18 3052 9900 0002 6007 0526 1234 5')).toBe('305299');
    expect(mfoFromIban('DE89370400440532013000')).toBeNull();
  });
});

describe('код за видом контрагента', () => {
  it('юрособа — ЄДРПОУ, ФОП — РНОКПП, порожній код допустимий', () => {
    expect(partyCodeError(1, '32145678')).toBeNull();
    expect(partyCodeError(2, '32145678')).not.toBeNull();
    expect(partyCodeError(2, '2987654320')).toBeNull();
    expect(partyCodeError(4, undefined)).toBeNull();
  });
});
