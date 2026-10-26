using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Relics;
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

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Cards
{
    /// <summary>
    /// 乱码的图标：重置你身上的能力（包括力量与敏捷），
    /// 再按被清掉的能力层数，把每一项替换成一个「随机能力 × 衰退」（层数减半）。
    ///
    /// 临时能力（ITemporaryPower，例如「本回合 +2 力量」）整类跳过：
    /// 包装本身不拆，它内部施加的那类能力（力量 / 敏捷 / 集中等）也不重置。
    /// 原因是临时能力的层数和它内部真正生效的能力是重复记账的，硬拆会破坏它回合结束时的结算。
    ///
    /// 候选池只有：打出这张牌的角色的能力池、力量、敏捷、降低力量、降低敏捷。
    /// </summary>
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class GarbledIcon : ModCardTemplate
    {
        /// <summary>衰退和随机化悬停说明所在的本地化表。</summary>
        private const string StaticHoverTipTable = "static_hover_tips";

        /// <summary>衰退和随机化在 static_hover_tips 里的条目名（后面接 .title / .description）。</summary>
        private const string DeclineLocEntry = "DEFECT_S_DATA_EXPANSION_DECLINE";
        private const string RandomizationLocEntry = "DEFECT_S_DATA_EXPANSION_RANDOMIZATION";

        public GarbledIcon() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
        {
        }

        /// <summary>
        /// 卡图路径
        /// </summary>
        public override CardAssetProfile AssetProfile => new(
            PortraitPath: $"res://Defect_s_Data_Expansion/images/cards/{GetType().Name}.png"
        );

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

            Creature creature = Owner.Creature;

            // 先快照：下面要一边移除一边施加，枚举中途改集合会直接抛异常。
            List<PowerModel> snapshot = creature.Powers.ToList();

            // 临时能力整类保护：包装本身（ITemporaryPower）不拆，
            // 它内部施加的那类能力（InternallyAppliedPower，比如 StrengthPower）也一并跳过。
            // 那些内部能力的层数里混着临时部分，单独重置会让包装在回合结束时多扣一次。
            HashSet<Type> protectedTypes = snapshot
                .OfType<ITemporaryPower>()
                .Select(power => power.InternallyAppliedPower.GetType())
                .ToHashSet();

            List<PowerModel> removable = snapshot
                .Where(power => power is not ITemporaryPower)
                .Where(power => !protectedTypes.Contains(power.GetType()))
                .ToList();

            // 清空所有「非临时」能力 —— 力量、敏捷、集中等被保护的类型除外。
            foreach (PowerModel power in removable)
            {
                await PowerCmd.Remove(power);
            }

            // 被保护的类别也不进候选池：否则虽然没重置它，却可能又随机给同一类能力加层，
            // 就达不到「原样保留」了。
            List<Replacement> pool = CandidatePool(protectedTypes);

            foreach (PowerModel power in removable)
            {
                // 「衰退」：换来的能力只有原来一半的层数（向下取整，1 层变 0 层就不再补）
                int halved = power.Amount / 2;
                if (halved <= 0 || pool.Count == 0)
                {
                    continue;
                }

                Replacement chosen = pool[Owner.RunState.Rng.CombatCardGeneration.NextInt(0, pool.Count)];

                // 施加者填自己、来源填这张牌，和原版自施加能力的写法一致。
                // 负数的力量 / 敏捷就是「降低力量 / 降低敏捷」（这两个能力本身允许负数）。
                await PowerCmd.Apply(
                    choiceContext,
                    chosen.Power.ToMutable(),
                    creature,
                    halved * chosen.Sign,
                    creature,
                    this);
            }
        }

        /// <summary>候选池里的一项：哪个能力、带什么符号（-1 就是「降低」版）。</summary>
        private readonly record struct Replacement(PowerModel Power, decimal Sign);

        /// <summary>
        /// 候选池 = 角色卡池里的<see cref="PowerType.Buff">正面能力</see> ∪ 力量 / 敏捷 / 降低力量 / 降低敏捷。
        /// 被临时能力保护起来的类别会整类剔除，保证它们不会被这张牌改动。
        /// </summary>
        private List<Replacement> CandidatePool(HashSet<Type> protectedTypes)
        {
            List<Replacement> pool = CharacterPowerPool()
                .Where(power => !protectedTypes.Contains(power.GetType()))
                .Select(power => new Replacement(power, 1m))
                .ToList();

            if (!protectedTypes.Contains(typeof(StrengthPower)))
            {
                pool.Add(new Replacement(ModelDb.Power<StrengthPower>(), 1m));   // 力量
                pool.Add(new Replacement(ModelDb.Power<StrengthPower>(), -1m));  // 降低力量
            }

            if (!protectedTypes.Contains(typeof(DexterityPower)))
            {
                pool.Add(new Replacement(ModelDb.Power<DexterityPower>(), 1m));  // 敏捷
                pool.Add(new Replacement(ModelDb.Power<DexterityPower>(), -1m)); // 降低敏捷
            }

            return pool;
        }

        /// <summary>
        /// 「这个角色卡池里的能力」引擎里没有现成的表（没有 PowerPool 这种东西），
        /// 所以从角色自己的卡池反推：把该角色每张牌的 PowerVar&lt;T&gt; 里的 T 收集起来。
        /// 只要正面能力（<see cref="PowerType.Buff"/>）—— 卡牌里的负面 PowerVar 是打给敌人的，
        /// 不该被这张牌随机到自己头上。IsVisible 用来滤掉那些幕后干活、不该露面的能力。
        /// </summary>
        private IEnumerable<PowerModel> CharacterPowerPool()
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

            // 力量和敏捷下面单独加（含「降低」版），这里去掉免得重复加权
            return ModelDb.AllPowers.Where(power =>
                power.IsVisible
                && power.Type == PowerType.Buff
                && types.Contains(power.GetType())
                && power.GetType() != typeof(StrengthPower)
                && power.GetType() != typeof(DexterityPower));
        }

        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromPower<StrengthPower>(),
            HoverTipFactory.FromPower<DexterityPower>(),
            new HoverTip(
                new LocString(StaticHoverTipTable, DeclineLocEntry + ".title"),
                new LocString(StaticHoverTipTable, DeclineLocEntry + ".description")),
            new HoverTip(
                new LocString(StaticHoverTipTable, RandomizationLocEntry + ".title"),
                new LocString(StaticHoverTipTable, RandomizationLocEntry + ".description"))
        ];

        protected override void OnUpgrade()
        {
            EnergyCost.UpgradeBy(-1);
        }
    }
}
