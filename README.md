# TimeboxBell
Windows chime for timeboxing: plays a sound at :00 and :30 plus warnings 7 and 5 minutes before, and keeps your PC awake while running

## Usage

`TimeboxBell.exe` runs in the notification area. The icon menu shows the time of the next bell, lets you
pause the bells (meeting, lunch) and exit the program. While paused nothing rings, and bells missed during
the pause are not played after resuming. While the program is running, the PC does not go to sleep and
the display does not turn off.

```powershell
dotnet publish src/TimeboxBell.Tray -c Release -o artifacts/publish/tray
```

The published `.exe` is a single file including the .NET runtime – it needs no installation and no
administrator rights.

Only one instance rings per signed-in user: starting `TimeboxBell.exe` or the console app while another
instance is already running shows a message and exits (exit code 3).

## Settings

The `appsettings.json` file next to the `.exe` (changes take effect after restarting the program):

```jsonc
{
  "Bells": [ { "Minute": 23, "Type": "Warning" }, { "Minute": 25, "Type": "PomodoroEnd" }, ... ],
  "Sounds": { "Warning": "C:\\Windows\\Media\\Windows Notify Calendar.wav", ... },
  "PreventSleep": true
}
```

- `Type`: `Warning`, `PomodoroEnd` or `NewHalfHour`. A `Bells` list replaces the entire default schedule.
- `Sounds`: a relative path is resolved against the folder containing `appsettings.json`; `""` means just a beep.
  A missing or unplayable sound also results in a beep.
- A missing file or key means the default value. An error in the file (invalid JSON, minute outside 0–59,
  unknown key…) is reported with a clear message and the program does not start.

## Console app

```powershell
pwsh build/smoke-test.ps1        # publishes artifacts/publish/console/TimeboxBell.Console.exe and checks the schedule listing and single instance
TimeboxBell.Console.exe          # rings according to the schedule and prevents sleep; Ctrl+C exits
TimeboxBell.Console.exe --list-schedule   # prints the loaded settings and exits
```
