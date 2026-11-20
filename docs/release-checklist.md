# Ruční akceptační checklist před zveřejněním release

Skutečné uspání a probuzení PC, zhasínání obrazovky a spořič automaticky otestovat nejde.
Workflow `Release` proto zakládá release jen jako koncept (draft). Zveřejni ho až po projití
tohoto checklistu.

Testuj na firemním PC pod běžným účtem. Použij `TimeboxBell.exe` a `appsettings.json`
stažené z konceptu release do jedné složky, ne lokální build.

## Příprava

- [ ] Žádná jiná instance TimeboxBell neběží.
- [ ] Zjisti nastavení napájení: `powercfg /query SCHEME_CURRENT SUB_SLEEP STANDBYIDLE` a
      `powercfg /query SCHEME_CURRENT SUB_VIDEO VIDEOIDLE`. Pro test si můžeš dočasně nastavit
      krátké časy, například `powercfg /change standby-timeout-ac 2` a
      `powercfg /change monitor-timeout-ac 1`. Původní hodnoty si zapiš a na konci je vrať.

## 1. Spuštění bez administrátorských práv

- [ ] `TimeboxBell.exe` spuštěný dvojklikem z běžného účtu se rozběhne. Neobjeví se výzva UAC
      ani hláška o chybějícím .NET a ikona se ukáže v oznamovací oblasti.

## 2. Zvonění ve správný čas právě jednou

- [ ] Nech program běžet aspoň přes jednu celou půlhodinu (třeba :20–:31).
- [ ] V :23 zazní Warning, v :25 PomodoroEnd a v :30 NewHalfHour. Každý zvuk zazní jednou,
      nejvýš pár sekund po celé minutě.
- [ ] Menu u ikony ukazuje správný čas dalšího zvonění.

## 3. Chybějící zvuk znamená pípnutí bez pádu

- [ ] Ukonči program. V `appsettings.json` změň jednu cestu v `Sounds` na neexistující
      soubor (třeba `"Warning": "neni.wav"`) a nastav jedno zvonění na nejbližší minutu.
- [ ] Spusť program. Ve zvolenou minutu se ozve pípnutí a program běží dál.
- [ ] Vrať `appsettings.json` do původního stavu.

## 4. Spánek a obrazovka

- [ ] Program běží a nikdo na PC nesahá déle, než je časový limit spánku i obrazovky.
      PC se neuspí a obrazovka nezhasne.
- [ ] `powercfg /requests` ukazuje TimeboxBell v sekcích SYSTEM a DISPLAY.
- [ ] Ukonči program přes menu ikony. `powercfg /requests` už TimeboxBell neukazuje
      a po uplynutí limitu se PC normálně uspí (nebo zhasne obrazovka).
- [ ] Spořič obrazovky a zamčení obrazovky nastavené politikou program záměrně neblokuje.
      Pokud se spustí, není to chyba. Zvonění ale musí znít dál.

## 5. Zvonění po probuzení ze spánku

- [ ] Spusť program a ručně uspi PC (Start → Napájení → Režim spánku).
- [ ] Po pár minutách PC probuď. Zvonění, která připadla na dobu spánku, se dodatečně
      nepřehrají.
- [ ] Nejbližší další zvonění po probuzení zazní ve správný čas.

## Na konec

- [ ] Vrať původní nastavení napájení (`powercfg /change …`).
- [ ] Zveřejni koncept release na GitHubu (Releases → Edit → Publish release).
