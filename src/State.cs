using System;

namespace DTools.FSM
{
	public abstract class State<TState>: IDisposable where TState : State<TState>
	{
		protected StateMachine<TState> StateMachine { get; private set; }
		protected bool Active { get; private set; }

		public void Enter(TState prevState)
		{
			Active = true;
			OnEnter(prevState);
		}

		public void Exit(TState nextState)
		{
			Active = false;
			OnExit(nextState);
		}
		
		protected virtual void OnEnter(TState prevState)
		{}

		protected virtual void OnExit(TState nextState)
		{}
		
		public virtual void Dispose()
		{}

		public void SetStateMachine(StateMachine<TState> stateMachine)
		{
			StateMachine = stateMachine;
		}
	}

	public interface IParamState<TValue>
	{
		void Enter<TState>(TState prevState, TValue parameter) where TState : State<TState>;
	}
}