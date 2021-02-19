using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using tConfigWrapper.Common;
using Terraria.UI;
using Terraria;

namespace tConfigWrapper.UI {
	public class SwitchModStateButton : UIElement {
		private readonly Vector2 _position;
		private readonly string _mod;

		public SwitchModStateButton(string mod, Vector2 position, int top, int left) {
			_mod = mod;
			Vector2 size = Main.fontMouseText.MeasureString(ModState.EnabledMods.Contains(_mod) ? "Enabled" : "Disabled");
			_position = position;
			Top.Pixels = top;
			Left.Pixels = left;
			Width.Pixels = size.X;
			Height.Pixels = size.Y;
		}

		public override void Draw(SpriteBatch spriteBatch) {
			spriteBatch.DrawString(Main.fontMouseText, ModState.EnabledMods.Contains(_mod) ? "Enabled" : "Disabled", _position, ModState.EnabledMods.Contains(_mod) ? Color.Green : Color.Red);
		}

		public override void Click(UIMouseEvent evt) {
			base.Click(evt);
			if (ModState.EnabledMods.Contains(_mod))
					ModState.DisableMod(_mod);
				else
					ModState.EnableMod(_mod);
		}
	}
}