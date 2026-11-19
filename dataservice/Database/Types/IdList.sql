/* Табличний тип: список цілочисельних ідентифікаторів (напр. Id ролей користувача).
   Для типів немає CREATE OR ALTER — тип створюється лише якщо його ще немає.
   Щоб змінити визначення: видалити залежні процедури, DROP TYPE, перезапустити деплой. */
IF TYPE_ID(N'dbo.IdList') IS NULL
    CREATE TYPE dbo.IdList AS TABLE
    (
        Id int NOT NULL PRIMARY KEY
    );
GO
