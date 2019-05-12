using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using SkiaSharp;
using TeaserDSV.Model;
using TeaserDSV.Utilities;
using Brush = System.Drawing.Brush;
using Color = System.Drawing.Color;
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
        private Thread thrPictureRedraw;

        private readonly cSmoker oSmoker = new cSmoker();

        private readonly object _lockObj = new object();
        private readonly AutoResetEvent areCalcParticles = new AutoResetEvent(false);
        private readonly AutoResetEvent areDrawParticles = new AutoResetEvent(false);

        private readonly AutoResetEvent areRefreshDrawing = new AutoResetEvent(false);
        private readonly AutoResetEvent areNewSixMessage = new AutoResetEvent(false);

        //private BackGroundPool oBackgPool = new BackGroundPool(sExtPath);
        private readonly skBackgroundPool oSkBackgPool = new skBackgroundPool(sExtPath);
        private BitmapPool oBMPPool;

        private readonly Body m_Body = new Body();

        private bool bIsRunning { get; set; }

        private int LedPointIndex;

        private Graphics grphxDrawer;
        private Image bgImage;
        private int iFrameNum = 0;

        private bool mouseDown;
        private Point lastFormLocation;

        private bool isLedOn;
        private int m_color;
        private int m_direction;
        private Timer recalcTimer;

        private const string sExtPath = @"..\..\External\";
        private readonly Bitmap ledImage = new Bitmap(sExtPath + "OnSep32x32_.png");
        private BitmapPool oBitmapPool;
        private SKImage skledImage;

        public EventHandler evUpdateClosed;


        readonly Stopwatch swMainLoopTimer = new Stopwatch();
        readonly Stopwatch swCommTimer = new Stopwatch();
        readonly Stopwatch swProjTimer = new Stopwatch();
        readonly Stopwatch swSmokeTimer = new Stopwatch();
        readonly Stopwatch swRedrawTimer = new Stopwatch();


        public double FrameRenderTime { get; set; }
        public double CommReceiveTime { get; set; }
        public double ProjCalcTime { get; set; }
        public double SmokeCalcTime { get; set; }

        public double ParticlesRedrawTime { get; set; }
        public double TargetRedrawTime { get; set; }
        public double LedRedrawTime { get; set; }

        List<double> lstTimes = new List<double>();
        #endregion Private Members


        public fInjectedImage()
        {
            InitializeComponent();
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // Set defaults and init members

            SmokeColorInit();
            InitCanvasForm();
            InitializeTimer();
            InitTarget();
            // Start worker threads that wait for AutoReset event
            StartListening();
            InitSixDataHandler();
            InitRedraw();
        }

        protected override void OnPaint(PaintEventArgs e)
        {

        }

        private void InitRedraw()
        {
            thrPictureRedraw = new Thread(RedrawPicBox);
            thrPictureRedraw.Name = "Picturebox redraw";
            thrPictureRedraw.IsBackground = true;
            thrPictureRedraw.Priority = ThreadPriority.Highest;
            thrPictureRedraw.Start();
        }
        private void InitCanvasForm()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.UserPaint, true);
            this.BackgroundImageLayout = ImageLayout.Stretch;

            //picBox.Dock = DockStyle.Fill;
            SKBitmap bg = oSkBackgPool.GetBackGroundFromPool();
            oBMPPool = new BitmapPool(bg.Width, bg.Height, 20);

            Bitmap bmp = oBMPPool.GetObject();
            this.BackgroundImage = ConvertToBitmap(bg, bmp);

            Scales.SetScale(new Size(bg.Width, bg.Height));
            Screen[] screens = Screen.AllScreens;
            if (screens.Length > 1)
            {
                this.Location = screens[1].WorkingArea.Location;
                this.WindowState = FormWindowState.Maximized;
            }
            else
            {
                this.Location = screens[0].WorkingArea.Location;
            }

            this.FormBorderStyle = FormBorderStyle.None;

            SKBitmap skledBitmap = SKBitmap.Decode(sExtPath + "OnSep32x32_.png");

            skledImage = SKImage.FromBitmap(skledBitmap);
        }

        private void RedrawPicBox()
        {
            double dTime = 0;
            double dTimeDraw = 0;
            while (bIsRunning)
            {
                areRefreshDrawing.WaitOne();
                //if (!picBox.IsDisposed)
                //{
                try
                {
                    
                    swMainLoopTimer.Restart();
                    SKBitmap skbitmap = oSkBackgPool.GetBackGroundFromPool(); //Get background bitmap
                    if (skbitmap != null)
                    {
                        lock (_lockObj)
                        {
                            swRedrawTimer.Restart();
                            RedrawParticles(skbitmap);
                            dTimeDraw=(double)swRedrawTimer.ElapsedTicks / Stopwatch.Frequency * 1000D;
                            ParticlesRedrawTime=dTimeDraw;
                            swRedrawTimer.Restart();
                            RedrawTarget(skbitmap);
                            dTimeDraw=(double)swRedrawTimer.ElapsedTicks / Stopwatch.Frequency * 1000D;
                            TargetRedrawTime=dTimeDraw;
                             swRedrawTimer.Restart();
                            DrawLed(skbitmap);
                            dTimeDraw=(double)swRedrawTimer.ElapsedTicks / Stopwatch.Frequency * 1000D;
                            LedRedrawTime=dTimeDraw;

                        }

                        #region previous implemetation with Graphics
                        //grphxDrawer = Graphics.FromImage(copyBGImage);
                        //RedrawParticles(grphxDrawer);
                        //RedrawTarget(grphxDrawer, IsLedOn);
                        //DrawLedFromFile(grphxDrawer);
                        #endregion previous implemetation with Graphics

                        // instead of creating Bitmap objects i am reusing them from pool
                        Bitmap bmp = oBMPPool.GetObject();

                        this.BackgroundImage = ConvertToBitmap(skbitmap, bmp);
                        dTime = (double)swMainLoopTimer.ElapsedTicks / Stopwatch.Frequency * 1000D;
                        FrameRenderTime = dTime;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);

                }
                //}
            }
        }

        private void SmokeColorInit()
        {
            Color tmp = Color.FromName(SettingsHolder.Instance.enmSmokeColor.ToString());

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
            thrSixDataHandle = new Thread(ComputeProjection) { IsBackground = true, Name = "Six handler" };
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
            conqSixMsgs = new ConcurrentQueue<SixMsg>();
            bIsRunning = true;
            thrTargetDraw = new Thread(SmokeParticlesManager) { IsBackground = true, Name = "Smoke particle thread" };
            thrTargetDraw.Start();
            m_Body.OriginalPoints = LoadTargetFromFile(sExtPath + "TeaserShape.txt");
            LedPointIndex = GetLedPointFromTarget(m_Body);
        }

        private int GetLedPointFromTarget(Body mBody)
        {
            int iRes = -1;
            if (mBody != null)
            {
                for (int ii = 0; ii < m_Body.OriginalPoints.Length; ii++)
                {
                    if (m_Body.OriginalPoints[ii].isLED)
                    {
                        iRes = ii;
                    }
                }
            }


            return iRes;
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
            // sets smoke computation
            areCalcParticles.Set();
            // set redraw of all picture elements (background,smoke,led,target)
            areRefreshDrawing.Set();

            areDrawParticles.Set(); // not in use yet
        }

        public void SmokeParticlesManager()
        {
            double dTime = 0;
            try
            {
                while (bIsRunning)
                {
                    areCalcParticles.WaitOne();
                    swSmokeTimer.Restart();
                    lock (_lockObj)
                    {
                        // calculate position and state of all particles
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
                        if (m_Body.IsSmokeOn)
                        {
                            if (m_Body.ImagePoints[LedPointIndex].point.X > 0 || m_Body.ImagePoints[LedPointIndex].point.Y > 0)
                            {
                                CreateParticleEmitter(m_Body.ImagePoints[LedPointIndex].point);
                            }
                        }
                    }
                    dTime = (double)swSmokeTimer.ElapsedTicks / Stopwatch.Frequency * 1000D;
                    SmokeCalcTime = dTime;
                }
            }
            catch (ThreadAbortException ex)
            {
                Debug.Print("Proc Thread aborting");
            }
            //this.Invoke(new MethodInvoker(CloseThisForm));
        }

        private void CreateParticleEmitter(PointF emitPoint)
        {
            lock (_lockObj)
            {
                int localLength = SettingsHolder.Instance.ParticleNumber;
                for (int ii = 0; ii < localLength; ii++)
                {
                    Particle createparticle = oSmoker.GetFromPool(emitPoint, SettingsHolder.Instance.ParticlesSpeed);
                    oSmoker.Particles.Add(createparticle);
                }
            }
        }

        private void RedrawTarget(Graphics grphxDrawerTarget, bool IsLedVisible = false)
        {
            if (m_Body.ImagePoints != null && m_Body.ImagePoints.Length > 0)
            {
                PointF[] XandZ = GetPointsArr(m_Body.ImagePoints);
                grphxDrawerTarget.FillPolygon(new SolidBrush(Color.Red), XandZ);
            }
        }

        private void RedrawTarget(SKBitmap grphxDrawerTarget, bool IsLedVisible = false)
        {

            if (m_Body.ImagePoints != null && m_Body.ImagePoints.Length > 0)
            {
                SKPoint[] XandZ = GetSkPointsArr(m_Body.ImagePoints);
                using (SKPaint skpaint = new SKPaint { Color = new SKColor(255, 0, 0), Style = SKPaintStyle.Fill })
                using (SKCanvas skcanvas = new SKCanvas(grphxDrawerTarget))
                {
                    SKPath path = new SKPath() { FillType = SKPathFillType.EvenOdd };
                    path.AddPoly(XandZ, true);
                    skcanvas.DrawPath(path, skpaint);
                    //skcanvas.DrawPoints(SKPointMode.Polygon,XandZ,skpaint);
                }
            }
        }

        private void DrawLed(Graphics grphxDrawerTarget)
        {
            if (m_Body.ImagePoints != null && m_Body.ImagePoints.Length > 0)
            {

                ledImage.MakeTransparent(Color.Black);
                for (int ii = 0; ii < m_Body.ImagePoints.Length; ii++)
                {
                    if (m_Body.ImagePoints[ii].isLED && m_Body.IsLEDOn)
                    {
                        PointF LedPos = m_Body.ImagePoints[ii].point;
                        SizeF LedSize = m_Body.LedSize;
                        Point leftUp = new Point((int)(LedPos.X - LedSize.Width / 2), (int)(LedPos.Y - LedSize.Height / 2));
                        RectangleF destRect = new RectangleF(leftUp.X, leftUp.Y, LedSize.Width, LedSize.Height);
                        if (destRect.IntersectsWith(grphxDrawerTarget.VisibleClipBounds) && destRect.Width < grphxDrawerTarget.VisibleClipBounds.Width && destRect.Height < grphxDrawerTarget.VisibleClipBounds.Height)
                        {
                            grphxDrawerTarget.DrawImage(ledImage, destRect,
                                new RectangleF(0, 0, ledImage.Width, ledImage.Height), GraphicsUnit.Pixel);
                        }
                        break;
                    }
                }
            }

        }

        private void DrawLed(SKBitmap grphxDrawerTarget)
        {
            if (m_Body.ImagePoints != null && m_Body.ImagePoints.Length > 0 && CheckIfLedIsVissible(grphxDrawerTarget))
            {
                PointF LedPos = m_Body.ImagePoints[LedPointIndex].point;
                SizeF LedSize = m_Body.LedSize;

                Point leftUp = new Point((int)(LedPos.X - LedSize.Width / 2), (int)(LedPos.Y - LedSize.Height / 2));
                SKRect destRect = new SKRect(leftUp.X, leftUp.Y, leftUp.X + LedSize.Width, leftUp.Y + LedSize.Height);

                using (SKCanvas skcanvas = new SKCanvas(grphxDrawerTarget))
                {
                    if (destRect.IntersectsWith(skcanvas.DeviceClipBounds) && destRect.Width < skcanvas.DeviceClipBounds.Width && destRect.Height < skcanvas.DeviceClipBounds.Height)
                    {
                        skcanvas.DrawImage(skledImage, destRect);
                    }
                }
            }
        }

        private bool CheckIfLedIsVissible(SKBitmap grphxDrawerTarget)
        {
            return m_Body.IsLEDOn && m_Body.ImagePoints[LedPointIndex].point.X > 0 &&
                   m_Body.ImagePoints[LedPointIndex].point.Y > 0 &&
                   m_Body.ImagePoints[LedPointIndex].point.X < grphxDrawerTarget.Width &&
                   m_Body.ImagePoints[LedPointIndex].point.Y < grphxDrawerTarget.Height;
        }

        private void RedrawParticles(Graphics grphxDrawerParticles)
        {
            int alpha = 0;

            lock (_lockObj)
            {
                for (var ii = 0; ii < oSmoker.Particles.Count; ii++)
                {
                    Particle p = oSmoker.Particles[ii];
                    float pX = (p.Location.X - SettingsHolder.Instance.ParticleSize / 2F);

                    float pY = (p.Location.Y - SettingsHolder.Instance.ParticleSize / 2F);

                    if (pX < 0 || pY < 0 ||
                        pX > grphxDrawerParticles.VisibleClipBounds.Width ||
                        pY > grphxDrawerParticles.VisibleClipBounds.Height)
                    {
                        continue;
                    }

                    p.GetColorToLife(ref alpha, ref m_color, m_direction);

                    Brush particleBrush;
                    using (particleBrush = new SolidBrush(Color.FromArgb(alpha, m_color, m_color, m_color)))
                    {
                        grphxDrawerParticles.FillEllipse(particleBrush, pX, pY,
                                SettingsHolder.Instance.ParticleSize, SettingsHolder.Instance.ParticleSize);
                    }
                }
            }
        }


        private void RedrawParticles(SKBitmap grphxDrawerParticles)
        {

            int alpha = 0;

            for (var ii = 0; ii < oSmoker.Particles.Count; ii++)
            {
                Particle p = oSmoker.Particles[ii];
                p.GetColorToLife(ref alpha, ref m_color, m_direction);

                using (SKPaint skpaint = new SKPaint { Color = new SKColor((byte)m_color, (byte)m_color, (byte)m_color, (byte)alpha) })
                {
                    var pX = (p.Location.X - SettingsHolder.Instance.ParticleSize / 2F);

                    var pY = (p.Location.Y - SettingsHolder.Instance.ParticleSize / 2F);
                    if (pX < grphxDrawerParticles.Width && pX > 0 &&
                        pY < grphxDrawerParticles.Height && pY > 0)
                    {
                        using (SKCanvas skcanvas = new SKCanvas(grphxDrawerParticles))
                        {
                            skcanvas.DrawOval(pX, pY, SettingsHolder.Instance.ParticleSize, SettingsHolder.Instance.ParticleSize, skpaint);
                        }
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
            double dTime = (double)swCommTimer.ElapsedTicks / Stopwatch.Frequency * 1000D;
            CommReceiveTime = dTime;
            SixMsg oSixMsg = (SixMsg)sender;
            conqSixMsgs.Enqueue(oSixMsg);
            areNewSixMessage.Set();
            swCommTimer.Restart();
        }

        private void ComputeProjection()
        {
            double dTime = 0;
            while (bIsRunning)
            {
                try
                {

                    areNewSixMessage.WaitOne();
                    swProjTimer.Restart();

                    if (conqSixMsgs.Count > 0)
                    {
                        SixMsg oSixMsg;
                        conqSixMsgs.TryDequeue(out oSixMsg);

                        lock (_lockObj)
                        {
                            m_Body.RollAngle = oSixMsg.Object_Roll;
                            m_Body.YawAngle = oSixMsg.Object_Yaw;
                            m_Body.PitchAngle = oSixMsg.Object_Pitch;
                            m_Body.IsLEDOn = (oSixMsg.Object_model == 1);
                            m_Body.IsSmokeOn = (oSixMsg.Smoke == 1);
                            m_Body.ComputeProjection(new double[] { oSixMsg.Object_X, oSixMsg.Object_Y, oSixMsg.Object_Z });
                        }

                        //BoundaryDetect();
                    }
                    else
                    {
                        continue;
                    }
                    dTime = (double)swProjTimer.ElapsedTicks / Stopwatch.Frequency * 1000D;
                    ProjCalcTime = dTime;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }

            }
        }

        private PointF[] GetPointsArr(ShapePoint2D[] shapePoints)
        {
            List<PointF> pnts = new List<PointF>(shapePoints.Length);

            for (int ii = 0; ii < shapePoints.Length; ii++)
            {

                pnts.Add(new PointF((float)shapePoints[ii].point.X, shapePoints[ii].point.Y));
            }

            List<PointF> convxHullpnts = ConvexHull.GetConvexHull(pnts);
            return convxHullpnts.ToArray();
        }
        private SKPoint[] GetSkPointsArr(ShapePoint2D[] shapePoints)
        {
            List<SKPoint> pnts = new List<SKPoint>(shapePoints.Length);
            for (int ii = 0; ii < shapePoints.Length; ii++)
            {
                if (shapePoints[ii].point.X > 0 &&
                    shapePoints[ii].point.Y > 0 &&
                    shapePoints[ii].point.X < this.Width &&
                    shapePoints[ii].point.Y < this.Height)
                {
                    pnts.Add(new SKPoint((float)shapePoints[ii].point.X, (float)shapePoints[ii].point.Y));
                }
            }
            List<SKPoint> convxHullpnts = ConvexHull.GetConvexHull(pnts);
            return convxHullpnts.ToArray();
        }


        public void CloseThisForm()
        {
            mouseDown = false;
            bIsRunning = false;
            oListener.StopListening();
            recalcTimer.Enabled = false;
            recalcTimer.Stop();
            Thread.Sleep(TimeSpan.FromMilliseconds(2 * SettingsHolder.Instance.RedrawFreq));
            oSkBackgPool.Stop();
            while (thrPictureRedraw.IsAlive)
            {
                areRefreshDrawing.Set();
                areCalcParticles.Set();
                areNewSixMessage.Set();
                Application.DoEvents();
            }

            evUpdateClosed(string.Empty, EventArgs.Empty);
            this.Close();
        }
#warning For debug
        private void BoundaryDetect(PointF emitterOfSmokeLocation)
        {
            if (emitterOfSmokeLocation.X > this.Width)
            {
            }
            else if (emitterOfSmokeLocation.Y > this.Height)
            {
            }
            else if (emitterOfSmokeLocation.X < 0)
            {
            }
            else if (emitterOfSmokeLocation.Y < 0)
            {
            }
        }

        unsafe struct copystruct128
        {
            fixed long l[128];
        }
        public unsafe void MemmoryCopy128b(IntPtr Destination, IntPtr Source, int Length)
        {
            // no need to pin the dest,src variables with 'fixed' because they are meant for a locked bitmaps anyway

            copystruct128* src = (copystruct128*)Source;
            copystruct128* dest = (copystruct128*)Destination;

            for (int ii = 0; ii < Length / sizeof(copystruct128); ii++)
            {
                *dest++ = *src++;

            }
        }

        public Image ConvertToBitmap(SKBitmap Source, Bitmap Destination)
        {

            BitmapData bmpData = Destination.LockBits(new Rectangle(0, 0, Destination.Width, Destination.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);

            MemmoryCopy128b(bmpData.Scan0, Source.GetPixels(), Source.ByteCount);

            //Marshal.Copy(Source.GetPixels(), byTemp_ConvertToBitmap, 0, byTemp_ConvertToBitmap.Length);
            //Marshal.Copy(byTemp_ConvertToBitmap, 0, bmpData.Scan0, byTemp_ConvertToBitmap.Length);

            Destination.UnlockBits(bmpData);
            return Destination;
        }
        #region From event handlers

        protected override void OnClosing(CancelEventArgs e)
        {
            bIsRunning = false;
            if (thrTargetDraw.IsAlive)
            {
                thrTargetDraw.Abort();
            }
            oListener.StopListening();

            base.OnClosing(e);
        }

        private void _MouseDoubleClick(object sender, MouseEventArgs e)
        {
            CloseThisForm();
        }

        private void _DoubleClick(object sender, EventArgs e)
        {
            CloseThisForm();

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


    public class BackGroundPool
    {
        private ConcurrentQueue<Image> qBckgImagePool = new ConcurrentQueue<Image>();
        private Thread thBackLoad;
        private List<string> lstBackgImages;
        private bool cancelReq = false;
        private int iCurrentPosition;
        private AutoResetEvent arReadImageEvent = new AutoResetEvent(true);

        public Image GetBackGroundFromPool()
        {
            Image result = null;
            qBckgImagePool.TryDequeue(out result);
            arReadImageEvent.Set();
            return result;
        }
        public BackGroundPool(string sExtPath)
        {
            lstBackgImages = new List<string>(Directory.GetFiles(sExtPath + "\\Frames\\", "*.jpg"));
            if (thBackLoad == null)
            {
                thBackLoad = new Thread(LoadImagesAsync);
                thBackLoad.IsBackground = true;
                thBackLoad.Start();
            }

            while (qBckgImagePool.Count == 0)
            {

            }
            iCurrentPosition = 0;
        }


        private void LoadImagesAsync()
        {
            Image result;
            bool bIsIncreasing = true;
            while (!cancelReq)
            {
                arReadImageEvent.WaitOne();
                while (qBckgImagePool.Count < 2)
                {
                    bIsIncreasing = ChangeDirectionOfFilesRead(bIsIncreasing);


                    result = Image.FromFile(lstBackgImages[iCurrentPosition]);
                    qBckgImagePool.Enqueue(result);

                    if (bIsIncreasing)
                    {
                        iCurrentPosition++;
                    }
                    else
                    {
                        iCurrentPosition--;
                    }
                }

            }
        }

        private bool ChangeDirectionOfFilesRead(bool bIsIncreasing)
        {
            if (iCurrentPosition == (lstBackgImages.Count - 1))
            {
                bIsIncreasing = false;
            }
            else if (!bIsIncreasing && iCurrentPosition == 0)
            {
                bIsIncreasing = true;
            }
            return bIsIncreasing;
        }

        public void Stop()
        {
            cancelReq = true;
            arReadImageEvent.Set();
        }
    }
    public class skBackgroundPool
    {
        private readonly ConcurrentQueue<SKBitmap> qBckgImagePool = new ConcurrentQueue<SKBitmap>();
        private readonly Thread thBackLoad;
        private readonly List<string> lstBackgImages;
        private bool cancelReq = false;
        private int iCurrentPosition;
        private readonly AutoResetEvent arReadImageEvent = new AutoResetEvent(true);

        public SKBitmap GetBackGroundFromPool()
        {
            SKBitmap result = null;
            qBckgImagePool.TryDequeue(out result);
            arReadImageEvent.Set();
            return result;
        }
        byte[][] bgImages;
        public skBackgroundPool(string sExtPath)
        {
            lstBackgImages = new List<string>(Directory.GetFiles(sExtPath + "\\Frames\\", "*.jpg"));
            bgImages=new byte[lstBackgImages.Count][];

            for (int ii = 0; ii < bgImages.Count(); ii++)
            {
                bgImages[ii]=File.ReadAllBytes(lstBackgImages[ii]);
            }

            if (thBackLoad == null)
            {
                thBackLoad = new Thread(LoadImagesAsync);
                thBackLoad.IsBackground = true;
                thBackLoad.Start();
            }

            while (qBckgImagePool.Count == 0)
            {

            }
            iCurrentPosition = 0;
        }

        private void LoadImagesAsync()
        {
            SKBitmap skBitmap;
            bool bIsIncreasing = true;
            while (!cancelReq)
            {
                arReadImageEvent.WaitOne();
                while (qBckgImagePool.Count < 10)
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    skBitmap = SKBitmap.Decode(bgImages[iCurrentPosition]);
                    qBckgImagePool.Enqueue(skBitmap);
                    sw.Stop();

                    bIsIncreasing = ChooseDirectionOfFilesRead(bIsIncreasing);

                    if (bIsIncreasing)
                    {
                        iCurrentPosition++;
                    }
                    else
                    {
                        iCurrentPosition--;
                    }
                }

            }
        }

        private bool ChooseDirectionOfFilesRead(bool bIsIncreasing)
        {
            if (iCurrentPosition == (lstBackgImages.Count - 1))
            {
                bIsIncreasing = false;
            }
            else if (!bIsIncreasing && iCurrentPosition == 0)
            {
                bIsIncreasing = true;
            }
            return bIsIncreasing;
        }

        public void Stop()
        {
            cancelReq = true;
            arReadImageEvent.Set();
        }
    }


    interface IDrawable
    {

        Rectangle GetVisibleClipBounds();
        void FillEllipse(int alpha, int m_color1, int m_color2, int m_color3, int pX, int pY, int elipseWidth,
            int elipseHight);

        void FillPolygon(Color col, PointF XandZ);
        void DrawImage(Bitmap img, Rectangle source, Rectangle dest);
    }


}
