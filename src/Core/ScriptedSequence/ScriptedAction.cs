 
namespace ExpressEnginex.ScriptedSeq
{
    [System.Serializable]
    public class ScriptedAction<T>
    {
        public readonly static int START_METHOD_ID  = 1;
        public readonly static int UPDATE_METHOD_ID = 2;
        public readonly static int END_METHOD_ID    = 3;


        public enum ExecutionResult
        {
            Running,
            Success,

            ResetFromCurrent,
            ResetFromLast,
            ResetFromStart
        }


        protected T owner;

        private float elapsedTime;

        public string label = "";


        public ScriptedAction(T owner, string label="")
        {
            this.owner = owner;
            this.label = label;
        }

        public virtual bool OnStart()
        {
            return true;
        }

        public virtual ExecutionResult Update()
        {
            return ExecutionResult.Success;
        }

        /// <summary>
        /// Internally used to update, should not be called by end user.
        /// </summary>
        /// <param name="reset"></param>
        public void InternalUpdate(bool reset = false)
        {
            if (reset)
                elapsedTime = 0;

            elapsedTime += UnityEngine.Time.deltaTime;
        }

        public virtual bool OnEnd()
        {
            return true;
        }

        public void Reset() => elapsedTime = 0.0f;

        public float ElapsedTime() => elapsedTime;
    }
}
