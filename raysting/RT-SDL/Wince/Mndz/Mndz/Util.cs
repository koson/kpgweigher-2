﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;
namespace Mndz
{
    class Util
    {
        public static IniHandler ConstIni; 
        static Util()
        {
            ConstIni = new IniHandler(Path.Combine(GlobalConfig.basedir,"define.ini"), "");
        }
        public static bool TryDoubleParse(string val, out double result)
        {
            try
            {
                result = Double.Parse(val);
                return true;
            }
            catch
            {
                result = 0;
                return false;
            }
        }
        public static bool TryDecimalParse(string val, out Decimal result)
        {
            try
            {
                result = Decimal.Parse(val);
                return true;
            }
            catch
            {
                result = new Decimal(0);
                return false;
            }
        }
        //scale 0s after 1st one, example if you want 10.000000, scale should be 7
        //return string until  1/10^scale with space
        public static string FormatDataWithSpace(double data, int scale) 
        {
            StringBuilder sb = new StringBuilder(FormatData(data, scale));
            int pos = sb.ToString().IndexOf('.')+4;
            while (pos < sb.Length)
            {
                sb.Insert(pos, " ");
                pos = pos + 4;
            }
            return sb.ToString();
            
        }
           //return data until  1/10^scale
            public static string FormatData(double data, int scale) 
            {
                double r = Math.Abs(data);
                if ((r < Math.Exp(-scale)) && (r > 1))
                {
                    return r.ToString("F11").Substring(0,scale+1);
                }
                if (r < 1e-11)
                {
                    return r.ToString("F11");
                }
                bool sign = (data < 0);
                double rng = 5e+12;
                int pos = 13;
                while (r < rng)
                {
                    rng = rng / 10;
                    pos = pos - 1;
                }
                pos = pos - scale;
                if (pos < 0)
                {
                    pos = -pos;
                    return data.ToString("F" + pos.ToString());
                }
                else
                {
                    return data.ToString("F1");
                }
            }
            static public string GMKFormat(ref double v)
            {
                string r;
                if (v >= 1e+9)
                {
                    v = v / 1e+9;
                    r = "G";
                }
                else if (v >= 1e+6)
                {
                    v = v / 1e+6;
                    r = "M";
                }
                else
                {
                    v = v / 1e+3;
                    r = "k";
                }
                return r;
            }
            public static void Test()
            {
                //string abc = FormatDat(10, 9);
            }
            public static double my_min(IEnumerable<double> values)
            {
                if (values.Count() < 1)
                    return 0;
                return values.Min();
            }
            public static double my_max(IEnumerable<double> values)
            {
                if (values.Count() < 1)
                    return 0;
                return values.Max();
            }
            public static double my_sum(IEnumerable<double> values)
            {
                if (values.Count() < 1)
                    return 0;
                return values.Sum();
            }
            public static double my_avg(IEnumerable<double> values)
            {
                if (values.Count() < 1)
                    return 0;

                return values.Average();
            }
            public static double my_sqrtsum(IEnumerable<double> values)
            {
                if (values.Count() < 2)
                    return 0;
                double avg = my_avg(values);
                if (Math.Abs(avg) < 1e-15)
                    return 1e-15;
                double sqr = values.Select<double, double>((o) => { return o * (o - avg); }).Sum();
                sqr = Math.Sqrt(sqr / (values.Count() - 1)) / avg;
                return sqr;
            }
            public static double[] to_ppm_array(IEnumerable<double> values)
            {
                if (values.Count() < 2)
                    return new double[] { };

                double avg = my_avg(values);
                if (Math.Abs(avg) < 1e-15)
                    avg = 1e-15;
                return values.Select<double, double>((o) => { return (o - avg) * 1e6 / avg; }).ToArray();
            }
    }
    public class IniHandler
    {
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filepath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retval, int size, string filepath);
        public string head
        {
            get
            {
                return _head;
            }
            set
            {
                if (value != "")
                    _head = "_" + value;
                else
                    _head = "";
            }
        }
        private string _head; //US or CN
        private string _fn;   //File
        private StringBuilder tmpbuf;
        private RegistryKey hklm;
        private RegistryKey ctrl;
        private RegistryKey aimdir;
        public IniHandler(string filename, string hd)
        {
            hklm = Registry.CurrentUser;
            ctrl = hklm.OpenSubKey("ControlPanel", true);
            aimdir = ctrl.OpenSubKey("BackLight", true);

            head = hd;
            _fn = filename;
            tmpbuf = new StringBuilder("", 255);

        }
        public void ClearAll()
        {
            if (File.Exists(_fn))
                File.Delete(_fn);
            FileStream fs = File.Create(_fn);
            if (fs is FileStream)
            {
                fs.Close();
                fs.Dispose();
            }
            fs = null;
        }
        public int IntValue(string section, string key)
        {
            try
            {
                   return Int32.Parse(StringValue(section,key));
            }
            catch //for unavailable key please return 9999
            {
                return 999;
            }
        }
        public void CheckValue(string section, string key, string def_value)
        {
            try
            {
                //HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\Timeouts
                if (!aimdir.GetValueNames().Contains(key))
                {
                    aimdir.SetValue(key, def_value, RegistryValueKind.String);
                }
            }
            catch
            {
                
            }
        }
        public string StringValue(string section, string key)
        {
            try
            {
                //HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\Timeouts
                return aimdir.GetValue(key).ToString();
            }
            catch
            {
                return "";
            }
            /*
            try
            {
                StreamReader sr = File.OpenText(_fn);
                if (sr is StreamReader)
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (line.StartsWith(key + "="))
                        {
                            sr.Close();
                            return line.Remove(0, (key + "=").Length);
                        }
                    }
                }
                //GetPrivateProfileString(section + _head, key, "", tmpbuf, 255, _fn);
                //return tmpbuf.ToString();
                return "";
            }
            catch //for unavailable key please return 9999
            {
                return "";
            }
             */
        }
        public void WriteString(string section, string key, object value)
        {
            try
            {
                //HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\Timeouts
                aimdir.SetValue(key, value, RegistryValueKind.String);
                aimdir.Flush();
            }
            catch
            {
                
            }
            /*
            //WritePrivateProfileString(section + _head, key, value.ToString(), _fn);
            try
            {
                string newfile = _fn + ".bak";
                if (File.Exists(newfile))
                    File.Delete(newfile);
                StreamWriter sw = new StreamWriter(_fn + ".bak");
                if (sw is StreamWriter)
                {
                    StreamReader sr = File.OpenText(_fn);
                    if (sr is StreamReader)
                    {
                        while (!sr.EndOfStream)
                        {
                            string line = sr.ReadLine();
                            if (line.StartsWith(key + "="))
                            {
                                sw.WriteLine(key + "=" + value);
                            }
                            else
                            {
                                sw.WriteLine(line);
                            }
                        }
                        sr.Close();
                    }
                    sw.Close();
                }
                File.Delete(_fn);
                File.Move(newfile, _fn);
            }
            catch
            {
            }
             * */
        }
    }
    internal class Logger
    {
        static public void TouchFile(string file)
        {
            if (!File.Exists(file))
            {
                try
                {
                    FileStream fsLog = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read);
                    StreamWriter sw = new StreamWriter(fsLog);
                    sw.Close();
                    fsLog.Close();
                }
                catch
                {
                }
            }
        }
        static public string GetLogFileName()
        {
            DateTime dt = DateTime.Now;
            string folder = Path.Combine(GlobalConfig.basedir,dt.ToString("yyyy_MM"));
            string file = folder + "\\" + DateTime.Now.ToString("yyyy_MM_dd") + ".txt";
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            TouchFile(file);
            return file;
        }
        static public void SysLog(string line)
        {
            string fn = GlobalConfig.system_log;
            try
            {
                if (line == "")
                {
                    if (File.Exists(fn))
                        File.Delete(fn);
                    TouchFile(fn);
                    return;
                }
                FileStream fsLog = new FileStream(fn, FileMode.Append, FileAccess.Write, FileShare.Read);
                using (StreamWriter sw = new StreamWriter(fsLog))
                {
                    sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\t" + line);
                }
                fsLog.Close();
            }
            catch
            {
            }
        }
        static public void Log(string line)
        {
            string fn = GetLogFileName();
            try
            {
                FileStream fsLog = new FileStream(fn, FileMode.Append, FileAccess.Write, FileShare.Read);
                using (StreamWriter sw = new StreamWriter(fsLog))
                {
                    sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\t" + line);
                }
                fsLog.Close();
            }
            catch
            {
            }
        }

    }

    //控制蜂鸣器的步骤是1、打开流驱动接口2、操作硬件3、关闭流驱动接口
    unsafe public class Beep
    {
        const UInt32 OPEN_EXISTING = 3;
        const UInt32 GENERIC_READ = 0x80000000;
        const UInt32 GENERIC_WRITE = 0x40000000;
        const Int32 INVALID_HANDLE_VALUE = -1;
        private IntPtr hPort = (IntPtr)INVALID_HANDLE_VALUE;
        // PWM的控制字，http://www.doc88.com/p-200931819849.html
        //const UInt32 IOCTL_PWM_SET_PRESCALER = 1;
        const UInt32 IOCTL_PWM_SET_FREQ = 2;
        const UInt32 IOCTL_PWM_SET_DUTY = 3;
        const UInt32 IOCTL_PWM_STOP = 1;
        //const UInt32 IOCTL_PWM_GET_FREQUENCY = 4;
        //首先找到函数在dll库中的原型
        //然后将该函数原型中的参数类型和C#中的数据类型进行比较得出C#的该函数原型如下
        [DllImport("coredll.dll")]
        public static extern IntPtr CreateFile(
            String lpFileName,
            UInt32 dwDesiredAccess,
            UInt32 dwShareMode,
            IntPtr lpSecurityAttributes,
            UInt32 dwCreationDisposition,
            UInt32 dwFlagsAndAttributes,
            IntPtr hTemplateFile
            );
        [DllImport("coredll.dll")]
        public static extern bool DeviceIoControl(
            IntPtr hDevice,
            UInt32 dwIoControlCode,
            Byte[] lpInBuffer,
            UInt32 nInBufferSize,
            Byte[] lpOutBuffer,
            UInt32 nOutBufferSize,
            UInt32 lpBytesReturned,
            IntPtr lpOverlapped
            );
        [DllImport("coredll.dll")]
        public static extern bool CloseHandle(IntPtr hDevice);
        UInt32[] prescale = new UInt32[2] { 0, 15 };//15
        UInt32[] divider = new UInt32[2] { 0, 8 };//8
        UInt32[] buff = new UInt32[3] { 0, 488, 244 };//488,244来源
        /*int freq = 800;       // 工作频率初值
         #define S3C2440_PCLK 50000000    // PCLK是50MHz
         #define Prescaler0 15     // 预分频
         #define MUX0   8     // 定时器分频值
         #define TCNTB0   (S3C2440_PCLK/128/freq)   // 工作频率
         #define TCMPB0   (TCNTB0>>1)    // 占空比，默认是50%
         
         BYTE prescale[2] = {0, Prescaler0};
         BYTE divider[2] = {0, MUX0};
         DWORD buff[3] = {0, TCNTB0, TCMPB0};*/
        public Beep(string dev)
        {
            hPort = CreateFile(dev, GENERIC_READ | GENERIC_WRITE, 0, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
        }
        public void BeepLoad()
        {
            if (hPort == (IntPtr)INVALID_HANDLE_VALUE)
            {
                return;
            }
            FreqSet(hPort, 2000);
            Thread.Sleep(50);
            FreqSet(hPort, 0);

        }
        private void FreqSet(IntPtr hPort, UInt32 value)
        {
            byte[] sBuf = new byte[4];
            UInt32 sInput;

            if (hPort == (IntPtr)INVALID_HANDLE_VALUE)
            {
                return;
            }
            sInput = 50;
            BitConverter.GetBytes(sInput).CopyTo(sBuf, 0);
            DeviceIoControl(hPort, IOCTL_PWM_SET_DUTY, sBuf, 4, null, 0, 0, IntPtr.Zero);
            sInput = value;
            BitConverter.GetBytes(sInput).CopyTo(sBuf, 0);
            DeviceIoControl(hPort, IOCTL_PWM_SET_FREQ, sBuf, 4, null, 0, 0, IntPtr.Zero);
        }
    }
    internal static class GlobalConfig
    {

        public static bool ISDEBUG = false;
        public static string sSwiPort = "COM1";
        public static string sCmdPort = "COM2";
        public static string sADPort = "COM99";
        public static string sAD2Port = "COM4";  //FOR VX reading
        public static string sNavmeter = "PZ158";
        public static string sNavmeter2 = "PZ158";
        public static string udiskdir = @"\硬盘\";
        public static string udiskdir2 = @"\硬盘\";
        public static string basedir = @"\Nandflash\Tsioex\";
        public static string history_log;
        public static string system_log;
        
        static GlobalConfig()
        {
            history_log = Path.Combine(basedir,"history.log");
            system_log = Path.Combine(basedir , "run.txt");
        }
    }
    internal class StringResource
    {
        private static Dictionary<string, string> str_tbl;
        static public string language;

        static public void SetLanguage(string lang)
        {
            language = lang;
        }
        static StringResource()
        {
            str_tbl = new Dictionary<string, string>();
            if (!File.Exists(GlobalConfig.history_log))
                File.Create(GlobalConfig.history_log).Close();
            FileStream fsLog = new FileStream(GlobalConfig.history_log, FileMode.Truncate, FileAccess.Write, FileShare.Read);
            fsLog.Close();
            SetLanguage("zh-CN");

            str_tbl["backspace"] = "退格";
            str_tbl["clr"] = "清除";
            str_tbl["quit"] = "退出";
            str_tbl["ok"] = "确定";

            
            str_tbl["vxzero"] = "Vx清零";
            str_tbl["vgzero"] = "指零仪清零";
            str_tbl["selectrx"] = "请选择被测电阻步进值";
            str_tbl["inputrs"] = "请输入标准电阻值({0})";
            str_tbl["selectes"] = "请选择标准电压步进值";

            str_tbl["inputda"] = "请输入DA零位值";
            str_tbl["inputaddelay"] = "请输入测量延时值(秒)";
            str_tbl["selectvx"] = "请选择被测电压值Vx";
            str_tbl["inputrs"] = "输入Rs值(ohm)";
            str_tbl["inputrs1"] = "请输入Rs({0})实际值(ohm)";
            str_tbl["inputes"] = "请输入Es({0})实际值(V)";
            str_tbl["out_of_range"] = "输入值无效";
            str_tbl["hvout"] = "校验高压表";
            str_tbl["inputhv"] = "请输入高压表校验电压(V)";
            str_tbl["selecthvport"] = "请选择电压输出端子";
            str_tbl["over10volt"] = "超出最大电压输出范围";
            str_tbl["NAV_INIT_PZ158"] = "Un%01;00\r";
//            str_tbl["NAV_10MV_PZ158"] = "Un%01;12;02\r";
//            str_tbl["NAV_120MV_PZ158"] = "Un%01;12;00\r";
            str_tbl["NAV_1V_PZ158"] = "Un%01;12;01\r";
            str_tbl["NAV_30V_PZ158"] = "Un%01;12;02\r";
            str_tbl["NAV_AFLTOFF_PZ158"] = "Un%01;26\r";
            str_tbl["NAV_AFLTON_PZ158"] = "Un%01;27\r";
            str_tbl["NAV_ZEROON_PZ158"] = "Un%01;06\r";
            str_tbl["NAV_READ_PZ158"] = "Un%01;01\r";

            str_tbl["NAV_INIT_PZ2182"] = "Un%01;00\r";
            str_tbl["NAV_10MV_PZ2182"] = "Un%01;12;00\r";
            str_tbl["NAV_120MV_PZ2182"] = "Un%01;12;01\r";
            str_tbl["NAV_1V_PZ2182"] = "Un%01;12;02\r";
            str_tbl["NAV_30V_PZ2182"] = "Un%01;12;02\r";
            str_tbl["NAV_AFLTOFF_PZ2182"] = "Un%01;26\r";
            str_tbl["NAV_AFLTON_PZ2182"] = "Un%01;27\r";
            str_tbl["NAV_ZEROON_PZ2182"] = "Un%01;06\r";
            str_tbl["NAV_READ_PZ2182"] = "Un%01;01\r";

            str_tbl["NAV_INIT_2182"] = "Un*RST\n*CLS\n:init:cont on;:ABORT\n:sens:func 'volt:dc'\n:sense:chan 1\n:sens:volt:rang:auto on\n:sens:volt:chan1:lpas off\n:SENS:VOLT:DC:NPLC 1\nVOLT:DIG 8\n:syst:azer on\n";
            str_tbl["NAV_10MV_2182"] = "Un:sens:volt:chan1:rang 0.01\n";
            str_tbl["NAV_120MV_2182"] = "Un:sens:volt:chan1:rang 0.1\n";
            str_tbl["NAV_1V_2182"] = "Un:sens:volt:chan1:rang 1\n";
            str_tbl["NAV_30V_2182"] = "Un:sens:volt:chan1:rang 10\n";
            str_tbl["NAV_AFLTOFF_2182"] = "Un:sens:volt:chan1:dfil:stat off\n";
            str_tbl["NAV_AFLTON_2182"] = "Un:sens:volt:chan1:dfil:wind 5\n:sens:volt:chan1:dfil:coun 10\n:sens:volt:chan1:dfil:tcon mov\n:sens:volt:chan1:dfil:stat on\n";
            str_tbl["NAV_ZEROON_2182"] = "Un:sens:volt:ref:acq\n:sens:volt:ref:stat on\n";
            str_tbl["NAV_READ_2182"] = "Un:fetc?\n";
        }
        static public void dolog(string log)
        {
            try
            {
                FileStream fsLog = new FileStream("history.log", FileMode.Append, FileAccess.Write, FileShare.Read);
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
            }
            else
            {
                return "Invalid String Key";
            }

        }
        private StringResource() { }

    }

}
