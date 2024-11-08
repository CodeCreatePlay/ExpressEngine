using System;
using System.Collections.Generic;

namespace ExpressEngine.RuleBuilder.Interfaces
{
	/*
	Classes for book keeping consequents and their corresponding antecedents, represented as 'Rules'.
	*/
	
    public abstract class ICondition
    {
        public abstract bool Evaluate();
    }

    public abstract class IAction<T>
    {
        public abstract T Fire();
    }

    public abstract class RuleBase<T>
    {
        public abstract bool Evaluate();
        public abstract T Fire();
    }

    public class Rule<T> : RuleBase<T>
    {
        private ICondition condition;
        private IAction<T> action;

        public Rule(ICondition condition)
        {
            this.condition = condition ?? throw new ArgumentNullException(nameof(condition));
            this.action = null;
        }

        public void AddAction(IAction<T> action)
        {
            this.action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public override bool Evaluate()
        {
            if (condition == null)
                throw new InvalidOperationException("Condition is not set for this rule.");
            return condition.Evaluate();
        }

        public override T Fire()
        {
            if (action == null)
                throw new InvalidOperationException("Action is not set for this rule.");
            return action.Fire();
        }
    }

    public class SwitchRule<T> : RuleBase<T>
    {
        private readonly Func<object> keySelector; // Function to select the switch key
        private readonly Dictionary<object, IAction<T>> cases = new(); // Cases with their actions
        private IAction<T> defaultAction; // Optional default case action

        public SwitchRule(Func<object> keySelector)
        {
            this.keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
        }

        public void AddCase(object caseKey, IAction<T> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            cases[caseKey] = action;
        }

        public void AddCase(object caseKey, Func<T> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            cases[caseKey] = new ActionAntecedent(action);
        }

        public void SetDefault(IAction<T> defaultAction)
        {
            this.defaultAction = defaultAction;
        }

        public override bool Evaluate()
        {
            var key = keySelector();
            return key != null && (cases.ContainsKey(key) || defaultAction != null);
        }

        public override T Fire()
        {
            var key = keySelector();
            if (key != null && cases.TryGetValue(key, out var action))
            {
                return action.Fire();
            }
            else if (defaultAction != null)
            {
                return defaultAction.Fire();
            }

            throw new InvalidOperationException("No matching case found and no default case set.");
        }

        private class ActionAntecedent : IAction<T>
        {
            private readonly Func<T> action;
            public ActionAntecedent(Func<T> action) => this.action = action;
            public override T Fire() => action();
        }
    }
	
	/*
	Base classes to provide a mechanism for creating transition chains using the fluent builder pattern.
	*/
	
	public abstract class RuleBuilder<T>
	{
		protected List<RuleBase<T>> chain = new();
		
		public abstract T Evaluate();
	}

	public abstract class SwitchBuilder<T> : RuleBuilder<T>
	{
		public abstract SwitchCaseBuilder<T> Case(object caseKey, Func<T> action);
		public abstract SwitchDefaultBuilder<T> Default(Func<T> action);
	}

	public abstract class SwitchCaseBuilder<T> : RuleBuilder<T>
	{
		public abstract SwitchBuilder<T> EndSwitch();
	}

	public abstract class SwitchDefaultBuilder<T> : RuleBuilder<T>
	{
		public abstract SwitchBuilder<T> EndSwitch();
	}

	public abstract class IfBuilder<T> : RuleBuilder<T>
	{
		public abstract ThenBuilder<T> Then(Func<T> action);
		public abstract ElseIfBuilder<T> ElseIf(Func<bool> condition);
		public abstract ElseBuilder<T> Else(Func<T> action);
	}

	public abstract class ElseIfBuilder<T> : RuleBuilder<T>
	{
		public abstract ThenBuilder<T> Then(Func<T> action);
		public abstract ElseBuilder<T> Else(Func<T> action);
	}

	public abstract class ElseBuilder<T> : RuleBuilder<T>
	{
		public abstract RuleBuilder<T> EndElse();
	}

	public abstract class ThenBuilder<T> : RuleBuilder<T> { }
}
