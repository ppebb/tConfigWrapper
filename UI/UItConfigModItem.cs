using Terraria.GameContent.UI.Elements;

namespace tConfigWrapper.UI {
	public class UItConfigModItem {
		private const float PADDING = 5f;
		private const int MAXREFERENCESTOSHOWINTOOLTIP = 16;

		private UIImage _moreInfoButton;
		private UIImage _modIcon;
		//private UIHoverImage _keyImage;
		private UIImage _configButton;
		private UIText _modName;
		//private UIModStateText _uiModStateText;
		//private UIHoverImage _modReferenceIcon;
		private readonly string _mod;

		public UItConfigModItem(string mod) {
			_mod = mod;
		}
	}
}
