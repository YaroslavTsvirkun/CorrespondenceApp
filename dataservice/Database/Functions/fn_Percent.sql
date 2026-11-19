/* Скалярна функція: цілий відсоток частини від загального (0, якщо загальне = 0). */
CREATE OR ALTER FUNCTION dbo.fn_Percent
(
    @Part  int,
    @Total int
)
RETURNS int
AS
BEGIN
    RETURN CASE WHEN ISNULL(@Total, 0) = 0 THEN 0 ELSE ISNULL(@Part, 0) * 100 / @Total END;
END;
GO
