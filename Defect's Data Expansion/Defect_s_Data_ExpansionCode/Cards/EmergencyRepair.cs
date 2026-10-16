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
    /// 抢修：失去1层过热。消耗。升级后获得保留。
    /// </summary>
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class EmergencyRepair : ModCardTemplate
    {
        public EmergencyRepair() : base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new PowerVar<OverheatPower>(1m)
        ];

        /// <summary>消耗。</summary>
        public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [
            CardKeyword.Exhaust
        ];

        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromPower<OverheatPower>()
        ];

        /// <summary>
        /// 卡图路径
        /// </summary>
        public override CardAssetProfile AssetProfile => new(
            PortraitPath: $"res://Defect_s_Data_Expansion/images/cards/{GetType().Name}.png"
        );

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

            // 必须判「身上有没有过热」：PowerCmd.Apply 在目标没有该能力时会新建一个实例，
            // 而新建那条路径不检查 ShouldRemoveDueToAmount，负数层数的过热会一直挂在身上。
            // 已经存在时会走 ModifyAmount，那边会自动把降到 0 以下的能力移除。
            if (!Owner.Creature.Powers.Any(power => power is OverheatPower))
            {
                return;
            }

            await PowerCmd.Apply<OverheatPower>(
                choiceContext,
                Owner.Creature,
                -DynamicVars["OverheatPower"].BaseValue,
                Owner.Creature,
                this);
        }

        /// <summary>
        /// 升级只加「保留」，层数固定 1 层不动。
        /// </summary>
        protected override void OnUpgrade()
        {
            AddKeyword(CardKeyword.Retain);
        }
    }
}
