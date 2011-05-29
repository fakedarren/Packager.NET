using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Web;
using System.Xml;
using Yaml.Grammar;

namespace Packager
{
	public static class Utilities
	{
        /// <summary>
        /// Returns a list of all filenames in a particular folder, and recursively parses all its children.
        /// </summary>
        /// <param name="folder">The (mapped) path to list</param>
        /// <returns>A list of files in the directory</returns>
        public static List<string> GetAllFilesInFolder(string folder)
        {
            var files = new List<string>();
            var stack = new Stack<string>();
            stack.Push(folder);
            while (stack.Count > 0)
            {
                string directory = stack.Pop();
                try
                {
					files.AddRange(Directory.GetFiles(directory, "*.*"));
                    foreach (string directoryname in Directory.GetDirectories(directory))
                    {
                        stack.Push(directoryname);
                    }
                }
                catch { throw new Exception("Packager: Could not find directory " + directory + " to parse recursively."); }
            }
			return files;
        }

        /// <summary>
        /// Creates a hash, used as the file name to cache any one set of assets
        /// </summary>
        /// <param name="input">The content to hash</param>
        /// <returns>An MD5 Hash filename (without the extension)</returns>
		public static string CreateMD5Hash(string input)
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

		public static void ParseAsset(ref Asset asset)
		{
			var yaml = Utilities.FetchYAMLFromFile(asset.MappedPath);

			var parser = new YamlParser();
			bool success;
			YamlStream yamlStream = parser.ParseYamlStream(new TextInput(yaml), out success);

			foreach (YamlDocument doc in yamlStream.Documents)
			{
				Mapping map = (doc.Root as Mapping);
				if (map == null)
				{
					throw new Exception("Packager: Failed to resolve dependencies for " + asset.MappedPath);
				}
				else
				{
					foreach (MappingEntry entry in map.Enties)
					{
						if (entry.Value is Sequence)
						{
							foreach (DataItem item in (entry.Value as Sequence).Enties)
							{
								if (entry.Key.ToString() == "requires")
								{
									string value = item.ToString();
									var cleanItem = value.LastIndexOf('/') == -1 ? value : value.Substring(value.LastIndexOf('/') + 1);
									asset.Requires.Add(cleanItem);
								}
								else if (entry.Key.ToString() == "provides")
								{
									string value = item.ToString();
									var cleanItem = value.LastIndexOf('/') == -1 ? value : value.Substring(value.LastIndexOf('/') + 1);
									asset.Provides.Add(cleanItem);
								}
							}
						}
					}
				}
			}
		}

		public static string FetchYAMLFromFile(string file)
		{
			string yaml = "";
			if (File.Exists(file))
			{
				StreamReader stream = new StreamReader(file);
				string line;
				bool started = false;
				while ((line = stream.ReadLine()) != null)
				{
					if (line.StartsWith("...")) started = false;
					if (started)
					{
						if ((line.Contains("requires: ") || line.Contains("provides: ")) && !line.Contains("["))
						{
							int breakpoint = line.IndexOf(":") + 1;
							yaml += (line.Substring(0, breakpoint) + "\r\n");
							yaml += ("  - " + line.Substring(breakpoint + 1) + "\r\n");
						}
						else
						{
							yaml += (line + "\r\n");
						}
					}
					if (line.StartsWith("---")) started = true;
				}
				stream.Close();
			}
			else
			{
				throw new Exception("Packager.NET failed to find file '" + file + "'");
			}
			return yaml;
		}

		public static void GetAllDependencies(ref Dictionary<string, Asset> assets, string type)
		{
			var newAssets = new Dictionary<string, Asset>();
			foreach (Asset asset in assets.Values)
			{
				foreach (string requirement in asset.Requires)
				{
					if (!assets.ContainsKey(requirement))
					{
						if (type == "CSS")
						{
							foreach (Asset stylesheet in Config.Stylesheets)
							{
								if (stylesheet.Provides.Contains(requirement) && !newAssets.ContainsKey(stylesheet.Path))
								{
									newAssets.Add(stylesheet.Path, stylesheet);
								}
							}
						}
						if (type == "Script")
						{
							foreach (Asset script in Config.Scripts)
							{
								if (script.Provides.Contains(requirement) && !newAssets.ContainsKey(script.Path))
								{
									newAssets.Add(script.Path, script);
								}
							}
						}
					}
				}
			}
			bool recurse = false;
			foreach (string newAsset in newAssets.Keys)
			{
				if (!assets.ContainsKey(newAsset))
				{
					recurse = true;
					assets.Add(newAsset, newAssets[newAsset]);
				}
			}
			if (recurse == true) GetAllDependencies(ref assets, type);
		}
    }
}