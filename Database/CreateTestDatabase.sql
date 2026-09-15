/* =====================================================================
   Лабораторна робота №1 з дисципліни
   «Автоматизовані системи тестування програмного продукту»
   Варіант 2: бібліотека AnalaizerClassLibrary, метод CreateStack

   Скрипт створює базу даних з наборами тестових даних для модульних
   тестів методу AnalaizerClass.CreateStack().

   Виконувати у SQL Server Management Studio або у Visual Studio
   (View -> SQL Server Object Explorer -> New Query) на екземплярі
   (localdb)\MSSQLLocalDB або на локальному MS SQL Server.
   ===================================================================== */

IF DB_ID(N'CreateStackTestDB') IS NULL
    CREATE DATABASE CreateStackTestDB;
GO

USE CreateStackTestDB;
GO

IF OBJECT_ID(N'dbo.RpnTestCases', N'U') IS NOT NULL DROP TABLE dbo.RpnTestCases;
IF OBJECT_ID(N'dbo.ExceptionTestCases', N'U') IS NOT NULL DROP TABLE dbo.ExceptionTestCases;
GO

/* ---------------------------------------------------------------------
   Таблиця 1. Позитивні тестові набори.
   Expression  - вхідний вираз, який записується у поле
                 AnalaizerClass.expression;
   ExpectedRpn - очікуваний зворотний польський запис; елементи
                 ArrayList, що повертає CreateStack, об'єднані
                 роздільником '|' (роздільник обрано тому, що сам
                 символ '|' не входить до алфавіту виразів калькулятора,
                 а пробіл не може бути роздільником — він сам є
                 можливим токеном, див. тест-кейс TC-34);
   Category    - група тестів (Base/Priority/Bracket/Unary/Boundary/Defect);
   Description - призначення тесту;
   CoverageNote- гілка коду, яку покриває тест.
   --------------------------------------------------------------------- */
CREATE TABLE dbo.RpnTestCases
(
    Id           INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Expression   NVARCHAR(200)  NOT NULL,
    ExpectedRpn  NVARCHAR(400)  NOT NULL,
    Category     NVARCHAR(20)   NOT NULL,
    Description  NVARCHAR(200)  NOT NULL,
    CoverageNote NVARCHAR(200)  NULL
);
GO

/* ---------------------------------------------------------------------
   Таблиця 2. Негативні тестові набори — вхідні дані, на яких метод
   завершується виключенням. Значення NULL у полі Expression означає,
   що полю AnalaizerClass.expression присвоюється null.
   --------------------------------------------------------------------- */
CREATE TABLE dbo.ExceptionTestCases
(
    Id                INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Expression        NVARCHAR(200)  NULL,
    ExpectedException NVARCHAR(100)  NOT NULL,
    Description       NVARCHAR(200)  NOT NULL
);
GO

INSERT INTO dbo.RpnTestCases (Expression, ExpectedRpn, Category, Description, CoverageNote) VALUES
(N'', N'', N'Base', N'Порожній вираз', N'тіло циклу foreach не виконується'),
(N'5', N'5', N'Base', N'Одне однозначне число', N'гілка «токен не є оператором»'),
(N'12345', N'12345', N'Base', N'Багатозначне число', N'цикл накопичення цифр у Separate'),
(N'2+3', N'2|3|+', N'Base', N'Додавання двох чисел', N'push в порожній стек'),
(N'7-4', N'7|4|-', N'Base', N'Віднімання', N'оператор «-»'),
(N'6*7', N'6|7|*', N'Base', N'Множення', N'оператор «*»'),
(N'8/2', N'8|2|/', N'Base', N'Ділення', N'оператор «/»'),
(N'9%4', N'9|4|%', N'Base', N'Остача від ділення', N'оператор «%»'),
(N'12+345', N'12|345|+', N'Base', N'Багатозначні операнди', N'IsOperator для рядка довжиною > 1'),
(N'2+3*4', N'2|3|4|*|+', N'Priority', N'Множення після додавання', N'GetPriority(c) > GetPriority(Peek) — push'),
(N'2*3+4', N'2|3|*|4|+', N'Priority', N'Додавання після множення', N'GetPriority(c) <= GetPriority(Peek) — вивантаження стека'),
(N'10-2-3', N'10|2|-|3|-', N'Priority', N'Ліва асоціативність віднімання', N'вихід з while за умовою stack.Count > 0'),
(N'100/5/2', N'100|5|/|2|/', N'Priority', N'Ліва асоціативність ділення', N'повторне вивантаження стека'),
(N'1+2+3+4+5', N'1|2|+|3|+|4|+|5|+', N'Priority', N'Ланцюг операторів однакового пріоритету', N'багаторазове вивантаження'),
(N'8%3*2', N'8|3|%|2|*', N'Priority', N'Остача перед множенням', N'пріоритет 3 проти 2'),
(N'2+3*4-5/2', N'2|3|4|*|+|5|2|/|-', N'Priority', N'Змішані пріоритети', N'комбінація push та вивантаження'),
(N'(2+3)*4', N'2|3|+|4|*', N'Bracket', N'Дужки на початку виразу', N'«(» в порожній стек, стек порожній у кінці'),
(N'2*(3+4)', N'2|3|4|+|*', N'Bracket', N'Дужки в кінці виразу', N'«(» у непорожній стек'),
(N'((1+2))', N'1|2|+', N'Bracket', N'Подвійні дужки', N'цикл вивантаження до «(» з нуля ітерацій'),
(N'((1+2)*(3+4))', N'1|2|+|3|4|+|*', N'Bracket', N'Дві групи дужок у зовнішніх дужках', N'послідовні блоки дужок'),
(N'(1+(2*(3+4)))', N'1|2|3|4|+|*|+', N'Bracket', N'Максимальна глибина вкладеності — 3', N'межове значення MAX_DEPTH_BRACKET'),
(N'(2+3)*(4-1)', N'2|3|+|4|1|-|*', N'Bracket', N'Дві незалежні групи дужок', N'два цикли вивантаження до «(»'),
(N'm5', N'5|m', N'Unary', N'Унарний мінус', N'GetPriority = 4'),
(N'p5', N'5|p', N'Unary', N'Унарний плюс', N'GetPriority = 4'),
(N'm5+3', N'5|m|3|+', N'Unary', N'Унарний мінус і додавання', N'унарний оператор вивантажується першим'),
(N'2*m3', N'2|3|m|*', N'Unary', N'Унарний мінус після бінарного оператора', N'push унарного оператора'),
(N'm(2+3)', N'2|3|+|m', N'Unary', N'Унарний мінус перед дужкою', N'унарний оператор залишається в стеку до кінця'),
(N'p2*m3', N'2|p|3|m|*', N'Unary', N'Обидва унарні оператори у виразі', N'комбінація «p» і «m»'),
(N'2147483647+1', N'2147483647|1|+', N'Boundary', N'Межові значення MAXINT', N'довгий числовий токен'),
(N'2+3+4+5+6+7+8+9+10+11+12+13+14+15+16', N'2|3|+|4|+|5|+|6|+|7|+|8|+|9|+|10|+|11|+|12|+|13|+|14|+|15|+|16|+', N'Boundary', N'15 операндів і 14 операторів (29 токенів)', N'межа MAX_COUNT_OPERANDS = 30'),
(N'abc', N'abc', N'Boundary', N'Буквений токен', N'гілка Char.IsLetter у Separate'),
(N'6*4%5', N'6|4|5|%|*', N'Defect', N'Пріоритет «%» вищий за «*» (дефект Д1)', N'GetPriority("%") = 3 > GetPriority("*") = 2'),
(N'(2+3', N'2|3|+|(', N'Defect', N'Незакрита дужка потрапляє у результат (дефект Д2)', N'залишок стека додається без перевірки'),
(N'2 + 3', N'2| | |3|+', N'Defect', N'Пробіли стають окремими токенами (дефект Д3)', N'гілка «ні цифра, ні буква» у Separate'),
(N')2', N'2|)', N'Defect', N'Вираз починається із закритої дужки (дефект Д4)', N'push «)» у порожній стек');
GO

INSERT INTO dbo.ExceptionTestCases (Expression, ExpectedException, Description) VALUES
(N'2+3)', N'InvalidOperationException', N'Зайва закрита дужка — Stack.Pop() на порожньому стеку (дефект Д5)'),
(N'(2+3)*4)', N'InvalidOperationException', N'Зайва закрита дужка в кінці складеного виразу (дефект Д5)'),
(N'2*3)', N'InvalidOperationException', N'Зайва закрита дужка після множення (дефект Д5)'),
(NULL, N'NullReferenceException', N'expression = null — звернення до input.Length у Separate (дефект Д6)');
GO

/* Контроль наповнення */
SELECT Category, COUNT(*) AS CasesCount FROM dbo.RpnTestCases GROUP BY Category;
SELECT COUNT(*) AS TotalPositive FROM dbo.RpnTestCases;      -- очікується 35
SELECT COUNT(*) AS TotalNegative FROM dbo.ExceptionTestCases; -- очікується 4
GO
