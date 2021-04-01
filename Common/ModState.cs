using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.World.Generation;

namespace tConfigWrapper.Common {
	public class ModState {
		public static List<string> AllMods = new List<string>();
		/// <summary>
		///  List of every enabled mod. ModName is added to the list if it's enabled, and removed if disabled. List is serialized every time a change is made to it.
		/// </summary>
		public static List<string> EnabledMods = new List<string>();
		public static List<string> EnabledModsOld;
		public static List<string> ChangedMods = new List<string>();

		public static void EnableMod(string modName) {
			if (EnabledMods.Contains(modName))
				return;

			EnabledMods.Add(modName);
			SerializeEnabledMods();
		}

		public static void DisableMod(string modName) {
			if (!EnabledMods.Contains(modName))
				return;

			EnabledMods.Remove(modName);
			SerializeEnabledMods();
		}

		public static void ToggleMod(string modName) {
			if (EnabledMods.Contains(modName))
				DisableMod(modName);
			else
				EnableMod(modName);

			if (EnabledMods.Contains(modName) != EnabledModsOld.Contains(modName))
				ChangedMods.Add(modName);
			else
				ChangedMods.Remove(modName);
		}

		public static void GetAllMods() => AllMods = Directory.GetFiles(tConfigWrapper.ModsPath, "*.obj").ToList();

		public static void SerializeEnabledMods() => File.WriteAllText(tConfigWrapper.ModsPath + "\\enabled.json", JsonConvert.SerializeObject(EnabledMods, Formatting.Indented));

		public static void SerializeModPack(string name) => File.WriteAllText(tConfigWrapper.ModsPath + $"\\ModPacks\\{name}.json", JsonConvert.SerializeObject(EnabledMods, Formatting.Indented));

		public static void DeserializeEnabledMods() {
			if (File.Exists(tConfigWrapper.ModsPath + "\\enabled.json")) {
				EnabledMods = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(tConfigWrapper.ModsPath + "\\enabled.json"));
				EnabledModsOld = new List<string>(EnabledMods);
			}
			else
				EnabledModsOld = new List<string>();
		}

		public static void DeserializeModPack(string path) {
			EnabledMods = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(tConfigWrapper.ModsPath + $"\\{path}"));
		}

		public static void LoadStaticFields() {
			AllMods = new List<string>();
			EnabledMods = new List<string>();
			ChangedMods = new List<string>();
		}

		public static void UnloadStaticFields() {
			AllMods = null;
			EnabledMods = null;
			EnabledModsOld = null;
			ChangedMods = null;
		}
	}

	public class SaveLoadedMods : ModPlayer {
		public List<string> usedtConfigMods = new List<string>();
		public override void Initialize() {
			usedtConfigMods = new List<string>();
		}

		public override void Load(TagCompound tag) {
			usedtConfigMods = (List<string>)tag.GetList<string>("usedtConfigMods");
		}

		public override TagCompound Save() {
			return new TagCompound {
				{ "usedtConfigMods", usedtConfigMods }
			};
		}
	}
}
