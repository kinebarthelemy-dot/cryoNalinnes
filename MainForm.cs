using GestionCryo;

public partial class MainForm : Form
{
    private readonly ClientFile _file;
    private readonly DataGridView _grid = new();
    private readonly Button _btnNouveau = new();
    private readonly Button _btnSeance = new();
    private readonly Button _btnHistorique = new();
    private readonly Button _btnModifier = new();
    private readonly Button _btnSupprimer = new();
    private readonly Button _btnExportJson = new();
    private readonly Button _btnExportCsv = new();
    private readonly Button _btnImport = new();
    private readonly Label _lblInfo = new();

    public MainForm()
    {
        _file = Storage.Load();
        InitializeUi();
        RefreshGrid();
    }

    private void InitializeUi()
    {
        Text = "Gestion Cryo - Abonnements";
        MinimumSize = new Size(760, 480);
        StartPosition = FormStartPosition.CenterScreen;

        _grid.Dock = DockStyle.Fill;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.ReadOnly = true;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.DataBindingComplete += (s, e) =>
        {
            foreach (DataGridViewRow row in _grid.Rows)
            {
                var restant = (int)row.Cells["SeancesRestantes"].Value;
                row.DefaultCellStyle.BackColor = restant == 0
                    ? Color.MistyRose
                    : restant == 1 ? Color.LightYellow : Color.White;
            }
        };

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            Padding = new Padding(4)
        };

        _btnNouveau.Text = "Nouveau client";
        _btnNouveau.AutoSize = true;
        _btnNouveau.Click += (s, e) => NouveauClient();

        _btnSeance.Text = "Séance effectuée (-1)";
        _btnSeance.AutoSize = true;
        _btnSeance.Click += (s, e) => SeanceEffectuee();

        _btnHistorique.Text = "Historique / Corriger";
        _btnHistorique.AutoSize = true;
        _btnHistorique.Click += (s, e) => VoirHistorique();

        _btnModifier.Text = "Modifier client";
        _btnModifier.AutoSize = true;
        _btnModifier.Click += (s, e) => ModifierClient();

        _btnSupprimer.Text = "Supprimer client";
        _btnSupprimer.AutoSize = true;
        _btnSupprimer.Click += (s, e) => SupprimerClient();

        _btnExportJson.Text = "Sauvegarde (JSON)";
        _btnExportJson.AutoSize = true;
        _btnExportJson.Click += (s, e) => ExporterSauvegarde();

        _btnExportCsv.Text = "Exporter CSV";
        _btnExportCsv.AutoSize = true;
        _btnExportCsv.Click += (s, e) => ExporterCsv();

        _btnImport.Text = "Restaurer";
        _btnImport.AutoSize = true;
        _btnImport.Click += (s, e) => ImporterSauvegarde();

        _lblInfo.AutoSize = true;
        _lblInfo.Padding = new Padding(12, 6, 0, 0);
        _lblInfo.ForeColor = Color.DimGray;

        panel.Controls.AddRange(new Control[]
        {
            _btnNouveau, _btnSeance, _btnHistorique, _btnModifier, _btnSupprimer,
            _btnExportJson, _btnExportCsv, _btnImport, _lblInfo
        });

        Controls.Add(_grid);
        Controls.Add(panel);
    }

    private Client? SelectedClient => _grid.CurrentRow?.DataBoundItem as Client;

    private void RefreshGrid()
    {
        _grid.DataSource = null;
        _grid.DataSource = _file.Clients
            .OrderBy(c => c.Nom, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!_grid.Columns.Contains("Id")) return;
        _grid.Columns["Id"].Visible = false;
        _grid.Columns["Nom"].HeaderText = "Nom";
        _grid.Columns["Telephone"].HeaderText = "Téléphone";
        _grid.Columns["SeancesRestantes"].HeaderText = "Séances restantes";
        _grid.Columns["Seances"].Visible = false;

        _lblInfo.Text = $"{_file.Clients.Count} client(s) — données : %APPDATA%\\GestionCryo\\clients.json";
    }

    private void SaveAndRefresh()
    {
        Storage.Save(_file);
        RefreshGrid();
    }

    private Client? GetSelectedOrWarn()
    {
        var client = SelectedClient;
        if (client == null)
            MessageBox.Show("Sélectionnez d'abord un client dans la liste.",
                "Aucun client", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return client;
    }

    private void NouveauClient()
    {
        using var dialog = new ClientDialog(null);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _file.Clients.Add(dialog.Client!);
            SaveAndRefresh();
        }
    }

    private void ModifierClient()
    {
        var client = GetSelectedOrWarn();
        if (client == null) return;

        using var dialog = new ClientDialog(client);
        if (dialog.ShowDialog(this) == DialogResult.OK)
            SaveAndRefresh();
    }

    private void SeanceEffectuee()
    {
        var client = GetSelectedOrWarn();
        if (client == null) return;

        if (client.SeancesRestantes <= 0)
        {
            var rep = MessageBox.Show(
                $"{client.Nom} n'a plus de séance (0/3 ou plus). Voulez-vous quand même enregistrer une séance et repasser à 1 séance restante ?",
                "Plus de séances", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (rep != DialogResult.Yes) return;
            client.SeancesRestantes = 1;
        }

        client.Seances.Add(new Seance { Date = DateTime.Now });
        client.SeancesRestantes--;
        SaveAndRefresh();

        MessageBox.Show(
            $"Séance enregistrée pour {client.Nom} le {DateTime.Now:dd/MM/yyyy HH:mm}.\nSéances restantes : {client.SeancesRestantes}.",
            "Séance enregistrée", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void VoirHistorique()
    {
        var client = GetSelectedOrWarn();
        if (client == null) return;

        using var dialog = new HistoryDialog(client);
        dialog.ShowDialog(this);
        SaveAndRefresh();
    }

    private void SupprimerClient()
    {
        var client = GetSelectedOrWarn();
        if (client == null) return;

        var rep = MessageBox.Show(
            $"Supprimer définitivement {client.Nom} et son historique ({client.Seances.Count} séance(s)) ?",
            "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (rep != DialogResult.Yes) return;

        _file.Clients.Remove(client);
        SaveAndRefresh();
    }

    private void ExporterSauvegarde()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Sauvegarder les données",
            Filter = "Fichier JSON (*.json)|*.json",
            FileName = $"sauvegarde-cryo-{DateTime.Now:yyyy-MM-dd}.json",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        Storage.Export(_file, dialog.FileName);
        MessageBox.Show($"Sauvegarde créée :\n{dialog.FileName}",
            "Sauvegarde", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ExporterCsv()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Exporter en CSV",
            Filter = "Fichier CSV (*.csv)|*.csv",
            FileName = $"clients-cryo-{DateTime.Now:yyyy-MM-dd}.csv",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var lignes = new List<string> { "Nom;Telephone;Seances restantes;Seances effectuees" };
        foreach (var c in _file.Clients.OrderBy(c => c.Nom, StringComparer.OrdinalIgnoreCase))
        {
            lignes.Add($"{c.Nom};{c.Telephone};{c.SeancesRestantes};{c.Seances.Count}");
        }
        File.WriteAllLines(dialog.FileName, lignes);

        MessageBox.Show($"Export CSV créé :\n{dialog.FileName}",
            "Export CSV", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ImporterSauvegarde()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Restaurer une sauvegarde",
            Filter = "Fichier JSON (*.json)|*.json"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var imported = Storage.Import(dialog.FileName);
        if (imported?.Clients == null)
        {
            MessageBox.Show("Fichier invalide ou illisible.", "Erreur",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var rep = MessageBox.Show(
            $"La sauvegarde contient {imported.Clients.Count} client(s).\n" +
            "Remplacer toutes les données actuelles par cette sauvegarde ?",
            "Restaurer", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (rep != DialogResult.Yes) return;

        _file.Clients = imported.Clients;
        SaveAndRefresh();
        MessageBox.Show("Données restaurées.", "Restauration",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
