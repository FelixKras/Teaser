using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TeaserSixTester
{
    public partial class frmMain : Form
    {
        private cControl oControl;

        //private AutoResetEvent areWaitForConnectionCheck;
        private readonly EventWaitHandle evWaitForConnectionCheck;

        private bool bIsConnected { get; set; }
        private bool bClosing { get; set; }

        public frmMain()
        {
            InitializeComponent();

            oControl = new cControl();
            oControl.evReceivedResponse += OnReceivedMessage;
            oControl.evSentMessage += OnReceivedMessage;
            evWaitForConnectionCheck = new EventWaitHandle(false, EventResetMode.AutoReset);

            oControl.StartMessageTask(); //and wait
            CheckConnection();


        }

        private bool IsSixFileParsed()
        {
            return oControl.ParseScriptFile(SettingsHolder.Instance.SixFileLocation);
        }

        private void OnReceivedMessage(object sender, EventArgs e)
        {
            string msg = sender as string;
            if (msg != null)
            {
                listBox1.InvokeIfRequired(() =>
                {
                    string sMsgToDisplay = DateTime.UtcNow.ToString("HH:mm:ss.fff") + ": " + msg;
                    listBox1.Items.Insert(0, sMsgToDisplay);
                });
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void setIPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            oControl.SetIPSettings();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            if (!IsSixFileParsed())
            {
                MessageBox.Show("Please set file path from Settings menu", "Warning", MessageBoxButtons.OK);
            }
            else
            {
                listBox1.Items.Clear();
                oControl.StartSendingMessages();
            }
        }

        private void CheckConnection()
        {
            Stopwatch sw = new Stopwatch();
            sw.Restart();
            Task tskCheckConnection = new Task(() =>
            {
                while (!this.Disposing || bClosing)
                {
                    try
                    {
                        bIsConnected = oControl.CheckConnection(shouldReconnect: true);
                        evWaitForConnectionCheck.WaitOne(TimeSpan.FromMilliseconds(SettingsHolder.Instance.chckfreq));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }

                }
            });
            tskCheckConnection.Start();

        }

        private void button2_Click(object sender, EventArgs e)
        {
            oControl.StopSendingMessages();
        }



    }
}
