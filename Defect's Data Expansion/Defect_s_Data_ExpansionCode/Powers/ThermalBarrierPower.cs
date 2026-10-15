using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
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
    /// 「热量屏障」挂在自己身上的持续效果：你的回合开始时，每有 1 层过热就获得 X 点格挡。
    /// 层数（Amount）就是「每层过热给几点格挡」，实际给的量 = 层数 × 当前过热层数。
    /// </summary>
    [RegisterPower]
    public sealed class ThermalBarrierPower : ModPowerTemplate
    {
        private const string LocPrefix = "DEFECT_S_DATA_EXPANSION_POWER_THERMAL_BARRIER_POWER";

        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public override LocString Title => new("powers", LocPrefix + ".title");

        public override LocString Description => new("powers", LocPrefix + ".description");

        protected override string SmartDescriptionLocKey => LocPrefix + ".smartDescription";

        /// <summary>描述里带格挡，把格挡的说明也一并显示（原版给格挡的能力都这么做）。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.Static(StaticHoverTip.Block)
        ];

        public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
        {
            if (player != Owner.Player)
            {
                return;
            }

            int heat = Owner.GetPowerAmount<OverheatPower>();
            if (heat <= 0)
            {
                return;
            }

            Flash();

            // ValueProp.Move 和卡牌上 BlockVar 声明的那个一致，所以敏捷这类加成照样管用
            await CreatureCmd.GainBlock(Owner, heat * Amount, ValueProp.Move, null);
        }
    }
}
