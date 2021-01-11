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
		private Texture2D _texture;

		public BaseTile() { }

		public BaseTile(TileInfo info, string internalName, Texture2D texture) {
			_info = info;
			_internalName = internalName;
			_texture = texture;
		}

		public override void SetDefaults() {
			SetDefaultsFromInfo();
		}

		public override void PostSetDefaults() {
			Main.tileTexture[mod.TileType(_internalName)] = _texture;
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

				if (infoFieldValue != null)
					tileField.SetValue(this, infoFieldValue);
			}
		}
	}
}
