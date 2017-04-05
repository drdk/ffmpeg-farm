﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace FFmpegFarm.ConsoleHost
{
    public class Program
    {
        private const string Logo = @"
____ ____ _  _ ___  ____ ____ ____ ____ ____ _  _    _ _ _ ____ ____ _  _ ____ ____ 
|___ |___ |\/| |__] |___ | __ |___ |__| |__/ |\/|    | | | |  | |__/ |_/  |___ |__/ 
|    |    |  | |    |___ |__] |    |  | |  \ |  |    |_|_| |__| |  \ | \_ |___ |  \ 
                                                                                    ";
        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory());
            builder.AddJsonFile("appsettings.json");
            var cfg = builder.Build();
            var env = cfg.GetSection("EnvorimentVars").AsEnumerable().Skip(1).ToDictionary(pair => pair.Key.Replace("EnvorimentVars:",string.Empty), pair=> pair.Value);
            Console.WindowWidth = 100;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(Logo);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Configuration : \n{cfg["FFmpegPath"]}\n{cfg["ControllerApi"]}\n{cfg["threads"]} threads.\n\n");
            Console.WriteLine("Press ctrl+x to exit...\n");
            var exitEvent = new ManualResetEvent(false);
            var cancelSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) => {
                eventArgs.Cancel = true;
                exitEvent.Set();
                cancelSource.Cancel();
            };
            var logger = new ConsoleLogger();
            var threadCount = int.Parse(cfg["threads"]);
            var tasks = new Task[threadCount];
            Worker.Node.PollInterval  = TimeSpan.FromSeconds(10 * threadCount) ;
            for (var x = 0; x < tasks.Length; x++)
            {
                var task = Worker.Node.GetNodeTask(
                    cfg["FFmpegPath"],
                    cfg["ControllerApi"],
                    cfg["FFmpegLogPath"],
                    env,
                    logger,
                    cancelSource.Token);
                tasks[x] = task;
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }
            ConsoleKeyInfo keyInfo;
            while (!cancelSource.IsCancellationRequested &&
                   !(Console.KeyAvailable && (keyInfo = Console.ReadKey(false)).Key == ConsoleKey.X
                     && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control)))
            {
                Thread.Sleep(100);
            }
            if (!cancelSource.IsCancellationRequested)
                cancelSource.Cancel();
            Console.WriteLine("Shutting down....");

            try
            {
                Task.WaitAll(tasks);
            }
            catch (Exception e)
            {
                if (!(e.InnerException?.GetType() == typeof(OperationCanceledException)
                    || e.InnerException?.GetType() == typeof(TaskCanceledException)))
                    throw;
            }
           
            #if DEBUG
            Console.WriteLine("\nShut done completed... Press any key.");
            Console.ReadKey();
            #endif
        }
    }
}
