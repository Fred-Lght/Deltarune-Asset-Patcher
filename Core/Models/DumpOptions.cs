namespace DeltaAssetEditor.Core.Models;

/// <summary>
/// Опции экспорта строк в JSON.
/// </summary>
public class DumpOptions
{
    /// <summary>
    /// Если true, системный мусор (пустые строки, технические идентификаторы GML) отфильтровывается.
    /// </summary>
    public bool FilterTechnical { get; set; } = true;

    /// <summary>
    /// Форматировать JSON с отступами.
    /// </summary>
    public bool Indented { get; set; } = true;

    /// <summary>
    /// Включать ли поле is_technical в JSON.
    /// </summary>
    public bool IncludeTechnicalFlag { get; set; } = true;
}
