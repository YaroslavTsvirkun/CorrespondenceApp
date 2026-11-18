/* Скалярна функція: формування реєстраційного номера за патерном організації.
   Токени (без урахування регістру):
     {seq} / {seq:000} — порядковий номер з доповненням нулями до довжини маски
     {year} — рік (4 цифри), {yy} — рік (2 цифри)
     {type} — код типу документа, {dir} — код напряму
   Приклад: dbo.fn_FormatRegistrationNumber(N'{type}-{seq:000}/{year}', 7, 2026, N'LST', N'IN') -> LST-007/2026 */
CREATE OR ALTER FUNCTION dbo.fn_FormatRegistrationNumber
(
    @Pattern  nvarchar(100),
    @Seq      int,
    @Year     int,
    @TypeCode nvarchar(20),
    @DirCode  nvarchar(10)
)
RETURNS nvarchar(200)
AS
BEGIN
    DECLARE @r nvarchar(400) = ISNULL(NULLIF(LTRIM(RTRIM(@Pattern)), N''), N'{type}-{seq:000}/{year}');
    DECLARE @seqText nvarchar(20) = CAST(@Seq AS nvarchar(20));
    DECLARE @pos int = 1, @start int, @end int, @pad int, @mask nvarchar(400), @value nvarchar(40);

    -- {seq:000...} — маска з нулів задає мінімальну довжину
    WHILE 1 = 1
    BEGIN
        SET @start = CHARINDEX(N'{seq:', @r COLLATE Latin1_General_CI_AS, @pos);
        IF @start = 0 BREAK;
        SET @end = CHARINDEX(N'}', @r, @start);
        IF @end = 0 BREAK;

        SET @pad = @end - @start - 5;
        SET @mask = SUBSTRING(@r, @start + 5, @pad);
        IF @pad BETWEEN 1 AND 20 AND REPLACE(@mask, N'0', N'') = N''
        BEGIN
            SET @value = CASE WHEN LEN(@seqText) >= @pad THEN @seqText
                              ELSE RIGHT(REPLICATE(N'0', @pad) + @seqText, @pad) END;
            SET @r = STUFF(@r, @start, @end - @start + 1, @value);
            SET @pos = @start + LEN(@value);
        END
        ELSE
            SET @pos = @start + 1;   -- некоректна маска — лишаємо як є
    END;

    SET @r = REPLACE(@r COLLATE Latin1_General_CI_AS, N'{seq}',  @seqText);
    SET @r = REPLACE(@r COLLATE Latin1_General_CI_AS, N'{year}', CAST(@Year AS nvarchar(10)));
    SET @r = REPLACE(@r COLLATE Latin1_General_CI_AS, N'{yy}',   RIGHT(N'0' + CAST(@Year % 100 AS nvarchar(2)), 2));
    SET @r = REPLACE(@r COLLATE Latin1_General_CI_AS, N'{type}', ISNULL(@TypeCode, N''));
    SET @r = REPLACE(@r COLLATE Latin1_General_CI_AS, N'{dir}',  ISNULL(@DirCode, N''));

    RETURN @r;
END;
GO
