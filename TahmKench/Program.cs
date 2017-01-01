using System;
using System.Collections.Generic;
using System.Linq;
using Evade;
using LeagueSharp;
using LeagueSharp.Common;

namespace TahmKench
{
    class Program
    {
        private static int wRange = 300;
        static Obj_AI_Hero CCdAlly
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

        public static List<Skillshot> DetectedSkillshots = new List<Skillshot>();

        static Menu config= new Menu("Tahm", "op", true);
        static Orbwalking.Orbwalker orbwalker;

        static Spell q = new Spell(SpellSlot.Q, 800);
        static Spell w = new Spell(SpellSlot.W, 300) { Delay = 250 };

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            config.AddSubMenu(new Menu("Custom TargetSelector", "tsMenu"));
            TargetSelector.AddToMenu(config.SubMenu("tsMenu"));
            config.AddSubMenu(new Menu("Custom Orbwalker", "orbwalkMenu"));
            orbwalker = new Orbwalking.Orbwalker(config.SubMenu("orbwalkMenu"));

            config.AddItem(new MenuItem("useQ", "Use Q in combo")).SetValue(true);
            config.AddItem(new MenuItem("info", "-----"));
            config.AddItem(new MenuItem("useWSafe", "Use W to dodge skillshots")).SetValue(true);
            config.AddItem(new MenuItem("shieldTargeted", "Shield targeted skills")).SetValue(true);
            config.AddItem(new MenuItem("shieldifXHpAmount", "...if ally looses X % of maxHP")).SetValue(new Slider(20));
            config.AddItem(new MenuItem("shieldifIsUltimate", "..if its an ult")).SetValue(true);
            config.AddItem(new MenuItem("eatCCdAllies", "W cc'ed allies")).SetValue(true);
            config.AddToMainMenu();

            Evade.Config.CreateMenu();

            Game.OnUpdate += Game_OnUpdate;
            Obj_AI_Hero.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
            Drawing.OnDraw += Drawing_OnDraw;
            SkillshotDetector.OnDetectSkillshot += SkillshotDetectorOnOnDetectSkillshot;
        }

        private static void SkillshotDetectorOnOnDetectSkillshot(Skillshot skillshot)
        {
            DetectedSkillshots.Add(skillshot);
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            Render.Circle.DrawCircle(ObjectManager.Player.Position, w.Range, System.Drawing.Color.Blue);
        }

        static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!config.Item("shieldTargeted").GetValue<bool>() || !config.Item("useWSafe").GetValue<bool>())
                return;

            if (sender.IsEnemy && args.SData.TargettingType == SpellDataTargetType.Unit 
                && !args.Target.IsMe)
            {
                var target = (Obj_AI_Hero)args.Target;
                var spell = sender.Spellbook.Spells.FirstOrDefault(x => args.SData.Name.Contains(x.Name));

                if (args.Target.Position.Distance(ObjectManager.Player.Position) <= w.Range)
                {
                    if (sender.GetSpellDamage(target, spell.Name) >= 
                        target.MaxHealth * config.Item("shieldifXHpAmount").GetValue<Slider>().Value / 100)
                        w.Cast(target);
                    if (spell.Slot == SpellSlot.R &&
                        config.Item("shieldifIsUltimate").GetValue<bool>())
                        w.Cast(target);
                }    
            }
        }

        static void Game_OnUpdate(EventArgs args)
        {
            DetectedSkillshots.RemoveAll(skillshot => !skillshot.IsActive());

            CheckCCdAlly();

            if (config.Item("useQ").GetValue<bool>() && q.IsReady() && orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                var target = (TargetSelector.GetTarget(1500, TargetSelector.DamageType.Physical) ??
                               TargetSelector.GetTarget(1500, TargetSelector.DamageType.Magical)) ??
                               TargetSelector.GetTarget(1000, TargetSelector.DamageType.True);

                var pPos = q.GetPrediction(target);

                if (pPos.Hitchance >= HitChance.High)
                    q.Cast(pPos.CastPosition);
            }

            if (config.Item("useWSafe").GetValue<bool>() && w.IsReady())
            {
                var skillshots = DetectedSkillshots;

                foreach (var ally in HeroManager.Allies.Where(x => !x.IsDead && !x.IsMe && Evade.Config.EvadeMenu.Item(x.Name).GetValue<bool>() &&
                    x.Distance(ObjectManager.Player.Position) <= w.Range).OrderBy(x => x.Health))
                {
                    foreach (var skill in skillshots.Where(x => x.Evade()))
                    {
                        var hitTime = skill.StartTick + skill.SpellData.Delay +
                            1000 * (skill.Start.Distance(ally.Position) / skill.SpellData.MissileSpeed);

                        var timeLeft = hitTime - Utils.TickCount;

                        if (!skill.IsSafePath(ally.GetWaypoints(), ally, Game.Ping/2).IsSafe && timeLeft > w.Delay && 
                            timeLeft < w.Delay+300)
                        {
                            w.Cast(ally);
                            break;
                        }
                    }

                }
            }
        }

        private static void CheckCCdAlly()
        {
            if (CCdAlly != null && w.IsReady() && config.Item("eatCCdAllies").GetValue<bool>())
                w.Cast(CCdAlly);

        }
    }
}
