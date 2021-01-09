using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Graphics;
using System.IO;
using Terraria;
using Terraria.UI;

namespace tConfigWrapper.UI {
	public class TConfigModMenu : UIState {
		public string[] files;
		public override void Update(GameTime gameTime) {
			if (Main.keyState.IsKeyDown(Keys.Escape))
				Main.menuMode = 0;

			base.Update(gameTime);
		}

		public override void Draw(SpriteBatch spriteBatch) {
			files = Directory.GetFiles(tConfigWrapper.ModsPath);
			for (int i = 0; i < files.Length; i++) {
				string fileWithoutExt = Path.GetFileName(files[i]);
				spriteBatch.DrawString(Main.fontMouseText, fileWithoutExt.Split('.')[0], new Vector2(50, 10 + (i * 20)), Color.Cyan);
			}
		}
	}
}