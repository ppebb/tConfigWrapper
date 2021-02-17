using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace tConfigWrapper.Common {
	public class SuffixLogic : GlobalItem {
		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) { // This is good enough for now. Needs to be replaced with a detour or IL or something so the string works in more than just in inventory.
			foreach (ModPrefix prefix in LoadStep.suffixes) {
				if (item.prefix == mod.PrefixType(prefix.Name)) {
					int nameIndex = tooltips.FindIndex(line => line.Name == "ItemName");
					string die = tooltips[nameIndex].text;
					tooltips[nameIndex].text = tooltips[nameIndex].text.Replace($"{prefix.DisplayName.GetTranslation("English")} ", "") + $" {prefix.DisplayName.GetTranslation("English")}";
				}
			}
		}
	}
}