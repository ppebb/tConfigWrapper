using Terraria.ModLoader;

namespace tConfigWrapper.DataTemplates {

	public class BaseNPC : ModNPC {
		public override string Texture => "tConfigWrapper/DataTemplates/MissingTexture";
		public override bool CloneNewInstances => true;
	}
}
