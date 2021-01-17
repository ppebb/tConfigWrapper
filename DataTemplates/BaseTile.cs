using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ObjectData;
using Terraria.ModLoader;

namespace tConfigWrapper.DataTemplates {
	public class BaseTile : ModTile {

		private TileInfo _info;
		private readonly string _internalName;
		private readonly Texture2D _texture;
		private readonly Dictionary<string, bool> _tileBoolFields = new Dictionary<string, bool>();
		private readonly Dictionary<string, int> _tileNumberFields = new Dictionary<string, int>();

		public BaseTile() { }

		public BaseTile(TileInfo info, string internalName, Texture2D texture, Dictionary<string, bool> tileBoolFields, Dictionary<string, int> tileNumberFields) {
			_info = info;
			_internalName = internalName;
			_texture = texture;
			_tileBoolFields = tileBoolFields;
			_tileNumberFields = tileNumberFields;
		}

		public override void SetDefaults() {
			SetDefaultsFromInfo();
			_tileBoolFields.TryGetValue("FrameImportant", out bool frameImportant);
			if (frameImportant) {
				TileObjectData.newTile.Width = _tileNumberFields["Width"];
				TileObjectData.newTile.Height = _tileNumberFields["Height"];
				TileObjectData.addTile(Type);
			}
			foreach (var field in _tileBoolFields) {
				if (field.Key != "chair" || field.Key != "table" || field.Key != "torch" || field.Key != "door") { // this code is probably incredibly slow, oh well!
					FieldInfo statField = typeof(Main).GetField(field.Key);
					if (statField != null) {
						bool[] mainArray = (bool[])statField.GetValue(null);
						mainArray[Type] = field.Value;
						statField.SetValue(null, mainArray);
					}
				}
			}
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
				var tileField = typeof(ModTile).GetField(field.Name);

				if (infoFieldValue != null)
					tileField.SetValue(this, infoFieldValue);
			}
		}
	}
}
