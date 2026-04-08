using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TreinSturing.Application;
using TreinSturing.Configuration;
using TreinSturing.Domain;
using TreinSturing.Infrastructure;

namespace TreinSturing
{
    public partial class RunForm : Form
    {
        private CancellationTokenSource _cts;
        private Task _runTask;

        private ProgForm _progForm;

        private AppSettings _settings;
        private ILogSink _logSink;
        private IPlcReader _plcReader;
        private ITrainController _trainController;
        private TrainSyncService _trainSyncService;

        public RunForm()
        {
            InitializeComponent();
            StopButton.Enabled = false;
        }

        private void RunForm_Load(object sender, EventArgs e)
        {
            _settings = AppSettings.Load();

            _logSink = new DelegateLogSink(Log, Log);
            _plcReader = new Snap7PlcReader();
            _trainController = TrainControllerFactory.Create(_settings, _logSink);
            _trainSyncService = new TrainSyncService(_plcReader, _trainController, _settings, _logSink);

            Log($"Applicatie gestart. ControllerType = {_settings.ControllerType}");
        }

        private void Log(string msg)
        {
            var tb = Controls["textLog"] as TextBox;
            if (tb == null) return;

            var line = $"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}";

            if (tb.InvokeRequired)
            {
                tb.BeginInvoke((Action)(() => tb.AppendText(line)));
            }
            else
            {
                tb.AppendText(line);
            }
        }

        private void ProgButton_Click(object sender, EventArgs e)
        {
            if (_progForm == null || _progForm.IsDisposed)
                _progForm = new ProgForm();

            _progForm.Show();
            _progForm.BringToFront();
        }

        private void RunButton_Click(object sender, EventArgs e)
        {
            if (_runTask != null)
                return;

            RunButton.Enabled = false;
            StopButton.Enabled = true;

            _cts = new CancellationTokenSource();
            _runTask = RunAsync(_cts.Token);
        }

        private async Task RunAsync(CancellationToken ct)
        {
            try
            {
                await _trainSyncService.RunAsync(ct);
            }
            catch (OperationCanceledException)
            {
                Log("Run geannuleerd.");
            }
            catch (Exception ex)
            {
                Log("Run fout: " + ex.Message);
            }
            finally
            {
                try
                {
                    await _trainSyncService.StopAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Log("Stop fout: " + ex.Message);
                }

                ResetRunState();
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
            }
            catch
            {
            }
        }

        private void ResetRunState()
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)ResetRunState);
                return;
            }

            _cts?.Dispose();
            _cts = null;
            _runTask = null;

            RunButton.Enabled = true;
            StopButton.Enabled = false;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                _cts?.Cancel();
            }
            catch
            {
            }

            base.OnFormClosing(e);
        }
    }
}