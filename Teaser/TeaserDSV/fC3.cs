using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace TeaserDSV
{
    public partial class fC3 : Form
    {
        private bool IsStarted = false;
        private fInjectedImage frmDisplay;
        public fC3()
        {
            InitializeComponent();
            this.Text = Program.version;
        }

        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fSettings frm = new fSettings();
            frm.Show();

        }

        private void UpdateButton()
        {
            if (IsStarted)
            {
                button1.Text = "Start";
                IsStarted = false;
                frmDisplay.CloseThisForm();
                button1.Invalidate();
                button1.Refresh();
            }
            else
            {
                button1.Text = "Stop";
                IsStarted = true;
                button1.Invalidate();
                button1.Refresh();
                frmDisplay = new fInjectedImage();
                frmDisplay.evUpdateClosed += OnChildFormClose;
                frmDisplay.Show();
                UpdateFPS();
            }
        }

        private void UpdateFPS()
        {
            PropertyHolder propHolder = new PropertyHolder();
            propertyGrid1.SelectedObject = propHolder;

            Thread thr = new Thread(
                () =>
                {
                    int iNumOfSamples = 20;
                    Utils.FixedList renderLoopList = new Utils.FixedList(iNumOfSamples);
                    Utils.FixedList commLoopList = new Utils.FixedList(iNumOfSamples);
                    Utils.FixedList smokeLoopList = new Utils.FixedList(iNumOfSamples);
                    Utils.FixedList projLoopList = new Utils.FixedList(iNumOfSamples);


                    while (IsStarted)
                    {
                        renderLoopList.Add(frmDisplay.FrameRenderTime);
                        commLoopList.Add(frmDisplay.CommReceiveTime);
                        smokeLoopList.Add(frmDisplay.SmokeCalcTime);
                        projLoopList.Add(frmDisplay.ProjCalcTime);

                        double avgRender = renderLoopList.GetAverage();
                        double avgComm = commLoopList.GetAverage();
                        double avgSmoke = smokeLoopList.GetAverage();
                        double avgProj = projLoopList.GetAverage();

                        propHolder._commTime = avgComm;
                        propHolder._projTime = avgProj;
                        propHolder._renderTime = avgRender;
                        propHolder._smokeTime = avgSmoke;

                        propertyGrid1.Invoke(new MethodInvoker(
                            () => { propertyGrid1.Refresh(); }));

                        //label1.Invoke(new MethodInvoker(
                        //    () =>
                        //    {
                        //        label1.Text = avgRender.ToString("F2")+" ms per frame";
                        //        label2.Text =avgComm.ToString("F2")+" ms per comm msg";
                        //    }));

                        Thread.Sleep((int)SettingsHolder.Instance.RedrawFreq / 2);
                    }


                });
            thr.Start();
        }
        private void OnChildFormClose(object sender, EventArgs e)
        {
            button1.Text = "Start";
            IsStarted = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            UpdateButton();
        }
    }

    public class PropertyHolder
    {
        public double _renderTime;
        public double _commTime;
        public double _projTime;
        public double _smokeTime;


        [Category("Timing Properties")]
        [DisplayName("Render loop elapsed time [ms]")]
        [ReadOnly(true)]
        [Description("how many milisecconds it took to draw the frame (average of 10)")]
        public string renderTime
        {
            get { return _renderTime.ToString("F2"); }
        }

        [Category("Timing Properties")]
        [DisplayName("Comm message loop elapsed time [ms]")]
        [ReadOnly(true)]
        [Description("How many milisecconds it took to between two consecuitive messages(average)")]
        public string commTime
        {
            get { return _commTime.ToString("F2"); }
        }

        [Category("Timing Properties")]
        [DisplayName("Projection calculation elapsed time [ms]")]
        [ReadOnly(true)]
        [Description("How many milisecconds it took to calculate projection to image (average)")]
        public string projTime
        {
            get { return _projTime.ToString("F2"); }
        }

        [Category("Timing Properties")]
        [DisplayName("Smoke calculation elapsed time [ms]")]
        [ReadOnly(true)]
        [Description("How many milisecconds it took to calculate smoke (average)")]
        public string smokeTime
        {
            get {return _smokeTime.ToString("F2"); }
        }
    }
}
