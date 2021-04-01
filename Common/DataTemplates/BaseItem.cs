using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace tConfigWrapper.Common.DataTemplates {
	public class BaseItem : ModItem {
		public override string Texture => "tConfigWrapper/Common/DataTemplates/MissingTexture";
		public override bool CloneNewInstances => true;

		private ItemInfo _info;
		private readonly string _internalName;
		private readonly string _name;
		private readonly string _tooltip;
		private readonly Texture2D _texture;
		private readonly string _createTile;
		private readonly string _shoot;
		private readonly string _createWall;

		public BaseItem() { }

		public BaseItem(ItemInfo itemInfo, string internalName = null, string name = null, string createTile = null, string shoot = null, string createWall = null, string tooltip = null, Texture2D texture = null) {
			_info = itemInfo;
			_internalName = internalName;
			_name = name;
			_tooltip = tooltip;
			_texture = texture;
			_createTile = createTile;
			_createWall = createWall;
			_shoot = shoot;
		}

		public override void SetStaticDefaults() {
			if (_name != null)
				DisplayName.SetDefault(_name);

			if (_tooltip != null)
				Tooltip.SetDefault(_tooltip);

			if (_texture != null)
				Main.itemTexture[item.type] = _texture;
		}

		public override void SetDefaults() {
			SetDefaultsFromInfo();

			if (_createTile != null)
				item.createTile = Utilities.StringToContent("TileID", "TileType", _createTile.RemoveIllegalCharacters());

			if (_createTile != null)
				item.shoot = Utilities.StringToContent("ProjectileID", "ProjectileType", _shoot.RemoveIllegalCharacters());

			if (_createWall != null)
				item.createWall = Utilities.StringToContent("WallID", "WallType", _createWall.RemoveIllegalCharacters());
		}

		public override bool Autoload(ref string name) {
			return false; // Don't autoload since we want to manually create new items
		}

		/// <summary>
		/// Sets the default values of an <see cref="Item"/> by getting the values from <see cref="_info"/>
		/// </summary>
		private void SetDefaultsFromInfo() {
			var infoFields = typeof(ItemInfo).GetFields(); // Gets all the fields in ItemInfo
			foreach (FieldInfo field in infoFields) {
				var infoFieldValue = field.GetValue(_info); // Gets the value of the field
				var itemField = typeof(Item).GetField(field.Name); // Gets the field with a matching name in Item

				// If the value of infoFieldValue is not null, set the item field to infoFieldValue
				if (infoFieldValue != null)
					itemField.SetValue(item, infoFieldValue);
			}
		}
	}
}
