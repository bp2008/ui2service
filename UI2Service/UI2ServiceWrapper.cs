using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BPUtil;
using UI2Service.Configuration;

namespace UI2Service
{
	public class UI2ServiceWrapper
	{
		public static string Version = "0.1";
		public UI2Server httpServer;
		//UI2WebSocketServer webSocketServer;
		public static UI2ServiceConfig cfg;

		public UI2ServiceWrapper()
		{
			System.Net.ServicePointManager.Expect100Continue = false;
			System.Net.ServicePointManager.DefaultConnectionLimit = 640;
			Logger.logType = LoggingMode.Console | LoggingMode.File;
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

			cfg = new UI2ServiceConfig();
			if (File.Exists(Globals.ConfigFilePath))
				cfg.Load(Globals.ConfigFilePath);
			cfg.Save(Globals.ConfigFilePath);

			BPUtil.SimpleHttp.SimpleHttpLogger.RegisterLogger(Logger.httpLogger);

			//BPUtil.SimpleHttp.GlobalThrottledStream.ThrottlingManager.Initialize(3);
			//BPUtil.SimpleHttp.GlobalThrottledStream.ThrottlingManager.SetBytesPerSecond(0, cfg.options.uploadBytesPerSecond);
			//BPUtil.SimpleHttp.GlobalThrottledStream.ThrottlingManager.SetBytesPerSecond(1, cfg.options.downloadBytesPerSecond);
			//BPUtil.SimpleHttp.GlobalThrottledStream.ThrottlingManager.SetBytesPerSecond(2, -1);
			//BPUtil.SimpleHttp.GlobalThrottledStream.ThrottlingManager.BurstIntervalMs = cfg.options.throttlingGranularity;
		}
		#region Start / Stop
		public void Start()
		{
			Stop();

			httpServer = new UI2Server(cfg.webport, cfg.webport_https, cfg.blueIrisBaseURL);
			httpServer.Start();

			//webSocketServer = new UI2WebSocketServer(cfg.webSocketPort, cfg.webSocketPort_secure);
			//webSocketServer.Start();

			Logger.StartLoggingThreads();
		}
		public void Stop()
		{
			if (httpServer != null)
			{
				httpServer.Stop();
				httpServer.Join(1000);
			}

			//// webSocketServer does not support closing/stopping
			//webSocketServer = null;

			Logger.StopLoggingThreads();
		}
		#endregion

		static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			if (e.ExceptionObject == null)
			{
				Logger.Debug("UNHANDLED EXCEPTION - null exception");
			}
			else
			{
				try
				{
					Logger.Debug((Exception)e.ExceptionObject, "UNHANDLED EXCEPTION");
				}
				catch (Exception ex)
				{
					Logger.Debug(ex, "UNHANDLED EXCEPTION - Unable to report exception of type " + e.ExceptionObject.GetType().ToString());
				}
			}
		}
	}
}
