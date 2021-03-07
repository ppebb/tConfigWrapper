using Gajatko.IniFiles;
using Mono.Cecil;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using tConfigWrapper.Common;
using tConfigWrapper.Common.DataTemplates;
using tConfigWrapper.Loaders;
using static tConfigWrapper.Common.Utilities;
using Terraria.ID;
using Terraria.ModLoader;

namespace tConfigWrapper {
	public static class LoadStep {
		private static Action<string> _loadProgressText; // Heading text during loading
		private static Action<float> _loadProgress; // Progress bar during loading: 0-1 scale
		private static Action<string> _loadSubProgressText; // Subtext during loading
		public static int TaskCompletedCount; // Int used for tracking load progress during content loading
		internal static ConcurrentDictionary<int, ItemInfo> globalItemInfos = new ConcurrentDictionary<int, ItemInfo>(); // Dictionaries are selfexplanatory, concurrent so that multiple threads can access them without dying
		internal static ConcurrentDictionary<string, IniFileSection> recipeDict = new ConcurrentDictionary<string, IniFileSection>();
		internal static ConcurrentBag<ModPrefix> suffixes = new ConcurrentBag<ModPrefix>();
		internal static ConcurrentDictionary<string, MemoryStream> streamsGlobal = new ConcurrentDictionary<string, MemoryStream>();
		internal static string CurrentLoadingMod;

		internal static Mod Mod => ModContent.GetInstance<tConfigWrapper>();

		public static void Setup() {
			recipeDict.TryGetValue("", out _); // Sanity check to make sure it's initialized
			// Cringe reflection
			Assembly assembly = Assembly.GetAssembly(typeof(Mod));
			Type uiLoadModsType = assembly.GetType("Terraria.ModLoader.UI.UILoadMods");

			object loadModsValue = assembly.GetType("Terraria.ModLoader.UI.Interface")
				.GetField("loadMods", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
			MethodInfo loadStageMethod = uiLoadModsType.GetMethod("SetLoadStage", BindingFlags.Instance | BindingFlags.Public);
			PropertyInfo progressProperty = uiLoadModsType.GetProperty("Progress", BindingFlags.Instance | BindingFlags.Public);
			PropertyInfo subProgressTextProperty =
				uiLoadModsType.GetProperty("SubProgressText", BindingFlags.Instance | BindingFlags.Public);

			_loadProgressText = (string s) => loadStageMethod.Invoke(loadModsValue, new object[] {s, -1});
			_loadProgress = (float f) => progressProperty.SetValue(loadModsValue, f);
			_loadSubProgressText = (string s) => subProgressTextProperty.SetValue(loadModsValue, s);

			_loadProgressText?.Invoke("tConfig Wrapper: Loading Mods");
			_loadProgress?.Invoke(0f);

			foreach (var modName in ModState.AllMods) {
				if (ModState.EnabledMods.Contains(Path.GetFileNameWithoutExtension(modName)))
					Mod.Logger.Debug($"tConfig Mod: {Path.GetFileNameWithoutExtension(modName)} is enabled!"); // Writes all mod names to logs
			}

			for (int i = 0; i < ModState.EnabledMods.Count; i++) {
				// Iterates through every mod
				string currentMod = ModState.EnabledMods[i];
				string currentModNoExt = Path.GetFileNameWithoutExtension(ModState.EnabledMods[i]);
				CurrentLoadingMod = currentModNoExt;

				_loadProgressText?.Invoke(
					$"tConfig Wrapper: Loading {currentModNoExt}"); // Sets heading text to display the mod being loaded
				Mod.Logger.Debug($"Loading tConfig Mod: {currentModNoExt}"); // Logs the mod being loaded
				Mod.Logger.Debug($"Loading Content: {currentModNoExt}");

				ConcurrentDictionary<string, MemoryStream> streams = new ConcurrentDictionary<string, MemoryStream>();
				Decompressor.DecompressMod(currentMod, streams); // Decompresses mods since .obj files are literally just 7z files

				streamsGlobal.Clear();
				streamsGlobal = streams;
				TaskCompletedCount = 0;

				// Get the first stream that is an obj file
				var obj = streams.First(s => s.Key.EndsWith(".obj"));
				BinaryReader reader = new BinaryReader(obj.Value);

				// Create an Obj Loader and load the obj
				var loader = new ObjLoader(reader, currentModNoExt);
				loader.LoadObj(); // This was causing errors for some reason.

				// Get all classes that extend BaseLoader and make an instance of them
				var loaderInstances = GetLoaders(currentModNoExt, streams).ToArray();
				int contentCount = 0;
				List<Task> loadTasks = new List<Task>();

				// Call AddFiles for all loaders and add to the content amount
				CallMethodAsync(loaderInstances, baseLoader => contentCount += baseLoader.AddFiles(streams.Keys));

				//// Make a task for each loader and call IterateFiles
				CallMethodAsync(loaderInstances, baseLoader => baseLoader.IterateFiles(contentCount));

				// Call RegisterContent for each loader
				CallMethodAsync(loaderInstances, baseLoader => baseLoader.RegisterContent());

				// Dispose the streams
				foreach (var memoryStream in streams) {
					memoryStream.Value.Dispose();
				}
			}

			//Reset progress bar
			_loadSubProgressText?.Invoke("");
			_loadProgressText?.Invoke("Loading mod");
			_loadProgress?.Invoke(0f);
			CurrentLoadingMod = null;
		}

		private static IEnumerable<BaseLoader> GetLoaders(string modName, ConcurrentDictionary<string, MemoryStream> fileStreams) {
			Type baseType = typeof(BaseLoader);
			var childTypes = Mod.Code.GetTypes().Where(p => baseType.IsAssignableFrom(p) && !p.IsAbstract);

			foreach (Type childType in childTypes) {
				yield return (BaseLoader)Activator.CreateInstance(childType, modName, fileStreams);
			}
		}

		/// <summary>
		/// Calls <paramref name="method"/> for every T in <paramref name="objects"/>
		/// </summary>
		private static void CallMethodAsync<T>(IEnumerable<T> objects, Action<T> method) {
			List<Task> tasks = new List<Task>();

			// Make a task and run method for every thing in objects 
			foreach (T thing in objects) {
				tasks.Add(Task.Run(() => method.Invoke(thing)));
			}

			// Wait for all tasks to finish
			Task.WaitAll(tasks.ToArray());
		}

		internal static void AddRecipes() {
			LoadStep.SetupRecipes();

			var loaders = GetLoaders(null, null);
			CallMethodAsync(loaders, loader => loader.AddRecipes());
		}

		internal static void PostSetupContent() {
			var loaders = GetLoaders(null, null);
			CallMethodAsync(loaders, loader => loader.PostSetupContent());
		}

		public static void LoadAssembly(object stateInfo) {
			object[] parameters = (object[])stateInfo;
			CountdownEvent finished = (CountdownEvent)parameters[0];

			ModuleDefinition module = AssemblyLoader.GetModule(Path.GetFileNameWithoutExtension((string)parameters[2]));
			AssemblyLoader.FixIL((string)parameters[1], module);

			finished.Signal();
		}

		private static void SetupRecipes() { // Sets up recipes, what were you expecting?
			_loadProgressText.Invoke("tConfig Wrapper: Adding Recipes"); // Ah yes, more reflection
			_loadProgress.Invoke(0f);
			int progressCount = 0;
			bool initialized = (bool)Assembly.GetAssembly(typeof(Mod)).GetType("Terraria.ModLoader.MapLoader").GetField("initialized", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null); // Check if the map is already initialized
			foreach (var iniFileSection in recipeDict) { // Load every recipe in the recipe dict
				progressCount++; // Count the number of recipes, still broken somehow :(
				string modName = iniFileSection.Key.Split(':')[0];
				ModRecipe recipe = null;
				if (initialized) // Only make the recipe if the maps have already been initialized. The checks for initialized are because I run this method in GetTileMapEntires() to see what tiles are used in recipes and need to have a name in their map entry
					recipe = new ModRecipe(Mod);
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
								recipe?.SetResult(Mod, iniFileSection.Key, int.Parse(value));
							break;
						}
						case "needWater" when initialized:
							recipe.needWater = bool.Parse(value);
							break;
						case "Items" when initialized: {
							foreach (string recipeItem in value.Split(',')) {
								var recipeItemInfo = recipeItem.Split(null, 2);
								int amount = int.Parse(recipeItemInfo[0]);

								int itemID = Mod.ItemType($"{modName}:{recipeItemInfo[1].RemoveIllegalCharacters()}");
								if (itemID == 0)
									itemID = ItemID.FromLegacyName(recipeItemInfo[1], 4);

								var numberIngredients =
									recipe?.requiredItem.Count(i => i != null & i.type != ItemID.None);
								if (numberIngredients < 14)
									recipe?.AddIngredient(itemID, amount);
								else {
									Mod.Logger.Debug($"The following item has exceeded the max ingredient limit! -> {iniFileSection.Key}");
									tConfigWrapper.ReportErrors = true;
								}
							}
							break;
						}
						case "Tiles": { // Does stuff to check for modtiles and vanilla tiles that have changed their name since 1.1.2
							foreach (string recipeTile in value.Split(',')) {
								string recipeTileIR = recipeTile.RemoveIllegalCharacters();
								int tileInt = Mod.TileType($"{modName}:{recipeTileIR}");
								var tileModTile = Mod.GetTile($"{modName}:{recipeTileIR}");
								if (!TileID.Search.ContainsName(recipeTileIR) && !CheckIDConversion(recipeTileIR) && tileInt == 0 && tileModTile == null) { // Would love to replace this with Utilities.StringToContent() but this one is special and needs to add stuff to a dictionary so I can't
									if (initialized) {
										Mod.Logger.Debug($"TileID {modName}:{recipeTileIR} does not exist"); // We will have to manually convert anything that breaks lmao
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
										Mod.Logger.Debug($"{modName}:{recipeTileIR} added to recipe through mod.TileType!");
									}
									//else {
									//	tileMapData[tileModTile] = (true, tileMapData[tileModTile].Item2); // I do this because either I can't just change Item1 directly to true OR because I am very not smart and couldn't figure out how to set it individually.
									//}
								}
							}
							break;
						}
					}
				}

				if (recipe?.createItem != null && recipe?.createItem.type != ItemID.None && initialized)
					recipe?.AddRecipe();
				if (initialized)
					_loadProgress.Invoke(progressCount / recipeDict.Count);
			}
		}

		// TODO: Move this method
		public static void UpdateSubProgressText(string newText) {
			_loadSubProgressText?.Invoke(newText);
		}

		public static void UpdateProgress(float newProgress) {
			_loadProgress?.Invoke(newProgress);
		}

		internal static void LoadStaticFields() {
			globalItemInfos = new ConcurrentDictionary<int, ItemInfo>();
			recipeDict = new ConcurrentDictionary<string, IniFileSection>();
			suffixes = new ConcurrentBag<ModPrefix>();
		}

		internal static void UnloadStaticFields() {
			_loadProgressText = null;
			_loadProgress = null; 
			_loadSubProgressText = null;
			globalItemInfos = null;
			recipeDict = null;
			suffixes = null;
		}
	}
}