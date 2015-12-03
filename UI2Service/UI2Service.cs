using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace UI2Service
{
	public partial class UI2Service : ServiceBase
	{
		UI2ServiceWrapper wrapper;

		public UI2Service()
		{
			InitializeComponent();
		}

		protected override void OnStart(string[] args)
		{
			wrapper = new UI2ServiceWrapper();
			wrapper.Start();
		}

		protected override void OnStop()
		{
			wrapper.Stop();
		}
	}
}
