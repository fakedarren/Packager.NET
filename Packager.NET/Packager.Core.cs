using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Yahoo.Yui.Compressor;

namespace Packager
{

	#region Models

	public class Asset
	{
		public string Name;
		public string Description;
		public List<string> Requires = new List<string>();
		public List<string> Provides = new List<string>();
		public string Path;
		public string MappedPath
		{
			get
			{
				return HttpContext.Current.Server.MapPath("~" + this.Path);
			}
		}
	}

	public class CSS : Asset
	{
		public string Href { get; set; }
	}
	public class Script : Asset
	{
		public string Src { get; set; }
	}

	public class StyleSheetCollection : List<CSS> { }
	public class ScriptFileCollection : List<Script> { }

	#endregion


	#region Helper functions

	public class PackagerHelper
	{
		public PackagerHelper()
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(HttpContext.Current.Server.MapPath("~/Configuration/Packager.config"));

			this.Debug = Convert.ToBoolean(doc.SelectSingleNode("//debug").InnerText);
			this.Compress = Convert.ToBoolean(doc.SelectSingleNode("//compress").InnerText);
			this.Optimise = Convert.ToBoolean(doc.SelectSingleNode("//optimise").InnerText);
			this.CacheFolder = doc.SelectSingleNode("//cachefolder").InnerText;
			this.Root = doc.SelectSingleNode("//rootfolder").InnerText;

			foreach (XmlNode script in doc.SelectSingleNode("//javascript").ChildNodes) { this.JavaScriptPackages.Add(script.InnerText); }
			foreach (XmlNode stylesheet in doc.SelectSingleNode("//css").ChildNodes)    { this.CSSPackages.Add(stylesheet.InnerText); }

			Parser parser = new Parser();

			foreach (string javaScriptPackage in this.JavaScriptPackages)
			{
				List<string> scripts = this.GetAllFilesInFolder(HttpContext.Current.Server.MapPath(this.Root + javaScriptPackage));
				scripts.ForEach(script => this.AllScripts.Add(parser.ParseAsset(script)));
			}
			foreach (string cssPackage in this.CSSPackages)
			{
				List<string> stylesheets = this.GetAllFilesInFolder(HttpContext.Current.Server.MapPath(this.Root + cssPackage));
				stylesheets.ForEach(stylesheet => this.AllCSS.Add(parser.ParseAsset(stylesheet)));
			}
		}

		#region Properties

		public bool Debug;
		public bool Compress;
		public bool Optimise;
		public string CacheFolder;
		public string Root;

		public List<string> JavaScriptPackages = new List<string>();
		public List<string> CSSPackages = new List<string>();

		public List<Asset> AllScripts = new List<Asset>();
		public List<Asset> AllCSS = new List<Asset>();

		public List<string> JavaScriptRequirements = new List<string>();
		public List<string> CSSRequirements = new List<string>();

		public Dictionary<string, Asset> JavaScriptIncludes = new Dictionary<string, Asset>();
		public Dictionary<string, Asset> CSSIncludes = new Dictionary<string, Asset>();

		#endregion

		public void AddJavaScript(List<Script> scripts)
		{
			Parser parser = new Parser();
			foreach (var script in scripts)
			{
				var asset = parser.ParseAsset(HttpContext.Current.Server.MapPath("~" + script.Src));
				asset.Requires.ForEach(require => this.JavaScriptRequirements.Add(require));
			}
		}

		public void AddCSS(List<CSS> stylesheets)
		{
			Parser parser = new Parser();
			foreach (var css in stylesheets)
			{
				var asset = parser.ParseAsset(HttpContext.Current.Server.MapPath("~" + css.Href));
				asset.Requires.ForEach(require => this.CSSRequirements.Add(require));
			}
		}

		public void GetJavaScriptRequirements(string requirement)
		{
			foreach (Asset script in this.AllScripts)
			{
				if (script.Provides.Contains(requirement) && !this.JavaScriptIncludes.Keys.Contains(script.Name))
				{
					this.JavaScriptIncludes.Add(script.Name, script);
					script.Requires.ForEach(require => this.GetJavaScriptRequirements(require));
				}
			}
		}

		public void GetCSSRequirements(string requirement)
		{
			foreach (Asset css in this.AllCSS)
			{
				if (css.Provides.Contains(requirement) && !this.CSSIncludes.Keys.Contains(css.Name))
				{
					this.CSSIncludes.Add(css.Name, css);
					css.Requires.ForEach(require => this.GetCSSRequirements(require));
				}
			}
		}

		public List<string> GetAllFilesInFolder(string folder)
		{
			List<string> folders = new List<string>();
			Stack<string> stack = new Stack<string>();
			stack.Push(folder);
			while (stack.Count > 0)
			{
				string directory = stack.Pop();
				try
				{
					folders.AddRange(Directory.GetFiles(directory, "*.*"));
					foreach (string directoryname in Directory.GetDirectories(directory))
					{
						stack.Push(directoryname);
					}
				}
				catch
				{
					throw new Exception("Packager: Could not find directory " + directory + " to parse recursively.");
				}
			}
			return folders;
		}

		public string CreateMD5Hash(string input)
		{
			MD5 md5 = MD5.Create();
			byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
			byte[] hashBytes = md5.ComputeHash(inputBytes);
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < hashBytes.Length; i++)
			{
				sb.Append(hashBytes[i].ToString("X2"));
			}
			return sb.ToString();
		}

		public void OptimiseIncludes()
		{
			var includes = this.JavaScriptIncludes;
			var optimisationCache = HttpContext.Current.Server.MapPath(this.CacheFolder + "/optimisation.xml");
		}
	}

	#endregion


	#region Content Includes

	[ParseChildren(typeof(CSS), DefaultProperty = "CSSFiles", ChildrenAsProperties = true)]
	public class StyleSheets : WebControl
	{
		public StyleSheets()
		{
			this.CSSFiles = new StyleSheetCollection();
		}

		public StyleSheetCollection CSSFiles { get; private set; }

		protected override void OnPreRender(EventArgs e)
		{
			if (this.Page.Master != null && this.Page.Master.FindControl("CSSPlaceholder") != null)
			{
				var holder = (this.Page.Master.FindControl("CSSPlaceholder") as CSSHolder);
				this.CSSFiles.ForEach(stylesheet => holder.stylesheets.Add(stylesheet));
			}
			else if (this.Page.FindControl("CSSPlaceholder") != null)
			{
				var holder = (this.Page.FindControl("CSSPlaceholder") as CSSHolder);
				this.CSSFiles.ForEach(stylesheet => holder.stylesheets.Add(stylesheet));
			}
			else
			{
				throw new Exception("Packager: Could not find placeholder 'CSSPlaceholder' in Page or Master Page");
			}
		}

		protected override void Render(HtmlTextWriter writer)
		{
		}
	}

	[ParseChildren(typeof(Script), DefaultProperty = "ScriptFiles", ChildrenAsProperties = true)]
	public class Scripts : WebControl
	{
		public Scripts()
		{
			this.ScriptFiles = new ScriptFileCollection();
		}

		public ScriptFileCollection ScriptFiles { get; private set; }

		protected override void OnPreRender(EventArgs e)
		{
			if (this.Page.Master != null && this.Page.Master.FindControl("ScriptsPlaceholder") != null)
			{
				var holder = (this.Page.Master.FindControl("ScriptsPlaceholder") as ScriptHolder);
				this.ScriptFiles.ForEach(script => holder.scripts.Add(script));
			}
			else if (this.Page.FindControl("ScriptsPlaceholder") != null)
			{
				var holder = (this.Page.FindControl("ScriptsPlaceholder") as ScriptHolder);
				this.ScriptFiles.ForEach(script => holder.scripts.Add(script));
			}
			else
			{
				throw new Exception("Packager: Could not find placeholder 'ScriptsPlaceholder' in Page or Master Page");
			}
		}

		protected override void Render(HtmlTextWriter writer)
		{
		}
	}

	#endregion


	#region Content PlaceHolders

	public class CSSHolder : HtmlContainerControl
	{
		public List<CSS> stylesheets = new List<CSS>();
		public List<Asset> allAssets = new List<Asset>();

		protected override void Render(HtmlTextWriter writer)
		{
			PackagerHelper packager = new PackagerHelper();
			Parser parser = new Parser();

			Dictionary<string, Asset> includes = new Dictionary<string, Asset>();
			List<string> requires = new List<string>();

			packager.AddCSS(this.stylesheets);
			packager.CSSRequirements.ForEach(requirement => packager.GetCSSRequirements(requirement));

			Sorter sorter = new Sorter(packager.CSSIncludes);

			var uniques = sorter.Sorted;
			foreach (CSS css in this.stylesheets)
			{
				string href = css.Href;
				string mapped = HttpContext.Current.Server.MapPath("~" + href);
				if (!uniques.Contains(mapped)) uniques.Add(mapped);
			}

			if (packager.Debug)
			{
				string root = HttpContext.Current.Server.MapPath("~");
				foreach (var include in uniques)
				{
					string path = include.Replace(root, "").Replace(@"\", "/");
					writer.Write("\n<link href='" + packager.Root + path + "' type='text/css' rel='stylesheet' media='screen' />");
				}
			}
			else
			{
				// Concatenate
				string content = "";
				// Compress ?
				uniques.ForEach(filename => content += File.ReadAllText(filename));
				if (packager.Compress) content = CssCompressor.Compress(content);
				// Cache
				string hash = packager.CreateMD5Hash(content);
				string target = HttpContext.Current.Server.MapPath("~" + packager.CacheFolder + "/" + hash + ".css");
				if (!File.Exists(target))
				{
					TextWriter textwriter = new StreamWriter(target);
					textwriter.Write(content);
					textwriter.Close();
				}
				// Output
				writer.Write("\n<link href='" + packager.Root + packager.CacheFolder + "/" + hash + ".css' type='text/css' rel='stylesheet' media='screen' />");
			}
		}
	}

	public class ScriptHolder : HtmlContainerControl
	{
		public List<Script> scripts = new List<Script>();
		public List<Asset> allAssets = new List<Asset>();

		protected override void Render(HtmlTextWriter writer)
		{
			PackagerHelper packager = new PackagerHelper();
			Parser parser = new Parser();

			Dictionary<string, Asset> includes = new Dictionary<string, Asset>();
			List<string> requires = new List<string>();

			packager.AddJavaScript(this.scripts);
			packager.JavaScriptRequirements.ForEach(requirement => packager.GetJavaScriptRequirements(requirement));

			if (packager.Optimise)
			{
				packager.OptimiseIncludes();
			}

			Sorter sorter = new Sorter(packager.JavaScriptIncludes);

			var uniques = sorter.Sorted;
			foreach (Script script in this.scripts)
			{
				string src = script.Src;
				string mapped = HttpContext.Current.Server.MapPath("~" + src);
				if (!uniques.Contains(mapped)) uniques.Add(mapped);
			}

			if (packager.Debug)
			{
				string root = HttpContext.Current.Server.MapPath("~");
				foreach (var include in uniques)
				{
					string path = include.Replace(root, "").Replace(@"\", "/");
					writer.Write("\n<script src='" + packager.Root + path + "' type='text/javascript'></script>");
				}
			}
			else
			{
				// Concatenate
				string content = "";
				// Compress ?
				uniques.ForEach(filename => content += File.ReadAllText(filename));
				if (packager.Compress) content = JavaScriptCompressor.Compress(content);
				// Cache
				string hash = packager.CreateMD5Hash(content);
				string target = HttpContext.Current.Server.MapPath("~" + packager.CacheFolder + "/" + hash + ".js");
				if (!File.Exists(target))
				{
					TextWriter textwriter = new StreamWriter(target);
					textwriter.Write(content);
					textwriter.Close();
				}
				// Output
				writer.Write("\n<script src='" + packager.Root + packager.CacheFolder + "/" + hash + ".js' type='text/javascript'></script>");
			}
		}
	}

	#endregion

}