using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Powers
{
    /// <summary>
    /// 「序列检索」造成的本回合失去集中效果。
    /// InvertInternalPowerAmount = true：对外显示为正的层数，内部实际扣减集中，回合结束时返还。
    /// 文案见 localization/zhs/powers.json 中 SEQUENCE_SEARCH_FOCUS_DOWN_POWER 的两条键。
    /// </summary>
    public sealed class SequenceSearchFocusDownPower : CustomTemporaryPowerModelWrapper<Cards.SequenceSearch, FocusPower>
    {
        private const string LocPrefix = "DEFECT_S_DATA_EXPANSION-SEQUENCE_SEARCH_FOCUS_DOWN_POWER";

        protected override bool InvertInternalPowerAmount => true;

        /// <summary>能力图标悬停时显示的静态描述。</summary>
        public override LocString Description => new("powers", LocPrefix + ".description");

        /// <summary>带数值的能力描述（工具提示用，支持 {Amount} 等变量）。</summary>
        protected override string SmartDescriptionLocKey => LocPrefix + ".smartDescription";
    }
}
