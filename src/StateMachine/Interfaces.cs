using System;


namespace ExpressEngine.StateMachine.Interfaces
{
    public interface ICondition
    {
        bool Evaluate();
    }

    public interface IAction<T>
    {
        T Evaluate();
    }

    public interface ITransitionBuilderInitial<T>
    {
        ITransitionBuilderCondition<T> If(ICondition condition);
        ITransitionBuilderCondition<T> If(Func<bool> condition);
    }

    public interface ITransitionBuilderCondition<T>
    {
        ITransitionBuilderCondition<T> Then(T targetState);
        ITransitionBuilderCondition<T> Then(Func<T> nestedEvaluation);
        ITransitionBuilderCondition<T> ElseIf(ICondition condition);
        ITransitionBuilderCondition<T> ElseIf(Func<bool> condition);
        IAction<T> Else(T targetState);
    }
}
