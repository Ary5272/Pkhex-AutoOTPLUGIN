using System.IO;
using System.Text.Json;

namespace OTHandlerPlugin;

public sealed class PluginConfig
{
    public bool AutoDetect { get; set; } = true;
    public string OT { get; set; } = "";
    public uint TID { get; set; }            // display TID (Gen7+: ID32 % 1_000_000)
    public uint SID { get; set; }            // display SID (Gen7+: ID32 / 1_000_000)
    public int Gender { get; set; }          // 0 = male, 1 = female
    public int Language { get; set; } = 2;   // 2 = ENG
    public bool DumpAfterApply { get; set; } = false;
    public string DumpFolder { get; set; } = "";

    public uint ID32 => (SID * 1_000_000u) + TID;

    private static string ConfigPath
    {
        get
        {
            var dir = Path.GetDirectoryName(typeof(PluginConfig).Assembly.Location);
            return Path.Combine(string.IsNullOrEmpty(dir) ? "." : dir, "OTHandlerPlugin.json");
        }
    }

    public static PluginConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
                return JsonSerializer.Deserialize<PluginConfig>(File.ReadAllText(ConfigPath)) ?? new PluginConfig();
        }
        catch { /* fall through to default */ }
        return new PluginConfig();
    }

    public void Save()
    {
        try { File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true })); }
        catch { /* non-fatal */ }
    }
}
