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
    /// 散热蒸汽：失去1层过热，获得10（14）点格挡。
    /// </summary>
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class CoolingSteam : ModCardTemplate
    {
        public CoolingSteam() : base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
        {
        }

        /// <summary>这张牌会给自己加格挡。</summary>
        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new BlockVar(10m, ValueProp.Move),
            new PowerVar<OverheatPower>(1m)
        ];

        /// <summary>
        /// 卡图路径
        /// </summary>
        //public override CardAssetProfile AssetProfile => new(
        //    PortraitPath: $"res://Defect_s_Data_Expansion/images/cards/{GetType().Name}.png"
        //);

        /// <summary>悬停时把两个效果各自的说明都给出来。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromPower<OverheatPower>(),
            HoverTipFactory.Static(StaticHoverTip.Block),
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

            // 失去过热。必须走 ModifyAmount 而不是 PowerCmd.Remove：
            // 掉层要触发钩子，过热那笔「掉层返还能量」才发得出来。
            // 所以身上有热时这张牌实际只花 1 费。没有热就跳过这一步，格挡照给。
            if (Owner.Creature.GetPower<OverheatPower>() is { Amount: > 0 } overheat)
            {
                await PowerCmd.ModifyAmount(
                    choiceContext,
                    overheat,
                    -DynamicVars["OverheatPower"].BaseValue,
                    Owner.Creature,
                    this);
            }

            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        }

        /// <summary>升级只加格挡 10 → 14。</summary>
        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(4m);
        }
    }
}
