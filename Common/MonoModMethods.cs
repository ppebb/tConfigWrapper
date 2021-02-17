using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using ReLogic.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace tConfigWrapper {
	public partial class tConfigWrapper : Mod {
		public void LoadMethods() {
			Hooks.On_AddMenuButtons += Hooks_On_AddMenuButtons;
			On.Terraria.Main.DrawMenu += Main_DrawMenu;
			On.Terraria.Item.AffixName += Item_AffixName;
		}

		public int drawLogFailMessageTimer;
		private void Main_DrawMenu(On.Terraria.Main.orig_DrawMenu orig, Main self, GameTime gameTime) {
			orig(self, gameTime);
			Main.spriteBatch.Begin();
			if (FailedToSendLogs & drawLogFailMessageTimer < 360) {
				drawLogFailMessageTimer++;
				Main.spriteBatch.DrawString(Main.fontMouseText, "Failed to upload logs\nClick here to try again", new Vector2(25, 10), Color.Cyan);
				Vector2 stringPixelSize = Main.fontMouseText.MeasureString("Failed to upload logs\nClick here to try again");
				Rectangle die = new Rectangle(25, 10, (int)stringPixelSize.X, (int)stringPixelSize.Y);
				if (die.Contains(Main.MouseScreen.ToPoint()) && Main.mouseLeft) {
					UploadLogs(0);
					FailedToSendLogs = false;
				}
			}
			Main.spriteBatch.DrawString(Main.fontMouseText, Main.menuMode.ToString(), new Vector2(10, 10), Color.Cyan);
			Main.spriteBatch.End();
		}

		private void Hooks_On_AddMenuButtons(Hooks.Orig_AddMenuButtons orig, Main main, int selectedMenu, string[] buttonNames, float[] buttonScales, ref int offY, ref int spacing, ref int buttonIndex, ref int numButtons) {
			orig(main, selectedMenu, buttonNames, buttonScales, ref offY, ref spacing, ref buttonIndex, ref numButtons);
			MenuUtils.AddButton("tConfig Mods", delegate {
				Main.MenuUI.SetState(tCFModMenu);
				Main.menuMode = 888;
			}, selectedMenu, buttonNames, ref buttonIndex, ref numButtons);
		}

		private string Item_AffixName(On.Terraria.Item.orig_AffixName orig, Item self) {
			foreach (var prefix in LoadStep.suffixes) {
				if (self.prefix == ModContent.GetInstance<tConfigWrapper>().PrefixType(prefix.Name)) {
					string suffixed = self.Name.Replace($"{prefix.DisplayName.GetDefault()} ", "") + $" {prefix.DisplayName.GetDefault()}";
					return suffixed;
				}
				continue;
			}
			return orig(self);
		}
	}
}
