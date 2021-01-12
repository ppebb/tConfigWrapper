using Microsoft.Xna.Framework;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using tConfigWrapper.UI;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace tConfigWrapper {
	public class tConfigWrapper : Mod {
		public static string ModsPath = Main.SavePath + "\\tConfigWrapper\\Mods";
		public static string SevenDllPath => Path.Combine(Main.SavePath, "tConfigWrapper", Environment.Is64BitProcess ? "7z64.dll" : "7z.dll");
		public static bool ReportErrors = false;

		internal TConfigModMenu tCFModMenu;
		private UserInterface _tCFModMenu;

		public override void Load() {
			Directory.CreateDirectory(ModsPath + "\\ModSettings");
			Hooks.On_AddMenuButtons += Hooks_On_AddMenuButtons;
			On.Terraria.Main.DrawMenu += Main_DrawMenu;
			tCFModMenu = new TConfigModMenu();
			tCFModMenu.Activate();
			_tCFModMenu = new UserInterface();
			_tCFModMenu.SetState(tCFModMenu);

			var sevenZipBytes = GetFileBytes(Path.Combine("lib", Environment.Is64BitProcess ? "7z64.dll" : "7z.dll"));
			File.WriteAllBytes(SevenDllPath, sevenZipBytes);

			LoadStep.Setup();
		}

		public override void AddRecipes() {
			LoadStep.SetupRecipes();
		}

		public override void PostAddRecipes() {
			LoadStep.GetTileMapEntries();
			if (ReportErrors && CheckForInternetConnection() && ModContent.GetInstance<WrapperModConfig>().SendConfig)
				UploadLogs();
		}

		public override void Unload() {
			tCFModMenu.Deactivate();
			ReportErrors = false;
		}

		public override void Close() {
			File.Delete(SevenDllPath);
			base.Close();
		}

		public static bool CheckForInternetConnection() {
			try {
				using (var client = new WebClient()) {
					using (client.OpenRead("https://hastebin.com/")) {
						using (client.OpenRead("https://discord.com/api/webhooks/797477719301947432/pB9jjZt4km7baBFfiC2oAn5twSBVCitjwVxuoRRvMC8G7UjXfqyIY28LvXOjuUWMWmvJ"))
							return true;
					}
				}
			}
			catch {
				ModContent.GetInstance<tConfigWrapper>().Logger.Debug("Unable to connect to the internet");
				return false;
			}
		}

		public override void UpdateUI(GameTime gameTime) {
			_tCFModMenu?.Update(gameTime);
		}

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
			int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
			if (mouseTextIndex != -1) {
				layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
					"tConfigWrapper: A Description",
					delegate {
						if (!Main.gameMenu)
							return true;

						_tCFModMenu.Draw(Main.spriteBatch, new GameTime());
						return true;
					},
					InterfaceScaleType.UI)
				);
			}
		}

		private void UploadLogs() {
			LoadStep.loadProgressText?.Invoke("tConfig Wrapper: Uploading Logs");
			LoadStep.loadProgress?.Invoke(0f);
			LoadStep.loadSubProgressText?.Invoke("");
			using (FileStream fileStream = new FileStream(Path.Combine(Main.SavePath, "Logs", Main.dedServ ? "server.log" : "client.log"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
				using (StreamReader reader = new StreamReader(fileStream, Encoding.Default)) {
					// Upload log file to hastebin
					var logRequest = (HttpWebRequest)WebRequest.Create(@"https://hastebin.com/documents");
					logRequest.Method = "POST";
					logRequest.ContentType = "application/json";
					LoadStep.loadProgress(0.20f);
					var logContent = reader.ReadToEnd();
					var logData = Encoding.ASCII.GetBytes(logContent);
					logRequest.ContentLength = logData.Length;
					LoadStep.loadProgress(0.40f);
					using (var logRequestStream = logRequest.GetRequestStream()) {
						logRequestStream.Write(logData, 0, logData.Length);
					}
					LoadStep.loadProgress(0.60f);
					// Get and format the response, which includes the link to the hastebin
					var logResponse = (HttpWebResponse)logRequest.GetResponse();
					var logResponseString = new StreamReader(logResponse.GetResponseStream()).ReadToEnd();
					logResponseString = logResponseString.Split(':')[1].Replace("}", "").Replace("\"", "");
					logResponseString = $"https://hastebin.com/{logResponseString}";

					// Send link to discord via a webhook
					var discordRequest = (HttpWebRequest)WebRequest.Create(@"https://discord.com/api/webhooks/797477719301947432/pB9jjZt4km7baBFfiC2oAn5twSBVCitjwVxuoRRvMC8G7UjXfqyIY28LvXOjuUWMWmvJ");
					discordRequest.Method = "POST";
					discordRequest.ContentType = "application/json";
					LoadStep.loadProgress(0.80f);
					string serverOrClient = Main.dedServ ? "server" : "client";
					var discordContent = "{\"content\": \"A new " + serverOrClient + " log has been uploaded! Link: " + logResponseString + "\"}";
					var discordData = Encoding.ASCII.GetBytes(discordContent);
					discordRequest.ContentLength = discordData.Length;
					LoadStep.loadProgress(1f);
					using (var discordRequestStream = discordRequest.GetRequestStream()) {
						discordRequestStream.Write(discordData, 0, discordData.Length);
					}
				}
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