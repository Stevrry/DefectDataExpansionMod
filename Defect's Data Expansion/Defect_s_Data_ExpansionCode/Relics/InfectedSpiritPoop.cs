using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Config;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Relics
{
    /// <summary>
    /// 发酵的便便：每场战斗开始时，给予你1层虚弱。
    /// </summary>
    [RegisterRelic(typeof(EventRelicPool))]
    public sealed class InfectedSpiritPoop : ModRelicTemplate
    {
        /// <summary>
        /// 事件遗物：不进随机遗物池，也不区分角色。
        /// </summary>
        public override RelicRarity Rarity => RelicRarity.Event;

        /// <summary>模组内容关闭时，本遗物不应出现在任何地方。</summary>
        public override bool IsAllowed(IRunState runState) => ExpansionConfig.EnableExpansion;

        /// <summary>每场战斗开始时给的虚弱层数。</summary>
        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new PowerVar<WeakPower>(1m)
        ];

        public override RelicAssetProfile AssetProfile => new(
            // 小图标（原版85x85）
            IconPath: $"res://Defect_s_Data_Expansion/images/smallRelics/{GetType().Name}.png",
            // 轮廓图标（原版85x85）
            IconOutlinePath: $"res://Defect_s_Data_Expansion/images/outline/{GetType().Name}.png",
            // 大图标（原版256x256）
            BigIconPath: $"res://Defect_s_Data_Expansion/images/relics/{GetType().Name}.png");

        /// <summary>悬停时说明「虚弱」是什么。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromPower<WeakPower>()
        ];

        /// <summary>每场战斗开始时给自己上虚弱。</summary>
        public override async Task BeforeCombatStart()
        {
            Flash();

            await PowerCmd.Apply<WeakPower>(
                new ThrowingPlayerChoiceContext(),
                Owner.Creature,
                DynamicVars["WeakPower"].BaseValue,
                Owner.Creature,
                null);
        }
    }
}
