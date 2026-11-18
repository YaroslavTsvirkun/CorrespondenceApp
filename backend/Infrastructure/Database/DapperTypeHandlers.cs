using System.Data;
using Dapper;

namespace CorrespondenceApi.Infrastructure.Database;

/// <summary>Підтримка DateOnly у Dapper (колонки SQL типу <c>date</c>).</summary>
public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }

    public override DateOnly Parse(object value) => value switch
    {
        DateOnly d => d,
        DateTime dt => DateOnly.FromDateTime(dt),
        _ => DateOnly.FromDateTime(Convert.ToDateTime(value)),
    };
}

public static class DapperConfig
{
    private static int _configured;

    public static void Configure()
    {
        if (Interlocked.Exchange(ref _configured, 1) == 1) return;
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
    }
}
