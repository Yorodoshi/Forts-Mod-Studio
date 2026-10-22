using System;
using System.Drawing;
using System.Windows.Forms;

namespace FortsStudio {
 public partial class Studio {
  // The full guide is bundled with the application and opens without a browser or network.
  void ShowFormulaGuide() {
   using(var guideFont=new Font("Consolas",10))
   using(var dialog=new UiForm {
    Text="Гайд по формулам",BackColor=Theme.Card,ForeColor=Theme.Text,Font=Theme.TextFont(10),
    AutoScaleMode=AutoScaleMode.Dpi,ClientSize=new Size(920,760),MinimumSize=new Size(640,480),
    StartPosition=FormStartPosition.CenterParent,ShowInTaskbar=false,KeyPreview=true
   }) {
    var layout=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(14)};
    layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
    layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
    layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
    layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
    dialog.Controls.Add(layout);
    var searchRow=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=3,AutoSize=true,Padding=new Padding(0,0,0,10)};
    searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
    searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
    searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
    var searchLabel=Theme.Label("Поиск в гайде",9,true);searchLabel.Margin=new Padding(0,9,10,0);
    var search=new TextBox {Dock=DockStyle.Fill,BackColor=Theme.Input,ForeColor=Theme.Text,BorderStyle=BorderStyle.FixedSingle,Margin=new Padding(0,7,8,0)};
    search.AccessibleName=UiText.Get("Поиск в гайде");
    var content=new RichTextBox {
     Dock=DockStyle.Fill,ReadOnly=true,BackColor=Theme.Input,ForeColor=Theme.Text,
     BorderStyle=BorderStyle.None,Font=guideFont,DetectUrls=false,WordWrap=true,
     ScrollBars=RichTextBoxScrollBars.Vertical,HideSelection=false,
     Text=UiText.English?FormulaGuideEnglish:FormulaGuideRussian
    };
    content.AccessibleName=UiText.Get("Локальное руководство по формулам и переменным");
    var hint=Theme.Label("Ctrl+F — поиск · F3 / Shift+F3 — следующее / предыдущее · Esc — закрыть",9);
    hint.ForeColor=Theme.Muted;hint.MaximumSize=new Size(10000,0);
    Action<bool> find=previous=>{
     if(String.IsNullOrWhiteSpace(search.Text)) {search.Focus();return;}
     string needle=search.Text;string text=content.Text;int position;
     if(previous) {
      int before=content.SelectionStart-1;
      position=before>=0?text.LastIndexOf(needle,before,StringComparison.OrdinalIgnoreCase):-1;
      if(position<0)position=text.LastIndexOf(needle,StringComparison.OrdinalIgnoreCase);
     } else {
      int after=Math.Min(text.Length,content.SelectionStart+content.SelectionLength);
      position=text.IndexOf(needle,after,StringComparison.OrdinalIgnoreCase);
      if(position<0)position=text.IndexOf(needle,StringComparison.OrdinalIgnoreCase);
     }
     if(position<0) {hint.Text="Совпадений не найдено";return;}
     content.Select(position,needle.Length);content.ScrollToCaret();
     hint.Text="Ctrl+F — поиск · F3 / Shift+F3 — следующее / предыдущее · Esc — закрыть";
    };
    var next=Theme.Button("Найти далее",()=>find(false));next.Margin=new Padding(0);
    searchRow.Controls.Add(searchLabel,0,0);searchRow.Controls.Add(search,1,0);searchRow.Controls.Add(next,2,0);
    layout.Controls.Add(searchRow,0,0);layout.Controls.Add(content,0,1);
    var footer=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=2,AutoSize=true,Padding=new Padding(0,10,0,0)};
    footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
    var close=Theme.Button("Закрыть",()=>dialog.Close());close.Margin=new Padding(10,0,0,0);
    footer.Controls.Add(hint,0,0);footer.Controls.Add(close,1,0);layout.Controls.Add(footer,0,2);
    dialog.CancelButton=close;
    search.TextChanged+=(sender,args)=>{content.Select(0,0);};
    search.KeyDown+=(sender,args)=>{if(args.KeyCode==Keys.Enter){find(args.Shift);args.Handled=true;args.SuppressKeyPress=true;}};
    dialog.KeyDown+=(sender,args)=>{
     if(args.Control&&args.KeyCode==Keys.F){search.Focus();search.SelectAll();args.Handled=true;args.SuppressKeyPress=true;}
     else if(args.KeyCode==Keys.F3){find(args.Shift);args.Handled=true;args.SuppressKeyPress=true;}
    };
    dialog.ShowDialog(this);
   }
  }

  const string FormulaGuideRussian=@"# Переменные и формулы FMS



## С чего начать

1. Откройте «Сценарии» и создайте переменную в панели справа.
2. Задайте имя, тип и начальную формулу. Например: `power`, «Дробное», `sqrt(81) * 2`.
3. Используйте переменную в числовом поле блока: `$power + 5`.
4. Для изменения во время матча добавьте действие «Редактировать переменную», выберите переменную и операцию.
5. Откройте «Гайд по формулам» для локальной справки. В окне работают поиск, Ctrl+F и F3 / Shift+F3.

Панель справа показывает объявленные переменные и вычисленные **начальные** значения текущего проекта. Она не подключается к запущенному матчу и не показывает его живое состояние. Начальные формулы вычисляются при подготовке проекта; изменение одной переменной во время игры не пересчитывает автоматически остальные. Действие с формулой вычисляет её заново при выполнении в Forts.

## Имена и типы

Имя — от 1 до 40 латинских букв, цифр и `_`; первый символ — латинская буква или `_`. Ссылки записываются как `$power` или `power`; для ясности рекомендуем `$power`. Регистр учитывается: `power` и `Power` — разные имена. `speed`, `hp`, `damage`, `repeat_index`, `pi`, `e` и `tau` зарезервированы. Имена функций тоже лучше не использовать для своих переменных.

| Тип в редакторе | Значение и преобразование |
| --- | --- |
| Целочисленное | 32-битное знаковое число: от −2147483648 до 2147483647. Результат формулы усекается к нулю: `3.9` → `3`, `-3.9` → `-3`. Выход за диапазон вызывает ошибку. |
| Дробное | Число двойной точности. Допустимы только конечные значения; бесконечность и NaN считаются ошибками. |
| Список | До 4096 конечных чисел. Начальное значение, например `[5, 10.5, -2]` или `range(1, 12)`. Вложенные списки, строки и объекты не поддерживаются. |

Число и список не заменяют друг друга автоматически. Для получения числа из списка используйте `at`, `sum`, `mean` или `len`. Индексы начинаются с **1**, должны быть целыми и существовать в списке.

Начальная формула может ссылаться на другие объявленные переменные в любом порядке: `base = 10`, `power = $base * 3`. Цикл `a = $b`, `b = $a`, ссылка на себя и отсутствующая переменная вызывают ошибку. Эти ошибки видны в панели и при проверке проекта.

Область «Общая на весь матч» хранит одно значение для всего матча. Область «Отдельная у каждого объекта» хранит независимое значение для каждого объекта сценария. Списки присваиваются копированием: изменение одного списка не меняет его прежние копии у других переменных или объектов.

Переименование обновляет ссылки в формулах и блоках проекта. Удаление используемой переменной блокируется: сначала удалите или измените ссылки. При изменении типа проверьте совместимость её формулы и операций в блоках.

## Запись формул

Десятичный разделитель — точка: `1.25`. Поддерживается научная запись: `2.5e3`, `1e-4`. Аргументы функций и элементы списка разделяются запятыми. Углы тригонометрических функций задаются в радианах.

| Запись | Значение |
| --- | --- |
| `+`, `-`, `*`, `/` | Сложение, вычитание, умножение, деление |
| `%` | Остаток с округлением частного вниз: `-5 % 3` → `1` |
| `^` | Возведение в степень: `2 ^ 3` |
| `-x`, `+x` | Знак числа |
| `( ... )` | Явный порядок вычислений: `(2 + 3) * 4` |
| `pi`, `e`, `tau` | π, число Эйлера, 2π |
| `[1, 2, 3]` | Числовой список |

Приоритет: скобки и вызовы функций, степень, знак, умножение/деление/остаток, сложение/вычитание. Степени вычисляются справа налево: `2^3^2` → `512`. `-2^2` → `-4`, `2^-2` → `0.25`. Для сложных степеней и отрицательных оснований используйте скобки: `(-2)^2`, `-(2^2)`, `(2^3)^2`.

Формулы — ограниченный математический язык. Lua-код, команды, присваивания внутри выражения, доступ к файлам и сети не выполняются. Для изменения значения используйте отдельное действие.

## Справочник функций

### Числа и интервалы

| Функция | Результат / пример |
| --- | --- |
| `abs(x)` | Модуль: `abs(-5)` → `5` |
| `sqrt(x)`, `cbrt(x)` | Квадратный и кубический корень: `sqrt(81)` → `9`, `cbrt(-8)` → `-2` |
| `pow(x, y)` | Степень; то же назначение, что у `x ^ y` |
| `exp(x)`, `ln(x)` | e в степени x; натуральный логарифм |
| `log(x)`, `log(x, base)` | Натуральный логарифм; логарифм по указанному основанию: `log(8, 2)` → `3` |
| `log10(x)` | Десятичный логарифм |
| `floor(x)`, `ceil(x)` | Округление вниз и вверх: `floor(-1.2)` → `-2` |
| `round(x)` | До ближайшего целого, половины от нуля: `round(-2.5)` → `-3` |
| `sign(x)` | −1, 0 или 1 |
| `min(a, b, ...)`, `max(a, b, ...)` | Минимум/максимум двух и более чисел; также принимают один список: `max($values)` |
| `clamp(x, lo, hi)` | Ограничение диапазоном: `clamp(15, 0, 10)` → `10` |
| `lerp(a, b, t)` | `a + (b - a) * t`; t вне 0…1 даёт экстраполяцию |

Деление и остаток на ноль, корень из отрицательного числа, недопустимое основание логарифма или другой неконечный результат вызывают ошибку.

### Углы и тригонометрия

| Функции | Назначение |
| --- | --- |
| `sin(x)`, `cos(x)`, `tan(x)` | Тригонометрические функции, x в радианах |
| `asin(x)`, `acos(x)`, `atan(x)` | Обратные функции, результат в радианах |
| `atan2(y, x)` | Угол вектора с учётом четверти |
| `sinh(x)`, `cosh(x)`, `tanh(x)` | Гиперболические функции |
| `rad(degrees)`, `deg(radians)` | Перевод градусов в радианы и обратно |

Например: `sin(rad(30))` ≈ `0.5`, `deg(atan2(1, 1))` ≈ `45`.

### Списки

| Функция | Результат / пример |
| --- | --- |
| `len(list)` | Число элементов: `len([2, 4, 6])` → `3` |
| `at(list, index)` | Элемент по индексу: `at([2, 4, 6], 2)` → `4` |
| `sum(list)` | Сумма: `sum([2, 4, 6])` → `12`; пустой список → `0` |
| `mean(list)` | Среднее арифметическое непустого списка |
| `min(list)`, `max(list)` | Минимум/максимум непустого списка |
| `range(start, end[, step])` | Числовая последовательность с включением конца, если до него дошёл шаг; шаг не может быть нулевым |

Примеры: `range(1, 5)` → `[1, 2, 3, 4, 5]`; `range(0, 1, 0.25)` → `[0, 0.25, 0.5, 0.75, 1]`. Для убывающего ряда явно задайте отрицательный шаг: `range(5, 1, -1)`. Длиннее 4096 элементов создать нельзя.

### Численная высшая математика

`diff(expression, x, point)` — приближённая производная по локальной переменной x в заданной точке. Используется центральная разность с шагом `h = 1e-5 * max(1, abs(point))`.

Пример: `diff(x^3, x, 2)` ≈ `12`.

`integrate(expression, x, lower, upper[, steps])` — приближённый определённый интеграл по формуле Симпсона. По умолчанию 64 шага; можно задать чётное целое число от 2 до 256.

Примеры: `integrate(x^2, x, 0, 1)` ≈ `0.3333333333`; `integrate(sin(x), x, 0, pi, 128)` ≈ `2`.

Во втором аргументе пишется имя локальной переменной (`x` или `$x`). В первом аргументе она действует только внутри этого вызова и не требует объявления в проекте. Другие значения доступны как обычно: `integrate($scale * x^2, x, 0, 1)`. Вложенные вызовы допускаются в пределах лимитов вычисления.

Это численные приближения, а не символьная алгебра. Разрывы, особенности, резкие колебания и очень большие/малые масштабы могут давать неточный ответ или ошибку. Для таких задач меняйте диапазон и число шагов; не используйте результат как точное доказательство.

## Переменные события

Они доступны в формулах блоков во время выполнения и не подходят для начальных формул переменных проекта:

| Имя | Значение |
| --- | --- |
| `$speed` | Скорость из контекста текущего события |
| `$hp` | Прочность из контекста текущего события |
| `$damage` | Урон из контекста текущего события |
| `$repeat_index` | Номер текущего повтора от 1; вне цикла — 0 |

Доступность и смысл контекста зависят от события; формула не получает сведения о произвольном объекте сама. Для `$speed`, `$hp` и `$damage` используется 0, если нужного контекста или существующего объекта нет. Для игрового урона, скорости или времени выбирайте соответствующий числовой параметр блока, например `clamp($damage * 1.2, 0, 500)`.

## Изменение переменной действием

В действии «Редактировать переменную» выберите существующую переменную:

| Операция | Что делает |
| --- | --- |
| Задать значение / Заменить весь список | Заменяет значение результатом формулы. Для списка: `[10, 20, 30]` или `range(1, 12)`. |
| Прибавить значение | Прибавляет результат к числовой переменной. |
| Умножить на значение | Умножает числовую переменную на результат. |
| Добавить элемент в конец | Добавляет одно число в конец списка. |
| Изменить элемент по индексу | Заменяет число по существующему целому индексу от 1. |
| Удалить элемент по индексу | Удаляет элемент по существующему целому индексу; последующие индексы сдвигаются. |
| Очистить список | Устанавливает пустой список `[]`. |

Формула и индекс вычисляются при выполнении действия. Пустое поле индекса означает 1. Тип результата проверяется; целочисленный результат усекается к нулю и проверяется на переполнение. Операции сложения и умножения не работают со списком. Старые действия «Задать переменную», «Изменить переменную на» и «Умножить переменную на» сохраняют свою роль.

Например, переменная `shots` типа «Целочисленное», начальная формула `0`, и действие «Прибавить значение» с `1` считают выполненные действия. Переменная `salvo` типа «Список», формула `range(1, 12)`, затем «Изменить элемент по индексу», индекс `3`, формула `at($salvo, 3) * 2` изменит только третий элемент.

В условии сравнения списков доступны только равенство и неравенство: сравниваются длина и все элементы. Сравнения «больше» и «меньше» для списков не поддерживаются. Для сравнения длины или суммы сначала задайте вспомогательной числовой переменной результат `len($salvo)` или `sum($salvo)` действием «Редактировать переменную», затем сравните эту переменную в условии.

## Ограничения и ошибки

Проект: до 256 переменных. Одна формула: до 4096 символов, глубина дерева выражения до 32, до 100000 операций вычисления. Список: до 4096 чисел. Интеграл: до 256 шагов. Сложная вложенная формула может исчерпать лимит раньше, чем короткая простая формула.

Формулы и переменные работают в обычных событиях сценария. Событие «Удар снаряда: осколки» использует штатный механизм игры и не поддерживает эти блоки; проверка проекта сообщит о несовместимости.

Проверяйте сообщение ошибки рядом с переменной или в проверке проекта. Частые причины: запятая вместо десятичной точки, неизвестное имя, цикл начальных значений, несовместимый тип, индекс 0, пустой список в `mean`, деление на ноль, переполнение, превышение лимита. Измените формулу или зависимости перед экспортом.

Если ошибка возникает уже в Forts, текущее выполнение цепочки останавливается. Неудавшееся действие редактирования не изменяет свою целевую переменную. Уже выполненные действия этой цепочки сохраняют результат: общего отката цепочки нет. В журнал пишется `FMS_FORMULA_ERROR` — не более одного сообщения для одного блока за матч. Следующее событие или таймер может запустить цепочку снова.

Сохранение языка интерфейса не меняет формулы, имена и значения проекта. Имена функций и технические ссылки одинаковы в русской и английской версии.
";
  const string FormulaGuideEnglish=@"# FMS variables and formulas



## Getting started

1. Open Scenarios and create a variable in the panel on the right.
2. Set its name, type and initial expression. For example: `power`, Floating-point, `sqrt(81) * 2`.
3. Use it in a block's numeric field: `$power + 5`.
4. To change it during a match, add an Edit variable action, select the variable and an operation.
5. Open Formula guide for local help. The window supports search, Ctrl+F and F3 / Shift+F3.

The panel on the right lists declared variables and the computed **initial** values of the current project. It does not connect to a running match or show its live state. Initial expressions are evaluated when the project is prepared; changing a variable during the match does not automatically recompute the other variables. An action evaluates its expression again when it runs in Forts.

## Names and types

Use 1–40 Latin letters, digits and `_`; the first character must be a Latin letter or `_`. References can be `$power` or `power`; `$power` is recommended for clarity. Names are case-sensitive: `power` and `Power` differ. `speed`, `hp`, `damage`, `repeat_index`, `pi`, `e` and `tau` are reserved. Avoid using function names for your own variables too.

| Editor type | Value and conversion |
| --- | --- |
| Integer | Signed 32-bit number: −2147483648 to 2147483647. Expression results are truncated toward zero: `3.9` → `3`, `-3.9` → `-3`. Out-of-range results are errors. |
| Floating-point | Double-precision number. Only finite values are accepted; infinity and NaN are errors. |
| List | Up to 4096 finite numbers. Examples: `[5, 10.5, -2]` or `range(1, 12)`. Nested lists, strings and objects are unsupported. |

A number and a list do not convert into one another automatically. Use `at`, `sum`, `mean` or `len` to obtain a number from a list. Indices start at **1**, must be integers and must refer to an existing element.

Initial expressions may reference other declared variables in any order: `base = 10`, `power = $base * 3`. A cycle such as `a = $b`, `b = $a`, a self-reference or a missing variable is an error. The panel and project validation show these errors.

Global for the match scope stores one value for the entire match. Per object scope stores an independent value for each scenario object. List assignments copy values: editing one list does not change copies previously assigned to other variables or objects.

Renaming updates references in the project's expressions and blocks. Deleting an in-use variable is blocked: change or remove its references first. After changing a type, check that the expression and block operations remain compatible.

## Expression syntax

Use a decimal point: `1.25`. Scientific notation is supported: `2.5e3`, `1e-4`. Separate function arguments and list entries with commas. Trigonometric functions use radians.

| Syntax | Meaning |
| --- | --- |
| `+`, `-`, `*`, `/` | Addition, subtraction, multiplication, division |
| `%` | Remainder using a quotient rounded down: `-5 % 3` → `1` |
| `^` | Exponentiation: `2 ^ 3` |
| `-x`, `+x` | Unary sign |
| `( ... )` | Explicit evaluation order: `(2 + 3) * 4` |
| `pi`, `e`, `tau` | π, Euler's number, 2π |
| `[1, 2, 3]` | Numeric list |

Precedence: parentheses and calls, powers, unary signs, multiplication/division/remainder, addition/subtraction. Powers associate to the right: `2^3^2` → `512`. `-2^2` → `-4`, `2^-2` → `0.25`. Use parentheses for complex powers and negative bases: `(-2)^2`, `-(2^2)`, `(2^3)^2`.

Expressions use a restricted mathematical language. They do not execute Lua code, commands, assignments within an expression, file access or network access. Change values through a separate action.

## Function reference

### Numbers and intervals

| Function | Result / example |
| --- | --- |
| `abs(x)` | Absolute value: `abs(-5)` → `5` |
| `sqrt(x)`, `cbrt(x)` | Square and cube roots: `sqrt(81)` → `9`, `cbrt(-8)` → `-2` |
| `pow(x, y)` | Power; same purpose as `x ^ y` |
| `exp(x)`, `ln(x)` | e raised to x; natural logarithm |
| `log(x)`, `log(x, base)` | Natural logarithm; logarithm in a given base: `log(8, 2)` → `3` |
| `log10(x)` | Base-10 logarithm |
| `floor(x)`, `ceil(x)` | Round down/up: `floor(-1.2)` → `-2` |
| `round(x)` | Nearest integer, halves away from zero: `round(-2.5)` → `-3` |
| `sign(x)` | −1, 0 or 1 |
| `min(a, b, ...)`, `max(a, b, ...)` | Minimum/maximum of two or more numbers, or a single list: `max($values)` |
| `clamp(x, lo, hi)` | Restrict to an interval: `clamp(15, 0, 10)` → `10` |
| `lerp(a, b, t)` | `a + (b - a) * t`; t outside 0…1 extrapolates |

Division/remainder by zero, a square root of a negative value, an invalid logarithm base or any other nonfinite result is an error.

### Angles and trigonometry

| Functions | Purpose |
| --- | --- |
| `sin(x)`, `cos(x)`, `tan(x)` | Trigonometric functions, x in radians |
| `asin(x)`, `acos(x)`, `atan(x)` | Inverse functions, result in radians |
| `atan2(y, x)` | Vector angle with quadrant awareness |
| `sinh(x)`, `cosh(x)`, `tanh(x)` | Hyperbolic functions |
| `rad(degrees)`, `deg(radians)` | Degrees to radians and radians to degrees |

Examples: `sin(rad(30))` ≈ `0.5`, `deg(atan2(1, 1))` ≈ `45`.

### Lists

| Function | Result / example |
| --- | --- |
| `len(list)` | Entry count: `len([2, 4, 6])` → `3` |
| `at(list, index)` | Entry at an index: `at([2, 4, 6], 2)` → `4` |
| `sum(list)` | Sum: `sum([2, 4, 6])` → `12`; empty list → `0` |
| `mean(list)` | Arithmetic mean of a nonempty list |
| `min(list)`, `max(list)` | Minimum/maximum of a nonempty list |
| `range(start, end[, step])` | Numeric sequence, inclusive of the end only if a step reaches it; step cannot be zero |

Examples: `range(1, 5)` → `[1, 2, 3, 4, 5]`; `range(0, 1, 0.25)` → `[0, 0.25, 0.5, 0.75, 1]`. Specify a negative step for a descending range: `range(5, 1, -1)`. More than 4096 entries is an error.

### Numerical calculus

`diff(expression, x, point)` approximates a derivative with respect to the local variable x at a point. It uses a central difference with `h = 1e-5 * max(1, abs(point))`.

Example: `diff(x^3, x, 2)` ≈ `12`.

`integrate(expression, x, lower, upper[, steps])` approximates a definite integral using Simpson's rule. The default is 64 steps; an explicit step count must be an even integer from 2 to 256.

Examples: `integrate(x^2, x, 0, 1)` ≈ `0.3333333333`; `integrate(sin(x), x, 0, pi, 128)` ≈ `2`.

The second argument is a local variable name (`x` or `$x`). Within the first argument it applies only inside this call and does not require a project declaration. Other variables work normally: `integrate($scale * x^2, x, 0, 1)`. Nested calls are allowed within evaluation limits.

These are numerical approximations, not symbolic algebra. Discontinuities, singularities, sharp oscillations and very large/small scales may produce inaccurate results or errors. For these cases, adjust the bounds and step count; do not treat the answer as an exact proof.

## Event variables

These are available when block expressions run and cannot be used in project variables' initial expressions:

| Name | Value |
| --- | --- |
| `$speed` | Speed from the current event context |
| `$hp` | Health from the current event context |
| `$damage` | Damage from the current event context |
| `$repeat_index` | Current repeat number starting at 1; 0 outside a loop |

The event determines the available context and its meaning; an expression does not inspect arbitrary objects by itself. `$speed`, `$hp` and `$damage` use 0 when the required context or existing object is unavailable. Use a relevant numeric block parameter for in-game damage, speed or time, for example `clamp($damage * 1.2, 0, 500)`.

## Editing a variable with an action

In an Edit variable action, select an existing variable:

| Operation | Effect |
| --- | --- |
| Set value / Replace entire list | Replace the value with the expression result. For lists: `[10, 20, 30]` or `range(1, 12)`. |
| Add value | Add the result to a numeric variable. |
| Multiply by value | Multiply a numeric variable by the result. |
| Append item | Add one number to the end of a list. |
| Edit item at index | Replace the number at an existing integer index starting at 1. |
| Remove item at index | Remove an existing integer index; later indices shift. |
| Clear list | Set an empty list `[]`. |

The expression and index are evaluated when the action runs. An empty index field means 1. Result types are checked; integer results are truncated toward zero and checked for overflow. Add and Multiply do not work on lists. The older Set variable, Change variable by and Multiply variable by actions retain their roles.

For example, an Integer variable `shots` with initial expression `0` and an Add value action with `1` counts action executions. With a List variable `salvo` initialized to `range(1, 12)`, Edit item at index `3` with `at($salvo, 3) * 2` changes only its third entry.

List conditions support only equality and inequality: length and every entry are compared. Greater-than and less-than comparisons are unsupported for lists. To compare length or sum, first assign `len($salvo)` or `sum($salvo)` to a numeric helper variable using an Edit variable action, then compare that variable in a condition.

## Limits and errors

A project: up to 256 variables. Each expression: up to 4096 characters, expression tree depth 32, and 100000 evaluation operations. A list: up to 4096 numbers. An integral: up to 256 steps. A complex nested expression may exhaust its budget sooner than a short, simple one.

Expressions and variables work with regular scenario events. Projectile impact: fragments uses the game's native mechanism and does not support these blocks; project validation reports the incompatibility.

Read errors beside the variable or in project validation. Common causes include a decimal comma instead of a point, an unknown name, initial-value cycles, incompatible types, index 0, an empty list in `mean`, division by zero, overflow or a limit being exceeded. Fix the expression or dependencies before export.

If an error occurs in Forts, the current chain invocation stops. The failed variable-edit action leaves its target variable unchanged. Earlier completed actions retain their effects: there is no whole-chain rollback. `FMS_FORMULA_ERROR` is written to the log at most once per block per match. A later event or timer can start the chain again.

Changing the interface language preserves project expressions, names and values. Function names and technical references are identical in both languages.
";
 }
}
