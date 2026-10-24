# Публикация в App Store и Google Play

Актуально на 2026-10-24 (1.0.0). Тексты витрины (описания ru/en, ключевые слова,
категории) — в umbrella-репо: `docs/marketing/netbench-store-listing.md`. Здесь —
графика, ответы на анкеты сторов, заметки для ревью и порядок сборки релиза.

```
store/
├── google-play/   icon-512.png, feature-graphic.png, screenshots/
└── app-store/     screenshots/
```

Иконки и feature graphic генерируются: `python3 scripts/generate_icons.py`,
`python3 scripts/generate_store_graphics.py` (нужен Pillow).

## Идентичность и версии

- `applicationId` / Bundle ID — `net.plumix.netbench` (`NetBenchApplicationId` в
  `NetBench/Directory.Build.props`). После публикации менять нельзя.
- Версия — `NetBenchVersion` (видна пользователю), build number — `NetBenchBuildNumber`
  (versionCode / CFBundleVersion, только растёт). CI передаёт
  `-p:NetBenchBuildNumber=${{ github.run_number }}`; локальный релиз — номер больше
  последнего загруженного.

## Сборка релиза

### Google Play (AAB)

Upload-ключ лежит вне репозитория: `~/.android-keys/netbench-upload.jks`, алиас
`netbench-upload`, пароль — в `~/.android-keys/netbench-upload.password`.
**Сделайте резервную копию обоих файлов** (менеджер паролей) — без upload-ключа
обновления придётся вести через сброс ключа в Play Console. Ключ приложения хранит
Google (Play App Signing — включить при первой загрузке).

```bash
export NETBENCH_KEYSTORE=~/.android-keys/netbench-upload.jks
export NETBENCH_KEY_ALIAS=netbench-upload
export NETBENCH_KEYSTORE_PASSWORD="$(cat ~/.android-keys/netbench-upload.password)"
dotnet publish NetBench/NetBench.Android/NetBench.Android.csproj -c Release -p:NetBenchBuildNumber=2
# → NetBench/NetBench.Android/bin/Release/net10.0-android/publish/net.plumix.netbench-Signed.aab
```

Для CI: секреты `ANDROID_KEYSTORE_BASE64` (`base64 -i netbench-upload.jks`),
`ANDROID_KEY_ALIAS`, `ANDROID_KEYSTORE_PASSWORD` — job `build-android-release`.

Новый личный аккаунт Play: перед production нужен закрытый тест (не меньше
12 тестировщиков 14 дней подряд), затем заявка на production-доступ.

### App Store (IPA)

1. developer.apple.com → Identifiers: App ID `net.plumix.netbench` (уже заведён
   вместе с приложением в App Store Connect). Capabilities не нужны.
2. Profiles → App Store Connect distribution profile для этого App ID, скачать и
   открыть двойным кликом (ляжет в `~/Library/Developer/Xcode/UserData/Provisioning Profiles`).
3. Сборка (сертификат «Apple Distribution» уже в связке ключей):

```bash
rm -rf NetBench/NetBench.iOS/{bin,obj}
dotnet publish NetBench/NetBench.iOS/NetBench.iOS.csproj -c Release -p:NetBenchBuildNumber=2 \
  -p:CodesignProvision="<имя профиля>"
# → NetBench/NetBench.iOS/bin/Release/net10.0-ios/ios-arm64/publish/NetBench.iOS.ipa
```

4. Загрузка — Transporter (Mac App Store) или
   `xcrun altool --upload-app -f <ipa> -t ios --apiKey … --apiIssuer …`.

## Скриншоты

Сценарий съёмки (одинаковый для обоих сторов, светлая и тёмная темы по желанию):
главный экран со сценариями и историей → быстрый тест с пресетами → монитор во время
прогона → результат → шторка «Поделиться».

- **Google Play:** соотношение сторон не больше 2:1, поэтому эмулятор Pixel_6a
  (1080×2400) временно переводим в 1080×2160: `adb shell wm size 1080x2160`, снимаем
  `adb exec-out screencap -p > store/google-play/screenshots/01-home.png`, затем
  `adb shell wm size reset`.
- **App Store:** обязательный размер 6.9" — симулятор iPhone 17 Pro Max (1320×2868),
  `xcrun simctl io booted screenshot store/app-store/screenshots/01-home.png`.

Цель для живого прогона на скриншотах — свой сервер (например,
`python3 -m http.server` на Mac: `http://10.0.2.2:PORT` с эмулятора,
`http://localhost:PORT` с симулятора).

## App Review notes (App Store Connect → App Review Information)

> NetBench is an HTTP load-testing tool for developers to measure the performance of
> their own APIs. It sends HTTP requests only to the URL the user enters and shows
> throughput, latency percentiles and error rate.
>
> Safeguards against misuse: before the first run the app asks the user to confirm that
> they test only servers they own or are allowed to test; runs from the phone are limited
> to 50 concurrent virtual users and 5 minutes. The app has no account, no backend of its
> own, no analytics and no tracking.
>
> NSAllowsArbitraryLoads is enabled because developers test local and staging servers
> that are often served over plain http:// (for example http://192.168.1.10:8080);
> NSLocalNetworkUsageDescription covers targets on the local network. The app never
> loads web content and only contacts the address the user typed.
>
> How to test: tap "Quick test", enter https://httpbin.org/get (or any server you
> control), keep 10 users / 30 s and tap "Run". Confirm the "Test only your own servers"
> dialog. The result screen opens when the run finishes; "Share report" offers text,
> HTML, CSV and JSON.

Sign-in: не требуется. Contact: владелец аккаунта App Store Connect.

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
