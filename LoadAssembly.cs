using Mono.Cecil;
using Terraria.ModLoader;
using System.IO;

namespace tConfigWrapper {
	public static class LoadAssembly {
		public static void Yes(string modName) {
			string cringe = $"{Path.GetFileNameWithoutExtension(modName)}\\{Path.GetFileNameWithoutExtension(modName)}.dll";
			ModuleDefinition module = ModuleDefinition.ReadModule(LoadStep.streamsGlobal[cringe]);

			foreach (TypeDefinition type in module.Types) {
				ModContent.GetInstance<tConfigWrapper>().Logger.Debug(type.Name); // Wow this actually works how
			}
		}
	}
}
