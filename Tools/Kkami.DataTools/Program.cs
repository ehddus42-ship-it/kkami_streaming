using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Kkami.DataTools;

internal static class Program
{
    static readonly string[] DataSheets =
    {
        "miner", "piece", "currency", "boss", "stage", "skilltree", "stringkey", "chat",
        "res_img", "res_sfx", "res_vfx"
    };

    static readonly Dictionary<string, string[]> ImageMappings = new(StringComparer.Ordinal)
    {
        ["img_keyboard_01"] = new[] { "img_keyboard_01", "keyboard" },
        ["img_camera_01"] = new[] { "img_camera_01", "camera" },
        ["img_energydrink_01"] = new[] { "img_energydrink_01", "energydrink" },
        ["img_box_01"] = new[] { "img_box_01", "box" },
        ["img_redbox_01"] = new[] { "img_redbox_01", "redbox" },
        ["img_follow_01"] = new[] { "img_follow_01", "follow" },
        ["img_watch_01"] = new[] { "img_watch_01", "watcher" },
        ["img_love_01"] = new[] { "img_love_01", "love" },
        ["img_donation_01"] = new[] { "img_donation_01", "donation" },
        ["img_reddonation_01"] = new[] { "img_reddonation_01", "reddoination" },
        ["img_subscriber_01"] = new[] { "img_subscriber_01", "img_subscription_01", "subscription" },
        ["img_stage1_01"] = new[] { "img_stage1_01", "stage1" },
        ["img_stage2_01"] = new[] { "img_stage2_01", "stage2" },
        ["img_stage3_01"] = new[] { "img_stage3_01", "stage3" },
        ["img_stage4_01"] = new[] { "img_stage4_01", "stage4" },
        ["img_stage5_01"] = new[] { "img_stage5_01", "stage5" },
        ["img_angry_01"] = new[] { "img_angry_01", "kkami_appear/angry", "angry" },
        ["img_confused_01"] = new[] { "img_confused_01", "kkami_appear/confused", "confused" },
        ["img_sad_01"] = new[] { "img_sad_01", "kkami_appear/sad", "sad" },
        ["img_shocked_01"] = new[] { "img_shocked_01", "kkami_appear/shocked", "shocked" },
        ["img_superangry_01"] = new[] { "img_superangry_01", "kkami_appear/super_angry", "super_angry" },
        ["img_kkami_01"] = new[] { "img_kkami_01", "kkami big star-Photoroom" },
        ["img_manager_01"] = new[] { "img_manager_01" }
    };

    static readonly Dictionary<string, string> ExtraMetaTemplates = new(StringComparer.Ordinal)
    {
        ["vfx_boss1_01"] = "vfx_boss2_01",
        ["vfx_boss1_02"] = "vfx_boss2_02",
        ["vfx_kkami_01"] = "vfx_boss2_01",
        ["vfx_boss4_1_1"] = "vfx_boss4_01_1"
    };

    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        try
        {
            var projectRoot = FindProjectRoot(AppContext.BaseDirectory) ?? FindProjectRoot(Environment.CurrentDirectory);
            if (args.Length == 0)
            {
                if (projectRoot is null)
                    throw new InvalidOperationException("Unity 프로젝트 루트를 찾지 못했습니다.");
                return Export(new[]
                {
                    Path.Combine(projectRoot, "SourceData", "kkami_datatable_ver02.xlsx"),
                    "--project-root", projectRoot,
                    "--output", Path.Combine(projectRoot, "Assets", "GameKamiStreaming", "Resources", "GameKamiStreaming", "DataTables", "xlsx_export")
                });
            }

            return args[0] switch
            {
                "export" => Export(args[1..]),
                "sync-images" => SyncImages(args[1..]),
                "--help" or "-h" or "help" => PrintHelp(),
                _ when args[0].EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) => Export(args),
                _ => throw new ArgumentException($"알 수 없는 명령: {args[0]}")
            };
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"오류: {exception.Message}");
            return 1;
        }
    }

    static int PrintHelp()
    {
        Console.WriteLine("까미 스트리밍 데이터 도구");
        Console.WriteLine("  Kkami.DataTools export <xlsx> [--project-root <path>] [--output <path>]");
        Console.WriteLine("  Kkami.DataTools sync-images [--project-root <path>] [--external-source <path>]");
        Console.WriteLine("  Kkami.DataTools <xlsx> [--project-root <path>] [--output <path>]");
        return 0;
    }

    static int Export(string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException("XLSX 경로가 필요합니다.");

        var xlsx = Path.GetFullPath(args[0]);
        var projectRoot = ReadOption(args, "--project-root");
        var output = ReadOption(args, "--output") ?? Path.Combine(Path.GetDirectoryName(xlsx)!, "csv_export");
        if (!File.Exists(xlsx))
            throw new FileNotFoundException("XLSX 파일을 찾을 수 없습니다.", xlsx);

        using var reader = new XlsxReader(xlsx);
        ExportRaw(reader, Path.GetFullPath(output));
        var warnings = new List<string>();
        if (projectRoot is not null)
        {
            var runtimeOutput = Path.Combine(Path.GetFullPath(projectRoot), "Assets", "GameKamiStreaming", "Resources", "GameKamiStreaming", "DataTables");
            warnings.AddRange(ExportRuntime(reader, runtimeOutput));
            Console.WriteLine($"게임 CSV 갱신: {runtimeOutput}");
        }

        Console.WriteLine($"원본 시트 CSV 생성: {Path.GetFullPath(output)}");
        foreach (var warning in warnings)
            Console.WriteLine($"경고: {warning}");
        return 0;
    }

    static IEnumerable<string> ExportRuntime(XlsxReader reader, string output)
    {
        var warnings = new List<string>();
        var images = reader.Records("res_img")
            .Where(row => Get(row, "img_name_ID").Length > 0)
            .ToDictionary(row => Get(row, "img_name_ID"), row => Get(row, "img_name"), StringComparer.Ordinal);

        var currencies = reader.Records("currency")
            .Where(row => Integer(Get(row, "currency_ID")) > 0)
            .Select(row => new object?[] { Integer(Get(row, "currency_ID")), Get(row, "currency_name"), ResolveImage(Get(row, "img_name_ID"), images), "" })
            .ToList();
        WriteCsv(Path.Combine(output, "currency.csv"), new[] { "resource_id", "res_name", "resimg_id", "effect_id" }, currencies);

        var pieces = reader.Records("piece")
            .Where(row => Integer(Get(row, "piece_id")) > 0)
            .Select(row => new object?[]
            {
                Integer(Get(row, "piece_id")), Get(row, "piece_name"), Integer(Get(row, "currency_id")),
                Integer(Get(row, "start_currency_amount")), Integer(Get(row, "piece_hp"), 1),
                ResolveImage(Get(row, "img_name_ID"), images), Get(row, "sfx_name_ID"), Get(row, "vfx_name_ID"), ""
            }).ToList();

        var sourceVfxNames = reader.Records("res_vfx")
            .Select(row => Get(row, "vfx_name"))
            .Where(value => value.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
        var allVfxNames = new HashSet<string>(sourceVfxNames, StringComparer.Ordinal);
        foreach (var row in reader.Records("boss"))
        {
            var bossId = Integer(Get(row, "boss_ID"));
            if (bossId <= 0) continue;
            var idleVfx = Get(row, "vfx_name_ID");
            var deathVfx = Get(row, "vfx_name_ID.1");
            foreach (var reference in new[] { idleVfx, deathVfx }.Where(value => value.Length > 0))
            {
                allVfxNames.Add(reference);
                if (!sourceVfxNames.Contains(reference))
                    warnings.Add($"boss {bossId}: res_vfx에 없는 참조 '{reference}'");
            }
            pieces.Add(new object?[]
            {
                bossId, Get(row, "boss_name"), Integer(Get(row, "boss_currency_ID")), Integer(Get(row, "boss_currency_amount")),
                Integer(Get(row, "boss_hp"), 1), idleVfx, Get(row, "sfx_name_ID"), idleVfx, deathVfx
            });
        }
        WriteCsv(Path.Combine(output, "piece.csv"), new[] { "piece_id", "piece_name", "resource_id", "resource_int", "hp_int", "pieceimg_id", "sound_id", "effect_id", "death_effect_id" }, pieces);

        var stages = reader.Records("stage")
            .Where(row => Integer(Get(row, "stage_ID")) > 0)
            .Select(row => new object?[]
            {
                Integer(Get(row, "stage_ID")), OptionalInteger(Get(row, "boss_ID")), 30,
                Number(Get(row, "keyboard_spawn")), Number(Get(row, "camera_spawn")), Number(Get(row, "energydrink_spawn")),
                Number(Get(row, "box_spawn")), Number(Get(row, "redbox_spawn")), ResolveImage(Get(row, "img_name_ID"), images), ""
            }).ToList();
        WriteCsv(Path.Combine(output, "stage.csv"), new[] { "stage_id", "boss_id", "time_limit_sec", "piece_10001_weight", "piece_10002_weight", "piece_10003_weight", "piece_10004_weight", "piece_10005_weight", "stageimg_id", "effect_id" }, stages);

        var skills = reader.Records("skilltree")
            .Where(row => Integer(Get(row, "tile_id")) > 0)
            .Select(row => new object?[]
            {
                Integer(Get(row, "tile_id")), Integer(Get(row, "upgrade_type")), Integer(Get(row, "upgrade_rank")), Integer(Get(row, "upgrade_count")), Number(Get(row, "increase_by")), Boolean(Get(row, "sub_use")),
                Integer(Get(row, "follow_amount")), Integer(Get(row, "watch_amount")), Integer(Get(row, "love_amount")), Integer(Get(row, "donation_amount")),
                Integer(Get(row, "reddonation_amount")), Integer(Get(row, "subscriber_amount")), ResolveImage(Get(row, "img_name_ID"), images), Get(row, "sfx_name_ID"), Get(row, "vfx_name_ID"), Get(row, "skill_stringkey"), Get(row, "skill_name")
            }).ToList();
        WriteCsv(Path.Combine(output, "skilltree.csv"), new[] { "tile_id", "reinforced_int", "upgrade_rank", "upgrade_count", "up_int", "sub_use", "follow_int", "watcher_int", "love_int", "donation_int", "reddonation_int", "subscriber_int", "image_id", "sound_id", "effect_id", "skill_stringkey", "skill_name" }, skills);

        var skillDescriptions = reader.Records("stringkey")
            .Where(row => Get(row, "string_ID").Length > 0)
            .Select(row => new object?[] { Get(row, "string_ID"), Get(row, "skill_description") })
            .ToList();
        WriteCsv(Path.Combine(output, "stringkey.csv"), new[] { "string_id", "skill_description" }, skillDescriptions);

        var chats = reader.Records("chat")
            .Where(row => Integer(Get(row, "chat_ID")) > 0)
            .Select(row => new object?[]
            {
                Integer(Get(row, "chat_ID")), Get(row, "chat_dialogue").Trim('"'), Number(Get(row, "chat_spawn")),
                ResolveImage(Get(row, "img_name_ID"), images), ResolveImage(Get(row, "img_name_ID.1"), images)
            }).ToList();
        WriteCsv(Path.Combine(output, "chat.csv"), new[] { "chat_id", "chat_dialogue", "chat_spawn", "viewer_img_id", "kkami_portrait_img_id" }, chats);

        WriteCsv(Path.Combine(output, "res_vfx.csv"), new[] { "effect_id", "effect_name", "prefab_path" },
            allVfxNames.Order(StringComparer.Ordinal).Select(name => new object?[] { name, name, "" }).ToList());

        var rawOutput = Path.Combine(output, "xlsx_export");
        foreach (var sheet in DataSheets)
            WriteCsvRows(Path.Combine(rawOutput, sheet + ".csv"), reader.Rows(sheet));
        return warnings;
    }

    static void ExportRaw(XlsxReader reader, string output)
    {
        foreach (var sheet in reader.SheetNames)
            WriteCsvRows(Path.Combine(output, sheet + ".csv"), reader.Rows(sheet));
    }

    static int SyncImages(string[] args)
    {
        var projectRoot = ReadOption(args, "--project-root") ?? FindProjectRoot(Environment.CurrentDirectory)
            ?? throw new InvalidOperationException("Unity 프로젝트 루트를 찾지 못했습니다.");
        var externalSource = ReadOption(args, "--external-source");
        var sprites = Path.Combine(projectRoot, "Assets", "GameKamiStreaming", "Resources", "GameKamiStreaming", "Sprites");
        Directory.CreateDirectory(sprites);
        var copied = 0;
        var missing = new List<string>();

        foreach (var (target, candidates) in ImageMappings)
        {
            var source = FindImage(sprites, externalSource, candidates);
            if (source is null)
            {
                missing.Add(target);
                continue;
            }
            var destination = Path.Combine(sprites, target + Path.GetExtension(source));
            if (!Path.GetFullPath(source).Equals(Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
                File.Copy(source, destination, true);
            CreateMeta(destination, source, sprites);
            copied++;
        }

        foreach (var (target, template) in ExtraMetaTemplates)
        {
            var destination = Path.Combine(sprites, target + ".png");
            var templateMeta = Path.Combine(sprites, template + ".png.meta");
            CreateMetaFromTemplate(destination, templateMeta);
        }

        Console.WriteLine($"이미지 동기화: {copied}개");
        if (missing.Count > 0)
            Console.WriteLine("누락: " + string.Join(", ", missing));
        return 0;
    }

    static string? FindImage(string sprites, string? externalSource, IEnumerable<string> candidates)
    {
        foreach (var candidate in candidates)
        foreach (var root in new[] { sprites, externalSource }.Where(path => !string.IsNullOrWhiteSpace(path)))
        foreach (var extension in new[] { ".png", ".jpg", ".jpeg" })
        {
            var path = Path.Combine(root!, candidate + extension);
            if (File.Exists(path)) return path;
        }
        return null;
    }

    static void CreateMeta(string destination, string source, string sprites)
    {
        var meta = destination + ".meta";
        if (File.Exists(meta)) return;
        var template = File.Exists(source + ".meta") ? source + ".meta" : Path.Combine(sprites, "keyboard.png.meta");
        if (!File.Exists(template)) return;
        var content = File.ReadAllText(template);
        var guid = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        content = new Regex("(?m)^guid: .+$").Replace(content, "guid: " + guid, 1);
        File.WriteAllText(meta, content, new UTF8Encoding(false));
    }

    static void CreateMetaFromTemplate(string destination, string templateMeta)
    {
        var meta = destination + ".meta";
        if (!File.Exists(destination) || File.Exists(meta) || !File.Exists(templateMeta)) return;
        var content = File.ReadAllText(templateMeta);
        var guid = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        content = new Regex("(?m)^guid: .+$").Replace(content, "guid: " + guid, 1);
        File.WriteAllText(meta, content, new UTF8Encoding(false));
    }

    static string ResolveImage(string reference, IReadOnlyDictionary<string, string> images)
    {
        if (images.TryGetValue(reference, out var image)) return image;
        var match = Regex.Match(reference, "^I5010([1-5])$");
        return match.Success ? $"img_stage{match.Groups[1].Value}_01" : reference;
    }

    static string Get(IReadOnlyDictionary<string, string> row, string key) => row.TryGetValue(key, out var value) ? value : "";
    static int Integer(string value, int fallback = 0) => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? (int)parsed : fallback;
    static object OptionalInteger(string value) => Integer(value) is var parsed && parsed > 0 ? parsed : "";
    static object Number(string value) => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed % 1 == 0 ? (object)(int)parsed : parsed : 0;
    static string Boolean(string value) => new[] { "true", "1", "o", "y", "yes" }.Contains(value.ToLowerInvariant()) ? "true" : "false";

    static string? ReadOption(string[] args, string option)
    {
        var index = Array.IndexOf(args, option);
        if (index < 0) return null;
        if (index + 1 >= args.Length) throw new ArgumentException($"{option} 값이 필요합니다.");
        return args[index + 1];
    }

    static string? FindProjectRoot(string start)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(start));
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "Assets")) && Directory.Exists(Path.Combine(directory.FullName, "ProjectSettings")))
                return directory.FullName;
            directory = directory.Parent;
        }
        return null;
    }

    static void WriteCsv(string path, IEnumerable<string> headers, IEnumerable<object?[]> rows)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var writer = new StreamWriter(path, false, new UTF8Encoding(true));
        writer.WriteLine(string.Join(",", headers.Select(EscapeCsv)));
        foreach (var row in rows)
            writer.WriteLine(string.Join(",", row.Select(value => EscapeCsv(Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""))));
    }

    static void WriteCsvRows(string path, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var writer = new StreamWriter(path, false, new UTF8Encoding(true));
        var width = rows.Count == 0 ? 0 : rows.Max(row => row.Count);
        foreach (var row in rows)
            writer.WriteLine(string.Join(",", row.Concat(Enumerable.Repeat("", width - row.Count)).Select(EscapeCsv)));
    }

    static string EscapeCsv(string value) => value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
}

internal sealed class XlsxReader : IDisposable
{
    const string MainNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    const string DocumentRelationshipNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    const string PackageRelationshipNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";
    static readonly Regex CellReference = new("([A-Z]+)(\\d+)", RegexOptions.Compiled);

    readonly ZipArchive archive;
    readonly List<string> sharedStrings;
    readonly Dictionary<string, string> sheetPaths;

    public XlsxReader(string path)
    {
        archive = ZipFile.OpenRead(path);
        sharedStrings = ReadSharedStrings();
        sheetPaths = ReadSheetPaths();
    }

    public IEnumerable<string> SheetNames => sheetPaths.Keys;

    public IReadOnlyList<IReadOnlyList<string>> Rows(string sheetName)
    {
        var document = LoadXml(sheetPaths[sheetName]);
        XNamespace main = MainNamespace;
        var rows = new List<IReadOnlyList<string>>();
        foreach (var rowNode in document.Descendants(main + "row"))
        {
            var row = new List<string>();
            foreach (var cell in rowNode.Elements(main + "c"))
            {
                var index = ColumnIndex((string?)cell.Attribute("r") ?? "A1");
                while (row.Count <= index) row.Add("");
                var type = (string?)cell.Attribute("t") ?? "";
                var valueNode = cell.Element(main + "v");
                string value;
                if (type == "inlineStr") value = string.Concat(cell.Descendants(main + "t").Select(node => node.Value));
                else if (valueNode is null) value = "";
                else if (type == "s") value = sharedStrings[int.Parse(valueNode.Value, CultureInfo.InvariantCulture)];
                else if (type == "b") value = valueNode.Value == "1" ? "true" : "false";
                else value = valueNode.Value;
                row[index] = Clean(value);
            }
            while (row.Count > 0 && row[^1].Length == 0) row.RemoveAt(row.Count - 1);
            rows.Add(row);
        }
        return rows;
    }

    public IReadOnlyList<Dictionary<string, string>> Records(string sheetName)
    {
        var rows = Rows(sheetName);
        if (rows.Count < 4) return Array.Empty<Dictionary<string, string>>();
        var headers = UniqueHeaders(rows[1]);
        var result = new List<Dictionary<string, string>>();
        foreach (var row in rows.Skip(3))
        {
            var record = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var index = 0; index < headers.Count; index++)
            {
                if (headers[index].Length == 0) continue;
                record[headers[index]] = index < row.Count ? Clean(row[index]) : "";
            }
            if (record.Values.Any(value => value.Length > 0)) result.Add(record);
        }
        return result;
    }

    List<string> ReadSharedStrings()
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null) return new List<string>();
        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        XNamespace main = MainNamespace;
        return document.Descendants(main + "si").Select(item => string.Concat(item.Descendants(main + "t").Select(node => node.Value))).ToList();
    }

    Dictionary<string, string> ReadSheetPaths()
    {
        var workbook = LoadXml("xl/workbook.xml");
        var relationships = LoadXml("xl/_rels/workbook.xml.rels");
        XNamespace main = MainNamespace;
        XNamespace documentRelationship = DocumentRelationshipNamespace;
        XNamespace packageRelationship = PackageRelationshipNamespace;
        var targets = relationships.Descendants(packageRelationship + "Relationship")
            .ToDictionary(node => (string)node.Attribute("Id")!, node => (string)node.Attribute("Target")!);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var sheet in workbook.Descendants(main + "sheet"))
        {
            var target = targets[(string)sheet.Attribute(documentRelationship + "id")!].Replace('\\', '/');
            if (target.StartsWith('/')) target = target.TrimStart('/');
            else if (!target.StartsWith("xl/", StringComparison.Ordinal)) target = "xl/" + target;
            result[(string)sheet.Attribute("name")!] = target;
        }
        return result;
    }

    XDocument LoadXml(string path)
    {
        using var stream = archive.GetEntry(path)?.Open() ?? throw new InvalidDataException($"XLSX 항목 누락: {path}");
        return XDocument.Load(stream);
    }

    static int ColumnIndex(string reference)
    {
        var match = CellReference.Match(reference);
        var result = 0;
        foreach (var character in match.Groups[1].Value) result = result * 26 + character - 'A' + 1;
        return result - 1;
    }

    static IReadOnlyList<string> UniqueHeaders(IReadOnlyList<string> values)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var result = new List<string>();
        foreach (var rawValue in values)
        {
            var value = Clean(rawValue);
            counts.TryGetValue(value, out var count);
            result.Add(count == 0 ? value : value + "." + count);
            counts[value] = count + 1;
        }
        return result;
    }

    static string Clean(string value)
    {
        var text = value.Trim();
        return Regex.IsMatch(text, "^-?\\d+\\.0$") ? text[..^2] : text;
    }

    public void Dispose() => archive.Dispose();
}
