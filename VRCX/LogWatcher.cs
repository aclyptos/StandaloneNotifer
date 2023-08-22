// Copyright(c) 2019-2022 pypy, Natsumi and individual contributors.
// All rights reserved.
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace StandaloneNotifier.VRCX
{
    public class LogWatcher
    {
        private class LogContext
        {
            public long Length;
            public long Position;
            public string RecentWorldName;
            public bool ShaderKeywordsLimitReached = false;
            public bool AudioDeviceChanged = false;
            public string LastAudioDevice;
            public string LastVideoError;
            public string onJoinPhotonDisplayName;
            public string locationDestination;
        }

        public static readonly LogWatcher Instance;
        private readonly DirectoryInfo m_LogDirectoryInfo;
        private readonly Dictionary<string, LogContext> m_LogContextMap; // <FileName, LogContext>
        private readonly ReaderWriterLockSlim m_LogListLock;
        private readonly List<string[]> m_LogList;
        private Thread m_Thread;
        private bool m_ResetLog;
        private bool m_FirstRun = true;
        private static DateTime tillDate = DateTime.Now;

        // NOTE
        // FileSystemWatcher() is unreliable

        static LogWatcher()
        {
            Instance = new LogWatcher();
        }

        public LogWatcher()
        {
            var logPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low/VRChat/VRChat";
            m_LogDirectoryInfo = new DirectoryInfo(logPath);
            m_LogContextMap = new Dictionary<string, LogContext>();
            m_LogListLock = new ReaderWriterLockSlim();
            m_LogList = new List<string[]>();
            m_Thread = new Thread(ThreadLoop)
            {
                IsBackground = true
            };
        }

        internal void Init()
        {
            m_Thread.Start();
        }

        internal void Exit()
        {
            var thread = m_Thread;
            m_Thread = null;
            thread.Interrupt();
            thread.Join();
        }

        public void Reset()
        {
            m_ResetLog = true;
            m_Thread?.Interrupt();
        }

        public void SetDateTill(string date)
        {
            tillDate = DateTime.Parse(date);
        }

        private void ThreadLoop()
        {
            while (m_Thread != null)
            {
                Update();

                try
                {
                    Thread.Sleep(1000);
                }
                catch (ThreadInterruptedException)
                {
                    Console.WriteLine("thread interrupted");
                }
            }
        }

        // double GetFileInfos = 0;
        // double FileInfosSorting = 0;
        // double LoopIteration = 0;
        // double ParseLogTime = 0;
        // double UpdateTime = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Update()
        {
            // double getFileInfos = 0;
            // double fileInfosSorting = 0;
            // double loopIteration = 0;
            // double parseLog = 0;
            // double update = 0;
            // DateTime startUpdate = DateTime.Now;
            if (m_ResetLog == true)
            {
                m_FirstRun = true;
                m_ResetLog = false;
                m_LogContextMap.Clear();
                m_LogListLock.EnterWriteLock();
                try
                {
                    m_LogList.Clear();
                }
                finally
                {
                    m_LogListLock.ExitWriteLock();
                }
            }

            var deletedNameSet = new HashSet<string>(m_LogContextMap.Keys);
            m_LogDirectoryInfo.Refresh();

            if (m_LogDirectoryInfo.Exists == true)
            {
                // DateTime start = DateTime.Now;
                var fileInfos = m_LogDirectoryInfo.GetFiles("output_log_*.txt", SearchOption.TopDirectoryOnly);
                // getFileInfos = (DateTime.Now - start).TotalMilliseconds;

                // start = DateTime.Now;
                Array.Sort(fileInfos, (a, b) => a.CreationTimeUtc.CompareTo(b.CreationTimeUtc));
                // fileInfosSorting = (DateTime.Now - start).TotalMilliseconds;

                foreach (var fileInfo in fileInfos)
                {
                    // start = DateTime.Now;
                    fileInfo.Refresh();
                    if (fileInfo.Exists == false)
                    {
                        // if ((DateTime.Now - start).TotalMilliseconds > loopIteration) loopIteration = (DateTime.Now - start).TotalMilliseconds;
                        continue;
                    }

                    if (DateTime.Compare(fileInfo.LastWriteTime, tillDate) < 0)
                    {
                        // if ((DateTime.Now - start).TotalMilliseconds > loopIteration) loopIteration = (DateTime.Now - start).TotalMilliseconds;
                        continue;
                    }

                    if (m_LogContextMap.TryGetValue(fileInfo.Name, out LogContext logContext) == true)
                    {
                        deletedNameSet.Remove(fileInfo.Name);
                    }
                    else
                    {
                        logContext = new LogContext();
                        m_LogContextMap.Add(fileInfo.Name, logContext);
                    }

                    if (logContext.Length == fileInfo.Length)
                    {
                        // if ((DateTime.Now - start).TotalMilliseconds > loopIteration) loopIteration = (DateTime.Now - start).TotalMilliseconds;
                        continue;
                    }

                    logContext.Length = fileInfo.Length;

                    DateTime parsestart = DateTime.Now;
                    ParseLog(fileInfo, logContext);
                    // parseLog = (DateTime.Now - parsestart).TotalMilliseconds;
                    // if ((DateTime.Now - start).TotalMilliseconds > loopIteration) loopIteration = (DateTime.Now - start).TotalMilliseconds;
                }
            }

            foreach (var name in deletedNameSet)
            {
                m_LogContextMap.Remove(name);
            }

            m_FirstRun = false;

            /*
            update = (DateTime.Now - startUpdate).TotalMilliseconds;

            if (update > UpdateTime) UpdateTime = update;
            if (parseLog > ParseLogTime) ParseLogTime = parseLog;
            if (loopIteration > LoopIteration) LoopIteration = loopIteration;
            if (fileInfosSorting > FileInfosSorting) FileInfosSorting = fileInfosSorting;
            if (getFileInfos > GetFileInfos) GetFileInfos = getFileInfos;
            */

            /*
            Console.Title = "YoinkerDetector Standalone -> "
                + UpdateTime
                + "|"
                + ParseLogTime
                + "|"
                + LoopIteration
                + "|"
                + FileInfosSorting
                + "|"
                + GetFileInfos;
            */
        }

        private void ParseLog(FileInfo fileInfo, LogContext logContext)
        {
            try
            {
                using (var stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 65536, FileOptions.SequentialScan))
                {
                    stream.Position = logContext.Position;
                    using (var streamReader = new StreamReader(stream, Encoding.UTF8))
                    {
                        while (true)
                        {
                            var line = streamReader.ReadLine();
                            if (line == null)
                            {
                                logContext.Position = stream.Position;
                                break;
                            }

                            if (line.Length > 60 && line.Length < 110 && line[34] == '[' && line[54] == 'J')
                            {
                                if (DateTime.TryParseExact(
                                    line.Substring(0, 19),
                                    "yyyy.MM.dd HH:mm:ss",
                                    CultureInfo.InvariantCulture,
                                    DateTimeStyles.AssumeLocal,
                                    out DateTime lineDate
                                ) &&
                                DateTime.Compare(lineDate, tillDate) <= 0)
                                {
                                    continue;
                                }

                                ParseLogOnPlayerJoinedOrLeft(fileInfo, logContext, line);
                            }
                        }
                    }
                }
            }
            catch
#if DEBUG
            (Exception ex)
#endif
            {
#if DEBUG
                Console.WriteLine(ex);
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AppendLog(string[] item)
        {
            m_LogListLock.EnterWriteLock();
            try
            {
                if (!m_FirstRun)
                {
                    if (item.Length == 4 && item[2] == "player-joined")
                    {
                        Program.HandleJoin(item[3]);
                    }
                }
                m_LogList.Add(item);
            }
            finally
            {
                m_LogListLock.ExitWriteLock();
            }
        }

        private string ConvertLogTimeToISO8601(string line)
        {
            // 2020.10.31 23:36:22

            if (DateTime.TryParseExact(
                line.Substring(0, 19),
                "yyyy.MM.dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeLocal,
                out DateTime dt
            ) == false)
            {
                dt = DateTime.UtcNow;
            }

            return dt.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ParseLogOnPlayerJoinedOrLeft(FileInfo fileInfo, LogContext logContext, string line)
        {
            if (line.Contains("[Behaviour] OnPlayerJoined") &&
                !line.Contains("] OnPlayerJoined:"))
            {
                var lineOffset = line.LastIndexOf("] OnPlayerJoined");
                if (lineOffset < 0)
                    return true;
                lineOffset += 17;
                if (lineOffset > line.Length)
                    return true;

                var userDisplayName = line.Substring(lineOffset);

                AppendLog(new[]
                {
                    fileInfo.Name,
                    ConvertLogTimeToISO8601(line),
                    "player-joined",
                    userDisplayName
                });

                logContext.onJoinPhotonDisplayName = userDisplayName;

                return true;
            }

            return false;
        }
    }
}