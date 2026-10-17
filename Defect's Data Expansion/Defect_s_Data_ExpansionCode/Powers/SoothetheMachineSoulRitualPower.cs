using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Powers
{
    /// <summary>
    /// 「安抚机魂」的延迟效果：在你的下回合开始时获得仪式，随后自身移除。
    /// </summary>
    [RegisterPower]
    public sealed class SoothetheMachineSoulRitualPower : ModPowerTemplate
    {
        private const string LocPrefix = "DEFECT_S_DATA_EXPANSION_POWER_SOOTHETHE_MACHINE_SOUL_RITUAL_POWER";

        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        /// <summary>仪式本身会持续提供力量，一并显示。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromPower<RitualPower>()
        ];

        // 自定义图标路径。1:1即可。原版游戏大图256x256，小图64x64。
        //public override PowerAssetProfile AssetProfile => new(
        //    IconPath: "res://Test/images/powers/test_power.png",
        //    BigIconPath: "res://Test/images/powers/test_power.png"
        //);

        public override LocString Title => new("powers", LocPrefix + ".title");

        public override LocString Description => new("powers", LocPrefix + ".description");

        protected override string SmartDescriptionLocKey => LocPrefix + ".smartDescription";

        public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
        {
            if (player == Owner.Player && AmountOnTurnStart != 0)
            {
                Flash();
                await PowerCmd.Apply<RitualPower>(choiceContext, Owner, Amount, Owner, null);
                await PowerCmd.Remove(this);
            }
        }
    }
}
