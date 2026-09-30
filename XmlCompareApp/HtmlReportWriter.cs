using System.Net;
using System.Text;

namespace XmlCompareApp;

public static class HtmlReportWriter
{
    public static void Write(
        string outputPath,
        string file1,
        string file2,
        IReadOnlyList<FieldResult> results,
        bool mismatchesOnly)
    {
        int total = results.Count;
        int matching = results.Count(r => r.IsMatch);
        int missingIn1 = results.Count(r => !r.InFile1);
        int missingIn2 = results.Count(r => !r.InFile2);

        var rows = mismatchesOnly ? results.Where(r => !r.IsMatch).ToList() : results.ToList();

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\">");
        sb.AppendLine("<title>XML Comparison Report</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#222}");
        sb.AppendLine("h1{font-size:22px}");
        sb.AppendLine(".info td{padding:2px 12px 2px 0}");
        sb.AppendLine(".summary{display:flex;gap:12px;margin:16px 0}");
        sb.AppendLine(".card{border:1px solid #ddd;border-radius:6px;padding:10px 16px;min-width:140px}");
        sb.AppendLine(".card b{display:block;font-size:22px}");
        sb.AppendLine("table.result{border-collapse:collapse;width:100%}");
        sb.AppendLine("table.result th,table.result td{border:1px solid #ccc;padding:6px 8px;text-align:left;font-size:13px}");
        sb.AppendLine("table.result th{background:#2f5597;color:#fff;position:sticky;top:0}");
        sb.AppendLine(".present{background:#e2f0d9;color:#256029;font-weight:600}");
        sb.AppendLine(".missing{background:#fbe5e5;color:#b00020;font-weight:600}");
        sb.AppendLine("tr.mismatch td.field{font-weight:600}");
        sb.AppendLine(".attr{color:#7a4f01}");
        sb.AppendLine(".path{color:#666;font-family:Consolas,monospace}");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine("<h1>XML Comparison Report</h1>");
        sb.AppendLine("<table class=\"info\">");
        sb.AppendLine($"<tr><td><b>Generated</b></td><td>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</td></tr>");
        sb.AppendLine($"<tr><td><b>File 1</b></td><td>{Enc(Path.GetFullPath(file1))}</td></tr>");
        sb.AppendLine($"<tr><td><b>File 2</b></td><td>{Enc(Path.GetFullPath(file2))}</td></tr>");
        sb.AppendLine("<tr><td><b>Rules</b></td><td>Element order is ignored. Text values and attribute values are not compared.</td></tr>");
        sb.AppendLine($"<tr><td><b>Showing</b></td><td>{(mismatchesOnly ? "Only mismatched fields" : "All fields")}</td></tr>");
        sb.AppendLine("</table>");

        sb.AppendLine("<div class=\"summary\">");
        sb.AppendLine($"<div class=\"card\">Total fields<b>{total}</b></div>");
        sb.AppendLine($"<div class=\"card\">In both files<b>{matching}</b></div>");
        sb.AppendLine($"<div class=\"card\">Missing in File 1<b>{missingIn1}</b></div>");
        sb.AppendLine($"<div class=\"card\">Missing in File 2<b>{missingIn2}</b></div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<table class=\"result\">");
        sb.AppendLine("<tr><th>#</th><th>Field (hierarchy)</th><th>Type</th><th>Full Path</th><th>File 1</th><th>File 2</th></tr>");

        int i = 1;
        foreach (var r in rows)
        {
            int indent = r.Depth * 20;
            string name = r.Kind == FieldKind.Attribute
                ? $"<span class=\"attr\">@{Enc(r.Name)}</span>"
                : $"&lt;{Enc(r.Name)}&gt;";

            sb.Append(r.IsMatch ? "<tr>" : "<tr class=\"mismatch\">");
            sb.Append($"<td>{i++}</td>");
            sb.Append($"<td class=\"field\" style=\"padding-left:{indent + 8}px\">{name}</td>");
            sb.Append($"<td>{r.Kind}</td>");
            sb.Append($"<td class=\"path\">{Enc(r.Path)}</td>");
            sb.Append(StatusCell(r.InFile1));
            sb.Append(StatusCell(r.InFile2));
            sb.AppendLine("</tr>");
        }

        if (rows.Count == 0)
            sb.AppendLine("<tr><td colspan=\"6\">No differences found.</td></tr>");

        sb.AppendLine("</table></body></html>");

        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
    }

    private static string StatusCell(bool present) =>
        present ? "<td class=\"present\">Present</td>" : "<td class=\"missing\">Missing</td>";

    private static string Enc(string value) => WebUtility.HtmlEncode(value);
}
