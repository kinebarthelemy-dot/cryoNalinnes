using System.Text.Json;

namespace GestionCryo;

public static class Storage
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    private static string FilePath
    {
        get
        {
            var oneDrive = Environment.GetEnvironmentVariable("OneDrive");
            if (!string.IsNullOrEmpty(oneDrive) && Directory.Exists(oneDrive))
                return Path.Combine(oneDrive, "GestionCryo", "clients.json");

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GestionCryo", "clients.json");
        }
    }

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

    public static (List<Client> Nouveaux, List<Client> MisAJour, int Erreurs) ImportCsv(
        string path, IReadOnlyList<Client> existants)
    {
        var nouveaux = new List<Client>();
        var misAJour = new List<Client>();
        var erreurs = 0;

        try
        {
            var lignes = File.ReadAllLines(path);
            for (int i = 0; i < lignes.Length; i++)
            {
                var ligne = lignes[i].Trim();
                if (ligne.Length == 0) continue;
                if (i == 0 && ligne.StartsWith("Nom", StringComparison.OrdinalIgnoreCase)) continue;

                var parties = ligne.Split(';');
                if (parties.Length < 3 || string.IsNullOrWhiteSpace(parties[0]))
                {
                    erreurs++;
                    continue;
                }

                if (!int.TryParse(parties[2].Trim(), out int restantes))
                {
                    erreurs++;
                    continue;
                }

                var nom = parties[0].Trim();
                var tel = parties[1].Trim();
                var existant = existants.FirstOrDefault(
                    c => c.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase));

                if (existant != null)
                {
                    existant.Telephone = tel;
                    existant.SeancesRestantes = restantes;
                    misAJour.Add(existant);
                }
                else
                {
                    nouveaux.Add(new Client
                    {
                        Nom = nom,
                        Telephone = tel,
                        SeancesRestantes = restantes
                    });
                }
            }
        }
        catch
        {
            erreurs++;
        }

        return (nouveaux, misAJour, erreurs);
    }
}
