using System;
using System.Configuration;
using System.Linq;
using System.ServiceProcess;
using System.Timers;
using API.Logging;
using API.Repository;
using API.Service;
using Microsoft.Owin.Hosting;

namespace API.WindowsService
{
    public class APIService : ServiceBase
    {
        private IDisposable _server = null;
        private Timer _timer;
        private Janitor _janitor;

        protected override void OnStart(string[] args)
        {
            Start();
        }

        protected override void OnStop()
        {
            Stop();
            base.OnStop();
        }

        public void Start()
        {
            CheckHttpsSettings();

            var uris = ConfigurationManager.AppSettings["ListenAddress"].Split(';');
            var startOptions = new StartOptions();
            foreach (var uri in uris)
                startOptions.Urls.Add(uri);
            _server = WebApp.Start<Startup>(startOptions);

            _timer = new Timer(TimeSpan.FromDays(1).TotalMilliseconds) { AutoReset = true, Enabled = true };
            _janitor = new Janitor(new Helper(), new NLogWrapper(ConfigurationManager.AppSettings["NLog-Appname"] ?? System.Reflection.Assembly.GetExecutingAssembly().FullName));
            _timer.Elapsed += (sender, args) =>
            {
                if (!System.Threading.Monitor.TryEnter(_janitor)) return;
                _janitor.CleanUp();
                System.Threading.Monitor.Exit(_janitor);
            };
        }

        public new void Stop()
        {
            _timer?.Stop();
            _timer?.Dispose();
            _server?.Dispose();
        }

        /// <summary>
        /// Check if ForceHttpsOnGet contradicts ListenAddress in config.
        /// </summary>
        private static void CheckHttpsSettings()
        {
            var endpointUrls = ConfigurationManager.AppSettings["ListenAddress"].Split(';');
            if (bool.TryParse(ConfigurationManager.AppSettings["ForceHttpsOnGet"], out var forceForceHttps) && forceForceHttps)
            {
                var endpointUrl = endpointUrls.FirstOrDefault(u => u.Contains("https"));
                if (string.IsNullOrEmpty(endpointUrl))
                    throw new ConfigurationErrorsException($"Error in configuration. ForceHttpsOnGet is true but https is not configured in ListenAddress.");
            }
        }
    }
}