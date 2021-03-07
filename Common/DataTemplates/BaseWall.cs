using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace tConfigWrapper.Common.DataTemplates {
	public class BaseWall : ModWall {
		// Walls have like 3 fields and they're all handled differently so no dictionaries or info are needed
		private readonly string _drop;
		private readonly string _house;
		private readonly Texture2D _texture;

		public BaseWall() { }

		public BaseWall(string drop = null, string house = null, Texture2D texture = null) {
			_drop = drop;
			_house = house;
			_texture = texture;
		}

		public override bool Autoload(ref string name, ref string texture) {
			return false;
		}

		public override void SetDefaults() {
			dustType = -1;

			if (_texture != null)
				Main.wallTexture[Type] = _texture;

			if (_drop != null)
				drop = Utilities.StringToContent("ItemID", "ItemType", _drop);

			if (_house != null)
				Main.wallHouse[Type] = bool.Parse(_house);
		}
	}
}
