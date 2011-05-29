using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Web;
using System.Xml;

namespace Packager
{
    public class PackagerHelper
    {
        public PackagerHelper()
        {
            this.setOptions();

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

        /// <summary>
        /// Reads the Configuration file and setst options and registered Packages.
        /// All options are overridable via the querystring.
        /// </summary>
        private void setOptions()
        {
            var doc = new XmlDocument();
            doc.Load(HttpContext.Current.Server.MapPath("~/Configuration/Packager.config"));

            var request = HttpContext.Current.Request;

            this.Debug = Convert.ToBoolean(request.QueryString["debug"] ?? doc.SelectSingleNode("//debug").InnerText);
            this.Compress = Convert.ToBoolean(request.QueryString["compress"] ?? doc.SelectSingleNode("//compress").InnerText);
            this.Optimise = Convert.ToBoolean(request.QueryString["optimise"] ?? doc.SelectSingleNode("//optimise").InnerText);
            this.CacheFolder = Convert.ToString(request.QueryString["cachefolder"] ?? doc.SelectSingleNode("//cachefolder").InnerText);
            this.Root = Convert.ToString(request.QueryString["root"] ?? doc.SelectSingleNode("//rootfolder").InnerText);

            try
            {
                foreach (XmlNode script in doc.SelectSingleNode("//javascript").ChildNodes)
                {
                    this.JavaScriptPackages.Add(script.InnerText);
                }
            }
            catch { throw new Exception("You haven't registered any JavaScript packages in Packager.config!"); }

            try
            {
                foreach (XmlNode stylesheet in doc.SelectSingleNode("//css").ChildNodes)
                {
                    this.CSSPackages.Add(stylesheet.InnerText);
                }
            }
            catch { throw new Exception("You haven't registered any CSS packages in Packager.config!"); }
        }

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

        /// <summary>
        /// Returns a list of all filenames in a particular folder, and recursively parses all its children.
        /// </summary>
        /// <param name="folder">The (mapped) path to list</param>
        /// <returns>A list of files in the directory</returns>
        public List<string> GetAllFilesInFolder(string folder)
        {
            var folders = new List<string>();
            var stack = new Stack<string>();
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
                catch { throw new Exception("Packager: Could not find directory " + directory + " to parse recursively."); }
            }
            return folders;
        }

        /// <summary>
        /// Creates a hash, used as the file name to cache any one set of assets
        /// </summary>
        /// <param name="input">The content to hash</param>
        /// <returns>An MD5 Hash filename (without the extension)</returns>
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


        #region Currently unused

        public void OptimiseIncludes()
        {
            var includes = this.JavaScriptIncludes;
            var optimisationCache = HttpContext.Current.Server.MapPath(this.CacheFolder + "/optimisation.xml");
        }

        public void updateCSSTotals(List<string> list)
        {
            /*
            string config = HttpContext.Current.Server.MapPath("~" + this.Root + this.CacheFolder + "/Usage.config");

            XmlDocument doc = new XmlDocument();
            doc.Load(config);

            int total = Convert.ToInt32(doc.SelectSingleNode("//css").Attributes["total"].Value);
            doc.SelectSingleNode("//css").Attributes["total"].Value = (total + 1).ToString();

            foreach (string item in list)
            {
                string clean = item.Replace(HttpContext.Current.Server.MapPath("~"), "").Replace("/", "-");
                var node = doc.SelectSingleNode("//css/file[@name='" + clean + "']");
                if (node == null)
                {
                    XmlElement newnode = doc.CreateElement("file");
                    newnode.SetAttribute("name", clean);
                    newnode.InnerText = "1";
                    doc.SelectSingleNode("//css").AppendChild(newnode);
                }
                else
                {
                    int count = Convert.ToInt32(node.InnerText);
                    node.InnerText = (count + 1).ToString();
                }
            }

            doc.Save(config);
            */
        }

        public void updateJavaScriptTotals(List<string> list)
        {
            /*
            string config = HttpContext.Current.Server.MapPath("~" + this.Root + this.CacheFolder + "/Usage.config");

            XmlDocument doc = new XmlDocument();
            doc.Load(config);

            int total = Convert.ToInt32(doc.SelectSingleNode("//javascript").Attributes["total"].Value);
            doc.SelectSingleNode("//javascript").Attributes["total"].Value = (total + 1).ToString();

            foreach (string item in list)
            {
                string clean = item.Replace(HttpContext.Current.Server.MapPath("~"), "").Replace("/", "-");
                var node = doc.SelectSingleNode("//javascript/file[@name='" + clean + "']");
                if (node == null)
                {
                    XmlElement newnode = doc.CreateElement("file");
                    newnode.SetAttribute("name", clean);
                    newnode.InnerText = "1";
                    doc.SelectSingleNode("//javascript").AppendChild(newnode);
                }
                else
                {
                    int count = Convert.ToInt32(node.InnerText);
                    node.InnerText = (count + 1).ToString();
                }
            }

            doc.Save(config);
            */
        }

        #endregion

    }
}