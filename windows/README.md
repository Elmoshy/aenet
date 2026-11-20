# ÆNET for Windows

Native WPF app on .NET Framework 4.8 (included in Windows 10 1903+ and Windows 11). It writes an Xray config, runs
`xray.exe` hidden, and switches the Windows system proxy (127.0.0.1:10809 http, 10808 socks). A Windows Job Object makes
sure xray dies with the app.

    dotnet publish BatV2.csproj -c Release -o out
    # put xray.exe in dist\xray\ next to BatV2.exe, then:
    ISCC.exe installer\BatV2.iss

Not yet: TUN (system wide) mode, tray icon, true delay test.
