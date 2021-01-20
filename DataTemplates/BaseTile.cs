using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Reflection;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ObjectData;
using Terraria.ModLoader;

namespace tConfigWrapper.DataTemplates {
	public class BaseTile : ModTile {

		private TileInfo _info;
		private readonly string _internalName;
		private readonly Texture2D _texture;
		private readonly Dictionary<string, bool> _tileBoolFields = new Dictionary<string, bool>();
		private readonly Dictionary<string, int> _tileNumberFields = new Dictionary<string, int>();
		private readonly Dictionary<string, string> _tileStringFields = new Dictionary<string, string>();

		public BaseTile() { }

		public BaseTile(TileInfo info, string internalName, Texture2D texture, Dictionary<string, bool> tileBoolFields, Dictionary<string, int> tileNumberFields, Dictionary<string, string> tileStringFields) {
			_info = info;
			_internalName = internalName;
			_texture = texture;
			_tileBoolFields = tileBoolFields;
			_tileNumberFields = tileNumberFields;
			_tileStringFields = tileStringFields;
		}

		public override void SetDefaults() {
			SetDefaultsFromInfo();
			_tileBoolFields.TryGetValue("tileFrameImportant", out bool frameImportant);
			if (frameImportant) {
				TileObjectData.newTile.Width = _tileNumberFields["Width"];
				TileObjectData.newTile.Height = _tileNumberFields["Height"];
				TileObjectData.addTile(Type);
			}
			foreach (var field in _tileBoolFields) { // this code is probably incredibly slow, oh well!
				FieldInfo statField = typeof(Main).GetField(field.Key);
				if (statField != null) {
					bool[] mainArray = (bool[])statField.GetValue(null);
					mainArray[Type] = field.Value;
					statField.SetValue(null, mainArray);
				}
				else
					mod.Logger.Debug($"Tile field {field.Key} does not exist in Main!");
			}
			foreach (var field in _tileStringFields) {
				if (field.Key == "furniture") {
					string ech = char.ToUpper(field.Value[0]) + field.Value.Substring(1);
					FieldInfo statField = typeof(TileID.Sets.RoomNeeds).GetField($"CountsAs{ech}");
					int[] needsArray = (int[])statField.GetValue(null);
					AddToArray(ref needsArray);
					statField.SetValue(null, needsArray);
				}
			}
		}

		public override void PostSetDefaults() {
			Main.tileTexture[Type] = _texture;
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