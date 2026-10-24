namespace DeltaAssetEditor.Core.Models;

/// <summary>
/// Результат поиска подстроки в ресурсах data.win.
/// </summary>
public class StringSearchResult
{
    public int Id { get; init; }
    public string Text { get; init; } = string.Empty;
    public bool IsTechnical { get; init; }
    public int MatchIndex { get; init; }
    public int MatchLength { get; init; }

    public StringSearchResult(int id, string text, bool isTechnical, int matchIndex, int matchLength)
    {
        Id = id;
        Text = text;
        IsTechnical = isTechnical;
        MatchIndex = matchIndex;
        MatchLength = matchLength;
    }
}
