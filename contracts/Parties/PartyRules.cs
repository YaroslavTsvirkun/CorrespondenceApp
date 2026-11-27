using System.Text.RegularExpressions;

namespace CorrespondenceSystem.Contracts.Parties;

/// <summary>Види контрагента (dbo.Party.Kind).</summary>
public static class PartyKind
{
    public const byte LegalEntity = 1;   // юридична особа
    public const byte Entrepreneur = 2;  // фізична особа-підприємець
    public const byte Individual = 3;    // фізична особа
    public const byte StateBody = 4;     // орган влади / державна установа

    public static bool IsValid(byte kind) => kind is >= LegalEntity and <= StateBody;

    /// <summary>Юридичні особи й органи влади мають код ЄДРПОУ, ФОП і фізичні особи — РНОКПП.</summary>
    public static bool HasEdrpou(byte kind) => kind is LegalEntity or StateBody;
}

/// <summary>
/// Перевірка реквізитів контрагента: ЄДРПОУ, РНОКПП, IBAN. Ті самі правила — у frontend/src/data/partyCodes.ts.
/// </summary>
public static partial class PartyRules
{
    /// <summary>Код ЄДРПОУ: 8 цифр, остання — контрольна.</summary>
    public static bool IsValidEdrpou(string? code)
    {
        if (code is null || !EightDigits().IsMatch(code)) return false;
        var d = code.Select(c => c - '0').ToArray();
        var n = int.Parse(code);
        int[] w = n is < 30000000 or > 60000000 ? [1, 2, 3, 4, 5, 6, 7] : [7, 1, 2, 3, 4, 5, 6];
        var check = Weighted(d, w) % 11;
        if (check == 10)
        {
            check = Weighted(d, w.Select(x => x + 2).ToArray()) % 11;
            if (check == 10) check = 0;
        }
        return check == d[7];
    }

    /// <summary>РНОКПП (ідентифікаційний номер фізичної особи): 10 цифр, остання — контрольна.</summary>
    public static bool IsValidRnokpp(string? code)
    {
        if (code is null || !TenDigits().IsMatch(code)) return false;
        var d = code.Select(c => c - '0').ToArray();
        var sum = Weighted(d, [-1, 5, 7, 9, 4, 6, 10, 5, 7]);
        return ((sum % 11 + 11) % 11) % 10 == d[9];
    }

    /// <summary>Код контрагента відповідно до виду (порожній код допустимий).</summary>
    public static bool IsValidCode(byte kind, string? code) =>
        string.IsNullOrEmpty(code) || (PartyKind.HasEdrpou(kind) ? IsValidEdrpou(code) : IsValidRnokpp(code));

    /// <summary>IBAN без пробілів, великими літерами.</summary>
    public static string NormalizeIban(string? iban) =>
        Whitespace().Replace(iban ?? "", "").ToUpperInvariant();

    /// <summary>IBAN: формат, довжина для України (29 символів) і контрольна сума mod-97 (ISO 13616).</summary>
    public static bool IsValidIban(string? value)
    {
        var iban = NormalizeIban(value);
        if (!IbanFormat().IsMatch(iban)) return false;
        if (iban.StartsWith("UA", StringComparison.Ordinal) && !UkrainianIban().IsMatch(iban)) return false;

        var remainder = 0;
        foreach (var ch in iban[4..] + iban[..4])
        {
            var digits = ch is >= 'A' and <= 'Z' ? (ch - 'A' + 10).ToString() : ch.ToString();
            foreach (var c in digits) remainder = (remainder * 10 + (c - '0')) % 97;
        }
        return remainder == 1;
    }

    /// <summary>МФО банку з українського IBAN (символи 5–10) або null.</summary>
    public static string? MfoFromIban(string? value)
    {
        var iban = NormalizeIban(value);
        return UkrainianIban().IsMatch(iban) ? iban[4..10] : null;
    }

    private static int Weighted(int[] d, int[] w)
    {
        var sum = 0;
        for (var i = 0; i < w.Length; i++) sum += d[i] * w[i];
        return sum;
    }

    [GeneratedRegex(@"^\d{8}$")] private static partial Regex EightDigits();
    [GeneratedRegex(@"^\d{10}$")] private static partial Regex TenDigits();
    [GeneratedRegex(@"\s+")] private static partial Regex Whitespace();
    [GeneratedRegex("^[A-Z]{2}[0-9]{2}[A-Z0-9]{11,30}$")] private static partial Regex IbanFormat();
    [GeneratedRegex("^UA[0-9]{27}$")] private static partial Regex UkrainianIban();
    [GeneratedRegex("^[A-Z]{6}[A-Z0-9]{2}([A-Z0-9]{3})?$")] public static partial Regex SwiftBic();
    [GeneratedRegex("^[A-Z]{3}$")] public static partial Regex Currency();
    [GeneratedRegex(@"^\d{6}$")] public static partial Regex Mfo();
}
