using System;
using System.Collections.Generic;

namespace ExpressEngine.StateMachine
{
    using ExpressEngine.StateMachine.Interfaces;

	public class StateTransitionBuilder<T> : IStateBuilderInitial<T>, IStateBuilderCondition<T>, IStateBuilderAction<T>
	{
		private readonly StateMachine<T> _stateMachine;
		private readonly List<(ICondition, List<IConsequent>)> _rules = new();
		private readonly List<IConsequent> _elseConsequents = new();

		// Lists for method-based conditions and consequents
		private readonly List<(Func<bool>, List<Action>)> _methodRules = new();
		private readonly List<Action> _elseMethodConsequents = new();

		private BuilderState _state = BuilderState.None;

		public StateTransitionBuilder(StateMachine<T> stateMachine)
		{
			_stateMachine = stateMachine;
		}

		public static IStateBuilderInitial<T> Begin(StateMachine<T> stateMachine)
		{
			return new StateTransitionBuilder<T>(stateMachine);
		}

		// Ensures 'If' is always first for classes
		public IStateBuilderCondition<T> If(ICondition condition)
		{
			if (_state != BuilderState.None) throw new InvalidOperationException("If must be the first call.");
			_rules.Add((condition, new List<IConsequent>()));
			_state = BuilderState.If;
			return this;
		}

		// If for method-based conditions
		public IStateBuilderCondition<T> If(Func<bool> condition)
		{
			if (_state != BuilderState.None) throw new InvalidOperationException("If must be the first call.");
			_methodRules.Add((condition, new List<Action>()));
			_state = BuilderState.If;
			return this;
		}

		// Then for class-based consequents
		public IStateBuilderAction<T> Then(IConsequent consequent)
		{
			if (_state != BuilderState.If && _state != BuilderState.ElseIf)
				throw new InvalidOperationException("Then must come after If or ElseIf.");
			
			var conditionConsequents = _rules[^1].Item2;
			conditionConsequents.Add(consequent);
			consequent.Add(_rules[^1].Item1);

			_state = BuilderState.Then;
			return this;
		}

		// Then for method-based consequents
		public IStateBuilderAction<T> Then(Action action)
		{
			if (_state != BuilderState.If && _state != BuilderState.ElseIf)
				throw new InvalidOperationException("Then must come after If or ElseIf.");

			var conditionConsequents = _methodRules[^1].Item2;
			conditionConsequents.Add(action);

			_state = BuilderState.Then;
			return this;
		}

		// Handle And and Else blocks similarly for both methods and classes
		public IStateBuilderAction<T> And(IConsequent consequent)
		{
			if (_state != BuilderState.Then && _state != BuilderState.Else)
				throw new InvalidOperationException("And can only be called after Then or Else.");

			if (_state == BuilderState.Then)
			{
				var conditionConsequents = _rules[^1].Item2;
				conditionConsequents.Add(consequent);
				consequent.Add(_rules[^1].Item1);
			}
			else if (_state == BuilderState.Else)
			{
				_elseConsequents.Add(consequent);
			}

			return this;
		}

		public IStateBuilderAction<T> And(Action action)
		{
			if (_state != BuilderState.Then && _state != BuilderState.Else)
				throw new InvalidOperationException("And can only be called after Then or Else.");

			if (_state == BuilderState.Then)
			{
				var conditionConsequents = _methodRules[^1].Item2;
				conditionConsequents.Add(action);
			}
			else if (_state == BuilderState.Else)
			{
				_elseMethodConsequents.Add(action);
			}

			return this;
		}
		
		public IStateBuilderCondition<T> ElseIf(ICondition condition)
		{
			if (_state != BuilderState.Then)
				throw new InvalidOperationException("ElseIf can only be called after Then.");

			_rules.Add((condition, new List<IConsequent>()));
			_state = BuilderState.ElseIf;
			return this;
		}

		public IStateBuilderAction<T> Else(IConsequent consequent)
		{
			if (_state != BuilderState.Then)
				throw new InvalidOperationException("Else must come after Then.");

			_elseConsequents.Add(consequent);
			_state = BuilderState.Else;
			return this;
		}

		// Update Evaluate to handle both class and method-based conditions and actions
		public void Evaluate()
		{
			// First, evaluate class-based rules
			foreach (var (condition, consequents) in _rules)
			{
				if (condition.Evaluate())
				{
					foreach (var consequent in consequents)
					{
						consequent?.Execute();
					}
					return;
				}
			}

			// Then, evaluate method-based rules
			foreach (var (condition, consequents) in _methodRules)
			{
				if (condition())
				{
					foreach (var action in consequents)
					{
						action?.Invoke();
					}
					return;
				}
			}

			// Execute the class-based Else block if no conditions matched
			foreach (var consequent in _elseConsequents)
			{
				consequent?.Execute();
			}

			// Execute the method-based Else block if no conditions matched
			foreach (var action in _elseMethodConsequents)
			{
				action?.Invoke();
			}
		}
	}

    // Enum for internal state tracking
    public enum BuilderState
    {
        None,
        If,
        Then,
        ElseIf,
        Else,
    }
}

/*
usage example.
StateTransitionBuilder<MyState>.Begin(stateMachine)
    .If(new SomeClassCondition())       // Class-based condition
    .Then(new SomeClassConsequent())    // Class-based consequent
    .And(() => Console.WriteLine("Action from method"))  // Method-based consequent
    .ElseIf(() => health < 50)          // Method-based condition
    .Then(new HealCharacter())          // Class-based consequent
    .Else(() => Console.WriteLine("Else block"))         // Method-based Else consequent
    .Evaluate();
*/
