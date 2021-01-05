using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI;
using Terraria.ModLoader;
using SevenZip;
using System.IO;
using System.Collections.Generic;
using ReLogic.Graphics;

namespace tConfigWrapper {
	public class tConfigWrapper : Mod {
		internal UI.TConfigModMenu test;
		private UserInterface _test;
		public override void Load() {
			Directory.CreateDirectory(Main.SavePath + "/tConfigWrapper/Mods/ModSettings");
			Hooks.On_AddMenuButtons += Hooks_On_AddMenuButtons;
			On.Terraria.Main.DrawMenu += Main_DrawMenu;
			test = new UI.TConfigModMenu();
			test.Activate();
			_test = new UserInterface();
			_test.SetState(test);
		}

		public override void UpdateUI(GameTime gameTime) {
			_test?.Update(gameTime);
		}

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
			int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
			if (mouseTextIndex != -1) {
				layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
					"tConfigWrapper: A Description",
					delegate {
						_test.Draw(Main.spriteBatch, new GameTime());
						return true;
					},
					InterfaceScaleType.UI)
				);
			}
		}

		private void Main_DrawMenu(On.Terraria.Main.orig_DrawMenu orig, Main self, GameTime gameTime) {
			orig(self, gameTime);
			Main.spriteBatch.Begin();
			Main.spriteBatch.DrawString(Main.fontMouseText, Main.menuMode.ToString(), new Vector2(10, 10), Color.Cyan);
			Main.spriteBatch.End();
		}

		private void Hooks_On_AddMenuButtons(Hooks.Orig_AddMenuButtons orig, Main main, int selectedMenu, string[] buttonNames, float[] buttonScales, ref int offY, ref int spacing, ref int buttonIndex, ref int numButtons) {
			orig(main, selectedMenu, buttonNames, buttonScales, ref offY, ref spacing, ref buttonIndex, ref numButtons);
			MenuUtils.AddButton("tConfig Mods", delegate {
				Main.MenuUI.SetState(test);
				Main.menuMode = 888;
			}, selectedMenu, buttonNames, ref buttonIndex, ref numButtons);
		}
	}
}