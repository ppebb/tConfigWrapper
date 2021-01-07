using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.UI;
using Terraria.ModLoader;
using System.IO;
using Gajatko.IniFiles;
using ReLogic.Graphics;
using Microsoft.Xna.Framework.Graphics;
using SevenZip;

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

			using (MemoryStream stream = new MemoryStream())
			{
				//SevenZipBase.SetLibraryPath(tConfigWrapper.SevenDllPath);
				//string tmpPath = Main.SavePath + "\\tConfigWrapper\\tmpFile.7z";
				string tmpPath = Path.Combine(Main.SavePath, "tConfigWrapper", "tmpFile.zip");
				File.Copy(files[0], tmpPath, true);
				FileStream fileStream = File.Open(tmpPath, FileMode.Open);
				using (SevenZipExtractor extractor = new SevenZipExtractor(fileStream))
				{
					MemoryStream configStream = new MemoryStream();
					extractor.ExtractFile("Config.ini", configStream);
					configStream.Position = 0L;
					IniFileReader configReader = new IniFileReader(configStream);
				}
				fileStream.Dispose();
				File.Delete(tmpPath);
			}
		}
	}
}