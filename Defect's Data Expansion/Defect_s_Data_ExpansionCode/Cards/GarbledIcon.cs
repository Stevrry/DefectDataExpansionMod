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
    /// 乱码的图标：重置你身上的所有能力（包括力量与敏捷），
    /// 再按被清掉的能力层数，把每一项替换成一个「随机能力 × 衰退」（层数减半）。
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

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

            Creature creature = Owner.Creature;

            // 先快照：下面要一边移除一边施加，枚举中途改集合会直接抛异常。
            List<PowerModel> snapshot = creature.Powers.ToList();

            // 临时能力（ITemporaryPower，比如「本回合 +2 力量」的那层包装）也要拆掉，
            // 但它和它内部真正生效的能力（StrengthPower / DexterityPower）是重复记账的，
            // 所以「换几次」只按真正的那个算，否则一份力量会被算两遍。
            List<PowerModel> replaced = snapshot
                .Where(power => power is not ITemporaryPower)
                .ToList();

            // 全部清空 —— 力量、敏捷、集中，以及临时能力的包装，一个不留。
            foreach (PowerModel power in snapshot)
            {
                await PowerCmd.Remove(power);
            }

            List<Replacement> pool = CandidatePool();

            foreach (PowerModel power in replaced)
            {
                // 「衰退」：换来的能力只有原来一半的层数（向下取整，1 层变 0 层就不再补）
                int halved = power.Amount / 2;
                if (halved <= 0)
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
        /// 候选池 = 角色卡池里的能力 ∪ 力量 / 敏捷 / 降低力量 / 降低敏捷。
        /// </summary>
        private List<Replacement> CandidatePool()
        {
            List<Replacement> pool = CharacterPowerPool()
                .Select(power => new Replacement(power, 1m))
                .ToList();

            pool.Add(new Replacement(ModelDb.Power<StrengthPower>(), 1m));   // 力量
            pool.Add(new Replacement(ModelDb.Power<DexterityPower>(), 1m));  // 敏捷
            pool.Add(new Replacement(ModelDb.Power<StrengthPower>(), -1m));  // 降低力量
            pool.Add(new Replacement(ModelDb.Power<DexterityPower>(), -1m)); // 降低敏捷

            return pool;
        }

        /// <summary>
        /// 「这个角色卡池里的能力」引擎里没有现成的表（没有 PowerPool 这种东西），
        /// 所以从角色自己的卡池反推：把该角色每张牌的 PowerVar&lt;T&gt; 里的 T 收集起来。
        /// IsVisible 用来滤掉那些幕后干活、不该露面的能力。
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
