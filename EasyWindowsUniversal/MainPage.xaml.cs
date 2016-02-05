using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using NefitSharp;
using NefitSharp.Entities;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace EasyWindowsUniversal
{
    enum ScreenMode
    {
        MainScreen, BoilerScreen, SetpointScreen
    }

    internal class Conf
    {
        public string serial;
        public string accessKey;
        public string password;
        public double scale;
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private NefitClient _client;
        private UIStatus _currentStatus;
        private ProgramSwitch[] _currentProgram;
        private int _switchBackTicks;
        private double _displaySetpoint;
        private double _temperatureStep;
        private bool _temperatureStepDetermined;
        private Conf _config;

        private ScreenMode _currentScreenMode;
        //private RectangleF _manualProgramClickZone;
        //private RectangleF _autoProgramClickZone;
        //private RectangleF _temperatureUpClickZone;
        //private RectangleF _temperatureDownClickZone;

        //private RectangleF _boilerOnZone;
        //private RectangleF _boilerOffZone;

        //private RectangleF _contextMenu;
        private Point _mouseLocation;
        private static Color _lightBlueColor = Color.FromArgb(255, 129, 183, 255);
        private static Color _blueColor = Color.FromArgb(255, 0, 109, 254);
        private static Color _redColor = Color.FromArgb(255, 251, 0, 0);

        public MainPage()
        {
            InitializeComponent();
            _config = new Conf();
            _config.serial = "559914603";
            _config.accessKey = "ZGiesFgeHthmDQAJ";
            _config.password = "wiglil";
            _config.scale = 1;
            Start();
            //Paint();
        }

        private void Paint()
        {
            PaintNormalScreen();
        }

        private async void Start()
        {
            _temperatureStepDetermined = false;
            _currentStatus = null;
            _currentScreenMode = ScreenMode.MainScreen;
            _client = new NefitClient(_config.serial, _config.accessKey, _config.password);
            //_client.XMLLog += Log;
            if (await _client.ConnectAsync())
            {
                //  tmrUpdate.Enabled = true;
                tmrUpdate_Tick(this, new EventArgs());
            }
            else if (!_client.SerialAccessKeyValid)
            {
                //MessageBox.Show(@"Authentication error: serial or accesskey invalid, please recheck your credentials", @"Authentication error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //   settingsToolStripMenuItem_Click(this, new EventArgs());
            }
            else if (!_client.PasswordValid)
            {
                //MessageBox.Show(@"Authentication error: password invalid, please recheck your credentials", @"Authentication error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // settingsToolStripMenuItem_Click(this, new EventArgs());
            }
        }

        private void DrawString(double x, double y, string text, float fontSize, Color color)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Text = text;
            textBlock.FontSize = fontSize;
            textBlock.Foreground = new SolidColorBrush(color);
            Canvas.SetLeft(textBlock, x);
            Canvas.SetTop(textBlock, y);
            canvas.Children.Add(textBlock);
        }

        private void DrawScaledImage(string original, Point originalPos)
        {
            Image img = new Image();
            img.Source = new BitmapImage(new Uri("pack://application:,,,/EasyWindowsWPF;component/Resources/" + original + ".png", UriKind.Absolute));
            canvas.Children.Add(img);
            Canvas.SetTop(img, originalPos.Y);
            Canvas.SetLeft(img, originalPos.X);
        }

        //private void CurrentTimeIndicator(PaintEventArgs e)
        //{
        //    double degrees = (30 * DateTime.Now.Hour + DateTime.Now.Minute / 60.0 * 28);
        //    double hourRadian;
        //    float fCenterX = 316 * _config.scale;
        //    float fCenterY = 325 * _config.scale;
        //    Color[] colors = new Color[]
        //    {                
        //            Color.FromArgb(61, 61, 61), Color.FromArgb(87, 87, 87), Color.FromArgb(124, 124, 124), Color.FromArgb(174, 174, 174),
        //    };
        //    for (int q = 0; q < 4; q++)
        //    {
        //        degrees--;
        //        if (degrees % 30 == 0)
        //        {
        //            degrees--;
        //        }
        //        hourRadian = degrees * (Math.PI / 180);
        //        e.Graphics.DrawLine(new Pen(Color.Black, 6 * _config.scale), fCenterX + (float)(149F * Math.Sin(hourRadian)) * _config.scale, fCenterY - (float)(149F * Math.Cos(hourRadian)) * _config.scale, fCenterX + (float)(189F * Math.Sin(hourRadian)) * _config.scale, fCenterY - (float)(189F * Math.Cos(hourRadian)) * _config.scale);
        //        e.Graphics.DrawLine(new Pen(colors[q], 6 * _config.scale), fCenterX + (float)(173F * Math.Sin(hourRadian)) * _config.scale, fCenterY - (float)(173F * Math.Cos(hourRadian)) * _config.scale, fCenterX + (float)(189F * Math.Sin(hourRadian)) * _config.scale, fCenterY - (float)(189F * Math.Cos(hourRadian)) * _config.scale);
        //    }
        //    degrees = (30 * DateTime.Now.Hour + DateTime.Now.Minute / 60.0 * 28);
        //    hourRadian = degrees * (Math.PI / 180);
        //    e.Graphics.DrawLine(new Pen(Color.White, 6 * _config.scale), fCenterX + (float)(149F * Math.Sin(hourRadian)) * _config.scale, fCenterY - (float)(149F * Math.Cos(hourRadian)) * _config.scale, fCenterX + (float)(189F * Math.Sin(hourRadian)) * _config.scale, fCenterY - (float)(189F * Math.Cos(hourRadian)) * _config.scale);
        //}


        private void PaintNormalScreen()
        {
            DrawScaledImage("tempUp", new Point(293, 181));
            DrawScaledImage("tempDown", new Point(294, 451));
            DrawScaledImage("celcius", new Point(366, 268));
            DrawString(260, 256, (int)_currentStatus.InHouseTemperature + ",", 90F, Color.FromArgb(255, 255, 255, 255));
            DrawString(387, 304, (Math.Round(_currentStatus.InHouseTemperature - (int)_currentStatus.InHouseTemperature, 1) * 10).ToString(), 45F, Color.FromArgb(255, 255, 255, 255));
            //         if (Math.Abs(_currentStatus.TemperatureSetpoint - _currentStatus.InHouseTemperature) >= 0.5)
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

                DrawString(297, 200, (int)_currentStatus.TemperatureSetpoint + ",", 30F, spColor);
                DrawString(339, 216, (Math.Round(_currentStatus.TemperatureSetpoint - (int)_currentStatus.TemperatureSetpoint, 1) * 10).ToString(), 16F, spColor);
            }
            switch (_currentStatus.BoilerIndicator)
            {
                case BoilerIndicator.CentralHeating:
                    DrawScaledImage("flame", new Point(206, 322));
                    break;
                case BoilerIndicator.HotWater:
                    DrawScaledImage("boiler", new Point(190, 322));
                    break;
            }

            if (!_currentStatus.PowerSaveMode && !_currentStatus.HotWaterAvailable)
            {
                DrawScaledImage("boilerOff", new Point(300, 395));
            }
            else if (_currentStatus.HotWaterAvailable && _currentStatus.PowerSaveMode)
            {
                DrawScaledImage("leaf", new Point(300, 395));
            }
            else if (!_currentStatus.HotWaterAvailable && _currentStatus.PowerSaveMode)
            {
                DrawScaledImage("boilerOff", new Point(270, 395));
                DrawScaledImage("leaf", new Point(330, 395));
            }


            if (_currentStatus.UserMode == UserModes.Clock)
            {
                if (_currentProgram != null)
                {
                    //Pen whitePen = new Pen(Color.White, 16 * _config.scale);
                    Brush bluePen = new SolidColorBrush(_blueColor);
                    Brush redPen = new SolidColorBrush(_redColor);
                    //Pen lightBluePen = new Pen(_lightBlueColor, 4 * _config.scale);
                    for (int i = 0; i < 12; i++)
                    {
                        int hour = i;
                        if (i < DateTime.Now.Hour)
                        {
                            hour += 12;
                        }
                        int segmentStart = 271 + (i * 29) + i;
                        //DrawArc(Brushes.White, 16 * _config.scale, new Point(315, 326), 181, segmentStart, 28, canvas);
                        Brush p;

                        bool showRedColor;

                        int segments;
                        if ((i == DateTime.Now.Hour % 12 && _currentProgram[1].Timestamp.Hour - DateTime.Now.Hour < 12) || hour == _currentProgram[1].Timestamp.Hour)
                        {
                            segments = 28;
                        }
                        else
                        {
                            segments = 1;
                        }

                        bool currentStatus = _currentProgram[0].On;

                        for (int q = 0; q < segments; q++)
                        {
                            if (hour == DateTime.Now.Hour)
                            {
                                if (_currentProgram[1].Timestamp.Hour - 12 <= hour)
                                {
                                    if (DateTime.Now.Minute / 2.14 <= q)
                                    {
                                        showRedColor = currentStatus;
                                    }
                                    else if (q < Convert.ToInt32(_currentProgram[1].Timestamp.Minute / 2.14) || _currentProgram[1].Timestamp.Hour - 12 < hour)
                                    {
                                        //all minutes which have passed need to be in the new color
                                        showRedColor = _currentProgram[1].On;
                                    }
                                    else
                                    {
                                        showRedColor = currentStatus;
                                    }
                                }
                                else
                                {
                                    showRedColor = currentStatus;
                                }
                            }
                            else if (hour == _currentProgram[1].Timestamp.Hour)
                            {
                                if (q < Convert.ToInt32(_currentProgram[1].Timestamp.Minute / 2.14))
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
                            DrawArc(p, 4 * _config.scale, new Point(315, 326), 169, segmentStart, 28, canvas);
                        }
                    }
                    // CurrentTimeIndicator(e);
                }
                else
                {
                //    DrawArc(Brushes.White, 16 * _config.scale, new Point(315, 326), 181, 0, 360, canvas);
                }
            }
            else
            {
                DrawArc(new SolidColorBrush(Color.FromArgb(255, 128, 128, 128)), 16 * _config.scale, new Point(315, 326), 181, 0, 360, canvas);
            }

            if (_currentStatus.UserMode == UserModes.Manual)
            {
                //DrawScaledImage( "manualProgramActive", new Point(220, 555));
            }
            else
            {
                DrawScaledImage("manualProgramInactive", new Point(220, 555));
            }

            if (_currentStatus.FireplaceMode && _currentStatus.UserMode == UserModes.Manual)
            {
                DrawScaledImage("timerProgramFireplaceInactive", new Point(338, 555));
            }
            else if (_currentStatus.FireplaceMode && _currentStatus.UserMode == UserModes.Clock)
            {
                DrawScaledImage("timerProgramFireplaceActive", new Point(338, 555));
            }
            else if (_currentStatus.HolidayMode && _currentStatus.UserMode == UserModes.Manual)
            {
                DrawScaledImage("timerProgramHolidayInactive", new Point(338, 555));
            }
            else if (_currentStatus.HolidayMode && _currentStatus.UserMode == UserModes.Clock)
            {
                DrawScaledImage("timerProgramHolidayActive", new Point(338, 555));
            }
            else if (_currentStatus.DayAsSunday && _currentStatus.UserMode == UserModes.Manual)
            {
                DrawScaledImage("timerProgramSundayInactive", new Point(338, 555));
            }
            else if (_currentStatus.DayAsSunday && _currentStatus.UserMode == UserModes.Clock)
            {
                DrawScaledImage("timerProgramSundayActive", new Point(338, 555));
            }
            else if (_currentStatus.HedEnabled && _currentStatus.HedDeviceAtHome && _currentStatus.UserMode == UserModes.Manual)
            {
                DrawScaledImage("timerProgramHomeInactive", new Point(338, 555));
            }
            else if (_currentStatus.HedEnabled && _currentStatus.HedDeviceAtHome && _currentStatus.UserMode == UserModes.Clock)
            {
                DrawScaledImage("timerProgramHomeActive", new Point(338, 555));
            }
            else if (_currentStatus.HedEnabled && !_currentStatus.HedDeviceAtHome && _currentStatus.UserMode == UserModes.Manual)
            {
                DrawScaledImage("timerProgramNotHomeInactive", new Point(338, 555));
            }
            else if (_currentStatus.HedEnabled && !_currentStatus.HedDeviceAtHome && _currentStatus.UserMode == UserModes.Clock)
            {
                DrawScaledImage("timerProgramNotHomeActive", new Point(338, 555));
            }
            else if (_currentStatus.ClockProgram == ClockProgram.Auto && _currentStatus.UserMode == UserModes.Manual)
            {
                DrawScaledImage("timerProgramInactive", new Point(338, 555));
            }
            else if (_currentStatus.ClockProgram == ClockProgram.Auto && _currentStatus.UserMode == UserModes.Clock)
            {
                DrawScaledImage("timerProgramActive", new Point(338, 555));
            }
            else if (_currentStatus.ClockProgram == ClockProgram.SelfLearning && _currentStatus.UserMode == UserModes.Manual)
            {
                DrawScaledImage("timerProgramLearningInactive", new Point(338, 555));
            }
            else if (_currentStatus.ClockProgram == ClockProgram.SelfLearning && _currentStatus.UserMode == UserModes.Clock)
            {
                //     DrawScaledImage( "timerProgramLearningActive", new Point(338, 555));
            }
        }

        public void DrawArc(Brush clr, double width, Point center, double radius, double start_angle, double end_angle, Canvas canvas)
        {
            //start_angle = (start_angle * (2 * Math.PI)) / 360;
            //end_angle = start_angle + (end_angle * (2 * Math.PI)) / 360;

            //Path arc_path = new Path();
            //arc_path.Stroke = clr;
            //arc_path.StrokeThickness = width;
            //Canvas.SetLeft(arc_path, 0);
            //Canvas.SetTop(arc_path, 0);

            //start_angle = ((start_angle % (Math.PI * 2)) + Math.PI * 2) % (Math.PI * 2);
            //end_angle = ((end_angle % (Math.PI * 2)) + Math.PI * 2) % (Math.PI * 2);
            //if (end_angle < start_angle)
            //{
            //    double temp_angle = end_angle;
            //    end_angle = start_angle;
            //    start_angle = temp_angle;
            //}
            //if (start_angle == end_angle)
            //{
            //    DrawArc(clr, width, center, radius, start_angle, start_angle + Math.PI, canvas);
            //    DrawArc(clr, width, center, radius, start_angle + Math.PI, start_angle, canvas);
            //}
            //else
            //{
            //    double angle_diff = end_angle - start_angle;
            //    PathGeometry pathGeometry = new PathGeometry();
            //    PathFigure pathFigure = new PathFigure();
            //    ArcSegment arcSegment = new ArcSegment();
            //    arcSegment.IsLargeArc = angle_diff > Math.PI;
            //    Set start of arc
            //    pathFigure.StartPoint = new Point(center.X + radius * Math.Cos(start_angle), center.Y + radius * Math.Sin(start_angle));
            //    set end point of arc.
            //    arcSegment.Point = new Point(center.X + radius * Math.Cos(end_angle), center.Y + radius * Math.Sin(end_angle));
            //    arcSegment.Size = new Size(radius, radius);
            //    arcSegment.SweepDirection = SweepDirection.Clockwise;

            //    pathFigure.Segments.Add(arcSegment);
            //    pathGeometry.Figures.Add(pathFigure);
            //    arc_path.Data = pathGeometry;
            //    canvas.Children.Add(arc_path);
            //}
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
                        if (_currentStatus != null && _displaySetpoint != _currentStatus.TemperatureSetpoint)
                        {
                            _client.SetTemperature(_displaySetpoint);
                        }
                        _currentScreenMode = ScreenMode.MainScreen;
                        Paint();
                    }
                }
                if (_client.Connected)
                {
                    UIStatus stat = await _client.GetUIStatusAsync();
                    if (stat != null)
                    {
                        _currentStatus = stat;
                        Paint();
                    }
                    if (_currentProgram == null)
                    {
                        ProgramSwitch curr = await _client.GetCurrentSwitchPointAsync();
                        ProgramSwitch next = await _client.GetNextSwitchPointAsync();
                        if (curr != null && next != null)
                        {
                            _currentProgram = new ProgramSwitch[] { curr, next };
                            Paint();
                        }
                    }

                    if (!_temperatureStepDetermined)
                    {
                        _temperatureStep = await _client.EasyTemperatureStepAsync();
                        if (!double.IsNaN(_temperatureStep))
                        {
                            _temperatureStepDetermined = true;
                        }
                        else
                        {
                            _temperatureStep = 0.5;
                        }
                    }
                }
            }
            catch
            {
            }
        }
    }
}
