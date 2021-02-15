using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace tConfigWrapper.Common.DataTemplates {
	public class BaseGlobalItem : GlobalItem {
		public override void SetDefaults(Item item) {
			if (LoadStep.globalItemInfos.TryGetValue(item.type, out ItemInfo info)) {
				SetDefaultsFromInfo(item, info);
			}
		}

		/// <summary>
		/// Sets the default values of an <see cref="Item"/> by getting the values from <see cref="info"/>
		/// </summary>
		private void SetDefaultsFromInfo(Item item, ItemInfo info) {
			var infoFields = typeof(ItemInfo).GetFields(); // Gets all the fields in ItemInfo
			foreach (FieldInfo field in infoFields) {
				var infoFieldValue = field.GetValue(info); // Gets the value of the field
				var itemField = typeof(Item).GetField(field.Name); // Gets the field with a matching name in Item

				// If the value of infoFieldValue is not null, set the item field to infoFieldValue
				if (infoFieldValue != null)
					itemField.SetValue(item, infoFieldValue);
			}

			if (item.maxStack == 250) // So that anything that had the 1.1.2 max stack will be set for 1.3. Should also prevent mods from reducing existing content stacks from 1.3.
				item.maxStack = 999;
		}
	}
}
