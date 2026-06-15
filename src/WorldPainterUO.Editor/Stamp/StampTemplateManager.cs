namespace WorldPainterUO.Editor.Stamp;

public sealed class StampTemplateManager
{
    private readonly List<StampTemplate> _templates = [];
    private readonly string _searchPath;

    public StampTemplateManager(string searchPath)
    {
        _searchPath = searchPath;
    }

    public IReadOnlyList<StampTemplate> Templates => _templates;

    public void LoadAll()
    {
        _templates.Clear();

        if (!Directory.Exists(_searchPath))
            return;

        foreach (var file in Directory.GetFiles(_searchPath, "*.json"))
        {
            try
            {
                var template = StampTemplate.Load(file);
                if (template is not null && template.IsValid)
                    _templates.Add(template);
            }
            catch
            {
                // Skip invalid templates
            }
        }
    }

    public StampTemplate? FindByName(string name) =>
        _templates.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}
