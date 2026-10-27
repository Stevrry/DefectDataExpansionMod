using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace DefectsDataExpansion.DefectsDataExpansionCode.Powers
{
    /// <summary>
    /// 「升温」挂在自己身上的持续效果：本场战斗中，过热的层数不会低于 Amount 层。
    ///
    /// 真正的钳制在 <see cref="OverheatPower"/> 掉层时执行；这个能力只负责携带「地板」值。
    /// 层数（Amount）就是地板本身，连续打出两张会叠加成更高的地板。
    /// </summary>
    [RegisterPower]
    public sealed class WarmingPower : ModPowerTemplate
    {
        private const string LocPrefix = "DEFECTS_DATA_EXPANSION_POWER_WARMING_POWER";

        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public override LocString Title => new("powers", LocPrefix + ".title");

        public override LocString Description => new("powers", LocPrefix + ".description");

        protected override string SmartDescriptionLocKey => LocPrefix + ".smartDescription";

        /// <summary>描述里提到过热，把过热的说明一并显示。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromPower<OverheatPower>()
        ];
    }
}
