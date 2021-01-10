using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace tConfigWrapper.DataTemplates {

	public class BaseNPC : ModNPC {
		public override string Texture => "tConfigWrapper/DataTemplates/MissingTexture";
		public override bool CloneNewInstances => true;

		private NpcInfo _info;
		private readonly string _name;
		private readonly Texture2D _texture;

		public BaseNPC() { }

		public BaseNPC(NpcInfo npcInfo, string name = null, Texture2D texture = null) {
			_info = npcInfo;
			_name = name;
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
	}
}
