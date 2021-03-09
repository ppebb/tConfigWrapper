using Gajatko.IniFiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using tConfigWrapper.Common;
using tConfigWrapper.Common.DataTemplates;
using Terraria;
using Terraria.ModLoader;

namespace tConfigWrapper.Loaders {
	internal class WallLoader : BaseLoader {
		private static Dictionary<string, ModWall> _wallData = new Dictionary<string, ModWall>();

		private readonly Dictionary<string, (ModWall wall, string texture)> _wallsToLoad = new Dictionary<string, (ModWall wall, string texture)>();

		public WallLoader(string modName, ConcurrentDictionary<string, MemoryStream> fileStreams) : base(modName, fileStreams) { }

		protected override string TargetFolder => "Wall";

		protected override void HandleFile(string file) {
			MemoryStream iniStream = fileStreams[file];

			IniFileReader reader = new IniFileReader(iniStream);
			IniFile iniFile = IniFile.FromStream(reader);

			string internalName = $"{modName}:{Path.GetFileNameWithoutExtension(file).RemoveIllegalCharacters()}";
			string dropItem = null;
			string house = null;

			foreach (IniFileSection section in iniFile.sections)
			foreach (IniFileElement element in section.elements) {
				var splitElement = element.Content.Split('=');

				switch (splitElement[0]) {
					case "id":
					case "Blend": {
						if (int.Parse(splitElement[1]) != -1)
							Mod.Logger.Debug($"{internalName}.{splitElement[0]} was not -1!");
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

			string texturePath = Path.ChangeExtension(file, "png");
			Texture2D wallTexture = null;
			if (!Main.dedServ && fileStreams.TryGetValue(texturePath, out MemoryStream textureStream)) {
				wallTexture = Texture2D.FromStream(Main.instance.GraphicsDevice, textureStream);
			}

			if (wallTexture != null) {
				BaseWall baseWall = new BaseWall(dropItem, house, wallTexture);
				_wallsToLoad.Add(internalName, (baseWall, "tConfigWrapper/Common/DataTemplates/MissingTexture"));
				_wallData.Add(internalName, baseWall);
			}
		}

		public override void RegisterContent() {
			foreach (var (wallName, (modWall, texture)) in _wallsToLoad) {
				Mod.AddWall(wallName, modWall, texture);
			}
		}

		public override void PostSetupContent() {
			foreach (var (name, modWall) in _wallData) {
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
			}

			_wallData = null;
		}

		public override void InitStatic() => _wallData = new Dictionary<string, ModWall>();
	}
}
