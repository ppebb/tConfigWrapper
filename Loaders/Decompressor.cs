using SevenZip;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace tConfigWrapper.Loaders {
	internal class Decompressor {
		private static int _decompressTasksCompleted = 0; // Total number of items decompressed
		private static int _decompressTotalFiles = 0; // Total number of items that need to be decompressed

		internal static void DecompressMod(string objPath, ConcurrentDictionary<string, MemoryStream> streams) {
			LoadStep.UpdateSubProgressText("Decompressing");

			SevenZipExtractor extractor = new SevenZipExtractor(objPath);

			List<string> fileNames = extractor.ArchiveFileNames.ToList();
			double numThreads = Math.Min((double)ModContent.GetInstance<LoadConfig>().NumThreads, fileNames.Count);

			List<Task> tasks = new List<Task>();

			// Split the files into numThreads chunks
			var chunks = new List<List<string>>();
			int chunkSize = (int)Math.Round(fileNames.Count / numThreads, MidpointRounding.AwayFromZero);

			for (int i = 0; i < fileNames.Count; i += chunkSize) {
				chunks.Add(fileNames.GetRange(i, Math.Min(chunkSize, fileNames.Count - i)));
			}

			// Create tasks and decompress the chunks
			foreach (var chunk in chunks) {
				tasks.Add(Task.Run(() => DecompressMod(objPath, chunk, streams)));
			}

			// Wait for all tasks to finish
			Task.WaitAll(tasks.ToArray());

			extractor.Dispose();
		}

		private static void DecompressMod(string objPath, List<string> chunk, ConcurrentDictionary<string, MemoryStream> streams) {
			// Create a FileStream with the following arguments to be able to have multiple threads access it
			using (FileStream fileStream = new FileStream(objPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			using (SevenZipExtractor extractor = new SevenZipExtractor(fileStream)) {
				_decompressTotalFiles += chunk.Count; // Counts the number of items that need to be loaded for accurate progress bar
				foreach (var fileName in chunk) {
					LoadStep.UpdateProgress((float)_decompressTasksCompleted / _decompressTotalFiles); // Sets the progress bar
																									   // If the extension is not valid, skip the file
					string extension = Path.GetExtension(fileName);
					if (!(extension == ".ini" || extension == ".cs" || extension == ".png" || extension == ".dll" || extension == ".obj"))
						continue;

					// Create a MemoryStream and extract the file
					MemoryStream stream = new MemoryStream();
					extractor.ExtractFile(fileName, stream);
					stream.Position = 0;
					streams.TryAdd(fileName, stream);
					
					// Increments the number of tasks completed for accurate progress display
					_decompressTasksCompleted++; 
				}
			}
		}
	}
}