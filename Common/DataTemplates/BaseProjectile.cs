using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace tConfigWrapper.Common.DataTemplates {
	public class BaseProjectile : ModProjectile {
		public override string Texture => "tConfigWrapper/Common/DataTemplates/MissingTexture";
		public override bool CloneNewInstances => true;

		private ProjectileInfo _info;
		private readonly string _name;
		private readonly Texture2D _texture;

		public BaseProjectile() { }

		public BaseProjectile(ProjectileInfo projectileInfo, string name = null, Texture2D texture = null) {
			_info = projectileInfo;
			_name = name;
			_texture = texture;
		}

		public override void SetStaticDefaults() {
			if (_name != null)
				DisplayName.SetDefault(_name);

			if (_texture != null)
				Main.projectileTexture[projectile.type] = _texture;
		}

		/// <summary>
		/// Sets the default values of an <see cref="Projectile"/> by getting the values from <see cref="_info"/>
		/// </summary>
		private void SetDefaultsFromInfo() {
			var infoFields = typeof(ProjectileInfo).GetFields();
			foreach (FieldInfo field in infoFields) {
				var infoFieldValue = field.GetValue(_info);
				var projectileField = typeof(Projectile).GetField(field.Name);

				if (infoFields != null)
					projectileField.SetValue(projectile, infoFieldValue);
			}
		}
	}
}
