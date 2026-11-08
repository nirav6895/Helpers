# Changelog

## [1.0.0] - 2026-11-08

First tagged release. It has breaking changes from the untagged initial import, which no project used yet.
Renamed scripts keep their file GUIDs, so components already in scenes stay connected.

### Changed
- Everything is in the `NG.Utility` namespace (editor code in `NG.Utility.Editor`). The old namespaces `NG.Utilities`, `NG.Utility.Animation`, `NG.Utility.Attribute`, `NG.Utility.Internet` and `NG.Utility.ScrollRectExtention` hid `UnityEngine.Animation`, `System.Attribute` and the `Internet` class, and `AbbrevationUtility` had no namespace.
- The package has its own assemblies: `NG.Utility` and the Editor-only `NG.Utility.Editor`. Editor scripts moved from `Runtime/.../Editor` to `Editor/`.
- **AbbrevationUtility** is now **NumberAbbreviation**: one `Abbreviate` method (plus `ToAbbreviatedString()` extensions for int, long, float and double) with `maxDecimals`, `abbreviateFrom` and culture parameters, replacing `AbbreviateNumber` and `AbbreviateNumberFromK`. For the old `AbbreviateNumber`, pass `abbreviateFrom: 1000000`. Output uses the invariant culture by default.
- **RotateObject** is now **AutoRotate**. Speed is in degrees per second (it was degrees per frame), around any axis, in Self or World space.
- **TimelineExtensions** is now **PlayableDirectorExtensions**.
- **Internet** is now **InternetMonitor**: see Added.
- **EnumArrayAttribute** is now **LabelFromFieldAttribute**, and works with enum, string, number, bool and object reference fields.
- **ShowEnumElementsAttribute** is now **LabelFromEnumAttribute**.
- **ScrollRectSnapArrowButton** is now **ScrollSnapPageButton**, with Previous/Next instead of Left/Right and an **At End** setting (None, Disable, Hide) instead of *Disable Button At End Page*. The `Direction` enum moved inside it as `PageDirection`.
- **ScrollRectSnap** must be on the ScrollRect's GameObject (it uses that ScrollRect). Pages are the content's active children by default; **Page Count** splits the content into equal pages instead. Page dots moved to the new `PageIndicator`. *Initialize On Start* is removed: it initializes itself when first used. The threshold is now a fraction of a page.
- **HoldButton** events are renamed: `OnStartHold` → `HoldStarted`, `OnCompleteHold` → `HoldCompleted`, `OnReleaseHold` → `HoldCanceled`. *Require Hold Time* is now *Hold Duration* (existing values are kept).

### Added
- **InternetMonitor**: checks the connection with a real request (Check Url, Expected Response Code, Timeout) every Check Interval, raises `StatusChanged`, checks again when the app resumes, and has `SimulateOffline` with a Play Mode toggle in its Inspector.
- **HoldButton**: `HoldProgress` and On Hold Progress (connect it to an Image's fillAmount), UnityEvents for every hold event, Suppress Click After Hold, Repeat While Held, unscaled time.
- **ScrollRectSnap**: vertical scrolling, flick detection, `GoToPage`, `Next`, `Previous`, `Refresh`, `CurrentPage`, `PageChanged` (C# event and UnityEvent), Start Page, unscaled time, re-snapping when the screen size changes.
- **PageIndicator**: page dots with active/inactive colors and sprites.
- **SafeArea**: fits a RectTransform to `Screen.safeArea`, per edge.
- **EnumArray&lt;TEnum, T&gt;**: one serialized value per enum member, kept in sync when the enum changes.
- **SerializableDictionary&lt;TKey, TValue&gt;**: a dictionary in the Inspector, with duplicate keys highlighted.
- **ReadOnlyFieldAttribute**: shows a field without letting it be edited.
- **TimeFormatting**: `ToClock` ("02:15") and `ToCompact` ("1d 4h"), with round-up for countdowns and custom unit labels.
- **CountdownTimer**: pausable, repeatable countdown in plain C#.
- **Prefs**: PlayerPrefs for bool, long, double, enums, DateTime and objects as JSON.
- **SecurePrefs**: encrypted PlayerPrefs (AES-256 + HMAC-SHA256, hashed key names) with tamper detection, optional device binding and migration of existing plain values.
- **PinnedCertificateHandler**: certificate pinning for UnityWebRequest with standard SHA-256 public key pins, plus a *Certificate Pin Finder* window under Tools > NG Utility.
- Extension methods: `GetOrAddComponent`, `DestroyChildren`, `Vector2/Vector3.With`, `Color.WithAlpha`, `CanvasGroup.SetVisible`, `Shuffle`, `GetRandom`.
- `AnimatorExtensions.PlayAndWait` overload that takes a state hash.
- *UI Demo* sample, README, EditMode and PlayMode tests.

### Removed
- **AnimationUtility**: it only had a linear float lerp, its name clashed with `UnityEditor.AnimationUtility`, and tween libraries (PrimeTween, DOTween, LitMotion) do this better.

### Fixed
- **AbbrevationUtility** (now NumberAbbreviation): `AbbreviateNumber` returned "2500M" for 2.5 billion and dropped decimals ("1M" for 1.5 million). Both methods returned "1000K" for 999,999 and stopped at B.
- **AnimatorExtensions.PlayAndWait**: a state name that doesn't exist waited forever. The wait used scaled time, so it never ended for an Unscaled Time animator while the game was paused, and it ignored `Animator.speed`.
- **TimelineExtensions.PlayAndWait** (now PlayableDirectorExtensions): waited forever with Wrap Mode Hold, and pausing the director ended the wait.
- **RotateObject** (now AutoRotate): rotation speed depended on the frame rate.
- **Internet** (now InternetMonitor): the status change event was commented out, so it never notified anyone, and reachability alone counts Wi-Fi without internet as online.
- **ShowEnumElements** (now LabelFromEnum): enums not numbered 0, 1, 2... got the wrong labels.
- **EnumArray** and **ShowEnumElements** drawers (now LabelFromField and LabelFromEnum): exceptions were swallowed, including the one Unity uses when an object picker opens, which broke the Inspector's layout.
- **ScrollRectSnap**: did nothing when placed on another GameObject than the ScrollRect. Grabbing the content while it snapped made it fight the finger, and ScrollRect inertia fought the snap. One page set the position to NaN. Using it before it initialized threw a NullReferenceException. Lowering the page count below the current page left it on a missing page. It didn't snap while the game was paused.
- **ScrollRectSnapArrowButton** (now ScrollSnapPageButton): its state wasn't set until the first page change, and hiding only hid the background image.
- **HoldButton**: right and middle clicks started a hold. Holds never completed while the game was paused. A CanvasGroup with Interactable off didn't stop a hold. Disabling the button mid-hold left it holding.
- **HoldButtonEditor** compiled into the runtime code path instead of an Editor assembly.
