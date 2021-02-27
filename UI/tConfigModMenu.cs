using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Graphics;
using System.IO;
using System.Reflection;
using tConfigWrapper.Common;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace tConfigWrapper.UI {
	public class tConfigModMenu : UIState {
		public override void OnInitialize() {
			string sE() { return "Exit"; };
			Color cW() { return Color.White; };
			GenericButton exitButton = new GenericButton(sE, new Vector2(50, Main.screenHeight - 100), new Vector2(0.75f, 0.75f), () => { Main.menuMode = 0; }, cW, Main.fontDeathText);
			Append(exitButton);
			string sR() { return "Reload Mods";  };
			GenericButton reloadButton = new GenericButton(sR, new Vector2(50, Main.screenHeight - 140), new Vector2(0.75f, 0.75f), () => { typeof(ModLoader).GetMethod("Reload", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { }); } , cW, Main.fontDeathText);
			Append(reloadButton);
			for (int i = 0; i < ModState.AllMods.Count; i++) {
				string modName = ModState.AllMods[i];
				string s() { return ModState.EnabledMods.Contains(modName) ? "Enabled" : "Disabled"; }
				Color c() { return ModState.EnabledMods.Contains(modName) ? Color.Green : Color.Red; };
				GenericButton switchButton = new GenericButton(s, new Vector2(300, 65 + (i * 20)), Vector2.One, () => { ModState.ToggleMod(modName); }, c, Main.fontMouseText);
				Append(switchButton);
			}
			UITextBox modPackTextBox = new UITextBox(new Vector2(500, 500), Main.fontMouseText.MeasureString("Click to type"), "Click to type", "Press Enter to save the modpack", true);
			Append(modPackTextBox);
		}

		public override void Update(GameTime gameTime) {
			if (Main.keyState.IsKeyDown(Keys.Escape))
				Main.menuMode = 0;
			base.Update(gameTime);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, Main.fontDeathText, "tConfig Mods", new Vector2(50, 10), Color.White, 0f, Vector2.Zero, new Vector2(0.75f, 0.75f));
			for (int i = 0; i < ModState.AllMods.Count; i++) {
				string fileWithoutExt = Path.GetFileNameWithoutExtension(ModState.AllMods[i]);
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, Main.fontMouseText, fileWithoutExt, new Vector2(50, 65 + (i * 20)), Color.White, 0f, Vector2.Zero, Vector2.One);
				if (ModState.ChangedMods.Contains(ModState.AllMods[i]))
					ChatManager.DrawColorCodedStringWithShadow(spriteBatch, Main.fontMouseText, "* Reload Required!", new Vector2(400, 65 + (i * 20)), Color.Red, 0f, Vector2.Zero, Vector2.One);
			}
		}
	}
}