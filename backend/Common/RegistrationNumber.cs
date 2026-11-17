using System.Text.RegularExpressions;

namespace CorrespondenceApi.Common;

// Формування реєстраційного номера за патерном організації.
// Токени (без урахування регістру):
//   {seq} / {seq:000} — порядковий номер (лічильник за типом+роком), з доповненням нулями
//   {year} — рік (4 цифри),  {yy} — рік (2 цифри)
//   {type} — код типу документа,  {dir} — код напряму
public static class RegistrationNumberFormatter
{
    private static readonly Regex Token = new(
        @"\{(?<seq>seq)(?::(?<pad>0+))?\}|\{(?<year>year)\}|\{(?<yy>yy)\}|\{(?<type>type)\}|\{(?<dir>dir)\}",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public const string Default = "{type}-{seq:000}/{year}";

    public static string Format(string? pattern, int seq, int year, string? typeCode, string? dirCode)
    {
        if (string.IsNullOrWhiteSpace(pattern)) pattern = Default;
        return Token.Replace(pattern, m =>
        {
            if (m.Groups["seq"].Success)
            {
                var pad = m.Groups["pad"].Value;
                return pad.Length > 0 ? seq.ToString(new string('0', pad.Length)) : seq.ToString();
            }
            if (m.Groups["year"].Success) return year.ToString();
            if (m.Groups["yy"].Success) return (year % 100).ToString("00");
            if (m.Groups["type"].Success) return typeCode ?? "";
            if (m.Groups["dir"].Success) return dirCode ?? "";
            return m.Value;
        });
    }
}
