using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Windows.Forms;
using Ookii.Dialogs;

namespace TeaserDSV
{
    public enum SmokeColor
    {
        White,
        Black
    }

    internal static class Program
    {
        public const string versionNumber = "1.1.2.1";
        public const string version = "Teaser DSV: " + versionNumber;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Application.Run(new fC3());
            }
            catch (Exception e)
            {
                LogWriter.Instance.WriteToLog(e.Message);
            }
        }
    }

    public class SettingsHolder : IDisposable
    {
        private static SettingsHolder instance;
        private static object syncroot = new Object();

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
                            instance.ipAddress = "127.0.0.1";
                            instance.port = "5555";
                            instance.chckfreq = 5000;
                            instance.timeout = 50000;
                            instance.LedSizeH = 1.1;
                            instance.LedSizeW = 1.1;
                            instance.ParticleSize = 4;
                            instance.RedrawFreq = 10;
                            instance.ParticleNumber = 20;
                            instance.ParticleLifeTime = 3.0;
                            instance.ParticleDecceleration = 0.90F;
                            instance.ParticlesSpeed = 3;
                            instance.enmSmokeColor = SmokeColor.White;
                            instance.WindowWidth = 960;
                            instance.WindowHeight= 540;
                        }
                    }
                }

                return instance;
            }
        }


        private SettingsHolder()
        {
        }


        [Category("1. Communication Properties")]
        [DisplayName("Client UDP/IP address")]
        [ReadOnly(false)]
        [Description("Listener to the UDP communication IP address")]
        public string ipAddress { get; set; }

        [Category("1. Communication Properties")]
        [DisplayName("Client UDP/IP port")]
        [ReadOnly(false)]
        [Description("Listener to the UDP communication IP port")]
        public string port { get; set; }

        [Category("1. Communication Properties")]
        [DisplayName("Comm timeout")]
        [ReadOnly(false)]
        [Description("timeout for udp sending in secconds")]
        public uint timeout { get; set; }

        [Category("1. Communication Properties")]
        [DisplayName("Comm check freq")]
        [ReadOnly(false)]
        [Description("Comm check frequency")]
        public uint chckfreq { get; set; }

        [Category("2. View Properties")]
        [DisplayName("Redraw frequency")]
        [ReadOnly(false)]
        [Description("Time between redraws of a target")]
        public double RedrawFreq { get; set; }

        [Category("2. View Properties")]
        [DisplayName("Particle number")]
        [ReadOnly(false)]
        [Description("Amount of particles per spawn")]
        public int ParticleNumber { get; set; }

        [Category("2. View Properties")]
        [DisplayName("Particles Speed")]
        [ReadOnly(false)]
        [Description("initial Particles Speed")]
        public float ParticlesSpeed { get; set; }

        [Category("2. View Properties")]
        [DisplayName("Particles Size")]
        [ReadOnly(false)]
        [Description("Particles Size")]
        public int ParticleSize { get; set; }

        [Category("2. View Properties")]
        [DisplayName("Led size width")]
        [ReadOnly(false)]
        [Description("LED size factor")]
        public double LedSizeW { get; set; }

        [Category("2. View Properties")]
        [DisplayName("Led size height")]
        [ReadOnly(false)]
        [Description("LED size factor")]
        public double LedSizeH { get; set; }

        [Category("2. View Properties")]
        [DisplayName("Particle life time")]
        [ReadOnly(false)]
        [Description("Particles max lifetime in secconds")]
        public double ParticleLifeTime { get; set; }

        [Category("2. View Properties")]
        [DisplayName("Particle decceleration")]
        [ReadOnly(false)]
        [Description("Particle decceleration between each frame")]
        public float ParticleDecceleration { get; set; }

        [Category("2. View Properties")]
        [DisplayName("Smoke color")]
        [ReadOnly(false)]
        [Description("Smoke color can be Black or White")]
        public SmokeColor enmSmokeColor { get; set; }

        [Category("2. View Properties")]
        [DisplayName("Display window width")]
        [ReadOnly(false)]
        [Description("Display window width [pixels]")]
        public int WindowWidth { get; set; }

        [Category("2. View Properties")]
        [DisplayName("Display window height")]
        [ReadOnly(false)]
        [Description("Display window height [pixels]")]
        public int WindowHeight { get; set; }


        public void Dispose()
        {
            lock (syncroot)
            {
                instance = null;
            }
        }

        internal class myFileBrowser : UITypeEditor
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
                    string[] s1Descript =
                        context.PropertyDescriptor.Description.Split(new char[] {' '},
                            StringSplitOptions.RemoveEmptyEntries);

                    ofd.Filter = @"|*.csv";

                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        return ofd.FileName;
                    }
                }

                return value;
            }
        }
    }
}