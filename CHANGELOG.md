# Changes / Изменения

## 2.7.1

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
