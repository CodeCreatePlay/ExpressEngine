using System;
using System.Collections.Generic;


namespace ExpressEngine.StateMachine.TransitionBuilder
{
	using StateMachine.Interfaces;
	
	
    public class TransitionBuilder<T> : ITransitionBuilderInitial<T>, ITransitionBuilderCondition<T>, IAction<T>
    {
        private readonly List<ICondition> rules = new();
        private readonly Dictionary<ICondition, Func<T>> targetStates = new();
        private Func<T> _elseState;
        private BuilderState _state = BuilderState.None;

        public TransitionBuilder() { }

        public static ITransitionBuilderInitial<T> Begin()
        {
            return new TransitionBuilder<T>();
        }

        public ITransitionBuilderCondition<T> If(ICondition condition)
        {
            ValidateStateForIf();
            rules.Add(condition);
            _state = BuilderState.If;
            return this;
        }

        public ITransitionBuilderCondition<T> If(Func<bool> condition)
        {
            ValidateStateForIf();
            rules.Add(new PlaceholderState(condition));
            _state = BuilderState.If;
            return this;
        }

        public ITransitionBuilderCondition<T> Then(T targetState)
        {
            return Then(() => targetState);
        }

        public ITransitionBuilderCondition<T> Then(Func<T> nestedEvaluation)
        {
            ValidateStateForThen();
            var lastCondition = rules[^1];
            targetStates[lastCondition] = nestedEvaluation;
            _state = BuilderState.Then;
            return this;
        }

        public ITransitionBuilderCondition<T> ElseIf(ICondition condition)
        {
            ValidateStateForElseIf();
            rules.Add(condition);
            _state = BuilderState.ElseIf;
            return this;
        }

        public ITransitionBuilderCondition<T> ElseIf(Func<bool> condition)
        {
            ValidateStateForElseIf();
            rules.Add(new PlaceholderState(condition));
            _state = BuilderState.ElseIf;
            return this;
        }

        public IAction<T> Else(T targetState)
        {
            return Else(() => targetState);
        }

        public IAction<T> Else(Func<T> elseStateEvaluation)
        {
            ValidateStateForElse();
            _elseState = elseStateEvaluation;
            _state = BuilderState.Else;
            return this;
        }

		public T Evaluate()
		{
			foreach (var condition in rules)
			{
				if (condition.Evaluate() && targetStates.TryGetValue(condition, out var targetStateEvaluation))
				{
					return targetStateEvaluation();
				}
			}

			if (_elseState != null)
			{
				return _elseState();
			}

			throw new InvalidOperationException("No valid state transition found.");
		}

        private void TransitionTo(T targetState)
        {
            Console.WriteLine($"Transitioning to state: {targetState}");
        }

        private void ValidateStateForIf()
        {
            if (_state != BuilderState.None) throw new InvalidOperationException("If must be the first call.");
        }

        private void ValidateStateForThen()
        {
            if (_state != BuilderState.If && _state != BuilderState.ElseIf)
                throw new InvalidOperationException("Then must come after If or ElseIf.");
        }

        private void ValidateStateForElseIf()
        {
            if (_state != BuilderState.Then)
                throw new InvalidOperationException("ElseIf can only be called after Then.");
        }

        private void ValidateStateForElse()
        {
            if (_state != BuilderState.Then)
                throw new InvalidOperationException("Else must come after Then.");
        }

        private class PlaceholderState : ICondition
        {
            private readonly Func<bool> _callback;

            public PlaceholderState(Func<bool> callback) => _callback = callback;

            public bool Evaluate() => _callback();
        }
		
		// Enum to track the internal state of the builder
		public enum BuilderState
		{
			None,
			If,
			Then,
			ElseIf,
			Else,
		}
    }
}
