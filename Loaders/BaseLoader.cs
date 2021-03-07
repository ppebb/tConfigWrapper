using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Terraria.ModLoader;

namespace tConfigWrapper.Loaders {
	internal abstract class BaseLoader {
		protected abstract string TargetFolder { get; }

		protected readonly string modName;
		protected readonly List<string> files = new List<string>();
		protected readonly ConcurrentDictionary<string, MemoryStream> fileStreams;

		protected static Mod Mod => ModContent.GetInstance<tConfigWrapper>();

		public BaseLoader(string modName, ConcurrentDictionary<string, MemoryStream> fileStreams) {
			this.modName = modName;
			this.fileStreams = fileStreams;
		}

		/// <summary>
		/// This is the first method that's called, it adds all valid files to the <see cref="files"/> list. <br/>
		/// Normally, there's no reason for you to call or override this method.
		/// </summary>
		/// <returns>The amount of added files</returns>
		public virtual int AddFiles(IEnumerable<string> allFiles) {
			int addedFiles = 0;
			foreach (var file in allFiles) {
				if (file.Contains($"{Path.DirectorySeparatorChar}{TargetFolder}{Path.DirectorySeparatorChar}") && Path.GetExtension(file) == ".ini") {
					files.Add(file);
					addedFiles++;
				}
			}

			return addedFiles;
		}

		/// <summary>
		/// This is the second method that's called, and it calls <see cref="HandleFile"/> for every file that was added to <see cref="files"/> by <see cref="AddFiles"/><br/>
		/// Normally, there's no reason for you to call or override this method.
		/// </summary>
		public virtual void IterateFiles(int totalFiles) {
			foreach (string file in files) {
				LoadStep.UpdateSubProgressText(file);
				HandleFile(file);
				LoadStep.TaskCompletedCount++;
				LoadStep.UpdateProgress((float)LoadStep.TaskCompletedCount / totalFiles);
			}
		}

		/// <summary>
		/// This is called for every file in <see cref="files"/>, do whatever you need to do with the files here
		/// </summary>
		/// <param name="file">The name of the file</param>
		protected abstract void HandleFile(string file);

		/// <summary>
		/// This is the last method, here you are supposed to register content by calling Mod.AddX() or similar
		/// </summary>
		public abstract void RegisterContent();

		/// <summary>
		/// Ran when <see cref="Terraria.ModLoader.Mod.PostSetupContent"/> is called.
		/// <see cref="fileStreams"/>, <see cref="files"/> and <see cref="modName"/> are null when this method is called
		/// </summary>
		public virtual void PostSetupContent() { }

		/// <summary>
		/// Ran when <see cref="Terraria.ModLoader.Mod.AddRecipes"/> is called.
		/// <see cref="fileStreams"/>, <see cref="files"/> and <see cref="modName"/> are null when this method is called
		/// </summary>
		public virtual void AddRecipes() { }
	}
}