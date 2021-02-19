using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace tConfigWrapper.Common {
	//public class PreviouslyLoadedMods {
	//	readonly List<string> _enabledMods;
	//	readonly List<string> _previousPlayerMods;
	//	readonly List<string> _previousWorldMods;

	//	public PreviouslyLoadedMods(List<string> enabledMods, List<string> previousPlayerMods, List<string> previousWorldMods) {
	//		_enabledMods = enabledMods;
	//		_previousPlayerMods = previousPlayerMods;
	//		_previousWorldMods = previousWorldMods;
	//	}
	//}

	public class ModState {
		/// <summary>
		///  List of every enabled mod. ModName is added to the list if it's enabled, and removed if disabled. List is serialized every time a change is made to it.
		/// </summary>
		public static List<string> EnabledMods = new List<string>();
		public static List<string> AllMods = new List<string>();

		public static void EnableMod(string modName) {
			if (EnabledMods.Contains(modName))
				return;
			else {
				EnabledMods.Add(modName);
				SerializeEnabledMods();
			}
		}

		public static void DisableMod(string modName) {
			if (!EnabledMods.Contains(modName))
				return;
			else {
				EnabledMods.Remove(modName);
				SerializeEnabledMods();
			}
		}

		public static void GetAllMods() {
			AllMods = Directory.GetFiles(tConfigWrapper.ModsPath, "*.obj").ToList();
		}

		public static void SerializeEnabledMods() => File.WriteAllText(tConfigWrapper.ModsPath + "\\enabled.json", JsonConvert.SerializeObject(EnabledMods, Formatting.Indented));

		public static void DeserializeEnabledMods() {
			if (File.Exists(tConfigWrapper.ModsPath + "\\enabled.json"))
				 EnabledMods = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(tConfigWrapper.ModsPath + "\\enabled.json"));
			else
				File.Create(tConfigWrapper.ModsPath + "\\enabled.json");
		}
	}

	//public class SaveModsLoadedInWorld : ModWorld {
	//	public static List<string> PreviouslyLoadedMods = new List<string>();

	//	override 
	//}

	//public class SaveModsLoadedInPlayer : ModPlayer {
	//	public static List<string> PreviouslyLoadedMods = new List<string>();

	//	public override void OnEnterWorld(Player player) {
	//		SaveModsLoadedInWorld.PreviouslyLoadedMods = ModState.EnabledMods;
	//	}

	//	public static void SerializeEnabledMods() => File.WriteAllText(tConfigWrapper.ModsPath + "\\enabled.json", );
	//}
}
