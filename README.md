# TimeboxBell
Windows chime for timeboxing: plays a sound at :00 and :30 plus warnings 7 and 5 minutes before, and keeps your PC awake while running

## Konzolová aplikace

```powershell
pwsh build/smoke-test.ps1        # publikuje artifacts/publish/console/TimeboxBell.Console.exe a ověří výpis rozvrhu
TimeboxBell.Console.exe          # zvoní podle rozvrhu a brání uspání; Ctrl+C ukončí
TimeboxBell.Console.exe --list-schedule
```

Publikovaný `.exe` je jeden soubor včetně .NET runtime – nevyžaduje instalaci ani administrátorská práva.
