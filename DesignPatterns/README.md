# Design Patterns

Reusable patterns for Unity projects. Everything lives in the `NG.DesignPatterns` namespace, so one `using` is enough.

## Install

Add the package to `Packages/manifest.json`, pinned to a release tag so updates to `main` can't break your project:

```json
"com.ng.designpatterns": "https://github.com/nirav6895/Helpers.git?path=/DesignPatterns#designpatterns-v1.0.0"
```

To update, change the tag. See [CHANGELOG.md](CHANGELOG.md) for what changed.

## Which one should I use?

| Need | Use |
| --- | --- |
| A manager MonoBehaviour that exists once and is reached from anywhere | `Singleton<T>` |
| Code that depends on an interface, with the implementation swappable (tests, platforms) | `ServiceLocator` |
| Systems that react to something without knowing who caused it | `EventBus` with `IEvent` types |
| A simple payload-free signal | `EventBus<TEnum>` |
| Flow with clear states (game flow, UI flow, AI) | `StateMachine<TEvent>` |
| Prefab instances created and destroyed often (bullets, list items) | `ComponentPool<T>` |
| Plain C# objects created often (lists, StringBuilders, data objects) | `ObjectPool<T>` |
| Data shared between scenes for the current session | `SessionStore` |

Rules that apply to all of them:

- **Main thread only.** None of them are thread-safe; marshal background callbacks to the main thread first.
- **Unregister what you register.** Pair `Register`/`Add` in `OnEnable`/`Awake` with `Unregister`/`Remove` in `OnDisable`/`OnDestroy`.
- **Static state is reset when Play Mode starts**, so the patterns work with Domain Reload disabled.

## Singleton

```csharp
public class AudioManager : Singleton<AudioManager>
{
    protected override void OnAwake() { /* setup: runs only on the instance that is kept */ }
}

AudioManager.Instance.Play(clip);
```

- A second instance destroys its own GameObject; `Instance` keeps the first.
- Put setup in `OnAwake`, not `Awake`. If you override `OnDestroy`, call `base.OnDestroy()`.
- Tick **Persist Across Scenes** in the Inspector (or override `PersistAcrossScenes`) for `DontDestroyOnLoad`.

## ServiceLocator

```csharp
public interface IAudioService : IService { void Play(AudioClip clip); }

public class AudioService : MonoBehaviour, IAudioService
{
    void Awake()     => ServiceLocator.Add<IAudioService>(this);   // register under the interface
    void OnDestroy() => ServiceLocator.Remove(this);                // only removes registrations that are still this instance
    public void Play(AudioClip clip) { }
}

ServiceLocator.Get<IAudioService>().Play(clip);                     // logs an error if missing
if (ServiceLocator.TryGet(out IAudioService audio)) audio.Play(clip); // for optional services
```

## EventBus

```csharp
public struct PlayerDied : IEvent { public int Score; }

void OnEnable()  => EventBus.Register<PlayerDied>(OnPlayerDied);
void OnDisable() => EventBus.Unregister<PlayerDied>(OnPlayerDied);
void OnPlayerDied(PlayerDied e) { }

EventBus.Execute(new PlayerDied { Score = 120 });
```

- Listeners get events whose runtime type is exactly the registered type.
- A listener that throws is logged with its stack trace; the other listeners still run.
- Methods of destroyed objects are removed automatically, with a warning naming the class that forgot to unregister.

`EventBus<TEnum>` uses enum keys instead. The key says nothing about the payload type, so prefer `IEvent` types for events with data. If an `Execute` reaches none of a key's listeners because of a payload mismatch, a warning shows what they expect.

```csharp
EventBus<GameEvent>.Register(GameEvent.Paused, OnPaused);
EventBus<GameEvent>.Execute(GameEvent.Paused);
```

## StateMachine

```csharp
public enum FlowEvent { Play, Die, Retry }

class Menu : State<FlowEvent> { public override void Enter(object data) { /* show menu */ } }
class Playing : State<FlowEvent> { public override void Tick() { /* every frame */ } }

var flow = new StateMachine<FlowEvent>("GameFlow");
var menu = new Menu(); var playing = new Playing(); var gameOver = new State<FlowEvent>();
flow.AddState("Menu", menu);
flow.AddState("Playing", playing);
flow.AddState("GameOver", gameOver);
flow.AddTransition(menu, playing, FlowEvent.Play);
flow.AddTransition(playing, gameOver, FlowEvent.Die);
flow.AddMasterTransition(menu, FlowEvent.Retry);    // from any state
flow.StartStateMachine();

// In the owner's Update:  flow.Tick();
// Anywhere:               flow.ExecuteEvent(FlowEvent.Play);
```

- **Run to completion:** an event executed inside `Enter`, `Tick`, `Exit` or a `StateChanged` listener runs after that callback returns.
- **Unhandled events are ignored** by default. Set `DeferUnhandledEvents = true` to replay them once a later state can handle them.
- `AddAutoTransition(from, to)` moves on right after `from` is entered. `SetEndState` makes the machine ignore events until `RestartStateMachine`.
- `RestartStateMachine` and `StopStateMachine` call the current state's `Exit`. Stop the machine in the owner's `OnDestroy`.
- Misconfiguration (duplicate states or transitions, events before start) always logs a warning. Set `DebugMode = true` to log every transition.
- States can be any `IState`, including a MonoBehaviour. The per-frame callback is `Tick`, so Unity doesn't also call it as `Update`.

The **Counter-Based State Machine** sample (Package Manager > Design Patterns > Samples) extends it for server-driven, turn-based games.

## ComponentPool

```csharp
public class BulletPool : ComponentPool<Bullet> { }   // add to a GameObject and assign the prefab

Bullet bullet = bulletPool.Get(muzzle.position, muzzle.rotation);
bulletPool.Release(bullet);
```

- Implement `IPoolable` on the pooled component to reset state in `OnSpawn`/`OnDespawn`, or override `OnGet`/`OnRelease` in the pool.
- **Prewarm Count** creates instances in `Awake`; **Max Size** caps how many inactive instances are kept.
- Releasing the same item twice is ignored with a warning, and items destroyed while pooled are skipped.

## ObjectPool

The same pool for plain C# objects. It's not a MonoBehaviour: create it in code with a factory function.

```csharp
var listPool = new ObjectPool<List<Enemy>>(
    createFunc: () => new List<Enemy>(),
    actionOnRelease: list => list.Clear(),
    maxSize: 16);

List<Enemy> nearby = listPool.Get();
// ... use it ...
listPool.Release(nearby);
```

- Optional callbacks: `actionOnGet`, `actionOnRelease` (also applied to prewarmed items) and `actionOnDestroy` (items dropped by `maxSize` or `Clear`).
- Items that implement `IPoolable` get `OnSpawn` after `actionOnGet` and `OnDespawn` before `actionOnRelease`.
- `Prewarm`, `MaxSize`, double-release protection and skipping of destroyed Unity objects work as in `ComponentPool<T>`.
- `UnityEngine.Pool` also has an `ObjectPool<T>`. In a file that imports both namespaces, write `NG.DesignPatterns.ObjectPool<T>`.

## SessionStore

```csharp
public class PlayerProfile : ISessionData { public string Name; }

SessionStore.GetOrCreate<PlayerProfile>().Name = "Nirav";
string name = SessionStore.Get<PlayerProfile>()?.Name;
```

- One object per concrete type, kept in memory for the lifetime of the app. **Nothing is saved to disk.**
- Objects are keyed by their concrete runtime type: `Set(baseTypedVariable)` is found with `Get<ConcreteType>()`.

## Tests

EditMode and PlayMode tests live in `Tests/`. They show up in the Test Runner when the package is in `Assets/`, or when it's listed under `testables` in a project's manifest.
