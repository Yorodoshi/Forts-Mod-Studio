# Changes / Изменения

## 2.8.0 — переменные и формулы / variables and formulas

- Добавлены типы «Целочисленное» (Int32), «Дробное» (double) и «Список» чисел; прежние числовые переменные загружаются как дробные.
- Начальные значения и числовые значения блоков поддерживают формулы, ссылки на переменные, тригонометрию, логарифмы, операции со списками, численные производные и интегралы.
- Справа в сценариях появилась отдельная панель всех переменных с типом, областью, формулой и вычисленным начальным значением.
- Добавлен редактор с сохранением/отменой, обновлением ссылок при переименовании и списком зависимостей при удалении.
- Действие «Редактировать переменную» присваивает и изменяет числа, заменяет списки, добавляет, изменяет и удаляет элементы, очищает список.
- Формулы экспортируются как данные дерева выражения; добавлены проверки типов, областей определения, индексов, переполнения и сложности вычисления. Ошибка в матче останавливает текущий запуск цепочки и записывается в журнал.
- Полный гайд с поиском встроен в приложение и опубликован на русском и английском.

Typed variables, a dedicated right-hand variable pane, atomic editing/renaming, numeric and list mutation actions, and bounded mathematical expressions are now available. The expression language includes numeric differentiation and Simpson integration. Existing numeric projects retain their values. Initial values are shown in the editor; live match values are not read back from Forts.

[Руководство](docs/formulas.md) · [English guide](docs/formulas.en.md) · [Выпуск / Release notes](docs/releases/v2.8.0.md).

Сборка компилируется штатным компилятором .NET Framework. Автоматические тесты и интерактивный прогон FMS/Forts в этом изменении не выполнялись. Выпуск не подписан Authenticode; результат VirusTotal для него не заявляется.

## Rollback to 2.7.0 / Возврат 2.7.0

- Restored application source and build.ps1 exactly from v2.7.0; original release binaries are unchanged.
- Made 2.7.0 the primary release; retained 2.7.1 as a prerelease for investigation.
- Recorded the owner's VirusTotal report for 2.7.1: 4/71, including Microsoft.
- Removed the new automated publishing pipeline from main; preserved its historical tag and build records.
- Kept Russian/English switching and GitHub change documentation.

Возврат выполнен по просьбе владельца. Меньший счётчик обнаружений сам по себе не подтверждает безопасность старой версии. [Подробности](docs/releases/rollback-to-v2.7.0.md).

## 2.7.1 — prerelease / выведен из рекомендуемых

- Separated executable/DLL location approval from imported project data.
- Restricted DevIL module/dependency loading and checked native image buffers.
- Confined generated resource paths and audio-preview filenames.
- Bounded the Lua-checker process and drained both output streams concurrently.
- Added an explicit asInvoker manifest, release checksums, build metadata and a pinned GitHub Actions provenance workflow.
- Added bilingual security notes and a suspected-false-positive report template.
- Documented that the binary is unsigned and no clean VirusTotal result is claimed. Published the successful build record and exact release hashes.

Исправлены границы доверия к папке игры, загрузка декодера, выходные пути и запуск анализатора Lua. Добавлены контрольные суммы, происхождение сборки и сведения о подписи. [Полные заметки](docs/releases/v2.7.1.md).

## 2.7.0

- Added live Russian/English interface switching in the top toolbar.
- Russian remains the first-launch default; language selection is saved separately from projects.
- Translated editors, hints, dialogs, scenario controls, range messages and preview overlays.
- English object names are displayed when available; editable project content stays unchanged.
- Added bilingual documentation and a Windows build script.

Добавлено переключение русского и английского интерфейса с запоминанием выбора. Подготовлены документация и исходники для публикации.

## 2.6

- Prepared projectile sizes and rotation atlases replace per-frame filtering of full-size imports.
- Retained world tiles reuse geometry while the camera moves.
- Stationary object sprites preserve draw order during other objects' animations.
- Added stage timings and bounded cache diagnostics in Frame details.

## 2.5.2

- Reworked the shared object list and category selection state.
- Added background autosave serialization and reusable preview data.
