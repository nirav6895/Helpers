namespace NG.DesignPatterns
{
    /// <summary>
    /// Lifecycle callbacks of a state in a <see cref="StateMachine{TEvent}"/>.
    /// Derive from <see cref="State{TEvent}"/>, or implement this directly, e.g. on a MonoBehaviour.
    /// <para>
    /// The per-frame callback is named Tick, not Update, so a MonoBehaviour can implement this interface
    /// without Unity also calling it as its Update message.
    /// </para>
    /// </summary>
    public interface IState
    {
        #region Methods
        /// <summary>
        /// Called when the machine switches to this state.
        /// </summary>
        /// <param name="data">Data passed to ExecuteEvent or StartStateMachine; null for auto transitions.</param>
        void Enter(object data);

        /// <summary>
        /// Called by <see cref="StateMachine{TEvent}.Tick"/> while this is the current state.
        /// </summary>
        void Tick();

        /// <summary>
        /// Called when the machine leaves this state, restarts or stops.
        /// </summary>
        void Exit();
        #endregion
    }
}
