using Gajatko.IniFiles;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using tConfigWrapper.Common;
using tConfigWrapper.Common.DataTemplates;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tConfigWrapper.Loaders {
	internal class NPCLoader : BaseLoader {
		private Dictionary<string, ModNPC> npcToLoad = new Dictionary<string, ModNPC>();

		public NPCLoader(string modName, ConcurrentDictionary<string, MemoryStream> fileStreams) : base(modName, fileStreams) { }

		protected override string TargetFolder => "NPC";

		protected override void HandleFile(string file) {
			List<(int, int?, string, float)> dropList = new List<(int, int?, string, float)>();
			MemoryStream iniStream = fileStreams[file];
			IniFileReader reader = new IniFileReader(iniStream);
			IniFile iniFile = IniFile.FromStream(reader);

			object info = new NpcInfo();

			string npcName = Path.GetFileNameWithoutExtension(file);
			string internalName = $"{modName}:{npcName.RemoveIllegalCharacters()}";
			bool logNPCAndModName = false;

			foreach (IniFileSection section in iniFile.sections)
			foreach (IniFileElement element in section.elements) {
				if (string.IsNullOrWhiteSpace(element.Content))
					continue;

				switch (section.Name) {
					case "Stats": {
						var splitElement = element.Content.Split('=');

						string split1Correct = Utilities.ConvertField13(splitElement[0]);
						var statField = typeof(NpcInfo).GetField(split1Correct);

						switch (splitElement[0]) {
							case "soundHit": {
								var soundStyleID = int.Parse(splitElement[1]);
								var soundStyle = new LegacySoundStyle(3, soundStyleID); // All NPC hit sounds use 3
								statField = typeof(NpcInfo).GetField("HitSound");
								statField.SetValue(info, soundStyle);
								continue;
							}
							case "soundKilled": {
								var soundStyleID = int.Parse(splitElement[1]);
								var soundStyle = new LegacySoundStyle(4, soundStyleID); // All death sounds use 4
								statField = typeof(NpcInfo).GetField("DeathSound");
								statField.SetValue(info, soundStyle);
								continue;
							}
							case "type":
								continue;
							default: {
								if (statField == null) {
									Mod.Logger.Debug($"NPC field not found or invalid field! -> {splitElement[0]}");
									logNPCAndModName = true;
									tConfigWrapper.ReportErrors = true;
									continue;
								}

								break;
							}
						}

						TypeConverter converter = TypeDescriptor.GetConverter(statField.FieldType);
						object realValue = converter.ConvertFromString(splitElement[1]);
						statField.SetValue(info, realValue);
						break;
					}
					case "Buff Immunities": {
						var splitElement = element.Content.Split('=');
						splitElement[0] = splitElement[0].Replace(" ", "").Replace("!", "");

						FieldInfo npcInfoImmunity = typeof(NpcInfo).GetField("buffImmune");
						if (BuffID.Search.ContainsName(splitElement[0])) {
							// Will 100% need to adjust this once we get mod buff loading implemented
							bool[] immunity = new bool[BuffLoader.BuffCount];
							immunity[BuffID.Search.GetId(splitElement[0])] = bool.Parse(splitElement[1]);
							npcInfoImmunity.SetValue(info, immunity);
						}
						else
							Mod.Logger.Debug($"{splitElement[0]} doesn't exist!"); // Will have to manually convert 

						break;
					}
					case "Drops":
						// example of drop string: 1-4 Golden Flame=0.7
						string dropRangeString =
							element.Content.Split(new[] {' '}, 2)[0]; // This gets the drop range, everthing before the first space
						string dropItemString =
							element.Content.Split(new[] {' '}, 2)[1]
								.Split('=')[
									0]; // This gets everything after the first space, then it splits at the = and gets everything before it
						string dropChanceString = element.Content.Split('=')[1]; // Gets everything after the = sign
						int min;
						int? max = null;
						if (dropRangeString.Contains("-")) {
							min = int.Parse(dropRangeString.Split('-')[0]);
							max = int.Parse(dropRangeString.Split('-')[1]) + 1; // + 1 because the max is exclusive in Main.rand.Next()
						}
						else {
							min = int.Parse(dropRangeString);
						}

						dropList.Add((min, max, $"{modName}:{dropItemString}", float.Parse(dropChanceString) / 100));
						break;
				}
			}

			if (logNPCAndModName)
				Mod.Logger.Debug($"{internalName}"); //Logs the npc and mod name if "Field not found or invalid field". Mod and npc name show up below the other log line

			// Check if a texture for the .ini file exists
			string texturePath = Path.ChangeExtension(file, "png");
			Texture2D npcTexture = null;
			if (!Main.dedServ && fileStreams.TryGetValue(texturePath, out MemoryStream textureStream)) {
				npcTexture = Texture2D.FromStream(Main.instance.GraphicsDevice, textureStream); // Load a Texture2D from the stream
			}

			npcToLoad.Add(internalName, new BaseNPC((NpcInfo)info, dropList, npcName, npcTexture));

			reader.Dispose();
		}

		public override void RegisterContent() {
			foreach (var (name, modNPC) in npcToLoad) {
				Mod.AddNPC(name, modNPC);
			}
		}
	}
}
