using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace tConfigWrapper.DataTemplates
{
	public class BaseItem : ModItem
	{
		public override string Texture => "tConfigWrapper/DataTemplates/MissingTexture";
		public override bool CloneNewInstances => true;

		private ItemInfo _info;
		private string _name;
		private string _tooltip;
		private Texture2D _texture;

		public BaseItem()
		{
		}

		public BaseItem(ItemInfo itemInfo, string name = null, string tooltip = null, Texture2D texture = null)
		{
			_info = itemInfo;
			_name = name;
			_tooltip = tooltip;
			_texture = texture;
		}

		public override void SetStaticDefaults()
		{
			if (_name != null)
				DisplayName.SetDefault(_name);

			if (_tooltip != null)
				Tooltip.SetDefault(_tooltip);

			if (_texture != null)
				Main.itemTexture[item.type] = _texture;
		}

		public override void SetDefaults()
		{
			SetDefaultsFromInfo();
		}

		public override bool Autoload(ref string name)
		{
			return false; // Don't autoload since we want to manually create new items
		}

		/// <summary>
		/// Sets the default values of an <see cref="Item"/> by getting the values from <see cref="_info"/>
		/// </summary>
		private void SetDefaultsFromInfo()
		{
			var infoFields = typeof(ItemInfo).GetFields(); // Gets all the fields in ItemInfo
			foreach (FieldInfo field in infoFields)
			{
				var infoFieldValue = field.GetValue(_info); // Gets the value of the field
				var itemField = typeof(Item).GetField(field.Name); // Gets the field with a matching name in Item

				// If the value of infoFieldValue is not null, set the item field to infoFieldValue
				if (infoFieldValue != null)
					itemField.SetValue(item, infoFieldValue);
			}
		}
	}
}
