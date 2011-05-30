using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;

namespace Packager
{
	public static class Config
	{
		public static bool Loaded = false;

		public static XmlDocument ConfigurationFile = new XmlDocument();
		public static XmlNode ConfigurationSettings;

		public static bool DebugMode = true;
		public static bool Compress = false;
		public static bool Optimise = false;
		public static string CacheFolder = "/Cache";
		public static string RootFolder = "";

		public static List<Asset> Scripts;
		public static List<Asset> Stylesheets;

		public static void Load()
		{
			Loaded = true;

			var configPath = HttpContext.Current.Server.MapPath("~/Configuration/Packager.config");
			ConfigurationFile.Load(configPath);

			var domain = HttpContext.Current.Request.Url.Host.ToString();

			ConfigurationSettings = ConfigurationFile.SelectSingleNode("/packager/configuration");

			var domainConfig = ConfigurationFile.SelectSingleNode("/packager/configuration[@domain=" + domain + "]");
			if (domainConfig != null) ConfigurationSettings = domainConfig;

			DebugMode = Convert.ToBoolean(ConfigurationSettings.SelectSingleNode("settings/debug").InnerText);
			Compress = Convert.ToBoolean(ConfigurationSettings.SelectSingleNode("settings/compress").InnerText);
			Optimise = Convert.ToBoolean(ConfigurationSettings.SelectSingleNode("settings/optimise").InnerText);

			CacheFolder = Convert.ToString(ConfigurationSettings.SelectSingleNode("settings/cachefolder").InnerText);
			RootFolder = Convert.ToString(ConfigurationSettings.SelectSingleNode("settings/rootfolder").InnerText);

			FetchScripts();
			FetchStylesheets();

			Cached.Scripts = new Dictionary<string, Asset>();
			Cached.Stylesheets = new Dictionary<string, Asset>();
			Cached.VirtualDirectoryPathMap = new Dictionary<string, string>();
		}

		public static void FetchScripts()
		{
			Scripts = new List<Asset>();
			foreach (XmlNode script in ConfigurationSettings.SelectSingleNode("javascript").ChildNodes)
			{
				var root = HttpContext.Current.Server.MapPath("~");
				var folder = script.InnerText;
				var safePath = Utilities.GetSafePath(root, folder);

				foreach (string file in Utilities.GetAllFilesInFolder(safePath))
				{
					var asset = new Asset()
					{
						Path = Utilities.GetOriginalPath(file)
					};
					Utilities.ParseAsset(ref asset);
					Scripts.Add(asset);
				}
			}
		}

		public static void FetchStylesheets()
		{
			Stylesheets = new List<Asset>();
			foreach (XmlNode stylesheet in ConfigurationSettings.SelectSingleNode("css").ChildNodes)
			{
				var root = HttpContext.Current.Server.MapPath("~");
				var folder = stylesheet.InnerText;
				var safePath = Utilities.GetSafePath(root, folder);

				foreach (string file in Utilities.GetAllFilesInFolder(safePath))
				{
					var asset = new Asset()
					{
						Path = Utilities.GetOriginalPath(file)
					};
					Utilities.ParseAsset(ref asset);
					Stylesheets.Add(asset);
				}
			}
		}

		public static void Log()
		{
			var response = HttpContext.Current.Response;

			response.Write("<h2>Current Configuration Settings</h2>");
			response.Write("<ul>");
			response.Write("<li>Debug mode: " + DebugMode + "</li>");
			response.Write("<li>Compress: " + Compress + "</li>");
			response.Write("<li>Optimise: " + Optimise + "</li>");
			response.Write("<li>Root Folder: " + RootFolder + "</li>");
			response.Write("<li>Cache Folder: " + CacheFolder + "</li>");
			response.Write("<li>JavaScript Assets Registered: " + Scripts.Count + "</li>");
			response.Write("<li>Stylesheet Assets Registered: " + Stylesheets.Count + "</li>");
			response.Write("<li>Virtual Directories Paths Mapped: " + Cached.VirtualDirectoryPathMap.Count + "</li>");
			response.Write("</ul>");

			response.Write("<br /><br /><br /><h2>Cached Virtual Directory Maps</h2>");
			response.Write("<ul>");
			foreach (string safepath in Cached.VirtualDirectoryPathMap.Keys)
			{
				response.Write("<li>" + safepath + " => " + Cached.VirtualDirectoryPathMap[safepath] + "</li>");
			}
			response.Write("</ul>");

			response.Write("<br /><br /><br /><h2>Registered JavaScript Packages</h2>");
			foreach (Asset asset in Scripts)
			{
				response.Write("<hr /><h3>" + asset.Path + "</h3>");
				if (asset.Requires.Count > 0)
				{
					response.Write("<h4>Requires</h4><ul>");
					foreach (string requirement in asset.Requires)
					{
						response.Write("<li>" + requirement + "</li>");
					}
					response.Write("</ul>");
				}
				if (asset.Provides.Count > 0)
				{
					response.Write("<h4>Provides</h4><ul>");
					foreach (string provision in asset.Provides)
					{
						response.Write("<li>" + provision + "</li>");
					}
					response.Write("</ul>");
				}
			}

			response.Write("<br /><br /><br /><h2>Registered CSS Packages</h2>");
			foreach (Asset asset in Stylesheets)
			{
				response.Write("<hr /><h3>" + asset.Path + "</h3>");
				if (asset.Requires.Count > 0)
				{
					response.Write("<h4>Requires</h4><ul>");
					foreach (string requirement in asset.Requires)
					{
						response.Write("<li>" + requirement + "</li>");
					}
					response.Write("</ul>");
				}
				if (asset.Provides.Count > 0)
				{
					response.Write("<h4>Provides</h4><ul>");
					foreach (string provision in asset.Provides)
					{
						response.Write("<li>" + provision + "</li>");
					}
					response.Write("</ul>");
				}
			}
		}
	}
}
