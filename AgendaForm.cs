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
        ClientSize = new Size(480, 420);

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
        _btnAujourdHui.Click += (s, e) =>
        {
            _datePicker.Value = DateTime.Today;
        };

        _lblResume.SetBounds(140, 50, 330, 22);
        _lblResume.ForeColor = Color.DimGray;

        _list.SetBounds(12, 85, 458, 290);
        _list.HorizontalScrollbar = true;

        _btnFermer.Text = "Fermer";
        _btnFermer.SetBounds(370, 383, 100, 30);
        _btnFermer.DialogResult = DialogResult.Cancel;
        CancelButton = _btnFermer;

        Controls.AddRange(new Control[]
        {
            _datePicker, _btnPrecedent, _btnSuivant, _btnAujourdHui,
            _lblResume, _list, _btnFermer
        });
    }

    private void ChangerJour(int delta)
    {
        _datePicker.Value = _datePicker.Value.AddDays(delta);
    }

    private void RefreshList()
    {
        var jour = _datePicker.Value.Date;
        _list.Items.Clear();

        var entrees = _file.Clients
            .SelectMany(c => c.Seances, (c, seance) => new { Client = c, Seance = seance })
            .Where(x => x.Seance.Date.Date == jour)
            .OrderBy(x => x.Seance.Date)
            .ToList();

        foreach (var x in entrees)
            _list.Items.Add($"{x.Seance.Date:HH\\hmm} — {x.Client.Nom}");

        _lblResume.Text = entrees.Count == 0
            ? "Aucune séance ce jour-là."
            : $"{entrees.Count} séance(s) le {jour:dd/MM/yyyy}.";
    }
}
