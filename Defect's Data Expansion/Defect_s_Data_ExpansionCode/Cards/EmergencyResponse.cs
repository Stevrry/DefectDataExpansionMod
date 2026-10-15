using System.Collections.Generic;
using System.Threading.Tasks;
using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Cards
{
    /// <summary>
    /// 应急处理：失去所有的过热。每失去 1 层过热，就获得 1 点能量并受到 1 点伤害。升级后获得保留。
    /// </summary>
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class EmergencyResponse : ModCardTemplate
    {
        /// <summary>每失去 1 层过热给几点能量（描述里也是这个数）。</summary>
        private const decimal EnergyPerStack = 1m;

        public EmergencyResponse() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            // 每层过热让人挨几点伤害；实际挨的总量 = 层数 × 这个值
            new DamageVar(1m, ValueProp.Unpowered)
        ];

        /// <summary>悬停时说明它会烧掉什么、换来什么。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromPower<OverheatPower>(),
            EnergyHoverTip
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

            if (Owner.Creature.GetPower<OverheatPower>() is not { Amount: > 0 } overheat)
            {
                return;
            }

            // 层数先记下来 —— ModifyAmount 跑完 Amount 就归零了
            int lostStacks = overheat.Amount;

            // 必须走 ModifyAmount 而不是 PowerCmd.Remove：过热那笔「掉层返还能量」要发出来
            await PowerCmd.ModifyAmount(choiceContext, overheat, -lostStacks, Owner.Creature, this);

            await PlayerCmd.GainEnergy(lostStacks * EnergyPerStack, Owner);

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
