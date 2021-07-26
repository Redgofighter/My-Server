using System;
using System.Threading;

namespace VoxelLegacyGameServer
{
    class Program
    {
        public static bool isRunning = false;
        public static String ServerVersion = "v0.0.01";
        public static String SupportedGameVersion = "v0.0.01";

        static void Main(string[] args)
        {
            Console.Title = "Voxel Legacy Game Server";
            Console.WriteLine($"Server Version {ServerVersion} Game Version Supported {SupportedGameVersion}");
            isRunning = true;

            Thread mainThread = new Thread(new ThreadStart(MainThread));
            mainThread.Start();

            Server.Start(24, 2600);//Find an unused port on wiki for release(List of TCP and UDP port numbers)
        }

        private static void MainThread()
        {
            Console.WriteLine($"Main thread started. Running at {Constants.TICKS_PER_SEC} ticks per second.");
            DateTime _nextLoop = DateTime.Now;

            while (isRunning)
            {
                while (_nextLoop < DateTime.Now)
                {
                    GameLogic.Update();

                    _nextLoop = _nextLoop.AddMilliseconds(Constants.MS_PER_TICK);
                    if (_nextLoop > DateTime.Now)
                    {
                        Thread.Sleep(_nextLoop - DateTime.Now);//Thread to sleep
                    }
                }
            }
        }
    }
}
