#region Using
using System;
using UnityEngine;
#endregion

namespace NG.Utility
{
    /// <summary>
    /// A countdown in plain C# (not a MonoBehaviour). Start it, call <see cref="Tick()"/> from an Update, and
    /// <see cref="Completed"/> is raised when it reaches zero.
    /// <code>
    /// private readonly CountdownTimer spawnTimer = new CountdownTimer(3f) { Repeat = true };
    ///
    /// void Start()  { spawnTimer.Completed += SpawnEnemy; spawnTimer.Start(); }
    /// void Update() { spawnTimer.Tick(); }
    /// </code>
    /// It can be paused and resumed, use unscaled time, and repeat. <see cref="Tick()"/> reads <see cref="Time"/>,
    /// so it's main thread only; <see cref="Tick(float)"/> takes the time step from you.
    /// (It's not named Timer, which would clash with System.Threading.Timer.)
    /// </summary>
    public class CountdownTimer
    {
        #region Variables
        /// <summary>
        /// Raised when the countdown reaches zero; with <see cref="Repeat"/>, every time it does.
        /// </summary>
        public event Action Completed;

        private float duration;
        private float remaining;
        private bool isRunning;
        #endregion


        #region Constructors
        /// <param name="duration">Seconds to count down from.</param>
        /// <param name="useUnscaledTime">Count with <see cref="Time.unscaledDeltaTime"/>, so it keeps running while the game is paused.</param>
        public CountdownTimer(float duration, bool useUnscaledTime = false)
        {
            Duration = duration;
            remaining = this.duration;
            UseUnscaledTime = useUnscaledTime;
        }
        #endregion


        #region Properties
        /// <summary>
        /// Seconds to count down from. Changing it takes effect on the next <see cref="Start()"/> or repeat;
        /// it doesn't change <see cref="Remaining"/>.
        /// </summary>
        public float Duration
        {
            get => duration;
            set => duration = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Seconds left.
        /// </summary>
        public float Remaining => remaining;

        /// <summary>
        /// Seconds counted so far in the current run.
        /// </summary>
        public float Elapsed => duration - remaining;

        /// <summary>
        /// 0 at the start, 1 when the countdown reaches zero, e.g. for a progress bar.
        /// </summary>
        public float Progress => duration > 0f ? Mathf.Clamp01(1f - remaining / duration) : 1f;

        /// <summary>
        /// Whether it's counting down: started and not paused, stopped or completed.
        /// </summary>
        public bool IsRunning => isRunning;

        /// <summary>
        /// Whether it has reached zero. A repeating timer never stays completed.
        /// </summary>
        public bool IsCompleted => remaining <= 0f;

        /// <summary>
        /// Start over automatically after completing, keeping any time that ran over so the period stays exact.
        /// </summary>
        public bool Repeat { get; set; }

        /// <summary>
        /// Count with <see cref="Time.unscaledDeltaTime"/> in <see cref="Tick()"/>, so it keeps running while the game is paused.
        /// </summary>
        public bool UseUnscaledTime { get; set; }
        #endregion


        #region Public Methods
        /// <summary>
        /// Starts counting down from <see cref="Duration"/>, also when it's already running.
        /// </summary>
        public void Start()
        {
            remaining = duration;
            isRunning = true;
        }

        /// <summary>
        /// Sets <see cref="Duration"/> and starts counting down from it.
        /// </summary>
        public void Start(float newDuration)
        {
            Duration = newDuration;
            Start();
        }

        /// <summary>
        /// Stops counting and keeps the remaining time. <see cref="Resume"/> continues from it.
        /// </summary>
        public void Pause()
        {
            isRunning = false;
        }

        /// <summary>
        /// Continues counting after <see cref="Pause"/>. Does nothing once the countdown has completed.
        /// </summary>
        public void Resume()
        {
            if (remaining > 0f)
                isRunning = true;
        }

        /// <summary>
        /// Stops counting and resets the remaining time to <see cref="Duration"/>.
        /// </summary>
        public void Stop()
        {
            isRunning = false;
            remaining = duration;
        }

        /// <summary>
        /// Counts down by this frame's delta time (scaled or unscaled, see <see cref="UseUnscaledTime"/>).
        /// Call it once per frame, e.g. from Update.
        /// </summary>
        public void Tick()
        {
            Tick(UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
        }

        /// <summary>
        /// Counts down by <paramref name="deltaTime"/> seconds. Negative values count as zero.
        /// Raises <see cref="Completed"/> at most once per call; with <see cref="Repeat"/>, periods missed in a long
        /// step are raised on the following calls.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!isRunning)
                return;

            remaining -= Mathf.Max(0f, deltaTime);
            if (remaining > 0f)
                return;

            if (Repeat && duration > 0f)
            {
                remaining += duration;
            }
            else
            {
                remaining = 0f;
                isRunning = false;
            }

            Completed?.Invoke();
        }
        #endregion
    }
}
