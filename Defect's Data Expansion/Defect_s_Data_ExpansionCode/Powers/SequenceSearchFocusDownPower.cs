using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Combat.Powers;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Powers
{
    /// <summary>
    /// 「序列检索」造成的本回合失去集中效果。
    ///
    /// IsPositive => false：对外按正数显示层数，内部实际给 <see cref="FocusPower"/> 施加负数，
    /// </summary>
    public sealed class SequenceSearchFocusDownPower : ModTemporaryAppliedPowerTemplate<SequenceSearch, FocusPower>
    {
        private const string LocPrefix = "DEFECT_S_DATA_EXPANSION_POWER_SEQUENCE_SEARCH_FOCUS_DOWN_POWER";

        protected override bool IsPositive => false;

        /// <summary>能力图标悬停时显示的静态描述。</summary>
        public override LocString Description => new("powers", LocPrefix + ".description");

        /// <summary>带数值的能力描述（工具提示用，支持 {Amount} 等变量）。</summary>
        protected override string SmartDescriptionLocKey => LocPrefix + ".smartDescription";
    }
}
