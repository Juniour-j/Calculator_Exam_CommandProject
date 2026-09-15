using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using AnalaizerClassLibrary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnalaizerClassLibrary.Tests
{
    /// <summary>
    /// Модульні тести методу AnalaizerClass.CreateStack().
    /// Лабораторна робота №1, варіант 2.
    ///
    /// Тестові дані зберігаються в базі даних MS SQL Server
    /// (скрипт Database\CreateTestDatabase.sql) і підключаються до
    /// тестових методів атрибутом
    /// [DataSource(постачальник, рядок з'єднання, таблиця, спосіб доступу)].
    /// </summary>
    [TestClass]
    public class CreateStackTests
    {
        /// <summary>
        /// Роздільник токенів у полі ExpectedRpn таблиці RpnTestCases.
        /// Пробіл використати неможливо: він сам є можливим токеном.
        /// </summary>
        private const char SEPARATOR = '|';

        /// <summary>
        /// Рядок з'єднання з базою даних тестових наборів.
        /// Атрибут [DataSource] вимагає константу часу компіляції, тому
        /// рядок оголошено як const.
        ///
        /// Якщо база даних створена в SQL Server Management Studio на
        /// екземплярі (localdb)\MSSQLLocalDB — залишити як є.
        ///
        /// Якщо база даних підключається як файл .mdf (варіант із
        /// прикладів до лекції), використати натомість:
        ///
        /// @"Data Source=(LocalDB)\MSSQLLocalDB;" +
        /// @"AttachDbFilename=|DataDirectory|CreateStackTestDB.mdf;" +
        /// @"Integrated Security=True;Connect Timeout=30"
        ///
        /// Псевдозмінна |DataDirectory| розкривається у теку, з якої
        /// запускаються тести (bin\Debug), тому файл .mdf має бути доданий
        /// до проєкту з властивістю «Copy to Output Directory = Copy always».
        /// Абсолютний шлях теж припустимий, але тоді в ньому не повинно
        /// бути подвійних пробілів — інакше з'єднання не встановлюється.
        /// </summary>
        private const string CONNECTION_STRING =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=CreateStackTestDB;Integrated Security=True";

        /// <summary>
        /// Контекст тесту. Через властивість DataRow надає доступ
        /// до чергового рядка таблиці з тестовими даними.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>
        /// Приведення системи у відоме стан перед кожним тестом:
        /// метод CreateStack працює зі статичним полем expression,
        /// тому залишки попереднього тесту мають бути скинуті.
        /// </summary>
        [TestInitialize]
        public void SetUp()
        {
            AnalaizerClass.expression = string.Empty;
            AnalaizerClass.ShowMessage = false;   // щоб тести не відкривали MessageBox
        }

        [TestCleanup]
        public void TearDown()
        {
            AnalaizerClass.expression = string.Empty;
        }

        /// <summary>
        /// Перетворює ArrayList, який повертає CreateStack,
        /// у рядок токенів, розділених символом '|'.
        /// </summary>
        private static string Join(ArrayList list)
        {
            Assert.IsNotNull(list, "CreateStack повернув null замість ArrayList");

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                Assert.IsInstanceOfType(list[i], typeof(string),
                    string.Format("Елемент {0} масиву ЗПЗ не є рядком", i));

                if (i > 0) sb.Append(SEPARATOR);
                sb.Append((string)list[i]);
            }
            return sb.ToString();
        }

        // =================================================================
        // 1. Позитивні тести, керовані даними з таблиці dbo.RpnTestCases
        // =================================================================

        /// <summary>
        /// Основний тест: для кожного вхідного виразу з бази даних метод
        /// CreateStack має повернути очікуваний зворотний польський запис.
        /// </summary>
        [TestMethod]
        [TestCategory("CreateStack")]
        [DataSource("System.Data.SqlClient", CONNECTION_STRING, "RpnTestCases", DataAccessMethod.Sequential)]
        public void CreateStack_ExpressionFromDatabase_ReturnsExpectedRpn()
        {
            // Arrange
            string expression = TestContext.DataRow["Expression"].ToString();
            string expected = TestContext.DataRow["ExpectedRpn"].ToString();
            string description = TestContext.DataRow["Description"].ToString();

            AnalaizerClass.expression = expression;

            // Act
            ArrayList actualList = AnalaizerClass.CreateStack();
            string actual = Join(actualList);

            // Assert
            Assert.AreEqual(expected, actual,
                string.Format("Тест-кейс №{0} ({1}). Вираз: '{2}'. Очікувалось: '{3}', отримано: '{4}'",
                              TestContext.DataRow["Id"], description, expression, expected, actual));
        }

        /// <summary>
        /// Кількість елементів результату має дорівнювати кількості
        /// токенів в очікуваному записі (перевірка на «зайві» елементи,
        /// які не помітно під час порівняння рядків).
        /// </summary>
        [TestMethod]
        [TestCategory("CreateStack")]
        [DataSource("System.Data.SqlClient", CONNECTION_STRING, "RpnTestCases", DataAccessMethod.Sequential)]
        public void CreateStack_ExpressionFromDatabase_ReturnsExpectedTokenCount()
        {
            string expression = TestContext.DataRow["Expression"].ToString();
            string expected = TestContext.DataRow["ExpectedRpn"].ToString();

            int expectedCount = expected.Length == 0
                ? 0
                : expected.Split(SEPARATOR).Length;

            AnalaizerClass.expression = expression;

            ArrayList actual = AnalaizerClass.CreateStack();

            Assert.AreEqual(expectedCount, actual.Count,
                string.Format("Невірна кількість елементів ЗПЗ для виразу '{0}'", expression));
        }

        /// <summary>
        /// Метод не повинен змінювати вхідне поле expression —
        /// він лише читає його (перевірка відсутності побічних ефектів).
        /// </summary>
        [TestMethod]
        [TestCategory("CreateStack")]
        [DataSource("System.Data.SqlClient", CONNECTION_STRING, "RpnTestCases", DataAccessMethod.Sequential)]
        public void CreateStack_DoesNotModifyInputExpression()
        {
            string expression = TestContext.DataRow["Expression"].ToString();
            AnalaizerClass.expression = expression;

            AnalaizerClass.CreateStack();

            Assert.AreEqual(expression, AnalaizerClass.expression,
                "Метод CreateStack змінив значення поля expression");
        }

        /// <summary>
        /// Повторний виклик методу на тих самих даних має давати
        /// той самий результат (детермінованість, відсутність
        /// накопичення стану між викликами).
        /// </summary>
        [TestMethod]
        [TestCategory("CreateStack")]
        [DataSource("System.Data.SqlClient", CONNECTION_STRING, "RpnTestCases", DataAccessMethod.Sequential)]
        public void CreateStack_CalledTwice_ReturnsSameResult()
        {
            AnalaizerClass.expression = TestContext.DataRow["Expression"].ToString();

            string first = Join(AnalaizerClass.CreateStack());
            string second = Join(AnalaizerClass.CreateStack());

            Assert.AreEqual(first, second,
                "Два послідовні виклики CreateStack дали різний результат");
        }

        // =================================================================
        // 2. Негативні тести, керовані даними з dbo.ExceptionTestCases
        // =================================================================

        /// <summary>
        /// Для некоректних вхідних даних метод має завершитись
        /// виключенням очікуваного типу. Тест документує реальну
        /// поведінку методу: власної обробки помилок CreateStack не має.
        /// </summary>
        [TestMethod]
        [TestCategory("CreateStack.Negative")]
        [DataSource("System.Data.SqlClient", CONNECTION_STRING, "ExceptionTestCases", DataAccessMethod.Sequential)]
        public void CreateStack_InvalidExpression_ThrowsExpectedException()
        {
            // Arrange
            object raw = TestContext.DataRow["Expression"];
            string expression = raw == DBNull.Value ? null : raw.ToString();
            string expectedException = TestContext.DataRow["ExpectedException"].ToString();
            string description = TestContext.DataRow["Description"].ToString();

            AnalaizerClass.expression = expression;

            // Act
            try
            {
                ArrayList result = AnalaizerClass.CreateStack();

                // Assert (виключення не сталося)
                Assert.Fail(string.Format(
                    "Очікувалось виключення {0} для виразу '{1}' ({2}), але метод повернув {3} елементів",
                    expectedException, expression ?? "null", description, result.Count));
            }
            catch (AssertFailedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Assert (перевірка типу виключення)
                Assert.AreEqual(expectedException, ex.GetType().Name,
                    string.Format("Невірний тип виключення для виразу '{0}' ({1})",
                                  expression ?? "null", description));
            }
        }

        /// <summary>
        /// Той самий негативний випадок, записаний коротшою формою —
        /// через атрибут [ExpectedException]. Тест вважається пройденим,
        /// якщо метод згенерував виключення саме вказаного типу.
        /// </summary>
        [TestMethod]
        [TestCategory("CreateStack.Negative")]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CreateStack_ExtraClosingBracket_ThrowsInvalidOperationException()
        {
            // Arrange
            AnalaizerClass.expression = "2+3)";

            // Act — виключення очікується всередині методу
            AnalaizerClass.CreateStack();

            // Assert виконує атрибут [ExpectedException]
        }

        /// <summary>
        /// Неініціалізоване поле expression призводить до
        /// NullReferenceException у методі Separate().
        /// </summary>
        [TestMethod]
        [TestCategory("CreateStack.Negative")]
        [ExpectedException(typeof(NullReferenceException))]
        public void CreateStack_NullExpression_ThrowsNullReferenceException()
        {
            AnalaizerClass.expression = null;

            AnalaizerClass.CreateStack();
        }

        // =================================================================
        // 3. Тести, що не потребують зовнішніх даних
        // =================================================================

        /// <summary>
        /// Метод завжди повертає ініціалізований об'єкт ArrayList,
        /// навіть для порожнього виразу.
        /// </summary>
        [TestMethod]
        [TestCategory("CreateStack")]
        public void CreateStack_EmptyExpression_ReturnsEmptyArrayList()
        {
            AnalaizerClass.expression = string.Empty;

            ArrayList result = AnalaizerClass.CreateStack();

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        /// <summary>
        /// Обмеження MAX_COUNT_OPERANDS = 30 у методі CreateStack
        /// не перевіряється (воно реалізоване лише в RunEstimate).
        /// Тест фіксує цю поведінку: вираз із 41 токена обробляється
        /// без помилки.
        /// </summary>
        [TestMethod]
        [TestCategory("CreateStack")]
        public void CreateStack_MoreThanMaxOperands_DoesNotLimitResult()
        {
            StringBuilder sb = new StringBuilder("1");
            for (int i = 2; i <= 21; i++)
                sb.Append("+").Append(i);          // 21 операнд + 20 операторів = 41 токен

            AnalaizerClass.expression = sb.ToString();

            ArrayList result = AnalaizerClass.CreateStack();

            Assert.AreEqual(41, result.Count,
                "CreateStack не повинен обмежувати кількість операндів — це задача RunEstimate");
        }

        /// <summary>
        /// Дефект Д1: пріоритет операції знаходження остачі «%» (3)
        /// вищий за пріоритет множення «*» (2), хоча з погляду
        /// математики вони рівні й обчислюються зліва направо.
        /// Тест документує фактичну (помилкову) поведінку методу.
        /// </summary>
        [TestMethod]
        [TestCategory("CreateStack.Defect")]
        public void CreateStack_ModAfterMult_UsesHigherPriorityForMod_Defect()
        {
            AnalaizerClass.expression = "6*4%5";

            string actual = Join(AnalaizerClass.CreateStack());

            // математично коректним був би запис "6|4|*|5|%"  ((6*4)%5 = 4)
            Assert.AreEqual("6|4|5|%|*", actual,
                "Зафіксовано фактичну поведінку: '%' має вищий пріоритет, ніж '*'");
        }

        /// <summary>
        /// Дефект Д2: якщо у виразі є незакрита дужка, символ «(»
        /// потрапляє у результуючий масив зворотного польського запису.
        /// </summary>
        [TestMethod]
        [TestCategory("CreateStack.Defect")]
        public void CreateStack_UnclosedBracket_LeavesBracketInResult_Defect()
        {
            AnalaizerClass.expression = "(2+3";

            ArrayList result = AnalaizerClass.CreateStack();

            CollectionAssert.Contains(result, "(",
                "Очікувалось, що незакрита дужка залишиться в результаті (дефект Д2)");
        }

        /// <summary>
        /// Перевірка швидкодії: обробка виразу з максимально допустимою
        /// кількістю токенів має виконуватись за прийнятний час.
        /// </summary>
        [TestMethod]
        [TestCategory("CreateStack")]
        public void CreateStack_MaxCountOperands_CompletesFast()
        {
            StringBuilder sb = new StringBuilder("1");
            for (int i = 2; i <= 15; i++)
                sb.Append("*").Append(i);

            AnalaizerClass.expression = sb.ToString();

            Stopwatch sw = Stopwatch.StartNew();
            AnalaizerClass.CreateStack();
            sw.Stop();

            Assert.IsTrue(sw.ElapsedMilliseconds < 100,
                string.Format("Обробка виразу зайняла {0} мс", sw.ElapsedMilliseconds));
        }
    }
}
