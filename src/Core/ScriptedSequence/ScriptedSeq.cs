
/// <summary>
/// 
/// </summary>
namespace ExpressEnginex.ScriptedSeq
{
    [System.Serializable]
    public class ScriptedSeq<T>
    {
        public struct CurrentActnInfo
        {
            public ScriptedAction<T> actn;
            public int execMethodID;


            public CurrentActnInfo(ScriptedAction<T> actn, int execMethodID)
            {
                this.actn = actn;
                this.execMethodID = execMethodID;
            }
        }

        public enum StopProcedure
        {
            StopAbruptly,
            StopAfterCurrent,
            StopAfterAll,
        }

        private readonly static int START_METHOD_ID  = 1010;
        private readonly static int UPDATE_METHOD_ID = 2020;
        private readonly static int END_METHOD_ID    = 3030;


        private readonly ScriptedAction<T>[] actionsArray; // list of actions, as set by user

        private readonly T owner;                      // the owner of this action list
        private int idx = -1;                          // index of current executing action
        private ScriptedAction<T> currentAction;       // current action which is executing
        private ScriptedAction<T> lastActn;            // last executing action

        private bool isInTransition;                   // 
        private StopProcedure stopProcedure;
        private bool shouldStop;                       //
        private bool isStopped;                        // 

        public CurrentActnInfo currentActnInfo;        //
        public bool currentActnInfoUpdated;            //

        // cache
        private ScriptedAction<T>.EndStatus successStatus;


        // constructor
        public ScriptedSeq(T _owner, ScriptedAction<T>[] actions=default)
        {
            owner = _owner;
            actionsArray = actions;

            idx = -1;

            stopProcedure = StopProcedure.StopAfterCurrent;
            shouldStop = false;
            isStopped  = false;

            currentActnInfoUpdated = false;
        }

        public void Update()
        {
            if (actionsArray.Length == 0)
                return;

            // update stop procedure first
            if (shouldStop)
            {
                switch(stopProcedure)
                {
                    case StopProcedure.StopAbruptly:
                        isStopped = true;
                        break;

                    // ** other two procedures are checked in UpdateCurrentActnInfo method
                }
            }
            // ---------------------------

            if (isStopped)
                return;

            if (currentAction == null)
                MoveToFirst(); 

            if(currentAction != null)
                currentAction.InternalUpdate();

            if (isInTransition)
            {
                Transition(lastActn);
                return;
            }

            successStatus = currentAction.Update();

            if (successStatus == ScriptedAction<T>.EndStatus.Success)
                Step(previous: false);

            else
            {
                switch(successStatus)
                {
                    case ScriptedAction<T>.EndStatus.ResetFromStart:
                        MoveToFirst();
                        break;

                    case ScriptedAction<T>.EndStatus.ResetFromLast:
                        Step(previous: true);
                        break;

                    case ScriptedAction<T>.EndStatus.ResetFromCurrent:
                        currentAction.OnStart();
                        break;
                }
            }
        }

        /// <summary>
        /// Move one step forward or backward the actions list.
        /// </summary>
        private void Step(bool previous=false)
        {
            lastActn = currentAction;

            if (previous)
                idx--;
            else
                idx++;

            idx = UnityEngine.Mathf.Clamp(idx, 0, actionsArray.Length - 1);

            isInTransition = true;
            _currentActionExited = false;
        }

        private void MoveToFirst()
        {
            lastActn = currentAction;
            idx = 0;

            isInTransition = true;
            _currentActionExited = false;
        }


        private bool _currentActionExited = false;
        private void Transition(ScriptedAction<T> lastActn)
        {
            currentAction = actionsArray[idx];

            if (lastActn != null)
            {
                if (!_currentActionExited && !lastActn.OnEnd())
                { UpdateCurrentActnInfo(END_METHOD_ID); return; }

                if (!_currentActionExited)
                { _currentActionExited = true; lastActn.Reset(); }

                UpdateCurrentActnInfo(START_METHOD_ID);
                if (!isStopped && !currentAction.OnStart())
                    return;
            }
            else
            {
                if (!_currentActionExited)
                { _currentActionExited = true; }

                UpdateCurrentActnInfo(START_METHOD_ID);
                if (!isStopped && !currentAction.OnStart())
                    return;
            }

            isInTransition = false;
            UpdateCurrentActnInfo(UPDATE_METHOD_ID);
        }


        private int _currentExecMethodID = -1;
        private int _lastExecMethodID = -1;
        private void UpdateCurrentActnInfo(int newExecutingMethodID)
        {
            if(_currentExecMethodID != newExecutingMethodID)
            {
                currentActnInfo      = new CurrentActnInfo(currentAction, newExecutingMethodID);
                _lastExecMethodID    = _currentExecMethodID;
                _currentExecMethodID = newExecutingMethodID;
            }

            if (shouldStop)
            {
                switch (stopProcedure)
                {
                    case StopProcedure.StopAfterCurrent:
                        if (_lastExecMethodID == END_METHOD_ID)
                            isStopped = true;
                        break;

                    case StopProcedure.StopAfterAll:
                        if (idx == actionsArray.Length - 1 && _currentExecMethodID == END_METHOD_ID)
                            isStopped = true;
                        break;
                }
            }
        }

        public void Start()
        {
            this.isStopped = false;
            this.shouldStop = false;
        }

        public void SetStopProcedure(StopProcedure stopProcedure)
        {
            this.shouldStop = true;
            this.stopProcedure = stopProcedure;
        }

        public CurrentActnInfo GetCurrentActnInfo() => currentActnInfo;

        public T GetOwner() => owner;

        public bool IsOK() => isStopped;

        public bool IsStopped() => false;
    }
}
