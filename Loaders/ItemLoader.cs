using Gajatko.IniFiles;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using tConfigWrapper.Common;
using tConfigWrapper.Common.DataTemplates;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tConfigWrapper.Loaders {
	internal class ItemLoader : BaseLoader {
		private Dictionary<string, ModItem> itemsToLoad = new Dictionary<string, ModItem>();

		public ItemLoader(string modName, ConcurrentDictionary<string, MemoryStream> fileStreams) : base(modName, fileStreams) { }

		public override string TargetFolder => "Item";

		protected override void HandleFile(string file) {
			MemoryStream iniStream = fileStreams[file];

			IniFileReader reader = new IniFileReader(iniStream);
			IniFile iniFile = IniFile.FromStream(reader);

			object info = new ItemInfo();
			List<string> toolTipList = new List<string>();

			// Get the mod name
			string itemName = Path.GetFileNameWithoutExtension(file);
			string internalName = $"{modName}:{itemName.RemoveIllegalCharacters()}";
			// TODO: If the item is from Terraria, make it a GlobalItem
			if (ItemID.FromLegacyName(itemName, 4) != 0)
				internalName = itemName;
			bool logItemAndModName = false;
			string createWall = null;
			string createTile = null;
			string shoot = null;

			foreach (IniFileSection section in iniFile.sections) {
				foreach (IniFileElement element in section.elements) {
					switch (section.Name) {
						case "Stats": {
								var splitElement = element.Content.Split('=');

								var statField = typeof(ItemInfo).GetField(splitElement[0]);

								switch (splitElement[0]) {
									// Set the tooltip, has to be done manually since the toolTip field doesn't exist in 1.3
									case "toolTip":
									case "toolTip1":
									case "toolTip2":
									case "toolTip3":
									case "toolTip4":
									case "toolTip5":
									case "toolTip6":
									case "toolTip7": {
											toolTipList.Add(splitElement[1]);
											continue;
										}
									case "useSound": {
											var soundStyleId = int.Parse(splitElement[1]);
											var soundStyle = new LegacySoundStyle(2, soundStyleId); // All items use the second sound ID
											statField = typeof(ItemInfo).GetField("UseSound");
											statField.SetValue(info, soundStyle);
											continue;
										}
									case "createTileName": {
											createTile = $"{modName}:{splitElement[1]}";
											continue;
										}
									case "projectile": {
											shoot = $"{modName}:{splitElement[1]}";
											continue;
										}
									case "createWallName": {
											createWall = $"{modName}:{splitElement[1]}";
											continue;
										}
									case "type":
										continue;
									default: {
											if (statField == null) {
												Mod.Logger.Debug($"Item field not found or invalid field! -> {splitElement[0]}");
												logItemAndModName = true;
												tConfigWrapper.ReportErrors = true;
												continue;
											}
											break;
										}
								}

								// Convert the value to an object of type statField.FieldType
								TypeConverter converter = TypeDescriptor.GetConverter(statField.FieldType);
								object realValue = converter.ConvertFromString(splitElement[1]);
								statField.SetValue(info, realValue);
								break;
							}
						case "Recipe": {
								if (!LoadStep.recipeDict.ContainsKey(internalName))
									LoadStep.recipeDict.TryAdd(internalName, section);
								break;
							}
					}
				}
			}

			if (logItemAndModName)
				Mod.Logger.Debug($"{internalName}"); //Logs the item and mod name if "Field not found or invalid field". Mod and item name show up below the other log line

			string toolTip = string.Join("\n", toolTipList);

			// Check if a texture for the .ini file exists
			string texturePath = Path.ChangeExtension(file, "png");
			Texture2D itemTexture = null;
			if (!Main.dedServ && fileStreams.TryGetValue(texturePath, out MemoryStream textureStream)) {
				itemTexture = Texture2D.FromStream(Main.instance.GraphicsDevice, textureStream); // Load a Texture2D from the stream
			}

			int id = ItemID.FromLegacyName(itemName, 4);
			if (id != 0) {
				if (!LoadStep.globalItemInfos.ContainsKey(id))
					LoadStep.globalItemInfos.TryAdd(id, (ItemInfo)info);
				else
					LoadStep.globalItemInfos[id] = (ItemInfo)info;

				reader.Dispose();
				return;
			}

			itemsToLoad.Add(internalName, new BaseItem((ItemInfo)info, internalName, itemName, createTile, shoot, createWall, toolTip, itemTexture));
			reader.Dispose();
		}

		public override void RegisterContent() {
			foreach (var (name, modItem) in itemsToLoad) {
				Mod.AddItem(name, modItem);
			}
		}
	}
}
