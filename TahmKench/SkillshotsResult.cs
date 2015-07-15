using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace TahmKench
{
    class SkillshotsResult
    {
        private static float wRange = 300;

        public static List<Skillshot> DetectedSkillShots = new List<Skillshot>();
        public static List<Skillshot> EvadeDetectedSkillshots = new List<Skillshot>();

        public static Obj_AI_Hero CCdAlly
        {
            get
            {
                return
                    HeroManager.Allies
                        .Where(
                            unit =>
                                !unit.IsMe && !unit.IsDead &&
                                ObjectManager.Player.Distance(unit.Position) <= wRange)
                                .OrderBy(x => x.CombatType == GameObjectCombatType.Ranged).ThenBy(x => x.Health)
                        .FirstOrDefault(
                            ally =>
                                ally.HasBuffOfType(BuffType.Charm) || ally.HasBuffOfType(BuffType.CombatDehancer) ||
                                ally.HasBuffOfType(BuffType.Fear) || ally.HasBuffOfType(BuffType.Knockback) ||
                                ally.HasBuffOfType(BuffType.Knockup) || ally.HasBuffOfType(BuffType.Polymorph) ||
                                ally.HasBuffOfType(BuffType.Snare) || ally.HasBuffOfType(BuffType.Stun) ||
                                ally.HasBuffOfType(BuffType.Suppression) || ally.HasBuffOfType(BuffType.Taunt));
            }
        }
    }
}
