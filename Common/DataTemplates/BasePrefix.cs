using Terraria;
using Terraria.ModLoader;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace tConfigWrapper.Common.DataTemplates {
	public class BasePrefix : ModPrefix {
		private readonly string _name;
		private readonly string _requirement;
		private readonly float _rollChance;
		private readonly Dictionary<string, string> _itemFields = new Dictionary<string, string>();
		private readonly Dictionary<string, string> _playerFields = new Dictionary<string, string>();
		
		public BasePrefix() { }

		public BasePrefix(string name, string requirement, float rollChance, Dictionary<string, string> itemFields, Dictionary<string, string> playerFields) {
			_name = name;
			_requirement = requirement;
			_rollChance = rollChance;
			_itemFields = itemFields;
			_playerFields = playerFields;
		}

		public override void SetDefaults() {
			DisplayName.SetDefault(_name);
		}

		public override float RollChance(Item item) {
			return _rollChance;
		}

		public override PrefixCategory Category {
			get {
				switch (_requirement) {
					case "melee":
						return PrefixCategory.Melee;
					case "ranged":
						return PrefixCategory.Ranged;
					case "magic":
						return PrefixCategory.Magic;
					case "accessory":
						return PrefixCategory.Accessory;
					default:
						return PrefixCategory.AnyWeapon;
				}
			}
		}

		public override bool Autoload(ref string name) {
			return false;
		}

		public override void Apply(Item item) {
			foreach (var stat in _itemFields) {
				float statValue = float.Parse(stat.Value);
				switch (stat.Key) {
					case "manaCost":
						item.mana = (int)(item.mana * statValue);
						continue;
					case "damage":
						item.damage = (int)(item.damage * statValue);
						continue;
					case "scale":
						item.scale *= statValue;
						continue;
					case "knockback":
						item.knockBack *= statValue;
						continue;
					case "shootSpeed":
						item.shootSpeed *= statValue;
						continue;
					case "speed":
						item.useTime = (int)(item.useTime * statValue);
						item.useAnimation = (int)(item.useAnimation * statValue);
						continue;
					case "defense":
						item.defense += (int)statValue;
						continue;
					case "crit":
						item.crit += (int)statValue;
						continue;
				}
			}
		}

		public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus) {
			Player player = Main.LocalPlayer;

			foreach (var stat in _playerFields) {
				float statValue = float.Parse(stat.Value);
				switch (stat.Key) {
					case "defense":
						player.statDefense += (int)statValue;
						continue;
					case "crit":
						player.magicCrit += (int)statValue;
						player.meleeCrit += (int)statValue;
						player.rangedCrit += (int)statValue;
						player.thrownCrit += (int)statValue;
						continue;
					case "mana":
						player.statManaMax2 += (int)statValue;
						continue;
					case "damage":
						player.allDamage += statValue;
						continue;
					case "moveSpeed":
						player.moveSpeed *= statValue;
						continue;
					case "meleeSpeed":
						player.meleeSpeed *= statValue;
						continue;
				}
			}
		}
	}
}