# ÆNET for Android

Kotlin app. Local build:

    bash scripts/fetch_v2ray.sh     # pulls the engine module at the commit in engine.ref
    gradle :app:assembleRelease     # Gradle 8.7, JDK 17

Release signing: set the secrets `KEYSTORE_B64`, `KEYSTORE_PASSWORD`, `KEY_ALIAS`, `KEY_PASSWORD` in the repository.
Without them CI builds with a temporary key (updates then need an uninstall first). Tagged releases refuse to build without them.

`engine.ref` freezes the engine commit. The CI summary prints the commit that was used.
