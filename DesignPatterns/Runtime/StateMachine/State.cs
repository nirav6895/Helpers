namespace NG.DesignPatterns
{
    /// <summary>
    /// Convenience base class for states of a <see cref="StateMachine{TEvent}"/>. Gives access to the owning
    /// <see cref="Machine"/> (e.g. to execute events) and the state's <see cref="Name"/>.
    /// Override only the callbacks you need.
    /// </summary>
    /// <typeparam name="TEvent">Event key type of the owning state machine.</typeparam>
    public class State<TEvent> : IState
    {
        #region Properties
        /// <summary>
        /// Name the state was added with. Not necessarily unique; used in log messages.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The state machine this state was added to, or null before AddState.
        /// </summary>
        public StateMachine<TEvent> Machine { get; private set; }
        #endregion


        #region Virtual Methods
        /// <summary>
        /// Called once, when the state is added to a state machine. <see cref="Machine"/> and <see cref="Name"/> are set.
        /// </summary>
        protected virtual void Initialize() { }

        /// <inheritdoc/>
        public virtual void Enter(object data) { }

        /// <inheritdoc/>
        public virtual void Tick() { }

        /// <inheritdoc/>
        public virtual void Exit() { }
        #endregion


        #region Methods
        public override string ToString()
        {
            return Name ?? GetType().Name;
        }

        /// <summary>
        /// Called by <see cref="StateMachine{TEvent}.AddState(string, IState)"/>.
        /// </summary>
        internal void Attach(StateMachine<TEvent> machine, string name)
        {
            Machine = machine;
            Name = name;
            Initialize();
        }
        #endregion
    }
}
