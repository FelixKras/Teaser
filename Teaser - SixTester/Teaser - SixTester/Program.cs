using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ookii.Dialogs;

namespace TeaserSixTester
{
    static class Program
    {
      
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            (new cTimerHelper()).SetTimerResolution(500 * 10);
            Application.Run(new frmMain());
        }

     }

    [StructLayout(LayoutKind.Sequential)]
    public struct TimerCaps
    {
        public bool HasBeenRun;
        public float PeriodMin;
        public float PeriodMax;
        public float PeriodCurrent;
    }
    class cTimerHelper
    {

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern uint NtSetTimerResolution(uint DesiredResolution, bool SetResolution, ref uint CurrentResolution);

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern uint NtQueryTimerResolution(out uint MaximumResolution, out uint MinimumResolution, out uint CurrentResolution);
        public void Start()
        {
            TimerCaps myTimerCaps = new TimerCaps();
            QueryTimerResolution(ref myTimerCaps);
            SetTimerResolution(5000);
            QueryTimerResolution(ref myTimerCaps);
        }

        public void QueryTimerResolution(ref TimerCaps myTimerCaps)
        {
            uint PeriodMin, PeriodMax, PeriodCurrent;
            var result = NtQueryTimerResolution(out PeriodMax, out PeriodMin, out PeriodCurrent);
            myTimerCaps.PeriodMax = PeriodMax / 10000F;
            myTimerCaps.PeriodMin = PeriodMin / 10000F;
            myTimerCaps.PeriodCurrent = PeriodCurrent / 10000F;
            if (result == 0)
            {
                myTimerCaps.HasBeenRun = true;
            }
        }

        public ulong SetTimerResolution(uint timerResolutionIn100nsUnits, bool doSet = true)
        {
            uint currentRes = 0;
            var result = NtSetTimerResolution(timerResolutionIn100nsUnits, doSet, ref currentRes);
            return currentRes;
        }

    }
    public class SettingsHolder : IDisposable
    {

        private static SettingsHolder instance;
        private static object syncroot = new Object();
        

        public EventHandler evFileLoaded;
        private string _fileLocation;

        public static SettingsHolder Instance
        {
            get
            {
                // If the instance is null then create one
                if (instance == null)
                {
                    lock (syncroot)
                    {
                        if (instance == null)
                        {
                            instance = new SettingsHolder();
                            instance._fileLocation = "Click here...   ->";
                            instance.ipAddress = "127.0.0.1";
                            instance.port = "5555";
                            instance.chckfreq = 5000;
                            instance.timeout = 50000;
                            instance.SendFreq = 10;
                        }
                    }
                }
                return instance;
            }

        }


        private SettingsHolder()
        {

        }

        public void UpdateFileLocation(string FilePath)
        {
            _fileLocation = FilePath;
        }

        [Category("1. Communication Properties")]
        [DisplayName("Data file location")]
        [ReadOnly(false)]
        [Description("Data file location")]
        [EditorAttribute(typeof(myFileBrowser), typeof(System.Drawing.Design.UITypeEditor))]
        public string SixFileLocation
        {
            get { return _fileLocation; }
            set
            {
                _fileLocation = value;
                evFileLoaded.Raise(_fileLocation);
            }
        }


        [Category("1. Communication Properties")]
        [DisplayName("Client UDP/IP address")]
        [ReadOnly(false)]
        [Description("Listener to the UDP communication IP address")]
        public string ipAddress
        {
            get; set;
        }
        [Category("1. Communication Properties")]
        [DisplayName("Client UDP/IP port")]
        [ReadOnly(false)]
        [Description("Listener to the UDP communication IP port")]
        public string port
        {
            get; set;
        }

        [Category("1. Communication Properties")]
        [DisplayName("Comm timeout")]
        [ReadOnly(false)]
        [Description("timeout for udp sending in secconds")]
        public uint timeout
        {
            get; set;
        }

        [Category("1. Communication Properties")]
        [DisplayName("Comm check freq")]
        [ReadOnly(false)]
        [Description("Comm check frequency")]
        public uint chckfreq
        {
            get; set;
        }

        [Category("1. Communication Properties")]
        [DisplayName("Message frequency")]
        [ReadOnly(false)]
        [Description("Time between sends of six messages [ms]")]
        public int SendFreq { get; set; }

        public void Dispose()
        {
            lock (syncroot)
            {
                instance = null;
            }
        }
    }

    public class myFileBrowser : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            using (Ookii.Dialogs.VistaOpenFileDialog ofd = new VistaOpenFileDialog())
            {
                ofd.Multiselect = false;
                string[] s1Descript = context.PropertyDescriptor.Description.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                //ofd.Filter = @"|*.csv";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    return ofd.FileName;
                }
            }
            return value;

        }
    }

}
