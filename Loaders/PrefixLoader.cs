using Gajatko.IniFiles;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using tConfigWrapper.Common;
using tConfigWrapper.Common.DataTemplates;
using Terraria.ModLoader;

namespace tConfigWrapper.Loaders {
	internal class PrefixLoader : BaseLoader {
		private readonly Dictionary<string, ModPrefix> _prefixesToLoad = new Dictionary<string, ModPrefix>();

		public PrefixLoader(string modName, ConcurrentDictionary<string, MemoryStream> fileStreams) : base(modName, fileStreams) { }

		protected override string TargetFolder => "Prefix";

		protected override void HandleFile(string file) {
			MemoryStream iniStream = fileStreams[file];
			Dictionary<string, string> itemFields = new Dictionary<string, string>();
			Dictionary<string, string> playerFields = new Dictionary<string, string>();

			IniFileReader reader = new IniFileReader(iniStream);
			IniFile iniFile = IniFile.FromStream(reader);

			string internalName = $"{modName}:{Path.GetFileNameWithoutExtension(file).RemoveIllegalCharacters()}";
			bool addToSuffixBag = false;
			string name = null;
			string requirementType = null;
			string weight = null;

			foreach (IniFileSection section in iniFile.sections)
			foreach (IniFileElement element in section.elements) {
				var splitElement = element.Content.Split('=');

				switch (section.Name) {
					case "Stats": {
						switch (splitElement[0]) {
							case "name": {
								name = splitElement[1];
								continue;
							}
							case "suffix" when splitElement[1] == "True": {
								addToSuffixBag = true;
								continue;
							}
							case "weight": {
								weight = splitElement[1];
								continue;
							}
						}

						break;
					}
					case "Requirements": {
						switch (splitElement[0]) {
							case "melee" when splitElement[1] == "True":
							case "ranged" when splitElement[1] == "True":
							case "magic" when splitElement[1] == "True":
							case "accessory" when splitElement[1] == "True": {
								requirementType = splitElement[0];
								continue;
							}
						}

						continue;
					}
					case "Item": {
						itemFields.Add(splitElement[0], splitElement[1]);
						continue;
					}
					case "Player": {
						playerFields.Add(splitElement[0], splitElement[1]);
						continue;
					}
				}
			}

			BasePrefix prefix = new BasePrefix(name, requirementType, float.Parse(weight ?? "1"), itemFields, playerFields);

			_prefixesToLoad.Add(internalName, prefix);
			if (addToSuffixBag) {
				LoadStep.suffixes.Add(prefix);
			}
		}

		public override void RegisterContent() {
			foreach (var (prefixName, modPrefix) in _prefixesToLoad) {
				Mod.AddPrefix(prefixName, modPrefix);
			}
		}
	}
}
