using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace UI2Service
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] args)
		{
			if (args.Length == 1 && args[0] == "cmd")
			{
				UI2ServiceWrapper wrapper = new UI2ServiceWrapper();
				wrapper.Start();
				printInstructions(wrapper);
				string line;
				while ((line = Console.ReadLine()) != "exit")
				{
					Console.WriteLine(line);
					printInstructions(wrapper);
				}
				wrapper.Stop();
			}
			else
			{
				Console.WriteLine("Start this program with the argument \"cmd\" to use command-line mode.");
				ServiceBase[] ServicesToRun;
				ServicesToRun = new ServiceBase[] { new UI2Service() };
				ServiceBase.Run(ServicesToRun);
			}
		}
		static void printInstructions(UI2ServiceWrapper wrapper)
		{
			Console.WriteLine("*******************************");
			Console.WriteLine("UI2Service version " + UI2ServiceWrapper.Version);
			Console.WriteLine("*******************************");
			if (UI2ServiceWrapper.cfg.webport < 0 && UI2ServiceWrapper.cfg.webport_https < 0)
				Console.WriteLine("No ports defined. Check config.");
			else
			{
				Console.WriteLine("http port: " + (wrapper.httpServer.Port_http == -1 ? UI2ServiceWrapper.cfg.webport : wrapper.httpServer.Port_http));
				Console.WriteLine("https port: " + (wrapper.httpServer.Port_https == -1 ? UI2ServiceWrapper.cfg.webport_https : wrapper.httpServer.Port_https));
			}
			Console.WriteLine("Type exit to close this program");
		}
	}
}
