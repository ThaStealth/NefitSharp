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

        private void DrawSegments(Graphics e)
        {
            DrawScaledImage(e, Resources.whiteSegment, new Point(224, 137));
            Bitmap segment2 = (Bitmap)Resources.whiteSegment.Clone();
            segment2.RotateFlip(RotateFlipType.RotateNoneFlipX);
            DrawScaledImage(e, segment2, new Point(320, 137));
            
            Bitmap segment3 = RotateImg(Resources.whiteSegment, (360 / 12) * 2, Color.Transparent);
            DrawScaledImage(e, segment3, new Point(401, 151));

            Bitmap segment4 = (Bitmap)Resources.whiteSegment.Clone();
            segment4.RotateFlip(RotateFlipType.Rotate270FlipXY);
            DrawScaledImage(e, segment4, new Point(468, 233));

            Bitmap segment5 = (Bitmap)segment2.Clone();
            segment5.RotateFlip(RotateFlipType.Rotate270FlipXY);
            DrawScaledImage(e, segment5, new Point(468, 328));

            Bitmap segment6 = RotateImg(Resources.whiteSegment, (360 / 12) * 5, Color.Transparent);
            DrawScaledImage(e, segment6, new Point(393, 410));

            Bitmap segment8 = (Bitmap)segment2.Clone();
            segment8.RotateFlip(RotateFlipType.RotateNoneFlipY);
            DrawScaledImage(e, segment8, new Point(319, 477));

            Bitmap segment7 = (Bitmap)Resources.whiteSegment.Clone();
            segment7.RotateFlip(RotateFlipType.RotateNoneFlipY);
            DrawScaledImage(e, segment7, new Point(224, 477));

            Bitmap segment9 = RotateImg(Resources.whiteSegment, (360 / 12) * 8, Color.Transparent);
            DrawScaledImage(e, segment9, new Point(154, 402));

            Bitmap segment10 = (Bitmap)segment5.Clone();
            segment10.RotateFlip(RotateFlipType.RotateNoneFlipX);
            DrawScaledImage(e, segment10, new Point(128, 328));

            Bitmap segment11 = (Bitmap)segment4.Clone();
            segment11.RotateFlip(RotateFlipType.RotateNoneFlipX);
            DrawScaledImage(e, segment11, new Point(128, 233));
            
            Bitmap segment12 = RotateImg(Resources.whiteSegment, (360 / 12) * 11, Color.Transparent);
            DrawScaledImage(e, segment12, new Point(142, 163));
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

                if (Math.Abs(_currentStatus.TemperatureSetpoint - _currentStatus.InHouseTemperature)>=0.5 )
                {
                    Color spColor;
                    if (_currentStatus.TemperatureSetpoint < _currentStatus.InHouseTemperature)
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

                if (_currentStatus.UserMode == UserModes.Clock)
                {
                    DrawSegments(e.Graphics);
                }
                else
                {
                    DrawScaledImage(e.Graphics, Resources.programOff, new Point(128, 137));
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
        }

        public static Bitmap RotateImg(Bitmap bmp, float angle, Color bkColor)
        {
            angle = angle % 360;
            if (angle > 180)
                angle -= 360;

            System.Drawing.Imaging.PixelFormat pf = default(System.Drawing.Imaging.PixelFormat);
            if (bkColor == Color.Transparent)
            {
                pf = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
            }
            else
            {
                pf = bmp.PixelFormat;
            }

            float sin = (float)Math.Abs(Math.Sin(angle * Math.PI / 180.0)); // this function takes radians
            float cos = (float)Math.Abs(Math.Cos(angle * Math.PI / 180.0)); // this one too
            float newImgWidth = sin * bmp.Height + cos * bmp.Width;
            float newImgHeight = sin * bmp.Width + cos * bmp.Height;
            float originX = 0f;
            float originY = 0f;

            if (angle > 0)
            {
                if (angle <= 90)
                    originX = sin * bmp.Height;
                else
                {
                    originX = newImgWidth;
                    originY = newImgHeight - sin * bmp.Width;
                }
            }
            else
            {
                if (angle >= -90)
                    originY = sin * bmp.Width;
                else
                {
                    originX = newImgWidth - sin * bmp.Height;
                    originY = newImgHeight;
                }
            }

            Bitmap newImg = new Bitmap((int)newImgWidth, (int)newImgHeight, pf);
            Graphics g = Graphics.FromImage(newImg);
            g.Clear(bkColor);
            g.TranslateTransform(originX, originY); // offset the origin to our calculated values
            g.RotateTransform(angle); // set up rotate
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
            g.DrawImageUnscaled(bmp, 0, 0); // draw the image at 0, 0
            g.Dispose();

            return newImg;
        }

        private void frmMain_Click(object sender, EventArgs e)
        {
            Point corrPos = new Point(MousePosition.X - Left, MousePosition.Y - Top);
            if (_client!=null && _client.Connected)
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
