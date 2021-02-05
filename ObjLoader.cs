using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using tConfigWrapper.Common;
using Terraria;

namespace tConfigWrapper {
	public class ObjLoader {
		private readonly BinaryReader _reader;
		private readonly Version _modVersion;

		public ObjLoader(BinaryReader reader, Version modVersion) {
			_reader = reader;
			_modVersion = modVersion;
		}

		public void LoadObj() {

		}

		private void LoadObjInternal() {
			LoadCustomDll();
			//LoadCustomSounds();
			//LoadCustomTileObj();
			//LoadCustomWallObj();
			//LoadCustomProjObj();
			//LoadCustomHoldStyleObj();
			//LoadCustomUseStyleObj();
			//Prefix.LoadPrefixes();
			//LoadCustomItemObj();
			//LoadCustomBuffObj();
			//LoadCustomNPCObj();
			//LoadCustomGoreObj();
		}

		private void LoadCustomDll() {
			if (_modVersion < new Version(0, 16, 9) || !_reader.ReadBoolean()) 
				return;

			int byteCount = _reader.ReadInt32();
			byte[] rawAssembly = _reader.ReadBytes(byteCount);
			Assembly assembly = Assembly.Load(rawAssembly);
			// TODO: Do stuff with the assembly
		}

		private void LoadGenericObj(List<FieldInfo> fields, object defaults, object item) {
			Type type = defaults.GetType();
			foreach (var field in fields) {
				if (field.IsStatic || field.Name == "useCode" || field.Name == "unloadedPrefix" || field.Name == "dontDrawFace" ||
				    field.Name == "dontRelocate" || field.Name == "baseGravity" || field.Name == "maxGravity" ||
				    field.Name == "SpawnBiomes")
					continue;

				var fieldType = field.FieldType;
				var fieldName = field.Name;

				if (type.GetField(fieldName) == null)
					continue;

				bool validFieldIGuess;
				switch (fieldName) {
					case "toolTip3":
					case "toolTip4":
					case "toolTip5":
					case "toolTip6":
					case "toolTip7":
						validFieldIGuess = _modVersion > new Version(0, 17);
						break;
					default:
						validFieldIGuess = true;
						break;
				}

				if (!validFieldIGuess) 
					continue;

				switch (fieldType.ToString()) {
					case "Double":
						field.SetValue(item, _reader.ReadDouble());
						break;
					case "Int32":
						field.SetValue(item, _reader.ReadInt32());
						break;
					case "Single":
						field.SetValue(item, _reader.ReadSingle());
						break;
					case "Boolean":
						field.SetValue(item, _reader.ReadBoolean());
						break;
					case "Microsoft.Xna.Framework.Color":
						field.SetValue(item, _reader.ReadRGBA());
						break;
					case "System.String":
						field.SetValue(item, _reader.ReadString());
						break;
				}
			}
		}
	}
}
