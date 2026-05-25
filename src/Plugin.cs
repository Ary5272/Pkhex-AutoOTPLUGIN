using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using PKHeX.Core;

namespace OTHandlerPlugin;

/// <summary>
/// PKHeX plugin: one-click OT/HT ownership pass for the loaded boxes — all entity formats.
/// Owner trainer comes from Settings (auto-detected as the most common OT+ID in the boxes, or set manually).
///   - Owner's Pokemon  -> cleared to pure-OT (no HT) if it stays legal.
///   - Everything else  -> Handling Trainer set to the owner (formats that support an HT).
///   - Anything that would become illegal, or formats without an HT, are left exactly as-is.
/// Works across PK6/PK7/PB7/PK8/PB8/PA8/PK9 via base PKM + per-generation interfaces.
/// </summary>
public sealed class OTHandlerPlugin : IPlugin
{
    public string Name => "OT/HT Ownership";
    public int Priority => 1;
    public ISaveFileProvider SaveFileEditor { get; private set; }

    private PluginConfig _cfg = PluginConfig.Load();

    public void Initialize(params object[] args)
    {
        SaveFileEditor = args.OfType<ISaveFileProvider>().FirstOrDefault();
        var menu = args.OfType<ToolStrip>().FirstOrDefault();
        if (menu != null)
            AddPluginControl(menu);
    }

    private void AddPluginControl(ToolStrip menuStrip)
    {
        var parent = new ToolStripMenuItem(Name);
        var apply = new ToolStripMenuItem("Apply to boxes");
        apply.Click += (_, _) => Run();
        var settings = new ToolStripMenuItem("Settings...");
        settings.Click += (_, _) => OpenSettings();
        parent.DropDownItems.Add(apply);
        parent.DropDownItems.Add(settings);

        var tools = menuStrip.Items.Find("Menu_Tools", false).FirstOrDefault() as ToolStripDropDownItem;
        if (tools != null)
            tools.DropDownItems.Add(parent);
        else
            menuStrip.Items.Add(parent);
    }

    public void NotifySaveLoaded() { }
    public void NotifyDisplayLanguageChanged(string language) { }
    public bool TryLoadFile(string filePath) => false;

    private void OpenSettings()
    {
        using var f = new SettingsForm(_cfg);
        f.ShowDialog();
    }

    private void Run()
    {
        var sav = SaveFileEditor?.SAV;
        if (sav == null)
        {
            MessageBox.Show("No save file is loaded.", Name);
            return;
        }

        var data = sav.BoxData;

        string ownerName;
        uint ownerID32;
        byte ownerGender;
        byte ownerLang;
        string mode;

        if (_cfg.AutoDetect)
        {
            var top = data.Where(p => p != null && p.Species != 0)
                          .GroupBy(p => (p.OriginalTrainerName, p.ID32))
                          .OrderByDescending(g => g.Count())
                          .FirstOrDefault();
            if (top == null)
            {
                MessageBox.Show("No Pokémon in the boxes to read a trainer from. Load your collection first.", Name);
                return;
            }
            var rep = top.First();
            ownerName = rep.OriginalTrainerName;
            ownerID32 = rep.ID32;
            ownerGender = (byte)rep.OriginalTrainerGender;
            ownerLang = (byte)rep.Language;
            mode = "auto-detected";
        }
        else
        {
            if (string.IsNullOrWhiteSpace(_cfg.OT))
            {
                MessageBox.Show("No trainer is set. Open Settings… and enter one (or tick Auto-detect).", Name);
                return;
            }
            ownerName = _cfg.OT;
            ownerID32 = _cfg.ID32;
            ownerGender = (byte)_cfg.Gender;
            ownerLang = (byte)_cfg.Language;
            mode = "from Settings";
        }

        int pure = 0, htSet = 0, kept = 0, total = 0;
        for (int i = 0; i < data.Count; i++)
        {
            var pk = data[i];
            if (pk == null || pk.Species == 0)
                continue;
            total++;
            data[i] = Choose(pk, ownerName, ownerID32, ownerGender, ownerLang, ref pure, ref htSet, ref kept);
        }
        sav.BoxData = data;
        SaveFileEditor.ReloadSlots();

        string dumpMsg = "";
        if (_cfg.DumpAfterApply)
        {
            string folder = _cfg.DumpFolder;
            if (string.IsNullOrWhiteSpace(folder))
            {
                using var fbd = new FolderBrowserDialog { Description = "Folder to dump the boxes into" };
                folder = fbd.ShowDialog() == DialogResult.OK ? fbd.SelectedPath : null;
            }
            if (!string.IsNullOrWhiteSpace(folder))
            {
                int dumped = DumpBoxes(sav, folder);
                dumpMsg = $"\nDumped {dumped} file(s) to:\n{folder}\n";
            }
        }

        uint dispTID = ownerID32 % 1_000_000u;
        uint dispSID = ownerID32 / 1_000_000u;
        MessageBox.Show(
            $"Owner ({mode}): {ownerName}  (TID {dispTID} / SID {dispSID})\n\n" +
            $"Processed {total} Pokémon:\n\n" +
            $"   {pure}  owner's  -> pure-OT (HT cleared, owner is the handler)\n" +
            $"   {htSet}  others   -> HT = {ownerName}\n" +
            $"   {kept}  kept as-is (would be illegal, or no HT support)\n" +
            dumpMsg + "\n" +
            "All results are legality-checked." +
            (_cfg.DumpAfterApply ? "" : " Remember to save / export your boxes."),
            Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static PKM Choose(PKM orig, string ownerName, uint ownerID32, byte ownerGender, byte ownerLang,
                              ref int pure, ref int htSet, ref int kept)
    {
        bool mine = orig.OriginalTrainerName == ownerName && orig.ID32 == ownerID32;
        bool hasHT = orig.Format >= 6; // Handling Trainer exists from Gen 6 onward

        var pk = orig.Clone();
        if (mine)
        {
            pk.CurrentHandler = 0;
            if (hasHT)
            {
                pk.HandlingTrainerName = string.Empty;
                pk.HandlingTrainerGender = 0;
                pk.HandlingTrainerFriendship = 0;
                if (pk is IHandlerLanguage hl) hl.HandlingTrainerLanguage = 0;
                if (pk is IMemoryHT m)
                {
                    m.HandlingTrainerMemory = 0;
                    m.HandlingTrainerMemoryIntensity = 0;
                    m.HandlingTrainerMemoryFeeling = 0;
                    m.HandlingTrainerMemoryVariable = 0;
                }
                TrySet(pk, "HandlingTrainerID", 0);
            }
        }
        else
        {
            if (!hasHT) { kept++; return orig; } // can't re-home a foreign-OT mon without an HT slot
            pk.HandlingTrainerName = ownerName;
            pk.HandlingTrainerGender = ownerGender;
            pk.HandlingTrainerFriendship = 255;
            pk.CurrentHandler = 1;
            if (pk is IHandlerLanguage hl) hl.HandlingTrainerLanguage = ownerLang;
        }
        pk.RefreshChecksum();

        if (new LegalityAnalysis(pk).Valid)
        {
            if (mine) pure++; else htSet++;
            return pk;
        }
        kept++;
        return orig;
    }

    // set a property by name if the entity has it (e.g. HandlingTrainerID, which has no shared interface)
    private static void TrySet(object o, string prop, object val)
    {
        var p = o.GetType().GetProperty(prop);
        if (p != null && p.CanWrite)
            p.SetValue(o, Convert.ChangeType(val, p.PropertyType));
    }

    private static int DumpBoxes(SaveFile sav, string folder)
    {
        Directory.CreateDirectory(folder);
        int n = 0;
        foreach (var pk in sav.BoxData)
        {
            if (pk == null || pk.Species == 0)
                continue;
            File.WriteAllBytes(Path.Combine(folder, pk.FileName), pk.Data.ToArray());
            n++;
        }
        return n;
    }
}
