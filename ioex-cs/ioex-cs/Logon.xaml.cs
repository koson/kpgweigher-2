﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Resources;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Markup;
using System.Windows.Shapes;
using System.Xml.Linq;
using System.Xml;
using System.Security.Cryptography;
using System.IO;
using System.Threading;
namespace ioex_cs
{
    /// <summary>
    /// Interaction logic for Logon.xaml
    /// </summary>
    public partial class LogonWindow : Window
    {
        private Thread bootup;
        System.Windows.Forms.Timer uiTimer;
        private bool boot_ok;
        public LogonWindow()
        {

            InitializeComponent();
            bootup = new Thread(PackerBoot);
            bootup.IsBackground = true;
            uiTimer = new System.Windows.Forms.Timer();
            uiTimer.Tick += new EventHandler(uiTimer_Tick);
            uiTimer.Interval = 200;
            
        
        }
        void uiTimer_Tick(object sender, EventArgs e)
        {
            if (bootup.IsAlive)
                return;
            uiTimer.Stop();
            if (boot_ok == false)
            {
                App p = Application.Current as App;
                MessageBox.Show(StringResource.str("bootfail"));
                bootmsg.Clear();
                bootup = new Thread(PackerBoot);
                
                bootup.IsBackground = false;
                
                bootup.Start();
                uiTimer.Start();
            }
            else
            {
                App p = (Application.Current as App);
                
                p.SwitchTo("runmode");
                this.Visibility = Visibility.Hidden;

            }
        }
        public void PackerBoot()
        {
            App p = (Application.Current as App);
            try
            {
                //search for nodes
                UpdateMessage(StringResource.str("search_newnode"));
                p.agent.missingnode.status = NodeStatus.ST_IDLE;
                p.agent.missingnode["addr"] = null;

                boot_ok = true;
                //check the availability of each board
                foreach(UIPacker pk in p.packers)
                {
                    foreach (byte n in pk.weight_nodes)
                    {
                        pk.agent.SetStatus(n, NodeStatus.ST_IDLE);
                        if (!pk.agent.search(n))
                        {
                            boot_ok = false;
                            UpdateMessage( "\r\n" +  StringResource.str("search_node") + n + StringResource.str("fail") );
                        }
                        else
                        {
                            UpdateMessage(".");
                        }
                    }
                    byte nvib = pk.vib_addr;
                    pk.agent.SetStatus(nvib, NodeStatus.ST_IDLE);
                    if (!pk.agent.search(nvib))
                    {
                        boot_ok = false;
                        UpdateMessage("\r\n" + StringResource.str("search_node") + nvib + StringResource.str("fail"));
                    }
                    if (boot_ok)
                    {
                        //start engine
                        pk.agent.Start();
                        pk.nc.Start();
                        //load config for node
                        UpdateMessage("\r\n" + StringResource.str("init_nodereg"));
                        pk.LoadConfig(pk.all_conf.cfg_name);
                        pk.agent.SetVibIntf(pk.vib_addr, pk.getInterface());
                        NodeAgent.bBootDone = true;
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateMessage(ex.Message);
                boot_ok = false;
                Thread.Sleep(3000);
            }
            
        }
        public void UpdateMessage(string msg)
        {
            Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, (Action)delegate
            {
                bootmsg.AppendText(msg);
                bootmsg.ScrollToEnd();
            });
            
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            boot_ok = false;
            bootup.Start();
            uiTimer.Start();
        }
    }
    public class StringResource
    {
        private static Dictionary<string, string> str_tbl;
        static public string language;
        static public void SetLanguage(string lang)
        {
            language = lang;
            App p = (Application.Current as App);
            Uri uri = new Uri("/ioex-cs;component/Resources/lang/" + lang + ".xaml", UriKind.Relative);
            p.Resources.Source = uri;

            ResourceDictionary rd = new ResourceDictionary();
            rd.Source = uri;

            str_tbl.Clear();
            foreach (object e in rd.Keys)
            {

                str_tbl[e.ToString()] = rd[e].ToString();
            }
        }
        static StringResource()
        {
            str_tbl = new Dictionary<string, string>();
            FileStream fsLog = new FileStream(System.Threading.Thread.GetDomain().BaseDirectory + "\\history.log", FileMode.Truncate, FileAccess.Write, FileShare.Read);
            fsLog.Close();

            SetLanguage("zh-CN");
        }
        static public void dolog(string log)
        {
            try
            {
                FileStream fsLog = new FileStream(System.Threading.Thread.GetDomain().BaseDirectory + "\\history.log", FileMode.Append, FileAccess.Write, FileShare.Read);
                StreamWriter sw = new StreamWriter(fsLog);
                sw.WriteLine(DateTime.Now.ToString("G") + "\t" + log);
                sw.Close();
                fsLog.Close();
            }
            catch (System.Exception)
            {

            }

        }

        public static string str(string key)
        {
            if (str_tbl.ContainsKey(key))
            {
                return str_tbl[key];
            }else
            {
                return "Invalid String Key";
            }
            
        }
        private StringResource(){}

    }

    public class Password
    {
        private static XmlConfig pwds;
        static    private string MD5Value(String str, Boolean isStr)
        {
         MD5 md5 = new MD5CryptoServiceProvider();
        byte[] md5ch;
        if (isStr)
         {
            byte[] ch = System.Text.Encoding.Default.GetBytes(str);
             md5ch = md5.ComputeHash(ch);
         } 
        else
         {
            if (!File.Exists(str))
                return string.Empty;
             FileStream fs = new FileStream(str, FileMode.Open, FileAccess.Read);
             md5ch = md5.ComputeHash(fs);
             fs.Close();
         }
         md5.Clear();
        string strMd5 = "";
        for (int i = 0; i < md5ch.Length - 1; i++ )
         {
             strMd5 += md5ch[i].ToString("x").PadLeft(2, '0');
         }
        return strMd5;
        
        }
        static Password()
        {
            //load password.xml and fill in the username list
            pwds = new XmlConfig("password.xml");
            pwds.LoadConfigFromFile();
            string a = MD5Value("0lock1r%4#", true);
        }
        public static IEnumerable<string> users {
            get{
                return pwds.Keys;
            }
        }
        public static bool compare_pwd(string user, string data)
        {
            return (get_pwd(user) == MD5Value(data + user + "1r%4#", true));

        }
        private static string get_pwd(string user){
                XElement x = pwds.LoadConfig(user);
                if (null == x)
                    return pwds.GetHashCode().ToString(); //just a difficult password to guess
                else
                    return x.Value;
        }
        public static void set_pwd(string user,string value){
                XElement x = new XElement("Item");
                x.SetAttributeValue("Name", user);
                x.SetValue(MD5Value(value + user +　"1r%4#", true));
                pwds.AddConfig(user,x);
                pwds.SaveConfigToFile();
            
        }
    }

}
