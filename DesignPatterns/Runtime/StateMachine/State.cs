#region Using
using System.Collections.Generic;
#endregion

namespace NG.DesignPatterns.StateMachine
{
    /// <Summary> 
    /// Basic Structure of State which contains of Enter, Exit and Update etc.
    /// </Summary>
    public class State : IState
    {
        #region Public Variables and Properties
        /// <Summary> 
        /// Name of the State
        /// It's not unique parameter. It's just to remember.
        /// </Summary>
        public string Name { get; private set; }

        /// <Summary> 
        /// Dicationary of Transitions : Event Name and ToState
        /// It contains only those transitions which can possible from this State only
        /// </Summary>
        public Dictionary<string, State> transitions { get; private set; }

        /// <Summary> 
        /// State Machine Reference
        /// </Summary>
        public StateMachine stateMachine = null;
        #endregion


        #region Public Methods
        /// <Summary> 
        /// Initialize the State
        /// </Summary>
        public void Initialize(string name, StateMachine stateMachine)
        {
            Name = name;
            this.stateMachine = stateMachine;
            transitions = new Dictionary<string, State>();
            Initialize();
        }

        /// <Summary> 
        /// Initialize the State
        /// This method is used for overriding
        /// </Summary>
        public virtual void Initialize()
        {

        }

        /// <Summary> 
        /// Entering into State
        /// </Summary>
        public virtual void Enter(object data = null)
        {
        }

        /// <Summary> 
        /// Exiting from State
        /// </Summary>
        public virtual void Exit()
        {
        }

        /// <Summary> 
        /// Updating State. This happens on each frame.
        /// </Summary>
        public virtual void Update()
        {
        }

        /// <Summary> 
        /// Add Transition for this State
        /// </Summary>
        public virtual void AddTransition(State toState, string eventName)
        {
            if (transitions.ContainsKey(eventName))
            {
                stateMachine.DebugLogWarning(Name + " already have transition with event Name : " + eventName);
                return;
            }

            transitions.Add(eventName, toState);
        }

        /// <Summary> 
        /// Add Transition for this State
        /// </Summary>
        public virtual void AddAutoTransition(State toState)
        {
            AddTransition(toState, StateMachine.AUTO_EVENT);
        }
        #endregion
    }
}