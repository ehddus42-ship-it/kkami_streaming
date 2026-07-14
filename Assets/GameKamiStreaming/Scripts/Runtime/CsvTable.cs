using System.Collections.Generic;
using System.Text;

namespace GameKamiStreaming
{
    public sealed class CsvTable
    {
        public readonly List<string> Headers = new List<string>();
        public readonly List<Dictionary<string, string>> Rows = new List<Dictionary<string, string>>();

        public static CsvTable Parse(string csv)
        {
            var table = new CsvTable();
            var records = ReadRecords(csv);
            if (records.Count == 0)
            {
                return table;
            }

            foreach (var header in records[0])
            {
                // Unity TextAsset keeps the UTF-8 BOM, which would otherwise make the
                // first CSV header (for example "string_id") impossible to look up.
                table.Headers.Add(header.Trim().TrimStart('\ufeff'));
            }
            for (var r = 1; r < records.Count; r++)
            {
                if (records[r].Count == 0)
                {
                    continue;
                }

                var row = new Dictionary<string, string>();
                var hasValue = false;
                for (var c = 0; c < table.Headers.Count; c++)
                {
                    var value = c < records[r].Count ? records[r][c].Trim() : string.Empty;
                    row[table.Headers[c]] = value;
                    hasValue |= !string.IsNullOrWhiteSpace(value);
                }

                if (hasValue)
                {
                    table.Rows.Add(row);
                }
            }

            return table;
        }

        static List<List<string>> ReadRecords(string csv)
        {
            var records = new List<List<string>>();
            var record = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < csv.Length; i++)
            {
                var ch = csv[i];
                if (ch == '"')
                {
                    if (inQuotes && i + 1 < csv.Length && csv[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (ch == ',' && !inQuotes)
                {
                    record.Add(field.ToString());
                    field.Length = 0;
                }
                else if ((ch == '\n' || ch == '\r') && !inQuotes)
                {
                    if (ch == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n')
                    {
                        i++;
                    }

                    record.Add(field.ToString());
                    field.Length = 0;
                    if (record.Count > 1 || !string.IsNullOrWhiteSpace(record[0]))
                    {
                        records.Add(record);
                    }
                    record = new List<string>();
                }
                else
                {
                    field.Append(ch);
                }
            }

            record.Add(field.ToString());
            if (record.Count > 1 || !string.IsNullOrWhiteSpace(record[0]))
            {
                records.Add(record);
            }

            return records;
        }
    }
}
