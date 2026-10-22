# Запрос на проверку возможного ложного срабатывания

Это шаблон для владельца проекта. Обращения от его имени автоматически не отправлялись.

Сначала проверьте точный SHA256 и повторный анализ отчёта. VirusTotal рекомендует обращаться к движку, выдавшему обнаружение: [официальная инструкция](https://docs.virustotal.com/docs/false-positive).

- McAfee: [официальная страница отправки образцов](https://www.mcafee.com/en-us/consumer-corporate/mcafee-labs/how-to-submit-sample.html).
- SecureAge: [официальная форма связи](https://www.secureage.com/contact-us). Укажите, что запрос относится к обнаружению SecureAge в VirusTotal.

Приложите публичный файл выпуска или ссылку на него и соответствующий отчёт. Для нового выпуска используйте его собственный SHA256 из `SHA256SUMS.txt`; не переносите результат проверки между версиями.

## English submission template

Subject: Request for manual review — possible false positive, Forts Mod Studio

Hello,

Please review a possible false-positive detection in Forts Mod Studio, a Windows desktop mod editor for the game Forts.

Public source repository: https://github.com/Yorodoshi/Forts-Mod-Studio

Release: [insert the exact release URL]

File SHA256: [insert the hash from this release's SHA256SUMS.txt]

VirusTotal report: [insert the report URL for the same hash]

Your engine's detection name: [insert the exact detection label]

The application edits local JSON projects and image/audio assets, generates game mod files and reads local game templates. It loads the installed game's DevIL image decoder and invokes the game's LuaPlusC syntax checker. The historical 2.7.1 release restricts those code paths to a Steam-discovered or explicitly approved local game installation; this change and its published GitHub build provenance do not apply to the restored 2.7.0 release. Public source and the release-specific verification notes are available in the repository. Published versions 2.7.0, 2.7.1 and 2.8.0 are not Authenticode-signed.

Please determine whether the detection is warranted and advise which behavior or component requires correction. We are requesting a manual review, not asserting that a low number of detections proves the file safe.

Thank you.
