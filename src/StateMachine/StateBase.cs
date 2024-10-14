using System.Reflection;

namespace ExpressEngine.StateMachine
{
    public abstract class StateBase<T>
    {
        public readonly T Owner;

        // Default Constructor
        public StateBase(T owner)
        {
            this.Owner = owner;

            AssignDefaultOrCustomMethods();
        }

        // Constructor with explicit enter, update, exit
        public StateBase(T owner, System.Func<bool> enter, System.Action update, System.Func<bool> exit)
        {
            this.Owner = owner;

            this.Enter = enter;
            this.Update = update;
            this.Exit = exit;
        }

        // Method to assign default or custom OnEnter, OnUpdate, OnExit methods.
        private void AssignDefaultOrCustomMethods()
        {
            System.Type type = this.GetType();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            // Enter method
            MethodInfo mInfo = type.GetMethod("OnEnter", flags);
            if (mInfo != null && mInfo.ReturnType == typeof(bool))
            {
                Enter = (System.Func<bool>)mInfo.CreateDelegate(typeof(System.Func<bool>), this);
            }
            else
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogWarningFormat("User-defined 'OnEnter' not found for state '{0}', using default.", this.ToString());
#endif
                Enter = OnEnter;
            }

            // Update method
            mInfo = type.GetMethod("OnUpdate", flags);
            if (mInfo != null && mInfo.ReturnType == typeof(void))
            {
                Update = (System.Action)mInfo.CreateDelegate(typeof(System.Action), this);
            }
            else
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogWarningFormat("User-defined 'OnUpdate' not found for state '{0}', using default.", this.ToString());
#endif
                Update = OnUpdate;
            }

            // Exit method
            mInfo = type.GetMethod("OnExit", flags);
            if (mInfo != null && mInfo.ReturnType == typeof(bool))
            {
                Exit = (System.Func<bool>)mInfo.CreateDelegate(typeof(System.Func<bool>), this);
            }
            else
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogWarningFormat("User-defined 'OnExit' not found for state '{0}', using default.", this.ToString());
#endif
                Exit = OnExit;
            }
        }

        // Default implementations of Enter, Update, and Exit methods.
        private bool OnEnter()
        {
            return true;
        }

        private void OnUpdate()
        {
        }

        private bool OnExit()
        {
            return true;
        }

        // Public properties to access methods.
        public System.Func<bool> Enter { get; private set; } = null;
        public System.Action Update { get; private set; } = null;
        public System.Func<bool> Exit { get; private set; } = null;
    }
}
