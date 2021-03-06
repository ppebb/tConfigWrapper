using Gajatko.IniFiles;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using tConfigWrapper.Common;
using tConfigWrapper.Common.DataTemplates;
using Terraria;
using Terraria.ModLoader;

namespace tConfigWrapper.Loaders {
	internal class ProjectileLoader : BaseLoader {
		private readonly Dictionary<string, ModProjectile> projectilesToLoad = new Dictionary<string, ModProjectile>();

		public ProjectileLoader(string modName, ConcurrentDictionary<string, MemoryStream> fileStreams) : base(modName, fileStreams) { }

		public override string TargetFolder => "Projectile";

		protected override void HandleFile(string file) {
			MemoryStream iniStream = fileStreams[file];

			IniFileReader reader = new IniFileReader(iniStream);
			IniFile iniFile = IniFile.FromStream(reader);

			object info = new ProjectileInfo();

			string projectileName = Path.GetFileNameWithoutExtension(file);
			string internalName = $"{modName}:{projectileName.RemoveIllegalCharacters()}";
			bool logProjectileAndModName = false;

			foreach (IniFileSection section in iniFile.sections)
			foreach (IniFileElement element in section.elements) {
				switch (section.Name) {
					case "Stats": {
						var splitElement = element.Content.Split('=');

						var statField = typeof(ProjectileInfo).GetField(splitElement[0]);

						switch (splitElement[0]) {
							case "type": {
								continue;
							}
							default: {
								if (statField == null) {
									Mod.Logger.Debug($"Projectile field not found or invalid field! -> {splitElement[0]}");
									logProjectileAndModName = true;
									tConfigWrapper.ReportErrors = true;
									continue;
								}

								break;
							}
						}

						//Conversion garbage
						TypeConverter converter = TypeDescriptor.GetConverter(statField.FieldType);
						object realValue = converter.ConvertFromString(splitElement[1]);
						statField.SetValue(info, realValue);
						break;
					}
				}
			}

			string texturePath = Path.ChangeExtension(file, "png");
			Texture2D projectileTexture = null;
			if (!Main.dedServ && fileStreams.TryGetValue(texturePath, out MemoryStream textureStream)) {
				projectileTexture = Texture2D.FromStream(Main.instance.GraphicsDevice, textureStream);
			}

			if (logProjectileAndModName) {
				Mod.Logger.Debug($"{internalName}");
			}

			projectilesToLoad.Add(internalName, new BaseProjectile((ProjectileInfo)info, projectileName, projectileTexture));
		}

		public override void RegisterContent() {
			foreach (var (projectileName, modProj) in projectilesToLoad) {
				Mod.AddProjectile(projectileName, modProj);
			}
		}
	}
}
