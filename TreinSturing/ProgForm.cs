using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace TreinSturing
{
    public partial class ProgForm : Form
    {
        // Zelfde PLC-gegevens als in RunForm (mag je natuurlijk centraliseren)
        private const string PLC_IP = "192.168.0.1";
        private const int PLC_RACK = 0;
        private const int PLC_SLOT = 2;

        // Hoeveel bytes je per DB wilt laten zien in de tabel
        private const int DEFAULT_DB_LENGTH = 16;

        private PlcReader _plc;
        private bool _databasesLoaded = false;

        public ProgForm()
        {
            InitializeComponent();
        }

        private void ProgForm_Load(object sender, EventArgs e)
        {
            try
            {
                _plc = new PlcReader();
                var rc = _plc.Connect(PLC_IP, PLC_RACK, PLC_SLOT);
                if (rc != 0)
                {
                    MessageBox.Show($"Kan niet verbinden met PLC (code {rc}).",
                                    "PLC fout",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                    return;
                }

                // Zoek welke DB's er zijn
                LoadDatabaseListFromPlc();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij verbinden/initialiseren: {ex.Message}",
                                "Fout",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Scant een bereik van DB-nummers en vult de ComboBox
        /// met DB's die daadwerkelijk gelezen kunnen worden.
        /// </summary>
        private void LoadDatabaseListFromPlc()
        {
            if (_plc == null || !_plc.IsConnected)
                return;

            comboDb.Items.Clear();

            var foundDbs = new List<int>();

            // Simpele brute-force scan: pas bereik aan wat bij jouw PLC past
            for (int dbNumber = 1; dbNumber <= 80; dbNumber++)
            {
                try
                {
                    // We proberen gewoon 1 byte te lezen vanaf offset 0
                    var test = _plc.ReadDbBytes(dbNumber, 2, 1);
                    foundDbs.Add(dbNumber);
                }
                catch
                {
                    // DB bestaat niet of is niet leesbaar – negeren
                }
            }

            if (foundDbs.Count == 0)
            {
                MessageBox.Show("Geen datablocks gevonden in het opgegeven bereik (DB1–DB64).",
                                "Info",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                return;
            }

            // Vul de ComboBox, bijvoorbeeld als 'DB24'
            foreach (var db in foundDbs)
            {
                comboDb.Items.Add($"DB{db}");
            }

            _databasesLoaded = true;

            // Optioneel: direct eerste DB selecteren zodat de tabel meteen gevuld wordt
            comboDb.SelectedIndex = 0;
        }

        private void comboDb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_databasesLoaded || comboDb.SelectedItem == null)
                return;

            if (_plc == null || !_plc.IsConnected)
                return;

            // Haal DB-nummer uit de tekst ("DB24" -> 24)
            var text = comboDb.SelectedItem.ToString();
            int dbNumber;

            if (text.StartsWith("DB", StringComparison.OrdinalIgnoreCase))
                int.TryParse(text.Substring(2), out dbNumber);
            else
                int.TryParse(text, out dbNumber);

            if (dbNumber <= 0)
                return;

            try
            {
                // Lees die database éénmalig
                var data = _plc.ReadDbBytes(dbNumber, 2, DEFAULT_DB_LENGTH);
                UpdatePlcTable(data);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij lezen DB{dbNumber}: {ex.Message}",
                                "Leesfout",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        public void UpdatePlcTable(byte[] data)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdatePlcTable(data)));
                return;
            }

            var table = new DataTable();
            table.Columns.Add("Index");
            table.Columns.Add("Decimaal");
            table.Columns.Add("Hex");
            table.Columns.Add("Binair");

            for (int i = 0; i < data.Length; i++)
            {
                byte b = data[i];
                string hex = "0x" + b.ToString("X2");
                string bin = Convert.ToString(b, 2).PadLeft(8, '0');

                table.Rows.Add(i, b, hex, bin);
            }

            plcGrid.DataSource = table;
        }

        private void BackButton_Click(object sender, EventArgs e)
        {
            if (this.Owner != null)
            {
                this.Owner.Show();
            }
            this.Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _plc?.Disconnect();
            base.OnFormClosing(e);
        }
    }
}
