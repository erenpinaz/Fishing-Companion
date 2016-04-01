using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreAudio;
using FishingCompanion.Lib;

namespace FishingCompanion
{
    internal class Program
    {
        private static Process _gameProcess;
        private static IntPtr _hWnd;

        private static int _baitDuration = 300;
        private static int _fishingDuration = 21;

        private static MMDeviceEnumerator _devEnum;
        private static MMDevice _device;
        private static AudioSessionControl2 _session;
        private static readonly Queue<int> VolumeQueue = new Queue<int>(5);

        private static void Main(string[] args)
        {
            Console.WriteLine("[Initialization] -> Searching game process");

            // Get the game process (32-bit)
            _gameProcess = Process.GetProcessesByName("wow").FirstOrDefault();

            if (_gameProcess == null)
            {
                Console.WriteLine("[Initialization] -> Unable to find 32-bit game process. Searching for 64-bit version");

                // Get the game process (64-bit)
                _gameProcess = Process.GetProcessesByName("wow-64").FirstOrDefault();
            }

            if (_gameProcess == null)
            {
                Console.WriteLine("[Initialization] -> Unable to find 64-bit game process. Searcing with window name");

                // Get the game process by the window title
                _gameProcess =
                    Process.GetProcesses()
                        .FirstOrDefault(p => p.MainWindowTitle.ToUpperInvariant() == "WORLD OF WARCRAFT");
            }

            if (_gameProcess == null)
            {
                Console.WriteLine("[Initialization] -> Unable to find game window. Press any key to exit.");
                Console.ReadLine();

                // Exit if process is not found
                Environment.Exit(0);
            }

            Console.WriteLine("[Initialization] -> Game windows found. (PID : " + _gameProcess.Id + ", Title : " +
                              _gameProcess.MainWindowTitle + ")");

            // Process found get the handle
            _hWnd = _gameProcess.MainWindowHandle;

            // Bring game window to front
            Console.WriteLine("[Initialization] -> Activating game window");
            Win32.ActivateWindow(_hWnd);

            // Setup game audio session (splash listener)
            Console.WriteLine("[Initialization] -> Configuring audio session");
            _devEnum = new MMDeviceEnumerator();
            _device = _devEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);

            var sessionFound = false;
            for (var i = 0; i < _device.AudioSessionManager2.Sessions.Count; i++)
            {
                _session = _device.AudioSessionManager2.Sessions[i];
                if (_session.GetProcessID == _gameProcess.Id)
                {
                    sessionFound = true;
                    break;
                }
            }

            if (!sessionFound)
            {
                Console.WriteLine("[Initialization] -> Unable to find audio session. Press any key to exit.");
                Console.ReadLine();

                // Exit if audio session is not found
                Environment.Exit(0);
            }
            else
            {
                Console.WriteLine("[Initialization] -> Audio session is ready");
            }

            // Start fishing
            StartFishing();
        }

        /// <summary>
        /// Starts fishing procedure
        /// </summary>
        private static async void StartFishing()
        {
            Task.Factory.StartNew(CastBaitTask);

            while (true)
            {
                _fishingDuration = 21;

                // Cast fishing
                Console.WriteLine("[Procedure Manager] -> Casting fishing");
                Win32.CastFishing(_hWnd);

                // Scan fishing bobber
                var bobberFound = await Task.FromResult(ScanBobberTask());
                if (bobberFound)
                {
                    // Setup splash listener task
                    Task.Factory.StartNew(ListenSplashTask).Wait();
                }

                Thread.Sleep(TimeSpan.FromSeconds(3));
            }
        }

        private static bool ScanBobberTask()
        {
            // Get current (default) cursor
            var defaultCursor = Win32.GetDefaultGameCursor(_hWnd);

            // Calculate scanning area
            var wowRect = Win32.GetWindowRect(_hWnd);
            var xMin = wowRect.Right/4;
            var xMax = xMin*3;
            var yMin = wowRect.Bottom/3;
            var yMax = yMin*2;

            // Scan for bobber
            Console.WriteLine("[Procedure Manager] -> Scanning for bobber (Scan area x: [" + xMin + "-" + xMax +
                              "], y: [" + yMin + "-" + yMax + "])");

            var bobberFound = false;
            while (xMin <= xMax && !bobberFound)
            {
                for (var i = yMin; i < yMax; i += 50)
                {
                    Win32.MoveMouse(xMin, i);

                    Thread.Sleep(20);

                    var currentCursor = Win32.GetCursor();
                    if ((currentCursor.hCursor != defaultCursor.hCursor) ||
                        currentCursor.flags != defaultCursor.flags)
                    {
                        Console.WriteLine("[Procedure Manager] -> Bobber found");
                        bobberFound = true;
                        break;
                    }
                }

                xMin += 30;
            }

            return bobberFound;
        }

        /// <summary>
        /// Casts bait every 5 minutes
        /// </summary>
        private static void CastBaitTask()
        {
            Console.WriteLine("[Task Manager] -> Bait caster task started");

            Console.WriteLine("[Procedure Manager] -> Bait casted");
            Win32.CastBait(_gameProcess.MainWindowHandle);

            while (true)
            {
                Console.WriteLine("[Procedure Manager] -> Time left until bait ends: " + _baitDuration);

                _baitDuration--;

                if (_baitDuration == 0)
                {
                    Console.WriteLine("[Procedure Manager] -> Bait casted");
                    Win32.CastBait(_gameProcess.MainWindowHandle);

                    _baitDuration = 300;
                }

                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        private static void ListenSplashTask()
        {
            Console.WriteLine("[Thread Manager] -> Splash listener thread started");

            while (true)
            {
                var currentVol = Convert.ToInt32(_session.AudioMeterInformation.MasterPeakValue*100);
                VolumeQueue.Enqueue(currentVol);

                if (VolumeQueue.Count > 5)
                {
                    VolumeQueue.Dequeue();
                }

                if (currentVol - Convert.ToInt32(VolumeQueue.Sum()/VolumeQueue.Count()) >= 13)
                {
                    Console.WriteLine("[Audio Manager] -> Splash sound heard");

                    Console.WriteLine("[Procedure Manager] -> Looting fish");
                    Win32.SendMouseClick(_gameProcess.MainWindowHandle);

                    break;
                }
                _fishingDuration--;
                Console.WriteLine("[Procedure Manager] -> Time left until fishing ends: " + _fishingDuration);

                if (_fishingDuration == 0)
                {
                    Console.WriteLine("[Procedure Manager] -> Fishing ended");
                    break;
                }

                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        private enum ActionStatus
        {
            Idle,
            Casting
        }
    }
}