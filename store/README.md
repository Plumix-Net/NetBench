# Релиз: App Store, Google Play и desktop

Актуально на 2026-10-25 (1.0.0). Здесь — как устроен релиз, какие ключи нужны и что остаётся
сделать руками в консолях сторов. Исходные тексты витрины (копирайт) — в umbrella-репо:
`docs/marketing/netbench-store-listing.md`; то, что реально выгружается, — в `fastlane/metadata`.

```
fastlane/
├── Fastfile, Appfile          лейны выгрузки (bundle exec fastlane lanes)
├── .env.example               образец локальных ключей → fastlane/.env (в .gitignore)
├── metadata/ios/<язык>/       App Store: name, subtitle, description, keywords, release_notes, URL…
│   └── review_information/    заметки для App Review
├── metadata/android/<язык>/   Google Play: title, short/full description,
│   └── images/                icon, featureGraphic, phoneScreenshots/
└── screenshots/ios/<язык>/    скриншоты iPhone 6.9" (1320×2868)
CHANGELOG.md                   заметки версии → GitHub Release и desktop-обновление
scripts/pack-desktop.sh        desktop-установщики (Velopack)
.github/workflows/release.yml  релиз по тегу vX.Y.Z
```

Языки: App Store — `en-US`, `ru`; Google Play — `en-US`, `ru-RU`.

## Как выпустить версию

1. Поднять `NetBenchVersion` в `NetBench/Directory.Build.props`.
2. Раздел `## vX.Y.Z` в `CHANGELOG.md` (заметки GitHub Release и desktop-обновления).
3. «Что нового» для сторов — `fastlane/metadata/ios/{en-US,ru}/release_notes.txt` (в Play уходит
   тот же текст, лимит 500 символов). Проверка лимитов: `bundle exec fastlane check`.
4. Если менялся мобильный UI — перегенерировать скриншоты (ниже) и закоммитить.
5. `git tag vX.Y.Z && git push origin vX.Y.Z` — workflow **Release**:
   - IPA → TestFlight, тексты и скриншоты → App Store Connect (версия создаётся, на ревью не уходит);
   - AAB → внутреннее тестирование Play, листинг и скриншоты → Play;
   - установщики Windows/macOS/Linux → черновик GitHub Release `vX.Y.Z` с заметками из CHANGELOG.
6. Руками: в App Store Connect выбрать билд у версии и «Add for Review»; в Play — продвинуть релиз
   из internal в closed/production; на GitHub — опубликовать черновик релиза (после публикации
   установленные desktop-копии обновятся сами).

Build number считает fastlane: следующий после последнего в TestFlight / максимального versionCode
во всех треках Play. Задать вручную — `NETBENCH_BUILD_NUMBER` или `build_number:` у лейна.

Ручной запуск workflow (Actions → Release → Run workflow) — та же цепочка для текущей версии с выбором
платформ. Локально — те же лейны: `bundle exec fastlane ios beta`, `… android internal`, `… ios metadata`,
`… android metadata` (ключи — из `fastlane/.env`).

Ruby для fastlane — версия из `.ruby-version` (на Mac: `brew install ruby`, затем
`PATH=/opt/homebrew/opt/ruby/bin:$PATH bundle install`).

## Ключи для fastlane и CI

Один раз завести и положить в GitHub → Settings → Secrets and variables → Actions:

| Секрет | Откуда |
|---|---|
| `ASC_KEY_ID`, `ASC_ISSUER_ID`, `ASC_KEY_P8_BASE64` | App Store Connect → Users and Access → Integrations → App Store Connect API → ключ с ролью **App Manager**; `base64 -i AuthKey_XXXX.p8`. Тот же ключ нотаризует macOS-сборку. |
| `IOS_DIST_CERT_P12_BASE64`, `IOS_DIST_CERT_PASSWORD` | Связка ключей → «Apple Distribution: …» → экспорт в .p12 с паролем; `base64 -i dist.p12`. Профиль App Store fastlane создаёт сам через API. |
| `PLAY_JSON_KEY` | Google Cloud → сервисный аккаунт → JSON-ключ (содержимое целиком); в Play Console → Users and permissions пригласить его e-mail с правами на приложение (релизы, листинг). |
| `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEY_ALIAS`, `ANDROID_KEYSTORE_PASSWORD` | upload-ключ (ниже), `base64 -i netbench-upload.jks`. |
| `MACOS_DEVELOPER_ID_P12_BASE64`, `MACOS_DEVELOPER_ID_P12_PASSWORD` | «Developer ID Application: …» (и «Developer ID Installer: …», если есть — тогда собирается подписанный .pkg) → экспорт в один .p12. Без секрета macOS-сборка неподписанная. |

Переменная репозитория `PLAY_RELEASE_STATUS` — `draft` (по умолчанию: пока в Play нет ни одного
опубликованного релиза, API принимает только черновики), после первой публикации — `completed`.

Локально те же значения — в `fastlane/.env` (образец — `fastlane/.env.example`); сертификаты Apple
берутся из связки ключей, upload-ключ Android — из `~/.android-keys`.

## Одноразовые шаги в консолях

Fastlane не умеет (или API не даёт) — сделать руками до первого релиза:

- **Google Play:** первый AAB загрузить вручную (Play Console → Testing → Internal testing → Create
  release; файл — из артефакта `netbench-android-aab` прогона CI или `bundle exec fastlane android build`):
  API работает только с приложением, у которого уже есть хоть одна сборка. При первой загрузке —
  включить Play App Signing. Заполнить Data safety, рейтинг контента, целевую аудиторию, «App access»,
  «Ads» (ответы — ниже), контактный e-mail разработчика.
- **App Store Connect:** App Privacy, Age Rating, цена (бесплатно), контакт для App Review (имя,
  телефон, e-mail) — ответы ниже. Заметки для ревью выгружаются из
  `fastlane/metadata/ios/review_information/notes.txt`.

## Подпись

- **Android:** upload-ключ вне репозитория — `~/.android-keys/netbench-upload.jks`, алиас
  `netbench-upload`, пароль в `~/.android-keys/netbench-upload.password`. **Сделайте резервную копию
  обоих файлов** (менеджер паролей) — без upload-ключа обновления придётся вести через сброс ключа
  в Play Console. Ключ приложения хранит Google (Play App Signing).
- **iOS:** сертификат «Apple Distribution» (связка ключей / секрет CI); App Store-профиль для
  `net.plumix.netbench` fastlane скачивает или создаёт сам (`get_provisioning_profile`).
- **macOS desktop:** «Developer ID Application» + нотаризация (Velopack: `--signAppIdentity`,
  `--notaryProfile`), hardened runtime с `NetBench.Desktop/Packaging/netbench.entitlements`.
  Локально нотаризация: `xcrun notarytool store-credentials netbench-notary --key AuthKey.p8 --key-id … --issuer …`,
  затем `MACOS_NOTARY_PROFILE=netbench-notary`.
- **Windows desktop:** без подписи (SmartScreen покажет предупреждение при первом запуске);
  сертификат можно добавить позже через `--signParams` у `vpk pack`.

## Desktop (Windows, macOS, Linux)

`scripts/pack-desktop.sh <rid>` — self-contained publish и упаковка
[Velopack](https://velopack.io) (`vpk` — локальный dotnet-tool из `.config/dotnet-tools.json`). Пакует только
под свою ОС, поэтому в CI — матрица раннеров:

| RID | Что получается |
|---|---|
| `win-x64` | `NetBench-win-x64-Setup.exe`, `-Portable.zip` |
| `osx-arm64`, `osx-x64` | `NetBench-osx-*-Portable.zip` (NetBench.app); `-Setup.pkg` — при сертификате Developer ID Installer |
| `linux-x64` | `NetBench-linux-x64.AppImage` |

Плюс пакеты обновления (`*-full.nupkg`, `*-delta.nupkg`, `releases.<rid>.json`): установленное приложение
(`NetBench.Desktop/DesktopUpdater.cs`) при запуске проверяет GitHub Releases своего канала (= RID), качает
новую версию в фоне и ставит её после выхода. Черновики и пре-релизы обновлением не считаются.
Иконки desktop — `scripts/generate_icons.py` (`Assets/netbench.ico`, `Packaging/netbench.{icns,png}`).

## Скриншоты

`bundle exec fastlane screenshots` (или `dotnet run --project NetBench/NetBench.Screenshots -c Release -- --output .`)
рендерит мобильный UI headless (Avalonia.Headless + Skia, как на устройстве) и пишет оба набора:

- App Store — `fastlane/screenshots/ios/<язык>/` 1320×2868 (iPhone 6.9");
- Google Play — `fastlane/metadata/android/<язык>/images/phoneScreenshots/` 1080×1920.

Кадры: главный экран → быстрый тест → живой прогон → результат → редактор сценария, ru и en, светлая и
тёмная темы. Данные — демо (`NetBench.Screenshots/DemoData.cs`), прогон идёт по-настоящему против
`DemoTarget` — локального прокси, который отвечает за адрес `http://192.168.1.10:8080`; в сеть ничего не
уходит. Данные лежат во временном каталоге (`MobileShell(dataDirectory)`) — данные приложения на машине
не читаются и не пишутся. Подписи кадров — `Program.cs` (`Listing`),
рамка и статус-бар — `StoreFrame.cs`. Генерировать на macOS: моноширинный шрифт приложения системный, и
на Mac он ближе всего к iOS (CI-джоба `screenshots` на Linux — проверка сценариев, её кадры не выгружаются).

Feature graphic и иконка Play — `scripts/generate_store_graphics.py`, `scripts/generate_icons.py`.

## App Privacy (App Store) и Data safety (Google Play)

Приложение ничего не собирает: нет аналитики, крэш-репортинга, рекламы, аккаунтов
и собственного сервера; сценарии, история и настройки хранятся только на устройстве.

- **App Store → App Privacy:** «Data Not Collected». Tracking — No.
  Манифест `NetBench.iOS/PrivacyInfo.xcprivacy` совпадает: трекинга и собираемых
  данных нет; required-reason API — метки времени файлов (C617.1), время с загрузки
  (35F9.1), свободное место (E174.1) — их использует рантайм .NET.
- **Google Play → Data safety:**
  - Does your app collect or share any of the required user data types? — **No**.
  - Is all of the user data collected by your app encrypted in transit? — не
    применимо (данные не собираются); запросы к целям пользователя могут идти по http
    по его выбору.
  - Do you provide a way for users to request that their data is deleted? — не
    применимо; все данные удаляются вместе с приложением.
- **Export compliance (App Store):** `ITSAppUsesNonExemptEncryption = false` в
  Info.plist — только системный HTTPS, своей криптографии нет.

## Рейтинг контента

- **App Store (Age Rating):** все пункты «None», нет неограниченного доступа к вебу
  (приложение не показывает веб-контент) → 4+.
- **Google Play (IARC):** категория «Utility, Productivity, Communication, or Other»;
  насилия, сексуального контента, языка, контролируемых веществ, азартных игр нет;
  пользователи не общаются и не обмениваются контентом; местоположение не передаётся;
  цифровых покупок нет → Everyone / PEGI 3.
- **Google Play → Target audience:** 18+ (инструмент разработчика), не для детей.
- **Google Play → Ads:** No ads. **News app:** No. **Government app:** No.
- **Google Play → App access:** All functionality is available without special access.
