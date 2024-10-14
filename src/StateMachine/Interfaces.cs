namespace ExpressEngine.StateMachine.Interfaces
{
	public interface ICondition
	{
		bool Evaluate(); // This method will return true or false based on the condition.
	}

	public interface IConsequent
	{
		void Add(ICondition condition); // Links a condition with this consequent (state/action)
		void Execute(); // Executes the consequent when the condition is met
	}

	public interface IStateBuilderInitial<T>
	{
		IStateBuilderCondition<T> If(ICondition condition);
	}

	public interface IStateBuilderCondition<T>
	{
		IStateBuilderAction<T> Then(IConsequent consequent);
		IStateBuilderCondition<T> ElseIf(ICondition condition);
		IStateBuilderAction<T> Else(IConsequent consequent);
	}

	public interface IStateBuilderAction<T>
	{
		IStateBuilderAction<T> And(IConsequent consequent);
		void Evaluate();
	}
}
