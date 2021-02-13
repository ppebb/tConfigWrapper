using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace tConfigWrapper.Common.DataTemplates {

	public class BaseNPC : ModNPC {
		public override string Texture => "tConfigWrapper/Common/DataTemplates/MissingTexture";
		public override bool CloneNewInstances => true;

		private NpcInfo _info;
		private readonly string _name;
		private readonly Texture2D _texture;
		private readonly List<(int, int?, string, float)> _dropList = new List<(int, int?, string, float)>();

		public BaseNPC() { }

		public BaseNPC(NpcInfo npcInfo, List<(int, int?, string, float)> dropList, string name = null, Texture2D texture = null) {
			_info = npcInfo;
			_name = name;
			_dropList = dropList;
			_texture = texture;
		}

		public override void SetStaticDefaults() {
			if (_name != null)
				DisplayName.SetDefault(_name);

			if (_texture != null)
				Main.npcTexture[npc.type] = _texture;
		}

		public override void SetDefaults() {
			SetDefaultsFromInfo();
		}

		public override bool Autoload(ref string name) {
			return false;
		}

		/// <summary>
		/// Sets the default values of an <see cref="NPC"/> by getting the values from <see cref="_info"/>
		/// </summary>
		private void SetDefaultsFromInfo() {
			var infoFields = typeof(NpcInfo).GetFields();
			foreach (FieldInfo field in infoFields) {
				var infoFieldValue = field.GetValue(_info);
				var npcField = typeof(NPC).GetField(field.Name);

				if (infoFieldValue != null)
					npcField.SetValue(npc, infoFieldValue);
			}
		}

		public override void NPCLoot() {
			foreach (var (min, max, item, chance) in _dropList) {
				int dropInt = Utilities.StringToContent("ItemID", "ItemType", item);
				if (Main.rand.NextFloat() < (chance)) { // 
					if (max != null)
						Item.NewItem(npc.getRect(), dropInt, Main.rand.Next(min, (int)max));
					else
						Item.NewItem(npc.getRect(), dropInt, min);
				}
			}
		}
	}
}
