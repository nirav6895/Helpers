namespace NG.DesignPatterns.StateMachine
{
    public interface IState
    {
        #region Methods
        void Initialize(string name, StateMachine stateMachine);
        void Initialize();
        void Enter(object data = null);
        void Update();
        void Exit();
        void AddTransition(State toState, string eventName);
        void AddAutoTransition(State toState);
        #endregion
    }
}