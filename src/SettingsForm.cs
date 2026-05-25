using System;
using System.Windows.Forms;

namespace OTHandlerPlugin;

public sealed class SettingsForm : Form
{
    private readonly CheckBox chkAuto = new();
    private readonly TextBox txtOT = new();
    private readonly NumericUpDown numTID = new();
    private readonly NumericUpDown numSID = new();
    private readonly ComboBox cmbGender = new();
    private readonly ComboBox cmbLang = new();
    private readonly CheckBox chkDump = new();
    private readonly TextBox txtDumpFolder = new();
    private readonly Button btnBrowse = new();
    private readonly PluginConfig cfg;

    private static readonly int[] LangIds = { 1, 2, 3, 4, 5, 7, 8, 9, 10 };
    private static readonly string[] LangNames = { "JPN", "ENG", "FRE", "ITA", "GER", "SPA", "KOR", "CHS", "CHT" };

    public SettingsForm(PluginConfig config)
    {
        cfg = config;
        Text = "OT/HT Ownership - Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new System.Drawing.Size(340, 320);

        chkAuto.Text = "Auto-detect owner from loaded boxes";
        chkAuto.SetBounds(14, 12, 310, 22);
        chkAuto.CheckedChanged += (_, _) => ToggleManual();

        AddLabel("OT name:", 50);
        txtOT.SetBounds(130, 47, 190, 23);
        txtOT.MaxLength = 12;

        AddLabel("TID (display):", 80);
        numTID.SetBounds(130, 77, 110, 23);
        numTID.Maximum = 999999;

        AddLabel("SID (display):", 110);
        numSID.SetBounds(130, 107, 110, 23);
        numSID.Maximum = 4294;

        AddLabel("Gender:", 140);
        cmbGender.SetBounds(130, 137, 110, 23);
        cmbGender.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbGender.Items.AddRange(new object[] { "Male", "Female" });

        AddLabel("Language:", 170);
        cmbLang.SetBounds(130, 167, 110, 23);
        cmbLang.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbLang.Items.AddRange(LangNames);

        chkDump.Text = "Dump boxes to a folder after applying";
        chkDump.SetBounds(14, 208, 310, 22);
        chkDump.CheckedChanged += (_, _) => ToggleDump();

        AddLabel("Folder:", 238);
        txtDumpFolder.SetBounds(70, 235, 180, 23);
        btnBrowse.Text = "Browse...";
        btnBrowse.SetBounds(256, 234, 70, 25);
        btnBrowse.Click += (_, _) => Browse();

        var ok = new Button { Text = "Save", DialogResult = DialogResult.OK };
        ok.SetBounds(160, 278, 80, 30);
        ok.Click += (_, _) => Apply();
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
        cancel.SetBounds(246, 278, 80, 30);

        Controls.AddRange(new Control[]
        {
            chkAuto, txtOT, numTID, numSID, cmbGender, cmbLang,
            chkDump, txtDumpFolder, btnBrowse, ok, cancel
        });
        AcceptButton = ok;
        CancelButton = cancel;

        // populate
        chkAuto.Checked = cfg.AutoDetect;
        txtOT.Text = cfg.OT;
        numTID.Value = Math.Min(cfg.TID, 999999u);
        numSID.Value = Math.Min(cfg.SID, 4294u);
        cmbGender.SelectedIndex = cfg.Gender == 1 ? 1 : 0;
        cmbLang.SelectedIndex = LangToIndex(cfg.Language);
        chkDump.Checked = cfg.DumpAfterApply;
        txtDumpFolder.Text = cfg.DumpFolder;
        ToggleManual();
        ToggleDump();
    }

    private void AddLabel(string text, int y)
    {
        var l = new Label { Text = text };
        l.SetBounds(14, y, 110, 20);
        Controls.Add(l);
    }

    private void ToggleManual()
    {
        bool manual = !chkAuto.Checked;
        txtOT.Enabled = numTID.Enabled = numSID.Enabled = cmbGender.Enabled = cmbLang.Enabled = manual;
    }

    private void ToggleDump()
    {
        txtDumpFolder.Enabled = btnBrowse.Enabled = chkDump.Checked;
    }

    private void Browse()
    {
        using var fbd = new FolderBrowserDialog { Description = "Folder to dump the boxes into" };
        if (!string.IsNullOrWhiteSpace(txtDumpFolder.Text))
            fbd.SelectedPath = txtDumpFolder.Text;
        if (fbd.ShowDialog() == DialogResult.OK)
            txtDumpFolder.Text = fbd.SelectedPath;
    }

    private void Apply()
    {
        cfg.AutoDetect = chkAuto.Checked;
        cfg.OT = txtOT.Text;
        cfg.TID = (uint)numTID.Value;
        cfg.SID = (uint)numSID.Value;
        cfg.Gender = cmbGender.SelectedIndex;
        cfg.Language = LangIds[cmbLang.SelectedIndex < 0 ? 1 : cmbLang.SelectedIndex];
        cfg.DumpAfterApply = chkDump.Checked;
        cfg.DumpFolder = txtDumpFolder.Text;
        cfg.Save();
    }

    private static int LangToIndex(int lang)
    {
        for (int i = 0; i < LangIds.Length; i++)
            if (LangIds[i] == lang) return i;
        return 1; // ENG
    }
}
