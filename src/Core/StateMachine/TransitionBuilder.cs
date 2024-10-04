using System;
using System.Collections.Generic;


namespace ExpressEnginex.StateMachine
{
	using ExpressEnginex.StateMachine.Interfaces;
	
	
	public class StateTransitionBuilder<T>
	{
		private readonly StateMachine<T> _stateMachine;
		private readonly List<(ICondition, IConsequent)> _rules = new List<(ICondition, IConsequent)>();
		private IConsequent _elseConsequent;

		private BuilderState _state = BuilderState.None; // Track the current state of the builder

		public StateTransitionBuilder(StateMachine<T> stateMachine)
		{
			_stateMachine = stateMachine;
		}

		public static StateTransitionBuilder<T> Begin(StateMachine<T> stateMachine)
		{
			return new StateTransitionBuilder<T>(stateMachine);
		}

		// Ensures 'If' is always first
		public StateTransitionBuilder<T> If(ICondition condition)
		{
			if (_state != BuilderState.None) throw new InvalidOperationException("If must be the first call.");
			_rules.Add((condition, null));
			_state = BuilderState.If;
			return this;
		}

		// Enforces 'Then' must follow 'If' or 'ElseIf'
		public StateTransitionBuilder<T> Then(IConsequent consequent)
		{
			if (_state != BuilderState.If && _state != BuilderState.ElseIf) 
				throw new InvalidOperationException("Then must come after If or ElseIf.");
			
			_rules[_rules.Count - 1] = (_rules[_rules.Count - 1].Item1, consequent);
			_state = BuilderState.Then;
			return this;
		}

		// 'ElseIf' can only come after a valid 'Then'
		public StateTransitionBuilder<T> ElseIf(ICondition condition)
		{
			if (_state != BuilderState.Then) 
				throw new InvalidOperationException("ElseIf can only be called after Then.");
			
			_rules.Add((condition, null));
			_state = BuilderState.ElseIf;
			return this;
		}

		// 'Else' must come after 'Then' and be the last statement
		public StateTransitionBuilder<T> Else(IConsequent consequent)
		{
			if (_state != BuilderState.Then) 
				throw new InvalidOperationException("Else must come after Then.");
			
			_elseConsequent = consequent;
			_state = BuilderState.Else;
			return this;
		}

		// Evaluate conditions and execute logic
		public void Evaluate()
		{
			if (_state != BuilderState.Then && _state != BuilderState.Else)
				throw new InvalidOperationException("Incomplete rule chain. Make sure to use Then before evaluation.");

			foreach (var (condition, consequent) in _rules)
			{
				if (condition.Evaluate())
				{
					consequent?.Execute();
					return;
				}
			}

			// Execute the Else block if no conditions matched
			_elseConsequent?.Execute();
		}
	}

	// State tracking to enforce correct logical flow
	public enum BuilderState
	{
		None,      // Initial state, can only call 'If'
		If,        // After 'If', must call 'Then'
		Then,      // After 'Then', can call 'ElseIf' or 'Else'
		ElseIf,    // After 'ElseIf', must call 'Then'
		Else,      // After 'Else', no more calls are allowed
	}
}

/*
// A sample condition to check if AI is out of combat
public class IsNotInCombatCondition : ICondition
{
    private AI_Character _character;
    public IsNotInCombatCondition(AI_Character character)
    {
        _character = character;
    }

    public bool Evaluate()
    {
        return !_character.IsInCombat;
    }
}

// A sample condition to check if AI has enough health
public class HealthAboveCondition : ICondition
{
    private AI_Character _character;
    private int _threshold;

    public HealthAboveCondition(AI_Character character, int threshold)
    {
        _character = character;
        _threshold = threshold;
    }

    public bool Evaluate()
    {
        return _character.Health > _threshold;
    }
}

// A consequent that transitions to patrol state
public class TransitionToPatrolConsequent : IConsequent
{
    private StateMachine<AI_Character> _stateMachine;
    private int _patrolState;

    public TransitionToPatrolConsequent(StateMachine<AI_Character> stateMachine, int patrolState)
    {
        _stateMachine = stateMachine;
        _patrolState = patrolState;
    }

    public void Add(ICondition condition) 
	{ 
		// Optionally link condition if needed
	}

    public void Execute()
    {
        _stateMachine.SwitchState(_patrolState);
    }
}
*/
