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
        List<object> lstMessages = new List<object>();
        //private AutoResetEvent areWaitForConnectionCheck;
        private readonly EventWaitHandle evWaitForConnectionCheck;

        private bool bIsConnected { get; set; }
        private bool bClosing { get; set; }
        private bool bStopRefresh { get; set; }
        public frmMain()
        {
            InitializeComponent();

            oControl = new cControl();
            oControl.evReceivedResponse += OnReceivedMessage;
            oControl.evSentMessage += OnReceivedMessage;
            evWaitForConnectionCheck = new EventWaitHandle(false, EventResetMode.AutoReset);

            CheckConnection();
            InitManualPropertyGrid();

        }

        private void InitManualPropertyGrid()
        {
            propertyGrid1.SelectedObject = SettingsForManualSend.Instance;
            propertyGrid1.PropertySort = PropertySort.Categorized;
            
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
                string sMsgToDisplay = DateTime.UtcNow.ToString("HH:mm:ss.fff") + ": " + msg;
                listBox1.InvokeIfRequired(
                    () =>
                    {
                        listBox1.Items.Insert(0, sMsgToDisplay);
                    });
                //lstMessages.Insert(0, sMsgToDisplay);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

            if (!IsSixFileParsed())
            {
                MessageBox.Show("Please set file path from Settings menu", "Warning", MessageBoxButtons.OK);
            }
            else
            {
                //lstMessages.Clear();
                listBox1.Items.Clear();
                oControl.SetActiveSource(tabControl1.SelectedIndex == 0);
                oControl.StartMessageSendingLoop(); //and wait
                oControl.StartSendingMessages();
                //StartRefreshingListBox();
            }
        }

        private void StartRefreshingListBox()
        {
            AutoResetEvent areScheduler = new AutoResetEvent(false);
            Thread thrRefreshListbox = new Thread(
                () =>
                {
                    while (!this.Disposing || bClosing || bStopRefresh)
                    {
                        areScheduler.WaitOne(TimeSpan.FromMilliseconds(500));
                        listBox1.InvokeIfRequired(
                            () =>
                            {
                                if (lstMessages.Count > 0)
                                {
                                    listBox1.Items.AddRange(lstMessages.ToArray());
                                    listBox1.SelectedIndex = 0;
                                    lstMessages.Clear();
                                }


                            });

                    }
                });
            thrRefreshListbox.IsBackground = true;
            thrRefreshListbox.Name = "Listbox refresh";
            thrRefreshListbox.Start();
        }
        private void StopRefreshingListbox()
        {
            bStopRefresh = false;
        }
        private void CheckConnection()
        {
            Stopwatch sw = new Stopwatch();
            sw.Restart();
            Thread thrdCheckConnection = new Thread(() =>
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
                        //throw;
                    }

                }
            });
            thrdCheckConnection.IsBackground = true;
            thrdCheckConnection.Name = "Check connection";
            thrdCheckConnection.Start();

        }

        private void button2_Click(object sender, EventArgs e)
        {
            oControl.StopSendingMessages();
            //StopRefreshingListbox();
        }

        private void setIPToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            oControl.SetIPSettings();
        }

        private void exitToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            oControl.StopSendingMessages();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            oControl.SetActiveSource(tabControl1.SelectedIndex == 0);
            oControl.StartMessageSendingLoop(); //and wait
            oControl.StartSendingMessages();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            oControl.StopSendingMessages();
        }
    }
}
