using System.IO;
using System.Text.Json;

namespace GenSubtitle.App.Services;

public sealed class TaskCacheService
{
    private readonly string _cachePath;

    public TaskCacheService()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GenSubtitle");
        Directory.CreateDirectory(root);
        _cachePath = Path.Combine(root, "tasks.json");
    }

    public List<string> Load()
    {
        if (!File.Exists(_cachePath))
        {
            return new List<string>();
        }

        var json = File.ReadAllText(_cachePath);
        return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
    }

    public void Save(IEnumerable<string> paths)
    {
        var json = JsonSerializer.Serialize(paths.Distinct().ToList(), new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_cachePath, json);
    }
}
