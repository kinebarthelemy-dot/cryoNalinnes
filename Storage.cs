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
            int idxNom = 0, idxTel = 1, idxSeances = 2;
            int debut = 0;

            if (lignes.Length > 0)
            {
                var sep = DetecterSeparateur(lignes[0]);
                var entete = lignes[0].ToLowerInvariant().Split(sep)
                    .Select(p => p.Trim().Trim('"')).ToArray();

                if (entete.Any(h => h.Contains("nom")))
                {
                    var iNom = IndexColonne(entete, new[] { "nom", "name" });
                    var iTel = IndexColonne(entete, new[] { "tel", "gsm", "phone", "portable" });
                    var iSe = IndexColonne(entete, new[] { "seance", "séance", "sance" });

                    if (iNom >= 0) idxNom = iNom;
                    if (iTel >= 0) idxTel = iTel;
                    if (iSe >= 0) idxSeances = iSe;
                    debut = 1;
                }
            }

            for (int i = debut; i < lignes.Length; i++)
            {
                var ligne = lignes[i].Trim();
                if (ligne.Length == 0) continue;

                var sep = DetecterSeparateur(ligne);
                var parties = ligne.Split(sep)
                    .Select(p => p.Trim().Trim('"'))
                    .ToArray();

                var maxNecessaire = Math.Max(idxNom, Math.Max(idxTel, idxSeances));
                if (parties.Length <= maxNecessaire || string.IsNullOrWhiteSpace(parties[idxNom]))
                {
                    erreurs++;
                    continue;
                }

                var nom = parties[idxNom].Trim();
                var tel = idxTel < parties.Length ? parties[idxTel].Trim() : "";
                var txtSeances = idxSeances < parties.Length ? parties[idxSeances].Trim() : "0";

                if (!int.TryParse(txtSeances, out int restantes))
                {
                    erreurs++;
                    continue;
                }

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

    private static char DetecterSeparateur(string ligne)
    {
        int pv = ligne.Count(c => c == ';');
        int v = ligne.Count(c => c == ',');
        int t = ligne.Count(c => c == '\t');

        if (v > pv && v > t) return ',';
        if (t > pv && t > v) return '\t';
        return ';';
    }

    private static int IndexColonne(string[] entete, string[] mots)
    {
        for (int i = 0; i < entete.Length; i++)
        {
            foreach (var mot in mots)
            {
                if (entete[i].Contains(mot))
                    return i;
            }
        }
        return -1;
    }
}
