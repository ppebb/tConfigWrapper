using MonoMod.RuntimeDetour.HookGen;
using System;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tConfigWrapper {
	public static class Hooks {
		public delegate void Orig_AddMenuButtons(Main main, int selectedMenu, string[] buttonNames, float[] buttonScales, ref int offY, ref int spacing, ref int buttonIndex, ref int numButtons);
		public delegate void Hook_AddMenuButtons(Orig_AddMenuButtons orig, Main main, int selectedMenu, string[] buttonNames, float[] buttonScales, ref int offY, ref int spacing, ref int buttonIndex, ref int numButtons);

		public static event Hook_AddMenuButtons On_AddMenuButtons {
			add {
				HookEndpointManager.Add<Hook_AddMenuButtons>(typeof(Mod).Assembly.GetType("Terraria.ModLoader.UI.Interface").GetMethod("AddMenuButtons", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic), value);
			}
			remove {
				HookEndpointManager.Remove<Hook_AddMenuButtons>(typeof(Mod).Assembly.GetType("Terraria.ModLoader.UI.Interface").GetMethod("AddMenuButtons", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic), value);
			}
		}

		//public delegate void Orig_DisplayLoadError(string msg, Exception e, bool fatal, bool continueIsRetry);
		//public delegate void Hook_DisplayLoadError(Orig_DisplayLoadError orig, string msg, Exception e, bool fatal, bool continueIsRetry);

		//public static event Hook_DisplayLoadError On_DisplayLoadError {
		//	add {
		//		HookEndpointManager.Add<Hook_DisplayLoadError>(typeof(Mod).Assembly.GetType("Terraria.ModLoader.ModLoader").GetMethod("DisplayLoadError", BindingFlags.NonPublic | BindingFlags.Static), value);
		//	}
		//	remove {
		//		HookEndpointManager.Remove<Hook_DisplayLoadError>(typeof(Mod).Assembly.GetType("Terraria.ModLoader.ModLoader").GetMethod("DisplayLoadError", BindingFlags.NonPublic | BindingFlags.Static), value);
		//	}
		//}

		public delegate void Orig_Populate();
		public delegate void Hook_Populate(Orig_Populate orig);

		public static event Hook_Populate On_Populate {
			add {
				HookEndpointManager.Add<Hook_Populate>(typeof(Mod).Assembly.GetType("Terraria.Modloader.UI.UIMods").GetMethod("Populate", BindingFlags.NonPublic | BindingFlags.Instance), value);
			}
			remove {
				HookEndpointManager.Remove<Hook_Populate>(typeof(Mod).Assembly.GetType("Terraria.Modloader.UI.UIMods").GetMethod("Populate", BindingFlags.NonPublic | BindingFlags.Instance), value);
			}
		}

	}

	public static class MenuUtils {
		public static void AddButton(string text, Action act, int selectedMenu, string[] buttonNames, ref int buttonIndex, ref int numButtons) {
			buttonNames[buttonIndex] = text;

			if (selectedMenu == buttonIndex) {
				Main.PlaySound(SoundID.MenuOpen);
				act();
			}

			buttonIndex++;
			numButtons++;
		}
	}
}
