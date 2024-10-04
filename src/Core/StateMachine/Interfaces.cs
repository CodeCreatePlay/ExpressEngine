using System;


namespace ExpressEnginex.StateMachine.Interfaces
{
	public interface ICondition
	{
		bool Evaluate(); // This method will return true or false based on the condition.
	}
}

namespace ExpressEnginex.StateMachine.Interfaces
{
	public interface IConsequent
	{
		void Add(ICondition condition); // Links a condition with this consequent (state/action)
		void Execute(); // Executes the consequent when the condition is met
	}
}
