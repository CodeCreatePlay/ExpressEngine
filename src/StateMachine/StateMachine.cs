namespace ExpressEngine.StateMachine
{	
    public class StateMachine<T>
    {
        private System.Collections.Generic.Dictionary<int, StateBase<T>> statesmap;

        private StateBase<T> globalState; // generally global should be the one where entiry stops to exist

        public static readonly int INVALID_STATE_ID = -1;
        public static readonly int GLOBAL_STATE_ID  = 1010;

        private StateBase<T> currentState;
        private int currentStateIdx = -1;
        private int nextStateIdx = -1;
        private int lastStateIdx = -1;

        private bool hasGlobalState  = false;
        private bool isInGlobalState = false;
        private System.Func<bool> globalStateTrigger = null; // global state condition callback, sm will switch
															 // to global state as long as this will return true

        public StateBase<T> CurrentState { get { return currentState; } }
        public int CurrentStateIDX { get { return currentStateIdx; } }
        public T Owner  { get; private set; }
        public System.Collections.Generic.Dictionary<int, StateBase<T>> StatesMap
        {
            get 
            {
                if (statesmap == null) statesmap = new();
                return statesmap;
            }
        }
		
        // cache
        // internally used to keep track of transition
        private bool shouldTransition = false;
        private bool currentStateExited = false;


        // constructor
        public StateMachine(T owner)
        {
            this.Owner = owner;
        }

        public void AddState(StateBase<T> newState, int idx)
        {
            if (!CanAddState(state: newState, idx))
                return;

            StatesMap[idx] = newState;
        }

        public bool AddState(System.Action updateMethodCallback, int idx,
    System.Func<bool> enterMethodCallback = null, System.Func<bool> exitMethodCallback = null)
        {
            PlaceholderState placeHolderState = new(Owner, enterMethodCallback, updateMethodCallback, exitMethodCallback);

            if (!CanAddState(placeHolderState, idx))
                return false;

            AddState(placeHolderState, idx);
            return true;
        }

        public void AddGlobalState(StateBase<T> state, System.Func<bool> triggerMethodCallback)
        {
            if (state == null || triggerMethodCallback == null)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogWarning("Failed to add global state.");
#endif
                return;
            }

            globalState = state;
            this.globalStateTrigger = triggerMethodCallback;
            hasGlobalState = true;
        }

        public StateBase<T> AddGlobalState(
            StateBase<T> state, System.Func<bool> triggerMethodCallback, System.Action update,
            System.Func<bool> enter = null, System.Func<bool> exit = null)
        {
            if (state == null || triggerMethodCallback == null)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogWarning("Failed to add global state.");
#endif
                return null;
            }

            PlaceholderState placeHolderState = new(Owner, enter, update, exit);
            globalState = placeHolderState;
            globalStateTrigger = triggerMethodCallback;
            hasGlobalState = true;
            return globalState;
        }

        private bool CanAddState(StateBase<T> state, int idx)
        {
            if(idx == INVALID_STATE_ID || idx < 0 || idx == GLOBAL_STATE_ID)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogWarningFormat("Unable to add state '{0}', invalid state idx", nameof(state));
#endif
                return false;
            }

            if (state == null || StatesMap.ContainsValue(state))
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogWarningFormat("Unable to add state '{0}', either value already exists, or its null.", nameof(state));
#endif
                return false;
            }

            if (idx == INVALID_STATE_ID || StatesMap.ContainsKey(idx))
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogWarningFormat("Unable to add state '{0}', key already exists.", idx);
#endif
                return false;
            }

            return true;
        }

        public void SwitchState(int idx)
        {
            StateBase<T> newState;

            // make sure state exists
            if (!statesmap.ContainsKey(idx))
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogWarningFormat("Unable to switch state; state {0} does not exits.!", nameof(idx));
#endif
                return;
            }

            newState = statesmap[idx];

            if (currentStateIdx == idx)
                return;

            this.nextStateIdx = idx;
            shouldTransition = true;
        }

        /// <summary>
        /// Switches to the last state if it exists, it is up to the user to
        /// make sure last state exists using 'CanSwitchToLastState' function call.
        /// </summary>
        public void SwitchToLastState()
        {
            if (lastStateIdx != -1)
                SwitchState(lastStateIdx);
        }

        private void Transition()
        {
            if (!IsOK())
                return;

            if (!currentStateExited && currentState != null && !currentState.Exit())
                return;

            currentStateExited = true;
            lastStateIdx = currentStateIdx;
            currentStateIdx = nextStateIdx;
            currentState = statesmap[currentStateIdx];

            if (!currentState.Enter())
                return;

            currentStateExited = false;
            shouldTransition = false;
        }

        public void Update()
        {
            if (hasGlobalState && globalStateTrigger())
            {
                if(!isInGlobalState)
                {
                    // same logic as in switch state
                    isInGlobalState = true;
                    nextStateIdx = GLOBAL_STATE_ID;
                    shouldTransition = true;
                }
            }

            if (currentStateIdx != -1 && !shouldTransition)
			{
				currentState.Update();
			}
            else if(shouldTransition) 
			{
                Transition();
			}
        }

        private bool IsOK()
        {
            return UnityEngine.Application.isPlaying;
        }

        private class PlaceholderState : StateBase<T>
        {
            public PlaceholderState(T owner, System.Func<bool> enter, System.Action update,
                System.Func<bool> exit) :
                base(owner, enter, update, exit)
            {
            }
        }
    }
}