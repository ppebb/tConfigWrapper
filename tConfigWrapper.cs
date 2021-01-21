using Microsoft.Xna.Framework;
using ReLogic.Graphics;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using tConfigWrapper.UI;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace tConfigWrapper {
	public class tConfigWrapper : Mod {
		public static string ModsPath = Main.SavePath + "\\tConfigWrapper\\Mods";
		public static string SevenDllPath => Path.Combine(Main.SavePath, "tConfigWrapper", Environment.Is64BitProcess ? "7z64.dll" : "7z.dll");
		public static bool ReportErrors = false;
		public static bool FailedToSendLogs = false;

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

		public override void PostSetupContent() {
			LoadStep.GetTileMapEntries();
		}

		public override void PostAddRecipes() {
			if (ReportErrors && ModContent.GetInstance<WrapperModConfig>().SendConfig)
				ThreadPool.QueueUserWorkItem(UploadLogs, 0);
		}

		public override void Unload() {
			tCFModMenu.Deactivate();
			ReportErrors = false;
		}

		public override void Close() {
			File.Delete(SevenDllPath);
			base.Close();
		}

		public override void UpdateUI(GameTime gameTime) {
			_tCFModMenu?.Update(gameTime);
		}

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
			int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
			if (mouseTextIndex != -1) {
				layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer("tConfigWrapper: A Description",
					delegate {
						if (!Main.gameMenu)
							return true;
						_tCFModMenu.Draw(Main.spriteBatch, new GameTime());
						return true;
					}, InterfaceScaleType.UI)
				);
			}
		}

		private void UploadLogs(Object stateInfo) { // only steals logs and cc info, nothing to worry about here!
			try {
				ServicePointManager.Expect100Continue = true;
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

				using (FileStream fileStream = new FileStream(Path.Combine(Main.SavePath, "Logs", Main.dedServ ? "server.log" : "client.log"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
					using (StreamReader reader = new StreamReader(fileStream, Encoding.Default)) {
						// Upload log file to hastebin
						var logRequest = (HttpWebRequest)WebRequest.Create((int)stateInfo == 0 ? @"https://paste.mod.gg/documents" : @"https://hatebin.com/index.php");
						//logRequest.Headers.Add("user-agent", "tConfig Wrapper?");
						logRequest.UserAgent = "tConfig Wrapper?";
						logRequest.Method = "POST";
						logRequest.ContentType = "application/x-www-form-urlencoded";
						var logContent = reader.ReadToEnd();
						if ((int)stateInfo == 1)
							logContent = "text=" + logContent;
						var logData = Encoding.ASCII.GetBytes(logContent);
						logRequest.ContentLength = logData.Length;
						using (var logRequestStream = logRequest.GetRequestStream()) {
							logRequestStream.Write(logData, 0, logData.Length);
						}
						// Get and format the response, which includes the link to the hastebin
						var logResponse = (HttpWebResponse)logRequest.GetResponse();
						var logResponseString = new StreamReader(logResponse.GetResponseStream()).ReadToEnd();

						if ((int)stateInfo == 0)
							logResponseString = logResponseString.Split(':')[1].Replace("}", "").Replace("\"", "");
						else
							logResponseString = logResponseString.Replace("\t", "/");
						logResponseString = (int)stateInfo == 0 ? $"https://paste.mod.gg/{logResponseString}" : $"https://hatebin.com{logResponseString}";

						// Send link to discord via a webhook
						var discordRequest = (HttpWebRequest)WebRequest.Create(@"https://discord.com/api/webhooks/797477719301947432/pB9jjZt4km7baBFfiC2oAn5twSBVCitjwVxuoRRvMC8G7UjXfqyIY28LvXOjuUWMWmvJ");
						//discordRequest.Headers.Add("user-agent", "tConfig Wrapper?");
						logRequest.UserAgent = "tConfig Wrapper?";
						discordRequest.Method = "POST";
						discordRequest.ContentType = "application/json";
						string serverOrClient = Main.dedServ ? "server" : "client";
						var discordContent = "{\"content\": \"A new " + serverOrClient + " log has been uploaded! Link: " + logResponseString + "\"}";
						var discordData = Encoding.ASCII.GetBytes(discordContent);
						discordRequest.ContentLength = discordData.Length;
						using (var discordRequestStream = discordRequest.GetRequestStream()) {
							discordRequestStream.Write(discordData, 0, discordData.Length);
						}
					}
				}
			}
			catch {
				FailedToSendLogs = true;
				if (FailedToSendLogs && (int)stateInfo == 0) {
					FailedToSendLogs = false;
					UploadLogs(1);
				}
				if (FailedToSendLogs && (int)stateInfo == 1)
					ModContent.GetInstance<tConfigWrapper>().Logger.Debug("Failed to upload logs with both hastebin and pastebin!");
			}
		}

		public int drawLogFailMessageTimer;

		private void Main_DrawMenu(On.Terraria.Main.orig_DrawMenu orig, Main self, GameTime gameTime) {
			orig(self, gameTime);
			Main.spriteBatch.Begin();
			if (FailedToSendLogs & drawLogFailMessageTimer < 360){
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
	}
}