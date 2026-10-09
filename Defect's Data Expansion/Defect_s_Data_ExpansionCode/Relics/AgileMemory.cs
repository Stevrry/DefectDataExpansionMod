using System.Collections.Generic;
using System.Threading.Tasks;
using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Config;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Relics
{
    /// <summary>
    /// 敏捷的记忆：在下一场战斗开始时，获得 2 点敏捷。只生效一次，用掉之后遗物置灰。
    /// 图标暂时复用原版「结实绷带」
    /// </summary>
    [RegisterRelic(typeof(EventRelicPool))]
    public sealed class AgileMemory : ModRelicTemplate
    {
        private bool _dexterityInNextCombat;

        private bool _isSpent;

        /// <summary>
        /// 事件遗物
        /// </summary>
        public override RelicRarity Rarity => RelicRarity.Event;

        /// <summary>暂时使用原版「结实绷带」的图片。</summary>
        protected override string IconBaseName => "tough_bandages";

        /// <summary>模组内容关闭时，本遗物不应出现在任何地方。</summary>
        public override bool IsAllowed(IRunState runState) => ExpansionConfig.EnableExpansion;

        /// <summary>
        /// 兑现过一次之后，就只是块占地方的纪念品了。
        ///
        /// 引擎读这个属性会干两件事：把图标渲染成「已用过」（灰色，见 RelicModel 里的 _isUsed 着色器参数），
        /// 以及把悬停说明换成 gameplay_ui 里的 RELIC_USED_UP ——
        /// 「[red]这件遗物已经用完。[/red]\n{description}」，描述本身不用我们另外写。
        /// 同原版先古遗物「白银熔炉」（SilverCrucible）和「飞毛腿靴」（WingedBoots）。
        /// </summary>
        public override bool IsUsedUp => _isSpent;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new PowerVar<DexterityPower>(2m)
        ];

        /// <summary>悬停时说明它会给你叠什么。</summary>
        protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromPower<DexterityPower>()
        ];

        /// <summary>
        /// 拿到手就「上膛」，等下一场战斗开始时兑现，然后就不再触发。
        /// 同样的写法见原版遗物「古朴茶具」。
        /// </summary>
        [SavedProperty]
        public bool DexterityInNextCombat
        {
            get => _dexterityInNextCombat;
            set
            {
                AssertMutable();
                if (_dexterityInNextCombat == value)
                {
                    return;
                }

                _dexterityInNextCombat = value;
                // 上膛期间高亮，和「古朴茶具」一致；已经用掉的话保持灰色，别被这里改回去
                Status = value ? RelicStatus.Active : (_isSpent ? RelicStatus.Disabled : RelicStatus.Normal);
            }
        }

        /// <summary>
        /// 是否已经用掉。跟着存档走：拿到遗物之后、打响下一场之前是可以存档退出的，
        /// 「用没用过」这件事必须记得住。
        /// </summary>
        [SavedProperty]
        public bool IsSpent
        {
            get => _isSpent;
            set
            {
                AssertMutable();
                if (_isSpent == value)
                {
                    return;
                }

                _isSpent = value;
                CheckIfUsedUp();
            }
        }

        public override Task AfterObtained()
        {
            DexterityInNextCombat = true;
            return Task.CompletedTask;
        }

        public override async Task BeforeCombatStart()
        {
            if (!DexterityInNextCombat)
            {
                return;
            }

            // 先清标记再发放：万一中间出岔子，也不会在同一场战斗里反复触发
            DexterityInNextCombat = false;

            Flash();
            await PowerCmd.Apply<DexterityPower>(
                new ThrowingPlayerChoiceContext(),
                Owner.Creature,
                DynamicVars.Dexterity.BaseValue,
                Owner.Creature,
                null);

            // 发完了才算用掉，遗物就此置灰
            IsSpent = true;
        }

        /// <summary>
        /// 用完了就把遗物置灰。RelicStatus.Disabled 会让引擎把图标渲染成「已用过」的样子，
        /// 悬停说明也会自动带上「这件遗物已经用完。」。
        /// </summary>
        private void CheckIfUsedUp()
        {
            if (IsUsedUp)
            {
                Status = RelicStatus.Disabled;
            }
        }
    }
}
