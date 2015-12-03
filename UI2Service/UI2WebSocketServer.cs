using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BPUtil;
using Fleck;

namespace UI2Service
{
	public class UI2WebSocketServer
	{
		WebSocketServer ws_insecure;
		WebSocketServer wss_secure;

		public UI2WebSocketServer(int port, int secure_port = -1, System.Security.Cryptography.X509Certificates.X509Certificate2 cert = null)
		{
			FleckLog.logStuff = false;

			if (port > 65535 || port < -1) port = -1;
			if (secure_port > 65535 || secure_port < -1) secure_port = -1;

			if (port > -1)
			{
				ws_insecure = new WebSocketServer("ws://localhost:" + port);
			}

			if (secure_port > -1)
			{
				wss_secure = new WebSocketServer("wss://localhost:" + secure_port);
				wss_secure.Certificate = cert != null ? cert : BPUtil.SimpleHttp.HttpServer.GetSelfSignedCertificate();
			}
		}

		/// <summary>
		/// Starts listening for connections.
		/// </summary>
		public void Start()
		{
			if (ws_insecure != null)
				ws_insecure.Start(handler);
			if (wss_secure != null)
				wss_secure.Start(handler);
		}
		/// <summary>
		/// Handles incoming requests.
		/// </summary>
		/// <param name="socket"></param>
		public void handler(IWebSocketConnection socket)
		{
			socket.OnOpen = () =>
			{
				Console.WriteLine("WebSocket Open!");
			};
			socket.OnClose = () =>
			{
				Console.WriteLine("WebSocket Close!");
			};
			socket.OnMessage = message =>
			{
				try
				{
					Console.WriteLine("WebSocket message: " + message);
				}
				catch (Exception ex)
				{
					Logger.Debug(ex);
				}
			};
		}
	}
}
