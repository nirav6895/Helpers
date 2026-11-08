# Utility

Everyday helpers for Unity projects. Everything lives in the `NG.Utility` namespace, so one `using` is enough. Requires Unity 2021.3 or newer and uGUI.

## Install

Add the package to `Packages/manifest.json`, pinned to a release tag so updates to `main` can't break your project:

```json
"com.ng.utility": "https://github.com/nirav6895/Helpers.git?path=/Utility#utility-v1.0.0"
```

To update, change the tag. See [CHANGELOG.md](CHANGELOG.md) for what changed.

## What's inside

| Need | Use |
| --- | --- |
| A button that reacts to being held down (confirm, +/- repeat) | `HoldButton` |
| A ScrollRect that snaps to pages, with dots and previous/next buttons | `ScrollRectSnap`, `PageIndicator`, `ScrollSnapPageButton` |
| UI that stays clear of notches and the home indicator | `SafeArea` |
| Knowing when the device goes online or offline | `InternetMonitor` |
| One serialized value per enum member | `EnumArray<TEnum, T>` |
| A dictionary in the Inspector | `SerializableDictionary<TKey, TValue>` |
| Better list labels or read-only fields in the Inspector | `[LabelFromField]`, `[LabelFromEnum]`, `[ReadOnlyField]` |
| "1.5K", "2.5M" | `NumberAbbreviation` |
| "02:15", "1d 4h" | `TimeFormatting` |
| A pausable, repeatable countdown | `CountdownTimer` |
| PlayerPrefs for bool, long, double, enums, DateTime and objects | `Prefs` |
| Waiting for an Animator state or a Timeline to finish | `animator.PlayAndWait(...)`, `director.PlayAndWait()` |
| A spinning loading icon or collectible | `AutoRotate` |
| Small helpers: `GetOrAddComponent`, `DestroyChildren`, `With`, `WithAlpha`, `SetVisible`, `Shuffle`, `GetRandom` | Extension methods |

Components are under **Add Component > NG**. UI components use unscaled time by default, so they keep working in pause menus where `Time.timeScale` is 0.

## HoldButton

A `Button` that also reacts to being held. A tap still invokes **On Click**; holding for **Hold Duration** seconds invokes **On Hold Completed**.

```csharp
holdButton.HoldCompleted += DeleteSave;
fillImage.fillAmount = holdButton.HoldProgress;   // or connect On Hold Progress to Image.fillAmount in the Inspector
```

- The hold is canceled when the pointer is released early, leaves the button, or the button becomes non-interactable or disabled. Only the left mouse button and touches start a hold.
- **Suppress Click After Hold** (on by default) stops On Click from also firing when a completed hold is released.
- For +/- buttons, turn on **Repeat While Held** and connect On Click and On Hold Completed to the same method: a tap changes the value once, and holding keeps changing it every **Repeat Interval**.
- Every event exists twice: as a UnityEvent for the Inspector (`On Hold Started`, ...) and as a C# event for code (`HoldStarted`, ...).

## ScrollRectSnap, PageIndicator, ScrollSnapPageButton

Add `ScrollRectSnap` to the GameObject that has the `ScrollRect`. After a drag, the content moves to the nearest page, or to the next one when it was dragged past **Drag Threshold** of a page or flicked.

```csharp
snap.PageChanged += page => title.text = $"Page {page + 1} of {snap.PageCount}";
snap.GoToPage(2);              // or Next(), Previous(); GoToPage(2, animate: false) jumps
snap.Refresh();                // after adding or removing pages
```

- It scrolls horizontally if the ScrollRect's **Horizontal** is on, otherwise vertically.
- By default every active child of the content is a page and is centered in the viewport, so layout group spacing and padding are respected. Set **Page Count** to split the content into equal pages instead.
- `PageIndicator` colors one dot per page (and can swap sprites). `ScrollSnapPageButton` goes on a Button and moves to the previous or next page; at the ends it is disabled or hidden (**At End**).

## SafeArea

Add it to a full-screen child of a Screen Space canvas and put the content that must stay visible under it. Backgrounds that should fill the screen go outside it. It updates when the safe area or screen size changes, e.g. on rotation. Each edge can be turned off. Use the Device Simulator to preview phones with a notch.

## InternetMonitor

`Application.internetReachability` only says whether a network interface is up: Wi-Fi without internet, or with a login page, still counts as reachable. Add `InternetMonitor` to a GameObject in your first scene and it also requests **Check Url** every **Check Interval** seconds, counting the device as online only when the expected response comes back.

```csharp
void OnEnable()  => InternetMonitor.StatusChanged += OnInternetChanged;
void OnDisable() => InternetMonitor.StatusChanged -= OnInternetChanged;
void OnInternetChanged(bool online) => noInternetBanner.SetActive(!online);

if (InternetMonitor.IsAvailable) { /* ... */ }
InternetMonitor.SimulateOffline = true;   // test the offline flow; also a toggle in its Inspector in Play Mode
```

- The default Check Url, `https://www.google.com/generate_204`, answers 204. A captive portal answers 200 with its login page, so **Expected Response Code** 204 tells them apart. Use your own URL (and 0 = any 2xx) if you prefer.
- On WebGL the Check Url must allow cross-origin requests (CORS); otherwise turn **Verify With Request** off.
- It checks again right away when the app comes back from the background. Without a monitor, `IsAvailable` falls back to `Application.internetReachability`.

## EnumArray

One value per enum member, shown in the Inspector with each member's name.

```csharp
[SerializeField] private EnumArray<WeaponType, float> damage;

float swordDamage = damage[WeaponType.Sword];
foreach (KeyValuePair<WeaponType, float> pair in damage) { }
```

Values are saved with their enum value, so adding, removing or reordering members keeps the other values. Unlike a plain array, it can't go out of sync with the enum.

## SerializableDictionary

```csharp
[SerializeField] private SerializableDictionary<string, int> prices;

int swordPrice = prices["sword"];
```

In the Inspector it's a list of key/value rows. A row with a duplicate key (or an empty object reference as key) is highlighted and ignored: the first row with a key wins. Keys and values must be types Unity can serialize.

## Inspector attributes

```csharp
[LabelFromField(nameof(Reward.type))] public List<Reward> rewards;     // "Gold", "Gems"... instead of "Element 0"
[LabelFromEnum(typeof(WeaponType))] public float[] damagePerWeapon;    // element i is the enum's i-th member
[SerializeField, ReadOnlyField] private int score;                     // visible, not editable
```

`[LabelFromEnum]` doesn't resize the array; use `EnumArray` for that. `[ReadOnlyField]` isn't called `[ReadOnly]` so it doesn't clash with `Unity.Collections.ReadOnly`.

## Formatting

```csharp
1500.ToAbbreviatedString();                          // "1.5K"
2500000000L.ToAbbreviatedString();                   // "2.5B"
NumberAbbreviation.Abbreviate(999999, abbreviateFrom: 1000000);   // "999999"

TimeFormatting.ToClock(135);                         // "02:15"
TimeFormatting.ToClock(remaining, roundUp: true);    // for countdowns: 0.4 s left shows "00:01"
TimeFormatting.ToCompact(100000);                    // "1d 3h"
```

Units go K, M, B, T, then Qa to Dc for idle games. Output uses the invariant culture by default ("1.5K" everywhere); pass a culture to localize the decimal separator, and `TimeUnitLabels` to localize "d h m s".

## CountdownTimer

A plain C# countdown (not a MonoBehaviour). It's not called `Timer` so it doesn't clash with `System.Threading.Timer`.

```csharp
private readonly CountdownTimer spawnTimer = new CountdownTimer(3f) { Repeat = true };

void Start()  { spawnTimer.Completed += SpawnEnemy; spawnTimer.Start(); }
void Update() { spawnTimer.Tick(); }
```

`Pause`, `Resume`, `Stop`, `Remaining`, `Progress` and unscaled time are supported. `Tick(deltaTime)` takes the time step from you, e.g. for tests.

## Prefs

```csharp
Prefs.SetBool("music", true);
Prefs.SetLong("coins", 9000000000L);
Prefs.SetEnum("quality", Quality.High);
Prefs.SetDateTime("lastReward", DateTime.UtcNow);
Prefs.SetObject("settings", settings);               // [Serializable] class, stored as JSON

Settings loaded = Prefs.GetObject("settings", new Settings());
```

A missing key or unreadable value returns the default. Enums are stored by name. PlayerPrefs isn't encrypted, so don't store purchases in it.

## Animation helpers

```csharp
yield return animator.PlayAndWait("Open");   // plays the state from the start and waits for it to finish once
yield return director.PlayAndWait();         // plays a Timeline and waits until it stops (or reaches the end with Hold)
```

- `PlayAndWait` respects the Animator's speed and Update Mode, and stops waiting if the animator leaves the state or is disabled. A missing state logs a warning instead of waiting forever.
- `AutoRotate` spins an object at a constant speed in degrees per second, around any axis, optionally with unscaled time.

## Sample

**Package Manager > Utility > Samples > UI Demo** imports a scene with a snapping carousel (page dots and arrows), a hold button with a fill bar wired in the Inspector, a safe area panel, an internet status line, number abbreviation and a countdown.

## Tests

EditMode and PlayMode tests live in `Tests/`. They show up in the Test Runner when the package is in `Assets/`, or when it's listed under `testables` in a project's manifest.
