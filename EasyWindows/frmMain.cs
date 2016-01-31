using System;
using System.Drawing;
using System.Windows.Forms;
using DigitalThermostat.Properties;
using NefitSharp;
using NefitSharp.Entities;

namespace DigitalThermostat
{
    public partial class FrmMain : Form
    {
        private NefitClient _client;
        private UIStatus _currentStatus;
        private SystemSettings? _settings;

        private RectangleF _manualProgramClickZone;
        private RectangleF _autoProgramClickZone;
        private RectangleF _temperatureUpClickZone;
        private RectangleF _temperatureDownClickZone;
        private RectangleF _contextMenu;
        private Point _mouseLocation;

        public FrmMain()
        {
            InitializeComponent();            
        } 

        private void Rescale()
        {            
            Width = Convert.ToInt32(635*Settings.Default.scale);
            Height = Convert.ToInt32(843*Settings.Default.scale);
            BackgroundImage = new Bitmap(Resources.nefit, Convert.ToInt32(Resources.nefit.Width*Settings.Default.scale), Convert.ToInt32(Resources.nefit.Height*Settings.Default.scale));

            _contextMenu = new RectangleF(495*Settings.Default.scale, 0, 140*Settings.Default.scale, 87*Settings.Default.scale);
            _manualProgramClickZone = new RectangleF(215*Settings.Default.scale, 550*Settings.Default.scale, 87*Settings.Default.scale, 97*Settings.Default.scale);
            _autoProgramClickZone = new RectangleF(335*Settings.Default.scale, 550*Settings.Default.scale, 87*Settings.Default.scale, 87*Settings.Default.scale);
            _temperatureUpClickZone = new RectangleF(290*Settings.Default.scale, 180*Settings.Default.scale, 50*Settings.Default.scale, 20*Settings.Default.scale);
            _temperatureDownClickZone = new RectangleF(290*Settings.Default.scale, 450*Settings.Default.scale, 50*Settings.Default.scale, 20*Settings.Default.scale);
            Invalidate();
        }

        private void frmMain_Shown(object sender, EventArgs e)
        {
            if (Settings.Default.firstStart)
            {
                settingsToolStripMenuItem_Click(sender, e);
            }
            else
            {
                Rescale();
                Start();
            }
        }

        private void Start()
        {
            _client = new NefitClient(Settings.Default.serial,Settings.Default.accessKey,Settings.Default.password);
            _client.Connect();
            if (_client.Connected)
            {
                tmrUpdate.Enabled = true;
            }
        }


        private void DrawScaledImage(Graphics e, Image original, Point originalPos)
        {
            Rectangle rect = new Rectangle(Convert.ToInt32(originalPos.X * Settings.Default.scale), Convert.ToInt32(originalPos.Y * Settings.Default.scale), Convert.ToInt32(original.Width * Settings.Default.scale), Convert.ToInt32(original.Height * Settings.Default.scale));
            e.DrawImage(original, rect, new Rectangle(0, 0, original.Width, original.Height), GraphicsUnit.Pixel);
        }

        private void frmMain_Paint(object sender, PaintEventArgs e)
        {
            if ((_client!=null && !_client.Connected) || _currentStatus == null)
            {
                e.Graphics.DrawString("X", new Font("Leelawadee UI", 110F * Settings.Default.scale, FontStyle.Regular), new SolidBrush(Color.Red), new PointF(250 * Settings.Default.scale, 221 * Settings.Default.scale));
            }

            if (_currentStatus != null)
            {
                DrawScaledImage(e.Graphics, Resources.tempUp, new Point(293, 181));
                DrawScaledImage(e.Graphics, Resources.tempDown, new Point(294, 451));
                DrawScaledImage(e.Graphics, Resources.celcius, new Point(366, 268));
                e.Graphics.DrawString((int) _currentStatus.InHouseTemperature + ",", new Font("Leelawadee UI", 90F * Settings.Default.scale, FontStyle.Regular), new SolidBrush(Color.White), new PointF(212 * Settings.Default.scale, 241 * Settings.Default.scale));
                e.Graphics.DrawString((Math.Round(_currentStatus.InHouseTemperature - (int)_currentStatus.InHouseTemperature, 1) * 10).ToString(), new Font("Leelawadee UI", 45F * Settings.Default.scale, FontStyle.Regular), new SolidBrush(Color.White), new PointF(387 * Settings.Default.scale, 304 * Settings.Default.scale));

                if (_currentStatus.PowerSaveMode || _currentStatus.BoilerIndicator == BoilerIndicator.CentralHeating)
                {
                    Color spColor;
                    if (_currentStatus.PowerSaveMode)
                    {
                        spColor = Color.FromArgb(0, 109, 254);
                    }
                    else
                    {
                        spColor = Color.FromArgb(251, 0, 0);
                    }

                    e.Graphics.DrawString((int) _currentStatus.TemperatureSetpoint + ",", new Font("Leelawadee UI", 30F*Settings.Default.scale, FontStyle.Regular), new SolidBrush(spColor), new PointF(277 * Settings.Default.scale, 197 * Settings.Default.scale));
                    e.Graphics.DrawString((Math.Round(_currentStatus.TemperatureSetpoint - (int) _currentStatus.TemperatureSetpoint, 1)*10).ToString(), new Font("Leelawadee UI", 16F * Settings.Default.scale, FontStyle.Regular), new SolidBrush(spColor), new PointF(339 * Settings.Default.scale, 216 * Settings.Default.scale));
                }
                switch (_currentStatus.BoilerIndicator)
                {
                    case BoilerIndicator.CentralHeating:
                        DrawScaledImage(e.Graphics, Resources.flame, new Point(206, 322));                        
                        break;
                    case BoilerIndicator.HotWater:
                        DrawScaledImage(e.Graphics, Resources.boiler, new Point(190, 322));                        
                        break;
                }
                if (_currentStatus.PowerSaveMode)
                {
                    DrawScaledImage(e.Graphics, Resources.leaf, new Point(300, 395));
                }                

                if (_currentStatus.ClockProgram == ClockProgram.SelfLearning && _currentStatus.UserMode == UserModes.Manual)
                {
                    DrawScaledImage(e.Graphics, Resources.manualProgramActive, new Point(220, 555));
                    DrawScaledImage(e.Graphics, Resources.timerProgramLearningInactive, new Point(338, 555));
                }
                else if (_currentStatus.ClockProgram == ClockProgram.SelfLearning && _currentStatus.UserMode == UserModes.Clock)
                {
                    DrawScaledImage(e.Graphics, Resources.timerProgramLearningActive, new Point(338, 555));
                    DrawScaledImage(e.Graphics, Resources.manualProgramInactive, new Point(220, 555));                    
                }
                else if (_currentStatus.ClockProgram == ClockProgram.Auto && _currentStatus.UserMode == UserModes.Manual)
                {
                    DrawScaledImage(e.Graphics, Resources.manualProgramActive, new Point(220, 555));
                    DrawScaledImage(e.Graphics, Resources.timerProgramInactive, new Point(338, 555));
                }
                else if (_currentStatus.ClockProgram == ClockProgram.Auto && _currentStatus.UserMode == UserModes.Clock)
                {
                    DrawScaledImage(e.Graphics, Resources.timerProgramActive, new Point(338, 555));
                    DrawScaledImage(e.Graphics, Resources.manualProgramInactive, new Point(220, 555));
                }
            }
        }

        private void frmMain_Click(object sender, EventArgs e)
        {
            Point corrPos = new Point(MousePosition.X - Left, MousePosition.Y - Top);
            if (_client.Connected)
            {
                if (_temperatureUpClickZone.Contains(corrPos))
                {
                    double newSetpoint = _currentStatus.TemperatureSetpoint;
                    if (_settings.HasValue)
                    {
                        newSetpoint += _settings.Value.EasyTemperatureStep;
                    }
                    else
                    {
                        newSetpoint += 0.5;
                    }
                    _client.SetTemperature(newSetpoint);
                }
                else if (_temperatureDownClickZone.Contains(corrPos))
                {
                    double newSetpoint = _currentStatus.TemperatureSetpoint;
                    if (_settings.HasValue)
                    {
                        newSetpoint -= _settings.Value.EasyTemperatureStep;
                    }
                    else
                    {
                        newSetpoint -= 0.5;
                    }
                    _client.SetTemperature(newSetpoint);
                }

                else if (_manualProgramClickZone.Contains(corrPos) && _currentStatus.UserMode == UserModes.Clock)
                {
                    _client.SetUserMode(UserModes.Manual);
                }
                else if (_autoProgramClickZone.Contains(corrPos) && _currentStatus.UserMode == UserModes.Manual)
                {
                    _client.SetUserMode(UserModes.Clock);
                }
            }
            if (_contextMenu.Contains(corrPos))
            {
                ctxSettings.Show(MousePosition);
            }
        }

        private async void tmrUpdate_Tick(object sender, EventArgs e)
        {
            if (_client.Connected)
            {
                if (_settings == null)
                {
                    _settings = await _client.GetSystemSettingsAsync();
                }

                UIStatus stat = await _client.GetUIStatusAsync();
                if (stat != null)
                {
                    _currentStatus = stat;
                    Invalidate();
                }
            }
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {            
            tmrUpdate.Enabled = false;
            if (_client != null)
            {
                _client.Disconnect();                
            }
            _client = null;
            FrmSettings settings = new FrmSettings();
            settings.ShowDialog();
            Rescale();           
            Start();
        }


        private void infoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmAbout about = new FrmAbout();
            about.ShowDialog();
        }
    
        private void FormMouseDown(object sender, MouseEventArgs e)
        {
            _mouseLocation = new Point(-e.X, -e.Y);
        }

        private void FormMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Point mousePos = MousePosition;
                mousePos.Offset(_mouseLocation.X, _mouseLocation.Y);
                Location = mousePos;
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
