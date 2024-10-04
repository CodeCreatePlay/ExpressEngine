using ExpressEnginex.Fuzzy.Interfaces;


namespace ExpressEnginex.Fuzzy
{
    public static class FuzzyRule
    {
        public static IRuleBuilder If(ICondition condition)
        {
            return new FuzzyRuleBuilder(condition);
        }
    }
}
