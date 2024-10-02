using System;
using System.Collections.Generic;
using UnityEngine;


namespace ExpressEnginex.StateMachine
{
    public class TransitionBuilder : MonoBehaviour
    {
        public class LogicBuilder<T>
        {
            private readonly StateMachine<T> _stateMachine;
            private readonly List<Func<bool>> _conditions;
            private int _targetState;

            // Constructor to initialize the builder with the state machine
            public LogicBuilder(StateMachine<T> stateMachine)
            {
                _stateMachine = stateMachine;
                _conditions = new List<Func<bool>>();
            }

            // Static method to begin the builder chain
            public static LogicBuilder<T> Begin(StateMachine<T> stateMachine)
            {
                return new LogicBuilder<T>(stateMachine);
            }

            // Start the logic with an If condition
            public LogicBuilder<T> If(Func<bool> condition)
            {
                _conditions.Add(condition);
                return this;
            }

            // And condition (logical AND)
            public LogicBuilder<T> And(Func<bool> condition)
            {
                _conditions.Add(condition);
                return this;
            }

            // Or condition (logical OR)
            public LogicBuilder<T> Or(Func<bool> condition)
            {
                _conditions.Add(condition);
                return this;
            }

            // Negate a condition (logical NOT)
            public LogicBuilder<T> Not(Func<bool> condition)
            {
                _conditions.Add(() => !condition());
                return this;
            }

            // Define the target state to switch to
            public LogicBuilder<T> Then(int targetState)
            {
                _targetState = targetState;
                return this;
            }

            // Evaluate all conditions and switch state if valid
            public void Evaluate()
            {
                // Process each condition and combine the results
                bool result = true;
                foreach (var condition in _conditions)
                {
                    result &= condition();  // Simply AND all conditions
                    if (!result) break; // If any condition fails, break early
                }

                if (result)
                {
                    _stateMachine.SwitchState(_targetState);
                }
            }
        }
    }
}