using System.Text.Json;
using System.Text.Json.Serialization;

namespace TimeboxBell.Core;

/// <summary>Nastavení nejde použít. Zpráva je určená uživateli – říká, který soubor, kde a co je špatně.</summary>
public sealed class SettingsException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// Načítá <see cref="TimeboxBellSettings"/> z JSON. Chybějící soubor nebo klíč znamená výchozí
/// hodnotu; nevalidní obsah vede na <see cref="SettingsException"/>.
/// </summary>
/// <remarks>
/// Formát:
/// <code>
/// {
///   "Bells": [ { "Minute": 23, "Type": "Warning" }, ... ],
///   "Sounds": { "Warning": "C:\\Windows\\Media\\...wav", ... },
///   "PreventSleep": true
/// }
/// </code>
/// Komentáře a koncové čárky jsou povolené, názvy klíčů nerozlišují velikost písmen. Neznámé klíče
/// jsou chyba, aby se překlep neztratil. Uvedený seznam <c>Bells</c> nahradí celý výchozí rozvrh;
/// <c>Sounds</c> nahradí jen uvedené druhy. Relativní cesta ke zvuku se bere vůči složce souboru
/// nastavení, prázdná cesta znamená jen pípnutí.
/// </remarks>
public static class SettingsLoader
{
    /// <summary>Název souboru nastavení.</summary>
    public const string FileName = "appsettings.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    private static readonly string AllowedTypes = string.Join(", ", Enum.GetNames<BellType>());

    /// <summary>Cesta k appsettings.json vedle spuštěného programu.</summary>
    public static string DefaultPath => Path.Combine(AppContext.BaseDirectory, FileName);

    /// <summary>Načte nastavení ze souboru; když soubor neexistuje, vrátí výchozí nastavení.</summary>
    /// <exception cref="SettingsException">Soubor nejde přečíst nebo je nevalidní.</exception>
    public static TimeboxBellSettings Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            return TimeboxBellSettings.Default;
        }

        string json;
        try
        {
            json = File.ReadAllText(fullPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new SettingsException($"Soubor nastavení {fullPath} nejde přečíst: {ex.Message}", ex);
        }

        return Parse(json, Path.GetDirectoryName(fullPath)!, fullPath);
    }

    /// <summary>Zpracuje obsah souboru nastavení.</summary>
    /// <param name="json">Obsah souboru.</param>
    /// <param name="baseDirectory">Složka, vůči které se berou relativní cesty ke zvukům.</param>
    /// <param name="source">Název zdroje do chybových zpráv (obvykle cesta k souboru).</param>
    /// <exception cref="SettingsException">Obsah je nevalidní.</exception>
    public static TimeboxBellSettings Parse(string json, string baseDirectory, string source)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(baseDirectory);

        SettingsDto? dto;
        try
        {
            dto = string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<SettingsDto>(json, Options);
        }
        catch (JsonException ex)
        {
            var where = ex.LineNumber is { } line ? $" na řádku {line + 1}" : string.Empty;
            var path = string.IsNullOrEmpty(ex.Path) || ex.Path == "$" ? string.Empty : $" ({ex.Path})";
            throw new SettingsException($"Soubor nastavení {source} je chybný{where}{path}: {ex.Message}", ex);
        }

        var defaults = TimeboxBellSettings.Default;
        if (dto is null)
        {
            return defaults;
        }

        return new TimeboxBellSettings(
            dto.Bells is null ? defaults.Schedule : ParseSchedule(dto.Bells, source),
            dto.Sounds is null ? defaults.Sounds : ParseSounds(dto.Sounds, baseDirectory, source),
            dto.PreventSleep ?? defaults.PreventSleep);
    }

    private static BellSchedule ParseSchedule(List<BellDto?> bells, string source)
    {
        var rules = new List<BellRule>();
        var seen = new Dictionary<int, int>();
        for (var i = 0; i < bells.Count; i++)
        {
            var where = $"Soubor nastavení {source}: Bells[{i}]";
            var bell = bells[i] ?? throw new SettingsException($"{where} je prázdné (null).");
            if (bell.Minute is not { } minute)
            {
                throw new SettingsException($"{where}: chybí \"Minute\" (0–59).");
            }

            if (minute is < 0 or > 59)
            {
                throw new SettingsException($"{where}: minuta {minute} musí být v rozsahu 0–59.");
            }

            if (seen.TryGetValue(minute, out var first))
            {
                throw new SettingsException($"{where}: minuta {minute} už je v Bells[{first}].");
            }

            if (string.IsNullOrWhiteSpace(bell.Type))
            {
                throw new SettingsException($"{where}: chybí \"Type\" ({AllowedTypes}).");
            }

            rules.Add(new BellRule(minute, ParseType(bell.Type, where)));
            seen[minute] = i;
        }

        return new BellSchedule(rules);
    }

    private static BellSounds ParseSounds(Dictionary<string, string?> sounds, string baseDirectory, string source)
    {
        var paths = Enum.GetValues<BellType>().ToDictionary(t => t, t => BellSounds.Default.GetPath(t) ?? string.Empty);
        foreach (var (key, value) in sounds)
        {
            var type = ParseType(key, $"Soubor nastavení {source}: Sounds");
            if (value is null)
            {
                throw new SettingsException(
                    $"Soubor nastavení {source}: Sounds.{key} je null; pro pípnutí použijte \"\".");
            }

            try
            {
                paths[type] = value.Trim().Length == 0 ? string.Empty : Path.GetFullPath(value, baseDirectory);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                throw new SettingsException($"Soubor nastavení {source}: Sounds.{key} není platná cesta: {value}", ex);
            }
        }

        return new BellSounds(paths);
    }

    private static BellType ParseType(string text, string where)
    {
        // Jen pojmenované hodnoty – Enum.TryParse by přijal i čísla jako "7".
        foreach (var type in Enum.GetValues<BellType>())
        {
            if (string.Equals(type.ToString(), text.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return type;
            }
        }

        throw new SettingsException($"{where}: neznámý druh zvonění \"{text}\" (povolené: {AllowedTypes}).");
    }

    private sealed class SettingsDto
    {
        public List<BellDto?>? Bells { get; set; }

        public Dictionary<string, string?>? Sounds { get; set; }

        public bool? PreventSleep { get; set; }
    }

    private sealed class BellDto
    {
        public int? Minute { get; set; }

        public string? Type { get; set; }
    }
}
