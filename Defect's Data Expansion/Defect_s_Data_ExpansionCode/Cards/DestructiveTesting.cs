using System.Collections.Generic;
using System.Threading.Tasks;
using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Cards
{
    /// <summary>
    /// 破坏性试验：抽2(3)张牌，获得2层过热。
    /// </summary>
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class DestructiveTesting : ModCardTemplate
    {
        public DestructiveTesting() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new CardsVar(2),
            new PowerVar<Overheat>(2m)
        ];

        /// <summary>
        /// 卡图路径
        /// </summary>
        public override CardAssetProfile AssetProfile => new(
            PortraitPath: $"res://Defect_s_Data_Expansion/images/cards/{GetType().Name}.png"
        );

        /// <summary>
        /// 悬停卡牌时显示的能力提示框。
        /// </summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromPower<Overheat>()
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

            // 过热挂在自己身上，层数越高代价越重，但掉层时会返还能量
            await PowerCmd.Apply<Overheat>(
                choiceContext,
                Owner.Creature,
                DynamicVars["Overheat"].BaseValue,
                Owner.Creature,
                this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Cards.UpgradeValueBy(1m);
        }
    }
}
