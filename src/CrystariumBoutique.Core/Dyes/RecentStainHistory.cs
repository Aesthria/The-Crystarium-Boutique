using CrystariumBoutique.Core.Appearance;

namespace CrystariumBoutique.Core.Dyes;

public sealed class RecentStainHistory
{
    public const int DefaultCapacity = 12;

    private readonly List<StainId> stains;

    public RecentStainHistory(int capacity = DefaultCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        Capacity = capacity;
        stains = new List<StainId>(capacity);
    }

    public int Capacity { get; }

    public IReadOnlyList<StainId> Items => stains;

    public void Record(StainId stain)
    {
        if (stain.Value == 0)
        {
            return;
        }

        stains.Remove(stain);
        stains.Insert(0, stain);
        if (stains.Count > Capacity)
        {
            stains.RemoveAt(stains.Count - 1);
        }
    }
}
