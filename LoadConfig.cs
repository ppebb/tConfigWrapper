using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace tConfigWrapper {
	public class LoadConfig : ModConfig {

		public override ConfigScope Mode => ConfigScope.ClientSide;

		[DefaultValue(false)]
		[Label("Send logs when you encounter an error during mod loading.")]
		[Tooltip("Please leave this enabled as it helps the developers resolve bugs quicker.")]
		public bool SendConfig { get; set; }

		
		[DefaultValue(3)]
		[Label("The number of threads used when decompressing mods.")]
		[Tooltip("Don't touch this unless you know what you are doing.")]
		[Range(1, 10)]
		public int NumThreads { get; set; }
	}
}
