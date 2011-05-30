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

		private string uncompressedContent = String.Empty;
		public string UncompressedContent
		{
			get
			{
				if (uncompressedContent == String.Empty)
				{
					uncompressedContent = File.ReadAllText(MappedPath);
				}
				return uncompressedContent;
			}
		}

		private string compressedContent = String.Empty;
		public string CompressedContent
		{
			get
			{
				if (compressedContent == String.Empty)
				{
					var content = File.ReadAllText(MappedPath);
					if (MappedPath.ToLower().EndsWith(".css"))
					{
						try { compressedContent = CssCompressor.Compress(content); }
						catch { compressedContent = content; }
					}
					else if (MappedPath.ToLower().EndsWith(".js"))
					{
						try { compressedContent = JavaScriptCompressor.Compress(content); }
						catch { compressedContent = content; }
					}
				}
				return compressedContent;
			}
		}
	}

    public class CSS : Asset
    {
        public string Href { get; set; }
		public new string Path
		{
			get
			{
				return Href;
			}
		}
		public new string MappedPath
		{
			get
			{
				return HttpContext.Current.Server.MapPath("~" + this.Href);
			}
		}

		private string compressedContent = String.Empty;
		public new string CompressedContent
		{
			get
			{
				if (compressedContent == String.Empty)
				{
					var content = File.ReadAllText(MappedPath);
					try { compressedContent = CssCompressor.Compress(content); }
					catch { compressedContent = content; }
				}
				return compressedContent;
			}
		}
	}
    public class Script : Asset
    {
        public string Src { get; set; }
		public new string Path
		{
			get
			{
				return Src;
			}
		}
		public new string MappedPath
		{
			get
			{
				return HttpContext.Current.Server.MapPath("~" + this.Src);
			}
		}

		private string compressedContent = String.Empty;
		public new string CompressedContent
		{
			get
			{
				if (compressedContent == String.Empty)
				{
					var content = File.ReadAllText(MappedPath);
					try { compressedContent = JavaScriptCompressor.Compress(content); }
					catch { compressedContent = content; }
				}
				return compressedContent;
			}
		}
	}

    public class StyleSheetCollection : List<CSS> { }
    public class ScriptFileCollection : List<Script> { }

    #endregion


	#region Content Caches

	public static class Cached
	{
		public static Dictionary<string, Asset> Stylesheets = new Dictionary<string,Asset>();
		public static Dictionary<string, Asset> Scripts = new Dictionary<string,Asset>();
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
		public Dictionary<string, Asset> allStylesheets = new Dictionary<string, Asset>();

        protected override void Render(HtmlTextWriter writer)
        {
			if (!Config.Loaded || HttpContext.Current.Request["clearcache"] != null) Config.Load();

			foreach (CSS stylesheet in stylesheets)
			{
				var asset = new Asset()
				{
					Path = stylesheet.Path
				};
				Utilities.ParseAsset(ref asset);
				if (!allStylesheets.ContainsKey(asset.Path))
				{
					allStylesheets.Add(asset.Path, asset);
				}
			}

			Utilities.GetAllDependencies(ref allStylesheets, "CSS");

			var sorter = new Sorter(allStylesheets);

			bool forcedDebug = false;
			if (HttpContext.Current.Request["debug"] != null && HttpContext.Current.Request["debug"] == "true") forcedDebug = true;

			if (Config.DebugMode == true || forcedDebug == true)
			{
				foreach (string path in sorter.Sorted)
				{
					writer.Write("\n<link href='" + Config.RootFolder + path + "' type='text/css' rel='stylesheet' media='screen' />");
				}
			}
			else
			{
				string output = "";

				foreach (string path in sorter.Sorted)
				{
					if (!Cached.Stylesheets.ContainsKey(path))
					{
						Cached.Stylesheets.Add(path, allStylesheets[path]);
					}
					var cachedAsset = Cached.Stylesheets[path];
					output += (Config.Compress == true ? cachedAsset.CompressedContent : cachedAsset.UncompressedContent) + "\r\n";
				}

				if (output.Length > 0)
				{
					string hash = Utilities.CreateMD5Hash(output);
					string target = HttpContext.Current.Server.MapPath("/" + Config.RootFolder + Config.CacheFolder + "/" + hash + ".css");
					if (!File.Exists(target))
					{
						TextWriter textwriter = new StreamWriter(target);
						textwriter.Write(output);
						textwriter.Close();
					}

					writer.Write("\n<link href='" + Config.RootFolder + Config.CacheFolder + "/" + hash + ".css' type='text/css' rel='stylesheet' media='screen' />");
				}
			}
        }
    }

    public class ScriptHolder : HtmlContainerControl
    {
        public List<Script> scripts = new List<Script>();
		public Dictionary<string, Asset> allScripts = new Dictionary<string, Asset>();

        protected override void Render(HtmlTextWriter writer)
        {
			if (!Config.Loaded || HttpContext.Current.Request["clearcache"] != null) Config.Load();

			foreach (Script script in scripts)
			{
				var asset = new Asset()
				{
					Path = script.Path
				};
				Utilities.ParseAsset(ref asset);
				if (!allScripts.ContainsKey(asset.Path))
				{
					allScripts.Add(asset.Path, asset);
				}
			}

			Utilities.GetAllDependencies(ref allScripts, "Script");

			var sorter = new Sorter(allScripts);

			bool forcedDebug = false;
			if (HttpContext.Current.Request["debug"] != null && HttpContext.Current.Request["debug"] == "true") forcedDebug = true;

			if (Config.DebugMode == true || forcedDebug == true)
			{
				foreach (string path in sorter.Sorted)
				{
					writer.Write("\n<script src='" + Config.RootFolder + path + "' type='text/javascript'></script>");
				}
			}
			else
			{
				string output = "";

				foreach (string path in sorter.Sorted)
				{
					if (!Cached.Scripts.ContainsKey(path))
					{
						Cached.Scripts.Add(path, allScripts[path]);
					}

					var cachedAsset = Cached.Scripts[path];
					output += (Config.Compress == true ? cachedAsset.CompressedContent : cachedAsset.UncompressedContent) + "\r\n";
				}

				if (output.Length > 0)
				{
					string hash = Utilities.CreateMD5Hash(output);
					string target = HttpContext.Current.Server.MapPath("/" + Config.RootFolder + Config.CacheFolder + "/" + hash + ".js");
					if (!File.Exists(target))
					{
						TextWriter textwriter = new StreamWriter(target);
						textwriter.Write(output);
						textwriter.Close();
					}

					writer.Write("\n<script src='" + Config.RootFolder + Config.CacheFolder + "/" + hash + ".js' type='text/javascript'></script>");
				}
			}
        }
    }

    #endregion

}