using Gajatko.IniFiles;
using Microsoft.Xna.Framework.Graphics;
using SevenZip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using tConfigWrapper.DataTemplates;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tConfigWrapper {
	public static class LoadStep {
		public static string[] files;
		public static Action<string> loadProgressText;
		public static Action<float> loadProgress;
		public static Action<string> loadSubProgressText;
		internal static Dictionary<int, ItemInfo> globalItemInfos = new Dictionary<int, ItemInfo>();

		private static Dictionary<string, IniFileSection> recipeDict = new Dictionary<string, IniFileSection>();

		private static Mod mod => ModContent.GetInstance<tConfigWrapper>();

		public static void Setup() {
			var a = typeof(ModTile).GetFields(BindingFlags.Instance | BindingFlags.Public);
			mod.Logger.Debug(string.Join("\n", a.Select(x => $"{x.Name} - {x.FieldType}")));
			Assembly assembly = Assembly.GetAssembly(typeof(Mod));
			Type UILoadModsType = assembly.GetType("Terraria.ModLoader.UI.UILoadMods");

			object loadModsValue = assembly.GetType("Terraria.ModLoader.UI.Interface").GetField("loadMods", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
			MethodInfo LoadStageMethod = UILoadModsType.GetMethod("SetLoadStage", BindingFlags.Instance | BindingFlags.Public);
			PropertyInfo ProgressProperty = UILoadModsType.GetProperty("Progress", BindingFlags.Instance | BindingFlags.Public);
			PropertyInfo SubProgressTextProperty = UILoadModsType.GetProperty("SubProgressText", BindingFlags.Instance | BindingFlags.Public);

			loadProgressText = (string s) => LoadStageMethod.Invoke(loadModsValue, new object[] { s, -1 });
			loadProgress = (float f) => ProgressProperty.SetValue(loadModsValue, f);
			loadSubProgressText = (string s) => SubProgressTextProperty.SetValue(loadModsValue, s);

			loadProgressText?.Invoke("tConfig Wrapper: Loading Mods");
			loadProgress?.Invoke(0f);

			files = Directory.GetFiles(tConfigWrapper.ModsPath);
			for (int i = 0; i < files.Length; i++) {
				mod.Logger.Debug($"tConfig Mod: {Path.GetFileNameWithoutExtension(files[i])} is enabled!");
			}

			for (int i = 0; i < files.Length; i++) {
				loadProgressText?.Invoke($"tConfig Wrapper: Loading {Path.GetFileNameWithoutExtension(files[i])}");
				mod.Logger.Debug($"Loading tConfig Mod: {Path.GetFileNameWithoutExtension(files[i])}");
				Stream stream;
				using (stream = new MemoryStream()) {
					using (SevenZipExtractor extractor = new SevenZipExtractor(files[i])) {
						// Note for pollen, when you have a stream, at the end you have to dispose it
						// There are two ways to do this, calling .Dispose(), or using "using (Stream whatever ...) { ... }"
						// The "using" way is better because it will always Dispose it, even if there's an exception

						// Disposing via .Dispose()
						MemoryStream configStream = new MemoryStream();
						extractor.ExtractFile("Config.ini", configStream);
						configStream.Position = 0L;
						IniFileReader configReader = new IniFileReader(configStream);
						IniFile configFile = IniFile.FromStream(configReader);
						configStream.Dispose();
						mod.Logger.Debug($"Loading Content: {Path.GetFileNameWithoutExtension(files[i])}");
						int numIterations = 0;
						foreach (string fileName in extractor.ArchiveFileNames) {
							loadSubProgressText?.Invoke(fileName);
							numIterations++;
							if (Path.GetExtension(fileName) != ".ini")
								continue; // If the extension is not .ini, ignore the file

							if (fileName.Contains("\\Item\\"))
								CreateItem(fileName, Path.GetFileNameWithoutExtension(files[i]), extractor);

							else if (fileName.Contains("\\NPC\\"))
								CreateNPC(fileName, Path.GetFileNameWithoutExtension(files[i]), extractor);

							//else if (fileName.Contains("\\Tile\\"))
								//CreateTile(fileName, Path.GetFileNameWithoutExtension(files[i]), extractor);
							loadProgress?.Invoke((float)numIterations / extractor.ArchiveFileNames.Count);
						}
					}
					// this is for the obj (im scared of it)
					//stream.Position = 0L;
					//BinaryReader reader;
					//using (reader = new BinaryReader(stream))
					//{
					//	string modName = Path.GetFileName(files[i]).Split('.')[0];
					//	stream.Position = 0L;
					//	var version = new Version(reader.ReadString());

					//	// Don't know what these things are
					//	int modVersion;
					//	string modDLVersion, modURL;
					//	if (version >= new Version("0.20.5"))
					//		modVersion = reader.ReadInt32();

					//	if (version >= new Version("0.22.8") && reader.ReadBoolean())
					//	{
					//		modDLVersion = reader.ReadString();
					//		modURL = reader.ReadString();
					//	}

					//	reader.Close();
					//	stream.Close();
					//}
				}
			}
			//Reset progress bar
			loadSubProgressText?.Invoke("");
			loadProgressText?.Invoke("Loading mod");
			loadProgress?.Invoke(0f);
		}

		public static void SetupRecipes() {
			loadProgressText.Invoke("tConfig Wrapper: Adding Recipes");
			loadProgress.Invoke(0f);
			int progressCount = 0;
			foreach (var iniFileSection in recipeDict) {
				progressCount++;
				string modName = iniFileSection.Key.Split(':')[0];
				ModRecipe recipe = new ModRecipe(mod);
				foreach (var element in iniFileSection.Value.elements) {
					string[] splitElement = element.Content.Split('=');
					string key = splitElement[0];
					string value = splitElement[1];

					if (key == "Amount") {
						int id;
						if ((id = ItemID.FromLegacyName(iniFileSection.Key.Split(':')[1], 4)) != 0)
							recipe.SetResult(id, int.Parse(value));
						else
							recipe.SetResult(mod, iniFileSection.Key, int.Parse(value));
					}

					if (key == "needWater")
						recipe.needWater = bool.Parse(value);

					if (key == "Items") {
						foreach (string recipeItem in value.Split(',')) {
							var recipeItemInfo = recipeItem.Split(null, 2);
							int amount = int.Parse(recipeItemInfo[0]);

							int itemID = mod.ItemType($"{modName}:{recipeItemInfo[1]}");
							if (itemID == 0)
								itemID = ItemID.FromLegacyName(recipeItemInfo[1], 4);


							var numberIngredients = recipe.requiredItem.Count(i => i != null & i.type != ItemID.None);
							if (numberIngredients < 14)
								recipe.AddIngredient(itemID, amount);
							else {
								mod.Logger.Debug($"The following item has exceeded the max ingredient limit! -> {iniFileSection.Key}");
								tConfigWrapper.ReportErrors = true;
							}
						}
					}

					if (key == "Tiles") {
						foreach (string recipeTile in value.Split(',')) {
							string noSpaceTile = recipeTile.Replace(" ", "");
							if (!TileID.Search.ContainsName(noSpaceTile) && !CheckStringConversion(noSpaceTile)) {
								mod.Logger.Debug($"TileID {noSpaceTile} does not exist"); // we will have to manually convert anything that breaks lmao
								tConfigWrapper.ReportErrors = true;
							}
							else if (CheckStringConversion(noSpaceTile)) {
								string converted = ConvertTileStringTo14(noSpaceTile);
								recipe.AddTile(TileID.Search.GetId(converted));
							}
							else
								recipe.AddTile(TileID.Search.GetId(noSpaceTile));
						}
					}
				}

				if (recipe.createItem != null && recipe.createItem.type != ItemID.None)
					recipe.AddRecipe();
				loadProgress.Invoke(progressCount / recipeDict.Count);
			}
		}

		private static string ConvertTileStringTo14(string noSpaceTile) {
			if (noSpaceTile == "Anvil")
				return "Anvils";
			else if (noSpaceTile == "WorkBench" || noSpaceTile == "Workbench")
				return "WorkBenches";
			else if (noSpaceTile == "Furnace")
				return "Furnaces";
			else if (noSpaceTile == "Tinkerer'sWorkshop")
				return "TinkerersWorkbench";
			else if (noSpaceTile == "Bottle")
				return "Bottles";
			else if (noSpaceTile == "Bookcase")
				return "Bookcases";
			return noSpaceTile;
		}

		private static bool CheckStringConversion(string noSpaceTile) => noSpaceTile == "Anvil" || noSpaceTile == "WorkBench" || noSpaceTile == "Workbench" || noSpaceTile == "Furnace" || noSpaceTile == "Tinkerer'sWorkshop" || noSpaceTile == "Bottle" || noSpaceTile == "Bookcase";

		private static void CreateItem(string fileName, string modName, SevenZipExtractor extractor) {
			using (MemoryStream iniStream = new MemoryStream()) {
				extractor.ExtractFile(fileName, iniStream);
				iniStream.Position = 0;

				IniFileReader reader = new IniFileReader(iniStream);
				IniFile iniFile = IniFile.FromStream(reader);

				object info = new ItemInfo();
				string tooltip = null;

				// Get the mod name
				string itemName = Path.GetFileNameWithoutExtension(fileName);
				string internalName = $"{modName}:{itemName}";
				bool logItemAndModName = false;

				foreach (IniFileSection section in iniFile.sections) {
					foreach (IniFileElement element in section.elements) {
						if (section.Name == "Stats") {
							var splitElement = element.Content.Split('=');

							var statField = typeof(ItemInfo).GetField(splitElement[0]);

							// Set the tooltip, has to be done manually since the toolTip field doesn't exist in 1.3
							if (splitElement[0] == "toolTip") {
								tooltip = splitElement[1];
								continue;
							}
							else if (splitElement[0] == "useSound") {
								var soundStyleId = int.Parse(splitElement[1]);
								var soundStyle = new LegacySoundStyle(2, soundStyleId); // All items use the second sound ID
								statField = typeof(ItemInfo).GetField("UseSound");
								statField.SetValue(info, soundStyle);
								continue;
							}
							else if (splitElement[0] == "createTileName") {
								statField = typeof(ItemInfo).GetField("createTile");
								var succeed = int.TryParse(splitElement[1], out var createTileID);
								if (succeed) {
									statField.SetValue(info, createTileID);
									mod.Logger.Debug($"TileID {createTileID} was sucessfully parsed!");
								}
								else {
									// ModContent Tile
									mod.Logger.Debug($"TryParse(): Failed to parse the placeable tile! -> {splitElement[1]}");
								}
								continue;
							}
							else if (splitElement[0] == "type")
								continue;
							else if (statField == null) {
								mod.Logger.Debug($"Item field not found or invalid field! -> {splitElement[0]}");
								logItemAndModName = true;
								tConfigWrapper.ReportErrors = true;
								continue;
							}

							// Convert the value to an object of type statField.FieldType
							TypeConverter converter = TypeDescriptor.GetConverter(statField.FieldType);
							object realValue = converter.ConvertFromString(splitElement[1]);
							statField.SetValue(info, realValue);
						}
						else if (section.Name == "Recipe") {
							if (!recipeDict.ContainsKey(internalName))
								recipeDict.Add(internalName, section);
						}
					}
				}

				if (logItemAndModName)
					mod.Logger.Debug($"{modName}: {itemName}"); //Logs the item and mod name if "Field not found or invalid field". Mod and item name show up below the other log line

				// Check if a texture for the .ini file exists
				string texturePath = Path.ChangeExtension(fileName, "png");
				Texture2D itemTexture = null;
				if (extractor.ArchiveFileNames.Contains(texturePath)) {
					using (MemoryStream textureStream = new MemoryStream()) {
						extractor.ExtractFile(texturePath, textureStream); // Extract the texture
						textureStream.Position = 0;

						itemTexture = Texture2D.FromStream(Main.instance.GraphicsDevice, textureStream); // Load a Texture2D from the stream
					}
				}

				int id;
				if ((id = ItemID.FromLegacyName(itemName, 4)) != 0) {
					if (!globalItemInfos.ContainsKey(id))
						globalItemInfos.Add(id, (ItemInfo)info);
					else
						globalItemInfos[id] = (ItemInfo)info;

					reader.Dispose();
					return;
				}

				if (itemTexture != null)
					mod.AddItem(internalName, new BaseItem((ItemInfo)info, itemName, tooltip, itemTexture));
				else
					mod.AddItem(internalName, new BaseItem((ItemInfo)info, itemName, tooltip));

				reader.Dispose();
			}
		}

		private static void CreateNPC(string fileName, string modName, SevenZipExtractor extractor) {
			using (MemoryStream iniStream = new MemoryStream()) {
				extractor.ExtractFile(fileName, iniStream);
				iniStream.Position = 0L;

				IniFileReader reader = new IniFileReader(iniStream);
				IniFile iniFile = IniFile.FromStream(reader);

				object info = new NpcInfo();

				string npcName = Path.GetFileNameWithoutExtension(fileName);
				string internalName = $"{modName}:{npcName}";
				bool logNPCAndModName = false;

				foreach (IniFileSection section in iniFile.sections) {
					foreach (IniFileElement element in section.elements) {
						if (section.Name == "Stats") {
							var splitElement = element.Content.Split('=');

							string split1Correct = ConvertField14(splitElement[0]);
							var statField = typeof(NpcInfo).GetField(split1Correct);

							if (splitElement[0] == "soundHit") {
								var soundStyleID = int.Parse(splitElement[1]);
								var soundStyle = new LegacySoundStyle(3, soundStyleID); // All NPC hit sounds use 3
								statField = typeof(NpcInfo).GetField("HitSound");
								statField.SetValue(info, soundStyle);
								continue;
							}
							else if (splitElement[0] == "soundKilled") {
								var soundStyleID = int.Parse(splitElement[1]);
								var soundStyle = new LegacySoundStyle(4, soundStyleID); // All death sounds use 4
								statField = typeof(NpcInfo).GetField("DeathSound");
								statField.SetValue(info, soundStyle);
								continue;
							}
							else if (splitElement[0] == "type")
								continue;
							else if (statField == null) {
								mod.Logger.Debug($"NPC field not found or invalid field! -> {splitElement[0]}");
								logNPCAndModName = true;
								tConfigWrapper.ReportErrors = true;
								continue;
							}

							TypeConverter converter = TypeDescriptor.GetConverter(statField.FieldType);
							object realValue = converter.ConvertFromString(splitElement[1]);
							statField.SetValue(info, realValue);
						}
						else if (section.Name == "BuffImmunities") {
							// do
						}
						else if (section.Name == "Drops") {
							// do
						}
					}
				}

				if (logNPCAndModName)
					mod.Logger.Debug($"{modName}: {npcName}"); //Logs the npc and mod name if "Field not found or invalid field". Mod and npc name show up below the other log line

				// Check if a texture for the .ini file exists
				string texturePath = Path.ChangeExtension(fileName, "png");
				Texture2D npcTexture = null;
				if (extractor.ArchiveFileNames.Contains(texturePath)) {
					using (MemoryStream textureStream = new MemoryStream()) {
						extractor.ExtractFile(texturePath, textureStream); // Extract the texture
						textureStream.Position = 0;

						npcTexture = Texture2D.FromStream(Main.instance.GraphicsDevice, textureStream); // Load a Texture2D from the stream
					}
				}

				if (npcTexture != null)
					mod.AddNPC(internalName, new BaseNPC((NpcInfo)info, npcName, npcTexture));
				else
					mod.AddNPC(internalName, new BaseNPC((NpcInfo)info, npcName));

				reader.Dispose();
			}
		}

		private static string ConvertField14(string splitElement) {
			if (splitElement == "knockBackResist")
				return "knockBackResist";
			return splitElement;
		}

		private static void CreateTile(string fileName, string modName, SevenZipExtractor extractor) {
			using (MemoryStream iniSteam = new MemoryStream()) {
				extractor.ExtractFile(fileName, iniSteam);
				iniSteam.Position = 0L;

				IniFileReader reader = new IniFileReader(iniSteam);
				IniFile iniFile = IniFile.FromStream(reader);

				object info = new TileInfo();

				string tileName = Path.GetFileNameWithoutExtension(fileName);
				string internalName = $"{modName}:{tileName}";
				bool logTileAndName = false;

				foreach (IniFileSection section in iniFile.sections) {
					foreach (IniFileElement element in section.elements) {
						if (section.Name == "Stats") {
							var splitElement = element.Content.Split('=');

							var statField = typeof(TileInfo).GetField(splitElement[0]);

							if (splitElement[0] == "type")
								continue;
							else if (statField == null) {
								mod.Logger.Debug($"Tile field not found or invalid field! -> {splitElement[0]}");
								logTileAndName = true;
								tConfigWrapper.ReportErrors = true;
								continue;
							}

							TypeConverter converter = TypeDescriptor.GetConverter(statField.FieldType);
							object realValue = converter.ConvertFromString(splitElement[1]);
							statField.SetValue(info, realValue);
						}
					}
				}

				string texturePath = Path.ChangeExtension(fileName, "png");
				/*Texture2D tileTexture = null;
				if (extractor.ArchiveFileNames.Contains(texturePath)) {
					using (MemoryStream textureSteam = new MemoryStream()) {
						extractor.ExtractFile(texturePath, textureSteam);
						textureSteam.Position = 0L;

						tileTexture = Texture2D.FromStream(Main.instance.GraphicsDevice, textureSteam);
					}
				}*/

				if (extractor.ArchiveFileNames.Contains(texturePath)) {
					BaseTile tile = new BaseTile();
					
					mod.AddTile(internalName, new BaseTile((TileInfo)info, internalName, texturePath, extractor), "tConfigWrapper/DataTemplates/MissingTexture");
				}

				if (logTileAndName)
					mod.Logger.Debug($"{modName}: {tileName}"); //Logs the tile and mod name if "Field not found or invalid field". Mod and tile name show up below the other log lines
			}
		}
	}
}
