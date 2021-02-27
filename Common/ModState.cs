using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.World.Generation;

namespace tConfigWrapper.Common {
	public class PrevMods {
		public HashSet<string> PrevEnabledMods;
	}

	public class ModState {
		public static List<string> AllMods = new List<string>();
		/// <summary>
		///  List of every enabled mod. ModName is added to the list if it's enabled, and removed if disabled. List is serialized every time a change is made to it.
		/// </summary>
		public static List<string> EnabledMods = new List<string>();
		public static List<string> EnabledModsOld;
		public static List<string> ChangedMods = new List<string>();
		public static Dictionary<string, PrevMods> PrevLoadedPlayerMods = new Dictionary<string, PrevMods>();
		public static Dictionary<string, PrevMods> PrevLoadedWorldMods = new Dictionary<string, PrevMods>();

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

		public static void GetAllMods() {
			AllMods = Directory.GetFiles(tConfigWrapper.ModsPath, "*.obj").ToList();
		}

		public static void SerializeEnabledMods() => File.WriteAllText(tConfigWrapper.ModsPath + "\\enabled.json", JsonConvert.SerializeObject(EnabledMods, Formatting.Indented));

		public static void SerializePreviousPlayerMods() => File.WriteAllText(tConfigWrapper.ModsPath + "\\prevPlayerMods.json", JsonConvert.SerializeObject(PrevLoadedPlayerMods, Formatting.Indented));

		public static void SerializePreviousWorldMods() => File.WriteAllText(tConfigWrapper.ModsPath + "\\prevWorldMods.json", JsonConvert.SerializeObject(PrevLoadedWorldMods, Formatting.Indented));

		public static void SerializeModPack(string name) => File.WriteAllText(tConfigWrapper.ModsPath + $"\\ModPacks\\{name}.json", JsonConvert.SerializeObject(EnabledMods, Formatting.Indented));

		public static void DeserializeEnabledMods() {
			if (File.Exists(tConfigWrapper.ModsPath + "\\enabled.json")) {
				EnabledMods = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(tConfigWrapper.ModsPath + "\\enabled.json"));
				EnabledModsOld = new List<string>(EnabledMods);
			}
		}

		public static void DeserializePrevPlayerMods() {
			if (File.Exists(tConfigWrapper.ModsPath + "\\prevPlayerMods.json"))
				PrevLoadedPlayerMods = JsonConvert.DeserializeObject<Dictionary<string, PrevMods>>(File.ReadAllText(tConfigWrapper.ModsPath + "\\prevPlayerMods.json"));
		}

		public static void DeserializePrevWorldMods() {
			if (File.Exists(tConfigWrapper.ModsPath + "\\prevWorldMods.json"))
				PrevLoadedWorldMods = JsonConvert.DeserializeObject<Dictionary<string, PrevMods>>(File.ReadAllText(tConfigWrapper.ModsPath + "\\prevWorldMods.json"));
		}

		public static void DeserializeModPack(string path) {
			EnabledMods = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(tConfigWrapper.ModsPath + $"\\{path}"));
		}

		public static void LoadStaticFields() {
			AllMods = new List<string>();
			EnabledMods = new List<string>();
			ChangedMods = new List<string>();
			PrevLoadedPlayerMods = new Dictionary<string, PrevMods>();
			PrevLoadedWorldMods = new Dictionary<string, PrevMods>();
		}

		public static void UnloadStaticFields() {
			AllMods = null;
			EnabledMods = null;
			EnabledModsOld = null;
			ChangedMods = null;
			PrevLoadedPlayerMods = null;
			PrevLoadedWorldMods = null;
		}
	}

	public class SaveLoadedMods : ModPlayer {
		public override void SetupStartInventory(IList<Item> items, bool mediumcoreDeath) { // Runs on player creation, so that mods are serialized even if you don't enter the world.
			if (ModState.PrevLoadedPlayerMods.ContainsKey(player.name))
				ModState.PrevLoadedPlayerMods.Remove(player.name);

			ModState.PrevLoadedPlayerMods.Add(player.name, new PrevMods {
				PrevEnabledMods = new HashSet<string>(ModState.EnabledMods)
			});

			ModState.SerializePreviousPlayerMods();
		}

		public override void OnEnterWorld(Player player) {
			if (ModState.PrevLoadedPlayerMods.ContainsKey(player.name))
				ModState.PrevLoadedPlayerMods.Remove(player.name);

			ModState.PrevLoadedPlayerMods.Add(player.name, new PrevMods {
				PrevEnabledMods = new HashSet<string>(ModState.EnabledMods)
			});

			ModState.SerializePreviousPlayerMods();

			if (ModState.PrevLoadedWorldMods.ContainsKey($"{Main.worldID}:{Main.worldName}"))
				ModState.PrevLoadedWorldMods.Remove($"{Main.worldID}:{Main.worldName}");

			ModState.PrevLoadedWorldMods.Add($"{Main.worldID}:{Main.worldName}", new PrevMods {
				PrevEnabledMods = new HashSet<string>(ModState.EnabledMods)
			});
			ModState.SerializePreviousWorldMods();
		}
	}

	public class SaveModsOnWorldCreation : ModWorld {
		public override void ModifyWorldGenTasks(List<GenPass> tasks, ref float totalWeight) { // Runs on world creation, so that mods are serialized even if you don't enter the world.
			if (ModState.PrevLoadedWorldMods.ContainsKey($"{Main.worldID}:{Main.worldName}"))
				ModState.PrevLoadedWorldMods.Remove($"{Main.worldID}:{Main.worldName}");

			ModState.PrevLoadedWorldMods.Add($"{Main.worldID}:{Main.worldName}", new PrevMods {
				PrevEnabledMods = new HashSet<string>(ModState.EnabledMods)
			});
			ModState.SerializePreviousWorldMods();
		}
	}
}
