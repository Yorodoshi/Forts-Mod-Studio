using System;
using System.Collections.Generic;

namespace FortsStudio {
 static partial class UiText {
  // Keep parser identifiers and position numbers intact while translating its diagnostics.
  static readonly string[,] formulaErrorPhrases={
   {"Renamed formula exceeds 4096 characters.","Формула после переименования превышает 4096 символов."},
   {"Integral steps must be an even integer from 2 to 256.","Число шагов интеграла должно быть чётным целым от 2 до 256."},
   {"Formula result is not finite or is outside a function domain.","Результат формулы не конечен или выходит за область определения функции."},
   {"A numeric value is required; a list cannot be used here.","Здесь требуется число; список использовать нельзя."},
   {"List index must be an integer from 1 to its length.","Индекс списка должен быть целым числом от 1 до его длины."},
   {"Calculus second argument must be a local variable name (for example x)","Второй аргумент производной или интеграла должен быть именем локальной переменной (например x)"},
   {"Formula work limit exceeded (100000).","Превышен лимит вычисления формулы (100000 операций)."},
   {"Clamp minimum exceeds maximum.","В clamp нижняя граница превышает верхнюю."},
   {"Range step cannot be zero.","Шаг range не может быть нулевым."},
   {"Invalid logarithm domain.","Недопустимый аргумент или основание логарифма."},
   {"Formula exceeds 4096 characters.","Формула превышает 4096 символов."},
   {"Invalid variable name.","Недопустимое имя переменной."},
   {"Division by zero.","Деление на ноль."},
   {"A list is required.","Требуется список."},
   {"Formula is empty.","Формула пуста."},
   {"Formula nesting exceeds 32 levels","Вложенность формулы превышает 32 уровня"},
   {"List exceeds 4096 items","Список превышает 4096 элементов"},
   {"Expected a number, variable or function","Ожидается число, переменная или функция"},
   {"Wrong number of arguments for '","Неверное количество аргументов у '"},
   {"Unknown function '","Неизвестная функция '"},
   {"Unknown function: ","Неизвестная функция: "},
   {"Unknown variable: ","Не найдена переменная: "},
   {"Expected a value","Ожидается значение"},
   {"Unexpected character","Неожиданный символ"},
   {"Number must be finite","Число должно быть конечным"},
   {"Invalid exponent","Недопустимая экспонента"},
   {"Invalid number","Недопустимое число"},
   {"Expected '","Ожидается '"},
   {" requires a nonempty list."," требует непустой список."},
   {" must contain a number or numeric list."," должна содержать число или числовой список."},
   {"Variable ","Переменная "},
   {" at position "," в позиции "}
  };
  public static string FormulaError(string message) {
   if(English||String.IsNullOrEmpty(message))return message??"";
   for(int i=0;i<formulaErrorPhrases.GetLength(0);i++)message=message.Replace(formulaErrorPhrases[i,0],formulaErrorPhrases[i,1]);
   return message;
  }
  static void AddFormulaTranslations(Dictionary<string,string> map) {
   map["Индекс должен быть целым числом от 1 до 4096."]="The index must be an integer from 1 to 4096.";
   map["Гайд по формулам"]="Formula guide";
   map["Поиск в гайде"]="Search the guide";
   map["Локальное руководство по формулам и переменным"]="Local formula and variable guide";
   map["Найти далее"]="Find next";
   map["Совпадений не найдено"]="No matches found";
   map["Ctrl+F — поиск · F3 / Shift+F3 — следующее / предыдущее · Esc — закрыть"]="Ctrl+F — search · F3 / Shift+F3 — next / previous · Esc — close";
   map["Закрыть"]="Close";
   map["Переменные"]="Variables";
   map["Переменные проекта · "]="Project variables · ";
   map["Все объявленные переменные. Показаны начальные значения проекта; текущие значения внутри запущенной Forts здесь не считываются."]="All declared variables. These are the project's initial values; live values in a running Forts match are not read here.";
   map["＋ Переменная"]="＋ Variable";
   map["＋ Создать переменную"]="＋ Create variable";
   map["Ошибка начальных значений: "]="Initial value error: ";
   map["Переменных пока нет. Создайте целочисленное, дробное значение или список чисел."]="No variables yet. Create an integer, floating-point value or list of numbers.";
   map["Целочисленное"]="Integer";
   map["Дробное"]="Floating-point";
   map["Список"]="List";
   map["Общая на весь матч"]="Global for the match";
   map["Отдельная у каждого объекта"]="Per object";
   map["Начальное значение: "]="Initial value: ";
   map["Формула: "]="Expression: ";
   map["Редактировать"]="Edit";
   map["Удалить"]="Delete";
   map["Новая переменная"]="New variable";
   map["Редактирование переменной"]="Edit variable";
   map["Редактировать переменную"]="Edit variable";
   map["Имя · латиница, цифры и _"]="Name · Latin letters, digits and _";
   map["Тип значения"]="Value type";
   map["Область"]="Scope";
   map["Формула начального значения"]="Initial value expression";
   map["Формула может ссылаться на другие переменные: $counter. Переименование обновляет ссылки во всём проекте. Начальные формулы рассчитываются при подготовке мода."]="An expression can reference other variables: $counter. Renaming updates references throughout the project. Initial expressions are evaluated when preparing the mod.";
   map["Имя должно начинаться с латинской буквы или _ и содержать только латиницу, цифры и _."]="The name must start with a Latin letter or _ and contain only Latin letters, digits and _.";
   map["Это имя зарезервировано: "]="This name is reserved: ";
   map["Переменная с таким именем уже существует."]="A variable with this name already exists.";
   map["Целочисленное: 32 бита; дробная часть результата отбрасывается к нулю."]="Integer: 32 bits; the fractional part of the result is truncated toward zero.";
   map["Список чисел: [1, 2, 3] или range(1, 10). Индексы начинаются с 1. Пустая формула задаёт []."]="Numeric list: [1, 2, 3] or range(1, 10). Indices start at 1. An empty expression initializes [].";
   map["Дробное: конечное число двойной точности. В формулах используйте точку: 1.5."]="Floating-point: a finite double-precision number. Use a decimal point in expressions: 1.5.";
   map["Сохранить"]="Save";
   map["Отмена"]="Cancel";
   map["Переменная используется. Сначала измените или удалите эти ссылки:\n\n"]="This variable is in use. Change or remove these references first:\n\n";
   map["Удаление переменной"]="Delete variable";
   map["Удалить переменную "]="Delete variable ";
   map["Заменить весь список"]="Replace entire list";
   map["Добавить элемент в конец"]="Append item";
   map["Изменить элемент по индексу"]="Edit item at index";
   map["Удалить элемент по индексу"]="Remove item at index";
   map["Очистить список"]="Clear list";
   map["Задать значение"]="Set value";
   map["Прибавить значение"]="Add value";
   map["Умножить на значение"]="Multiply by value";
   map["Операция не подходит типу переменной. Выберите другую операцию."]="This operation does not match the variable type. Choose another operation.";
   map["Операция"]="Operation";
   map["Индекс элемента · начиная с 1"]="Item index · starting at 1";
   map["Формула списка"]="List expression";
   map["Формула значения"]="Value expression";
   map["Выберите существующую переменную."]="Select an existing variable.";
   map[" · формула"]=" · expression";
   map["Пусто: используется прежнее числовое значение блока."]="Empty: the block's existing numeric value is used.";
   map["Не найдена переменная: "]="Variable not found: ";
   map["Вычисляется в Forts при выполнении блока."]="Evaluated in Forts when this block runs.";
   map["При начальных значениях: "]="With initial values: ";
   map["Ошибка формулы: "]="Expression error: ";
   map["Не найдена: "]="Not found: ";
   map["Имя переменной: 1–40 латинских букв, цифр или _, первая — буква или _."]="Variable name: 1–40 Latin letters, digits or _; the first character must be a letter or _.";
   map["Максимум 256 переменных в проекте."]="A project can have at most 256 variables.";
   map["Максимум 4096 элементов списка."]="A list can have at most 4096 entries.";
   map["Не объявлена переменная: "]="Variable is not declared: ";
   map["Неверная область переменной "]="Invalid variable scope: ";
   map["Неизвестный тип переменной: "]="Unknown variable type: ";
   map["Ожидается список чисел, например [1, 2, 3]."]="Expected a numeric list, for example [1, 2, 3].";
   map["Ожидается число, а не список."]="Expected a number, not a list.";
   map["Повторное имя переменной: "]="Duplicate variable name: ";
   map["Пустое объявление переменной."]="Empty variable declaration.";
   map["Результат должен быть конечным числом."]="The result must be a finite number.";
   map["Целочисленное значение выходит за диапазон Int32."]="The integer value is outside the Int32 range.";
   map["Циклическая зависимость начальных значений: "]="Cyclic dependency between initial values: ";
   map[": для списка используйте действие «Редактировать переменную»."]=": use an Edit variable action for a list.";
   map[": задайте формулу списка, например [1, 2, 3]."]=": provide a list expression, for example [1, 2, 3].";
   map[": не объявлена переменная "]=": variable is not declared: ";
   map[": операция не соответствует типу переменной."]=": the operation does not match the variable type.";
   map[": списки можно сравнивать только через == или ~=."]=": lists support only == and ~= comparisons.";
   map[": формула: "]=": expression: ";
   map[": формулы и переменные требуют обычного события, а не штатных осколков."]=": expressions and variables require a regular event, not native-impact fragments.";
   map["Начальные значения переменных: "]="Initial variable values: ";
   map["Не найден встроенный исполнитель сценариев."]="The bundled scenario runtime was not found.";
   map["Пусто: индекс 1."]="Empty: index 1.";
   map["Формула должна возвращать список чисел."]="The expression must return a numeric list.";
   map["Формула должна возвращать число, а не список."]="The expression must return a number, not a list.";
   map["Функция требует список чисел: "]="This function requires a numeric list: ";
   map["Здесь требуется число, а не список."]="A number is required here, not a list.";
   for(int i=0;i<formulaErrorPhrases.GetLength(0);i++)map[formulaErrorPhrases[i,1]]=formulaErrorPhrases[i,0];
  }
 }
}
