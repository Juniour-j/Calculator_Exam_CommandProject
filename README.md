# Лабораторна робота №1 — Модульне тестування ПЗ

**Дисципліна:** Автоматизовані системи тестування програмного продукту
**Варіант 2:** бібліотека `AnalaizerClassLibrary`, метод `CreateStack()`
**Виконав:** Викович Назарій, група ПМ-33

## Що тестується

Метод `AnalaizerClass.CreateStack()` перетворює вираз зі статичного поля
`AnalaizerClass.expression` у зворотний польський запис (ЗПЗ) і повертає його
у вигляді `System.Collections.ArrayList`.

## Структура репозиторію

```
Calculator/
├── AnalizerClassLibrary/            # бібліотека, що тестується (метод CreateStack)
├── CalcClassBr/                     # бібліотека математичних функцій
├── ErrorLibrary/                    # коди помилок
├── GraphInterface/                  # графічний інтерфейс калькулятора
├── AnalaizerClassLibrary.Tests/     # <-- проєкт модульних тестів (MSTest)
│   ├── CreateStackTests.cs          # тести + константа CONNECTION_STRING
│   ├── App.config
│   └── AnalaizerClassLibrary.Tests.csproj
└── Database/
    └── CreateTestDatabase.sql       # створення БД CreateStackTestDB з тестовими наборами
```

## Як запустити тести

1. **Створити базу даних з тестовими даними.**
   Відкрити `Database/CreateTestDatabase.sql` у SQL Server Management Studio
   або у Visual Studio (*View → SQL Server Object Explorer → New Query*),
   підключитися до `(localdb)\MSSQLLocalDB` і виконати скрипт (F5).
   У результаті створюється БД `CreateStackTestDB` з таблицями
   `dbo.RpnTestCases` (35 наборів) і `dbo.ExceptionTestCases` (4 набори).

2. **Додати проєкт тестів у розв'язок.**
   Скопіювати теку `AnalaizerClassLibrary.Tests` поруч з іншими проєктами,
   далі у Visual Studio: *Solution → Add → Existing Project…* і вибрати
   `AnalaizerClassLibrary.Tests.csproj`.
   NuGet-пакети `MSTest.TestAdapter` і `MSTest.TestFramework` (2.2.10)
   відновлюються автоматично під час першої збірки.

3. **Перевірити рядок з'єднання** — константа `CONNECTION_STRING` на початку
   файлу `CreateStackTests.cs`. Вона підставляється в атрибут:

   ```csharp
   [DataSource("System.Data.SqlClient", CONNECTION_STRING,
               "RpnTestCases", DataAccessMethod.Sequential)]
   ```

   Якщо база підключається як файл `.mdf`, замінити на
   `Data Source=(localdb)\MSSQLLocalDB;AttachDbFilename=<повний шлях>;Integrated Security=True`
   (у шляху не повинно бути подвійних пробілів — інакше з'єднання не встановлюється).

4. **Запустити тести:** *Test → Test Explorer → Run All Tests* (`Ctrl+R, A`).

5. **Покриття коду:** *Test → Analyze Code Coverage for All Tests*
   (доступно у Visual Studio Enterprise) або розширення Fine Code Coverage.

## Склад тестів

| Тест-метод | Джерело даних | К-сть наборів |
|---|---|---|
| `CreateStack_ExpressionFromDatabase_ReturnsExpectedRpn` | `RpnTestCases` | 35 |
| `CreateStack_ExpressionFromDatabase_ReturnsExpectedTokenCount` | `RpnTestCases` | 35 |
| `CreateStack_DoesNotModifyInputExpression` | `RpnTestCases` | 35 |
| `CreateStack_CalledTwice_ReturnsSameResult` | `RpnTestCases` | 35 |
| `CreateStack_InvalidExpression_ThrowsExpectedException` | `ExceptionTestCases` | 4 |
| `CreateStack_ExtraClosingBracket_ThrowsInvalidOperationException` | — (`[ExpectedException]`) | 1 |
| `CreateStack_NullExpression_ThrowsNullReferenceException` | — (`[ExpectedException]`) | 1 |
| `CreateStack_EmptyExpression_ReturnsEmptyArrayList` | — | 1 |
| `CreateStack_MoreThanMaxOperands_DoesNotLimitResult` | — | 1 |
| `CreateStack_ModAfterMult_UsesHigherPriorityForMod_Defect` | — | 1 |
| `CreateStack_UnclosedBracket_LeavesBracketInResult_Defect` | — | 1 |
| `CreateStack_MaxCountOperands_CompletesFast` | — | 1 |

Разом: **12 тест-методів, 151 запуск**.

## Виявлені дефекти

| № | Дефект | Приклад |
|---|---|---|
| Д1 | Пріоритет `%` (3) вищий за `*` і `/` (2), хоча математично вони рівні | `6*4%5` → `6 4 5 % *` замість `6 4 * 5 %` |
| Д2 | Незакрита дужка потрапляє у результуючий ЗПЗ | `(2+3` → `2 3 + (` |
| Д3 | Пробіли стають окремими токенами ЗПЗ | `2 + 3` → `2 " " " " 3 +` |
| Д4 | Закрита дужка на початку виразу потрапляє у результат | `)2` → `2 )` |
| Д5 | Зайва закрита дужка спричиняє `InvalidOperationException` замість коду помилки Error 01 | `2+3)` |
| Д6 | `expression = null` спричиняє `NullReferenceException` | `null` |
| Д7 | Гілка `default: return 5` у `GetPriority` недосяжна (мертвий код) | — |

Дефекти Д2–Д6 не виявляються під час звичайної роботи калькулятора, оскільки
метод `Estimate()` викликає `CreateStack()` лише після `CheckCurrency()` і
`Format()`. Проте сам по собі метод `CreateStack()` є публічним і не захищений
від некоректних вхідних даних.
