//using Microsoft.Xna.Framework;
//using Mono.Cecil;
//using Mono.Cecil.Cil;
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Reflection;
//using Terraria;

//namespace tConfigWrapper.Loaders {
//	internal class AssemblyLoader : BaseLoader {
//		public static ModuleDefinition TerrariaModule = ModuleDefinition.ReadModule(typeof(Main).Assembly.Location);
//		public AssemblyLoader(string modName, ConcurrentDictionary<string, MemoryStream> fileStreams) : base(modName, fileStreams) { }

//		protected override string TargetFolder => "";

//		public override int AddFiles(IEnumerable<string> allFiles) {
//			int addedFiles = 0;
//			foreach (string file in allFiles) {
//				// If file is a dll, add it to files
//				if (Path.GetExtension(file) == ".dll") {
//					files.Add(file);
//					addedFiles++;
//				}
//			}
//			return addedFiles;
//		}

//		protected override void HandleFile(string file) {
//			string asmPath = $"{tConfigWrapper.ModsPath}\\PatchedAssemblies\\{modName}";

//			MemoryStream stream = fileStreams[file];
//			//Do stuff with the DLL lmao
//			AssemblyDefinition asm = GetAssembly(stream);

//			if (!File.Exists(asmPath)) {
//				asm = FixAssembly(asm);
//				asm.Write(asmPath);
//			}

//			Assembly.Load(asmPath);
//		}

//		public override void RegisterContent() { /* Literally nothing to register, just have this because some idiot marked it abstract instead of virual :pensiv: */
//		}

//		internal static void LoadAssembliesIntoCecil() {
//			IEnumerable<Assembly> modAssemblies = Terraria.ModLoader.ModLoader.Mods.Select(x => x.Code);
//			Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
//			IEnumerable<Assembly> nonModAssemblies = allAssemblies.Except(modAssemblies);

//			foreach (Assembly asm in nonModAssemblies) {
//				AssemblyDefinition.ReadAssembly(asm.Location);
//			}
//		}

//		private AssemblyDefinition GetAssembly(MemoryStream stream) {
//			AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(stream);
//			return asm;
//		}

//		private AssemblyDefinition FixAssembly(AssemblyDefinition asm) {
//			foreach (ModuleDefinition module in asm.Modules) {
//				FixModule(module);
//				// Eventually generics will break (:
//			}

//			return asm;
//		}

//		private void FixModule(ModuleDefinition module) {
//			foreach (TypeDefinition type in module.Types)
//				FixType(type);
//		}

//		private void FixType(TypeDefinition type) {
//			foreach (FieldDefinition field in type.Fields)
//				FixField(field);

//			foreach (MethodDefinition method in type.Methods)
//				FixMethod(method);

//			foreach (TypeDefinition nestedType in type.NestedTypes)
//				FixType(nestedType);
//		}

//		private void FixField(FieldDefinition field) {
//			if (field.FieldType.Scope.ToString().StartsWith("tConfig"))
//				field.FieldType = field.Module.ImportReference(new TypeReference(field.FieldType.Namespace, field.FieldType.Name, TerrariaModule, TerrariaModule).Resolve());
//			FieldDefinition reference = field.Resolve();
//			field.Module.ImportReference(reference);
//		}

//		private void FixMethod(MethodDefinition method) {
//			foreach (ParameterDefinition parameter in method.Parameters)
//				FixParameter(parameter);

//			foreach (Instruction instruction in method.Body.Instructions)
//				FixInstruction(instruction);
//		}

//		private void FixParameter(ParameterDefinition parameter) {
//			if (parameter.ParameterType.Scope.ToString().StartsWith("tConfig"))
//				parameter.ParameterType = parameter.ParameterType.Module.ImportReference(new TypeReference(parameter.ParameterType.Namespace, parameter.ParameterType.Name, TerrariaModule, TerrariaModule).Resolve());
//		}

//		private void FixInstruction(Instruction instruction) {
//			if (instruction.Operand is FieldReference reference) {
//				if (reference.FieldType.Scope.ToString().StartsWith("tConfig")) {
//					string funnyNameBuffer = reference.FieldType.Name;
//					string funnyNameSpaceBuffer = reference.FieldType.Namespace;
//					if (reference.FieldType.Name.ToString() == "Dust[]") {
//						funnyNameBuffer = "dust[]";
//						funnyNameSpaceBuffer = "Terraria";
//					}
//					reference.FieldType = reference.Module.ImportReference(new TypeReference(funnyNameSpaceBuffer, funnyNameBuffer, TerrariaModule, TerrariaModule).Resolve());
//				}

//				if (reference.DeclaringType.Scope.ToString().StartsWith("tConfig"))
//					reference.DeclaringType = reference.Module.ImportReference(new TypeReference(reference.DeclaringType.Namespace, reference.DeclaringType.Name, TerrariaModule, TerrariaModule).Resolve());
//			}
//		}

//		internal static void LoadStaticFields() {
//			TerrariaModule = ModuleDefinition.ReadModule(typeof(Main).Assembly.Location);
//		}

//		internal static void UnloadStaticFields() {
//			TerrariaModule = null;
//		}
//	}
//}