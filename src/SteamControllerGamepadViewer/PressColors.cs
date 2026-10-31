using System.Text.Json;
using System.Text.RegularExpressions;

namespace SteamControllerGamepadViewer;

internal sealed record PressColorConfig
{
    public string Mode { get; init; } = "global";
    public string GlobalColor { get; init; } = "#25a7ff";
    public Dictionary<string, string> Controls { get; init; } = new();
    public string Resolve(string control) => Mode == "per-control" && Controls.TryGetValue(control, out var color) ? color : GlobalColor;
}

internal sealed class PressColorStore
{
    private readonly object gate = new();
    private readonly string path;
    private PressColorConfig current;
    private bool configured;
    public bool HasSavedSettings { get { lock (gate) return configured; } }
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public static string DefaultPath => Path.Combine(Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") is { Length: > 0 } config && Path.IsPathRooted(config)
        ? config : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config"), "SteamControllerGamepadViewer", "press-colors.json");
    public static readonly string[] ControlNames = DiscoverControls();
    public PressColorStore() : this(DefaultPath) { }
    internal PressColorStore(string path)
    {
        this.path = path;
        configured = File.Exists(path);
        try { current = Parse(File.ReadAllText(path)); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { current = new(); }
    }
    public PressColorConfig Current { get { lock (gate) return current with { Controls = new(current.Controls) }; } }
    public static PressColorConfig Parse(string json)
    {
        try { return Validate(JsonSerializer.Deserialize<PressColorConfig>(json, JsonOptions) ?? new()); }
        catch (Exception ex) when (ex is JsonException or ArgumentException) { return new(); }
    }
    public static PressColorConfig Validate(PressColorConfig value)
    {
        if (value.Mode is not ("global" or "per-control") || !IsColor(value.GlobalColor) || value.Controls is null)
            throw new ArgumentException("Choose a valid mode and #RRGGBB colors.");
        var controls = new Dictionary<string, string>();
        foreach (var (key, color) in value.Controls)
        {
            if (!ControlNames.Contains(key) || !IsColor(color)) throw new ArgumentException("Unknown control or invalid color: " + key);
            controls[key] = color.ToLowerInvariant();
        }
        return value with { GlobalColor = value.GlobalColor.ToLowerInvariant(), Controls = controls };
    }
    public PressColorConfig Save(PressColorConfig value)
    {
        value = Validate(value);
        lock (gate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, JsonSerializer.Serialize(value, JsonOptions));
                File.Move(temporary, path, true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            current = value;
            configured = true;
            return Current;
        }
    }
    public PressColorConfig Reset() => Save(new());
    private static bool IsColor(string? color) => color is not null && Regex.IsMatch(color, "^#[0-9a-fA-F]{6}$");
    private static string[] DiscoverControls()
    {
        EmbeddedWebAssets.TryReadText("index.html", out var html);
        var names = Regex.Matches(html, "data-control=\"([^\"]+)\"").Select(m => m.Groups[1].Value);
        return names.Concat(new[] { "left-stick-touch", "right-stick-touch", "left-pad-touch", "left-pad-click", "right-pad-touch", "right-pad-click" }).Distinct().Order().ToArray();
    }
}
