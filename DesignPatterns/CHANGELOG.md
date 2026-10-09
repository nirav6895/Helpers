# Changelog

## [1.0.0] - 2026-10-09

First tagged release. It has breaking changes from the untagged initial import, which no project used yet.

### Changed
- All types are now in the `NG.DesignPatterns` namespace. The per-pattern namespaces had the same names as their classes, which broke short names inside `NG.DesignPatterns.*`.
- Static `Dispose()` methods are renamed to `Clear()`.
- **StateMachine** is now `StateMachine<TEvent>` (enum or string events), with `State<TEvent>` and an `IState` interface that any class can implement, including a MonoBehaviour. Transitions belong to the machine. `Update` is renamed to `Tick` and is public.
- **StateMachine**: unhandled events are ignored by default. The old replay-later behaviour is opt-in through `DeferUnhandledEvents`, and one unhandled event no longer blocks the events queued behind it.
- **EventObserver**: `Execute<T>` dispatches by the event's runtime type, like `Execute(IEvent)` does.
- **EventObserver&lt;TEnum&gt;**: the `struct` constraint on payloads is removed, so strings and classes work.
- **ServiceLocator**: `IService` is a marker interface (no `Register()` method). `Get<T>()` logs an error when the service is missing.
- **DataBase** is renamed to **SessionStore** (`Set`, `Get`, `TryGet`, `GetOrCreate`, `Contains`, `Remove`, `Clear`), and `IData` to `ISessionData`. Objects are always keyed by their runtime type. `IData.Save` and `SaveState()` are removed.
- **ObjectPool** is renamed to **ComponentPool** (it clashed with `UnityEngine.Pool.ObjectPool<T>`), with `Get`/`Release` instead of `Pop`/`Push`. Released items always go back under the pool's transform.
- **CounterBasedStateMachine** moved to the importable *Counter-Based State Machine* sample and now uses `ExecuteCountedEvent(eventKey, counter, data)`.

### Added
- **StateMachine**: run-to-completion processing, `StopStateMachine`, `StateChanged`, `CanHandle`, `DebugMode`, `MaxStepsPerCall` loop guard, and a custom event comparer.
- **Singleton**: `OnAwake` hook, optional *Persist Across Scenes* (`DontDestroyOnLoad`).
- **ServiceLocator**: `TryGet`, `Remove(instance)`.
- **EventObserver**: `HasListeners<T>()`.
- **ComponentPool**: `IPoolable`, `OnGet`/`OnRelease` hooks, `Prewarm`, *Max Size*, `Get(position, rotation, parent)`, `Clear`.
- Static registries reset when Play Mode starts, so the patterns work with Domain Reload disabled.
- README, EditMode and PlayMode tests.

### Fixed
- **StateMachine**: `State.Update()` was never called. Warnings could never be shown. `RestartStateMachine` skipped `Exit()` and kept the counter machine's queued events. Master transitions ignored the target state's auto transition. A state could be exited before its `Enter` returned.
- **CounterBasedStateMachine**: a re-sent counter threw `ArgumentException` out of `ExecuteEvent`. Queued events were replayed without their counter.
- **Singleton**: a duplicate's subclass setup ran on the destroyed object. `Instance` kept pointing at a destroyed object. `DestroyImmediate` was used at runtime. A wrong type argument silently left `Instance` null.
- **EventObserver**: listener exceptions were logged without a stack trace, and `Execute(IEvent)` logged only "Exception has been thrown by the target of an invocation". Every `Execute` allocated. `Execute(IEvent)` used reflection. Registering a callback twice made it fire twice.
- **EventObserver&lt;TEnum&gt;**: a payload type mismatch was dropped silently.
- **ServiceLocator**: `Remove<T>()` from an old instance removed its replacement. Destroyed Unity services were still returned.
- **DataBase**: saving through a base-typed variable stored the object under the base type, so loading the concrete type silently created a new empty object. Struct data was returned as a copy, so edits were lost.
- **ObjectPool**: releasing an item twice handed it to two owners. Items parked under another parent could be destroyed while pooled, and `Pop` then returned a destroyed object.
- XML doc comments used `<Summary>`, which IDEs don't show.
