namespace GestionCryo;

public class AgendaForm : Form
{
    private readonly ClientFile _file;
    private readonly DateTimePicker _datePicker = new();
    private readonly Button _btnPrecedent = new();
    private readonly Button _btnSuivant = new();
    private readonly Button _btnAujourdHui = new();
    private readonly ListBox _list = new();
    private readonly Label _lblResume = new();
    private readonly Button _btnFermer = new();
    private readonly ComboBox _comboClients = new();
    private readonly Button _btnEncoder = new();
    private readonly Button _btnSupprimer = new();

    private List<(Client Client, Seance Seance)> _entrees = new();

    public bool Modifications { get; private set; }

    public AgendaForm(ClientFile file)
    {
        _file = file;
        InitializeUi();
        RefreshList();
    }

    private void InitializeUi()
    {
        Text = "Agenda / Journal des séances";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(480, 540);

        _datePicker.Format = DateTimePickerFormat.Long;
        _datePicker.SetBounds(12, 15, 220, 26);
        _datePicker.ValueChanged += (s, e) => RefreshList();

        _btnPrecedent.Text = "< Jour précédent";
        _btnPrecedent.SetBounds(240, 13, 120, 28);
        _btnPrecedent.Click += (s, e) => ChangerJour(-1);

        _btnSuivant.Text = "Jour suivant >";
        _btnSuivant.SetBounds(365, 13, 105, 28);
        _btnSuivant.Click += (s, e) => ChangerJour(1);

        _btnAujourdHui.Text = "Aujourd'hui";
        _btnAujourdHui.SetBounds(12, 45, 120, 28);
        _btnAujourdHui.Click += (s, e) => { _datePicker.Value = DateTime.Today; };

        _lblResume.SetBounds(140, 50, 330, 22);
        _lblResume.ForeColor = Color.DimGray;

        _list.SetBounds(12, 85, 458, 280);
        _list.HorizontalScrollbar = true;

        var lblEncoder = new Label
        {
            Text = "Encoder une séance pour :",
            Left = 12,
            Top = 378,
            AutoSize = true
        };

        _comboClients.SetBounds(12, 398, 250, 26);
        _comboClients.DropDownStyle = ComboBoxStyle.DropDownList;
        _comboClients.Sorted = true;

        _btnEncoder.Text = "Encoder (date choisie)";
        _btnEncoder.SetBounds(270, 396, 200, 28);
        _btnEncoder.Click += (s, e) => EncoderSeance();

        _btnSupprimer.Text = "Supprimer l'entrée sélectionnée (+1)";
        _btnSupprimer.SetBounds(12, 432, 250, 28);
        _btnSupprimer.Click += (s, e) => SupprimerEntree();

        _btnFermer.Text = "Fermer";
        _btnFermer.SetBounds(370, 500, 100, 30);
        _btnFermer.DialogResult = DialogResult.Cancel;
        CancelButton = _btnFermer;

        Controls.AddRange(new Control[]
        {
            _datePicker, _btnPrecedent, _btnSuivant, _btnAujourdHui,
            _lblResume, _list, lblEncoder, _comboClients, _btnEncoder,
            _btnSupprimer, _btnFermer
        });

        RemplirCombo();
    }

    private void RemplirCombo()
    {
        var selection = _comboClients.SelectedItem as string;
        _comboClients.Items.Clear();
        foreach (var c in _file.Clients.OrderBy(c => c.Nom, StringComparer.OrdinalIgnoreCase))
            _comboClients.Items.Add(c.Nom);
        if (selection != null && _comboClients.Items.Contains(selection))
            _comboClients.SelectedItem = selection;
        else if (_comboClients.Items.Count > 0)
            _comboClients.SelectedIndex = 0;
    }

    private void ChangerJour(int delta)
    {
        _datePicker.Value = _datePicker.Value.AddDays(delta);
    }

    private void RefreshList()
    {
        var jour = _datePicker.Value.Date;
        _list.Items.Clear();

        _entrees = _file.Clients
            .SelectMany(c => c.Seances, (c, seance) => (Client: c, Seance: seance))
            .Where(x => x.Seance.Date.Date == jour)
            .OrderBy(x => x.Seance.Date)
            .ToList();

        foreach (var x in _entrees)
            _list.Items.Add($"{x.Seance.Date:HH\\hmm} — {x.Client.Nom}");

        _lblResume.Text = _entrees.Count == 0
            ? "Aucune séance ce jour-là."
            : $"{_entrees.Count} séance(s) le {jour:dd/MM/yyyy}.";
    }

    private void EncoderSeance()
    {
        var nom = _comboClients.SelectedItem as string;
        if (string.IsNullOrEmpty(nom))
        {
            MessageBox.Show("Sélectionnez un client dans la liste.", "Aucun client",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var client = _file.Clients
            .FirstOrDefault(c => c.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase));
        if (client == null) return;

        var jour = _datePicker.Value.Date;
        var dateSeance = jour.Add(DateTime.Now.TimeOfDay);

        var rep = MessageBox.Show(
            $"Encoder une séance pour {client.Nom} le {dateSeance:dd/MM/yyyy} à {dateSeance:HH\\hmm} ?\n" +
            $"Séances restantes après encodage : {Math.Max(client.SeancesRestantes - 1, 0)}.",
            "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (rep != DialogResult.Yes) return;

        if (client.SeancesRestantes > 0)
            client.SeancesRestantes--;

        client.Seances.Add(new Seance { Date = dateSeance });
        Modifications = true;
        RefreshList();
    }

    private void SupprimerEntree()
    {
        if (_list.SelectedIndex < 0)
        {
            MessageBox.Show("Sélectionnez une entrée dans la liste.", "Aucune sélection",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var entree = _entrees[_list.SelectedIndex];
        var rep = MessageBox.Show(
            $"Supprimer la séance de {entree.Client.Nom} du {entree.Seance.Date:dd/MM/yyyy HH\\hmm} ?\n" +
            "Le compteur sera réincrémenté (+1 séance restante).",
            "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (rep != DialogResult.Yes) return;

        entree.Client.Seances.Remove(entree.Seance);
        entree.Client.SeancesRestantes++;
        Modifications = true;
        RemplirCombo();
        RefreshList();
    }
}
