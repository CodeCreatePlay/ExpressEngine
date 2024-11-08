using System;
using System.Collections.Generic;
using System.Linq;

namespace ExpressEngine.RuleBuilder
{
    using RuleBuilder.Interfaces;

    public class RuleBuilder<T>
    {
        private readonly List<RuleBase<T>> chain = new();

        public RuleBuilder() { }

        public static RuleBuilder<T> Begin()
        {
            return new RuleBuilder<T>();
        }

        // Switch-Case Block Methods
        public SwitchBuilder<T> Switch(Func<object> keySelector)
        {
            var switchRule = new SwitchRule<T>(keySelector);
            chain.Add(switchRule);
            return new SwitchBuilder<T>(this, switchRule);
        }

        // Conditional Block Methods
        public ConditionalBuilder<T> If(ICondition condition)
        {
            var rule = new Rule<T>(condition);
            chain.Add(rule);
            return new ConditionalBuilder<T>(this, rule);
        }

        public ConditionalBuilder<T> If(Func<bool> condition)
        {
            var rule = new Rule<T>(new ConditionPlaceholder(condition));
            chain.Add(rule);
            return new ConditionalBuilder<T>(this, rule);
        }

        // Evaluate method to execute the first matching rule's action
        public T Evaluate()
        {
            var matchingRule = chain.FirstOrDefault(rule => rule.Evaluate())
                ?? throw new InvalidOperationException("No valid state transition found.");

            return matchingRule.Fire();
        }
		
		// ------------------------------------------------------------------------------------------ //
		// The switch-builder and condition-builder classes provide mechanism for creating transition 
		// chains using fluent builder pattern.
		
        // Builder for Switch statements
        public class SwitchBuilder<T>
        {
            private readonly RuleBuilder<T> parentBuilder;
            private readonly SwitchRule<T> switchRule;

            public SwitchBuilder(RuleBuilder<T> parentBuilder, SwitchRule<T> switchRule)
            {
                this.parentBuilder = parentBuilder;
                this.switchRule = switchRule;
            }

            public SwitchBuilder<T> Case(object caseKey, T targetState)
            {
                return Case(caseKey, () => targetState);
            }

            public SwitchBuilder<T> Case(object caseKey, Func<T> action)
            {
                switchRule.AddCase(caseKey, new ActionPlaceholder<T>(action));
                return this; // Returning self for method chaining
            }

            public SwitchBuilder<T> Default(T targetState)
            {
                return Default(() => targetState);
            }

            public SwitchBuilder<T> Default(Func<T> action)
            {
                switchRule.SetDefault(new ActionPlaceholder<T>(action));
                return this; // Returning self for method chaining
            }

            public RuleBuilder<T> EndSwitch()
            {
                return parentBuilder; // Return to the parent builder
            }
        }

        // Builder for Conditional statements
        public class ConditionalBuilder<T>
        {
            private readonly RuleBuilder<T> parentBuilder;
            private readonly Rule<T> conditionRule;

            public ConditionalBuilder(RuleBuilder<T> parentBuilder, Rule<T> conditionRule)
            {
                this.parentBuilder = parentBuilder;
                this.conditionRule = conditionRule;
            }

            public ConditionalBuilder<T> Then(T targetState)
            {
                return Then(() => targetState);
            }

            public ConditionalBuilder<T> Then(Func<T> action)
            {
                conditionRule.AddAction(new ActionPlaceholder<T>(action));
                return this; // Returning self for method chaining
            }

            public ConditionalBuilder<T> ElseIf(ICondition condition)
            {
                var rule = new Rule<T>(condition);
                parentBuilder.chain.Add(rule);
                return new ConditionalBuilder<T>(parentBuilder, rule);
            }

            public ConditionalBuilder<T> ElseIf(Func<bool> condition)
            {
                var rule = new Rule<T>(new ConditionPlaceholder(condition));
                parentBuilder.chain.Add(rule);
                return new ConditionalBuilder<T>(parentBuilder, rule);
            }

            public RuleBuilder<T> Else(T targetState)
            {
                return Else(() => targetState);
            }

            public RuleBuilder<T> Else(Func<T> action)
            {
                var rule = new Rule<T>(new ConditionPlaceholder(() => true));
                rule.AddAction(new ActionPlaceholder<T>(action));
                parentBuilder.chain.Add(rule);
                return parentBuilder; // Return to the parent builder
            }
        }

		// ------------------------------------------------------------------------------------------ //
		// Placeholder classes for encapsulating user-defined conditions and action callbacks as
		// ICondition and IAction objects.

        // Placeholder Classes
        private class ConditionPlaceholder : ICondition
        {
            private readonly Func<bool> callback;
            public ConditionPlaceholder(Func<bool> callback) => this.callback = callback;
            public override bool Evaluate() => callback();
        }

        private class ActionPlaceholder<T> : IAction<T>
        {
            private readonly Func<T> callback;
            public ActionPlaceholder(Func<T> callback) => this.callback = callback;
            public override T Fire() => callback();
        }
    }
}
