using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.UI;
using Terraria.ModLoader;
using System.IO;
using ReLogic.Graphics;
using Microsoft.Xna.Framework.Graphics;

namespace tConfigWrapper.UI {
	public class TConfigModMenu : UIState {
		public string[] files;
		public override void Update(GameTime gameTime) {
			if (Main.keyState.IsKeyDown(Keys.Escape)) {
				Main.menuMode = 0;
			}
			Terraria.GameInput.PlayerInput.WritingText = true;
			Main.instance.HandleIME();
			base.Update(gameTime);
		}

		public override void Draw(SpriteBatch spriteBatch) {
			files = Directory.GetFiles(tConfigWrapper.ModsPath);
			for (int i = 0; i < files.Length; i++) {
				spriteBatch.DrawString(Main.fontMouseText, files[i], new Vector2(50, 10 + (i * 20)), Color.Cyan);
			}
		}
	}
}