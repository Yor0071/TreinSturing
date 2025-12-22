using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;

namespace TreinSturing
{
    public partial class RunForm : Form
    {
        // ===== SETTINGS (no UI) =====
        private const string PLC_IP = "192.168.0.1";
        private const int PLC_RACK = 0;
        private const int PLC_SLOT = 2;

        // Startadres en lengte per locomotief-DB
        private const int PLC_START = 2;
        private const int PLC_LEN = 16;

        private const int POLL_INTERVAL_MS = 200;
        // ============================

        // Dynamische lijst met gevonden locomotief-DB's
        private List<int> _locomotiveDbs = new List<int>();

        // Laatste snelheden per DB (DB-nummer -> snelheid-byte)
        private readonly Dictionary<int, byte> _lastSpeedByDb = new Dictionary<int, byte>();

        private readonly PlcReader _plc = new PlcReader();
        private CancellationTokenSource _cts;
        private Task _runTask;

        private ProgForm _progForm;
        private SerialPort _decoderPort;

        public RunForm()
        {
            InitializeComponent();
            StopButton.Enabled = false;
        }

        private void Log(string msg)
        {
            var tb = Controls["textLog"] as TextBox;
            if (tb != null)
            {
                if (tb.InvokeRequired)
                {
                    tb.BeginInvoke((Action)(() =>
                        tb.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}")
                    ));
                }
                else
                {
                    tb.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
                }
            }
        }

        private void RunForm_Load(object sender, EventArgs e)
        {
            // nothing needed
        }

        private void ProgButton_Click(object sender, EventArgs e)
        {
            if (_progForm == null || _progForm.IsDisposed)
                _progForm = new ProgForm();

            _progForm.Show();
        }

        private async void RunButton_Click(object sender, EventArgs e)
        {
            if (_runTask != null) return; // already running

            RunButton.Enabled = false;
            StopButton.Enabled = true;

            _cts = new CancellationTokenSource();
            _runTask = StartRunAsync(_cts.Token);
            // don't await: keep UI responsive; Stop will cancel
        }

        private async Task StartRunAsync(CancellationToken ct)
        {
            try
            {
                var rc = _plc.Connect(PLC_IP, PLC_RACK, PLC_SLOT);
                if (rc != 0)
                {
                    Log($"PLC connect error: code {rc}");
                    ResetRunState();
                    return;
                }

                Log($"PLC connected ({PLC_IP}). Locomotief-DB's zoeken...");

                // 1) Zoek automatisch welke DB's er bestaan
                _locomotiveDbs = DiscoverLocomotiveDbs();

                if (_locomotiveDbs.Count == 0)
                {
                    Log("Geen locomotief-DB's gevonden in het scanbereik.");
                    ResetRunState();
                    return;
                }

                Log("Gevonden DB's (locomotieven): " + string.Join(", ", _locomotiveDbs));

                // 2) Bij nieuwe run: oude snelheden leegmaken
                _lastSpeedByDb.Clear();

                // 3) Loop: elke DB lezen en met zijn eigen vorige waarde vergelijken
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        foreach (var dbNumber in _locomotiveDbs)
                        {
                            var data = _plc.ReadDbBytes(dbNumber, PLC_START, PLC_LEN);

                            // ---- DEBUG: toon huidige snelheid ----    
                            if (data.Length > 1)
                            {
                                byte speed = data[1];
                                Log($"DB{dbNumber}: huidige snelheid = {speed}");
                            }
                            // ----------------------------------------

                            // Vergelijkt alleen met zichzelf, niet met andere DB's:
                            CheckAndHandleSpeedChange(dbNumber, data);

                            // Als je in ProgForm één specifieke DB live wilt laten zien,
                            // kun je hier bijvoorbeeld alleen DB24 doorgeven:
                            /*
                            if (_progForm != null && !_progForm.IsDisposed && dbNumber == 24)
                            {
                                _progForm.UpdatePlcTable(data);
                            }
                            */
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Read error: {ex.Message}");
                    }

                    await Task.Delay(POLL_INTERVAL_MS, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // expected on Stop
            }
            catch (Exception ex)
            {
                Log($"Run exception: {ex.Message}");
            }
            finally
            {
                _plc.Disconnect();
                Log("Run stopped. PLC disconnected.");
                ResetRunState();
            }
        }


        private void SendSpeedToDecoder(int locoNumber, byte speedFromPlc)
        {
            try
            {
                EnsureDecoderPortOpen();

                // 6050: eerste byte = speed/command (0–31), tweede = loc adres (1–80)
                byte speedCommand = (byte)(speedFromPlc & 0x1F);  // neem alleen onderste 5 bits
                byte address = (byte)locoNumber;             // DB-nummer = locnummer (1–80)

                byte[] frame = new byte[] { speedCommand, address };

                _decoderPort.Write(frame, 0, frame.Length);

                string rawBits = Convert.ToString(speedFromPlc, 2).PadLeft(8, '0');
                Log(
                    $"➡️  6050 TX: rawSpeed={speedFromPlc} (bits {rawBits}) → " +
                    $"cmd={speedCommand} dec, addr={address}"
                );
            }
            catch (Exception ex)
            {
                Log($"❌ Decoder send error: {ex.Message}");
            }
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            StopRun();
        }

        private void StopRun()
        {
            try
            {
                _cts?.Cancel();
                _runTask?.Wait(500); // short join; optional
            }
            catch { /* ignore */ }
            finally
            {
                _cts?.Dispose();
                _cts = null;
                _runTask = null;
                RunButton.Enabled = true;
                StopButton.Enabled = false;
            }
        }

        private void ResetRunState()
        {
            if (InvokeRequired) { BeginInvoke((Action)ResetRunState); return; }
            _cts?.Dispose();
            _cts = null;
            _runTask = null;
            RunButton.Enabled = true;
            StopButton.Enabled = false;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_runTask != null)
                _cts?.Cancel(); // cleanup; finally{} will disconnect

            try
            {
                if (_decoderPort != null)
                {
                    if (_decoderPort.IsOpen)
                    {
                        _decoderPort.Close();
                        Log("Decoder-poort gesloten.");
                    }
                    _decoderPort.Dispose();
                    _decoderPort = null;
                }
            }
            catch (Exception ex)
            {
                Log($"Fout bij sluiten decoder-poort: {ex.Message}");
            }

            base.OnFormClosing(e);
        }


        /// <summary>
        /// Vergelijkt de snelheid (2e byte) in de DB met de vorige waarde
        /// voor dezelfde DB. Als deze is veranderd → stuur naar Märklin.
        /// </summary>
        private void CheckAndHandleSpeedChange(int dbNumber, byte[] data)
        {
            if (data == null || data.Length <= 1)
                return;

            byte currentSpeed = data[1];

            if (_lastSpeedByDb.TryGetValue(dbNumber, out var lastSpeed))
            {
                if (lastSpeed == currentSpeed)
                {
                    Log($"DB{dbNumber}: snelheid onveranderd ({currentSpeed} / {Convert.ToString(currentSpeed, 2).PadLeft(8, '0')})");
                    return;
                }

                Log($"DB{dbNumber}: snelheid gewijzigd van {lastSpeed} ({Convert.ToString(lastSpeed, 2).PadLeft(8, '0')}) " +
                    $"→ {currentSpeed} ({Convert.ToString(currentSpeed, 2).PadLeft(8, '0')})");
            }
            else
            {
                Log($"DB{dbNumber}: eerste meting, snelheid = {currentSpeed} ({Convert.ToString(currentSpeed, 2).PadLeft(8, '0')})");
            }

            _lastSpeedByDb[dbNumber] = currentSpeed;

            int locoNumber = dbNumber; // DB-nummer = locnummer

            SendSpeedToDecoder(locoNumber, currentSpeed);
        }

        /// <summary>
        /// Leest welke DB's er bestaan door in een bereik te testen.
        /// Elke gevonden DB zien we als 'locomotief-DB'.
        /// </summary>
        private List<int> DiscoverLocomotiveDbs()
        {
            var result = new List<int>();

            // Bepaal zelf het bereik: 1–64, 1–255, ...
            for (int dbNumber = 1; dbNumber <= 81; dbNumber++)
            {
                try
                {
                    // We proberen alleen even te lezen of hij bestaat:
                    // minimaal 2 bytes omdat we snelheid in byte 1 willen.
                    var test = _plc.ReadDbBytes(dbNumber, PLC_START, 2);

                    // Als we hier komen was er geen exception -> DB bestaat
                    result.Add(dbNumber);
                }
                catch
                {
                    // DB bestaat niet of is niet leesbaar → negeren
                }
            }

            return result;
        }

        private void EnsureDecoderPortOpen()
        {
            if (_decoderPort == null)
            {
                // PAS DIT AAN AAN JOUW HARDWARE:
                _decoderPort = new SerialPort("COM4", 2400, Parity.None, 8, StopBits.Two);
                _decoderPort.Handshake = Handshake.None;
                _decoderPort.NewLine = "\r\n";
            }

            if (!_decoderPort.IsOpen)
            {
                _decoderPort.Open();
                Log("Decoder-poort geopend (COM4, 2400 baud).");
            }
        }

    }
}
