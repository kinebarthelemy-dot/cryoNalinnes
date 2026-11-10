namespace GestionCryo;

public class ClientDialog : Form
{
    private readonly TextBox _txtNom = new();
    private readonly TextBox _txtTel = new();
    private readonly NumericUpDown _numSeances = new();
    private readonly Button _btnOk = new();
    private readonly Button _btnAnnuler = new();

    public Client? Client { get; private set; }
    private readonly Client? _existing;

    public ClientDialog(Client? existing)
    {
        _existing = existing;
        InitializeUi();
    }

    private void InitializeUi()
    {
        Text = _existing == null ? "Nouveau client" : "Modifier client";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(380, 200);

        var lblNom = new Label { Text = "Nom :", Left = 20, Top = 25, AutoSize = true };
        _txtNom.SetBounds(130, 22, 220, 25);

        var lblTel = new Label { Text = "Téléphone :", Left = 20, Top = 60, AutoSize = true };
        _txtTel.SetBounds(130, 57, 220, 25);

        var lblSeances = new Label { Text = "Séances restantes :", Left = 20, Top = 95, AutoSize = true };
        _numSeances.SetBounds(130, 92, 80, 25);
        _numSeances.Minimum = 0;
        _numSeances.Maximum = 99;
        _numSeances.Value = 3;

        _btnOk.Text = "Enregistrer";
        _btnOk.SetBounds(130, 140, 110, 32);
        _btnOk.Click += (s, e) => Valider();

        _btnAnnuler.Text = "Annuler";
        _btnAnnuler.SetBounds(250, 140, 100, 32);
        _btnAnnuler.DialogResult = DialogResult.Cancel;

        if (_existing != null)
        {
            _txtNom.Text = _existing.Nom;
            _txtTel.Text = _existing.Telephone;
            _numSeances.Value = Math.Min(_existing.SeancesRestantes, 99);

            if (_existing.Achats.Count > 0)
            {
                var jours = _existing.JoursRestants;
                var texte = $"Durée de validité : séances valables jusqu'au {_existing.Validite} " +
                    (jours >= 0 ? $"({jours} jour(s) restant(s))" : "(expiré)");
                var lblValidite = new Label
                {
                    Text = texte,
                    Left = 20,
                    Top = 175,
                    Width = 340,
                    AutoSize = false,
                    ForeColor = jours >= 0 && jours <= 60 ? Color.DarkOrange : Color.DimGray
                };
                ClientSize = new Size(380, 235);
                Controls.Add(lblValidite);
            }
        }

        AcceptButton = _btnOk;
        CancelButton = _btnAnnuler;
        Controls.AddRange(new Control[]
        {
            lblNom, _txtNom, lblTel, _txtTel, lblSeances, _numSeances, _btnOk, _btnAnnuler
        });
    }

    private void Valider()
    {
        if (string.IsNullOrWhiteSpace(_txtNom.Text))
        {
            MessageBox.Show("Le nom est obligatoire.", "Erreur",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (_existing != null)
        {
            _existing.Nom = _txtNom.Text.Trim();
            _existing.Telephone = _txtTel.Text.Trim();
            _existing.SeancesRestantes = (int)_numSeances.Value;
            Client = _existing;
        }
        else
        {
            Client = new Client
            {
                Nom = _txtNom.Text.Trim(),
                Telephone = _txtTel.Text.Trim(),
                SeancesRestantes = (int)_numSeances.Value
            };
        }

        DialogResult = DialogResult.OK;
    }
}
