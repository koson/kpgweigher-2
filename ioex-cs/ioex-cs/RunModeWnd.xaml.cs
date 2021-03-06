﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
namespace ioex_cs
{
    /// <summary>
    /// Interaction logic for RunMode.xaml
    /// </summary>

    public partial class RunMode : Window
    {
        /// <summary>
        /// 显示和隐藏鼠标指针.
        /// </summary>
        [DllImport("user32.dll", EntryPoint = "ShowCursor", CharSet = CharSet.Auto)]
        public static extern int ShowCursor(int bShow);
        private System.Windows.Forms.Timer tm_cursor; //timer for cursor hiding
        private bool bShowCursor = true;

        private object ts_pwd = null;   //timespan for reset password check

        private Mutex hitmut = new Mutex(); //mutex for weight display refresh

        private UIPacker curr_packer    //get current packer instance
        {
            get
            {
                App p = Application.Current as App;
                return p.packers[0];
            }
        }

        private Queue<string> lastcalls;//buffer of calls
        
        private string lastcall{
            get //peek the lastcall
            {
                if (lastcalls.Count == 0)
                    return "";
                else
                    return lastcalls.Peek();
            }
            set
            {
                if (value == "")    //throw away the top call
                {
                    if(lastcalls.Count > 0)
                        lastcalls.Dequeue();
                }
                else
                {
                    if(!lastcalls.Contains(value)) //no duplicate calls in the list
                        lastcalls.Enqueue(value);
                }
            }
        }
        private byte curr_node = 0xff;
        System.Windows.Forms.Timer uiTimer;
        private App currentApp()
        {
            return Application.Current as App;
        }
        #region initialize
        private bool _contentLoaded2 = false;

        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void MyInitializeComponent(int nodenumber)
        {
            if (_contentLoaded2)
            {
                return;
            }
            _contentLoaded2 = true;

            if (nodenumber <= 10)
            {
                System.Windows.Application.LoadComponent(this, new System.Uri("/ioex-cs;component/runmodewnd.xaml", System.UriKind.Relative));
            }
            else
            {
                System.Windows.Application.LoadComponent(this, new System.Uri("/ioex-cs;component/Resources/runmodewnd14.xaml", System.UriKind.Relative));
            }
        }

        public RunMode(int nodenumber)
        {
            MyInitializeComponent(nodenumber);
            lastcalls = new Queue<string>();
            this.Loaded +=new RoutedEventHandler(RunMode_Loaded);
            uiTimer = new System.Windows.Forms.Timer();
            uiTimer.Tick += new EventHandler(uiTimer_Tick);
            uiTimer.Interval = 200; //200ms for UI update
            uiTimer.Start();

            tm_cursor = new System.Windows.Forms.Timer();
            tm_cursor.Tick += new EventHandler(tm_cursor_Tick);
            tm_cursor.Interval = 5000;
            this.MouseLeftButtonDown += new MouseButtonEventHandler(RunMode_MouseMove);
            this.title_speed.MouseLeftButtonDown += new System.Windows.Input.MouseButtonEventHandler(this.title_speed_MouseDown);
        }
        void RunMode_Loaded(object sender, RoutedEventArgs e)
        {
        }
        #endregion
        #region mouse hide after 5 seconds in running mode
        void RunMode_MouseMove(object sender, MouseEventArgs e)
        {
            if (!bShowCursor)
            {
                ShowCursor(1);
                bShowCursor = true;
                if (curr_packer.status != PackerStatus.RUNNING)
                    tm_cursor.Stop();   //no more hide required for not running status
                else
                    tm_cursor.Start();  //wait 5 seconds for hide
            }
        }

        void tm_cursor_Tick(object sender, EventArgs e)
        {
            if (curr_packer.status == PackerStatus.RUNNING) //in running status, hide the cursor after 5 seconds
            {
                ShowCursor(0);
                bShowCursor = false;
            }
            tm_cursor.Stop();
        }
        #endregion
        private void title_speed_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ts_pwd == null)
                ts_pwd = new TimeSpan(DateTime.Now.Ticks);
        }

        //hide start button for nonvalid license
        public void Disable(Visibility state)
        {
            this.btn_allstart.Visibility = state;
            this.lbl_bgstart.Visibility = state;
        }
        #region refresh UI
        public void RefreshVibUI()
        {
            UIPacker p = curr_packer;
            if (p.agent.vibstate == VibStatus.VIB_READY || p.agent.vibstate == VibStatus.VIB_INIDLE)
            {
                main_bucket.Template = this.FindResource("MainBucket") as ControlTemplate;
                main_bucket.ApplyTemplate();
            }
            if (p.agent.vibstate == VibStatus.VIB_WORKING)
            {
                main_bucket.Template = this.FindResource("MainBucketAct") as ControlTemplate;
                main_bucket.ApplyTemplate();

                lbl_status.Content = StringResource.str("waitpack");
            }
        }
        public void RefreshNodeUI()
        {
            UIPacker p = curr_packer;
            lbl_status.Content = "";
            foreach (byte n in p.weight_nodes)
            {
                UpdateNodeUI(n);
            }
            if (lbl_status.Content.ToString() != "")
            {
                lbl_status.Foreground = Brushes.Red;
                return;
            }
            lbl_status.Foreground = Brushes.Green;
            RefreshVibUI();
            if (lbl_status.Content.ToString() == "")
            {
                lbl_status.Content = StringResource.str("normal");
            }
        }
        public void UpdateSysConfigUI()
        {
            App p = currentApp();
            UIPacker pack = curr_packer;
            this.input_uvar.Content = pack.curr_cfg.upper_var.ToString() + StringResource.str("gram");

            this.input_dvar.Content = pack.curr_cfg.lower_var.ToString() + StringResource.str("gram");

            this.lbl_weight.Content = pack.curr_cfg.target.ToString() + StringResource.str("gram");

            this.prd_no.Content = pack.curr_cfg.product_no.ToString();

            this.operator_no.Content = p.oper;

            this.prd_desc.Content = pack.curr_cfg.product_desc.ToString();

            ImageBrush ib = (this.FindName("ellipseWithImageBrush") as Rectangle).Fill as ImageBrush;
            //load the corresponding picture.
            string path_to_jpg = ProdNum.baseDir + "\\prodpic\\" + StringResource.language + "\\" + pack.curr_cfg.product_desc.ToString() + ".jpg";

            if (File.Exists(path_to_jpg))
                ib.ImageSource = new BitmapImage(new Uri(path_to_jpg));
            else
                ib.ImageSource = new BitmapImage(new Uri(ProdNum.baseDir + "\\prodpic\\default.jpg"));
        }
        public void UpdateNodeUI(byte n)
        {
            App p = currentApp();
            UIPacker pack = curr_packer;

            //display the variable based on current setting
                Label lb = this.FindName("wei_node"+n) as Label;
                Button btn = this.FindName("bucket" + n) as Button;
                Button pbtn = this.FindName("pass" + n) as Button;

                string err = pack.agent.GetErrors(n);
                double wt = -1000;
                string ct="";

                if (err == "")
                {
                    wt = pack.agent.weight(n);
                    ct = wt.ToString("F1");
                    pbtn.Template = this.FindResource("PassBar") as ControlTemplate;
                    pbtn.ToolTip = "";
                    pbtn.ApplyTemplate();
                }
                else
                {   if (AlertWnd.b_show_alert && AlertWnd.b_turnon_alert)
                    {
                        pbtn.Template = this.FindResource("PassBarError") as ControlTemplate;
                        pbtn.ToolTip = StringResource.str(err.Substring(0, err.IndexOf(';')));
                        lbl_status.Content = StringResource.str(err.Substring(0, err.IndexOf(';'))) + "\n";
                        lb.Content = StringResource.str(err.Substring(0, err.IndexOf(';')));//"ERR";
                        pbtn.ApplyTemplate();
                    }
                }

                if (pack.agent.GetStatus(n) == NodeStatus.ST_LOST || pack.agent.GetStatus(n) == NodeStatus.ST_DISABLED)
                {
                    btn.Template = this.FindResource("WeightBarError") as ControlTemplate;
                    btn.ApplyTemplate();
                }
                if (pack.agent.GetStatus(n) == NodeStatus.ST_IDLE)
                {
                    btn.Template = this.FindResource("WeightBar") as ControlTemplate;
                    btn.ApplyTemplate();
                }
                if (wt > -1000 && wt <= WeighNode.MAX_VALID_WEIGHT)
                    lb.Content = ct;
        }
        public void RefreshRunNodeUI() //node ui update at run time
        {
            lbl_status.Content = "";
            foreach (UIPacker pk in currentApp().packers)
            {
                foreach (byte naddr in pk.weight_nodes)
                {
                    string param = "wei_node" + naddr.ToString();
                    Label lb = this.FindName(param) as Label;
                    Button btn = this.FindName(param.Replace("wei_node", "bucket")) as Button;
                    byte n = (byte)(RunMode.StringToId(param));
                    NodeAgent agent = pk.agent;

                    double wt = agent.weight(n);
                    if (wt > -1000 && wt <= WeighNode.MAX_VALID_WEIGHT)
                        lb.Content = agent.weight(n).ToString("F1");

                    if (agent.GetStatus(n) == NodeStatus.ST_LOST || agent.GetStatus(n) == NodeStatus.ST_DISABLED)
                    {
                        btn.Template = this.FindResource("WeightBarError") as ControlTemplate;
                        btn.ApplyTemplate();
                    }
                    string err = agent.GetErrors(n);
                    if (err != "" && AlertWnd.b_turnon_alert && AlertWnd.b_show_alert)
                        lbl_status.Content = n.ToString() + ":" + StringResource.str(err.Substring(0, err.IndexOf(';'))) + "\n";
                }
                if (pk.status == PackerStatus.RUNNING)
                {
                    lbl_speed.Content = pk.speed.ToString();
                    lbl_lastweight.Content = pk.last_pack_weight.ToString("F1");
                    lbl_totalpack.Content = pk.total_packs.ToString();

                    RefreshVibUI();
                }
            }
            if (lbl_status.Content.ToString() == "")
            {
                lbl_status.Content = StringResource.str("normal");
                lbl_status.Foreground = Brushes.Green;
            }
            else
            {
                lbl_status.Foreground = Brushes.Red;
                if (AlertWnd.b_turnon_alert && AlertWnd.b_stop_onalert && (curr_packer.status == PackerStatus.RUNNING))
                    btn_start_click(null, null);
            }

        }
        //update UI when a packer is hitted
        public void CombineNodeUI(CombineEventArgs ce)
        {
            foreach (byte naddr in currentApp().packers[ce.packer_id].weight_nodes)
            {
                string param = "wei_node" + naddr.ToString();
                Label lb = this.FindName(param) as Label;
                Button btn = this.FindName(param.Replace("wei_node", "bucket")) as Button;
                
                NodeAgent agent = currentApp().packers[ce.packer_id].agent;

                //update weight first
                double wt = -100000;
                for (int i = 0; i < ce.release_addrs.Length; i++)
                {
                    if (ce.release_addrs[i] == naddr)
                    {
                        wt = ce.release_wts[i];
                        break;
                    }
                }

                if (wt > -1000 && wt <= WeighNode.MAX_VALID_WEIGHT)
                    lb.Content = wt.ToString("F1");

                //update status display
                if (agent.GetStatus(naddr) == NodeStatus.ST_LOST || agent.GetStatus(naddr) == NodeStatus.ST_DISABLED)
                {
                    btn.Template = this.FindResource("WeightBarError") as ControlTemplate;
                }
                else if (naddr != curr_packer.vib_addr)
                {
                    if (ce.release_addrs.Contains(naddr))
                        btn.Template = this.FindResource("WeightBarRelease") as ControlTemplate;
                    else
                    {
                        btn.Template = this.FindResource("WeightBar") as ControlTemplate;
                    }
                }
                btn.ApplyTemplate();
            }
            //Update speed information
            UIPacker p = currentApp().packers[ce.packer_id];
            if (p.status == PackerStatus.RUNNING)
            {
                lbl_speed.Content = p.speed.ToString();
                lbl_lastweight.Content = p.last_pack_weight.ToString("F1");
                lbl_totalpack.Content = p.total_packs.ToString();
                RefreshVibUI();
            }
        }
#endregion
        private static bool tmlock = false;
        void uiTimer_Tick(object sender, EventArgs e)
        {
            
            lbl_datetime.Content = DateTime.Now.ToLongDateString() + "  " + DateTime.Now.ToLongTimeString();

            
            if (!this.IsVisible)
                return;
            if (tmlock)
                return;
            tmlock = true;

            UIPacker p = curr_packer;
            if (NodeCombination.q_hits.Count > 0)
            {
                CombineNodeUI(NodeCombination.q_hits.Dequeue());
                tmlock = false;
                return;
            }
            if(p.agent.bNeedRefresh && this.IsVisible)
            {
                p.agent.bNeedRefresh = false;
                lastcall = "RefreshUI";
            }
            if (lastcall != "")
            {
                if (lastcall == "StartStop")
                {
                    ToggleStartStop();
                    lastcall = "";
                }

                if (lastcall == "UpdatePrdNo")
                {
                    p.LoadPackConfig(p.curr_cfg.product_no,false);
                    UpdateSysConfigUI();
                    Thread.Sleep(2000);
                    lastcall = "";
                }
                
                if ((lastcall == "RefreshUI"))
                {

                    if (p.status != PackerStatus.RUNNING)
                    {
                        RefreshNodeUI();
                        currentApp().agent.ClearWeights();
                        currentApp().agent.bWeightReady = false;
                    }
                    else
                    {
                        RefreshRunNodeUI();
                    }
                    lastcall = "";
                }
                txt_oper.Visibility = Visibility.Hidden;
                bg_oper.Visibility = Visibility.Hidden;
                tmlock = false;
                return;
            }
            tmlock = false;
        }
        
        //send group command
        private void group_action(string action)
        {
            App p = currentApp();
            if (curr_packer.status == PackerStatus.PAUSED || curr_packer.status == PackerStatus.RUNNING)
            {
                return;
            }
            curr_packer.GroupAction(action);
            //todo update result message
        }
        private void btn_empty_click(object sender, RoutedEventArgs e)
        {
            if (curr_packer.status != PackerStatus.RUNNING)
                group_action("empty");
        }

        private void btn_zero_click(object sender, RoutedEventArgs e)
        {
            if (curr_packer.status != PackerStatus.RUNNING)
                group_action("zero");
        }
        private void ToggleStartStop()
        {
            
            
            App p = currentApp();
            if (curr_packer.status == PackerStatus.RUNNING)
            {
                if (hitmut.WaitOne(1000))
                {
                    curr_packer.StopRun();
                    this.btn_allstart.Content = StringResource.str("all_start");
                    btn_allstart.Style = this.FindResource("StartBtn") as Style;
                    //show cursor;
                    tm_cursor.Stop();
                    ShowCursor(1);
                    bShowCursor = true;
                    hitmut.ReleaseMutex();
                    btn_allstart.ApplyTemplate();
                }
            }
            else
            {
                if (hitmut.WaitOne(1000))
                {
                    curr_packer.bSimulate = false;
                    curr_packer.StartRun();
                    this.btn_allstart.Content = StringResource.str("all_stop");
                    btn_allstart.Style = this.FindResource("StartBtn2") as Style;
                    hitmut.ReleaseMutex();
                    btn_allstart.ApplyTemplate();
                }
            }
            
            
        }
        private void btn_start_click(object sender, RoutedEventArgs e)
        {
            lastcall = "StartStop";
            if (curr_packer.status == PackerStatus.RUNNING)
            {
                lbl_status.Content = StringResource.str("stopping");
            }
            else
            {
                lbl_status.Content = StringResource.str("starting");
                tm_cursor.Start();
            }
        }
        private void btn_history_click(object sender, RoutedEventArgs e)
        {
            App p = currentApp();
            if (curr_packer.status == PackerStatus.RUNNING)
                return;
            while (lastcall != "")
                this.uiTimer_Tick(null, null);
            p.SwitchTo("history");
        }

        //going to config menu
        private void btn_singlemode_click(object sender, RoutedEventArgs e)
        {
            App p = currentApp();
            if (curr_packer.status == PackerStatus.RUNNING)
                return;

            (Application.Current as App).kbdwnd.Init(StringResource.str("enter_singlemode_pwd"), "singlemode", true, KbdData);
            
        }
        private void grp_reg(string regname)
        {
            App p = currentApp();
            if (curr_packer.status == PackerStatus.RUNNING)
            {
                return;
            }
            p.kbdwnd.Init(StringResource.str("enter_"+regname), regname, false, KbdData);

        }
        static public int StringToId(string name)
        {
            StringBuilder sb = new StringBuilder();
            Regex re = new Regex("(\\d+)");
            Match m2 = re.Match(name);
            if (m2.Success)
                return int.Parse(m2.Groups[0].Value);
            else
                return -1;
        }
        
        private object NameToControl(string name)
        {
            return this.FindName(name);
        }
        private void grp_target_click(object sender, RoutedEventArgs e)
        {
            if (curr_packer.status != PackerStatus.RUNNING)
                grp_reg("run_target");
        }
        private void grp_uvar_click(object sender, RoutedEventArgs e)
        {
            if (curr_packer.status != PackerStatus.RUNNING)
                grp_reg("run_uvar");
        }
        private void grp_dvar_click(object sender, RoutedEventArgs e)
        {
            if (curr_packer.status != PackerStatus.RUNNING)
                grp_reg("run_dvar");
        }
        private void weibucket_Click(object sender, RoutedEventArgs e)
        {
        }
        private void vibbucket_Click(object sender, RoutedEventArgs e)
        {
        }

        public void KbdData(string param, string data)
        {
            try
            {
                App p = Application.Current as App;
                UIPacker pack = curr_packer;
                if (param == "run_uvar")
                {
                    pack.curr_cfg.upper_var = double.Parse(data);
                    pack.SaveCurrentConfig(4);
                }
                if (param == "run_dvar")
                {
                    pack.curr_cfg.lower_var = double.Parse(data);
                    pack.SaveCurrentConfig(4);
                }
                if (param == "run_target")
                {
                    
                    pack.curr_cfg.target = Double.Parse(data);
                    pack.UpdateEachNodeTarget();
                    pack.SaveCurrentConfig(1+4);
                }
                if (param == "run_operator")
                {
                    (Application.Current as App).oper = data;
                    pack.SaveCurrentConfig(4);
                }
                if (param == "singlemode")
                {
                    if (Password.compare_pwd("user",data))
                    {
                        while (lastcall != "")
                            this.uiTimer_Tick(null, null);
                        p.SwitchTo("configmenu");
                        return;
                    }
                    else
                        MessageBox.Show(StringResource.str("invalid_pwd"));
                }
                UpdateSysConfigUI();
            }
            catch (System.Exception e)
            {
                MessageBox.Show("Invalid Parameter");
                return;
            }
        }

        private void operator_no_Click(object sender, RoutedEventArgs e)
        {
            if (curr_packer.status != PackerStatus.RUNNING)
                (Application.Current as App).kbdwnd.Init(StringResource.str("enter_operator_no"), "run_operator", false, KbdData);
        }

        private void prd_no_Click(object sender, RoutedEventArgs e)
        {
            if (curr_packer.status == PackerStatus.RUNNING)
                return;

            App p = Application.Current as App;
            if (curr_packer.status != PackerStatus.RUNNING)
                p.prodnum.Init(prd_no_selected,false);
        }
        private void prd_no_selected(string item)
        {
            App p = Application.Current as App;
            curr_packer.curr_cfg.product_no = item;
            curr_packer.SaveCurrentConfig(4);
            lastcall = "UpdatePrdNo";
            txt_oper.Content = StringResource.str("downloading");
            txt_oper.Visibility = Visibility.Visible;
            bg_oper.Visibility = Visibility.Visible;
            
        }
        private int ButtonToId(object sender)
        {
            StringBuilder sb = new StringBuilder();
            Regex re = new Regex("(\\d+)");
            Match m2 = re.Match((sender as Control).Name);
            if (m2.Success)
                return int.Parse(m2.Groups[0].Value);
            else
                return -1;

        }

        private void prd_no_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

            App p = Application.Current as App;
            if (curr_packer.status != PackerStatus.RUNNING)
                p.prodnum.Init(prd_no_selected,false);
        }
        private void UpdateAlertWindow(bool visible)
        {
            rect_alert1.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
            rect_alert2.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
            rect_alert3.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
            lbl_alert.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
            lbl_alert1.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
            lbl_alert2.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
            lbl_alert3.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
            lbl_alert4.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
            if (visible)
                lbl_alert.Content = "("+curr_node.ToString()+")"+StringResource.str("alert_select");
        }
        private void lbl_alert_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Label l = sender as Label;
            if ((l is Label) && (curr_node != 0xff))
            {
                App p = Application.Current as App;
                byte id = curr_node;
                if (l.Name == "lbl_alert1") //alert solved
                {
                    p.agent.ClearErrors(id);
                }
                if (l.Name == "lbl_alert2") //alert force
                {
                    p.agent.Action(id,"empty");
                    p.agent.ClearErrors(id);
                }
                if (l.Name == "lbl_alert3") //alert disable
                {
                    if ((p.agent.GetStatus(id) == NodeStatus.ST_LOST) || (p.agent.GetStatus(id) == NodeStatus.ST_DISABLED))
                        p.agent.SetStatus(id, NodeStatus.ST_IDLE);
                    else
                        p.agent.SetStatus(id, NodeStatus.ST_DISABLED);
                }
                if (l.Name == "lbl_alert4") //alert quit
                {
                    
                }
                curr_node = 0xff;
                UpdateAlertWindow(false);
            }
        }
        private void looseFocus()
        {
        }
        private void passbar_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            if (curr_packer.status == PackerStatus.RUNNING)
            {
                looseFocus();
                return;
            }
            if (!AlertWnd.b_manual_reset || !AlertWnd.b_turnon_alert )
                return;
            Button l = sender as Button;
            if (l is Button)
            {
                App p = Application.Current as App;
                byte id = (byte)ButtonToId(sender);
                //if (p.agent.GetErrors(id) != "")
                {
                    curr_node = id;
                    if (AlertWnd.b_turnon_alert && AlertWnd.b_show_alert)
                    {
                        if ((p.agent.GetStatus(id) == NodeStatus.ST_LOST) || (p.agent.GetStatus(id) == NodeStatus.ST_DISABLED))
                            lbl_alert3.Content = StringResource.str("alert_enable");
                        else
                            lbl_alert3.Content = StringResource.str("alert_disable");
                        UpdateAlertWindow(true);
                    }
                }
            }               
        }

        private void main_bucket_Click(object sender, RoutedEventArgs e)
        {
            //curr_packer.agent.TriggerPacker(curr_packer.vib_addr);
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            //full time run , internal function only
            curr_packer.GroupAction("start");
            while (true)
            {
                Thread.Sleep(500);
                curr_packer.GroupAction("fill");
                Thread.Sleep(500);
                curr_packer.GroupAction("pass");
                Thread.Sleep(1000);
                curr_packer.GroupAction("release");
                Thread.Sleep(500);
                curr_packer.agent.Action(curr_packer.vib_addr,"fill");
            }
        }

        private int title_cnt = 0;
        private void title_speed_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ts_pwd == null)
                return;
            title_cnt++;
            if (title_cnt < 5)
                return;

            TimeSpan ts2 = new TimeSpan(DateTime.Now.Ticks);
            TimeSpan ts = ts2.Subtract((TimeSpan)ts_pwd).Duration();
            if (ts.Seconds < 6)
            {
                Reset();
                MessageBox.Show(StringResource.str("pwd_restore_done"));
            }
            title_cnt = 0;
            ts_pwd = null;
        }
        public static void Reset()
        {
            Password.set_pwd("admin", "020527");
                Password.set_pwd("user", "111111");
        }

        private void lbl_datetime_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Process app = new Process();
            app.StartInfo.FileName = "rundll32.exe";
            app.StartInfo.Arguments = "shell32.dll,Control_RunDLL timedate.cpl,,1";
            app.Start();
            Thread.Sleep(2000);
        }
    }
}
