using System;
using ExpressEnginex.Fuzzy.Interfaces;


namespace ExpressEnginex.Fuzzy
{
    internal class FuzzyRuleBuilder : IRuleApplier, IRuleBuilder
    {
        private enum BuilderState
        {
            Initial,  // No conditions or actions yet
            If,       // After If is set
            Then,     // After Then is set following If or ElseIf
            ElseIf,   // After ElseIf is set (optional)
            Else,     // After Else is set (final state)
            Complete  // Rule building is complete
        }

        private BuilderState _state = BuilderState.If;
        private ICondition _condition;

        internal FuzzyRuleBuilder(ICondition condition)
        {
            _condition = condition;
        }

        public IRuleApplier If(ICondition condition)
        {
            if (_state != BuilderState.Initial)
                throw new InvalidOperationException("If must be the first condition.");
            
            _state = BuilderState.If;
            _condition = condition;
            return this;
        }

        public IRuleApplier Then(IConsequent consequent)
        {
            if (_state != BuilderState.If && _state != BuilderState.ElseIf)
                throw new InvalidOperationException("Then must follow If or ElseIf.");

            _state = BuilderState.Then;
            consequent.Add(_condition);
            return this;
        }

        public IRuleApplier ElseIf(ICondition condition)
        {
            if (_state != BuilderState.Then)
                throw new InvalidOperationException("ElseIf must follow Then.");
            
            _state = BuilderState.ElseIf;
            _condition = Condition.And(_condition, condition);  // Combine conditions logically
            return this;
        }

        public IRuleApplier Else(IConsequent consequent)
        {
            if (_state != BuilderState.Then)
                throw new InvalidOperationException("Else must follow Then.");
            
            _state = BuilderState.Else;
            var invertedCondition = Condition.Not(_condition);  // Invert the previous conditions for the Else case
            consequent.Add(invertedCondition);
            return this;
        }

        public IRuleBuilder And(ICondition condition)
        {
            if (_state != BuilderState.If)
                throw new InvalidOperationException("And can only follow If or ElseIf.");
            
            _condition = Condition.And(_condition, condition);
            return this;
        }

        public IRuleBuilder Or(ICondition condition)
        {
            if (_state != BuilderState.If)
                throw new InvalidOperationException("Or can only follow If or ElseIf.");
            
            _condition = Condition.Or(_condition, condition);
            return this;
        }

        // Mark the rule as complete after Else or Then (in case of no Else)
        public void Complete()
        {
            if (_state != BuilderState.Then && _state != BuilderState.Else)
                throw new InvalidOperationException("Rule must be completed after Then or Else.");
            
            _state = BuilderState.Complete;
        }
    }

	
	/*
    internal class FuzzyRuleBuilder : IRuleApplier, IRuleBuilder
    {
        private readonly ICondition _condition;


        internal FuzzyRuleBuilder(ICondition condition)
        {
            _condition = condition;
        }
    
        public IRuleApplier Then(IConsequent consequent)
        {
            consequent.Add(_condition);
            return this;
        }
   
        public IRuleApplier And(IConsequent consequent)
        {
            consequent.Add(_condition);
            return this;
        }

        public IRuleApplier Else(IConsequent consequent)
        {
            var invertedFuzzyCondition = Condition.Not(_condition);
            consequent.Add(invertedFuzzyCondition);
            return new FuzzyRuleBuilder(invertedFuzzyCondition);
        }

        public IRuleBuilder ElseIf(ICondition condition)
        {
            var invertedCondition = Condition.Not(_condition);
            var combinedCondition = Condition.And(invertedCondition, condition);
            return new FuzzyRuleBuilder(combinedCondition);
        }

        public IRuleBuilder And(ICondition condition)
        {
            var combinedCondition = Condition.And(_condition, condition);
            return new FuzzyRuleBuilder(combinedCondition);
        }

        public IRuleBuilder Or(ICondition condition)
        {
            var combinedCondition = Condition.Or(_condition, condition);
            return new FuzzyRuleBuilder(combinedCondition);
        }
    }
	*/
}
