namespace WienerSort.Sort;

public interface IChunkSorter
{
    void Sort(List<Entry> entries);
}

public class ChunkSorter(IComparer<Entry> comparer) : IChunkSorter
{
    public void Sort(List<Entry> entries)
    {
        entries.Sort(comparer);
    }
}