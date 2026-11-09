namespace GestionCryo;

public class HistoryDialog : Form
{
    private readonly Client _client;
    private readonly ListBox _list = new();
    private readonly Button _btnSupprimer = new();
    private readonly Button _btnFermer = new();
    private readonly Label _lblResume = new();

    public HistoryDialog(Client client)
    {
        _client = client;
        InitializeUi();
        RefreshList();
    }

    private void InitializeUi()
    {
        Text = $"Historique — {_client.Nom}";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(420, 380);

        _lblResume.SetBounds(12, 10, 390, 20);

        _list.SetBounds(12, 35, 390, 270);
        _list.HorizontalScrollbar = true;

        _btnSupprimer.Text = "Supprimer la séance (correction +1)";
        _btnSupprimer.SetBounds(12, 315, 240, 32);
        _btnSupprimer.Click += (s, e) => SupprimerSeance();

        _btnFermer.Text = "Fermer";
        _btnFermer.SetBounds(262, 315, 140, 32);
        _btnFermer.DialogResult = DialogResult.Cancel;
        CancelButton = _btnFermer;

        Controls.AddRange(new Control[] { _lblResume, _list, _btnSupprimer, _btnFermer });
    }

    private void RefreshList()
    {
        _list.Items.Clear();
        foreach (var seance in _client.Seances.OrderByDescending(s => s.Date))
            _list.Items.Add($"{seance.Date:dd/MM/yyyy HH:mm}");

        _lblResume.Text = $"Séances restantes : {_client.SeancesRestantes} — " +
            $"Séances effectuées : {_client.Seances.Count}";
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
            $"Supprimer la séance du {seance.Date:dd/MM/yyyy HH:mm} ?\nLe compteur sera réincrémenté (+1 séance restante).",
            "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (rep != DialogResult.Yes) return;

        _client.Seances.Remove(seance);
        _client.SeancesRestantes++;
        RefreshList();
    }
}
