using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI;
using Terraria.ModLoader;
using SevenZip;
using System;
using System.IO;
using System.Collections.Generic;
using ReLogic.Graphics;
using tConfigWrapper.UI;

namespace tConfigWrapper {
	public class tConfigWrapper : Mod {
		public static string ModsPath = Main.SavePath + "\\tConfigWrapper\\Mods";
		public static string SevenDllPath => Path.Combine(Main.SavePath, "tConfigWrapper", Environment.Is64BitProcess ? "7z64.dll" : "7z.dll");

		internal TConfigModMenu tCFModMenu;
		private UserInterface _tCGModMenu;
		public override void Load() {
			Directory.CreateDirectory(ModsPath + "\\ModSettings");
			Hooks.On_AddMenuButtons += Hooks_On_AddMenuButtons;
			On.Terraria.Main.DrawMenu += Main_DrawMenu;
			tCFModMenu = new TConfigModMenu();
			tCFModMenu.Activate();
			_tCGModMenu = new UserInterface();
			_tCGModMenu.SetState(tCFModMenu);

			var sevenZipBytes = GetFileBytes(Path.Combine("lib", Environment.Is64BitProcess ? "7z64.dll" : "7z.dll"));
			File.WriteAllBytes(SevenDllPath, sevenZipBytes);
			//SevenZipBase.SetLibraryPath(SevenDllPath);
		}

		public override void Unload() {
			tCFModMenu.Deactivate();
			File.Delete(SevenDllPath);
		}

		public override void UpdateUI(GameTime gameTime) {
			_tCGModMenu?.Update(gameTime);
		}

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
			int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
			if (mouseTextIndex != -1) {
				layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
					"tConfigWrapper: A Description",
					delegate {
						_tCGModMenu.Draw(Main.spriteBatch, new GameTime());
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
				Main.MenuUI.SetState(tCFModMenu);
				Main.menuMode = 888;
			}, selectedMenu, buttonNames, ref buttonIndex, ref numButtons);
		}
	}
}