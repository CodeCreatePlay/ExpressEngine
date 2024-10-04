 
namespace ExpressEnginex.ScriptedSeq
{
    [System.Serializable]
    public class ScriptedAction<T>
    {
        public enum EndStatus
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

        public virtual EndStatus Update()
        {
            return EndStatus.Success;
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

        public virtual bool OnEnd() { return true; }

        public void Reset() => elapsedTime = 0.0f;

        public float ElapsedTime() => elapsedTime;
    }
}
