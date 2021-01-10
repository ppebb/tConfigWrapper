using Microsoft.Xna.Framework.Graphics;
using SevenZip;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace tConfigWrapper.DataTemplates {
	public class BaseTile : ModTile {

		private TileInfo _info;
		private readonly string _internalName;
		private SevenZipExtractor _extractor;
		private readonly string _texturePath;

		public BaseTile() { }

		public BaseTile(TileInfo info, string internalName, string texturePath, SevenZipExtractor extractor) {
			_info = info;
			_internalName = internalName;
			_texturePath = texturePath;
			_extractor = extractor;
		}

		public override void SetDefaults() {
			SetDefaultsFromInfo();
		}

		public override void PostSetDefaults() {
			Texture2D tileTexture = null;
			if (_extractor.ArchiveFileNames.Contains(_texturePath)) {
				using (MemoryStream textureSteam = new MemoryStream()) {
					_extractor.ExtractFile(_texturePath, textureSteam);
					textureSteam.Position = 0L;

					tileTexture = Texture2D.FromStream(Main.instance.GraphicsDevice, textureSteam);
				}
			}

			if (tileTexture != null)
				Main.tileTexture[mod.TileType(_internalName)] = tileTexture;
		}

		public override bool Autoload(ref string name, ref string texture) {
			return false;
		}

		/// <summary>
		/// Sets the default values of an <see cref="Tile"/> by getting the values from <see cref="_info"/>
		/// </summary>
		private void SetDefaultsFromInfo() {
			var infoFields = typeof(TileInfo).GetFields();
			foreach (FieldInfo field in infoFields) {
				var infoFieldValue = field.GetValue(_info);
				var tileField = typeof(Tile).GetField(field.Name);

				//if (infoFieldValue != null)
					//tileField.SetValue(    I need an object for this lol      , infoFieldValue);
			}
		}
	}
}
