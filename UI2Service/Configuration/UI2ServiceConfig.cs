using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI2Service.Configuration
{
	public class UI2ServiceConfig : SerializableObjectBase
	{
		//public int webSocketPort = 44440;
		//public int webSocketPort_secure = 44441;
		public int webport = 44442;
		public int webport_https = 44443;
		public string blueIrisBaseURL = "http://localhost:80/";
		public bool logWebRequestsToFile = false;
	}
}
