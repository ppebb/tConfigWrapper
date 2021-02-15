using Microsoft.Xna.Framework;
using ReLogic.Reflection;
using System.Reflection;
using System;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace tConfigWrapper.Common {
	public class die{

	}

	/// <summary>
	/// A class containing utility methods
	/// </summary>
	public static class Utilities {
		/// <summary>
		/// Removes some illegal characters from a string
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static string RemoveIllegalCharacters(this String str) {
			if (str != null)
				return str.Replace(" ", "").Replace("'", "");
			else
				return str;
		}

		public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> tuple, out T1 key, out T2 value) {
			key = tuple.Key;
			value = tuple.Value;
		}

		public static Color[,] To2DColor(this Color[] colors, int width, int height) {
			Color[,] grid = new Color[height, width];
			for (int row = 0; row < height; row++) {
				for (int column = 0; column < width; column++) {
					grid[row, column] = colors[row * width + column];
				}
			}
			return grid;
		}

		/// <summary>
		/// Converts fields to the 1.3 equivalent
		/// </summary>
		/// <param name="fieldName"></param>
		/// <returns></returns>
		internal static string ConvertField13(string fieldName) {
			switch (fieldName) {
				case "knockBackResist":
					return "knockBackResist";
				case "hitSoundList":
					return "soundStyle";
				case "hitSound":
					return "soundType";
				case "pick":
				case "axe":
				case "hammer":
					return "mineResist";
				case "Shine":
					return "tileShine";
				case "Shine2":
					return "tileShine2";
				case "Lighted":
					return "tileLighted";
				case "MergeDirt":
					return "tileMergeDirt";
				case "Cut":
					return "tileCut";
				case "Alch":
					return "tileAlch";
				case "Stone":
					return "tileStone";
				case "WaterDeath":
					return "tileWaterDeath";
				case "LavaDeath":
					return "tileLavaDeath";
				case "Table":
					return "tileTable";
				case "BlockLight":
					return "tileBlockLight";
				case "NoSunLight":
					return "tileNoSunLight";
				case "Dungeon":
					return "tileDungeon";
				case "SolidTop":
					return "tileSolidTop";
				case "Solid":
					return "tileSolid";
				case "NoAttach":
					return "tileNoAttach";
				default:
					return fieldName;
			}
		}

		/// <summary>
		/// Converts ID strings to the 1.3 equivalent
		/// </summary>
		/// <param name="noSpaceTile"></param>
		/// <returns></returns>
		internal static string ConvertIDTo13(string noSpaceTile) {
			switch (noSpaceTile) {
				case "Anvil":
					return "Anvils";
				case "WorkBench":
				case "Workbench":
					return "WorkBenches";
				case "Furnace":
					return "Furnaces";
				case "TinkerersWorkshop":
					return "TinkerersWorkbench";
				case "Bottle":
					return "Bottles";
				case "Bookcase":
					return "Bookcases";
				case "Table":
					return "Tables";
				default:
					return noSpaceTile;
			}
		}

		/// <summary>
		/// Checks if an ID can be converted to a 1.3 string
		/// </summary>
		/// <param name="noSpaceTile"></param>
		/// <returns></returns>
		internal static bool CheckIDConversion(string noSpaceTile) {
			switch (noSpaceTile) {
				case "Anvil":
				case "WorkBench":
				case "Workbench":
				case "Furnace":
				case "TinkerersWorkshop":
				case "Bottle":
				case "Bookcase":
				case "Table":
					return true;
				default:
					return false;
			}
		}

		internal static void LoadStaticFields() {
			tConfigWrapper.ReportErrors = false;
			tConfigWrapper.ModsPath = Main.SavePath + "\\tConfigWrapper\\Mods";
			LoadStep.LoadStaticFields();
		}

		internal static void UnloadStaticFields() {
			tConfigWrapper.ReportErrors = false;
			tConfigWrapper.ModsPath = null;
			LoadStep.UnloadStaticFields();
		}

		public static ConcurrentDictionary<string, object> searchDict = new ConcurrentDictionary<string, object>();
		public static MethodInfo containsName = typeof(IdDictionary).GetMethod("ContainsName"); // Reflection allows for XContentID.Search.Method
		public static MethodInfo getID = typeof(IdDictionary).GetMethod("GetId");

		/// <summary>
		/// Returns an int from a content string. Returns 0 if it fails.
		/// </summary>
		/// <param name="mod"></param>
		/// <param name="contentIDType">An ID class as a string, such as ItemID, TileID, or NPCID</param>
		/// <param name="modContentMethod">The mod.XType method you want to use, such as ItemType, TileType, or NPCType</param>
		/// <param name="contentString">Should be the internal name of the content, if it is a vanilla ID string passing it in with {modName} in front will still work fine. String should contain no illegal characters</param>
		/// <returns></returns>
		internal static int StringToContent(string contentIDType, string modContentMethod, string contentString) {
			if (contentString == null)
				return 0; // I have to add this because yes
			int contentInt = (int)typeof(Mod).GetMethod(modContentMethod, new Type[] { typeof(string) }).Invoke(LoadStep.mod, new object[] { contentString }); // Is mod.XType
			if (!searchDict.ContainsKey(contentIDType)) {
				object search = typeof(Main).Assembly.GetType($"Terraria.ID.{contentIDType}").GetField("Search", BindingFlags.Static | BindingFlags.Public).GetValue(null);
				searchDict.TryAdd(contentIDType, search);
			}
			string contentStringNoMod = contentString.Split(':')[1]; // Takes something like Avalon:DarkShard which is actually vanilla and makes it vanilla
			if (!CheckIDConversion(contentStringNoMod) && contentInt == 0 && (bool)containsName.Invoke(searchDict[contentIDType], new object[] { contentStringNoMod }) && int.TryParse(contentStringNoMod, out int _)) { // Checks that the ID isn't a vanilla ID (1.3 or 1.1.2 variant) or an existing modded content
				LoadStep.mod.Logger.Debug($"{contentStringNoMod} used by {contentString.Split(':')[0]} does not exist!");
				tConfigWrapper.ReportErrors = true;
			}
			else if (int.TryParse(contentStringNoMod, out int result)) // Returns the parsed string if the string is just a straight number
				return result;
			else if (CheckIDConversion(contentStringNoMod)) // Returns the ID if it is a 1.1.2 ID that can be converted to a 1.3 ID
				return (int)getID.Invoke(searchDict[contentIDType], new object[] { ConvertIDTo13(contentStringNoMod) });
			else if ((bool)containsName.Invoke(searchDict[contentIDType], new object[] { contentStringNoMod })) // Checks if the string doesn't need to be converted and is consistent with the 1.3 ID
				return (int)getID.Invoke(searchDict[contentIDType], new object[] { contentStringNoMod });
			else if (contentInt != 0) // Returns if the content is modded
				return contentInt;
			else {
				LoadStep.mod.Logger.Debug("How was this even triggered");
				tConfigWrapper.ReportErrors = true;
				return 0;
			}
			return 0;
		}

		internal static Color ReadRGBA(this BinaryReader reader) {
			return new Color(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
		}
	}
}