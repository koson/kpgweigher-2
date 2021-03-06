﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Win32;
using System.Threading;
namespace Mndz
{
    public partial class Form1 : Form
    {
        private Processor processor;
        private ChoiceWnd dlg_choice;
        private kbdWnd dlg_kbd;
        private System.Windows.Forms.Timer tm;
        private RectButton[] kbd_btns;
        private RectButton[] rng_btns;
        private RectButton[] digi_upbtns;
        private RectButton[] digi_dnbtns;
        private Beep myBeep;
        [DllImport("coredll")]
        public static extern bool TouchCalibrate(); //设置本地时间
        private StringBuilder data;
        public static Decimal scale = 300;
        public static string s_scale = "300";
        public Form1()
        {

            DisablePowerSleep();

            InitializeComponent();
            myBeep = new Beep("PWM1:");

            if (this.label3.Text.IndexOf("1000A") >= 0)
                s_scale = "1000";
            else if (this.label3.Text.IndexOf("600A") >= 0)
                s_scale = "600";
            else
                s_scale = "300";

            scale = Decimal.Parse(s_scale);
            this.BackColor = Color.LightSkyBlue;
            led_current.ColorDark = this.BackColor;
            led_setting.ColorDark = this.BackColor;
            btn_turnon.BackColor = this.BackColor;
            btn_zeroon.BackColor = this.BackColor;
            rectMeter1.BackColor = this.BackColor;
            btn_turnon.BackColor = this.BackColor;
            rectMeter1.BgResId = "BGMETER";
            string mypath = Path.Combine(GlobalConfig.udiskdir2, "screen");
            if (Directory.Exists(mypath))
                btn_capture.Visible = true;
            Cursor.Hide();
            processor = new Processor();
            data = new StringBuilder(10);
            
            kbd_btns = new RectButton[]{ lbButton0,lbButton1,lbButton2,lbButton3,lbButton4,lbButton5,lbButton6,lbButton7,lbButton8,lbButton9,
                lbButtonCancel,lbButtonOK,lbButtonPT,lbButtonPercent};
            string[] btn_cap = "0,1,2,3,4,5,6,7,8,9,取消,设定,.,%".Split(new char[] { ',' });
            for (int i = 0; i < kbd_btns.Length; i++)
            {
                RectButton lbt = kbd_btns[i];
                lbt.BackColor = Color.Transparent;
                lbt.colorTop = Color.LightPink;
                lbt.colorShadow = Color.DarkGray;
                lbt.Label = btn_cap[i];
                lbt.Style = MyButtonType.raiseButton;
                lbt.Click += new EventHandler((o, e) =>
                {
                    this.Beep();
                    KeypadTick(NameInArray(o, kbd_btns));
                });
            }
            #region range switch
            rng_btns = new RectButton[] { rngbtn_1, rngbtn_10, rngbtn_100, rngbtn_300, rngbtn_600, rngbtn_1000 };

            for (int i = 0; i < rng_btns.Length; i++)
            {
                RectButton rb = rng_btns[i];
                rb.BackColor = this.BackColor;
                rb.colorShadow = this.BackColor;
                rb.colorTop = Color.LightGray;
                rb.Style = MyButtonType.rectButton;
                rb.ValidClick += new EventHandler((o, e) =>
                {
                    if (processor.bOn)
                        return;
                    processor.range = Int32.Parse((o as RectButton).Name.Remove(0, "rngbtn_".Length));
                    if (processor.setting > processor.range)
                        processor.setting = 0;
                    RefreshDisplay(false);
                    this.Beep();
                });
            }
            if (s_scale == "300")
            {
                this.rngbtn_1000.Visible = false;
                this.rngbtn_600.Visible = false;
            }
            if (s_scale == "600")
            {
                this.rngbtn_1000.Visible = false;
            }
            if (s_scale == "1000")
            {
                this.rngbtn_1.Visible = false;
                this.rngbtn_300.Visible = false;
                this.rngbtn_600.Visible = false;
                this.rngbtn_1000.Top = this.rngbtn_300.Top;
            }
            #endregion
            #region digi up and down

            digi_upbtns = new RectButton[] { rbtn_up1, rbtn_up2, rbtn_up3, rbtn_up4, rbtn_up5, rbtn_up6, rbtn_up7 };
            digi_dnbtns = new RectButton[] { rbtn_dn1, rbtn_dn2, rbtn_dn3, rbtn_dn4, rbtn_dn5, rbtn_dn6, rbtn_dn7 };
            for (int i = 0; i < digi_upbtns.Length; i++)
            {
                RectButton rb = digi_upbtns[i];
                rb.BackColor = this.BackColor;
                rb.colorShadow = this.BackColor;
                rb.colorTop = Color.DarkOrange;

                rb.bgScale = 3;
                rb.bOn = true;
                rb.Style = MyButtonType.triangleupButton;
                rb.ValidClick += new EventHandler((o, e) =>
                {
                    this.Beep();
                    TickDigit(digi_upbtns.Length - NameInArray(o, digi_upbtns), true);
                    RefreshDisplay(false);
                });
            }
            for (int i = 0; i < digi_dnbtns.Length; i++)
            {
                RectButton rb = digi_dnbtns[i];
                rb.BackColor = this.BackColor;
                rb.colorShadow = this.BackColor;
                rb.colorTop = Color.DarkOrange;

                rb.bgScale = 3;
                rb.bOn = true;
                rb.Style = MyButtonType.trianglednButton;

                rb.ValidClick += new EventHandler((o, e) =>
                {
                    this.Beep();
                    TickDigit(digi_upbtns.Length - NameInArray(o, digi_dnbtns), false);
                    RefreshDisplay(false);
                });
            }

            #endregion

            dlg_choice = new ChoiceWnd();
            dlg_kbd = new kbdWnd();
            led_setting.ColorLight = Color.DarkGreen;
            led_setting.ColorBackground = this.BackColor;
            led_setting.ElementWidth = 10;
            //          led_setting.RecreateSegments(led_setting.ArrayCount);
            led_current.ColorLight = Color.Red;
            led_current.ColorBackground = this.BackColor;
            led_current.ElementWidth = 12;
            //          led_current.RecreateSegments(led_current.ArrayCount);
            led_current.Value = "0.0000";
            btn_zeroon.Style = MyButtonType.rectButton;
            btn_zeroon.colorShadow = this.BackColor;
            btn_zeroon.colorTop = Color.Bisque;
            btn_zeroon.Label = "电流表清零";
            btn_zeroon.ValidClick += new EventHandler((o, e) =>
            {
                this.Beep();
                led_current.Value = "     ";
                processor.ZeroON();
            });

            //btn_turnon.bgColor = this.BackColor;
            //btn_turnon.SetStyle(Color.Green, MyButtonType.round2Button);
            btn_turnon.Label = "OFF";
            btn_turnon.Click += new EventHandler((o, e) =>
            {
                this.Beep();
                dt_lastoutput = DateTime.Now.AddSeconds(0.5);
                btn_turnon.colorTop = Color.LightYellow; //switching
            });

            tm = new System.Windows.Forms.Timer();
            tm.Interval = 500;
            tm.Tick += new EventHandler((o, e) =>
            {
                foreach (RectButton btn in kbd_btns)
                {
                    if (!btn.IsButtonUp)
                        btn.IsButtonUp = true;
                }
                if (btn_turnon.colorTop == Color.LightYellow) //still booting up
                {
                    processor.bOn = !processor.bOn;
                    RefreshDisplay(false);
                    return;
                }
                lbl_datetime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                if (DateTime.Now.Subtract(dt_lastoutput).TotalSeconds < 1)
                {
                    dt_lastoutput = DateTime.Now;
                    return;
                }
                if (data.Length == 0)
                {
                    processor.RefreshOutput();
                    if (processor.Current > -999)
                    {
                        UpdateCurrent(processor.Current);
                        processor.Current = -9999;
                    }
                }
            });
            tm.Enabled = true;
            processor.Reset();
            
            RefreshDisplay(true);
        }
        private static void DisablePowerSleep()
        {
            RegistryKey key3 = Registry.CurrentUser.OpenSubKey("ControlPanel", true).OpenSubKey("BackLight", true);
            if (key3.GetValue("UseExt").ToString() != "0")
            {
                key3.SetValue("UseExt", 0, RegistryValueKind.DWord);
            }
        }

 

        private void Beep()
        {
            myBeep.BeepLoad();
        }
        private void btn_capture_Click(object sender, EventArgs e)
        {
            btn_capture.Visible = false;
            this.Invalidate();
            Thread.Sleep(100);
            this.Invoke(new Action(() =>
            {
                Thread.Sleep(100);
                Form1.SaveScreen();
                btn_capture.Visible = true;
                MessageBox.Show("1-DONE!");
            }));
        }
        //up or down the digit based on position highest ditig is 1 lowest digit is 7
        //return value mean whether is valid or not
        internal void TickDigit(int position, bool bUp)
        {
            Decimal a;
            a = processor.setting;
            int unit = 0; //point position
            switch (processor.range)
            {
                case 1: unit = 1 - position; break;
                case 10: unit = 2 - position; break;
                case 100:
                case 300:
                case 600:
                    unit = 3 - position; break;
                case 1000: unit = 4 - position; break;
                default:
                    return;
            }

            if (bUp)
                a = a + Convert.ToDecimal(Math.Pow(10, unit));
            else
                a = a - Convert.ToDecimal(Math.Pow(10, unit));
            if((a < 0) || (a > processor.range))
                return;
            processor.setting = a;
        }

        private DateTime dt_lastoutput = DateTime.Now;
        private void CancelInput()
        {
            data.Remove(0, data.Length);
            led_setting.ColorLight = Color.DarkGreen;
            RefreshDisplay(false);
        }
        private void KeypadTick(int c)
        {
            //10 cancel, 11 ok, 12 pt, 13 percent
            if (c == 10)
            {
                CancelInput();
                return;
            }
            if (c == 13) //percent
            {
                try
                {

                    string n = (Decimal.Parse(data.ToString()) * 3).ToString();
                    data.Remove(0, data.Length);
                    data.Append(n);
                    c = 11;
                }
                catch
                {
                }
            }

            if (c == 11)
            {
                KbdData("value", data.ToString());
                CancelInput();

                return;
            }

            if (data.ToString().IndexOf(".") >= 0)
            {
                if (data.Length > 8)
                    return;
            }
            else
            {
                if (data.Length > 7)
                    return;
            }
            if (data.Length == 0) //newly input value
            {
                led_setting.ColorLight = Color.DarkMagenta;
            }

            if (c >= 0 && c <= 9)
            {
                data.Append(("0123456789")[c]);
            }

            if (c == 12)
            {
                if (data.ToString().IndexOf(".") >= 0)
                {
                    return;
                }
                data.Append('.');
            }
            led_setting.Value = data.ToString();
        }
        private int NameInArray(object o, object[] array)
        {
            string name = (o as Control).Name;
            for(int i=0;i<array.Length;i++)
            {
                if(name == (array[i] as Control).Name)
                    return i;
            }
            return -1;
        }
        public static void SaveScreen()
        {
      
            string mypath = Path.Combine(GlobalConfig.udiskdir2, "screen");
            if (!Directory.Exists(mypath))
                return;

            Random rnd = new Random();
            CaptureScreen.SaveScreenToFile(Path.Combine(mypath,rnd.Next().ToString()+".bmp"));
        }
        private void KbdData(string id, string param)
        {
                    Decimal a;
                    if (id == "daoffset")
                    {
                        if (!Util.TryDecimalParse(param, out a))
                            return;

                        processor.daoffset = a + processor.daoffset;
                        
                    }
                    if (id == "adscale")
                    {
                        double d;
                        if (!Util.TryDoubleParse(param, out d))
                            return;
                        if (processor.range.ToString() != s_scale)
                        {
                            Program.MsgShow("请选择满量程进行校准");
                            return;
                        }

                        if ((s_scale == "1000" && !processor.CalibrateADScale(d/1000.0)) ||
                            (s_scale == "600" && !processor.CalibrateADScale(d/1000.0)) ||
                            (s_scale == "300" && !processor.CalibrateADScale(d/1000.0)) )
                            Program.MsgShow("校准电流值失败.");
                    }
                    if (id == "value")
                    {
                        if (param == "5555555")
                        {
                            Process.GetCurrentProcess().Kill();
                            return;
                        }
                        if (param == "1234567") //calibration screen
                        {
                            Form1.TouchCalibrate();
                            /*
                            Process app = new Process();
                            app.StartInfo.WorkingDirectory = @"\Windows";
                            app.StartInfo.FileName = @"\Windows\TouchKit.exe";
                            app.StartInfo.Arguments = "";
                            app.Start();
                             */
                            return;
                        }
                        if (param == "00000")
                        {
                            Program.Upgrade();
                            return;
                        }
                        if (param == "6589019") //input standard resistance
                        {
                            this.Invoke(new Action(() =>
                            {
                                dlg_kbd.Init("请输入DA零位值", "daoffset", false, KbdData);
                            }));
                            return;
                        }
                        if (param == "1111111") //input external voltage value
                        {
                            this.Invoke(new Action(() =>
                            {
                                dlg_kbd.Init("请输入接入电流源实际值", "adscale", false, KbdData);
                            }));
                            return;
                        }
                        if (!Util.TryDecimalParse(param, out a))
                            return;

                        if (!IsValidCurrent(a))  //range check
                        {
                            this.Invoke(new Action(() => {
                                if (Form1.s_scale == "300")
                                    Program.MsgShow("输入值超出范围 (0-400 A)");
                                if (Form1.s_scale == "600")
                                    Program.MsgShow("输入值超出范围 (0-600 A)");
                                if (Form1.s_scale == "1000")
                                    Program.MsgShow("输入值超出范围 (0-1000 A)");

                            }));
                            return;
                        }
                        processor.setting = a;
                        if (Form1.s_scale == "300")
                            rectMeter1.Angle = Convert.ToInt32(Math.Floor( Convert.ToDouble(a) * 180 / 300.0));
                        if (Form1.s_scale == "600")
                            rectMeter1.Angle = Convert.ToInt32(Math.Floor(Convert.ToDouble(a) * 180 / (processor.range*1.0)));
                        if (Form1.s_scale == "1000")
                            rectMeter1.Angle = Convert.ToInt32(Math.Floor(Convert.ToDouble(a) * 180 / (processor.range * 1.0)));
                        
                        RefreshDisplay(true);
                    }
        }
        #region (NO USE) tick button
        /*
        private RectButton[] digi_dnbtns;
        private RectButton[] digi_upbtns;
        
        private RectButton Button(string type, int index)
        {
            if (type == "digi_up")
                return digi_upbtns[index];
            if (type == "digi_dn")
                return digi_dnbtns[index];
            if (type == "real")
                return real_btns[index];
            return null;
        }
        //up or down the digit based on position highest ditig is 1 lowest digit is 6
        internal void TickDigit(int position, bool bUp)
        {
            Decimal a;
            if (processor.iRange < 0) //real resi case
                return;

            a = processor.resistance;
            int unit = 0;
            unit = processor.iRange - position - 1;
            
            if(bUp)
                a = a + Convert.ToDecimal(Math.Pow(10,  unit));
            else
                a = a - Convert.ToDecimal(Math.Pow(10, unit));

            double b = Convert.ToDouble(a);
            if ((processor.iRange == 0) && (b > 0.02))
                return;
            if (((processor.iRange == 1) && (b > 0.2)))
                return;
            if (((processor.iRange == 2) && (b > 2)) )
                return;
            if (((processor.iRange == 3) && (b > 20)))
                return;
            if (b < 0)
                return;
            processor.resistance = a;
        }
        */
        #endregion
        public void UpdateCurrent(double reading)
        {
            string newcurr = "";
            switch(processor.range){
                case 1:
                    newcurr = reading.ToString("F6");
                    break;
                case 10:
                    newcurr = reading.ToString("F5");
                    break;
                case 100:
                case 300:
                case 600:
                    newcurr = reading.ToString("F4");
                    break;
                case 1000:
                    newcurr = reading.ToString("F3");
                    break;
                default: break; ;
            }
            if (newcurr != led_current.Value)
            {
                led_current.Value = newcurr;
                if (Form1.s_scale == "300")
                    rectMeter1.Angle = Convert.ToInt32(Math.Floor(Convert.ToDouble(reading) * 180 / 300.0));
                if (Form1.s_scale == "600")
                    rectMeter1.Angle = Convert.ToInt32(Math.Floor(Convert.ToDouble(reading) * 180 / (processor.range * 1.0)));
                if (Form1.s_scale == "1000")
                    rectMeter1.Angle = Convert.ToInt32(Math.Floor(Convert.ToDouble(reading) * 180 / (processor.range * 1.0)));
            }
        }
        private void RefreshDisplay(bool bRangeChange)
        {
            if (processor.bOn)
            {
                btn_turnon.Label = "ON";
                btn_turnon.colorTop = Color.Green;
                //btn_turnon.bOn = true;
            }
            else
            {
                btn_turnon.Label = "OFF";
                btn_turnon.colorTop = Color.LightGray;
                //btn_turnon.bOn = false;
            }
            switch(processor.range){
                case 1:
                    led_setting.Value = processor.setting.ToString("F6");
                    break;
                case 10:
                    led_setting.Value = processor.setting.ToString("F5");
                    break;
                case 100:
                case 300:
                case 600: 
                    led_setting.Value = processor.setting.ToString("F4");
                    break;
                case 1000:
                    led_setting.Value = processor.setting.ToString("F3");
                    break;
                default: break; ;
            }
            foreach (RectButton rb in rng_btns)
            {
                if (Int32.Parse(rb.Name.Remove(0, "rngbtn_".Length)) == processor.range)
                {
                    if (rb.colorTop != Color.Orange)
                        rb.colorTop = Color.Orange;
                }
                else
                {
                    if(rb.colorTop != Color.Gray)
                        rb.colorTop = Color.Gray;
                }
            }
        }
        //PC side command
        //curr: 1.234 on
        //curr: 1.345 off
        //setting? return setting: 1.234 on|off
        //curr? return curr: 1.234
        private Regex resi_set_mode = new Regex(@"curr:\s+([0-9.Mk]+)\s+(on|off)\s+(1|10|100|300|600|1000)$");
        internal void pc_cmd(string cmd)
        {
            Logger.SysLog(cmd);
            if (cmd == "*IDN?")
            {
                DeviceMgr.Report("RAYSTING RT" + s_scale + "A");
                return;
            }
            if (cmd == "H")
            {
                DeviceMgr.Reset();
                return;
            }
            if (cmd == "ZERO")
            {
                processor.ZeroON();
                return;
            }
            if (cmd == "curr?")
            {
                if (processor.bOn)
                    DeviceMgr.Report("curr: " + led_current.Value + " on");
                else
                    DeviceMgr.Report("curr: " + led_current.Value + " off");
                return;
            }
            Match m;

            m = resi_set_mode.Match(cmd);
            if (m.Success)
            {
                string rvalue = m.Groups[1].ToString();
                    Decimal a;
                    if (!Util.TryDecimalParse(m.Groups[1].ToString(), out a))
                        return;
                    if(!IsValidCurrent(a))  //range check
                        return;
                    
                    int rng = Int32.Parse(m.Groups[3].ToString());
                    if (a > rng)
                        return;
                    processor.range = rng;
                    processor.setting = a;
                    processor.bOn = (m.Groups[2].ToString() == "on");
                    RefreshDisplay(true);
                    cmd = "setting?";
            }
            if (cmd == "setting?")
            {
                if (processor.bOn)
                    DeviceMgr.Report("setting: " + led_setting.Value + " on " + processor.range.ToString());
                else
                    DeviceMgr.Report("setting: " + led_setting.Value + " off " + processor.range.ToString());
                return;
            }

        }
        public static bool IsValidCurrent(Decimal a)
        {
            if (Form1.s_scale == "300")
                return !(a < 0 || a > 400);
            if (Form1.s_scale == "600")
                return !(a < 0 || a > 600);
            if (Form1.s_scale == "1000")
                return !(a < 0 || a > 1000);

            return false;
        }

  
    }
    #region duplicate windows class
    public class GraphicsPath
    {
        private List<RectangleF> rectList;
        private List<double> a1list;
        private List<double> a2list;
        public GraphicsPath()
        {
            rectList = new List<RectangleF>();
            a1list = new List<double>();
            a2list = new List<double>();
        }
        public void AddArc(RectangleF start, double a1, double a2)
        {
            rectList.Add(start);
            a1list.Add(a1);
            a2list.Add(a2);
        }
    }
    
    public static class Brushes
    {
        public static Brush Black;
        public static Brush White;
        static Brushes()
        {
            Black = new System.Drawing.SolidBrush(Color.Black);
            White = new System.Drawing.SolidBrush(Color.White);
        }
    }
    public static class Pens
    {
        public static Pen Red;
        public static Pen Black;
        static Pens()
        {
            Red = new Pen(Color.Red);
            Black = new Pen(Color.Black);
        }
    }
    //manually simulating padding structure;
    // Summary:
    //     Represents padding or margin information associated with a user interface
    //     (UI) element.
    #endregion
    [Serializable]
    public struct Padding
    {
        // Summary:
        //     Provides a System.Windows.Forms.Padding object with no padding.
        public static readonly Padding Empty;
        //
        // Summary:
        //     Initializes a new instance of the System.Windows.Forms.Padding class using
        //     the supplied padding size for all edges.
        //
        // Parameters:
        //   all:
        //     The number of pixels to be used for padding for all edges.
        public Padding(int all)
        {
            _All = all;
            _Left = all;
            _Top = all;
            _Right = all;
            _Bottom = all;
        }
        //
        // Summary:
        //     Initializes a new instance of the System.Windows.Forms.Padding class using
        //     a separate padding size for each edge.
        //
        // Parameters:
        //   left:
        //     The padding size, in pixels, for the left edge.
        //
        //   top:
        //     The padding size, in pixels, for the top edge.
        //
        //   right:
        //     The padding size, in pixels, for the right edge.
        //
        //   bottom:
        //     The padding size, in pixels, for the bottom edge.
        public Padding(int left, int top, int right, int bottom)
        {
            _All = 0;
            _Left = left;
            _Top = top;
            _Right = right;
            _Bottom = bottom;
        }

        // Summary:
        //     Performs vector subtraction on the two specified System.Windows.Forms.Padding
        //     objects, resulting in a new System.Windows.Forms.Padding.
        //
        // Parameters:
        //   p1:
        //     The System.Windows.Forms.Padding to subtract from (the minuend).
        //
        //   p2:
        //     The System.Windows.Forms.Padding to subtract from (the subtrahend).
        //
        // Returns:
        //     The System.Windows.Forms.Padding result of subtracting p2 from p1.
        public static Padding operator -(Padding p1, Padding p2)
        {
            return new Padding(p1.Left - p2.Left, p1.Top - p2.Top, p1.Right - p2.Right, p1.Bottom - p2.Bottom);
        }
        //
        // Summary:
        //     Tests whether two specified System.Windows.Forms.Padding objects are not
        //     equivalent.
        //
        // Parameters:
        //   p1:
        //     A System.Windows.Forms.Padding to test.
        //
        //   p2:
        //     A System.Windows.Forms.Padding to test.
        //
        // Returns:
        //     true if the two System.Windows.Forms.Padding objects are different; otherwise,
        //     false.
        public static bool operator !=(Padding p1, Padding p2)
        {

            return !((p1.Left == p2.Left) &&
                (p1.Top == p2.Top) &&
                (p1.Right == p2.Right) &&
                (p1.Bottom == p2.Bottom));
        }
        //
        // Summary:
        //     Performs vector addition on the two specified System.Windows.Forms.Padding
        //     objects, resulting in a new System.Windows.Forms.Padding.
        //
        // Parameters:
        //   p1:
        //     The first System.Windows.Forms.Padding to add.
        //
        //   p2:
        //     The second System.Windows.Forms.Padding to add.
        //
        // Returns:
        //     A new System.Windows.Forms.Padding that results from adding p1 and p2.
        public static Padding operator +(Padding p1, Padding p2)
        {
            return new Padding(p1.Left + p2.Left, p1.Top + p2.Top, p1.Right + p2.Right, p1.Bottom + p2.Bottom);
        }
        //
        // Summary:
        //     Tests whether two specified System.Windows.Forms.Padding objects are equivalent.
        //
        // Parameters:
        //   p1:
        //     A System.Windows.Forms.Padding to test.
        //
        //   p2:
        //     A System.Windows.Forms.Padding to test.
        //
        // Returns:
        //     true if the two System.Windows.Forms.Padding objects are equal; otherwise,
        //     false.
        public static bool operator ==(Padding p1, Padding p2)
        {
            return ((p1.Left == p2.Left) &&
                (p1.Top == p2.Top) &&
                (p1.Right == p2.Right) &&
                (p1.Bottom == p2.Bottom));
        }

        // Summary:
        //     Gets or sets the padding value for all the edges.
        //
        // Returns:
        //     The padding, in pixels, for all edges if the same; otherwise, -1.
        private int _All;
        public int All { get{
                return _All;
            }
            set
            {
                _All = value;
            }
        }
        //
        // Summary:
        //     Gets or sets the padding value for the bottom edge.
        //
        // Returns:
        //     The padding, in pixels, for the bottom edge.
        private int _Bottom;
        public int Bottom { get { return _Bottom; } set { _Bottom = value; } }

        //
        // Summary:
        //     Gets the combined padding for the right and left edges.
        //
        // Returns:
        //     Gets the sum, in pixels, of the System.Windows.Forms.Padding.Left and System.Windows.Forms.Padding.Right
        //     padding values.
        public int Horizontal
        {
            get
            {

                return Left + Right;
            }
        }
        //
        // Summary:
        //     Gets or sets the padding value for the left edge.
        //
        // Returns:
        //     The padding, in pixels, for the left edge.
        private int _Left;
        public int Left { get { return _Left; } set { _Left = value;} }
        //
        // Summary:
        //     Gets or sets the padding value for the right edge.
        //
        // Returns:
        //     The padding, in pixels, for the right edge.
        private int _Right;
        public int Right { get { return _Right; } set { _Right = value; } }

        //
        // Summary:
        //     Gets the padding information in the form of a System.Drawing.Size.
        //
        // Returns:
        //     A System.Drawing.Size containing the padding information.
        public Size Size
        {
            get
            {
                return new Size(this.Horizontal, this.Vertical);
            }
        }
        //
        // Summary:
        //     Gets or sets the padding value for the top edge.
        //
        // Returns:
        //     The padding, in pixels, for the top edge.
        private int _Top;
        public int Top { get { return _Top; } set { _Top = value; } }

        //
        // Summary:
        //     Gets the combined padding for the top and bottom edges.
        //
        // Returns:
        //     Gets the sum, in pixels, of the System.Windows.Forms.Padding.Top and System.Windows.Forms.Padding.Bottom
        //     padding values.
        public int Vertical
        {
            get
            {
                return Bottom + Top;
            }
        }

        // Summary:
        //     Computes the sum of the two specified System.Windows.Forms.Padding values.
        //
        // Parameters:
        //   p1:
        //     A System.Windows.Forms.Padding.
        //
        //   p2:
        //     A System.Windows.Forms.Padding.
        //
        // Returns:
        //     A System.Windows.Forms.Padding that contains the sum of the two specified
        //     System.Windows.Forms.Padding values.
        public static Padding Add(Padding p1, Padding p2)
        {
            return new Padding(p1.Left + p2.Left, p1.Top + p2.Top, p1.Right + p2.Right, p1.Bottom + p2.Bottom);
        }
        //
        // Summary:
        //     Determines whether the value of the specified object is equivalent to the
        //     current System.Windows.Forms.Padding.
        //
        // Parameters:
        //   other:
        //     The object to compare to the current System.Windows.Forms.Padding.
        //
        // Returns:
        //     true if the System.Windows.Forms.Padding objects are equivalent; otherwise,
        //     false.
        public override bool Equals(object other)
        {
            return base.Equals(other);
        }

        //
        // Summary:
        //     Generates a hash code for the current System.Windows.Forms.Padding.
        //
        // Returns:
        //     A 32-bit signed integer hash code.
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        //
        // Summary:
        //     Subtracts one specified System.Windows.Forms.Padding value from another.
        //
        // Parameters:
        //   p1:
        //     A System.Windows.Forms.Padding.
        //
        //   p2:
        //     A System.Windows.Forms.Padding.
        //
        // Returns:
        //     A System.Windows.Forms.Padding that contains the result of the subtraction
        //     of one specified System.Windows.Forms.Padding value from another.
        public static Padding Subtract(Padding p1, Padding p2)
        {
            return new Padding(p1.Left - p2.Left, p1.Top - p2.Top, p1.Right - p2.Right, p1.Bottom - p2.Bottom);
        }
        //
        // Summary:
        //     Returns a string that represents the current System.Windows.Forms.Padding.
        //
        // Returns:
        //     A System.String that represents the current System.Windows.Forms.Padding.
        public override string ToString()
        {
            return String.Format("{0},{1},{2},{3}", this.Left, this.Top, this.Right, this.Bottom);
        }
    }
}
