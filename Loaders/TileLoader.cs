using Gajatko.IniFiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using tConfigWrapper.Common;
using tConfigWrapper.Common.DataTemplates;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tConfigWrapper.Loaders {
	internal class TileLoader : BaseLoader {
		private static Dictionary<ModTile, (bool display, string name)> _tileMapData = new Dictionary<ModTile, (bool, string)>();

		private readonly Dictionary<string, (ModTile tile, string texture)> _tilesToLoad = new Dictionary<string, (ModTile tile, string texture)>();

		public TileLoader(string modName, ConcurrentDictionary<string, MemoryStream> fileStreams) : base(modName, fileStreams) { }

		protected override string TargetFolder => "Tile";

		protected override void HandleFile(string file) {
			Dictionary<string, int> tileNumberFields = new Dictionary<string, int>();
			Dictionary<string, bool> tileBoolFields = new Dictionary<string, bool>();
			Dictionary<string, string> tileStringFields = new Dictionary<string, string>();

			MemoryStream iniStream = fileStreams[file];

			IniFileReader reader = new IniFileReader(iniStream);
			IniFile iniFile = IniFile.FromStream(reader);

			object info = new TileInfo();

			string displayName = Path.GetFileNameWithoutExtension(file);
			string internalName = $"{modName}:{displayName.RemoveIllegalCharacters()}";
			bool logTileAndModName = false;
			bool oreTile = false;

			foreach (IniFileSection section in iniFile.sections)
			foreach (IniFileElement element in section.elements) {
				if (section.Name != "Stats")
					continue;

				var splitElement = element.Content.Split('=');

				string converted = Utilities.ConvertField13(splitElement[0]);
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
					case "furniture": {
							tileStringFields.Add(converted, splitElement[1]);
							continue;
						}
					case "id":
					case "type":
					case "mineResist" when splitElement[1] == "0":
						continue;
					default: {
							if (statField == null) {
								Mod.Logger.Debug($"Tile field not found or invalid field! -> {converted}");
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

			string texturePath = Path.ChangeExtension(file, "png");
			Texture2D tileTexture = null;
			if (!Main.dedServ && fileStreams.TryGetValue(texturePath, out MemoryStream textureStream)) {
				tileTexture = Texture2D.FromStream(Main.instance.GraphicsDevice, textureStream);
			}

			if (tileTexture != null) {
				BaseTile baseTile = new BaseTile((TileInfo)info, internalName, tileTexture, tileBoolFields, tileNumberFields, tileStringFields);
				_tilesToLoad.Add(internalName, (baseTile, "tConfigWrapper/Common/DataTemplates/MissingTexture"));
				_tileMapData.Add(baseTile, (oreTile, displayName));
			}

			if (logTileAndModName)
				Mod.Logger.Debug($"{internalName}"); //Logs the tile and mod name if "Field not found or invalid field". Mod and tile name show up below the other log lines
		}

		public override void RegisterContent() {
			foreach (var (tileName, (tile, texture)) in _tilesToLoad) {
				Mod.AddTile(tileName, tile, texture);
			}
		}

		public override void PostSetupContent() {
			UpdateDisplayMap();

			foreach (var (modTile, (display, name)) in _tileMapData) {
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
			}

			_tileMapData = null;
		}

		private void UpdateDisplayMap() {
			foreach (var (itemName, fileSection) in LoadStep.recipeDict)
			foreach (var element in fileSection.elements) {
				string[] splitElement = element.Content.Split('=');
				string key = splitElement[0];
				string value = splitElement[1];

				if (key != "Tiles")
					continue;

				foreach (string recipeTile in value.Split(',')) {
					string recipeTileIR = recipeTile.RemoveIllegalCharacters();
					int tileInt = Mod.TileType($"{modName}:{recipeTileIR}");
					ModTile tileModTile = Mod.GetTile($"{modName}:{recipeTileIR}");

					if (!TileID.Search.ContainsName(recipeTileIR) && !Utilities.CheckIDConversion(recipeTileIR) &&
					    tileInt == 0 && tileModTile == null)
						continue;

					if (Utilities.CheckIDConversion(recipeTileIR) || TileID.Search.ContainsName(recipeTileIR))
						continue;

					if (tileInt == 0)
						continue;

					_tileMapData[tileModTile] = (true, _tileMapData[tileModTile].name);
				}
			}
		}
	}
}