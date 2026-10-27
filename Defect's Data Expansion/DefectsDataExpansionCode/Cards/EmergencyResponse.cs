using DefectsDataExpansion.DefectsDataExpansionCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace DefectsDataExpansion.DefectsDataExpansionCode.Cards
{
    /// <summary>
    /// 应急处理：保留。失去所有的过热。每失去 1 层过热，受到 2(1) 点伤害。
    /// </summary>
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class EmergencyResponse : ModCardTemplate
    {
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
        ];

        /// <summary>保留。</summary>
        public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [
            CardKeyword.Retain
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

            if (Owner.Creature.GetPower<OverheatPower>() is not { Amount: > 0 } overheat)
            {
                return;
            }

            // 层数先记下来
            int lostStacks = overheat.Amount;

            // 「失去」而不是「转化」：走普通的掉层，过热会按层数照常返还能量
            await PowerCmd.ModifyAmount(choiceContext, overheat, -lostStacks, Owner.Creature, this);

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
        /// 升级降低受到的伤害。
        /// </summary>
        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(-1m);
        }
    }
}
