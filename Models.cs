namespace GestionCryo;

public class Seance
{
    public DateTime Date { get; set; } = DateTime.Now;
}

public class Achat
{
    public DateTime Date { get; set; } = DateTime.Now;
    public int Nombre { get; set; } = 1;

    public DateTime Expiration => Date.AddYears(2);
    public bool EstExpire => DateTime.Now > Expiration;
}

public class Client
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nom { get; set; } = "";
    public string Telephone { get; set; } = "";
    public int SeancesRestantes { get; set; } = 3;
    public List<Seance> Seances { get; set; } = new();
    public List<Achat> Achats { get; set; } = new();

    public string Validite
    {
        get
        {
            if (Achats.Count == 0) return "";
            var plusProche = Achats.Min(a => a.Expiration);
            return plusProche.ToString("dd/MM/yyyy");
        }
    }

    public int JoursRestants
    {
        get
        {
            if (Achats.Count == 0) return -1;
            var plusProche = Achats.Min(a => a.Expiration);
            return (int)(plusProche.Date - DateTime.Now.Date).TotalDays;
        }
    }
}

public class ClientFile
{
    public List<Client> Clients { get; set; } = new();
}
