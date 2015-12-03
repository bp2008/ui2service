using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using BPUtil;
using BPUtil.SimpleHttp;

namespace UI2Service
{
	public class UI2Server : HttpServer
	{
		private ConcurrentDictionary<string, JpegDiffVideoEncoder> dictStreamIdToEncoder = new ConcurrentDictionary<string, JpegDiffVideoEncoder>();
		string BlueIrisBaseURL = "";
		public UI2Server(int port, int port_https, string BlueIrisBaseURL)
			: base(port, port_https)
		{
			this.BlueIrisBaseURL = BlueIrisBaseURL.TrimEnd('/') + "/";
			if (!this.BlueIrisBaseURL.StartsWith("https://") && !this.BlueIrisBaseURL.StartsWith("http://"))
				this.BlueIrisBaseURL = "http://" + this.BlueIrisBaseURL;
		}
		public override bool shouldLogRequestsToFile()
		{
			return UI2ServiceWrapper.cfg.logWebRequestsToFile;
		}
		public override void handleGETRequest(HttpProcessor p)
		{
			try
			{
				string requestedPage = p.request_url.AbsolutePath.TrimStart('/');
				string streamid = p.GetParam("streamid");
				if (requestedPage == "about")
				{
					p.writeSuccess();
					p.outputStream.Write("<div>UI2Service version " + UI2ServiceWrapper.Version + "</div>");
					p.outputStream.Write("<div>Thread pool min threads: " + this.pool.MinThreads + "</div>");
					p.outputStream.Write("<div>Thread pool max threads: " + this.pool.MaxThreads + "</div>");
					p.outputStream.Write("<div>Thread pool live threads: " + this.pool.CurrentLiveThreads + "</div>");
					p.outputStream.Write("<div>Thread pool busy threads: " + this.pool.CurrentBusyThreads + "</div>");
				}
				else if (requestedPage == "jpegdiffversions")
				{
					p.writeSuccess("text/plain");
					p.outputStream.Write(string.Join("|", JpegDiffVideoEncoder.Versions));
				}
				else if (requestedPage.StartsWith("image/") && !string.IsNullOrEmpty(streamid) && streamid.Length < 256)
				{
					bool startNewStream = p.GetBoolParam("startNewStream");
					int quality = p.GetIntParam("jdq", 80);
					int version = p.GetIntParam("jdv", 1);
					JpegDiffVideoEncoder encoder = dictStreamIdToEncoder.GetOrAdd(streamid, p_streamId =>
					{
						startNewStream = false;
						return new JpegDiffVideoEncoder();
					});
					byte[] jpegData = DownloadBytes(p, BlueIrisBaseURL + p.request_url.PathAndQuery.TrimStart('/'));
					if (jpegData == null || jpegData.Length == 0)
					{
						p.writeFailure("500");
						return;
					}
					int outputSizeBytes = 0;
					byte[] retVal;
					if (startNewStream)
						encoder = GetNewEncoder(streamid);
					try
					{
						encoder.CompressionQuality = quality;
						retVal = encoder.EncodeFrame(jpegData, jpegData.Length, out outputSizeBytes, version);
					}
					catch (JpegDiffVideoException)
					{
						Console.WriteLine("Frame resized.");
						encoder = GetNewEncoder(streamid);
						try
						{
							encoder.CompressionQuality = quality;
							retVal = encoder.EncodeFrame(jpegData, jpegData.Length, out outputSizeBytes, version);
						}
						catch (Exception ex)
						{
							Console.WriteLine("Eaten EncodeFrame exception: " + ex.ToString()); // This happens sometimes when decoding the jpeg header.  Not sure why.
							p.writeFailure("500");
							return;
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine("Eaten EncodeFrame exception: " + ex.ToString()); // This happens sometimes when decoding the jpeg header.  Not sure why.
						p.writeFailure("500");
						return;
					}
					p.writeSuccess("image/jpeg", outputSizeBytes);
					p.outputStream.Flush();
					p.rawOutputStream.Write(retVal, 0, outputSizeBytes);
				}
				else
				{
					ProxyRequestTo(p, BlueIrisBaseURL + p.request_url.PathAndQuery.TrimStart('/'));
				}
			}
			catch (Exception ex)
			{
				if (!p.isOrdinaryDisconnectException(ex))
					Logger.Debug(ex);
			}
		}
		private JpegDiffVideoEncoder GetNewEncoder(string streamid)
		{
			return dictStreamIdToEncoder.AddOrUpdate(streamid, p_streamId => new JpegDiffVideoEncoder(), (p_streamId, p_oldEncoder) =>
						{
							p_oldEncoder.Dispose();
							return new JpegDiffVideoEncoder();
						});
		}
		public override void handlePOSTRequest(HttpProcessor p, StreamReader inputData)
		{
			try
			{
				string requestedPage = p.request_url.AbsolutePath.TrimStart('/');
				if (!p.postContentType.Contains("application/x-www-form-urlencoded"))
				{
					Logger.Debug("Unsupported post content type: " + p.postContentType);
					return;
				}
				ProxyRequestTo(p, BlueIrisBaseURL + p.request_url.PathAndQuery.TrimStart('/'), "POST");
			}
			catch (Exception ex)
			{
				if (!p.isOrdinaryDisconnectException(ex))
					Logger.Debug(ex);
			}
		}

		private byte[] DownloadBytes(HttpProcessor p, string url, string method = "GET")
		{
			Uri uri = new Uri(url);
			HttpWebRequest request = WebRequest.CreateHttp(uri);

			if (method == "GET")
				request.Method = method;
			else if (method == "POST")
				request.Method = method;
			else
				throw new Exception("Unsupported HTTP request method: " + method);

			request.CookieContainer = new CookieContainer();
			foreach (BPUtil.SimpleHttp.Cookie cookie in p.requestCookies)
				request.CookieContainer.Add(new System.Net.Cookie(cookie.name, cookie.value, "/", uri.Host));

			if (method == "POST")
			{
				request.ContentType = p.postContentType;
				byte[] data = UTF8Encoding.UTF8.GetBytes(p.postFormDataRaw);
				request.ContentLength = data.Length;
				Stream requestStream = request.GetRequestStream();
				requestStream.Write(data, 0, data.Length);
			}

			HttpWebResponse response = null;
			int statusCode;
			string statusDescription;
			bool success = false;
			try
			{
				response = (HttpWebResponse)request.GetResponse();
				statusCode = ((int)response.StatusCode);
				statusDescription = response.StatusDescription;
				success = true;
			}
			catch (WebException ex)
			{
				statusCode = (int)((HttpWebResponse)ex.Response).StatusCode;
				statusDescription = ((HttpWebResponse)ex.Response).StatusDescription;
			}
			if (success)
			{
				using (MemoryStream ms = new MemoryStream())
				{
					response.GetResponseStream().CopyTo(ms);
					return ms.ToArray();
				}
			}
			return new byte[0];
		}

		private void ProxyRequestTo(HttpProcessor p, string url, string method = "GET")
		{
			Uri uri = new Uri(url);
			HttpWebRequest request = WebRequest.CreateHttp(uri);

			if (method == "GET")
				request.Method = method;
			else if (method == "POST")
				request.Method = method;
			else
				throw new Exception("Unsupported HTTP request method: " + method);

			request.CookieContainer = new CookieContainer();
			foreach (BPUtil.SimpleHttp.Cookie cookie in p.requestCookies)
				request.CookieContainer.Add(new System.Net.Cookie(cookie.name, cookie.value, "/", uri.Host));

			if (method == "POST")
			{
				request.ContentType = p.postContentType;
				byte[] data = UTF8Encoding.UTF8.GetBytes(p.postFormDataRaw);
				request.ContentLength = data.Length;
				Stream requestStream = request.GetRequestStream();
				requestStream.Write(data, 0, data.Length);
			}

			HttpWebResponse response = null;
			int statusCode;
			string statusDescription;
			bool success = false;
			try
			{
				response = (HttpWebResponse)request.GetResponse();
				statusCode = ((int)response.StatusCode);
				statusDescription = response.StatusDescription;
				success = true;
			}
			catch (WebException ex)
			{
				statusCode = (int)((HttpWebResponse)ex.Response).StatusCode;
				statusDescription = ((HttpWebResponse)ex.Response).StatusDescription;
			}
			if (success)
			{
				foreach (System.Net.Cookie cookie in response.Cookies)
					p.responseCookies.Add(cookie.Name, cookie.Value, DateTime.Now - cookie.Expires);
				p.writeSuccess(response.ContentType, response.ContentLength, statusCode.ToString());
				p.outputStream.Flush();
				response.GetResponseStream().CopyTo(p.rawOutputStream);
			}
			else
			{
				p.writeFailure(statusCode.ToString(), statusDescription);
			}
		}

		public override void stopServer()
		{
		}
	}
}
