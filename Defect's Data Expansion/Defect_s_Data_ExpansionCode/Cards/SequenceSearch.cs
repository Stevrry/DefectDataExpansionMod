using System.Collections.Generic;
using System.Threading.Tasks;
using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Cards
{
    /// <summary>
    /// 序列检索：本回合失去1(2)点集中，抽2(3)张牌。
    /// </summary>
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class SequenceSearch : ModCardTemplate
    {
        public SequenceSearch() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new CardsVar(2),
            new PowerVar<FocusPower>(1m)
        ];

        /// <summary>
        /// 悬停卡牌时显示的能力提示框（与超能光束等原版卡一致）。
        /// </summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromPower<FocusPower>()
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            // 本回合失去集中：施加临时的反向集中，回合结束时自动返还。
            await PowerCmd.Apply<SequenceSearchFocusDownPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["FocusPower"].BaseValue,
                Owner.Creature,
                this);

            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        }

        protected override void OnUpgrade()
        {
            DynamicVars["FocusPower"].UpgradeValueBy(1m);
            DynamicVars.Cards.UpgradeValueBy(1m);
        }
    }
}
