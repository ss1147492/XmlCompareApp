using System.Diagnostics;

namespace XmlCompareApp;

public class MainForm : Form
{
    private readonly TextBox txtFile1 = new() { Dock = DockStyle.Fill };
    private readonly TextBox txtFile2 = new() { Dock = DockStyle.Fill };
    private readonly TextBox txtOutput = new() { Dock = DockStyle.Fill };
    private readonly NumericUpDown nudSkipLevel = new() { Minimum = 0, Maximum = 2, Value = 0, Width = 55 };
    private readonly CheckBox chkMismatchOnly = new() { Text = "Show only mismatches", AutoSize = true };
    private readonly CheckBox chkOpenReport = new() { Text = "Open HTML report after compare", AutoSize = true, Checked = true };
    private readonly Button btnCompare = new() { Text = "Compare", AutoSize = true, Padding = new Padding(12, 2, 12, 2) };
    private readonly Label lblStatus = new() { AutoSize = true, Text = "Select two XML files and click Compare." };
    private readonly DataGridView grid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        BackgroundColor = SystemColors.Window
    };

    public MainForm()
    {
        Text = "XML Compare - Missing Elements & Attributes";
        Width = 1100;
        Height = 700;
        StartPosition = FormStartPosition.CenterScreen;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 7,
            Padding = new Padding(10)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        for (int r = 0; r < 5; r++)
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        AddFileRow(layout, 0, "File 1 (XML):", txtFile1, BrowseInput);
        AddFileRow(layout, 1, "File 2 (XML):", txtFile2, BrowseInput);
        AddFileRow(layout, 2, "Output report (HTML):", txtOutput, BrowseOutput);

        var skipPanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false };
        skipPanel.Controls.Add(new Label { Text = "Skip level:", AutoSize = true, Padding = new Padding(0, 5, 4, 0) });
        skipPanel.Controls.Add(nudSkipLevel);
        skipPanel.Controls.Add(new Label
        {
            Text = "0 = include root   1 = compare after root   2 = compare inside first child of root",
            AutoSize = true,
            Padding = new Padding(8, 5, 0, 0)
        });
        layout.Controls.Add(skipPanel, 1, 3);
        layout.SetColumnSpan(skipPanel, 2);

        var options = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false };
        options.Controls.Add(btnCompare);
        options.Controls.Add(chkMismatchOnly);
        options.Controls.Add(chkOpenReport);
        layout.Controls.Add(options, 1, 4);
        layout.SetColumnSpan(options, 2);

        grid.Columns.Add("Field", "Field (hierarchy)");
        grid.Columns.Add("Type", "Type");
        grid.Columns.Add("Path", "Full Path");
        grid.Columns.Add("File1", "File 1");
        grid.Columns.Add("File2", "File 2");
        grid.Columns["Field"]!.FillWeight = 30;
        grid.Columns["Type"]!.FillWeight = 10;
        grid.Columns["Path"]!.FillWeight = 40;
        grid.Columns["File1"]!.FillWeight = 10;
        grid.Columns["File2"]!.FillWeight = 10;
        grid.DefaultCellStyle.Font = new Font("Consolas", 9.5f);
        layout.Controls.Add(grid, 0, 5);
        layout.SetColumnSpan(grid, 3);

        layout.Controls.Add(lblStatus, 0, 6);
        layout.SetColumnSpan(lblStatus, 3);

        Controls.Add(layout);
        btnCompare.Click += (_, _) => RunCompare();
    }

    private static void AddFileRow(TableLayoutPanel layout, int row, string label, TextBox textBox, Action<TextBox> browse)
    {
        layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        layout.Controls.Add(textBox, 1, row);
        var button = new Button { Text = "Browse...", AutoSize = true };
        button.Click += (_, _) => browse(textBox);
        layout.Controls.Add(button, 2, row);
    }

    private void BrowseInput(TextBox target)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*",
            Title = "Select XML file"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        target.Text = dialog.FileName;

        if (string.IsNullOrWhiteSpace(txtOutput.Text))
        {
            string folder = Path.GetDirectoryName(dialog.FileName) ?? Environment.CurrentDirectory;
            txtOutput.Text = Path.Combine(folder, "xml_compare_report.html");
        }
    }

    private void BrowseOutput(TextBox target)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "HTML files (*.html)|*.html",
            DefaultExt = "html",
            FileName = string.IsNullOrWhiteSpace(target.Text) ? "xml_compare_report.html" : Path.GetFileName(target.Text),
            Title = "Save comparison report"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            target.Text = dialog.FileName;
    }

    private void RunCompare()
    {
        string file1 = txtFile1.Text.Trim();
        string file2 = txtFile2.Text.Trim();
        string output = txtOutput.Text.Trim();

        if (!File.Exists(file1) || !File.Exists(file2))
        {
            MessageBox.Show(this, "Please select two existing XML files.", "Input required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            MessageBox.Show(this, "Please choose where to save the HTML report.", "Output required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            Cursor = Cursors.WaitCursor;
            btnCompare.Enabled = false;

            int skipLevel = (int)nudSkipLevel.Value;
            var results = XmlComparer.Compare(file1, file2, skipLevel);
            HtmlReportWriter.Write(output, file1, file2, results, chkMismatchOnly.Checked);
            FillGrid(results);

            int missing1 = results.Count(r => !r.InFile1);
            int missing2 = results.Count(r => !r.InFile2);
            lblStatus.Text = $"{results.Count} fields compared | Missing in File 1: {missing1} | Missing in File 2: {missing2} | Report: {output}";

            if (chkOpenReport.Checked)
                Process.Start(new ProcessStartInfo(output) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Comparison failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = "Comparison failed.";
        }
        finally
        {
            Cursor = Cursors.Default;
            btnCompare.Enabled = true;
        }
    }

    private void FillGrid(IEnumerable<FieldResult> results)
    {
        grid.Rows.Clear();

        foreach (var r in results)
        {
            if (chkMismatchOnly.Checked && r.IsMatch)
                continue;

            string display = new string(' ', r.Depth * 3) +
                             (r.Kind == FieldKind.Attribute ? "@" + r.Name : "<" + r.Name + ">");

            int index = grid.Rows.Add(display, r.Kind.ToString(), r.Path, r.InFile1 ? "Present" : "Missing", r.InFile2 ? "Present" : "Missing");
            var row = grid.Rows[index];
            StyleStatus(row.Cells["File1"], r.InFile1);
            StyleStatus(row.Cells["File2"], r.InFile2);
        }
    }

    private static void StyleStatus(DataGridViewCell cell, bool present)
    {
        cell.Style.BackColor = present ? Color.FromArgb(226, 240, 217) : Color.FromArgb(251, 229, 229);
        cell.Style.ForeColor = present ? Color.FromArgb(37, 96, 41) : Color.FromArgb(176, 0, 32);
    }
}
