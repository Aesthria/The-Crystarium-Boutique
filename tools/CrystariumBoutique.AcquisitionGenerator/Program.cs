using System.Text.Json;

namespace CrystariumBoutique.AcquisitionGenerator;

public static class Program
{
    private static readonly JsonSerializerOptions InputJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return 2;
            }

            return args[0] switch
            {
                "generate" => Generate(ParseOptions(args[1..])),
                "generate-lumina-supplemental" => GenerateLuminaSupplemental(ParseOptions(args[1..])),
                "inspect" => Inspect(ParseOptions(args[1..])),
                "inspect-items" => InspectItems(ParseOptions(args[1..])),
                _ => UnknownCommand(args[0]),
            };
        }
        catch (Exception exception) when (exception is not StackOverflowException)
        {
            Console.Error.WriteLine($"Generation failed: {exception.GetType().Name}: {exception.Message}");
            return 1;
        }
    }

    private static int GenerateLuminaSupplemental(Dictionary<string, string> options)
    {
        var datasetDirectory = Require(options, "supplemental-data");
        var sourcePath = Require(options, "source");
        var gamePath = Require(options, "game-path");
        var supplementPath = Require(options, "output");
        var manifestPath = Require(options, "manifest");
        var reviewPath = Require(options, "review");
        var source = ReadJson<LuminaSupplementalSourceDescriptor>(sourcePath);
        var gameData = LuminaGameDataIndex.Load(
            gamePath,
            options.GetValueOrDefault("game-build"));
        var result = LuminaSupplementalAcquisitionGenerator.Generate(
            new LuminaSupplementalGeneratorInput(datasetDirectory, source, gameData));
        WriteJson(supplementPath, result.Supplement);
        WriteJson(manifestPath, result.Manifest);
        WriteJson(reviewPath, result.Review);
        Console.WriteLine(
            $"Accepted {result.Manifest.AcceptedRecords}; rejected {result.Manifest.RejectedRecords}; "
            + $"superseded {result.Manifest.SupersededRecords} from {result.Manifest.TotalInputRecords} relationship rows.");
        return 0;
    }

    private static int Generate(Dictionary<string, string> options)
    {
        var trackyPath = Require(options, "tracky");
        var sourcePath = Require(options, "source");
        var metadataPath = Require(options, "duty-metadata");
        var gamePath = Require(options, "game-path");
        var supplementPath = Require(options, "output");
        var manifestPath = Require(options, "manifest");
        var reviewPath = Require(options, "review");
        var trackyJson = File.ReadAllText(trackyPath);
        var source = ReadJson<TrackySourceDescriptor>(sourcePath);
        var metadata = ReadJson<DutyMetadataDocument>(metadataPath);
        var gameData = LuminaGameDataIndex.Load(
            gamePath,
            options.GetValueOrDefault("game-build"));
        var minimumObservations = options.TryGetValue("minimum-observations", out var minimumText)
            ? int.Parse(minimumText, System.Globalization.CultureInfo.InvariantCulture)
            : 2;
        if (minimumObservations < 2)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "The minimum observation policy cannot be lower than two.");
        }

        var result = AcquisitionSupplementGenerator.Generate(new AcquisitionGeneratorInput(
            trackyJson,
            AcquisitionSupplementGenerator.ComputeSha256File(trackyPath),
            source,
            metadata,
            gameData,
            new GeneratorPolicy(minimumObservations)));
        WriteJson(supplementPath, result.Supplement);
        WriteJson(manifestPath, result.Manifest);
        WriteJson(reviewPath, result.Review);
        Console.WriteLine(
            $"Accepted {result.Manifest.AcceptedRecords}; quarantined {result.Manifest.QuarantinedRecords}; "
            + $"rejected {result.Manifest.RejectedRecords} from {result.Manifest.TotalInputRecords} input rows.");
        return 0;
    }

    private static int Inspect(Dictionary<string, string> options)
    {
        var trackyPath = Require(options, "tracky");
        var gamePath = Require(options, "game-path");
        var dutyIds = Require(options, "duties")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => uint.Parse(value, System.Globalization.CultureInfo.InvariantCulture))
            .ToHashSet();
        var gameData = LuminaGameDataIndex.Load(
            gamePath,
            options.GetValueOrDefault("game-build"));
        foreach (var dutyId in dutyIds.Order())
        {
            if (gameData.TryGetDuty(dutyId, out var duty))
            {
                Console.WriteLine(
                    $"DUTY id={duty.ContentFinderConditionId} name={duty.Name} "
                    + $"type={duty.ContentType} territory={duty.TerritoryTypeId}");
            }
        }

        var observations = TrackyDatasetReader.Read(File.ReadAllText(trackyPath)).Observations
            .Where(value => dutyIds.Contains(value.ContentFinderConditionId))
            .GroupBy(value => new
            {
                value.ItemId,
                value.ContentFinderConditionId,
                value.TerritoryTypeId,
                value.TreasureId,
                value.MapId,
            })
            .Select(group => new
            {
                group.Key.ItemId,
                group.Key.ContentFinderConditionId,
                group.Key.TerritoryTypeId,
                group.Key.TreasureId,
                group.Key.MapId,
                Observations = group.Sum(value => value.ObservationCount),
            })
            .OrderBy(value => value.ContentFinderConditionId)
            .ThenByDescending(value => value.Observations)
            .ThenBy(value => value.ItemId);
        foreach (var observation in observations)
        {
            if (gameData.TryGetItem(observation.ItemId, out var item) && item.IsEquipment)
            {
                _ = gameData.TryGetMapTerritory(observation.MapId, out var mapTerritory);
                Console.WriteLine(
                    $"duty={observation.ContentFinderConditionId} item={observation.ItemId} "
                    + $"name={item.Name} observations={observation.Observations} "
                    + $"territory={observation.TerritoryTypeId} treasure={observation.TreasureId} "
                    + $"map={observation.MapId} mapTerritory={mapTerritory}");
            }
        }

        return 0;
    }

    private static int InspectItems(Dictionary<string, string> options)
    {
        var gamePath = Require(options, "game-path");
        var itemIds = Require(options, "items")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => uint.Parse(value, System.Globalization.CultureInfo.InvariantCulture))
            .Distinct()
            .Order();
        var gameData = LuminaGameDataIndex.Load(
            gamePath,
            options.GetValueOrDefault("game-build"));
        foreach (var itemId in itemIds)
        {
            if (gameData.TryGetItem(itemId, out var item))
            {
                Console.WriteLine($"item={item.ItemId} name={item.Name} equipment={item.IsEquipment}");
            }
            else
            {
                Console.WriteLine($"item={itemId} missing=true");
            }
        }

        return 0;
    }

    private static T ReadJson<T>(string path)
        where T : class
        => JsonSerializer.Deserialize<T>(File.ReadAllText(path), InputJsonOptions)
            ?? throw new InvalidDataException($"'{path}' did not contain a {typeof(T).Name} document.");

    private static void WriteJson(string path, object value)
    {
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException("An output path must have a parent directory."));
        File.WriteAllText(fullPath, AcquisitionSupplementGenerator.Serialize(value));
    }

    private static Dictionary<string, string> ParseOptions(string[] args)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < args.Length; index += 2)
        {
            if (!args[index].StartsWith("--", StringComparison.Ordinal) || index + 1 >= args.Length)
            {
                throw new ArgumentException("Options must use --name value pairs.");
            }

            if (!result.TryAdd(args[index][2..], args[index + 1]))
            {
                throw new ArgumentException($"Duplicate option '{args[index]}'.");
            }
        }

        return result;
    }

    private static string Require(Dictionary<string, string> options, string name)
        => options.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException($"Missing required --{name} option.");

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command '{command}'.");
        PrintUsage();
        return 2;
    }

    private static void PrintUsage()
    {
        Console.WriteLine(
            "CrystariumBoutique.AcquisitionGenerator generate "
            + "--tracky <ChestDropsV2.json> --source <source.json> "
            + "--duty-metadata <metadata.json> --game-path <FFXIV game or sqpack path> "
            + "--output <supplement.json> --manifest <manifest.json> --review <review.json>");
        Console.WriteLine(
            "CrystariumBoutique.AcquisitionGenerator inspect "
            + "--tracky <ChestDropsV2.json> --game-path <FFXIV game or sqpack path> --duties <id,id,...>");
        Console.WriteLine(
            "CrystariumBoutique.AcquisitionGenerator inspect-items "
            + "--game-path <FFXIV game or sqpack path> --items <id,id,...>");
        Console.WriteLine(
            "CrystariumBoutique.AcquisitionGenerator generate-lumina-supplemental "
            + "--supplemental-data <directory> --source <source.json> "
            + "--game-path <FFXIV game or sqpack path> --output <supplement.json> "
            + "--manifest <manifest.json> --review <review.json>");
    }
}
