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

            packager.updateCSSTotals(uniques);

            if (packager.Debug)
            {
                string root = HttpContext.Current.Server.MapPath("~");
                foreach (var include in uniques)
                {
                    string path = include.Replace(root, "").Replace(@"\", "/");
                    writer.Write("\n<link href='" + packager.Root + "/" + path + "' type='text/css' rel='stylesheet' media='screen' />");
                }
            }
            else
            {
                // Concatenate
                string content = "";
                uniques.ForEach(filename => content += File.ReadAllText(filename));
                // Compress ?
                if (!string.IsNullOrEmpty(content))
                {
                    if (packager.Compress) content = CssCompressor.Compress(content);
                    // Cache
                    string hash = packager.CreateMD5Hash(content);
                    string target = HttpContext.Current.Server.MapPath("~" + packager.Root + packager.CacheFolder + "/" + hash + ".css");
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

            packager.updateJavaScriptTotals(uniques);

            if (packager.Debug)
            {
                string root = HttpContext.Current.Server.MapPath("~");
                foreach (var include in uniques)
                {
                    string path = include.Replace(root, "").Replace(@"\", "/");
                    writer.Write("\n<script src='" + packager.Root + "/" + path + "' type='text/javascript'></script>");
                }
            }
            else
            {
                // Concatenate
                string content = "";
                // Compress ?
                uniques.ForEach(filename => content += File.ReadAllText(filename));
                if (!string.IsNullOrEmpty(content))
                {
                    if (packager.Compress) content = JavaScriptCompressor.Compress(content);
                    // Cache
                    string hash = packager.CreateMD5Hash(content);
                    string target = HttpContext.Current.Server.MapPath("~" + packager.Root + packager.CacheFolder + "/" + hash + ".js");
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
    }

    #endregion

}