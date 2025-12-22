using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TreinSturing
{
    public partial class RunForm : Form
    {
        // ===== SETTINGS (no UI) =====
        private const string PLC_IP = "192.168.0.10";
        private const int PLC_RACK = 0;
        private const int PLC_SLOT = 1;

        private const int PLC_DB = 1;
        private const int PLC_START = 0;
        private const int PLC_LEN = 16;

        private const int POLL_INTERVAL_MS = 200; // adjust if needed
        // ============================

        private readonly PlcReader _plc = new PlcReader();
        private CancellationTokenSource _cts;
        private Task _runTask;

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
            var progForm = new ProgForm();
            progForm.Show();
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

                Log($"PLC connected ({PLC_IP}). Starting run loop.");

                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var data = _plc.ReadDbBytes(PLC_DB, PLC_START, PLC_LEN);

                        // Placeholder forwarding (Märklin comes later)
                        SendToDecoderPlaceholder(data);

                        // Optional debug logging (comment out if too chatty):
                        // Log($"Read {PLC_LEN} bytes: {string.Join(" ", data.Select(b => b.ToString("X2")))}");
                    }
                    catch (Exception ex)
                    {
                        Log($"Read error: {ex.Message}");
                        // Optionally break if you want to stop on errors:
                        // break;
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

        private void SendToDecoderPlaceholder(byte[] data)
        {
            // TODO: implement Märklin USB/Serial later.
            // For now, do nothing.
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

            base.OnFormClosing(e);
        }
    }
}
