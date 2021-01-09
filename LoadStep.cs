using Gajatko.IniFiles;
using Microsoft.Xna.Framework.Graphics;
using SevenZip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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

		private static Dictionary<string, IniFileSection> recipeDict = new Dictionary<string, IniFileSection>();

		private static Mod mod => ModContent.GetInstance<tConfigWrapper>();

		public static void Setup() {
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

						foreach (string fileName in extractor.ArchiveFileNames) {
							if (Path.GetExtension(fileName) != ".ini")
								continue; // If the extension is not .ini, ignore the file

							if (fileName.Contains("\\Item\\"))
								CreateItem(fileName, Path.GetFileNameWithoutExtension(files[i]), extractor);

							else if (fileName.Contains("\\NPC\\"))
								CreateNPC();
						}

						loadProgress?.Invoke((float)i / files.Length);
						loadSubProgressText?.Invoke(Path.GetFileName(files[i]));
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
			// TODO: @pollen__, create a custom loading step
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

					if (key == "Amount")
						recipe.SetResult(mod, iniFileSection.Key, int.Parse(value));
					if (key == "needWater")
						recipe.needWater = bool.Parse(value);

					if (key == "Items") {
						foreach (string recipeItem in value.Split(',')) {
							var recipeItemInfo = recipeItem.Split(null, 2);
							int amount = int.Parse(recipeItemInfo[0]);

							int itemID = mod.ItemType($"{modName}:{recipeItemInfo[1]}");
							if (itemID == 0)
								itemID = ItemID.FromLegacyName(recipeItemInfo[1], 4);

							recipe.AddIngredient(itemID, amount);
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
				recipe.AddRecipe();
				loadProgress.Invoke(progressCount / recipeDict.Count);
			}
			//Reset progress bar
			loadSubProgressText?.Invoke("");
			loadProgressText?.Invoke("Loading mod");
			loadProgress?.Invoke(0f);
		}

		private static string ConvertTileStringTo14(string noSpaceTile) {
			if (noSpaceTile == "Anvil")
				return "Anvils";
			return noSpaceTile;
		}

		private static bool CheckStringConversion(string noSpaceTile) {
			if (noSpaceTile == "Anvil")
				return true;
			return false;
		}

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

							if (splitElement[0] == "useSound") {
								var soundStyleId = int.Parse(splitElement[1]);
								var soundStyle = new LegacySoundStyle(2, soundStyleId); // All items use the second sound ID
								statField = typeof(ItemInfo).GetField("UseSound");
								statField.SetValue(info, soundStyle);
								continue;
							}

							if (statField == null || splitElement[0] == "type") {
								mod.Logger.Debug($"Field not found or invalid field! -> {splitElement[0]}");
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
				//Possible code to change the internal mod name so WMITF will register items as from their original mod
				/*if (itemTexture != null) {
					ModItem item = new BaseItem((ItemInfo)info, itemName, tooltip, itemTexture);
					var field = typeof(Mod).GetField("<Name>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
					field.SetValue(item.mod, modName);
					ModContent.GetInstance<tConfigWrapper>().AddItem(internalName, item);
				}
				else {
					ModItem item = new BaseItem((ItemInfo)info, itemName, tooltip);
					var field = typeof(Mod).GetField("<Name>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
					field.SetValue(item.mod, modName);
					ModContent.GetInstance<tConfigWrapper>().AddItem(internalName, item);
				}*/
				if (itemTexture != null)
					mod.AddItem(internalName, new BaseItem((ItemInfo)info, itemName, tooltip, itemTexture));

				else
					mod.AddItem(internalName, new BaseItem((ItemInfo)info, itemName, tooltip));

				reader.Dispose();
			}
		}

		private static void CreateNPC() {
			using (MemoryStream iniSteam = new MemoryStream()) {

			}
		}
	}
}
