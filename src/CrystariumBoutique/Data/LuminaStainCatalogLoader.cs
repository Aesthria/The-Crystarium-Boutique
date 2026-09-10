using System.Collections.Immutable;
using System.Diagnostics;
using CrystariumBoutique.Core.Appearance;
using CrystariumBoutique.Core.Dyes;
using CrystariumBoutique.Core.Errors;
using Dalamud.Plugin.Services;
using LuminaStain = Lumina.Excel.Sheets.Stain;

namespace CrystariumBoutique.Data;

public sealed record StainCatalogLoad(
    StainCatalog Catalog,
    int ExaminedRows,
    TimeSpan Duration);

public sealed class LuminaStainCatalogLoader
{
    private readonly IDataManager dataManager;

    public LuminaStainCatalogLoader(IDataManager dataManager)
        => this.dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));

    public Result<StainCatalogLoad> Load()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var sheet = dataManager.GetExcelSheet<LuminaStain>();
            var stains = ImmutableArray.CreateBuilder<StainDefinition>();
            foreach (var row in sheet)
            {
                if (row.RowId is 0 or > byte.MaxValue)
                {
                    continue;
                }

                var name = row.Name.ExtractText().Trim();
                if (name.Length == 0)
                {
                    continue;
                }

                stains.Add(new StainDefinition(
                    new StainId((byte)row.RowId),
                    name,
                    StainColor.FromPackedRgb(row.Color),
                    StainGroups.FromShade(row.Shade),
                    row.SubOrder,
                    row.IsMetallic));
            }

            var catalog = new StainCatalog(stains.ToImmutable());
            stopwatch.Stop();
            return Result.Success(new StainCatalogLoad(catalog, sheet.Count, stopwatch.Elapsed));
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            return Result.Failure<StainCatalogLoad>(new BoutiqueError(
                BoutiqueErrorCode.DataLoadFailed,
                "The local dye catalog could not be loaded.",
                $"{exception.GetType().Name}: {exception.Message}"));
        }
    }
}
