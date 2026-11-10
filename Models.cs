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
}

public class ClientFile
{
    public List<Client> Clients { get; set; } = new();
}
