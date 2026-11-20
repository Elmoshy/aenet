# Contributing

Thanks for helping. Keep changes small and focused, one topic per pull request.

## Layout
- `android/`  Kotlin app. Build with Gradle 8.7 and JDK 17. The engine module is fetched by `android/scripts/fetch_v2ray.sh`.
- `windows/`  C# WPF app on .NET Framework 4.8 plus the Inno Setup script in `windows/installer`.
- `docs/`     screenshots and notes.

## Rules
- Parser or config-builder changes need a test (`android/tests/CoreTest.kt` for Android).
- Never commit servers, links with real credentials, keystores (`*.keystore`), certificates (`*.pfx`) or logs.
- Do not add analytics, ads or telemetry.
- Use the GitHub noreply email for commits if you want to keep your address private.

## Releases
Maintainers push a tag like `v1.0.0`. The `Release` workflow builds Android and Windows and publishes
the assets with a `SHA256SUMS.txt` file.
