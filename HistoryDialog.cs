namespace GestionCryo;

public class HistoryDialog : Form
{
    private readonly Client _client;
    private readonly ListBox _list = new();
    private readonly ListBox _listAchats = new();
    private readonly Button _btnSupprimer = new();
    private readonly Button _btnSupprimerAchat = new();
    private readonly Button _btnFermer = new();
    private readonly Label _lblResume = new();
    private readonly Label _lblAchats = new();

    public HistoryDialog(Client client)
    {
        _client = client;
        InitializeUi();
        RefreshLists();
    }

    private void InitializeUi()
    {
        Text = $"Historique — {_client.Nom}";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(460, 540);

        _lblResume.SetBounds(12, 10, 430, 20);

        var lblSeances = new Label
        {
            Text = "Séances effectuées :",
            Left = 12,
            Top = 35,
            AutoSize = true
        };
        _list.SetBounds(12, 55, 430, 180);
        _list.HorizontalScrollbar = true;

        _btnSupprimer.Text = "Supprimer la séance (correction +1)";
        _btnSupprimer.SetBounds(12, 240, 240, 30);
        _btnSupprimer.Click += (s, e) => SupprimerSeance();

        _lblAchats.Text = "Crédits de séances (expiration 2 ans) :";
        _lblAchats.SetBounds(12, 280, 430, 20);
        _listAchats.SetBounds(12, 300, 430, 150);
        _listAchats.HorizontalScrollbar = true;

        _btnSupprimerAchat.Text = "Supprimer le crédit (correction -N)";
        _btnSupprimerAchat.SetBounds(12, 455, 240, 30);
        _btnSupprimerAchat.Click += (s, e) => SupprimerAchat();

        _btnFermer.Text = "Fermer";
        _btnFermer.SetBounds(342, 495, 100, 32);
        _btnFermer.DialogResult = DialogResult.Cancel;
        CancelButton = _btnFermer;

        Controls.AddRange(new Control[]
        {
            _lblResume, lblSeances, _list, _btnSupprimer,
            _lblAchats, _listAchats, _btnSupprimerAchat, _btnFermer
        });
    }

    private void RefreshLists()
    {
        _list.Items.Clear();
        foreach (var seance in _client.Seances.OrderByDescending(s => s.Date))
            _list.Items.Add($"{seance.Date:dd/MM/yyyy HH\\hmm}");

        _listAchats.Items.Clear();
        foreach (var achat in _client.Achats.OrderByDescending(a => a.Date))
            _listAchats.Items.Add(
                $"{achat.Nombre} séance(s) le {achat.Date:dd/MM/yyyy} — expire le {achat.Expiration:dd/MM/yyyy}" +
                (achat.EstExpire ? "  [EXPIRÉ]" : ""));

        _lblResume.Text = $"Séances restantes : {_client.SeancesRestantes} — " +
            $"Séances effectuées : {_client.Seances.Count} — Crédits : {_client.Achats.Sum(a => a.Nombre)}";
    }

    private void SupprimerSeance()
    {
        if (_list.SelectedIndex < 0)
        {
            MessageBox.Show("Sélectionnez une séance à supprimer.", "Aucune sélection",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var indexFromEnd = _list.Items.Count - 1 - _list.SelectedIndex;
        var seance = _client.Seances
            .OrderByDescending(s => s.Date)
            .ElementAtOrDefault(indexFromEnd);
        if (seance == null) return;

        var rep = MessageBox.Show(
            $"Supprimer la séance du {seance.Date:dd/MM/yyyy HH\\hmm} ?\nLe compteur sera réincrémenté (+1 séance restante).",
            "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (rep != DialogResult.Yes) return;

        _client.Seances.Remove(seance);
        _client.SeancesRestantes++;
        RefreshLists();
    }

    private void SupprimerAchat()
    {
        if (_listAchats.SelectedIndex < 0)
        {
            MessageBox.Show("Sélectionnez un crédit à supprimer.", "Aucune sélection",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var indexFromEnd = _listAchats.Items.Count - 1 - _listAchats.SelectedIndex;
        var achat = _client.Achats
            .OrderByDescending(a => a.Date)
            .ElementAtOrDefault(indexFromEnd);
        if (achat == null) return;

        var rep = MessageBox.Show(
            $"Supprimer le crédit de {achat.Nombre} séance(s) du {achat.Date:dd/MM/yyyy} ?\n" +
            $"Le compteur sera diminué de {achat.Nombre} (minimum 0).",
            "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (rep != DialogResult.Yes) return;

        _client.Achats.Remove(achat);
        _client.SeancesRestantes = Math.Max(0, _client.SeancesRestantes - achat.Nombre);
        RefreshLists();
    }
}
