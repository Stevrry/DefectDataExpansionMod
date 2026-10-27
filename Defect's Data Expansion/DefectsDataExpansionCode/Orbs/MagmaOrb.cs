using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace DefectsDataExpansion.DefectsDataExpansionCode.Orbs;

/// <summary>
/// 熔离：回合结束时，对自己造成 1 点伤害，触发你所有充能球的被动 1 次。激发时，触发你所有充能球的被动 2 次。
/// 被动与激发的扩散对象都**不包括任何熔离充能球**（包括它自己），避免熔离球之间互相触发。
/// 熔离球不受集中影响，所以两个数值都直接写死。
/// </summary>
[RegisterOrb]
public class MagmaOrb : ModOrbTemplate
{
    /// <summary>被动：每回合结束时对自己造成的伤害和充能球被动触发次数。</summary>
    private const decimal PassiveDamage = 1m;
    private const decimal PassiveTimes = 1m;

    /// <summary>激发：把身上所有充能球的被动各触发几次。</summary>
    private const decimal EvokeTimes = 2m;

    /// <summary>不受集中影响。</summary>
    public override decimal PassiveVal => PassiveDamage;

    public override decimal EvokeVal => EvokeTimes;

    // 战斗中的数值显示样式：和普通原版充能球一致
    // 平时显示被动值，被预览为激发时显示激发值。
    public override ModOrbValueDisplayMode ValueDisplayMode => ModOrbValueDisplayMode.Contextual;

    /// <summary>暗色版主体色，熔离用暗红。</summary>
    public override Color DarkenedColor => new("6b1f0a");

    // 占位素材：真正的熔离球美术还没做，先借用原版等离子球的图标与场景
    public override OrbAssetProfile AssetProfile => new(
        IconPath: "res://images/orbs/plasma_orb.png",
        VisualsScenePath: "res://scenes/orbs/orb_visuals/plasma_orb.tscn"
    );

    // 让你不需要手动挂脚本。复制即可。
    protected override Node2D? TryCreateOrbSprite() => RitsuGodotNodeFactories.CreateFromScenePath<Node2D>(AssetProfile.VisualsScenePath!);

    /// <summary>回合结束时触发被动（和原版闪电球同一个钩子）。</summary>
    public override async Task BeforeTurnEndOrbTrigger(PlayerChoiceContext choiceContext)
    {
        await TriggerPassive(choiceContext, null);
    }

    /// <summary>被动：对自己造成伤害，然后触发除熔离外其它充能球的被动 1 次。</summary>
    public override async Task Passive(PlayerChoiceContext choiceContext, Creature? target)
    {
        ActivatePassive();

        await CreatureCmd.Damage(choiceContext, Owner.Creature, PassiveVal, ValueProp.Unpowered, Owner.Creature);

        await TriggerOtherOrbPassives(choiceContext);
    }

    /// <summary>
    /// 触发其它充能球的被动。所有熔离球（包括自己）都不在触发对象里，所以不会互相递归。
    /// </summary>
    private async Task TriggerOtherOrbPassives(PlayerChoiceContext choiceContext)
    {
        // PlayerCombatState 声明成可空（只有战斗中才非空），但充能球本来就只在战斗里存在
        if (Owner.PlayerCombatState is not { } combatState)
        {
            return;
        }

        // 先快照：某个球的被动有可能改动队列，边枚举边改会抛异常
        List<OrbModel> orbs = combatState.OrbQueue.Orbs
            .Where(orb => orb is not MagmaOrb)
            .ToList();

        for (int i = 0; i < PassiveTimes; i++)
        {
            foreach (OrbModel orb in orbs)
            {
                await OrbCmd.Passive(choiceContext, orb, null);
                await Cmd.Wait(0.25f);
            }
        }
    }

    /// <summary>
    /// 激发：把身上除熔离外的充能球被动各触发2次。
    /// </summary>
    public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext playerChoiceContext)
    {
        PlayEvokeSfx();

        // PlayerCombatState 声明成可空（只有战斗中才非空），但充能球本来就只在战斗里存在
        if (Owner.PlayerCombatState is not { } combatState)
        {
            return [];
        }

        // 先快照：某个球的被动有可能改动队列，边枚举边改会抛异常。
        // 所有熔离球（包括自己）都不在触发对象里。
        List<OrbModel> orbs = combatState.OrbQueue.Orbs
            .Where(orb => orb is not MagmaOrb)
            .ToList();

        for (int i = 0; i < EvokeTimes; i++)
        {
            foreach (OrbModel orb in orbs)
            {
                await OrbCmd.Passive(playerChoiceContext, orb, null);
                await Cmd.Wait(0.25f);
            }
        }

        return [Owner.Creature];
    }
}
