namespace Oleander.Assembly.Comparers.Core
{
    public interface IDiffItem
    {
        DiffType DiffType { get; }

        string ToXml();

        bool IsBreakingChange { get; }
    }
}
