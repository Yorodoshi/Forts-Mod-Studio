# Возврат 2.7.0 / Restore 2.7.0

По просьбе владельца проекта основным выпуском снова назначена **2.7.0**.

- Код программы и скрипт сборки в основной ветке восстановлены из тега `v2.7.0`.
- Исходный EXE 2.7.0 не пересобирался и не заменялся. Переключение русского и английского интерфейса сохранено.
- Выпуск 2.7.1 отмечен как предварительный и выведен из рекомендуемых; его исходники, файлы и аттестация остаются в истории для анализа.
- Новый автоматический процесс публикации убран из основной ветки вместе с откатом к прежней схеме сборки.
- Журнал изменений и документы безопасности остаются в GitHub.

Причина возврата — предоставленный владельцем отчёт [VirusTotal для 2.7.1](https://www.virustotal.com/gui/file/43e2d48921906361ba944372307185c8714d4c73269b8e51f39954c18e20c38e): **4/71**, включая Microsoft, Bkav Pro, McAfee Scanner и SecureAge. Это результат конкретного файла на момент отчёта. Причина обнаружений не установлена; меньший счётчик у старой версии сам по себе не доказывает её безопасность.

## Исходные файлы 2.7.0

EXE SHA256:

```text
f16fd6739f4bc4dd497550f6e666daac76a499d4a049053c35ab2a66e10773d0
```

ZIP SHA256:

```text
c3d6a3dd2b28785bbc413d1c3785d000821f784bf39a6d1a71e7f4c284239b6f
```

Для этого EXE пользователь ранее предоставил отчёт 2/68. Чистый результат не заявляется; программа не имеет подписи Authenticode.

## English

At the owner's request, **2.7.0** is the primary release again. Application source and the build script on the main branch have been restored from `v2.7.0`. The original EXE and ZIP have not been rebuilt or replaced; their SHA256 hashes are recorded above. Russian/English switching remains available.

Release 2.7.1 is now a prerelease retained for investigation, together with its source, binaries and build attestation. The new automated publishing pipeline has been removed from the main branch. Changes and security notes remain documented on GitHub.

The owner supplied a 4/71 VirusTotal report for 2.7.1, including Microsoft. The previous 2/68 report applies to 2.7.0. The cause is unconfirmed; a lower count alone is not proof of safety. No clean antivirus verdict or Authenticode signature is claimed.
