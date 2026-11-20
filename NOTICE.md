# Third party components

ÆNET's own code is MIT licensed (see `LICENSE`). The released binaries also contain or download the
components below, which keep their own licenses.

| Component | Used in | License | Source |
|---|---|---|---|
| Xray-core (`xray.exe`, unmodified, v25.5.16) | Windows | MPL-2.0 | https://github.com/XTLS/Xray-core |
| AndroidLibXrayLite (`libv2ray`) | Android | LGPL-3.0 | https://github.com/dev7dev/AndroidLibXrayLite |
| V2ray-Android engine module (fetched at build time, version frozen in `android/engine.ref`) | Android | see upstream repository | https://github.com/dev7dev/V2ray-Android |
| System.Text.Json | Windows | MIT | https://github.com/dotnet/runtime |
| Inno Setup (used to build the installer, not shipped) | Windows | Inno Setup License | https://jrsoftware.org/isinfo.php |

Notes
- Xray-core is MPL-2.0: the exact upstream release used is linked in `.github/workflows/windows.yml`. We do not modify it.
- AndroidLibXrayLite is LGPL-3.0: it is linked as a separate library (`.aar`). Its source is available at the link above.
- Each upstream project's own credits apply (see their repositories).
