using System.Text.Json.Serialization;

namespace DeltaAssetEditor.Core.Models;

/// <summary>
/// Представляет строковый ресурс из data.win для сериализации в JSON и привязки в GUI.
/// </summary>
public class StringEntry
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("is_technical")]
    public bool IsTechnical { get; set; }

    public StringEntry() { }

    public StringEntry(int id, string text, bool isTechnical = false)
    {
        Id = id;
        Text = text;
        IsTechnical = isTechnical;
    }
}
