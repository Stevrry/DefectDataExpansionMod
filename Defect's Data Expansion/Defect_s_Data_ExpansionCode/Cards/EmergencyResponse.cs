using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Cards
{
    /// <summary>
    /// 应急处理：转化所有的过热。每失去 1 层过热，受到 2 点伤害。升级后获得保留。
    /// </summary>
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class EmergencyResponse : ModCardTemplate
    {
        /// <summary>「转化」悬停说明所在的本地化表。</summary>
        private const string StaticHoverTipTable = "static_hover_tips";
        /// <summary>「转化」在 static_hover_tips 里的条目名（后面接 .title / .description）。</summary>
        private const string TransformLocEntry = "DEFECT_S_DATA_EXPANSION_TRANSFORM";

        public EmergencyResponse() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            // 每层过热让人挨几点伤害；实际挨的总量 = 层数 × 这个值
            new DamageVar(2m, ValueProp.Unpowered)
        ];

        /// <summary>悬停时说明它会烧掉什么。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromPower<OverheatPower>(),
            new HoverTip(
                new LocString(StaticHoverTipTable, TransformLocEntry + ".title"),
                new LocString(StaticHoverTipTable, TransformLocEntry + ".description"))
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

            if (Owner.Creature.GetPower<OverheatPower>() is not { Amount: > 0 } overheat)
            {
                return;
            }

            // 层数先记下来 —— ModifyAmount 跑完 Amount 就归零了，之后要拿它算伤害
            int lostStacks = overheat.Amount;

            // 「转化」那条路：掉层但不返还能量。这张牌只结算伤害，不再附带过热那笔掉层返能。
            await overheat.WithoutEnergyGain(async () =>
                await PowerCmd.ModifyAmount(choiceContext, overheat, -lostStacks, Owner.Creature, this));

            // 自伤走 Unpowered：力量之类的加成不该改这个数。格挡照常能吃掉它。
            await CreatureCmd.Damage(
                choiceContext,
                Owner.Creature,
                lostStacks * DynamicVars.Damage.BaseValue,
                ValueProp.Unpowered,
                this,
                cardPlay);
        }

        /// <summary>
        /// 升级获得保留。
        /// </summary>
        protected override void OnUpgrade()
        {
            AddKeyword(CardKeyword.Retain);
        }
    }
}
