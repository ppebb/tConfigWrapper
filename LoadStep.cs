using Terraria;
using Terraria.ModLoader;
using System;
using System.IO;
using System.Reflection;
using SevenZip;
using Gajatko.IniFiles;

namespace tConfigWrapper {
	public static class LoadStep {
		public static string[] files;
		public static Action<string> loadProgressText;
		public static Action<float> loadProgress;
		public static Action<string> loadSubProgressText;
		public static void Setup() {
			files = Directory.GetFiles(tConfigWrapper.ModsPath);
			Assembly assembly = Assembly.GetAssembly(typeof(Mod));
			Type UILoadModsType = assembly.GetType("Terraria.ModLoader.UI.UILoadMods");


			object loadModsValue = assembly.GetType("Terraria.ModLoader.UI.Interface").GetField("loadMods", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
			MethodInfo LoadStageMethod = UILoadModsType.GetMethod("SetLoadStage", BindingFlags.Instance | BindingFlags.Public);
			PropertyInfo ProgressProperty = UILoadModsType.GetProperty("Progress", BindingFlags.Instance | BindingFlags.Public);
			PropertyInfo SubProgressTextProperty = UILoadModsType.GetProperty("SubProgressText", BindingFlags.Instance | BindingFlags.Public);

			loadProgressText = (string s) => LoadStageMethod.Invoke(loadModsValue, new object[] { s, -1 });
			loadProgress = (float f) => ProgressProperty.SetValue(loadModsValue, f);
			loadSubProgressText = (string s) => SubProgressTextProperty.SetValue(loadModsValue, s);

			loadProgressText?.Invoke("tConfig Wrapper: Loading Mods");
			loadProgress?.Invoke(0f);
			using (MemoryStream stream = new MemoryStream()) {
				string tmpPath = Path.Combine(Main.SavePath, "tConfigWrapper", "tmpFile.zip");
				var a = Assembly.GetExecutingAssembly().Location;
				for (int i = 0; i < files.Length; i++) {
					File.Copy(files[i], tmpPath, true);
					FileStream fileStream = File.Open(tmpPath, FileMode.Open);
					using (SevenZipExtractor extractor = new SevenZipExtractor(fileStream)) {
						MemoryStream configStream = new MemoryStream();
						extractor.ExtractFile("Config.ini", configStream);
						configStream.Position = 0L;
						IniFileReader configReader = new IniFileReader(configStream);
						IniFile iniFile = IniFile.FromStream(configReader);

						foreach (string fileName in extractor.ArchiveFileNames) {
							ModContent.GetInstance<tConfigWrapper>().Logger.Debug($"Holy: {fileName}");
						}
						loadProgress?.Invoke((float)i / files.Length);
						loadSubProgressText?.Invoke(Path.GetFileName(files[i]));
					}
					fileStream.Dispose();
				}
				File.Delete(tmpPath);
			}

			//Reset progress bar
			loadSubProgressText?.Invoke("");
			loadProgressText?.Invoke("Adding Recipes");
			loadProgress?.Invoke(0f);
		}
	}
}
