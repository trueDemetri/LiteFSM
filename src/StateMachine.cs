//#define LOGGING_ON

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DTools.FSM
{
    public class StateMachine<TStateBase> : IDisposable where TStateBase : State<TStateBase>
    {
        private Dictionary<Type, TStateBase> _states;
        private bool _showErrors;
        private readonly bool _debug;

        public TStateBase CurrentState { get; private set; }

        public StateMachine(bool debug = false)
        {
	        _debug = debug;
        }

        public void SetStates(IReadOnlyCollection<TStateBase> states, bool showErrors = true)
	    {
		    ClearStates();
		    _showErrors = showErrors;
		    _states = new Dictionary<Type, TStateBase>(states.Count);
		    foreach (var state in states)
		    {
			    state.SetStateMachine(this);
				_states.Add(state.GetType(), state);
		    }
	    }
        
        public TState SetState<TState>(bool allowTransitionToSelf = false) where TState : TStateBase
        {
	        var states = SetStateInner<TState>(allowTransitionToSelf);
	        states.newState.Enter(states.oldState);

	        return states.newState;
        }

        public TState SetState<TState, TValue>(TValue parameter = default, bool allowTransitionToSelf = false) where TState : TStateBase, IParamState<TValue>
        {
	        var states = SetStateInner<TState>(allowTransitionToSelf);
	        states.newState.Enter(states.oldState, parameter);
	        return states.newState;
        }

        public void Dispose()
        {
			ClearStates();
        }

        private (TState newState, TStateBase oldState) SetStateInner<TState>(bool allowTransitionToSelf) where TState : TStateBase
        {
	        var stateType = typeof(TState);
	        if (!_states.TryGetValue(stateType, out var newState))
	        {
		        throw new Exception($"Unknown state with type {stateType.Name}");
	        }

	        if (CurrentState != null)
	        {
		        if (!allowTransitionToSelf && CurrentState.Equals(newState))
		        {
			        if (_showErrors)
				        Debug.LogError($"Trying to change state from {CurrentState.GetType().Name} to the same({newState.GetType().Name})");
			        return default;
		        }

		        CurrentState.Exit(newState);
	        }
	        
	        Log($"From {CurrentState?.GetType().Name} to {stateType.Name} at {Time.frameCount}");
	        var oldState = CurrentState;
	        CurrentState = newState;
	        return (newState as TState, oldState);
        }

        private void ClearStates()
        {
	        if (_states == null) return;
	        
	        foreach (var state in _states.Values)
	        {
		        state.Dispose();
	        }

	        _states.Clear();
        }

        [Conditional("LOGGING_ON")]
        private void Log(string message)
        {
	        if (_debug)
				Debug.Log(message);
        }
    }
}