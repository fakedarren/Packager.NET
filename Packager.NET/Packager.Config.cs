﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;

namespace Packager
{
    public static class Config
    {
        public static XmlDocument ConfigurationFile = new XmlDocument();
		public static XmlNode ConfigurationSettings;

		public static bool DebugMode;
		public static bool Compress;
		public static bool Optimise;
		public static string CacheFolder = "/Cache";
		public static string RootFolder = "";

		public static List<Asset> Stylesheets = new List<Asset>();
		public static List<Asset> Scripts = new List<Asset>();

		static void Config()
		{
			Load();
		}

		public static void Load()
		{
			var configPath = HttpContext.Current.Server.MapPath("~/Configuration/Packager.config");
			ConfigurationFile.Load(configPath);

			var domain = AppDomain.CurrentDomain;

			ConfigurationSettings = ConfigurationFile.SelectSingleNode("//configuration[@domain=" + domain + "]");
			if (ConfigurationSettings == null) ConfigurationSettings = ConfigurationFile.SelectSingleNode("//configuration");

			DebugMode = Convert.ToBoolean(ConfigurationSettings.SelectSingleNode("//debug").InnerText);
			Compress = Convert.ToBoolean(ConfigurationSettings.SelectSingleNode("//compress").InnerText);
			Optimise = Convert.ToBoolean(ConfigurationSettings.SelectSingleNode("//optimise").InnerText);

			CacheFolder = Convert.ToString(ConfigurationSettings.SelectSingleNode("//cachefolder").InnerText);
			RootFolder = Convert.ToString(ConfigurationSettings.SelectSingleNode("//rootfolder").InnerText);
		}
	}
}
