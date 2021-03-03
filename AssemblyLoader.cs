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
		public static ConcurrentDictionary<string, Action<Player, Rectangle>> UseItemEffect = new ConcurrentDictionary<string, Action<Player, Rectangle>>();
		public static ModuleDefinition TerrariaModule = ModuleDefinition.ReadModule(typeof(Main).Assembly.Location);

		/// <summary>
		/// Gets the assembly associated with the modName
		/// </summary>
		/// <param name="modName">The name of the mod without the path or extension</param>
		public static ModuleDefinition GetModule(string modName) {
			MemoryStream stream = LoadStep.streamsGlobal[$"{modName}\\{modName}.dll"];
			ModuleDefinition module = ModuleDefinition.ReadModule(stream);
			LoadedModules.TryAdd(Path.GetFileNameWithoutExtension(modName), module);
			return module;
		}

		public static void FixIL(string modName, ModuleDefinition module) {
			foreach (TypeDefinition type in module.Types) {
				foreach (MethodDefinition method in type.Methods) {
					if (method.Name != "UseItemEffect")
						continue;

					// Do cool stuff to fix IL and make it not broke!

					DynamicMethodDefinition dynamicMethod = method.ToDynamicMethod();
					RegisterDelegate(dynamicMethod, $"{modName}:{method.DeclaringType.Name.Replace("_", "")}:{dynamicMethod.Name}");
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
			foreach (var param in method.Parameters) {
				string[] typeArr = param.ParameterType.FullName.Split('.');
				Array.Resize(ref typeArr, typeArr.Length - 1);
				string nameSpace = string.Join(".", typeArr);

				Assembly asm = null;

				if (nameSpace.StartsWith("System"))
					asm = typeof(object).Assembly;
				else if (nameSpace.StartsWith("Terraria"))
					asm = typeof(Main).Assembly;
				else if (nameSpace.StartsWith("Microsoft"))
					asm = typeof(Vector2).Assembly;

				Type type = asm.GetType(param.ParameterType.FullName);
				
				if (param.ParameterType.IsByReference)
					type = type.MakeByRefType();

				parameters.Add(type);
			}

			DynamicMethodDefinition dynamicMethod = new DynamicMethodDefinition($"{method.DeclaringType.Name}_{method.Name}", Type.GetType(method.ReturnType.FullName), parameters.ToArray());
			ILProcessor il = dynamicMethod.GetILProcessor();

			foreach (Instruction instruction in method.Body.Instructions) {
				if (instruction.Operand is FieldReference reference) {
					TypeReference fieldType = reference.FieldType;

					if (fieldType.Scope.ToString().StartsWith("tConfig")) {
						fieldType = new TypeReference(fieldType.Namespace, fieldType.Name, TerrariaModule, TerrariaModule);
					}
					TypeReference declaringType = reference.DeclaringType;
					if (declaringType.Scope.ToString().StartsWith("tConfig")) {
						declaringType = new TypeReference(declaringType.Namespace, declaringType.Name, TerrariaModule, TerrariaModule);
					}
					il.Emit(instruction.OpCode, new FieldReference(reference.Name, fieldType, declaringType));
				}
				else
					il.Emit(instruction.OpCode, instruction.Operand);
			}

			foreach (Instruction inst in dynamicMethod.Definition.Body.Instructions) {
				if (inst.Operand is Instruction targetOrig) {
					inst.Operand = dynamicMethod.Definition.Body.Instructions[method.Body.Instructions.IndexOf(targetOrig)];
				}
			}

			return dynamicMethod;
		}

		internal static void LoadStaticFields() {
			LoadedModules = new ConcurrentDictionary<string, ModuleDefinition>();
			UseItemEffect = new ConcurrentDictionary<string, Action<Player, Rectangle>>();
			TerrariaModule = ModuleDefinition.ReadModule(typeof(Main).Assembly.Location);
	}

		internal static void UnloadStaticFields() {
			LoadedModules = null;
			UseItemEffect = null;
			TerrariaModule = null;
		}
	}
}
