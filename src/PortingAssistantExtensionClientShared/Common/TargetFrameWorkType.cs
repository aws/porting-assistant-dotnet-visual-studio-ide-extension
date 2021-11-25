using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistantVSExtensionClient.Common
{
	public static class TargetFrameworkType
	{
		public const string NO_SELECTION = "";
		public const string NETCOREAPP31 = "netcoreapp3.1";
		public const string NET50 = "net5.0";
		public const string NET60 = "net6.0";
		public static readonly List<string> ALL_SElECTION = new List<string>
			 {
				 NETCOREAPP31,
				 NET50,
				 NET60
			 };
	}
}
