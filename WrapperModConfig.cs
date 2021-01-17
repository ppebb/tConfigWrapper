using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace tConfigWrapper {
	public class WrapperModConfig : ModConfig {

		public override ConfigScope Mode => ConfigScope.ClientSide;

		[DefaultValue(false)]
		[Label("Send logs when you encounter an error during mod loading")]
		[Tooltip("Please leave this enabled as it helps the developers resolve bugs quicker.")]
		public bool SendConfig { get; set; }
	}
}
