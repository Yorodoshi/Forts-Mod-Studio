# Безопасность / Security

## Проверка выпуска

Скачивайте программу из [GitHub Releases этого репозитория](https://github.com/Yorodoshi/Forts-Mod-Studio/releases). У выпуска публикуются:

- `SHA256SUMS.txt` — контрольные суммы EXE и ZIP;
- `build-info.json` — версия, исходный коммит, сведения о компиляторе и фактический статус подписи;
- журнал сборки GitHub Actions и аттестация происхождения EXE/ZIP.

Сравнить сумму файла на Windows:

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath '.\Forts Mod Studio 2.7.1.exe'
```

При установленном GitHub CLI происхождение файла можно проверить так:

```powershell
gh attestation verify '.\Forts Mod Studio 2.7.1.exe' --repo Yorodoshi/Forts-Mod-Studio
```

Совпадение суммы подтверждает идентичность файла, а аттестация — его связь со сборкой из этого репозитория. Они сами по себе не являются заключением антивируса.

## Подпись и предупреждения Windows

Текущий EXE **не подписан сертификатом Authenticode издателя**. Название компании в свойствах EXE не является такой подписью. Аттестация GitHub также не заменяет Authenticode.

Для подписи будущих выпусков нужен действительный сертификат подписи кода или настроенная служба подписи владельца проекта. Самоподписанный сертификат не создаёт доверие издателю у остальных пользователей. Даже правильно подписанная новая программа может получить предупреждение репутации SmartScreen. [Документация Microsoft](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation).

## Что делает приложение

- Работает с проектами, импортированными изображениями и аудио, игровыми шаблонами и журналами.
- Записывает автокопии и настройки в `%LOCALAPPDATA%\FortsModStudio2`.
- По команде пользователя создаёт моды и карту полигона в установленной Forts.
- Для DDS/TGA использует `DevIL.dll` из выбранной установки Forts; для проверки Lua запускает её `LuaPlusC.exe` с параметром проверки синтаксиса.
- Для воспроизведения аудио использует Windows MCI. По нажатию соответствующих кнопок открывает Forts через Steam или страницы документации.
- Не содержит встроенных загрузчиков обновлений, телеметрии, автозапуска при входе в Windows или изменения настроек антивируса.

Начиная с 2.7.1 импорт проекта не разрешает загрузку DLL или запуск EXE из указанной им новой папки. Используется локальная установка, найденная через Steam, либо папка, явно указанная пользователем в **«Проект и установка» → «Выбрать папку игры»**. Одобренные папки хранятся отдельно, в `approved-game-folders.txt`. Зависимости декодера ищутся только в этой папке игры и системной папке Windows.

Программа запускается с правами текущего пользователя (`asInvoker`). Запись в защищённую папку игры может быть запрещена Windows; редактор не обходит права доступа.

## VirusTotal и возможные ложные срабатывания

VirusTotal показывает результаты независимых движков. Универсального допуска «прошёл VirusTotal» нет. Изменения безопасности не гарантируют счётчик 0/N, а отсутствие обнаружений не доказывает отсутствие уязвимостей.

В пользовательском отчёте для 2.7.0 отмечены **McAfee Scanner** и **SecureAge**, 2/68. SHA256 файла:

```text
f16fd6739f4bc4dd497550f6e666daac76a499d4a049053c35ab2a66e10773d0
```

Эта запись относится именно к 2.7.0. Она не является результатом сканирования 2.7.1. Причина двух срабатываний не установлена. Пример запроса на ручную проверку находится в [docs/false-positive-report.md](docs/false-positive-report.md). [Порядок оспаривания по документации VirusTotal](https://docs.virustotal.com/docs/false-positive).

Для сообщения о срабатывании приложите версию FMS, SHA256, ссылку на отчёт и точное имя движка. Не присылайте ключи доступа и личные проекты. Уязвимости можно сообщить через вкладку Security репозитория, если доступно приватное сообщение; иначе откройте Issue без публикации чувствительных данных.

## English

Download releases from this repository. Each release includes SHA256 checksums, build metadata, an Actions build log and provenance attestations for the EXE/ZIP. Checksums establish file identity; attestations establish build provenance. Neither is an antivirus verdict.

The current binary is **not Authenticode-signed**. An EXE company field and a GitHub attestation do not replace a trusted publisher certificate. A valid certificate or owner-configured signing service is required to sign future releases. A new signed binary may still receive a SmartScreen reputation warning.

FMS edits projects and local assets, stores autosaves/settings under `%LOCALAPPDATA%\FortsModStudio2`, and writes mods/maps when requested. It uses the installed game's DevIL decoder and Lua syntax checker, plus Windows MCI audio playback. It contains no telemetry, automatic updater, login persistence or antivirus-setting changes.

From 2.7.1, imported project data cannot approve a new executable/DLL directory. FMS uses a local Steam-discovered installation or a game folder explicitly selected by the user. Approved folders are kept separately in `approved-game-folders.txt`. Decoder dependencies are restricted to that game directory and Windows System32. The application manifest requests the current user's permissions (`asInvoker`).

The reported 2/68 detections for the SHA256 above apply to **2.7.0**, not 2.7.1. Their cause is unconfirmed. Report suspected false positives to the detecting vendor using the linked template. No VirusTotal result is advertised as clean without a corresponding report for the exact released hash.
