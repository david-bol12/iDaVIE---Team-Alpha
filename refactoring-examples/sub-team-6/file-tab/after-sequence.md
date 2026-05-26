# File tab — AFTER sequence diagram (Mermaid)

## TL;DR

Mermaid rendering of `after-trace.md`. ACL boundary drawn as a `box` around the Unity-side adapters (`FileDialogAdapter`, `FitsAdapter`, `VolumeAdapter`, `VCC`) — the `FileTabVM` lifeline never enters that box without crossing an interface. Every BEFORE `→ DLL` and `→ VCC` arrow collapses into a single `→ interface` message. `activate` bars split cleanly: VM during command execution, Vol during the load coroutine. Two `⚠` annotations mark the contained smells (field writes, busy-wait) — honestly drawn rather than hidden. Final `Vol → Peers: CubeLoaded(DTO)` arrow replaces the 13-step `postLoadFileFileSystem` cross-tab cascade.

---

Mermaid rendering of [`after-trace.md`](after-trace.md). Pair side-by-side with [`before-sequence.md`](before-sequence.md) on the panel slide: every `→ DLL` and `→ VCC` arrow in the BEFORE is replaced by a single `→ interface` message here.

The ACL boundary is drawn as a `box` around the Unity-side adapters. The `FileTabVM` lifeline never sends a message into the box without going through one of the three domain interfaces.

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant View as FileTabView<br/>(Unity MonoBehaviour, thin)
    participant VM as FileTabVM<br/>(pure C#, no UnityEngine ref)

    box rgb(245, 230, 230) ACL boundary — Unity assembly (adapters)
    participant Dialog as FileDialogAdapter<br/>(SFB + PlayerPrefs)
    participant Fits as FitsAdapter<br/>(FitsReader P/Invoke)
    participant Vol as VolumeAdapter<br/>(coroutine + prefab + VCC)
    participant VCC as VolumeCommandController
    end
    participant Peers as Peer-tab VMs<br/>(Rendering / Stats / Sources / Paint)

    %% ═══════════════════════════════════════════════════════════════════
    %% PHASE A — Pick a file (metadata read, validation, Load enabled)
    %% ═══════════════════════════════════════════════════════════════════
    rect rgb(235, 245, 230)
    Note over User,VCC: PHASE A — Pick file, validate, enable Load command
    User->>View: click "Browse Image"
    View->>VM: BrowseImageCommand.ExecuteAsync()<br/>[code-side subscription]
    activate VM

    VM->>Dialog: IFileDialogService.PickFileAsync(<br/>"Select FITS image", "", ["fits","fit"])
    activate Dialog
    Dialog->>Dialog: PlayerPrefs.GetString("LastPath")<br/>[Unity-side state]
    Dialog-->>User: native OS file picker
    User->>Dialog: select *.fits
    Dialog->>Dialog: PlayerPrefs.SetString("LastPath", ...)
    Dialog-->>VM: Task⟨string?⟩ → path
    deactivate Dialog

    VM->>VM: IsLoading = true —<br/>PropertyChanged → spinner on

    VM->>Fits: IFitsService.OpenImageAsync(path)
    activate Fits
    Note right of Fits: Task.Run( ReadFitsMetadata )<br/>off the Unity main thread
    Fits->>Fits: try {<br/>  FitsOpenFile → fptr<br/>  FitsGetHduCount<br/>  loop FitsMovabsHdu + FitsReadKey<br/>  ExtractHeaders<br/>} finally {<br/>  FitsCloseFile(fptr)  ← RAII<br/>}
    Fits-->>VM: FitsFileInfo DTO<br/>{FilePath, HduList, NAxis, AxisSizes, HeaderText}
    deactivate Fits
    Note right of VM: no IntPtr crosses ACL —<br/>only the immutable DTO

    VM->>VM: ImagePath = path<br/>HduOptions.AddRange(info.HduList)<br/>HeaderText = info.HeaderText<br/>Subset.ResetToAxisMaxima(...)<br/>PopulateZAxisOptions(info)<br/>NotifyIsLoadable()
    Note right of VM: pure C# state mutation —<br/>PropertyChanged drives the View

    VM->>VM: IsLoadable get → axis-count rule<br/>ValidationMessage = null
    VM->>VM: IsLoading = false<br/>CanExecuteChanged on LoadCommand
    deactivate VM

    VM-->>View: PropertyChanged: ImagePath, HduOptions,<br/>HeaderText, SubsetEnabled, IsLoadable, IsLoading
    View-->>User: HDU dropdown populated —<br/>header shown — Load button enabled
    end

    Note over User,VCC: validation passed — Load enabled<br/>(no separate "open" round-trip required)

    %% ═══════════════════════════════════════════════════════════════════
    %% PHASE B — Confirm load (cube becomes visible)
    %% ═══════════════════════════════════════════════════════════════════
    rect rgb(230, 240, 250)
    Note over User,VCC: PHASE B — Single Task — no coroutine in VM, no busy-wait in VM
    User->>View: click "Load"
    View->>VM: LoadCommand.ExecuteAsync()
    activate VM

    VM->>VM: build LoadCubeRequest {<br/>  ImagePath, MaskPath, HduIndex,<br/>  Subset?, ZAxisSelection<br/>}
    VM->>Vol: IVolumeService.LoadCubeAsync(<br/>request, IProgress⟨float⟩)
    activate Vol
    Note right of Vol: TaskCompletionSource⟨bool⟩ +<br/>StartCoroutine(LoadCubeCoroutine)<br/>— contained inside the adapter

    opt previous cube exists
        Vol->>VCC: RemoveDataSet(existing)
        Vol->>Vol: InputController SetActive(false/true)<br/>DisablePaintMode / endThresholdEditing /<br/>endZAxisEditing / CleanUp / Destroy
    end
    Vol-->>VM: progress.Report(0.2f)

    Vol->>Vol: Instantiate(_cubePrefab)<br/>renderer = newCube.GetComponent⟨VDSR⟩()<br/>renderer.FileName / MaskFileName /<br/>SelectedHdu / CubeDepthAxis /<br/>subsetBounds  ← contained smell
    Note right of Vol: ⚠ field writes still happen<br/>— but only inside the adapter
    Vol-->>VM: progress.Report(0.4f)

    Vol->>VCC: AddDataSet(renderer)
    Vol->>Vol: StartCoroutine(renderer._startFunc())

    loop while !renderer.started
        Vol->>Vol: yield return WaitForSeconds(0.1f)
    end
    Note right of Vol: ⚠ busy-wait still here<br/>— contained, returned as Task

    Vol-->>VM: tcs.TrySetResult(true) —<br/>progress.Report(1f)

    Vol->>Peers: CubeLoaded(CubeLoadedEventArgs)<br/>{ImagePath, MaskPath, HduIndex}
    Note right of Peers: Peer VMs rebind own state.<br/>★ Replaces postLoadFileFileSystem cascade<br/>and closes Anomaly #8 rest-freq leak —<br/>each peer owns its subscribe/unsubscribe.
    deactivate Vol

    VM->>VM: IsLoading = false<br/>CanExecuteChanged
    deactivate VM

    VM-->>View: PropertyChanged: IsLoading
    View-->>User: spinner hidden — cube visible
    end
```

---

## Side-by-side reading guide

Suggested slide layout for the panel:

| BEFORE callout | AFTER replacement |
|---|---|
| `CD → FR → DLL` triangle on every read | One `VM → Fits` arrow returning a DTO |
| `CD → VCC` direct singleton calls | `VM → Vol → VCC` — VCC reached only via adapter |
| `transform.Find` self-message | `PropertyChanged` event — no self-mutation visible |
| Two `activate` bars on `CanvassDesktop` (callback + coroutine) | `activate` bar on `VM` for the *command*, separate `activate` on `Vol` for the *coroutine* — lifelines split |
| `★` smell annotations on the arrows themselves | `⚠` annotations on `Note right of Vol` — smells acknowledged, contained, not eliminated |
| Phase A→B separator: "must click a second button" | Phase A→B separator: "Load enabled — single click possible" |
| `postLoadFileFileSystem` 13-step self-cascade into other tabs | One `Vol → Peers: CubeLoaded(DTO)` arrow — peer tabs subscribe themselves |

## Mapping of contained smells (honest about what remains)

The two `⚠` annotations in the diagram correspond to items in `after-trace.md` → *Known limitations*:

| Diagram marker | Smell ID | Adapter location | Fix vector |
|---|---|---|---|
| `⚠ field writes still happen` | S5 | `VolumeServiceAdapter.cs:115-124` | When Sub-team 3 introduces `IRendererCommand`, swap the field writes for a command emit. The `FileTabViewModel` does not change. |
| `⚠ busy-wait still here` | S6 | `VolumeServiceAdapter.cs:147-148` | When `VolumeDataSetRenderer` exposes a readiness `event` or `Task`, the loop becomes `await renderer.WhenStarted()`. The VM's `await _volumeService.LoadCubeAsync(...)` is unchanged. |

Both fixes are pure adapter-side edits — none of the 27 file-tab unit tests need to change.
