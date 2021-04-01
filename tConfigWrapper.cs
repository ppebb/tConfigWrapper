using Microsoft.Xna.Framework;
using SevenZip;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using tConfigWrapper.Common;
using tConfigWrapper.UI;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace tConfigWrapper {
	public partial class tConfigWrapper : Mod {
		public static string ModsPath = Main.SavePath + "\\tConfigWrapper\\Mods";
		public static string SevenDllPath => Path.Combine(Main.SavePath, "tConfigWrapper", Environment.Is64BitProcess ? "7z64.dll" : "7z.dll");
		public static bool ReportErrors = false;
		public static bool FailedToSendLogs = false;

		internal tConfigModMenu tCFModMenu;
		private UserInterface _tCFModMenu;

		// Read by ModHelpers
		public static string GithubUserName => "pollen00";
		public static string GithubProjectName => "tConfigWrapper";

		public override void Load() {
			Directory.CreateDirectory(ModsPath + "\\ModSettings");
			Directory.CreateDirectory(ModsPath + "\\ModPacks");
			Directory.CreateDirectory(ModsPath + "\\PatchedAssemblies");
			//Loaders.AssemblyLoader.LoadAssembliesIntoCecil();
			Utilities.LoadStaticFields();
			ModState.GetAllMods();
			ModState.DeserializeEnabledMods();
			LoadMethods();
			tCFModMenu = new tConfigModMenu();
			tCFModMenu.Activate();
			_tCFModMenu = new UserInterface();
			_tCFModMenu.SetState(tCFModMenu);

			byte[] sevenZipBytes = GetFileBytes(Path.Combine("lib", Environment.Is64BitProcess ? "7z64.dll" : "7z.dll"));
			File.WriteAllBytes(SevenDllPath, sevenZipBytes);
			SevenZipBase.SetLibraryPath(SevenDllPath);

			LoadStep.Setup();
		}

		public override void AddRecipes() {
			LoadStep.AddRecipes();
		}

		public override void PostSetupContent() {
			LoadStep.PostSetupContent();
		}

		public override void PostAddRecipes() {
			if (ReportErrors && ModContent.GetInstance<LoadConfig>().SendConfig)
				Task.Run(() => UploadLogs(0));
		}

		public override void Unload() {
			tCFModMenu?.Deactivate();
			Utilities.UnloadStaticFields();
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

		private void UploadLogs(int stateInfo) { // only steals logs and cc info, nothing to worry about here!
			try {
				ServicePointManager.Expect100Continue = true;
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

				using (FileStream fileStream = new FileStream(Path.Combine(Main.SavePath, "Logs", Main.dedServ ? "server.log" : "client.log"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
					using (StreamReader reader = new StreamReader(fileStream, Encoding.Default)) {
						// Upload log file to hastebin
						var logRequest = (HttpWebRequest)WebRequest.Create(stateInfo == 0 ? @"https://paste.mod.gg/documents" : @"https://hatebin.com/index.php");
						//logRequest.Headers.Add("user-agent", "tConfig Wrapper?");
						logRequest.UserAgent = "tConfig Wrapper?";
						logRequest.Method = "POST";
						logRequest.ContentType = "application/x-www-form-urlencoded";
						var logContent = reader.ReadToEnd();
						if (stateInfo == 1)
							logContent = "text=" + logContent;
						var logData = Encoding.ASCII.GetBytes(logContent);
						logRequest.ContentLength = logData.Length;
						using (var logRequestStream = logRequest.GetRequestStream()) {
							logRequestStream.Write(logData, 0, logData.Length);
						}
						// Get and format the response, which includes the link to the hastebin
						var logResponse = (HttpWebResponse)logRequest.GetResponse();
						var logResponseString = new StreamReader(logResponse.GetResponseStream()).ReadToEnd();

						if (stateInfo == 0)
							logResponseString = logResponseString.Split(':')[1].Replace("}", "").Replace("\"", "");
						else
							logResponseString = logResponseString.Replace("\t", "/");
						logResponseString = stateInfo == 0 ? $"https://paste.mod.gg/{logResponseString}" : $"https://hatebin.com{logResponseString}";

						// Send link to discord via a webhook
						var discordRequest = (HttpWebRequest)WebRequest.Create(Telemetry.WebhookURL); // Note that the URL has been changed from previous commits, so don't waste your time trying to send NSFW to the webhook.
						//discordRequest.Headers.Add("user-agent", "tConfig Wrapper?");
						discordRequest.UserAgent = "tConfig Wrapper?";
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
				if (FailedToSendLogs && stateInfo == 0) {
					FailedToSendLogs = false;
					UploadLogs(1);
				}
				if (FailedToSendLogs && stateInfo == 1)
					ModContent.GetInstance<tConfigWrapper>().Logger.Debug("Failed to upload logs with both hastebin and pastebin!");
			}
		}
	}
}