using System.Data;
using System.IO;
using System.Text;

namespace KosKu.Services;

public static class CsvExportService
{
    public static void Export(DataTable table, string path)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", table.Columns.Cast<DataColumn>().Select(c => Escape(c.ColumnName))));
        foreach (DataRow row in table.Rows)
            builder.AppendLine(string.Join(",", table.Columns.Cast<DataColumn>().Select(c => Escape(Convert.ToString(row[c]) ?? string.Empty))));
        File.WriteAllText(path, "\uFEFF" + builder, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static string Escape(string value)
    {
        var safe = value.Replace("\r", " ").Replace("\n", " ");
        if (safe.Contains(',') || safe.Contains('"')) safe = "\"" + safe.Replace("\"", "\"\"") + "\"";
        return safe;
    }
}
