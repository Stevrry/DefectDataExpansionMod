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
using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Config; // 内部配置

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Cards
{
    /// <summary>
    /// 炽热外壳：失去所有的过热，每层过热获得 2(3) 点荆棘。
    /// </summary>
    [DisabledCard] // 暂时弃用
    [RegisterCard(typeof(DefectCardPool))]
    public sealed class BlazingShell : ModCardTemplate
    {
        /// <summary>荆棘的变量名。</summary>
        private const string ThornsVar = "ThornsPower";

        public BlazingShell() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            // 这是「每层过热」给的点数，实际给的荆棘要再乘上当时的层数
            new PowerVar<ThornsPower>(2m)
        ];

        /// <summary>
        /// 卡图路径
        /// </summary>
        //public override CardAssetProfile AssetProfile => new(
        //    PortraitPath: $"res://Defect_s_Data_Expansion/images/cards/{GetType().Name}.png"
        //);

        /// <summary>悬停时说明它会烧掉什么、换来什么。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromPower<OverheatPower>(),
            HoverTipFactory.FromPower<ThornsPower>()
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            // 能力牌用的是 PowerUp 动画，不是技能/攻击的 Cast
            await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);

            // 没有过热就什么都不发生（层数也要先看，别拿 0 去乘）
            if (Owner.Creature.GetPower<OverheatPower>() is not { } overheat || overheat.Amount <= 0)
            {
                return;
            }

            // 先记下要烧掉几层，再烧。
            int stacks = overheat.Amount;

            // 必须走 ModifyAmount，不能用 PowerCmd.Remove：
            // Remove 直接删能力、不触发层数变化钩子，过热那笔「掉层返还能量」就拿不到了。
            await PowerCmd.ModifyAmount(choiceContext, overheat, -stacks, Owner.Creature, this);

            await PowerCmd.Apply<ThornsPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars[ThornsVar].BaseValue * stacks,
                Owner.Creature,
                this);
        }

        /// <summary>升级只加「每层过热」给的点数 2 → 3。</summary>
        protected override void OnUpgrade()
        {
            DynamicVars[ThornsVar].UpgradeValueBy(1m);
        }
    }
}
