using GestionCryo;

public partial class MainForm : Form
{
    private readonly ClientFile _file;
    private readonly DataGridView _grid = new();
    private readonly Button _btnNouveau = new();
    private readonly Button _btnSeance = new();
    private readonly Button _btnAjouterSeances = new();
    private readonly Button _btnHistorique = new();
    private readonly Button _btnAgenda = new();
    private readonly Button _btnModifier = new();
    private readonly Button _btnSupprimer = new();
    private readonly Button _btnExportJson = new();
    private readonly Button _btnExportCsv = new();
    private readonly Button _btnImport = new();
    private readonly Button _btnImportCsv = new();
    private readonly Label _lblInfo = new();

    public MainForm()
    {
        _file = Storage.Load();
        InitializeUi();
        RefreshGrid();
        VerifierExpiration();
    }

    private void VerifierExpiration()
    {
        var expires = _file.Clients
            .SelectMany(c => c.Achats, (c, a) => new { Client = c, Achat = a })
            .Where(x => x.Achat.EstExpire)
            .ToList();

        if (expires.Count == 0) return;

        var details = string.Join("\n", expires
            .OrderBy(x => x.Client.Nom)
            .Select(x =>
                $"- {x.Client.Nom} : {x.Achat.Nombre} séance(s) achetée(s) le {x.Achat.Date:dd/MM/yyyy}, expirée(s) le {x.Achat.Expiration:dd/MM/yyyy}"));

        var rep = MessageBox.Show(
            $"Des crédits de séances ont expiré (plus de 2 ans) :\n\n{details}\n\n" +
            "Retirer ces séances des compteurs ?",
            "Séances expirées", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (rep != DialogResult.Yes) return;

        foreach (var x in expires)
        {
            x.Client.Achats.Remove(x.Achat);
            x.Client.SeancesRestantes = Math.Max(0, x.Client.SeancesRestantes - x.Achat.Nombre);
        }
        SaveAndRefresh();
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
        _grid.AutoGenerateColumns = false;
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ColNom",
            DataPropertyName = "Nom",
            HeaderText = "Nom"
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ColTel",
            DataPropertyName = "Telephone",
            HeaderText = "Téléphone"
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ColRestantes",
            DataPropertyName = "SeancesRestantes",
            HeaderText = "Séances restantes"
        });
        _grid.DataBindingComplete += (s, e) =>
        {
            if (_grid.Columns.Count < 3) return;
            foreach (DataGridViewRow row in _grid.Rows)
            {
                var restant = row.Cells[2].Value as int?;
                if (restant == null) continue;
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

        _btnAjouterSeances.Text = "Ajouter séances";
        _btnAjouterSeances.AutoSize = true;
        _btnAjouterSeances.Click += (s, e) => AjouterSeances();

        _btnHistorique.Text = "Historique / Corriger";
        _btnHistorique.AutoSize = true;
        _btnHistorique.Click += (s, e) => VoirHistorique();

        _btnAgenda.Text = "Agenda / Journal";
        _btnAgenda.AutoSize = true;
        _btnAgenda.Click += (s, e) =>
        {
            using var agenda = new AgendaForm(_file);
            agenda.ShowDialog(this);
            if (agenda.Modifications)
                SaveAndRefresh();
        };

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

        _btnImportCsv.Text = "Importer CSV";
        _btnImportCsv.AutoSize = true;
        _btnImportCsv.Click += (s, e) => ImporterCsv();

        _lblInfo.AutoSize = true;
        _lblInfo.Padding = new Padding(12, 6, 0, 0);
        _lblInfo.ForeColor = Color.DimGray;

        panel.Controls.AddRange(new Control[]
        {
            _btnNouveau, _btnSeance, _btnAjouterSeances, _btnHistorique, _btnAgenda,
            _btnModifier, _btnSupprimer,
            _btnExportJson, _btnExportCsv, _btnImport, _btnImportCsv, _lblInfo
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
            if (dialog.Client!.SeancesRestantes > 0)
                dialog.Client.Achats.Add(new Achat
                {
                    Date = DateTime.Now,
                    Nombre = dialog.Client.SeancesRestantes
                });
            _file.Clients.Add(dialog.Client);
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
            MessageBox.Show(
                $"{client.Nom} n'a plus de séance. Utilisez le bouton « Ajouter séances » pour recharger son abonnement.",
                "Plus de séances", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        client.Seances.Add(new Seance { Date = DateTime.Now });
        client.SeancesRestantes--;
        SaveAndRefresh();

        MessageBox.Show(
            $"Séance enregistrée pour {client.Nom} le {DateTime.Now:dd/MM/yyyy HH:mm}.\nSéances restantes : {client.SeancesRestantes}.",
            "Séance enregistrée", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void AjouterSeances()
    {
        var client = GetSelectedOrWarn();
        if (client == null) return;

        var nombre = DemanderNombre(
            $"Combien de séances ajouter à {client.Nom} ?",
            $"Ajouter séances — {client.Nom}",
            1, 99, 3);
        if (nombre == null) return;

        client.SeancesRestantes += nombre.Value;
        client.Achats.Add(new Achat
        {
            Date = DateTime.Now,
            Nombre = nombre.Value
        });
        SaveAndRefresh();

        var expiration = DateTime.Now.AddYears(2);
        MessageBox.Show(
            $"{nombre.Value} séance(s) ajoutée(s) à {client.Nom}.\n" +
            $"Séances restantes : {client.SeancesRestantes}.\n\n" +
            $"Valables jusqu'au {expiration:dd/MM/yyyy} (2 ans).",
            "Séances ajoutées", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private int? DemanderNombre(string message, string titre, int min, int max, int defaut)
    {
        var lbl = new Label { Text = message, Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10, 10, 10, 0) };
        var num = new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Value = defaut,
            Left = 15,
            Top = 45,
            Width = 80
        };
        var btnOk = new Button { Text = "Ajouter", Left = 120, Top = 43, Width = 90, DialogResult = DialogResult.OK };
        var btnAnnuler = new Button { Text = "Annuler", Left = 220, Top = 43, Width = 90, DialogResult = DialogResult.Cancel };

        using var dialog = new Form
        {
            Text = titre,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(320, 90),
            AcceptButton = btnOk,
            CancelButton = btnAnnuler
        };
        dialog.Controls.AddRange(new Control[] { lbl, num, btnOk, btnAnnuler });

        return dialog.ShowDialog(this) == DialogResult.OK ? (int)num.Value : null;
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

    private void ImporterCsv()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Importer un fichier CSV",
            Filter = "Fichier CSV (*.csv)|*.csv"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var (nouveaux, misAJour, erreurs) = Storage.ImportCsv(dialog.FileName, _file.Clients);

        var rep = MessageBox.Show(
            $"Fichier : {dialog.FileName}\n\n" +
            $"{nouveaux.Count} nouveau(x) client(s)\n" +
            $"{misAJour.Count} client(s) existant(s) mis à jour\n" +
            $"{erreurs} ligne(s) ignorée(s)\n\n" +
            "Confirmer l'import ?",
            "Importer CSV", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (rep != DialogResult.Yes) return;

        _file.Clients.AddRange(nouveaux);
        SaveAndRefresh();

        MessageBox.Show("Import terminé.", "Importer CSV",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
