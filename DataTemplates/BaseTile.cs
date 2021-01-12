using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tConfigWrapper.DataTemplates {
	public class BaseTile : ModTile {

		private TileInfo _info;
		private readonly string _internalName;
		private readonly Texture2D _texture;
		public Color modeColor;

		public BaseTile() { }

		public BaseTile(TileInfo info, string internalName, Texture2D texture) {
			_info = info;
			_internalName = internalName;
			_texture = texture;
		}

		public override void SetDefaults() {
			SetDefaultsFromInfo();
			/*Color[] colors = new Color[_texture.Width * _texture.Height];
			_texture.GetData(colors);
			int r = colors.Sum(x => x.R) / colors.Length;
			int g = colors.Sum(x => x.G) / colors.Length;
			int b = colors.Sum(x => x.B) / colors.Length;
			int a = colors.Sum(x => x.A) / colors.Length;
			var mainColor = colors.GroupBy(col => new Color(col.R, col.G, col.B))
				.OrderByDescending(grp => grp.Count())
				.Where(grp => grp.Key.R != 0 || grp.Key.G != 0 || grp.Key.B != 0)
				.Select(grp => grp.Key)
				.First();
			AddMapEntry(mainColor, Language.GetText(_internalName));*/
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
