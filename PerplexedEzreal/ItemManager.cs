using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;  
namespace PerplexedEzreal
{
    public class ItemManager
    {
        private static Obj_AI_Hero Player = ObjectManager.Player;
        public static List<Item> Items;
        public static void Initialize()
        {
            Items = new List<Item>();
            //Offensive
            Items.Add(new Item("Cutlass", "比尔吉沃特弯刀", 3144, ItemType.Offensive, 450));
            Items.Add(new Item("BORK", "破败", 3153, ItemType.Offensive, 450));
            Items.Add(new Item("Ghostblade", "幽梦之灵", 3142, ItemType.Offensive, Player.AttackRange));
            Items.Add(new Item("Gunblade", "海克斯科技枪刃", 3146, ItemType.Offensive, 700));
            //Defensive
            Items.Add(new Item("Seraphs", "炽天使之拥", 3040, ItemType.Defensive));
            Items.Add(new Item("Zhonyas", "中娅沙漏", 3157, ItemType.Defensive));
            //Cleanse
            Items.Add(new Item("QSS", "水银饰带", 3140, ItemType.Cleanse));
            Items.Add(new Item("Mercurial", "水银弯刀", 3139, ItemType.Cleanse));

            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            double incomingDmg = 0;
            if (sender.Type == GameObjectType.obj_AI_Hero && sender.IsEnemy && args.Target.Type == GameObjectType.obj_AI_Hero && args.Target.IsMe)
            {
                Obj_AI_Hero attacker = ObjectManager.Get<Obj_AI_Hero>().First(hero => hero.NetworkId == sender.NetworkId);
                Obj_AI_Hero attacked = ObjectManager.Get<Obj_AI_Hero>().First(hero => hero.NetworkId == args.Target.NetworkId);

                SpellDataInst spellData = attacker.Spellbook.Spells.FirstOrDefault(hero => args.SData.Name.Contains(hero.SData.Name));
                SpellSlot spellSlot = spellData == null ? SpellSlot.Unknown : spellData.Slot;

                if (spellSlot == SpellSlot.Q || spellSlot == SpellSlot.W || spellSlot == SpellSlot.E || spellSlot == SpellSlot.R)
                    incomingDmg = Damage.GetSpellDamage(attacker, attacked, spellSlot);
            }
            else if (sender.Type == GameObjectType.obj_AI_Turret && sender.IsEnemy && args.Target.Type == GameObjectType.obj_AI_Hero && args.Target.IsMe)
                incomingDmg = sender.BaseAttackDamage;
            if (incomingDmg > 0)
            {
                UseDefensiveItemsIfInDanger(incomingDmg);
                SpellManager.UseHealIfInDanger(incomingDmg);
            }
        }

        public static void UseDefensiveItemsIfInDanger(double incomingDmg)
        {
            foreach (var item in ItemManager.Items)
            {
                if (item.Type == ItemType.Defensive)
                    item.UseIfInDanger(incomingDmg);
            }
        }

        public static void UseOffensiveItems()
        {
            foreach (var item in ItemManager.Items)
            {
                if (item.Type == ItemType.Offensive)
                {
                    var target = TargetSelector.GetTarget(item.Range, TargetSelector.DamageType.Magical);
                    if(target.IsValidTarget(item.Range))
                        item.Use(target);
                }
            }
        }

        public static void CleanseCC()
        {
            if (IsUnderCC())
            {
                foreach (var item in ItemManager.Items)
                {
                    if (item.Type == ItemType.Cleanse)
                        item.Use();
                }
            }
        }

        public static bool IsUnderCC()
        {
            return Player.HasBuffOfType(BuffType.Blind) || Player.HasBuffOfType(BuffType.Charm) || Player.HasBuffOfType(BuffType.Fear) || Player.HasBuffOfType(BuffType.Flee) || Player.HasBuffOfType(BuffType.Snare) || Player.HasBuffOfType(BuffType.Taunt) || Player.HasBuffOfType(BuffType.Suppression) || Player.HasBuffOfType(BuffType.Stun) || Player.HasBuffOfType(BuffType.Polymorph) || Player.HasBuffOfType(BuffType.Silence) || Player.HasBuffOfType(BuffType.Slow);
        }
    }

    public class Item
    {
        public string ShortName { get; set; }
        public string Name { get; set; }
        public int ID { get; set; }
        public ItemType Type { get; set; }
        public float Range { get; set; }
        public bool ShouldUse { get { return Config.ShouldUseItem(this.ShortName); } }
        public int UseOnPercent { get { return Config.UseOnPercent(this.ShortName); } }
        public Item(string shortName, string name, int id, ItemType type, float range = 0)
        {
            this.ShortName = shortName;
            this.Name = name;
            this.ID = id;
            this.Type = type;
            this.Range = range;
        }

        public void Use(Obj_AI_Hero target = null)
        {
            if (this.ShouldUse)
            {
                if (Items.CanUseItem(this.ID))
                    Items.UseItem(this.ID, target);
            }
        }

        public void UseIfInDanger(double incomingDmg)
        {
            Obj_AI_Hero Player = ObjectManager.Player;
            if (this.ShouldUse)
            {
                int healthToUse = (int)(Player.MaxHealth / 100) * this.UseOnPercent;
                if ((Player.Health - incomingDmg) <= healthToUse)
                    Use(Player);
            }
        }
    }

    public enum ItemType
    {
        Offensive,
        Defensive,
        Cleanse
    }
}
