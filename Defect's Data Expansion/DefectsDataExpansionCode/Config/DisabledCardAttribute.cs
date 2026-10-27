namespace DefectsDataExpansion.DefectsDataExpansionCode.Config
{
    /// <summary>
    /// 临时弃用一张卡：加在卡牌类上，这张牌就不会出现在任何卡池查询结果里
    /// （卡牌奖励、商店、事件、战斗内随机生成都走那条查询），所以游戏中拿不到。
    ///
    /// 卡牌类本身仍然正常注册，别的代码照样可以引用它（例如诅咒、事件指定给牌）。
    /// 想恢复：把这个特性删掉即可。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class DisabledCardAttribute : Attribute
    {
    }
}
