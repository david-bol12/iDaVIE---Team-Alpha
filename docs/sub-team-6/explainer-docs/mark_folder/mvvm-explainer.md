# MVVM — In-Depth Explainer

## What MVVM is, from first principles

MVVM stands for Model–View–ViewModel. It is a pattern for separating the visual presentation of an application from its logic and data.

- **Model** — the data and the rules that govern it. In iDaVIE's case the authoritative model lives server-side (FITS data, domain logic, native computations). On the client it appears only as immutable DTOs like `FitsFileInfo` and `LogEntry`.
- **View** — the part the user sees and interacts with. Buttons, text fields, sliders. In Unity this is `MonoBehaviour` components and Unity UI elements. The View has no logic — it only displays what the ViewModel tells it and reports user actions back.
- **ViewModel** — the decision-making layer. It owns state, validation, commands, and application behaviour. It is pure C# with no Unity dependency.

**The core rule:** the ViewModel never references the View, and the View never references the ViewModel directly — it binds to the ViewModel *interface*. This one-way dependency is what makes ViewModels testable without Unity.

---

## Why this matters for iDaVIE specifically

`CanvassDesktop.cs` is 1,899 lines of `MonoBehaviour` that mixes all three of these concerns in one class. The result:

- You cannot test any business logic without starting Unity, because everything is inside `MonoBehaviour`.
- You cannot change the UI framework (from Canvas to UI Toolkit) without rewriting the business logic at the same time, because they are fused.
- The class knows about 47 different types (CBO=47). Change any of them and you have to re-analyse the whole class.

MVVM solves all three problems by pulling the logic out into separate classes that have no knowledge of Unity.

---

## The three assembly split

The MVVM layers map to three C# assemblies. Assembly boundaries are not just folder organisation — they are compile-time enforcement.

| Assembly | Contents | Unity allowed? |
|---|---|---|
| `iDaVIE.Client.View` | UIDocument, Unity event wiring, MonoBehaviours | Yes |
| `iDaVIE.Client.ViewModel` | Observable properties, ICommand, validation | **No** |
| `iDaVIE.Client.Gateway` | IServiceGateway adapters, transport, ACL | Adapter layer only |

The dependency direction is:
```
View → ViewModel → Gateway
```

Reverse references fail CI (NDepend). Because the ViewModel assembly has no Unity package reference, a `using UnityEngine` in a ViewModel will not compile.

---

## Where the Model lives (the question that trips people up)

This is a **client–server MVVM**. There is no client Model assembly. The authoritative domain model lives **server-side** — Sub-team 1's kernel plus native plugins.

On the client the Model appears only as **immutable DTOs** returned through the Gateway:
- `FitsFileInfo` — file metadata from `file.open` / `dataset.getAxes`
- `LogEntry` — a structured log record from a `log.emit` notification
- `HduInfo` — HDU table entries

**The Gateway is not the Model.** It is the pipe. The DTOs flowing through it are the Model.

Today `CanvassDesktop` P/Invokes CFITSIO directly and holds the cube in a managed `float[]` on the main thread. That is why loading a large file freezes the UI (B-08) — the domain model is wrongly on the client. The refactor moves it server-side. The client keeps only DTOs.

---

## The four binding mechanics (the actual policy)

### 1. `INotifyPropertyChanged` — how the ViewModel tells the View to update

Every observable ViewModel property uses `PropertyChanged`. When the ViewModel changes a value, it fires the event. The View listens and updates itself. The ViewModel never touches the View directly.

Example: when `FileTabViewModel.IsLoadable` changes from `false` to `true`, it fires `PropertyChanged("IsLoadable")`. The View's binding on the Load button reads the new value and enables the button. The ViewModel does not know a button exists.

Rules:
- No bare public fields — always a property with notification.
- No side-effects in getters — getters must be pure.
- No heavy work in setters — setters notify and return; heavy work goes in a command.

### 2. `ICommand` — how the View calls the ViewModel

The View invokes ViewModel behaviour **only** through commands. Never `viewModel.DoTheThing()`.

In the file-tab worked example:
- `BrowseImageCommand` and `LoadCommand` are `ICommand` properties on `IFileTabViewModel`.
- The methods they call (`BrowseImageAsync`, `LoadAsync`) are **private** on `FileTabViewModel`.
- So `viewModel.LoadAsync()` won't compile — the only way in is `LoadCommand.ExecuteAsync()`.

Why this earns its keep:
- **Testability:** `await vm.LoadCommand.ExecuteAsync()` runs against stubs with no Unity.
- **Auto-gating:** `CanExecute` / `CanExecuteChanged` drives button enabled-state. The View makes no decisions about when the button is enabled — the ViewModel does.
- **Uniformity:** every action is either a sync `ICommand` or an async `IAsyncCommand`. Nothing special to learn per tab.

The rule governs **behaviour** only. Data flows via property get/set + `PropertyChanged`. Behaviour flows via `ICommand`.

### 3. `ObservableCollection<T>` — how the ViewModel exposes lists

For lists that the View displays (HDU options, log entries, sources), the ViewModel exposes an `ObservableCollection<T>` or an `IReadOnlyList<T>`. Mutations happen in place — never replace the collection instance, because the View's binding is to the object reference. Bulk loads happen atomically by populating a new list and then swapping once.

### 4. Threading — `IUIDispatcher`

Gateway callbacks arrive on the gateway's read-loop thread. Unity UI can only be touched from the main thread. If a ViewModel property is mutated on the wrong thread and the View's binding fires, Unity will throw an exception.

The fix is an injected `IUIDispatcher` — a lightweight interface with one method: `RunOnMainThread(Action)`. The concrete implementation (`UnityUIDispatcher`) wraps Unity's main-thread context. The ViewModel calls `_dispatcher.RunOnMainThread(() => AppendEntry(entry))` rather than touching the list directly from the callback thread.

**Critically:** the forbidden pattern is `UnityMainThreadDispatcher.Instance` — a static singleton. That is the same architectural smell as `Application.logMessageReceived`. The `IUIDispatcher` interface is injectable and testable.

---

## How a click flows end-to-end — File tab

This is the concrete trace from user action to server response.

```
User clicks Browse button
  → FileTabView.OnBrowseClicked()          [View forwards the event]
  → BrowseImageCommand.ExecuteAsync()      [View → VM via ICommand only]
  → IFileDialogService.PickFileAsync()     [VM calls interface; Unity adapter shows the dialog]
  → returns path string
  → IFitsService.OpenImageAsync(path)      [VM calls interface; gateway proxy sends file.open over JSON-RPC]
  → server loads FITS, returns FitsFileInfo DTO
  → VM updates HduOptions / IsLoadable / HeaderText   [each setter fires PropertyChanged]
  → View re-renders; Load button enables via CanExecuteChanged
```

Directionality:
- **View → VM:** behaviour via commands; data via property setters.
- **VM → View:** only via `PropertyChanged` (VM never references the View).
- **VM → Gateway:** only via injected interfaces, wired in `FileTabCompositionRoot`.
- **Gateway → Server:** JSON-RPC (`FitsServiceAdapter` owns the `file.*` / `dataset.*` method names).

The ViewModel is a **coordinator over interfaces**. That is why it unit-tests with no Unity, no OS dialog, no server.

---

## How a log entry flows end-to-end — Debug tab

The Debug tab uses the GoF Observer pattern, not the command pattern. The flow is push rather than pull.

```
Server pushes log.emit notification on the named pipe
  → IServiceGateway.OnNotification fires
  → GatewayLogStreamAdapter.OnGatewayNotification()   [filters for log.emit, deserialises JSON]
  → _inner.Publish(level, msg, timestamp)              [into the LogStream]
  → LogStream.Publish()                                [snapshot observers, iterate outside lock]
  → DebugTabViewModel.OnNext(LogEntry)                 [implements ILogObserver]
  → AppendEntry(entry) — adds to List<LogEntry>, caps at 2000, fires EntriesChanged
  → DebugTabView.OnEntriesChanged()                    [View handler rebuilds TMP text, last 500 entries]
  → if AutoScrollEnabled: scroll to bottom
```

This path exercises the **server-push** half of the transport. File tab exercises request/response. Together the two worked examples prove the gateway contract has real consumers on both paths.

---

## Owns / does NOT own — the SRP cut line

The rule: **the ViewModel owns state and decisions (pure C#); it does not own mechanisms that touch Unity, the OS, or native code.**

Those mechanisms cross the boundary:

| Current (CanvassDesktop) | Proposed home |
|---|---|
| `Transform.Find(...)` wiring | View (replaced by binding) |
| `StandaloneFileBrowser` call | Behind `IFileDialogService` (Unity adapter) |
| `FitsReader` P/Invoke | Behind `IFitsService` (→ server via JSON-RPC) |
| `Application.logMessageReceived` subscription | `GatewayLogStreamAdapter` (one class, ACL boundary) |
| `PlayerPrefs.GetString(...)` | Behind `IConfigService` (Unity adapter) |

These five "does NOT own" items are exactly the five coupling smells of the current `CanvassDesktop` (scene-graph, OS dialog, native interop, static event hook, global state).

---

## The composition root — how the object graph is wired

`CanvassDesktopShell` is the composition root. It is the only class allowed to reference both domain concrete types and Unity adapter types at once. It does nothing except wire the graph at startup.

```csharp
// Inside CanvassDesktopShell.Awake()
var gateway = new JsonRpcServiceGateway(pipe);
var fitsAdapter = new FitsServiceAdapter(gateway);
var dialogAdapter = new FileDialogServiceAdapter();
var vm = new FileTabViewModel(fitsAdapter, dialogAdapter, ...);
_fileTabView.BindTo(vm);
```

No `FindObjectOfType`. No `transform.Find`. No logic. Just construction and wiring.

`OnDestroy` disposes the VMs in reverse construction order, unsubscribing observers and closing connections symmetrically.

---

## Rejected alternatives

| Pattern | Why rejected |
|---|---|
| **MVP** | Presenter holds a direct reference to the View interface, making View replacement harder. UI Toolkit's binding model is built around observable properties — MVVM is the natural fit. |
| **MVU / Elm-style** | Elegant but mismatches UI Toolkit's two-way binding primitives. Adds unfamiliarity risk. |
| **Classical MVC** | Controller and View conflate in Unity practice — the MonoBehaviour ends up being both. Drawing a clean ACL boundary is harder when the controller still holds scene references. |
| **Reactive MVVM (Rx/UniRx)** | Compelling for the Debug tab's push model, but adds a library dependency and steep learning curve. The `ILogStream` event model is compatible with an Rx migration later without changing the ViewModel interface. |
| **Partial classes** | Still one class with Unity and non-Unity code mixed. Improves file organisation, not architecture. |
| **Unity Test Framework only** | Adds tests around tightly coupled code but does not solve the underlying problem. |

---

## CI enforcement

The architecture is not held together by convention — it is enforced at build time.

**NDepend CQLinq rule:**
```
from t in Application.Types
where t.IsInNamespace("iDaVIE.Client.ViewModel")
  && t.IsUsing("UnityEngine")
select new { t, Reason = "ViewModel must not reference UnityEngine" }
```
Any ViewModel that accidentally imports Unity types fails the build on every pull request.

**`dotnet build` on the skeleton projects:**
The ViewModel and domain assemblies are standalone `.csproj` files with no Unity package reference. If the code compiles — it is Unity-free. This is the strongest possible enforcement: not a linter warning, a compile error.

---

## Self-test — questions the panel will ask

- **"Is the Gateway the Model?"** — No. The Gateway is the pipe. The authoritative Model is server-side. On the client the Model appears as immutable DTOs flowing through the Gateway.
- **"Where does the Model live, then?"** — Server-side kernel + native plugins. Today it is wrongly on the client (cause of B-02, B-08). The refactor moves it.
- **"Is your ViewModel an interface?"** — No. `FileTabViewModel` is the concrete class. `IFileTabViewModel` is the boundary interface it implements (required by §4.2.4). `I` = interface convention.
- **"How does the View call the ViewModel?"** — Only via `ICommand`. Command bodies are private, so direct calls don't compile.
- **"Isn't the Update() loop MVU?"** — No. Unity `Update()` is a per-frame engine callback. MVU's `update` is a pure function over messages. We use event-driven MVVM and explicitly rejected MVU.
- **"How is 'no Unity in the ViewModel' enforced?"** — Assembly boundary with no Unity package reference, plus NDepend CQLinq on every PR. Build fails, not review comment.
- **"What does the Gateway actually do on the wire?"** — JSON-RPC 2.0 over named pipes. `FitsServiceAdapter` maps `OpenImageAsync` → `file.open` + `dataset.getAxes`. `GatewayLogStreamAdapter` subscribes to `log.emit` push notifications and republishes them as `ILogStream.Publish` calls.
- **"Did you implement all six tabs?"** — File and Debug fully (tested, CK-measured skeletons). Render, Stats, Sources, and Paint are design gestures — interfaces named, split-from-current-code tables done, no compiled skeleton.
- **"Show one defect fixed end-to-end."** — B-02: Debug tab crash during load. Cause: `Application.logMessageReceived` fires while the main thread is blocked in CFITSIO. Fix: Debug becomes an Observer of `ILogStream` marshalled via `IUIDispatcher`. No main-thread block possible.

- **"Analogy for ICommand."**
Think of it like a TV remote button.

The button (View) doesn't know how the TV changes channel. It just sends a signal.

The TV internals (ViewModel) receive the signal and do the work. They also decide whether the button should be lit up or greyed out.

ICommand is the signal contract between them — it says:

"here's how you trigger me" (Execute)
"here's whether I'm currently allowed" (CanExecute)
That's it. The button doesn't need to know what happens inside the TV, and the TV doesn't need to know a button exists.

