using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Graphics;
using System.IO;
using tConfigWrapper.Common;
using Terraria;
using Terraria.UI;

namespace tConfigWrapper.UI {
	public class TConfigModMenu : UIState {
		public override void OnInitialize() {
			for (int i = 0; i < ModState.AllMods.Count; i++) {
				SwitchModStateButton switchButton = new SwitchModStateButton(ModState.AllMods[i], new Vector2(250, 10 + (i * 20)));
				Append(switchButton);
			}
		}

		public override void Update(GameTime gameTime) {
			if (Main.keyState.IsKeyDown(Keys.Escape))
				Main.menuMode = 0;

			base.Update(gameTime);
		}

		public override void Draw(SpriteBatch spriteBatch) {
			for (int i = 0; i < ModState.AllMods.Count; i++) {
				string fileWithoutExt = Path.GetFileNameWithoutExtension(ModState.AllMods[i]);
				spriteBatch.DrawString(Main.fontMouseText, fileWithoutExt, new Vector2(50, 10 + (i * 20)), Color.Cyan);
			}
		}
	}
}