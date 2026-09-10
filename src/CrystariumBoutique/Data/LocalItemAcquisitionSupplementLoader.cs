using CrystariumBoutique.Core.Catalog;

namespace CrystariumBoutique.Data;

public static class LocalItemAcquisitionSupplementLoader
{
    public static ItemAcquisitionSupplementLoad Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
        {
            return ItemAcquisitionSupplementLoad.Empty with
            {
                Warnings = [$"The optional local acquisition supplement was not found: {path}"],
            };
        }

        try
        {
            return ItemAcquisitionSupplementParser.Parse(File.ReadAllText(path));
        }
        catch (IOException exception)
        {
            return Failed(path, exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Failed(path, exception);
        }
    }

    private static ItemAcquisitionSupplementLoad Failed(string path, Exception exception)
        => ItemAcquisitionSupplementLoad.Empty with
        {
            Warnings =
            [
                $"The local acquisition supplement could not be read from {path}: "
                + $"{exception.GetType().Name}: {exception.Message}",
            ],
        };
}
