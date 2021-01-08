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

			files = Directory.GetFiles(tConfigWrapper.ModsPath);

			string tmpPath = Path.Combine(Main.SavePath, "tConfigWrapper", "tmpFile.zip");
			for (int i = 0; i < files.Length; i++) {
				Stream stream;
				using (stream = new MemoryStream())
				{
					File.Copy(files[i], tmpPath, true);
					using (SevenZipExtractor extractor = new SevenZipExtractor(tmpPath))
					{
						// Note for pollen, when you have a stream, at the end you have to dispose it
						// There are two ways to do this, calling .Dispose(), or using "using (Stream whatever ...) { ... }"
						// The "using" way is better because it will always Dispose it, even if there's an exception

						// Disposing via .Dispose()
						MemoryStream configStream = new MemoryStream();
						extractor.ExtractFile("Config.ini", configStream);
						configStream.Position = 0L;
						IniFileReader configReader = new IniFileReader(configStream);
						IniFile configFile = IniFile.FromStream(configReader);
						configStream.Dispose();

						var logger = ModContent.GetInstance<tConfigWrapper>().Logger;
						foreach (string fileName in extractor.ArchiveFileNames)
						{
							ModContent.GetInstance<tConfigWrapper>().Logger.Debug($"Holy: {fileName}");
							if (Path.GetExtension(fileName) == ".ini")
							{
								if (fileName.Contains("\\Item\\")) {
									//ModContent.GetInstance<tConfigWrapper>().AddItem(); Okay so I know I literally got nothing done but I need a damn moditem class
									//and it's too late at night for me to be thinking about that for me to understand how to do that.
									//I want a single moditem class to handle all loading but I can't figure out how to do that right now because I am tired.
								}
							}
						}

						loadProgress?.Invoke((float) i / files.Length);
						loadSubProgressText?.Invoke(Path.GetFileName(files[i]));
					}


					// this is for the obj (im scared of it)
					//stream.Position = 0L;
					//BinaryReader reader;
					//using (reader = new BinaryReader(stream))
					//{
					//	string modName = Path.GetFileName(files[i]).Split('.')[0];
					//	stream.Position = 0L;
					//	var version = new Version(reader.ReadString());

					//	// Don't know what these things are
					//	int modVersion;
					//	string modDLVersion, modURL;
					//	if (version >= new Version("0.20.5"))
					//		modVersion = reader.ReadInt32();

					//	if (version >= new Version("0.22.8") && reader.ReadBoolean())
					//	{
					//		modDLVersion = reader.ReadString();
					//		modURL = reader.ReadString();
					//	}

					//	reader.Close();
					//	stream.Close();
					//}
				}
			}
			File.Delete(tmpPath);

			//Reset progress bar
			loadSubProgressText?.Invoke("");
			loadProgressText?.Invoke("Adding Recipes");
			loadProgress?.Invoke(0f);
		}
	}
}
