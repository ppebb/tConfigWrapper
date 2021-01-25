using ReLogic.Reflection;
using System.Reflection;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tConfigWrapper {
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
			return str.Replace(" ", "").Replace("'", "");
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
				case "Tinkerer'sWorkshop":
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
				case "Tinkerer'sWorkshop":
				case "Bottle":
				case "Bookcase":
				case "Table":
					return true;
				default:
					return false;
			}
		}

		internal static void UnloadStaticFields() {
			tConfigWrapper.ReportErrors = false;
			tConfigWrapper.ModsPath = null;
			LoadStep.UnloadStaticFields();
		}

		/// <summary>
		/// Returns an int from a content string. Returns 0 if it fails.
		/// </summary>
		/// <param name="mod"></param>
		/// <param name="contentIDType">An ID class as a string, such as ItemID, TileID, or NPCID</param>
		/// <param name="modContentMethod">The mod.XType method you want to use, such as ItemType, TileType, or NPCType</param>
		/// <param name="contentString">Should be the internal name of the content</param>
		/// <returns></returns>
		internal static int StringToContent(Mod mod, string contentIDType, string modContentMethod, string contentString) {
			MethodInfo containsName = typeof(IdDictionary).GetMethod("ContainsName"); // Reflection allows for XContentID.Search.Method
			MethodInfo getID = typeof(IdDictionary).GetMethod("GetId");
			int contentInt = (int)typeof(Mod).GetMethod(modContentMethod, new Type[] { typeof(string) }).Invoke(mod, new object[] { contentString }); // Is mod.XType
			var search = typeof(Main).Assembly.GetType($"Terraria.ID.{contentIDType}").GetField("Search", BindingFlags.Static | BindingFlags.Public).GetValue(null);
			if (!(bool)containsName.Invoke(search, new object[] { contentString.Split(':')[1] }) && !CheckIDConversion(contentString) && contentInt == 0) { // Checks that the ID doesn't exist, can't be converted to a 1.3 ID, and isn't mod content
				mod.Logger.Debug($"{contentIDType} {contentString} does not exist");
				return 0;
			}
			else if (CheckIDConversion(contentString)) { // Checks if contentString is a vanilla ID that can be converted to 1.3
				contentString = ConvertIDTo13(contentString);
				return (int)getID.Invoke(search, new object[] { contentString });
			}
			else if (contentInt != 0) { // This if check is useless but I still have it because yes
				return contentInt;
			}
			return 0;
		}
	}
}