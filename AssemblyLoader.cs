using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using Terraria;

namespace tConfigWrapper {
	public static class AssemblyLoader {
		public static ConcurrentDictionary<string, ModuleDefinition> LoadedModules = new ConcurrentDictionary<string, ModuleDefinition>();
		public static ConcurrentDictionary<string, DynamicMethodDefinition> AllDynamicMethods = new ConcurrentDictionary<string, DynamicMethodDefinition>();
		public static ConcurrentDictionary<string, Action<Player, Rectangle>> UseItemEffect = new ConcurrentDictionary<string, Action<Player, Rectangle>>();

		/// <summary>
		/// Gets the assembly associated with the modName
		/// </summary>
		/// <param name="modName">The name of the mod without the path or extension</param>
		public static ModuleDefinition GetModule(string modName) {
			MemoryStream stream = LoadStep.streamsGlobal[$"{modName}\\{modName}.dll"];
			BinaryReader reader = new BinaryReader(stream);
			string dllVersion = null;
			string url = null;
			Version modVersion = new Version(reader.ReadString());

			//if (modVersion < new Version(0, 35, 0))
			//	return false;

			if (modVersion > new Version(0, 20, 5))
				reader.ReadInt32();

			if (modVersion > new Version("0.22.8") && reader.ReadBoolean()) {
				dllVersion = reader.ReadString();
				url = reader.ReadString();
			}
			ModuleDefinition module = ModuleDefinition.ReadModule(stream);
			LoadedModules.TryAdd(Path.GetFileNameWithoutExtension(modName), module);
			return module;
		}

		public static void FixIL(string modName, ModuleDefinition module) {
			foreach (TypeDefinition type in module.Types) {
				foreach (MethodDefinition method in type.Methods) {
					if (method.Name == ".ctor")
						continue;

					// Do cool stuff to fix IL and make it not broke!
					DynamicMethodDefinition dynamicMethod = method.ToDynamicMethod();
					AllDynamicMethods.TryAdd($"{modName}:{method.DeclaringType.Name.Replace("_", "")}:{dynamicMethod.Name}", dynamicMethod);
				}
			}
		}

		public static void RegisterDelegate(DynamicMethodDefinition dynamicMethod, string delegateFullName) {
			MethodBase method = dynamicMethod.Generate();
			switch (method.Name) {
				case "UseItemEffect":
					UseItemEffect.TryAdd(delegateFullName, (Action<Player, Rectangle>)method.CreateDelegate<Action<Player, Rectangle>>());
					break;
			}
		}

		public static DynamicMethodDefinition ToDynamicMethod(this MethodDefinition method) {
			List<Type> parameters = new List<Type>();
			foreach (var param in method.GenericParameters)
				parameters.Add(Type.GetType(param.FullName));

			DynamicMethodDefinition dynamicMethod = new DynamicMethodDefinition(method.Name, Type.GetType(method.ReturnType.FullName), parameters.ToArray());
			ILProcessor il = dynamicMethod.GetILProcessor();
			
			foreach (Instruction instruction in method.Body.Instructions)
				il.Emit(instruction.OpCode, instruction.Operand);

			return dynamicMethod;
		}

		internal static void LoadStaticFields() {
			AllDynamicMethods = new ConcurrentDictionary<string, DynamicMethodDefinition>();
			LoadedModules = new ConcurrentDictionary<string, ModuleDefinition>();
			UseItemEffect = new ConcurrentDictionary<string, Action<Player, Rectangle>>();
		}

		internal static void UnloadStaticFields() {
			AllDynamicMethods = null;
			LoadedModules = null;
			UseItemEffect = null;
		}
	}
}
