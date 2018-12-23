using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

using TeaserDSV.Model;
using TeaserDSV.Utilities;
using ThreadState = System.Threading.ThreadState;
using Timer = System.Timers.Timer;

namespace TeaserDSV
{
    public partial class fInjectedImage : Form
    {
        #region Private Members
        private Listener oListener;
        private ConcurrentQueue<SixMsg> conqSixMsgs;

        private Thread thrTargetDraw;
        private Thread thrSixDataHandle;
        private readonly cSmoker oSmoker = new cSmoker();
        private int cParticleNumber;
        private PointF emitterOfSmokeLocation;
        private readonly object _lockObj = new object();
        private readonly AutoResetEvent areDraw = new AutoResetEvent(false);

        Body m_Body = new Body();

        private bool bIsRunning { get; set; }
        private bool bThreadStopped { get; set; }
        private Action actRefresh;

        private Brush particleBrush;

        private Graphics grphxDrawer;
        private Image bgImage;
        private int iFrameNum = 0;

        private bool mouseDown;
        private Point lastFormLocation;


        private bool IsLedOn;
        private int m_color;
        private int m_direction;
        private Timer recalcTimer;

        private const string sExtPath = @"..\..\External\";
        private Bitmap ledImage = new Bitmap(sExtPath + "OnSep32x32_.png");
        #endregion Private Members


        public fInjectedImage()
        {
            InitializeComponent();
            SmokeColorInit();

        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            InitializePicBox();
            InitializeTimer();
            InitTarget();
            StartListening();
            InitSixDataHandler();
        }
        private void RefreshPicBox()
        {
            picBox.Invalidate();
            RedrawScene();
            picBox.Update();
        }
        private void InitializePicBox()
        {
            picBox.Dock = DockStyle.Fill;
            bgImage = Image.FromFile(@"..\Images\bg2.jpg");
            picBox.Image = bgImage;
            Screen[] screens = Screen.AllScreens;
            if (screens.Length > 1)
            {
                this.Location = screens[1].WorkingArea.Location;
            }
            else
            {
                this.Location = screens[0].WorkingArea.Location;
            }
            actRefresh = new Action(RefreshPicBox);
            this.FormBorderStyle = FormBorderStyle.None;
            //this.WindowState = FormWindowState.Maximized;


        }

        private void SmokeColorInit()
        {
            Color tmp = Color.Black;
            tmp = Color.FromName(ConfigurationManager.AppSettings["SmokeColor"]);
            if (tmp.IsNamedColor)
            {
                if (tmp != Color.White && tmp != Color.Black)
                {
                    MessageBox.Show("Invalid smoke color");
                    return;
                }

                if (tmp == Color.White)
                {
                    m_color = 255;
                    m_direction = -1;
                }
                else
                {
                    m_color = 0;
                    m_direction = 1;
                }
            }
            else
            {
                MessageBox.Show("Invalid smoke color");
                return;
            }
        }

        private void InitSixDataHandler()
        {
            thrSixDataHandle = new Thread(HandleMessages) { IsBackground = true, Name = "Six handler" };
            thrSixDataHandle.Start();

        }

        private void InitializeTimer()
        {
            recalcTimer = new System.Timers.Timer(SettingsHolder.Instance.RedrawFreq);
            recalcTimer.Elapsed += RecalcTimer_Elapsed;
            recalcTimer.Enabled = true;
        }
        private void InitTarget()
        {
            cParticleNumber = SettingsHolder.Instance.ParticleNumber;
            conqSixMsgs = new ConcurrentQueue<SixMsg>();
            bIsRunning = true;
            bThreadStopped = false;
            thrTargetDraw = new Thread(SmokeParticlesManager) { IsBackground = true, Name = "Smoke particle thread" };
            thrTargetDraw.Start();

            m_Body.OriginalPoints = LoadTargetFromFile(sExtPath + "TeaserShape.txt");

        }

        private ShapePoint3D[] LoadTargetFromFile(string externalTeasershapeTxt)
        {
            List<ShapePoint3D> points = new List<ShapePoint3D>();
            string[] s1Lines = File.ReadAllLines(externalTeasershapeTxt);


            foreach (var line in s1Lines)
            {
                if (line.StartsWith("#"))
                {
                    continue;
                }
                else
                {
                    var vals = line.Split(new char[] { ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    double x = double.Parse(vals[0]);
                    double y = double.Parse(vals[1]);
                    double z = double.Parse(vals[2]);
                    bool isLED = (int.Parse(vals[3])) == 1;
                    points.Add(new ShapePoint3D(x, y, z, isLED));
                }
            }

            return points.ToArray();
        }

        private void RecalcTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            areDraw.Set();
        }

        public void SmokeParticlesManager()
        {

            try
            {
                while (bIsRunning)
                {
                    areDraw.WaitOne();

                    lock (_lockObj)
                    {
                        for (int ii = oSmoker.Particles.Count - 1; ii >= 0; ii--)
                        {
                            if (oSmoker.Particles[ii].PerformFrame())
                            {
                                oSmoker.ReturnToPool(oSmoker.Particles[ii]);
                            }
                            if (!bIsRunning)
                            {
                                break;
                            }

                        }

                    }
                    if (bIsRunning)
                    {
                        this.Invoke(actRefresh);
                    }

                }


            }
            catch (ThreadAbortException ex)
            {
                Debug.Print("Proc Thread aborting");

            }
            this.Invoke(new MethodInvoker(CloseThisForm));
        }
        private void CreateParticleEmitter(PointF emitPoint)
        {

            lock (_lockObj)
            {
                for (int ii = 0; ii < cParticleNumber; ii++)
                {
                    Particle createparticle = oSmoker.GetFromPool(emitPoint, SettingsHolder.Instance.ParticlesSpeed);
                    oSmoker.Particles.Add(createparticle);
                }
            }
        }

        private void RedrawScene()
        {
            Stopwatch sw = Stopwatch.StartNew();
            Image copyBGImage = bgImage.DeepClone();
            grphxDrawer = Graphics.FromImage(copyBGImage);


            RedrawParticles(grphxDrawer,bgImage.Size);


            RedrawTarget(grphxDrawer, IsLedOn);
            picBox.Image = copyBGImage.DeepClone();


            //m_BlinkingLed = DateTime.Now.Second % 2 == 0 ? Color.Red : Color.White;
            //copyBGImage.Save(string.Format("Image{0:D8}.jpg", iFrameNum++));

            sw.Stop();
            double result = ((double)sw.ElapsedTicks / Stopwatch.Frequency * 1000F);

        }

        private void RedrawTarget(Graphics grphxDrawerTarget, bool IsLedVisible = false)
        {
            if (m_Body.ImagePoints != null && m_Body.ImagePoints.Length > 0)
            {
                PointF[] XandZ = GetPointsArr(m_Body.ImagePoints,bgImage.Size);
                grphxDrawerTarget.FillPolygon(new SolidBrush(Color.Red), XandZ);

                for (int ii = 0; ii < m_Body.OriginalPoints.Length; ii++)
                {
                    if (m_Body.ImagePoints[ii].isLED && IsLedVisible)
                    {
                        DrawLedFromFile(grphxDrawerTarget, m_Body);
                    }
                }
            }
        }

        private void DrawLedFromFile(Graphics grphxDrawerTarget, Body body)
        {

            //ledImage.MakeTransparent(Color.Black);
            //float FOVAngleInRad = (float)(cCalcer.CameraSettings.FOVangAz * cCalcer.CameraSettings.Deg2Rad);
            //float PixelsOnFPA = cCalcer.CameraSettings.LedSize * cCalcer.CameraSettings.SensorWidth /
            //                    (float)(2 * body.CenterOfMassCartesian.Norm() * Math.Tan(FOVAngleInRad / 2));



            //float w = PixelsOnFPA;//ledImage.Width;//*fScale;
            //float h = PixelsOnFPA; //ledImage.Height;//*fScale;
            //Point leftUp = new Point((int)(positionOnScreen.X - w / 2), (int)(positionOnScreen.Y - h / 2));
            //
            //Rectangle destRect = new Rectangle(leftUp.X, leftUp.Y, (int)w, (int)h);
            //
            //grphxDrawerTarget.DrawImage(ledImage, destRect, new Rectangle(0, 0, (int)w, (int)h), GraphicsUnit.Pixel);
        }

        private void RedrawParticles(Graphics grphxDrawerParticles,Size canvasSize )
        {

            int alpha = 0;

            lock (_lockObj)
            {
                foreach (Particle p in oSmoker.Particles)
                {
                    p.GetColorToLife(ref alpha, ref m_color, m_direction);

                    using (particleBrush = new SolidBrush(Color.FromArgb(alpha, m_color, m_color, m_color)))
                    {
                        var pX = (p.Location.X - SettingsHolder.Instance.ParticleSize / 2)
                                 / Camera.CameraSettings.SensorWidth * canvasSize.Width;
                        var pY = (p.Location.Y - SettingsHolder.Instance.ParticleSize / 2)
                                 / Camera.CameraSettings.SensorHeight * canvasSize.Height;
                        grphxDrawerParticles.FillEllipse(particleBrush,pX,pY,
                            SettingsHolder.Instance.ParticleSize, SettingsHolder.Instance.ParticleSize);
                    }

                }
            }
        }


        private void StartListening()
        {
            oListener = new Listener(SettingsHolder.Instance.ipAddress, int.Parse(SettingsHolder.Instance.port));
            oListener.StartListening();
            oListener.evCommandReceived += OnMessageReceived;

        }

        private void OnMessageReceived(object sender, EventArgs eventArgs)
        {
            SixMsg oSixMsg = (SixMsg)sender;
            conqSixMsgs.Enqueue(oSixMsg);
        }

        private void HandleMessages()
        {
            SixMsg oSixMsg;
            while (bIsRunning)
            {
                try
                {
                    if (conqSixMsgs.Count > 0)
                    {
                        conqSixMsgs.TryDequeue(out oSixMsg);
                        
                        
                        m_Body.RollAngle = oSixMsg.Object_Roll;
                        m_Body.YawAngle = oSixMsg.Object_Yaw;
                        m_Body.PitchAngle = oSixMsg.Object_Pitch;
                        m_Body.ComputeProjection(new double[] { oSixMsg.Object_X, oSixMsg.Object_Y, oSixMsg.Object_Z });

                        IsLedOn = (oSixMsg.Object_model == 1);
                        
                        //BoundaryDetect();
                        if (oSixMsg.Smoke == 1)
                        {
                            emitterOfSmokeLocation = m_Body.ImagePoints.First(d => d.isLED).point;
                            
                            emitterOfSmokeLocation.X = emitterOfSmokeLocation.X;
                            emitterOfSmokeLocation.Y = emitterOfSmokeLocation.Y ;

                            CreateParticleEmitter(emitterOfSmokeLocation);
                        }


                    }
                    else
                    {
                        continue;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }

            }
        }

        private PointF[] ShiftToCM(PointF[] pntTargetShapeTransformed, PointF cmEmitterOfSmokeLocation)
        {
            PointF[] result = new PointF[pntTargetShapeTransformed.Length];
            for (int ii = 0; ii < pntTargetShapeTransformed.Length; ii++)
            {
                result[ii].X += pntTargetShapeTransformed[ii].X + cmEmitterOfSmokeLocation.X;
                result[ii].Y += pntTargetShapeTransformed[ii].Y + cmEmitterOfSmokeLocation.Y;
            }
            return result;
        }
        private PointF[] GetPointsArr(ShapePoint2D[] shapePoints,Size canvaSize)
        {
            List<PointF> pnts = new List<PointF>(shapePoints.Length);
            
            for (int ii = 0; ii < shapePoints.Length; ii++)
            {
                var Xp = (float) (shapePoints[ii].point.X / Camera.CameraSettings.SensorWidth * canvaSize.Width);
                var Yp = (float) (shapePoints[ii].point.Y / Camera.CameraSettings.SensorHeight * canvaSize.Height);
                pnts.Add(new PointF((float)Xp, (float)Yp));
            }

            List<PointF> convxHullpnts = ConvexHull.GetConvexHull(pnts);
            return convxHullpnts.ToArray();
        }

        private void CloseThisForm()
        {
            oListener.StopListening();
            recalcTimer.Stop();
            bIsRunning = false;
            this.Close();
        }
#warning For debug
        private void BoundaryDetect()
        {
            if (emitterOfSmokeLocation.X > picBox.Width)
            {
            }
            else if (emitterOfSmokeLocation.Y > picBox.Height)
            {
            }
            else if (emitterOfSmokeLocation.X < 0)
            {
            }
            else if (emitterOfSmokeLocation.Y < 0)
            {
            }
        }

        #region From event handlers

        protected override void OnClosing(CancelEventArgs e)
        {
            bIsRunning = false;
            if (thrTargetDraw.IsAlive)
            {
                thrTargetDraw.Abort();
            }

            base.OnClosing(e);
        }

        private void _MouseDoubleClick(object sender, MouseEventArgs e)
        {
            bIsRunning = false;
        }

        private void _DoubleClick(object sender, EventArgs e)
        {
            bIsRunning = false;

        }

        private void frmMain_MouseDown(object sender, MouseEventArgs e)
        {
            mouseDown = true;
            lastFormLocation = e.Location;
        }

        private void frmMain_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDown)
            {
                this.Location = new Point(
                    (this.Location.X - lastFormLocation.X) + e.X, (this.Location.Y - lastFormLocation.Y) + e.Y);

                this.Update();
            }
        }

        private void frmMain_MouseUp(object sender, MouseEventArgs e)
        {
            mouseDown = false;
        }
        #endregion From event handlers


    }
}
