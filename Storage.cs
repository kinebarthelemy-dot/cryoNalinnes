using System.Text.Json;

namespace GestionCryo;

public static class Storage
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    private static string FilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GestionCryo", "clients.json");

    public static ClientFile Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new ClientFile();
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<ClientFile>(json) ?? new ClientFile();
        }
        catch
        {
            return new ClientFile();
        }
    }

    public static void Save(ClientFile file)
    {
        var dir = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(file, Options));
    }

    public static void Export(ClientFile file, string path)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(file, Options));
    }

    public static ClientFile? Import(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            return JsonSerializer.Deserialize<ClientFile>(File.ReadAllText(path));
        }
        catch
        {
            return null;
        }
    }
}
