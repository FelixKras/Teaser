using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TeaserDSV
{
    public sealed class LogWriter : IDisposable
    {
        private ConcurrentQueue<Log> logQueue;
        private string logDir = AppDomain.CurrentDomain.BaseDirectory + "Logs\\";
        private string logFile;
        private int maxLogAge = 2;
        private int queueSize = 50;
        private DateTime LastFlushed = DateTime.Now;
        private readonly object oLocker = new object();
        private Task<bool> tskWriter;
        private AutoResetEvent waitHandle;
        private AutoResetEvent stopHandle;
        private bool bStopWriting;

        private static readonly Lazy<LogWriter> lazy = new Lazy<LogWriter>(() =>  new LogWriter());
        
        public static LogWriter Instance
        {
            get
            {
                return lazy.Value;
            }
        }

        private LogWriter()
        {
            logQueue = new ConcurrentQueue<Log>();
            logFile = "AppLog" + "_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".log";
            StartWritingTask();
            WriteToLog("Started on " + Environment.MachineName + " at: " + DateTime.Now);
        }


        

        private void StartWritingTask()
        {
            bStopWriting = false;
            waitHandle = new AutoResetEvent(false);
            stopHandle = new AutoResetEvent(false);

            tskWriter = new Task<bool>(FlushLog);
            tskWriter.Start();


        }


        //TODO:check how to improve dispose

        public void Dispose()
        {
            // Shitty design: i release any waiting thread with first stophandle, and then the main with the seccond

            bStopWriting = true;
            try
            {
                stopHandle.Set();
                tskWriter.Wait();
                logQueue.Enqueue(new Log("Writing stopped"));
                stopHandle.Set();
                FlushLog();
                waitHandle.Close();
                stopHandle.Close();
            }
            catch
            {

            }

        }

        /// <summary>
        /// The method that writes to the log file
        /// </summary>
        /// <param name="message">The message to write to the log</param>
        public void WriteToLog(string message)
        {
            Log logEntry = new Log(message);
            bool bIsLockTaken = false;

            logQueue.Enqueue(logEntry);



            try
            {
                bIsLockTaken = Monitor.TryEnter(oLocker, new TimeSpan(TimeSpan.TicksPerMillisecond / 2));

                // If we have reached the Queue Size then flush the Queue
                if (CheckIfFlushNeeded() && bIsLockTaken)
                {
                    waitHandle.Set();
                }
            }
            finally
            {
                if (bIsLockTaken)
                {
                    Monitor.Exit(oLocker);
                    bIsLockTaken = false;
                }
            }

        }


        private bool CheckIfFlushNeeded()
        {
            TimeSpan logAge = DateTime.Now - LastFlushed;
            if (logAge.TotalSeconds >= maxLogAge || logQueue.Count >= queueSize)
            {
                LastFlushed = DateTime.Now;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Flushes the Queue to the physical log file
        /// </summary>
        private bool FlushLog()
        {
            string logPath = logDir + "\\" + logFile;
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            FileStream fs;
            StreamWriter log;
            Log entry;
            bool bIsLockTaken = false;
            try
            {
                Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

                while (!bStopWriting)
                {
                    WaitHandle.WaitAny(new WaitHandle[] { waitHandle, stopHandle });


                    // This could be optimised to prevent opening and closing the file for each write
                    using (fs = File.Open(logPath, FileMode.Append, FileAccess.Write))
                    {
                        using (log = new StreamWriter(fs))
                        {
                            try
                            {
                                bIsLockTaken = Monitor.TryEnter(oLocker, -1);
                                while (logQueue.Count > 0)
                                {
                                    if (logQueue.TryDequeue(out entry))
                                    {
                                        log.WriteLine(string.Format("{0}:\t{1}", entry.LogTime, entry.Message));
                                    }

                                }
                            }
                            finally
                            {
                                if (bIsLockTaken)
                                {
                                    Monitor.Exit(oLocker);
                                    bIsLockTaken = false;
                                }
                            }


                        }
                    }
                }
            }
            finally
            {
                Thread.CurrentThread.Priority = ThreadPriority.Normal;
            }


            return true;

        }

        /// <summary>
        /// A Log class to store the message and the Date and Time the log entry was created
        /// </summary>
        class Log
        {
            public string Message { get; set; }
            public string LogTime { get; set; }
            public string LogDate { get; set; }

            public Log(string message)
            {
                LogTime = DateTime.Now.ToString("HH:mm:ss.fff");
                LogDate = DateTime.Now.ToString("yyyy-MM-dd");
                Message = message;

            }
        }
    }
}
