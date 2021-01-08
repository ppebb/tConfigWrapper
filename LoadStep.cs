using Terraria;
using Terraria.ModLoader;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using SevenZip;
using Gajatko.IniFiles;
using log4net;
using tConfigWrapper.DataTemplates;
using Terraria.ID;

namespace tConfigWrapper {
	public static class LoadStep {
		public static string[] files;
		public static Action<string> loadProgressText;
		public static Action<float> loadProgress;
		public static Action<string> loadSubProgressText;

		private static Mod mod => ModContent.GetInstance<tConfigWrapper>();

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
			for (int i = 0; i < files.Length; i++) {
				Stream stream;
				using (stream = new MemoryStream())
				{
					using (SevenZipExtractor extractor = new SevenZipExtractor(files[i]))
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
						
						foreach (string fileName in extractor.ArchiveFileNames)
						{
							if (Path.GetExtension(fileName) != ".ini") 
								continue; // If the extension is not .ini, ignore the file
							
							if (fileName.Contains("\\Item\\")) {
								CreateItem(fileName, Path.GetFileNameWithoutExtension(files[i]), extractor);
								//ModContent.GetInstance<tConfigWrapper>().AddItem(); Okay so I know I literally got nothing done but I need a damn moditem class
								//and it's too late at night for me to be thinking about that for me to understand how to do that.
								//I want a single moditem class to handle all loading but I can't figure out how to do that right now because I am tired.
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

			//Reset progress bar
			loadSubProgressText?.Invoke("");
			loadProgressText?.Invoke("Loading mod");
			loadProgress?.Invoke(0f);
		}

		private static void CreateItem(string fileName, string modName, SevenZipExtractor extractor)
		{
			using (MemoryStream iniStream = new MemoryStream())
			{
				extractor.ExtractFile(fileName, iniStream);
				iniStream.Position = 0;

				IniFileReader reader = new IniFileReader(iniStream);
				IniFile iniFile = IniFile.FromStream(reader);

				object info = new ItemInfo();

				foreach (IniFileSection section in iniFile.sections)
				{
					foreach (IniFileElement element in section.elements)
					{
						if (section.Name == "Stats")
						{
							var splitElement = element.Content.Split('=');

							var statField = typeof(ItemInfo).GetField(splitElement[0]);

							if (statField == null || splitElement[0] == "type")
							{
								mod.Logger.Debug($"Field not found or invalid field! -> {splitElement[0]}");
								continue;
							}

							// Convert the value to an object of type statField.FieldType
							TypeConverter converter = TypeDescriptor.GetConverter(statField.FieldType);
							object realValue = converter.ConvertFromString(splitElement[1]);
							statField.SetValue(info, realValue);
						}
					}
				}

				// Get the mod name
				string itemName = Path.GetFileNameWithoutExtension(fileName);
				string internalName = $"{modName}:{itemName}";
				ModContent.GetInstance<tConfigWrapper>().AddItem(internalName, new BaseItem((ItemInfo)info, itemName));
				reader.Dispose();
			}
		}
	}
}
