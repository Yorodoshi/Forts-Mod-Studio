# Безопасность / Security

## Проверка выпуска

Скачивайте программу из [GitHub Releases этого репозитория](https://github.com/Yorodoshi/Forts-Mod-Studio/releases). Основной выпуск — **2.8.0**, с формулами и редактором переменных. К нему приложены `SHA256SUMS.txt` и `build-info.json`: локальная сборка, без подписи Authenticode, без заявленного результата VirusTotal. Счётчики прежних версий не относятся к новому EXE.

Исторический SHA256 исходного EXE 2.7.0, восстановленного при прежнем откате:

```text
f16fd6739f4bc4dd497550f6e666daac76a499d4a049053c35ab2a66e10773d0
```

Сравнить сумму файла на Windows:

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath '.\Forts Mod Studio 2.8.exe'
```

Сверяйте сумму с файлом именно своего выпуска. У исторического выпуска 2.7.1 опубликованы также журнал GitHub Actions и аттестация происхождения EXE/ZIP. Они относятся только к 2.7.1. Совпадение суммы подтверждает идентичность файла; аттестация — связь с конкретной сборкой. Ни то, ни другое не является заключением антивируса. [Подробности проверки 2.7.1](docs/releases/v2.7.1-verification.md).

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

В 2.7.1 добавлялись отдельное подтверждение папок игровых EXE/DLL, ограничение поиска зависимостей декодера и манифест `asInvoker`. **При откате эти изменения отменены.** Выпуск 2.8 продолжает восстановленную основу 2.7.0; гарантии изменений 2.7.1 к нему не применяются. Проверка границ доверия при импорте проекта и загрузке игровых зависимостей остаётся незавершённой. Не открывайте проекты и игровые зависимости из источников, которым не доверяете.

Формулы 2.8 разбираются отдельным математическим парсером и экспортируются как дерево данных. Введённый текст не передаётся в `loadstring` или другую функцию исполнения кода. Размер выражений, глубина, списки и объём вычисления ограничены.

## VirusTotal и возможные ложные срабатывания

VirusTotal показывает результаты независимых движков. Универсального допуска «прошёл VirusTotal» нет. Изменения безопасности не гарантируют счётчик 0/N, а отсутствие обнаружений не доказывает отсутствие уязвимостей.

Пользовательские отчёты:

- **2.7.0, SHA256 выше: 2/68**, McAfee Scanner и SecureAge.
- **2.7.1, SHA256 `43e2d48921906361ba944372307185c8714d4c73269b8e51f39954c18e20c38e`: 4/71**, Bkav Pro, Microsoft, McAfee Scanner и SecureAge. [Отчёт VirusTotal](https://www.virustotal.com/gui/file/43e2d48921906361ba944372307185c8714d4c73269b8e51f39954c18e20c38e); точные метки — в [записи проверки](docs/releases/v2.7.1-verification.md).

Счётчики записаны по предоставленным пользователем снимкам экрана; независимая проверка через API не выполнялась. Причина срабатываний не установлена. Откат сделан по просьбе пользователя, а не как доказательство безопасности 2.7.0. Пример запроса на ручную проверку находится в [docs/false-positive-report.md](docs/false-positive-report.md). [Порядок оспаривания по документации VirusTotal](https://docs.virustotal.com/docs/false-positive).

Для сообщения о срабатывании приложите версию FMS, SHA256, ссылку на отчёт и точное имя движка. Не присылайте ключи доступа и личные проекты. Уязвимости можно сообщить через вкладку Security репозитория, если доступно приватное сообщение; иначе откройте Issue без публикации чувствительных данных.

## English

Download releases from this repository. **2.8.0 is the primary release**, with formulas and variable editing. Its checksums and local-build metadata are attached to the release; no VirusTotal result is claimed. The 2.7.0 hash above is historical. Release 2.7.1 remains a historical prerelease; its Actions provenance and attestations apply only to 2.7.1. Checksums establish file identity; attestations establish build provenance. Neither is an antivirus verdict.

The current binary is **not Authenticode-signed**. An EXE company field and a GitHub attestation do not replace a trusted publisher certificate. A valid certificate or owner-configured signing service is required to sign future releases. A new signed binary may still receive a SmartScreen reputation warning.

FMS edits projects and local assets, stores autosaves/settings under `%LOCALAPPDATA%\FortsModStudio2`, and writes mods/maps when requested. It uses the installed game's DevIL decoder and Lua syntax checker, plus Windows MCI audio playback. It contains no telemetry, automatic updater, login persistence or antivirus-setting changes.

The rollback removed 2.7.1's explicit approval of executable/DLL directories, restricted decoder dependency search and `asInvoker` manifest. Those protections do not apply to the restored 2.7.0 baseline or its 2.8 continuation. Review of that baseline's trust boundaries for imported projects and native game dependencies remains incomplete. Only open projects and game dependencies from sources you trust. Formula input in 2.8 is parsed as bounded mathematical data; it is not passed to Lua code evaluation.

The user reported **2/68 for 2.7.0** (McAfee Scanner and SecureAge) and **4/71 for 2.7.1** (Bkav Pro, Microsoft, McAfee Scanner and SecureAge). The linked verification record contains the exact hash, report and labels for 2.7.1. These are user-provided screenshots, not independent API scans. The cause remains unconfirmed; a lower count is not proof of safety. Report suspected false positives to the detecting vendor using the linked template. No clean result is claimed.
