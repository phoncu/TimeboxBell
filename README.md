# TimeboxBell
Windows chime for timeboxing: plays a sound at :00 and :30 plus warnings 7 and 5 minutes before, and keeps your PC awake while running

## Použití

`TimeboxBell.exe` běží v oznamovací oblasti. Menu ikony ukazuje čas dalšího zvonění, umí zvonění
pozastavit (schůzka, oběd) a program ukončit. Při pauze nic nezvoní a po pokračování se zameškaná
zvonění nepřehrávají. Dokud program běží, počítač se neuspí a obrazovka nezhasne.

```powershell
dotnet publish src/TimeboxBell.Tray -c Release -o artifacts/publish/tray
```

Publikovaný `.exe` je jeden soubor včetně .NET runtime – nevyžaduje instalaci ani administrátorská práva.

## Nastavení

Soubor `appsettings.json` vedle `.exe` (změna se projeví po restartu programu):

```jsonc
{
  "Bells": [ { "Minute": 23, "Type": "Warning" }, { "Minute": 25, "Type": "PomodoroEnd" }, ... ],
  "Sounds": { "Warning": "C:\\Windows\\Media\\Windows Notify Calendar.wav", ... },
  "PreventSleep": true
}
```

- `Type`: `Warning`, `PomodoroEnd` nebo `NewHalfHour`. Uvedený seznam `Bells` nahradí celý výchozí rozvrh.
- `Sounds`: relativní cesta se bere vůči složce s `appsettings.json`, `""` znamená jen pípnutí.
  Chybějící nebo nepřehratelný zvuk také vyvolá pípnutí.
- Chybějící soubor nebo klíč = výchozí hodnota. Chyba v souboru (špatný JSON, minuta mimo 0–59,
  neznámý klíč…) se ohlásí srozumitelnou zprávou a program se nespustí.

## Konzolová aplikace

```powershell
pwsh build/smoke-test.ps1        # publikuje artifacts/publish/console/TimeboxBell.Console.exe a ověří výpis rozvrhu
TimeboxBell.Console.exe          # zvoní podle rozvrhu a brání uspání; Ctrl+C ukončí
TimeboxBell.Console.exe --list-schedule   # vypíše načtené nastavení a skončí
```
