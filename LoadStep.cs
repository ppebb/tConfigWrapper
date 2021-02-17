using Gajatko.IniFiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SevenZip;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using tConfigWrapper.Common.DataTemplates;
using static tConfigWrapper.Common.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tConfigWrapper {
	public static class LoadStep {
		public static string[] files; // Array of all mods, should be changed to only enabled mods but I am not doing any of that yet :)
		public static Action<string> loadProgressText; // Heading text during loading
		public static Action<float> loadProgress; // Progress bar during loading: 0-1 scale
		public static Action<string> loadSubProgressText; // Subtext during loading
		public static int taskCompletedCount; // Int used for tracking load progress during content loading
		internal static ConcurrentDictionary<int, ItemInfo> globalItemInfos = new ConcurrentDictionary<int, ItemInfo>(); // Dictionaries are selfexplanatory, concurrent so that multiple threads can access them without dying
		private static ConcurrentDictionary<string, IniFileSection> recipeDict = new ConcurrentDictionary<string, IniFileSection>();
		private static ConcurrentDictionary<string, ModItem> itemsToLoad = new ConcurrentDictionary<string, ModItem>();
		private static ConcurrentDictionary<string, (ModTile tile, string texture)> tilesToLoad = new ConcurrentDictionary<string, (ModTile, string)>();
		private static ConcurrentDictionary<string, ModNPC> npcsToLoad = new ConcurrentDictionary<string, ModNPC>();
		private static ConcurrentDictionary<string, ModProjectile> projectilesToLoad = new ConcurrentDictionary<string, ModProjectile>();
		private static ConcurrentDictionary<string, (ModWall wall, string texture)> wallsToLoad = new ConcurrentDictionary<string, (ModWall, string)>();
		private static ConcurrentDictionary<string, ModPrefix> prefixesToLoad = new ConcurrentDictionary<string, ModPrefix>();
		internal static ConcurrentBag<ModPrefix> suffixes = new ConcurrentBag<ModPrefix>();
		internal static ConcurrentDictionary<ModTile, (bool, string)> tileMapData = new ConcurrentDictionary<ModTile, (bool, string)>();

		internal static Mod mod => ModContent.GetInstance<tConfigWrapper>();

		public static void Setup() { // Method to load everything
			var a = typeof(ModPrefix).GetFields(BindingFlags.Instance | BindingFlags.Public);
			mod.Logger.Debug(string.Join("\n", a.Select(x => $"{x.Name} - {x.FieldType}")));

			recipeDict.TryGetValue("", out _); // Sanity check to make sure it's initialized
			// Cringe reflection
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

			files = Directory.GetFiles(tConfigWrapper.ModsPath, "*.obj"); // Populates array with all mods
			foreach (var modName in files)
				mod.Logger.Debug($"tConfig Mod: {Path.GetFileNameWithoutExtension(modName)} is enabled!"); // Writes all mod names to logs

			for (int i = 0; i < files.Length; i++) { // Iterates through every mod
				loadProgressText?.Invoke($"tConfig Wrapper: Loading {Path.GetFileNameWithoutExtension(files[i])}"); // Sets heading text to display the mod being loaded
				mod.Logger.Debug($"Loading tConfig Mod: {Path.GetFileNameWithoutExtension(files[i])}"); // Logs the mod being loaded
				using (var finished = new CountdownEvent(1)) {
					using (SevenZipExtractor extractor = new SevenZipExtractor(files[i])) {
						bool CursedMod = extractor.ArchiveFileNames[0].Contains("Pickaxe+ v1.3a"); // Cursed mod bad

						if (CursedMod)
							loadSubProgressText?.Invoke("You are loading a cursed mod, it's not our fault it takes so long to load");

						mod.Logger.Debug($"Loading Content: {Path.GetFileNameWithoutExtension(files[i])}");

						ConcurrentDictionary<string, MemoryStream> streams = new ConcurrentDictionary<string, MemoryStream>();
						DecompressMod(files[i], extractor, streams); // Decompresses mods since .obj files are literally just 7z files

						// Clear dictionaries and task count or else stuff from other mods will interfere with the current mod being loaded
						itemsToLoad.Clear();
						tilesToLoad.Clear();
						npcsToLoad.Clear();
						projectilesToLoad.Clear();
						wallsToLoad.Clear();
						prefixesToLoad.Clear();
						taskCompletedCount = 0;

						// Slowass linq sorts content and then is assigned to individual threads
						IEnumerable<string> itemFiles = extractor.ArchiveFileNames.Where(name => name.Contains("\\Item\\") && Path.GetExtension(name) == ".ini");
						IEnumerable<string> npcFiles = extractor.ArchiveFileNames.Where(name => name.Contains("\\NPC\\") && Path.GetExtension(name) == ".ini");
						IEnumerable<string> tileFiles = extractor.ArchiveFileNames.Where(name => name.Contains("\\Tile\\") && Path.GetExtension(name) == ".ini");
						IEnumerable<string> projectileFiles = extractor.ArchiveFileNames.Where(name => name.Contains("\\Projectile\\") && Path.GetExtension(name) == ".ini");
						IEnumerable<string> wallFiles = extractor.ArchiveFileNames.Where(name => name.Contains("\\Wall\\") && Path.GetExtension(name) == ".ini");
						IEnumerable<string> prefixFiles = extractor.ArchiveFileNames.Where(name => name.Contains("\\Prefix\\") && Path.GetExtension(name) == ".ini");

						int contentCount = itemFiles.Count() + npcFiles.Count() + tileFiles.Count() + projectileFiles.Count() + wallFiles.Count() + prefixFiles.Count(); // Count all loadable content in mod for accurate loading progress

						if (contentCount != 0)
						{
							Thread itemThread = new Thread(CreateItem);
							itemThread.Start(new object[] { itemFiles, Path.GetFileNameWithoutExtension(files[i]), files[i], finished, extractor, contentCount, streams });
							finished.AddCount();

							Thread npcThread = new Thread(CreateNPC);
							npcThread.Start(new object[] { npcFiles, Path.GetFileNameWithoutExtension(files[i]), files[i], finished, extractor, contentCount, streams });
							finished.AddCount();

							Thread tileThread = new Thread(CreateTile);
							tileThread.Start(new object[] { tileFiles, Path.GetFileNameWithoutExtension(files[i]), files[i], finished, extractor, contentCount, streams });
							finished.AddCount();

							Thread projectileThread = new Thread(CreateProjectile);
							projectileThread.Start(new object[] { projectileFiles, Path.GetFileNameWithoutExtension(files[i]), files[i], finished, extractor, contentCount, streams });
							finished.AddCount();

							Thread wallThread = new Thread(CreateWall);
							wallThread.Start(new object[] { wallFiles, Path.GetFileNameWithoutExtension(files[i]), files[i], finished, extractor, contentCount, streams });
							finished.AddCount();

							Thread prefixThread = new Thread(CreatePrefix);
							prefixThread.Start(new object[] { prefixFiles, Path.GetFileNameWithoutExtension(files[i]), files[i], finished, extractor, contentCount, streams });
							finished.AddCount();

							finished.Signal();
							finished.Wait();
						}

						foreach (var memoryStream in streams) {
							memoryStream.Value.Dispose();
						}

						//Load content from dictionaries
						foreach (var item in itemsToLoad) {
							mod.AddItem(item.Key, item.Value);
						}

						foreach (var tile in tilesToLoad) {
							mod.AddTile(tile.Key, tile.Value.tile, tile.Value.texture);
						}

						foreach (var npc in npcsToLoad) {
							mod.AddNPC(npc.Key, npc.Value);
						}

						foreach (var projectile in projectilesToLoad) {
							mod.AddProjectile(projectile.Key, projectile.Value);
						}

						foreach (var wall in wallsToLoad) {
							mod.AddWall(wall.Key, wall.Value.wall, wall.Value.texture);
						}

						foreach (var prefix in prefixesToLoad) {
							mod.AddPrefix(prefix.Key, prefix.Value);
						}
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
			//Reset progress bar
			loadSubProgressText?.Invoke("");
			loadProgressText?.Invoke("Loading mod");
			loadProgress?.Invoke(0f);
		}

		private static void DecompressMod(string objPath, SevenZipExtractor extractor, ConcurrentDictionary<string, MemoryStream> streams) {
			List<string> fileNames = extractor.ArchiveFileNames.ToList();
			loadSubProgressText?.Invoke("Decompressing");
			double numThreads = Math.Min((double)ModContent.GetInstance<WrapperModConfig>().NumThreads, fileNames.Count);

			using (CountdownEvent decompressCount = new CountdownEvent(1)) {
				// Split the files into numThreads chunks
				var chunks = new List<List<string>>();
				int chunkSize = (int) Math.Round(fileNames.Count / numThreads, MidpointRounding.AwayFromZero);

				for (int i = 0; i < fileNames.Count; i += chunkSize) {
					chunks.Add(fileNames.GetRange(i, Math.Min(chunkSize, fileNames.Count - i)));
				}

				// Create threads and decompress the chunks
				foreach (var chunk in chunks) {
					ThreadPool.QueueUserWorkItem(DecompressMod, new object[] {objPath, chunk, streams, decompressCount});
					decompressCount.AddCount();
				}

				// Wait for the CountdownEvent to end
				decompressCount.Signal();
				decompressCount.Wait();
			}
		}

		public static int decompressTasksCompleted = 0; // Total number of items decompressed
		public static int decompressTotalFiles = 0; // Total number of items that need to be decompressed

		private static void DecompressMod(object callback) {
			// Process the parameters
			object[] parameters = (object[])callback;
			string objPath = parameters[0] as string;
			List<string> files = parameters[1] as List<string>;
			ConcurrentDictionary<string, MemoryStream> streams = parameters[2] as ConcurrentDictionary<string, MemoryStream>;
			CountdownEvent countdown = parameters[3] as CountdownEvent;

			// Create a FileStream with the following arguments to be able to have multiple threads access it
			using (FileStream fileStream = new FileStream(objPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			using (SevenZipExtractor extractor = new SevenZipExtractor(fileStream)) {
				decompressTotalFiles += files.Count; // Counts the number of items that need to be loaded for accurate progress bar
				foreach (var fileName in files) {
					loadProgress?.Invoke((float)decompressTasksCompleted / decompressTotalFiles); // Sets the progress bar
					// If the extension is not valid, skip the file
					string extension = Path.GetExtension(fileName);
					if (!(extension == ".ini" || extension == ".cs" || extension == ".png" || extension == ".dll" || extension == ".obj"))
						continue;

					// Create a MemoryStream and extract the file
					MemoryStream stream = new MemoryStream();
					extractor.ExtractFile(fileName, stream);
					stream.Position = 0;
					streams.TryAdd(fileName, stream);
					decompressTasksCompleted++; // Increments the number of tasks completed for accurate progress display
				}
			}

			// Signal the end of the thread
			countdown.Signal();
		}

		public static void SetupRecipes() { // Sets up recipes, what were you expecting?
			loadProgressText.Invoke("tConfig Wrapper: Adding Recipes"); // Ah yes, more reflection
			loadProgress.Invoke(0f);
			int progressCount = 0;
			bool initialized = (bool)Assembly.GetAssembly(typeof(Mod)).GetType("Terraria.ModLoader.MapLoader").GetField("initialized", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null); // Check if the map is already initialized
			foreach (var iniFileSection in recipeDict) { // Load every recipe in the recipe dict
				progressCount++; // Count the number of recipes, still broken somehow :(
				string modName = iniFileSection.Key.Split(':')[0];
				ModRecipe recipe = null;
				if (initialized) // Only make the recipe if the maps have already been initialized. The checks for initialized are because I run this method in GetTileMapEntires() to see what tiles are used in recipes and need to have a name in their map entry
					recipe = new ModRecipe(mod);
				foreach (var element in iniFileSection.Value.elements) { // ini recipe loading, code is readable enough.
					string[] splitElement = element.Content.Split('=');
					string key = splitElement[0];
					string value = splitElement[1];
					switch (key) {
						case "Amount" when initialized: {
							int id;
							string[] splitKey = iniFileSection.Key.Split(':');
							string itemName = splitKey.Length == 1 ? splitKey[0] : splitKey[1];
							if ((id = ItemID.FromLegacyName(itemName, 4)) != 0)
								recipe?.SetResult(id, int.Parse(value));
							else
								recipe?.SetResult(mod, iniFileSection.Key, int.Parse(value));
							break;
						}
						case "needWater" when initialized:
							recipe.needWater = bool.Parse(value);
							break;
						case "Items" when initialized: {
							foreach (string recipeItem in value.Split(',')) {
								var recipeItemInfo = recipeItem.Split(null, 2);
								int amount = int.Parse(recipeItemInfo[0]);

								int itemID = mod.ItemType($"{modName}:{recipeItemInfo[1].RemoveIllegalCharacters()}");
								if (itemID == 0)
									itemID = ItemID.FromLegacyName(recipeItemInfo[1], 4);

								var numberIngredients =
									recipe?.requiredItem.Count(i => i != null & i.type != ItemID.None);
								if (numberIngredients < 14)
									recipe?.AddIngredient(itemID, amount);
								else {
									mod.Logger.Debug($"The following item has exceeded the max ingredient limit! -> {iniFileSection.Key}");
									tConfigWrapper.ReportErrors = true;
								}
							}
							break;
						}
						case "Tiles": { // Does stuff to check for modtiles and vanilla tiles that have changed their name since 1.1.2
							foreach (string recipeTile in value.Split(',')) {
								string recipeTileIR = recipeTile.RemoveIllegalCharacters();
								int tileInt = mod.TileType($"{modName}:{recipeTileIR}");
								var tileModTile = mod.GetTile($"{modName}:{recipeTileIR}");
								if (!TileID.Search.ContainsName(recipeTileIR) && !CheckIDConversion(recipeTileIR) && tileInt == 0 && tileModTile == null) { // Would love to replace this with Utilities.StringToContent() but this one is special and needs to add stuff to a dictionary so I can't
									if (initialized) {
										mod.Logger.Debug($"TileID {modName}:{recipeTileIR} does not exist"); // We will have to manually convert anything that breaks lmao
										tConfigWrapper.ReportErrors = true;
									}
								}
								else if (CheckIDConversion(recipeTileIR) || TileID.Search.ContainsName(recipeTileIR)) {
									string converted = ConvertIDTo13(recipeTileIR);
									if (initialized)
										recipe?.AddTile(TileID.Search.GetId(converted));
								}
								else if (tileInt != 0) {
									if (initialized) {
										recipe?.AddTile(tileModTile);
										mod.Logger.Debug($"{modName}:{recipeTileIR} added to recipe through mod.TileType!");
									}
									else {
										tileMapData[tileModTile] = (true, tileMapData[tileModTile].Item2); // I do this because either I can't just change Item1 directly to true OR because I am very not smart and couldn't figure out how to set it individually.
									}
								}
							}
							break;
						}
					}
				}

				if (recipe?.createItem != null && recipe?.createItem.type != ItemID.None && initialized)
					recipe?.AddRecipe();
				if (initialized)
					loadProgress.Invoke(progressCount / recipeDict.Count);
			}
		}

		private static void CreateItem(object stateInfo) { // This is literally just to simplify the threading and counting of content loading
			object[] parameters = (object[])stateInfo;
			CountdownEvent countdown = (CountdownEvent)parameters[3];

			foreach (var fileName in (IEnumerable<string>)parameters[0]) {
				loadSubProgressText?.Invoke(fileName);
				CreateItem(fileName, (string)parameters[1], (string)parameters[2], (ConcurrentDictionary<string, MemoryStream>)parameters[6]);
				taskCompletedCount++;
				loadProgress?.Invoke((float)taskCompletedCount / (int)parameters[5]);
			}
			countdown.Signal();
		}

		private static void CreateItem(string fileName, string modName, string extractPath, ConcurrentDictionary<string, MemoryStream> streams) { // Loads content, I don't know how it works either
			MemoryStream iniStream = streams[fileName];

			IniFileReader reader = new IniFileReader(iniStream);
			IniFile iniFile = IniFile.FromStream(reader);

			object info = new ItemInfo();
			List<string> toolTipList = new List<string>();

			// Get the mod name
			string itemName = Path.GetFileNameWithoutExtension(fileName);
			string internalName = $"{modName}:{itemName.RemoveIllegalCharacters()}";
			// TODO: If the item is from Terraria, make it a GlobalItem
			if (ItemID.FromLegacyName(itemName, 4) != 0)
				internalName = itemName;
			bool logItemAndModName = false;
			string createWall = null;
			string createTile = null;
			string shoot = null;

			foreach (IniFileSection section in iniFile.sections) {
				foreach (IniFileElement element in section.elements) {
					switch (section.Name) {
						case "Stats": {
							var splitElement = element.Content.Split('=');

							var statField = typeof(ItemInfo).GetField(splitElement[0]);

							switch (splitElement[0]) {
								// Set the tooltip, has to be done manually since the toolTip field doesn't exist in 1.3
								case "toolTip":
								case "toolTip1":
								case "toolTip2":
								case "toolTip3":
								case "toolTip4":
								case "toolTip5":
								case "toolTip6":
								case "toolTip7": {
									toolTipList.Add(splitElement[1]);
									continue;
								}
								case "useSound": {
									var soundStyleId = int.Parse(splitElement[1]);
									var soundStyle = new LegacySoundStyle(2, soundStyleId); // All items use the second sound ID
									statField = typeof(ItemInfo).GetField("UseSound");
									statField.SetValue(info, soundStyle);
									continue;
								}
								case "createTileName": {
									createTile = $"{modName}:{splitElement[1]}";
									continue;
								}
								case "projectile": {
									shoot = $"{modName}:{splitElement[1]}";
									continue;
								}
								case "createWallName": {
									createWall = $"{modName}:{splitElement[1]}";
									continue;
								}
								case "type":
									continue;
								default: {
									if (statField == null) {
										mod.Logger.Debug($"Item field not found or invalid field! -> {splitElement[0]}");
										logItemAndModName = true;
										tConfigWrapper.ReportErrors = true;
										continue;
									}
									break;
								}
							}

							// Convert the value to an object of type statField.FieldType
							TypeConverter converter = TypeDescriptor.GetConverter(statField.FieldType);
							object realValue = converter.ConvertFromString(splitElement[1]);
							statField.SetValue(info, realValue);
							break;
						}
						case "Recipe": {
							if (!recipeDict.ContainsKey(internalName))
								recipeDict.TryAdd(internalName, section);
							break;
						}
					}
				}
			}

			if (logItemAndModName)
				mod.Logger.Debug($"{internalName}"); //Logs the item and mod name if "Field not found or invalid field". Mod and item name show up below the other log line

			string toolTip = null;

			foreach (string toolTipLine in toolTipList) {
				toolTip += "\n" + toolTipLine;
			}

			// Check if a texture for the .ini file exists
			string texturePath = Path.ChangeExtension(fileName, "png");
			Texture2D itemTexture = null;
			if (!Main.dedServ && streams.TryGetValue(texturePath, out MemoryStream textureStream)) {
				itemTexture = Texture2D.FromStream(Main.instance.GraphicsDevice, textureStream); // Load a Texture2D from the stream
			}

			int id;
			if ((id = ItemID.FromLegacyName(itemName, 4)) != 0) {
				if (!globalItemInfos.ContainsKey(id))
					globalItemInfos.TryAdd(id, (ItemInfo)info);
				else
					globalItemInfos[id] = (ItemInfo)info;

				reader.Dispose();
				return;
			}

			if (itemTexture != null)
				itemsToLoad.TryAdd(internalName, new BaseItem((ItemInfo)info, itemName, createTile, shoot, createWall, toolTip, itemTexture));
			else
				itemsToLoad.TryAdd(internalName, new BaseItem((ItemInfo)info, itemName, createTile, shoot, createWall, toolTip));
			reader.Dispose();
			//}
		}

		private static void CreateNPC(object stateInfo) { // This is literally just to simplify threading and progress counting
			object[] parameters = (object[])stateInfo;
			CountdownEvent countdown = (CountdownEvent)parameters[3];

			foreach (var fileName in (IEnumerable<string>)parameters[0]) {
				loadSubProgressText?.Invoke(fileName);
				CreateNPC(fileName, (string)parameters[1], (string)parameters[2], (ConcurrentDictionary<string, MemoryStream>)parameters[6]);
				taskCompletedCount++;
				loadProgress?.Invoke((float)taskCompletedCount / (int)parameters[5]);
			}
			countdown.Signal();
		}

		private static void CreateNPC(string fileName, string modName, string extractPath, ConcurrentDictionary<string, MemoryStream> streams) { // I don't know how this works either
			List<(int, int?, string, float)> dropList = new List<(int, int?, string, float)>();
			MemoryStream iniStream = streams[fileName];
			IniFileReader reader = new IniFileReader(iniStream);
			IniFile iniFile = IniFile.FromStream(reader);

			object info = new NpcInfo();

			string npcName = Path.GetFileNameWithoutExtension(fileName);
			string internalName = $"{modName}:{npcName.RemoveIllegalCharacters()}";
			bool logNPCAndModName = false;

			foreach (IniFileSection section in iniFile.sections) {
				foreach (IniFileElement element in section.elements) {
					switch (section.Name) {
						case "Stats": {
							var splitElement = element.Content.Split('=');

							string split1Correct = ConvertField13(splitElement[0]);
							var statField = typeof(NpcInfo).GetField(split1Correct);

							switch (splitElement[0]) {
								case "soundHit": {
									var soundStyleID = int.Parse(splitElement[1]);
									var soundStyle = new LegacySoundStyle(3, soundStyleID); // All NPC hit sounds use 3
									statField = typeof(NpcInfo).GetField("HitSound");
									statField.SetValue(info, soundStyle);
									continue;
								}
								case "soundKilled": {
									var soundStyleID = int.Parse(splitElement[1]);
									var soundStyle = new LegacySoundStyle(4, soundStyleID); // All death sounds use 4
									statField = typeof(NpcInfo).GetField("DeathSound");
									statField.SetValue(info, soundStyle);
									continue;
								}
								case "type":
									continue;
								default: {
									if (statField == null) {
										mod.Logger.Debug($"NPC field not found or invalid field! -> {splitElement[0]}");
										logNPCAndModName = true;
										tConfigWrapper.ReportErrors = true;
										continue;
									}

									break;
								}
							}

							TypeConverter converter = TypeDescriptor.GetConverter(statField.FieldType);
							object realValue = converter.ConvertFromString(splitElement[1]);
							statField.SetValue(info, realValue);
							break;
						}
						case "Buff Immunities": {
							var splitElement = element.Content.Split('=');
							splitElement[0].Replace(" ", "").Replace("!", "");

							FieldInfo npcInfoImmunity = typeof(NpcInfo).GetField("buffImmune");
							if (BuffID.Search.ContainsName(splitElement[0])) { // Will 100% need to adjust this once we get mod buff loading implemented
								bool[] immunity = new bool[BuffLoader.BuffCount];
								immunity[BuffID.Search.GetId(splitElement[0])] = bool.Parse(splitElement[1]);
								npcInfoImmunity.SetValue(info, immunity);
							}
							else
								mod.Logger.Debug($"{splitElement[0]} doesn't exist!"); // Will have to manually convert 
							break;
						}
						case "Drops":
							// example of drop string: 1-4 Golden Flame=0.7
							string dropRangeString = element.Content.Split(new[] { ' ' }, 2)[0]; // This gets the drop range, everthing before the first space
							string dropItemString = element.Content.Split(new[] { ' ' }, 2)[1].Split('=')[0]; // This gets everything after the first space, then it splits at the = and gets everything before it
							string dropChanceString = element.Content.Split('=')[1]; // Gets everything after the = sign
							int min;
							int? max = null;
							if (dropRangeString.Contains("-")) {
								min = int.Parse(dropRangeString.Split('-')[0]); 
								max = int.Parse(dropRangeString.Split('-')[1]) + 1; // + 1 because the max is exclusive in Main.rand.Next()
							}
							else {
								min = int.Parse(dropRangeString);
							}

							dropList.Add((min, max, $"{modName}:{dropItemString}", float.Parse(dropChanceString) / 100));
							break;
					}
				}
			}

			if (logNPCAndModName)
				mod.Logger.Debug($"{internalName}"); //Logs the npc and mod name if "Field not found or invalid field". Mod and npc name show up below the other log line

			// Check if a texture for the .ini file exists
			string texturePath = Path.ChangeExtension(fileName, "png");
			Texture2D npcTexture = null;
			if (!Main.dedServ && streams.TryGetValue(texturePath, out MemoryStream textureStream)) {
				npcTexture = Texture2D.FromStream(Main.instance.GraphicsDevice, textureStream); // Load a Texture2D from the stream
			}

			if (npcTexture != null)
				npcsToLoad.TryAdd(internalName, new BaseNPC((NpcInfo)info, dropList, npcName, npcTexture));
			else
				npcsToLoad.TryAdd(internalName, new BaseNPC((NpcInfo)info, dropList, npcName));

			reader.Dispose();
		}

		private static void CreateTile(object stateInfo) { // This is literally for easier multithreading again
			object[] parameters = (object[])stateInfo;
			CountdownEvent countdown = (CountdownEvent)parameters[3];

			foreach (var fileName in (IEnumerable<string>)parameters[0]) {
				loadSubProgressText?.Invoke(fileName);
				CreateTile(fileName, (string)parameters[1], (string)parameters[2], (ConcurrentDictionary<string, MemoryStream>)parameters[6]);
				taskCompletedCount++;
				loadProgress?.Invoke((float)taskCompletedCount / (int)parameters[5]);
			}
			countdown.Signal();
		}

		private static void CreateTile(string fileName, string modName, string extractPath, ConcurrentDictionary<string, MemoryStream> streams) { // I have no idea how this works either
			Dictionary<string, int> tileNumberFields = new Dictionary<string, int>();
			Dictionary<string, bool> tileBoolFields = new Dictionary<string, bool>();
			Dictionary<string, string> tileStringFields = new Dictionary<string, string>();

			MemoryStream iniStream = streams[fileName];

			IniFileReader reader = new IniFileReader(iniStream);
			IniFile iniFile = IniFile.FromStream(reader);

			object info = new TileInfo();

			string displayName = Path.GetFileNameWithoutExtension(fileName);
			string internalName = $"{modName}:{displayName.RemoveIllegalCharacters()}";
			bool logTileAndModName = false;
			bool oreTile = false;

			foreach (IniFileSection section in iniFile.sections) {
				foreach (IniFileElement element in section.elements) {
					if (section.Name == "Stats") {
						var splitElement = element.Content.Split('=');

						string converted = ConvertField13(splitElement[0]);
						var statField = typeof(TileInfo).GetField(converted);

						if ((converted == "tileShine" && splitElement[1] != "0") || displayName.Contains("Ore"))
							oreTile = true;

						switch (converted) {
							case "minPick":
							case "minAxe":
							case "minHammer": {
								if (converted == "minAxe")
									splitElement[1] = (int.Parse(splitElement[1]) * 5).ToString();
								statField = typeof(TileInfo).GetField("minPick");
								int splitInt = int.Parse(splitElement[1]);
								statField.SetValue(info, splitInt);
								continue;
							}
							case "Width":
							case "Height":
							case "tileShine":
								tileNumberFields.Add(converted, int.Parse(splitElement[1]));
								continue;
							case "tileLighted":
							case "tileMergeDirt":
							case "tileCut":
							case "tileAlch":
							case "tileShine2":
							case "tileStone":
							case "tileWaterDeath":
							case "tileLavaDeath":
							case "tileTable":
							case "tileBlockLight":
							case "tileNoSunLight":
							case "tileDungeon":
							case "tileSolidTop":
							case "tileSolid":
							case "tileNoAttach":
							case "tileNoFail":
							case "tileFrameImportant":
								tileBoolFields.Add(converted, bool.Parse(splitElement[1]));
								continue;
							case "DropName":
								tileStringFields.Add(converted, $"{modName}:{splitElement[1]}");
								continue;
							case "furniture":  {
								tileStringFields.Add(converted, splitElement[1]);
								continue;
							}
							case "id":
							case "type":
							case "mineResist" when splitElement[1] == "0":
								continue;
							default: {
								if (statField == null) {
									mod.Logger.Debug($"Tile field not found or invalid field! -> {converted}");
									logTileAndModName = true;
									tConfigWrapper.ReportErrors = true;
									continue;
								}
								break;
							}
						}

						TypeConverter converter = TypeDescriptor.GetConverter(statField.FieldType);
						object realValue = converter.ConvertFromString(splitElement[1]);
						statField.SetValue(info, realValue);
					}
				}
			}

			string texturePath = Path.ChangeExtension(fileName, "png");
			Texture2D tileTexture = null;
			if (!Main.dedServ && streams.TryGetValue(texturePath, out MemoryStream textureStream)) {
				tileTexture = Texture2D.FromStream(Main.instance.GraphicsDevice, textureStream);
			}

			if (tileTexture != null) {
				BaseTile baseTile = new BaseTile((TileInfo)info, internalName, tileTexture, tileBoolFields, tileNumberFields, tileStringFields);
				tilesToLoad.TryAdd(internalName, (baseTile, "tConfigWrapper/Common/DataTemplates/MissingTexture"));
				tileMapData.TryAdd(baseTile, (oreTile, displayName));
			}

			if (logTileAndModName)
				mod.Logger.Debug($"{internalName}"); //Logs the tile and mod name if "Field not found or invalid field". Mod and tile name show up below the other log lines
		}

		private static int mapIterationCount;
		public static void GetMapEntries() { // Loads tile map entries
			mapIterationCount = 0;
			loadProgressText?.Invoke("tConfig Wrapper: Loading Map Entries");
			loadProgress?.Invoke(0f);
			SetupRecipes(); // Check what tiles are used in recipes so it can add a name to it
			int mapContentCount = tileMapData.Count + wallsToLoad.Count;

			using (CountdownEvent finished = new CountdownEvent(1)) {
				if (mapContentCount != 0) {
					Thread tileMapEntryThread = new Thread(GetTileMapEntries);
					tileMapEntryThread.Start(finished);
					finished.AddCount();

					Thread wallMapEntryThread = new Thread(GetWallMapEntries);
					wallMapEntryThread.Start(finished);
					finished.AddCount();

					finished.Signal();
					finished.Wait();
				}
			}
		}

		private static void GetTileMapEntries(object stateInfo) {
			CountdownEvent countdown = (CountdownEvent)stateInfo;

			foreach (var (modTile, (display, name)) in tileMapData) {
				mapIterationCount++;
				loadSubProgressText?.Invoke(name);
				Texture2D tileTex = Main.tileTexture[modTile.Type];
				Color[] colors = new Color[tileTex.Width * tileTex.Height];
				tileTex.GetData(colors);
				Color[,] colorsGrid = colors.To2DColor(tileTex.Width, tileTex.Height);
				List<Color> noLineColor = new List<Color>();
				//Iterates through the 2D array of colors but it removes unwanted pixels.
				for (int x = 0; x < colorsGrid.GetLength(0); x++) {
					for (int y = 0; y < colorsGrid.GetLength(1); y++) {
						if (colorsGrid[x, y] != new Color(151, 107, 75) && colorsGrid[x, y] != new Color(114, 81, 56) && colorsGrid[x, y] != Color.Black && colorsGrid[x, y].A != 0 && (x + 1) % 18 > 1 && (y + 1) % 18 > 1)
							noLineColor.Add(colorsGrid[x, y]);
					}
				}
				int r = noLineColor.Sum(x => x.R) / noLineColor.Count;
				int g = noLineColor.Sum(x => x.G) / noLineColor.Count;
				int b = noLineColor.Sum(x => x.B) / noLineColor.Count;
				Color averageColor = new Color(r, g, b);

				if (display)
					modTile.AddMapEntry(averageColor, Language.GetText(name));
				else
					modTile.AddMapEntry(averageColor);

				loadProgress?.Invoke(mapIterationCount / (tileMapData.Count + wallsToLoad.Count));
			}
			countdown.Signal();
		}

		private static void GetWallMapEntries(object stateInfo) {
			CountdownEvent countdown = (CountdownEvent)stateInfo;

			foreach (var (wallName, (modWall, texture)) in wallsToLoad) {
				mapIterationCount++;
				loadSubProgressText?.Invoke(wallName);
				Texture2D wallTex = Main.wallTexture[modWall.Type];
				Color[] colors = new Color[wallTex.Width * wallTex.Height];
				wallTex.GetData(colors);
				Color[,] colorsGrid = colors.To2DColor(wallTex.Width, wallTex.Height);
				List<Color> noLineColor = new List<Color>();
				for (int x = 0; x < colorsGrid.GetLength(0); x++) {
					for (int y = 0; y < colorsGrid.GetLength(1); y++) {
						if (colorsGrid[x, y].A != 0 && (x + 3) % 36 > 3 && (y + 3) % 36 > 3)
							noLineColor.Add(colorsGrid[x, y]);
					}
				}
				int r = noLineColor.Sum(x => x.R) / noLineColor.Count;
				int g = noLineColor.Sum(x => x.G) / noLineColor.Count;
				int b = noLineColor.Sum(x => x.B) / noLineColor.Count;
				Color averageColor = new Color(r, g, b);

				modWall.AddMapEntry(averageColor);
				loadProgress?.Invoke(mapIterationCount / (tileMapData.Count + wallsToLoad.Count));
			}
			countdown.Signal();
		}

		private static void CreateProjectile(object stateInfo) { // This is literally for easier multithreading again
			object[] parameters = (object[])stateInfo;
			CountdownEvent countdown = (CountdownEvent)parameters[3];

			foreach (var fileName in (IEnumerable<string>)parameters[0]) {
				loadSubProgressText?.Invoke(fileName);
				CreateProjectile(fileName, (string)parameters[1], (string)parameters[2], (ConcurrentDictionary<string, MemoryStream>)parameters[6]);
				taskCompletedCount++;
				loadProgress?.Invoke((float)taskCompletedCount / (int)parameters[5]);
			}
			countdown.Signal();
		}

		private static void CreateProjectile(string fileName, string modName, string extractPath, ConcurrentDictionary<string, MemoryStream> streams) {
			MemoryStream iniStream = streams[fileName];

			IniFileReader reader = new IniFileReader(iniStream);
			IniFile iniFile = IniFile.FromStream(reader);

			object info = new ProjectileInfo();

			string projectileName = Path.GetFileNameWithoutExtension(fileName);
			string internalName = $"{modName}:{projectileName.RemoveIllegalCharacters()}";
			bool logProjectileAndModName = false;

			foreach (IniFileSection section in iniFile.sections) {
				foreach (IniFileElement element in section.elements) {
					switch (section.Name) {
						case "Stats": {
							var splitElement = element.Content.Split('=');

							var statField = typeof(ProjectileInfo).GetField(splitElement[0]);

							switch (splitElement[0]) {
								case "type": {
									continue;
								}
								default: {
									if (statField == null) {
										mod.Logger.Debug($"Projectile field not found or invalid field! -> {splitElement[0]}");
										logProjectileAndModName = true;
										tConfigWrapper.ReportErrors = true;
										continue;
									}
									break;
								}
							}

							//Conversion garbage
							TypeConverter converter = TypeDescriptor.GetConverter(statField.FieldType);
							object realValue = converter.ConvertFromString(splitElement[1]);
							statField.SetValue(info, realValue);
							break;
						}
					}
				}
			}

			string texturePath = Path.ChangeExtension(fileName, "png");
			Texture2D projectileTexture = null;
			if (!Main.dedServ && streams.TryGetValue(texturePath, out MemoryStream textureStream)) {
				projectileTexture = Texture2D.FromStream(Main.instance.GraphicsDevice, textureStream);
			}

			if (logProjectileAndModName) {
				mod.Logger.Debug($"{internalName}");
			}

			if (projectileTexture != null)
				projectilesToLoad.TryAdd(internalName, new BaseProjectile((ProjectileInfo)info, projectileName, projectileTexture));
			else
				projectilesToLoad.TryAdd(internalName, new BaseProjectile((ProjectileInfo)info, projectileName));
		}

		private static void CreateWall(object stateInfo) {
			object[] parameters = (object[])stateInfo;
			CountdownEvent countDown = (CountdownEvent)parameters[3];

			foreach (var fileName in (IEnumerable<string>)parameters[0]) {
				loadSubProgressText?.Invoke(fileName);
				CreateWall(fileName, (string)parameters[1], (string)parameters[2], (ConcurrentDictionary<string, MemoryStream>)parameters[6]);
				taskCompletedCount++;
				loadProgress?.Invoke((float)taskCompletedCount / (int)parameters[5]);
			}
			countDown.Signal();
		}

		private static void CreateWall(string fileName, string modName, string extractPath, ConcurrentDictionary<string, MemoryStream> streams) {
			MemoryStream iniStream = streams[fileName];

			IniFileReader reader = new IniFileReader(iniStream);
			IniFile iniFile = IniFile.FromStream(reader);

			string internalName = $"{modName}:{Path.GetFileNameWithoutExtension(fileName).RemoveIllegalCharacters()}";
			string dropItem = null;
			string house = null;

			foreach (IniFileSection section in iniFile.sections) {
				foreach (IniFileElement element in section.elements) {
					var splitElement = element.Content.Split('=');
					
					switch (splitElement[0]) {
						case "id":
						case "Blend": {
							if (int.Parse(splitElement[1]) != -1)
								mod.Logger.Debug($"{internalName}.{splitElement[0]} was not -1!");
							continue;
						}
						case "DropName": {
							dropItem = $"{modName}:{splitElement[1].RemoveIllegalCharacters()}";
							continue;
						}
						case "House": {
							house = splitElement[1];
							continue;
						}
					}
				}
			}

			string texturePath = Path.ChangeExtension(fileName, "png");
			Texture2D wallTexture = null;
			if (!Main.dedServ && streams.TryGetValue(texturePath, out MemoryStream textureStream)) {
				wallTexture = Texture2D.FromStream(Main.instance.GraphicsDevice, textureStream);
			}

			if (wallTexture != null)
				wallsToLoad.TryAdd(internalName, (new BaseWall(dropItem, house, wallTexture), "tConfigWrapper/Common/DataTemplates/MissingTexture"));
		}

		private static void CreatePrefix(object stateInfo) {
			object[] parameters = (object[])stateInfo;
			CountdownEvent countDown = (CountdownEvent)parameters[3];

			foreach (var fileName in (IEnumerable<string>)parameters[0]) {
				loadSubProgressText?.Invoke(fileName);
				CreatePrefix(fileName, (string)parameters[1], (string)parameters[2], (ConcurrentDictionary<string, MemoryStream>)parameters[6]);
				taskCompletedCount++;
				loadProgress?.Invoke((float)taskCompletedCount / (int)parameters[5]);
			}
			countDown.Signal();
		}

		private static void CreatePrefix(string fileName, string modName, string extractPath, ConcurrentDictionary<string, MemoryStream> streams) {
			MemoryStream iniStream = streams[fileName];
			Dictionary<string, string> itemFields = new Dictionary<string, string>();
			Dictionary<string, string> playerFields = new Dictionary<string, string>();

			IniFileReader reader = new IniFileReader(iniStream);
			IniFile iniFile = IniFile.FromStream(reader);

			string internalName = $"{modName}:{Path.GetFileNameWithoutExtension(fileName).RemoveIllegalCharacters()}";
			bool addToSuffixBag = false;
			string name = null;
			string requirementType = null;
			string weight = null;

			foreach (IniFileSection section in iniFile.sections) {
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
			}

			BasePrefix prefix = new BasePrefix(name, requirementType, float.Parse(weight ?? "1"), itemFields, playerFields);

			prefixesToLoad.TryAdd(internalName, prefix);
			if (addToSuffixBag) {
				suffixes.Add(prefix);
			}
		}

		internal static void LoadStaticFields() {
			globalItemInfos = new ConcurrentDictionary<int, ItemInfo>();
			recipeDict = new ConcurrentDictionary<string, IniFileSection>();
			itemsToLoad = new ConcurrentDictionary<string, ModItem>();
			tilesToLoad = new ConcurrentDictionary<string, (ModTile, string)>();
			npcsToLoad = new ConcurrentDictionary<string, ModNPC>();
			projectilesToLoad = new ConcurrentDictionary<string, ModProjectile>();
			wallsToLoad = new ConcurrentDictionary<string, (ModWall, string)>();
			prefixesToLoad = new ConcurrentDictionary<string, ModPrefix>();
			suffixes = new ConcurrentBag<ModPrefix>();
			tileMapData = new ConcurrentDictionary<ModTile, (bool, string)>();
		}

		internal static void UnloadStaticFields() {
			files = null;
			loadProgressText = null;
			loadProgress = null; 
			loadSubProgressText = null;
			globalItemInfos = null;
			recipeDict = null;
			itemsToLoad = null;
			tilesToLoad = null;
			npcsToLoad = null;
			wallsToLoad = null;
			prefixesToLoad = null;
			suffixes = null;
			tileMapData = null;
		}
	}
}