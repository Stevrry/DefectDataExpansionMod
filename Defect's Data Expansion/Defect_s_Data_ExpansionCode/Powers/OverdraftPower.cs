using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Powers
{
    /// <summary>
    /// 「透支」挂在自己身上的持续效果：每回合额外获得 1 点能量、额外多抽 1 张牌。
    /// </summary>
    [RegisterPower]
    public sealed class OverdraftPower : ModPowerTemplate
    {
        private const string LocPrefix = "DEFECT_S_DATA_EXPANSION_POWER_OVERDRAFT_POWER";

        public override PowerType Type => PowerType.Buff;

        public override PowerStackType StackType => PowerStackType.Counter;

        public override LocString Title => new("powers", LocPrefix + ".title");

        public override LocString Description => new("powers", LocPrefix + ".description");

        protected override string SmartDescriptionLocKey => LocPrefix + ".smartDescription";

        /// <summary>描述里带能量图标，把能量悬停也一并显示（原版领地同款）。</summary>
        protected override bool IncludeEnergyHoverTip => true;

        /// <summary>每回合额外多抽 X 张牌。</summary>
        public override decimal ModifyHandDraw(Player player, decimal count)
        {
            return player == Owner.Player ? count + Amount : count;
        }

        /// <summary>每回合额外获得 X 点能量。</summary>
        public override decimal ModifyMaxEnergy(Player player, decimal amount)
        {
            return player == Owner.Player ? amount + Amount : amount;
        }
    }
}
