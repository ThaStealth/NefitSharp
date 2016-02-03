using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using DigitalThermostat.Properties;
using NefitSharp;
using NefitSharp.Entities;

namespace DigitalThermostat
{
    enum ScreenMode
    {
        MainScreen,BoilerScreen,SetpointScreen
    }

    public partial class FrmMain : Form
    {
        private NefitClient _client;
        private UIStatus _currentStatus;
        private SystemSettings? _settings;
        private ProgramSwitch[] _currentProgram;
        private int _switchBackTicks;
        private double _displaySetpoint;

        private ScreenMode _currentScreenMode;
        private RectangleF _manualProgramClickZone;
        private RectangleF _autoProgramClickZone;
        private RectangleF _temperatureUpClickZone;
        private RectangleF _temperatureDownClickZone;

        private RectangleF _boilerOnZone;
        private RectangleF _boilerOffZone;

        private RectangleF _contextMenu;
        private Point _mouseLocation;
        private static Color _lightBlueColor = Color.FromArgb(129, 183, 255);
        private static Color _blueColor = Color.FromArgb(0, 109, 254);
        private static Color _redColor = Color.FromArgb(251, 0, 0);

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

            _boilerOnZone = new RectangleF(333 * Settings.Default.scale,271 * Settings.Default.scale, 56 * Settings.Default.scale, 38 * Settings.Default.scale);
            _boilerOffZone = new RectangleF(333 * Settings.Default.scale, 351 * Settings.Default.scale, 56 * Settings.Default.scale, 38 * Settings.Default.scale);

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
            _client = new NefitClient(Settings.Default.serial, Settings.Default.accessKey, Settings.Default.password, Settings.Default.debugMode);
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
            try
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                if ((_client != null && !_client.Connected) || _currentStatus == null)
                {
                    e.Graphics.DrawString("X", new Font("Leelawadee UI", 110F * Settings.Default.scale, FontStyle.Regular), new SolidBrush(Color.Red), new PointF(250 * Settings.Default.scale, 221 * Settings.Default.scale));
                }
                if (_currentStatus != null)
                {
                    switch (_currentScreenMode)
                    {
                        default:
                            PaintNormalScreen(e);
                            break;
                        case ScreenMode.BoilerScreen:
                            PaintHotwaterScreen(e);
                            break;
                        case ScreenMode.SetpointScreen:
                            PaintSetpointScreen(e);
                            break;
                    }
                }
            }
            catch
            {
            }
        }

        private void PaintHotwaterScreen(PaintEventArgs e)
        {
            if (_currentStatus.UserMode == UserModes.Manual)
            {
                DrawScaledImage(e.Graphics, Resources.boilerOn, new Point(333, 271));
                DrawScaledImage(e.Graphics, Resources.boilerOff, new Point(333, 351));
            }
            else
            {
                DrawScaledImage(e.Graphics, Resources.boileronProgram, new Point(333, 271));
                DrawScaledImage(e.Graphics, Resources.boilerOffProgram, new Point(333, 351));
            }
            if (_currentStatus.HotWaterAvailable)
            {
                DrawScaledImage(e.Graphics, Resources.checkMark, new Point(253, 271));
            }
            else
            {
                DrawScaledImage(e.Graphics, Resources.checkMark, new Point(253, 351));
            }

            DrawScaledImage(e.Graphics, Resources._return, new Point(220, 555));
        }


        private void PaintSetpointScreen(PaintEventArgs e)
        {
            DrawScaledImage(e.Graphics, Resources.setpointTemperatureUp, new Point(282, 181));
            DrawScaledImage(e.Graphics, Resources.setpointTemperatureDown, new Point(282, 451));
            DrawScaledImage(e.Graphics, Resources.celcius, new Point(366, 268));
            e.Graphics.DrawString((int)_displaySetpoint + ",", new Font("Leelawadee UI", 90F*Settings.Default.scale, FontStyle.Regular), new SolidBrush(Color.White), new PointF(212*Settings.Default.scale, 241*Settings.Default.scale));
            e.Graphics.DrawString((Math.Round(_displaySetpoint - (int)_displaySetpoint, 1)*10).ToString(), new Font("Leelawadee UI", 45F*Settings.Default.scale, FontStyle.Regular), new SolidBrush(Color.White), new PointF(387*Settings.Default.scale, 304*Settings.Default.scale));

            if (_currentStatus.UserMode == UserModes.Clock && _currentProgram != null)
            {
                Pen whitePen = new Pen(Color.White, 16 * Settings.Default.scale);
                Pen whitePenSmall = new Pen(Color.White, 4 * Settings.Default.scale);
                for (int i = 0; i < 12; i++)
                {
                    int segmentStart = 271 + (i*29) + i;
                    e.Graphics.DrawArc(whitePen, 135*Settings.Default.scale, 144*Settings.Default.scale, 362*Settings.Default.scale, 362*Settings.Default.scale, segmentStart, 28);
                    e.Graphics.DrawArc(whitePenSmall, 147*Settings.Default.scale, 156*Settings.Default.scale, 338*Settings.Default.scale, 338*Settings.Default.scale, segmentStart, 28);
                }

                CurrentTimeIndicator(e);
            }
            else
            {
                e.Graphics.DrawArc(new Pen(Color.FromArgb(128, 128, 128), 16 * Settings.Default.scale), 135 * Settings.Default.scale, 144 * Settings.Default.scale, 362 * Settings.Default.scale, 362 * Settings.Default.scale, 0, 360);
            }

        }

        private void CurrentTimeIndicator(PaintEventArgs e)
        {
            double degrees = (30 * DateTime.Now.Hour + DateTime.Now.Minute / 60.0 * 28);
            double hourRadian;
            float fCenterX = 316 * Settings.Default.scale;
            float fCenterY = 325 * Settings.Default.scale;
            Color[] colors = new Color[]
            {
                    Color.FromArgb(61, 61, 61), Color.FromArgb(87, 87, 87), Color.FromArgb(124, 124, 124), Color.FromArgb(174, 174, 174),
            };
            for (int q = 0; q < 4; q++)
            {
                degrees--;
                if (degrees % 30 == 0)
                {
                    degrees--;
                }
                hourRadian = degrees * (Math.PI / 180);
                e.Graphics.DrawLine(new Pen(Color.Black, 6 * Settings.Default.scale), fCenterX + (float)(149F * Math.Sin(hourRadian)) * Settings.Default.scale, fCenterY - (float)(149F * Math.Cos(hourRadian)) * Settings.Default.scale, fCenterX + (float)(189F * Math.Sin(hourRadian)) * Settings.Default.scale, fCenterY - (float)(189F * Math.Cos(hourRadian)) * Settings.Default.scale);
                e.Graphics.DrawLine(new Pen(colors[q], 6 * Settings.Default.scale), fCenterX + (float)(173F * Math.Sin(hourRadian)) * Settings.Default.scale, fCenterY - (float)(173F * Math.Cos(hourRadian)) * Settings.Default.scale, fCenterX + (float)(189F * Math.Sin(hourRadian)) * Settings.Default.scale, fCenterY - (float)(189F * Math.Cos(hourRadian)) * Settings.Default.scale);
            }
            degrees = (30 * DateTime.Now.Hour + DateTime.Now.Minute / 60.0 * 28);
            hourRadian = degrees * (Math.PI / 180);
            e.Graphics.DrawLine(new Pen(Color.White, 6 * Settings.Default.scale), fCenterX + (float)(149F * Math.Sin(hourRadian)) * Settings.Default.scale, fCenterY - (float)(149F * Math.Cos(hourRadian)) * Settings.Default.scale, fCenterX + (float)(189F * Math.Sin(hourRadian)) * Settings.Default.scale, fCenterY - (float)(189F * Math.Cos(hourRadian)) * Settings.Default.scale);
        }

        private void PaintNormalScreen(PaintEventArgs e)
        {
            DrawScaledImage(e.Graphics, Resources.tempUp, new Point(293, 181));
            DrawScaledImage(e.Graphics, Resources.tempDown, new Point(294, 451));
            DrawScaledImage(e.Graphics, Resources.celcius, new Point(366, 268));
            e.Graphics.DrawString((int) _currentStatus.InHouseTemperature + ",", new Font("Leelawadee UI", 90F*Settings.Default.scale, FontStyle.Regular), new SolidBrush(Color.White), new PointF(212*Settings.Default.scale, 241*Settings.Default.scale));
            e.Graphics.DrawString((Math.Round(_currentStatus.InHouseTemperature - (int) _currentStatus.InHouseTemperature, 1)*10).ToString(), new Font("Leelawadee UI", 45F*Settings.Default.scale, FontStyle.Regular), new SolidBrush(Color.White), new PointF(387*Settings.Default.scale, 304*Settings.Default.scale));


            if (Math.Abs(_currentStatus.TemperatureSetpoint - _currentStatus.InHouseTemperature) >= 0.5)
            {
                Color spColor;
                if (_currentStatus.TemperatureSetpoint < _currentStatus.InHouseTemperature)
                {
                    spColor = _blueColor;
                }
                else
                {
                    spColor = _redColor;
                }

                e.Graphics.DrawString((int) _currentStatus.TemperatureSetpoint + ",", new Font("Leelawadee UI", 30F*Settings.Default.scale, FontStyle.Regular), new SolidBrush(spColor), new PointF(277*Settings.Default.scale, 197*Settings.Default.scale));
                e.Graphics.DrawString((Math.Round(_currentStatus.TemperatureSetpoint - (int) _currentStatus.TemperatureSetpoint, 1)*10).ToString(), new Font("Leelawadee UI", 16F*Settings.Default.scale, FontStyle.Regular), new SolidBrush(spColor), new PointF(339*Settings.Default.scale, 216*Settings.Default.scale));
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

            if (!_currentStatus.PowerSaveMode && !_currentStatus.HotWaterAvailable)
            {
                DrawScaledImage(e.Graphics, Resources.boilerOff, new Point(300, 395));
            }
            else if (_currentStatus.HotWaterAvailable && _currentStatus.PowerSaveMode)
            {
                DrawScaledImage(e.Graphics, Resources.leaf, new Point(300, 395));
            }
            else if (!_currentStatus.HotWaterAvailable && _currentStatus.PowerSaveMode)
            {
                DrawScaledImage(e.Graphics, Resources.boilerOff, new Point(270, 395));
                DrawScaledImage(e.Graphics, Resources.leaf, new Point(330, 395));
            }


            if (_currentStatus.UserMode == UserModes.Clock && _currentProgram != null)
            {
                Pen whitePen = new Pen(Color.White, 16*Settings.Default.scale);
                Pen bluePen = new Pen(_blueColor, 4*Settings.Default.scale);
                Pen lightBluePen = new Pen(_lightBlueColor, 4*Settings.Default.scale);
                Pen redPen = new Pen(_redColor, 4*Settings.Default.scale);
                for (int i = 0; i < 12; i++)
                {
                    int hour = i;
                    if (i < DateTime.Now.Hour && DateTime.Now.Hour < 12)
                    {
                        hour += 12;
                    }
                    int segmentStart = 271 + (i*29) + i;
                    e.Graphics.DrawArc(whitePen, 135*Settings.Default.scale, 144*Settings.Default.scale, 362*Settings.Default.scale, 362*Settings.Default.scale, segmentStart, 28);
                    Pen p;

                    bool showRedColor;

                    int segments;
                    if ((i == DateTime.Now.Hour%12 && _currentProgram[1].Timestamp.Hour - DateTime.Now.Hour < 12) || hour == _currentProgram[1].Timestamp.Hour)
                    {
                        segments = 28;
                    }
                    else
                    {
                        segments = 1;
                    }

                    bool currentStatus = _currentProgram[0].On;
                    if (_currentStatus.HedEnabled)
                    {
                        currentStatus = _currentStatus.HedDeviceAtHome;
                    }

                    for (int q = 0; q < segments; q++)
                    {
                        if (i == DateTime.Now.Hour%12 && _currentProgram[1].Timestamp.Hour - DateTime.Now.Hour < 12)
                        {
                            if (q < Convert.ToInt32(_currentProgram[1].Timestamp.Minute/2.14))
                            {
                                showRedColor = _currentProgram[1].On;
                            }
                            else
                            {
                                showRedColor = currentStatus;
                            }
                        }
                        else if (hour == _currentProgram[1].Timestamp.Hour)
                        {
                            if (q < Convert.ToInt32(_currentProgram[1].Timestamp.Minute/2.14))
                            {
                                showRedColor = currentStatus;
                            }
                            else
                            {
                                showRedColor = _currentProgram[1].On;
                            }
                        }
                        else
                        {
                            if (hour >= DateTime.Now.Hour && hour < _currentProgram[1].Timestamp.Hour)
                            {
                                showRedColor = currentStatus;
                            }
                            else
                            {
                                showRedColor = _currentProgram[1].On;
                            }
                        }
                        if (showRedColor)
                        {
                            p = redPen;
                        }
                        else
                        {
                            p = bluePen;
                        }

                        e.Graphics.DrawArc(p, 147*Settings.Default.scale, 156*Settings.Default.scale, 338*Settings.Default.scale, 338*Settings.Default.scale, segmentStart + ((28F/segments)*q), (28F/segments)*(q+1));
                    }
                }
                CurrentTimeIndicator(e);
            }

            else
            {
                e.Graphics.DrawArc(new Pen(Color.FromArgb(128, 128, 128), 16*Settings.Default.scale), 135*Settings.Default.scale, 144*Settings.Default.scale, 362*Settings.Default.scale, 362*Settings.Default.scale, 0, 360);
            }

            if (_currentStatus.UserMode == UserModes.Manual)
            {
                DrawScaledImage(e.Graphics, Resources.manualProgramActive, new Point(220, 555));
            }
            else
            {
                DrawScaledImage(e.Graphics, Resources.manualProgramInactive, new Point(220, 555));
            }

            if (_currentStatus.FireplaceMode && _currentStatus.UserMode == UserModes.Manual)
            {
                DrawScaledImage(e.Graphics, Resources.timerProgramFireplaceInactive, new Point(338, 555));
            }
            else if (_currentStatus.FireplaceMode && _currentStatus.UserMode == UserModes.Clock)
            {
                DrawScaledImage(e.Graphics, Resources.timerProgramFireplaceActive, new Point(338, 555));
            }
            else if (_currentStatus.HolidayMode && _currentStatus.UserMode == UserModes.Manual)
            {
                DrawScaledImage(e.Graphics, Resources.timerProgramHolidayInactive, new Point(338, 555));
            }
            else if (_currentStatus.HolidayMode && _currentStatus.UserMode == UserModes.Clock)
            {
                DrawScaledImage(e.Graphics, Resources.timerProgramHolidayActive, new Point(338, 555));
            }
            else if (_currentStatus.DayAsSunday && _currentStatus.UserMode == UserModes.Manual)
            {
                DrawScaledImage(e.Graphics, Resources.timerProgramSundayInactive, new Point(338, 555));
            }
            else if (_currentStatus.DayAsSunday && _currentStatus.UserMode == UserModes.Clock)
            {
                DrawScaledImage(e.Graphics, Resources.timerProgramSundayActive, new Point(338, 555));
            }
            else if (_currentStatus.HedEnabled && _currentStatus.HedDeviceAtHome && _currentStatus.UserMode == UserModes.Manual)
            {
                DrawScaledImage(e.Graphics, Resources.timerProgramHomeInactive, new Point(338, 555));
            }
            else if (_currentStatus.HedEnabled && _currentStatus.HedDeviceAtHome && _currentStatus.UserMode == UserModes.Clock)
            {
                DrawScaledImage(e.Graphics, Resources.timerProgramHomeActive, new Point(338, 555));
            }
            else if (_currentStatus.HedEnabled && !_currentStatus.HedDeviceAtHome && _currentStatus.UserMode == UserModes.Manual)
            {
                DrawScaledImage(e.Graphics, Resources.timerProgramNotHomeInactive, new Point(338, 555));
            }
            else if (_currentStatus.HedEnabled && !_currentStatus.HedDeviceAtHome && _currentStatus.UserMode == UserModes.Clock)
            {
                DrawScaledImage(e.Graphics, Resources.timerProgramNotHomeActive, new Point(338, 555));
            }
            else if (_currentStatus.ClockProgram == ClockProgram.Auto && _currentStatus.UserMode == UserModes.Manual)
            {
                DrawScaledImage(e.Graphics, Resources.timerProgramInactive, new Point(338, 555));
            }
            else if (_currentStatus.ClockProgram == ClockProgram.Auto && _currentStatus.UserMode == UserModes.Clock)
            {
                DrawScaledImage(e.Graphics, Resources.timerProgramActive, new Point(338, 555));
            }
            else if (_currentStatus.ClockProgram == ClockProgram.SelfLearning && _currentStatus.UserMode == UserModes.Manual)
            {
                DrawScaledImage(e.Graphics, Resources.timerProgramLearningInactive, new Point(338, 555));
            }
            else if (_currentStatus.ClockProgram == ClockProgram.SelfLearning && _currentStatus.UserMode == UserModes.Clock)
            {
                DrawScaledImage(e.Graphics, Resources.timerProgramLearningActive, new Point(338, 555));
            }
        }


        private void frmMain_Click(object sender, EventArgs e)
        {
            try
            {
                Point corrPos = new Point(MousePosition.X - Left, MousePosition.Y - Top);
                if (_client != null && _client.Connected)
                {
                    switch (_currentScreenMode)
                    {
                        default:
                            {
                                if (_temperatureUpClickZone.Contains(corrPos) || (_temperatureDownClickZone.Contains(corrPos)))
                                {
                                    _displaySetpoint = _currentStatus.TemperatureSetpoint;
                                    _currentScreenMode = ScreenMode.SetpointScreen;
                                    _switchBackTicks = 3000;
                                    Invalidate();
                                }

                                else if (_manualProgramClickZone.Contains(corrPos) && _currentStatus.UserMode == UserModes.Clock)
                                {
                                    _client.SetUserMode(UserModes.Manual);
                                }
                                else if (_autoProgramClickZone.Contains(corrPos) && _currentStatus.UserMode == UserModes.Manual)
                                {
                                    _client.SetUserMode(UserModes.Clock);
                                }
                                else if (_autoProgramClickZone.Contains(corrPos) && _currentStatus.UserMode == UserModes.Clock || (_manualProgramClickZone.Contains(corrPos) && _currentStatus.UserMode == UserModes.Manual))
                                {
                                    _currentScreenMode = ScreenMode.BoilerScreen;
                                    Invalidate();
                                }
                            }
                            break;
                        case ScreenMode.BoilerScreen:
                            if (_manualProgramClickZone.Contains(corrPos))
                            {
                                _currentScreenMode = ScreenMode.MainScreen;
                                Invalidate();
                            }
                            else if (_boilerOffZone.Contains(corrPos))
                            {
                                
                            }
                            else if (_boilerOnZone.Contains(corrPos))
                            {

                            }
                            break;
                        case ScreenMode.SetpointScreen:
                            _switchBackTicks = 3000;
                            if (_temperatureUpClickZone.Contains(corrPos))
                            {                                
                                if (_settings.HasValue)
                                {
                                    _displaySetpoint += _settings.Value.EasyTemperatureStep;
                                }
                                else
                                {
                                    _displaySetpoint += 0.5;
                                }
                                Invalidate();
                            }
                            else if (_temperatureDownClickZone.Contains(corrPos))
                            {                                
                                if (_settings.HasValue)
                                {
                                    _displaySetpoint -= _settings.Value.EasyTemperatureStep;
                                }
                                else
                                {
                                    _displaySetpoint -= 0.5;
                                }
                                Invalidate();
                            }                                                       
                            break;
                    }
                }
                if (_contextMenu.Contains(corrPos))
                {
                    ctxSettings.Show(MousePosition);
                }
            }
            catch
            {
            }
        }

        private async void tmrUpdate_Tick(object sender, EventArgs e)
        {
            try
            {
                if (_switchBackTicks > 0 && _currentScreenMode == ScreenMode.SetpointScreen)
                {
                    _switchBackTicks -= 1000;
                    if (_switchBackTicks <= 0)
                    {
                        if (_currentStatus!=null && _displaySetpoint != _currentStatus.TemperatureSetpoint)
                        {
                            _client.SetTemperature(_displaySetpoint);
                        }
                        _currentScreenMode = ScreenMode.MainScreen;
                        Invalidate();
                    }
                }
                if (_client.Connected)
                {
                    if (_client.AuthenticationError)
                    {
                        tmrUpdate.Stop();
                        MessageBox.Show("Authentication error, please recheck your credentials", "Authentication error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        settingsToolStripMenuItem_Click(this, new EventArgs());
                    }

                    if (_settings == null)
                    {
                        _settings = await _client.GetSystemSettingsAsync();
                    }
                    if (_currentProgram == null)
                    {
                        _currentProgram = await _client.GetCurrentAndNextSwitchAsync();
                    }
                    UIStatus stat = await _client.GetUIStatusAsync();
                    if (stat != null)
                    {
                        _currentStatus = stat;
                        Invalidate();
                    }
                }
            }
            catch
            {
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
