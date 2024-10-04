namespace ExpressEnginex.Fuzzy.Interfaces
{
	using StateMachine;
	
    public interface IRuleApplier
    {		
        // IRuleApplier And(IConsequent consequent);
        IRuleApplier Else(IConsequent consequent);
        // IRuleBuilder ElseIf(ICondition fuzzyCondition);
    }
}
