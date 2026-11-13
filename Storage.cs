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
        var lignes = new List<string[]>();

        try
        {
            foreach (var ligne in File.ReadAllLines(path))
            {
                var l = ligne.Trim();
                if (l.Length == 0) continue;
                var sep = DetecterSeparateur(l);
                lignes.Add(l.Split(sep).Select(p => p.Trim().Trim('"')).ToArray());
            }
        }
        catch
        {
            return (new List<Client>(), new List<Client>(), 1);
        }

        return TraiterLignes(lignes, existants);
    }

    public static (List<Client> Nouveaux, List<Client> MisAJour, int Erreurs) ImportXlsx(
        string path, IReadOnlyList<Client> existants)
    {
        var lignes = new List<string[]>();

        try
        {
            using var classeur = new ClosedXML.Excel.XLWorkbook(path);
            var feuille = classeur.Worksheets.First();
            var plage = feuille.RangeUsed();
            if (plage == null)
                return (new List<Client>(), new List<Client>(), 1);

            foreach (var ligne in plage.Rows())
            {
                var cellules = ligne.Cells(1, Math.Max(4, plage.ColumnCount()))
                    .Select(c => c.GetString().Trim())
                    .ToArray();
                if (cellules.All(string.IsNullOrEmpty)) continue;
                lignes.Add(cellules);
            }
        }
        catch
        {
            return (new List<Client>(), new List<Client>(), 1);
        }

        return TraiterLignes(lignes, existants);
    }

    private static (List<Client> Nouveaux, List<Client> MisAJour, int Erreurs) TraiterLignes(
        List<string[]> lignes, IReadOnlyList<Client> existants)
    {
        var nouveaux = new List<Client>();
        var misAJour = new List<Client>();
        var erreurs = 0;

        int idxNom = 0, idxSeances = 1, idxTel = 2, idxDate = 3, idxAchetees = -1;
        bool dateEstExpiration = true;
        int debut = 0;

        if (lignes.Count > 0)
        {
            var entete = lignes[0]
                .Select(h => h.ToLowerInvariant().Trim())
                .ToArray();

            if (entete.Any(h => h.Contains("nom")))
            {
                var iNom = IndexColonne(entete, new[] { "nom", "name" });
                var iTel = IndexColonne(entete, new[] { "tel", "gsm", "phone", "portable" });
                var iRestantes = IndexColonne(entete, new[] { "restante" });
                var iDate = IndexColonne(entete, new[] { "date", "validite", "validité", "achat", "apd", "historique", "expir" });

                var iAchetees = -1;
                for (int i = 0; i < entete.Length; i++)
                {
                    var h = entete[i];
                    if ((h.Contains("seance") || h.Contains("séance")) && !h.Contains("restante"))
                    {
                        iAchetees = i;
                        break;
                    }
                }

                if (iNom >= 0) idxNom = iNom;
                if (iTel >= 0) idxTel = iTel;
                if (iDate >= 0) idxDate = iDate;
                if (iRestantes >= 0) idxSeances = iRestantes;
                else if (iAchetees >= 0) idxSeances = iAchetees;
                idxAchetees = iAchetees >= 0 ? iAchetees : idxSeances;

                dateEstExpiration = iDate < 0 || entete[iDate].Contains("expir");
                debut = 1;
            }
        }

        for (int i = debut; i < lignes.Count; i++)
        {
            var parties = lignes[i];

            var maxNecessaire = Math.Max(idxNom, Math.Max(idxSeances, idxDate));
            if (parties.Length <= maxNecessaire || string.IsNullOrWhiteSpace(parties[idxNom]))
            {
                erreurs++;
                continue;
            }

            var nom = parties[idxNom].Trim();
            var tel = idxTel < parties.Length ? parties[idxTel].Trim() : "";
            var txtSeances = idxSeances < parties.Length ? parties[idxSeances].Trim() : "0";
            var txtDate = idxDate < parties.Length ? parties[idxDate].Trim() : "";
            var txtAchetees = idxAchetees >= 0 && idxAchetees < parties.Length
                ? parties[idxAchetees].Trim()
                : "";

            if (!int.TryParse(txtSeances, out int restantes))
            {
                erreurs++;
                continue;
            }

            DateTime? dateAchat = null;
            if (!string.IsNullOrWhiteSpace(txtDate))
            {
                var dateLue = ParserDate(txtDate);
                if (dateLue == null)
                {
                    erreurs++;
                    continue;
                }
                dateAchat = dateEstExpiration ? dateLue.Value.AddYears(-2) : dateLue.Value;
            }

            var existant = existants.FirstOrDefault(
                c => c.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase));

            Client client;
            if (existant != null)
            {
                existant.Telephone = tel;
                existant.SeancesRestantes = restantes;
                client = existant;
                misAJour.Add(existant);
            }
            else
            {
                client = new Client
                {
                    Nom = nom,
                    Telephone = tel,
                    SeancesRestantes = restantes
                };
                nouveaux.Add(client);
            }

            var nombreAchat = restantes;
            if (int.TryParse(txtAchetees, out int achetees) && achetees > 0)
                nombreAchat = achetees;

            if (dateAchat.HasValue && nombreAchat > 0)
                client.Achats.Add(new Achat
                {
                    Date = dateAchat.Value,
                    Nombre = nombreAchat
                });
        }

        return (nouveaux, misAJour, erreurs);
    }

    private static DateTime? ParserDate(string texte)
    {
        var formats = new[]
        {
            "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy",
            "yyyy-MM-dd", "dd.MM.yyyy", "d.M.yyyy"
        };

        foreach (var f in formats)
        {
            if (DateTime.TryParseExact(texte, f,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var d))
                return d;
        }

        if (DateTime.TryParse(texte,
            System.Globalization.CultureInfo.GetCultureInfo("fr-BE"),
            System.Globalization.DateTimeStyles.None, out var d2))
            return d2;

        return null;
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
