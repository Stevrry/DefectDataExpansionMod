using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Cards
{
    /// <summary>
    /// 乱码的图标：将你身上所有能力与状态的层数除以 2，并重置成随机的一项。
    /// 抽取**不分类别** —— 正面能力可能被换成一个负面状态，反过来也一样。
    /// </summary>
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class GarbledIcon : ModCardTemplate
    {
        /// <summary>「衰退」悬停说明所在的本地化表。</summary>
        private const string StaticHoverTipTable = "static_hover_tips";

        /// <summary>「衰退」在 static_hover_tips 里的条目名（后面接 .title / .description）。</summary>
        private const string DeclineLocEntry = "DEFECT_S_DATA_EXPANSION_DECLINE";

        /// <summary>
        /// 不进候选池的「怪物专属特性」（负面状态）。
        ///
        /// 判据是「除了怪物自己，游戏里没有任何卡牌 / 遗物 / 药水 / 事件在用它」
        ///
        /// 新增怪物专属特性时需要往这里补。
        /// </summary>
        private static readonly HashSet<Type> MonsterOnlyDebuffs =
        [
            typeof(ConstrictPower),     // 紧缠      SlitheringStrangler
            typeof(DampenPower),        // 抑制      MagiKnight
            typeof(ImbalancedPower),    // 失衡      BowlbugRock
            typeof(PlowPower),          // 横冲直撞  CeremonialBeast
            typeof(ShriekPower),        // 尖啸      TerrorEel
            typeof(SlowPower),          // 缓慢      BygoneEffigy
            typeof(SurroundedPower),    // 遭到包围  Rocket
            typeof(TenderPower)         // 柔嫩      HunterKiller
        ];

        public GarbledIcon() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
        {
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

            Creature creature = Owner.Creature;

            // 先快照：下面要一边移除一边施加，枚举中途改集合会直接抛异常。
            // 寄生在别的能力上的临时能力（ITemporaryPower）跳过 —— 硬拆掉会让它的宿主对不上账。
            List<PowerModel> powers = creature.Powers
                .Where(power => power is not ITemporaryPower)
                .ToList();

            foreach (PowerModel power in powers)
            {
                int halved = power.Amount / 2;

                await PowerCmd.Remove(power);

                if (halved == 0)
                {
                    continue;
                }

                PowerModel? replacement = PickReplacement();
                if (replacement is null)
                {
                    continue;
                }

                // 施加者填自己、来源填这张牌，和原版自施加能力的写法一致
                await PowerCmd.Apply(choiceContext, replacement, creature, halved, creature, this);
            }
        }

        /// <summary>
        /// 从候选池里等概率抽一个，**不看被换掉的那个是正面还是负面** ——
        /// 正面能力可能被换成负面状态，负面状态也可能被换成正面能力，这就是这张牌要的「跨种类随机」。
        /// </summary>
        private PowerModel? PickReplacement()
        {
            return Owner.RunState.Rng.CombatCardGeneration.NextItem(CandidatePool())?.ToMutable();
        }

        /// <summary>
        /// 候选池 = 「这个角色能拿到的那批能力」 ∪ 「所有露脸的负面状态（去掉怪物专属特性）」。
        /// </summary>
        private List<PowerModel> CandidatePool()
        {
            return CharacterBuffPool()
                .Concat(ModelDb.AllPowers.Where(power =>
                    power.IsVisible
                    && power.Type == PowerType.Debuff
                    && !MonsterOnlyDebuffs.Contains(power.GetType())))
                .ToList();
        }

        /// <summary>
        /// 「这个角色能获得的正面能力」引擎里没有现成的表（没有 PowerPool 这种东西），
        /// 所以从角色自己的卡池反推：把该角色每张牌的 PowerVar&lt;T&gt; 里的 T 收集起来。
        /// IsVisible 用来滤掉那些幕后干活、不该露面的能力。
        /// </summary>
        private IEnumerable<PowerModel> CharacterBuffPool()
        {
            HashSet<Type> types = new();

            foreach (CardModel card in Owner.Character.CardPool.AllCards)
            {
                foreach (KeyValuePair<string, DynamicVar> entry in card.DynamicVars)
                {
                    Type varType = entry.Value.GetType();
                    if (!varType.IsGenericType || varType.GetGenericTypeDefinition() != typeof(PowerVar<>))
                    {
                        continue;
                    }

                    types.Add(varType.GetGenericArguments()[0]);
                }
            }

            return ModelDb.AllPowers.Where(power => power.IsVisible && power.Type == PowerType.Buff && types.Contains(power.GetType()));
        }

        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            new HoverTip(
                new LocString(StaticHoverTipTable, DeclineLocEntry + ".title"),
                new LocString(StaticHoverTipTable, DeclineLocEntry + ".description"))
        ];

        protected override void OnUpgrade()
        {
            EnergyCost.UpgradeBy(-1);
        }
    }
}
