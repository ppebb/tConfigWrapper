using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using tConfigWrapper.Common;
using tConfigWrapper.Common.DataTemplates;
using Terraria.ModLoader;

namespace tConfigWrapper {
	public class ObjLoader {
		private readonly BinaryReader _reader;
		private Version _modVersion;
		private readonly string _modName;

		private MethodInfo ReserveSoundIDMethodInfo =>
			typeof(SoundLoader).GetMethod("ReserveSoundID", BindingFlags.NonPublic | BindingFlags.Static);
		private FieldInfo SoundsField => typeof(SoundLoader).GetField("sounds", BindingFlags.NonPublic | BindingFlags.Static);
		private FieldInfo ModSoundsField => typeof(SoundLoader).GetField("modSounds", BindingFlags.NonPublic | BindingFlags.Static);
		private PropertyInfo ModSoundSoundProperty => typeof(ModSound).GetProperty("sound");

		public ObjLoader(BinaryReader reader/*, Version modVersion*/, string modName) {
			_reader = reader;
			_modName = modName;
		}

		public void LoadObj() {
			LoadObjInternal();
		}

		private void LoadObjInternal() {
			if (!GetModInfo(out _modVersion, out _, out _)) return;

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

		private bool GetModInfo(out Version modVersion, out string dlVersion, out string url) {
			dlVersion = null;
			url = null;
			modVersion = new Version(_reader.ReadString());

			//if (modVersion < new Version(0, 35, 0))
			//	return false;

			if (modVersion > new Version(0, 20, 5))
				_reader.ReadInt32();

			if (_modVersion > new Version("0.22.8") && _reader.ReadBoolean()) {
				dlVersion = _reader.ReadString();
				url = _reader.ReadString();
			}

			return true;
		}

		private void LoadCustomDll() {
			if (_modVersion < new Version(0, 16, 9) || !_reader.ReadBoolean()) 
				return;

			int byteCount = _reader.ReadInt32();
			byte[] rawAssembly = _reader.ReadBytes(byteCount);
			Assembly assembly = Assembly.Load(rawAssembly);
			// TODO: Do stuff with the assembly
			//foreach (var definedType in assembly.DefinedTypes) {
				//var someInstance = Activator.CreateInstance(definedType);
			//}
		}

		private void LoadCustomSounds() {
			if (_modVersion < new Version(0, 17))
				return;

			int soundCount = _reader.ReadInt32();
			for (int i = 0; i < soundCount; i++) {
				// Read the sound info
				string soundName = _reader.ReadString();
				int soundByteCount = _reader.ReadInt32();
				byte[] soundBytes = _reader.ReadBytes(soundByteCount);
				
				// This is copied from Mod.AddSound()
				int id = (int)ReserveSoundIDMethodInfo.Invoke(null, new object[] {SoundType.Custom});
				var sounds = (IDictionary<SoundType, IDictionary<string, int>>)SoundsField.GetValue(null);
				var modSounds = (IDictionary<SoundType, IDictionary<int, ModSound>>)ModSoundsField.GetValue(null);
				var modSoundInstance = Activator.CreateInstance<ModSound>();

				sounds[SoundType.Custom][$"{_modName}:{soundName}"] = id;
				modSounds[SoundType.Custom][id] = modSoundInstance;
				ModSoundSoundProperty.SetValue(modSoundInstance, LoadSound(soundBytes, soundName));
				
				SoundsField.SetValue(null, sounds);
				ModSoundsField.SetValue(null, modSounds);
			}
		}

		private void LoadCustomTileObj() {
			if (_modVersion < new Version(0, 17, 4))
				return;

			if (_modVersion < new Version(0, 24))
				_reader.ReadBoolean();

			Type tileType = typeof(TileInfo);
			var tileFields = tileType.GetFields();

			int tileCount = _reader.ReadInt32();
			for (int i = 0; i < tileCount; i++) {
				object tileInfo = new TileInfo();
				LoadGenericObj(tileFields, typeof(TileInfo), tileInfo);

				int tileByteCount = _reader.ReadInt32();
				using (MemoryStream stream = new MemoryStream(_reader.ReadBytes(tileByteCount))) {
					
				}
			}
		}

		private void LoadGenericObj(FieldInfo[] fields, Type defaultsType, object item) {
			foreach (var field in fields) {
				if (field.IsStatic || field.Name == "useCode" || field.Name == "unloadedPrefix" || field.Name == "dontDrawFace" ||
				    field.Name == "dontRelocate" || field.Name == "baseGravity" || field.Name == "maxGravity" ||
				    field.Name == "SpawnBiomes")
					continue;

				var fieldType = field.FieldType;
				var fieldName = field.Name;

				if (defaultsType.GetField(fieldName) == null)
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

		private SoundEffect LoadSound(byte[] bytes, string fileName) {
			// Apparently, tConfig were only allowed to use .wav files, so only load those.
			// If we want to support other formats later on, look at ModInternals.cs line 85-107, method "LoadSound"
			
			string extension = Path.GetExtension(fileName);

			using (MemoryStream stream = new MemoryStream(bytes)) {
				switch (extension) {
					case ".wav":
						return SoundEffect.FromStream(stream);
				}
			}

			return null;
		}
	}
}
