using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Powers
{
    /// <summary>
    /// 「热量屏障」挂在自己身上的持续效果：过热层数每变化一次，就获得 X 点格挡。
    /// 层数（Amount）就是「每次变化给几点格挡」。
    ///
    /// 触发源是 <see cref="IHeatChangeObserver"/> —— 和「热量收集」一样，
    /// 由 <see cref="OverheatPower"/> 在算出真实变化量之后回调，所以满层被削顶（其实没变）不会触发。
    /// </summary>
    [RegisterPower]
    public sealed class ThermalBarrierPower : ModPowerTemplate, IHeatChangeObserver
    {
        private const string LocPrefix = "DEFECT_S_DATA_EXPANSION_POWER_THERMAL_BARRIER_POWER";

        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public override LocString Title => new("powers", LocPrefix + ".title");

        public override LocString Description => new("powers", LocPrefix + ".description");

        protected override string SmartDescriptionLocKey => LocPrefix + ".smartDescription";

        /// <summary>描述里带过热和格挡，把两者的说明也一并显示。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromPower<OverheatPower>(),
            HoverTipFactory.Static(StaticHoverTip.Block)
        ];

        // 自定义图标路径。1:1即可。原版游戏大图256x256，小图64x64。
        public override PowerAssetProfile AssetProfile => new(
            IconPath: "res://Defect_s_Data_Expansion/images/powers/ThermalBarrier_s.png",
            BigIconPath: "res://Defect_s_Data_Expansion/images/powers/ThermalBarrier.png"
        );

        public async Task OnHeatChanged(PlayerChoiceContext choiceContext, Creature owner, decimal actualDelta)
        {
            // 过热只会通知「自己身上」的观察者，这层判断只是保险
            if (owner != Owner)
            {
                return;
            }

            Flash();

            // ValueProp.Move 和卡牌上 BlockVar 声明的那个一致，所以敏捷这类加成照样管用
            await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, null);
        }
    }
}
