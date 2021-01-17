using Terraria.ModLoader;

namespace tConfigWrapper.DataTemplates {
	public struct DisplayName {
		public DisplayName(bool display, string displayName) {
			DoDisplayName = display;
			Name = displayName;
		}

		public bool DoDisplayName { get; }
		public string Name { get; }

		public override string ToString() => $"({DoDisplayName}, {Name})";
	}
}
