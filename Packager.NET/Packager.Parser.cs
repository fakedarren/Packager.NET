using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using Yaml.Grammar;

namespace Packager
{

	public class Parser
	{
		public Asset ParseAsset(string file)
		{
			Asset asset = new Asset();
			asset.Name = file;
			string yaml = this.FetchYAMLFromFile(file);
			
			YamlParser parser = new YamlParser();
			bool success;
			YamlStream yamlStream = parser.ParseYamlStream(new TextInput(yaml), out success);

			foreach (YamlDocument doc in yamlStream.Documents)
			{
				Mapping map = (doc.Root as Mapping);
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
			return asset;
		}

		public string FetchYAMLFromFile(string file)
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

	}

}
