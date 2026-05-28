# File tab — AFTER sequence diagram (Mermaid)

## TL;DR

Mermaid rendering of `after-trace.md`, updated for the gateway rewire (ADR-009 / ADR-0002). Phase A's FITS reads no longer cross the Unity ↔ native boundary client-side — `FitsServiceAdapter` is now a **gateway proxy** that dispatches `file.open` and `dataset.getAxes` over JSON-RPC to the server kernel. No `IntPtr` ever exists on the client. The opaque `IFitsHandle` wraps a server-assigned `datasetId`; `Dispose()` fires a best-effort `file.close`. Phase B (cube load) is unchanged — the volume renderer is genuinely client-local and `VolumeServiceAdapter` remains a Unity adapter. The busy-wait elimination (S6) and the contained field-write smell (S5) carry over from the prior version.

---

Mermaid rendering of [`after-trace.md`](after-trace.md). Pair side-by-side with [`before-sequence.md`](before-sequence.md) on the panel slide: every BEFORE `CD → FR → DLL` arrow is replaced by an `IFitsService` call that **the adapter now forwards through `IServiceGateway` to the server**.

The diagram has two grouped lifelines: the **Gateway Layer (ACL)** on the client side (`iDaVIE.Client.Gateway` per D3 §2.1), and the **server-side kernel** beyond the named pipe.

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant View as FileTabView<br/>(Unity MonoBehaviour, thin)
    participant VM as FileTabVM<br/>(pure C#, no UnityEngine ref)

    box rgb(245, 230, 230) Gateway Layer (ACL) — iDaVIE.Client.Gateway
    participant Dialog as FileDialogAdapter<br/>(Unity adapter: SFB + PlayerPrefs)
    participant Fits as FitsServiceAdapter<br/>(gateway proxy: no Unity, no P/Invoke)
    participant Vol as VolumeServiceAdapter<br/>(Unity adapter: coroutine + prefab + VCC)
    participant VCC as VolumeCommandController
    participant Gw as JsonRpcServiceGateway<br/>(IServiceGateway, named pipe)
    end
    participant Server as iDaVIE Server<br/>(Sub-team 1 kernel + native FITS plug-in)
    participant Peers as Peer-tab VMs<br/>(Rendering / Stats / Sources / Paint)

    %% ═══════════════════════════════════════════════════════════════════
    %% PHASE A — Pick a file (metadata read, validation, Load enabled)
    %% ═══════════════════════════════════════════════════════════════════
    rect rgb(235, 245, 230)
    Note over User,Server: PHASE A — Pick file, fetch metadata via gateway, enable Load command
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
    Note right of Fits: Two-call protocol per<br/>ADR-0002 §"Method catalogue (v1)".<br/>No P/Invoke, no IntPtr.

    Fits->>Gw: SendAsync⟨FileOpenResult⟩(<br/>"file.open", { path, isMask: false })
    activate Gw
    Gw->>Server: { jsonrpc:"2.0", id:N, method:"file.open",<br/>params:{ path, isMask:false } }<br/>[length-prefixed JSON-RPC frame]
    Server->>Server: load native FITS plug-in,<br/>allocate dataset, assign id
    Server-->>Gw: { jsonrpc:"2.0", id:N,<br/>result:{ datasetId:"ds-3f1c" } }
    Gw-->>Fits: FileOpenResult { DatasetId }

    Fits->>Gw: SendAsync⟨DatasetAxesResult⟩(<br/>"dataset.getAxes", { datasetId })
    Gw->>Server: { method:"dataset.getAxes",<br/>params:{ datasetId:"ds-3f1c" } }
    Server-->>Gw: { result:{ hduList, nAxis,<br/>axisSizes, headerText, estimatedBytes } }
    Gw-->>Fits: DatasetAxesResult
    deactivate Gw

    Fits->>Fits: assemble FitsFileInfo {<br/>  Handle = RemoteFitsHandle(gateway, "ds-3f1c", path),<br/>  HduList, NAxis, AxisSizes,<br/>  HeaderText, EstimatedBytes<br/>}
    Fits-->>VM: FitsFileInfo DTO
    deactivate Fits
    Note right of VM: No native pointer ever existed<br/>on the client. The handle wraps<br/>an opaque server-assigned id.

    VM->>VM: ImagePath = path<br/>HduOptions.AddRange(info.HduList)<br/>HeaderText = info.HeaderText<br/>Subset.ResetToAxisMaxima(...)<br/>PopulateZAxisOptions(info)<br/>NotifyIsLoadable()
    Note right of VM: pure C# state mutation —<br/>PropertyChanged drives the View

    VM->>VM: IsLoadable get → axis-count rule<br/>ValidationMessage = null
    VM->>VM: IsLoading = false<br/>CanExecuteChanged on LoadCommand
    deactivate VM

    VM-->>View: PropertyChanged: ImagePath, HduOptions,<br/>HeaderText, SubsetEnabled, IsLoadable, IsLoading
    View-->>User: HDU dropdown populated —<br/>header shown — Load button enabled
    end

    Note over User,Server: validation passed — Load enabled<br/>HDU switches now call dataset.getHeader<br/>(no client-side file reopen)

    %% ═══════════════════════════════════════════════════════════════════
    %% PHASE B — Confirm load (cube becomes visible)
    %% ═══════════════════════════════════════════════════════════════════
    rect rgb(230, 240, 250)
    Note over User,Server: PHASE B — Render path stays client-side<br/>(VolumeServiceAdapter is a Unity adapter)
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
    Vol->>Vol: yield return StartCoroutine(renderer._startFunc())
    Note right of Vol: ★ event-driven — coroutine suspends<br/>until _startFunc completes.<br/>BEFORE busy-wait eliminated.

    Vol-->>VM: tcs.TrySetResult(true) —<br/>progress.Report(1f)

    Vol->>Peers: CubeLoaded(CubeLoadedEventArgs)<br/>{ImagePath, MaskPath, HduIndex}
    Note right of Peers: Peer VMs rebind own state.<br/>★ Replaces postLoadFileFileSystem cascade.
    deactivate Vol

    VM->>VM: IsLoading = false<br/>CanExecuteChanged
    deactivate VM

    VM-->>View: PropertyChanged: IsLoading
    View-->>User: spinner hidden — cube visible
    end

    %% ═══════════════════════════════════════════════════════════════════
    %% PHASE C — Replacing or closing the file (handle lifetime)
    %% ═══════════════════════════════════════════════════════════════════
    rect rgb(245, 245, 230)
    Note over User,Server: PHASE C — Handle disposal fires file.close<br/>(best-effort, fire-and-forget)
    VM->>Fits: previous info.Handle.Dispose()<br/>(VM swaps in a new FitsFileInfo)
    Fits->>Gw: SendAsync⟨object?⟩("file.close",<br/>{ datasetId:"ds-3f1c" })<br/>[no await — observed in continuation]
    Gw->>Server: { method:"file.close",<br/>params:{ datasetId:"ds-3f1c" } }
    Server-->>Gw: { result: null }
    end
```

---

## Side-by-side reading guide

Suggested slide layout for the panel:

| BEFORE callout | AFTER replacement |
|---|---|
| `CD → FR → DLL` triangle on every FITS read | `VM → Fits → Gw → Server` — adapter dispatches `file.open` + `dataset.getAxes`; server runs the native plug-in |
| Native `IntPtr` held by `CanvassDesktop` field across coroutines | Opaque `IFitsHandle` wrapping a server-assigned `datasetId` — no native pointer on the client |
| `ChangeHduSelection` (line 1435) reopens the file on every dropdown change | Single durable handle; HDU switches issue `dataset.getHeader` against the same `datasetId` |
| `CD → VCC` direct singleton calls | `VM → Vol → VCC` — VCC reached only via adapter (Phase B unchanged from prior diagram) |
| `transform.Find` self-message | `PropertyChanged` event — no self-mutation visible |
| Two `activate` bars on `CanvassDesktop` (callback + coroutine) | `activate` bar on `VM` for the command, separate `activate` on `Vol` for the coroutine, separate `activate` on `Gw` for the round-trip — lifelines split |
| `★` smell annotations on the arrows themselves | One `⚠` annotation on `Note right of Vol` (S5 field writes — contained); BEFORE busy-wait replaced by coroutine suspension (S6 eliminated); BEFORE FITS P/Invoke eliminated (server-owned) |
| `postLoadFileFileSystem` 13-step self-cascade into other tabs | One `Vol → Peers: CubeLoaded(DTO)` arrow — peer tabs subscribe themselves |

## Mapping of contained smells (honest about what remains)

After the gateway rewire (ADR-009 / ADR-0002), several smells previously *contained* inside `FitsServiceAdapter` are now **eliminated** — they are server-side concerns, not client-side ACL ones.

| Smell ID | Status now | Where it lives |
|---|---|---|
| **S5** field writes onto `VolumeDataSetRenderer` (`renderer.FileName = ...`) | ⚠ contained inside `VolumeServiceAdapter.cs` | Phase B, `Note right of Vol`. Fix vector: when Sub-team 3 introduces `IRendererCommand`, swap field writes for a command emit. ViewModel and 34 unit tests do not change. |
| **S6** busy-wait on `renderer.started` flag | ✓ eliminated | Replaced by `yield return StartCoroutine(renderer._startFunc())` coroutine suspension. |
| **FITS P/Invoke leakage** (BEFORE `FitsOpenFile` / `FitsGetHduCount` / `FitsMovabsHdu` etc. on the client) | ✓ eliminated client-side | Server-side native plug-in (Sub-team 1 kernel). Client speaks only JSON-RPC. |
| **Native `IntPtr` lifetime** (BEFORE coroutine vs. callback ordering bugs) | ✓ eliminated | Server owns the dataset; the client carries an opaque string id and `file.close` for cleanup. |
| **`ChangeHduSelection` file-reopen-per-switch** (BEFORE line 1435) | ✓ eliminated | `dataset.getHeader(datasetId, hduIndex)` is a single server call; no client-side reopen. |

The S5 fix is a pure adapter-side edit — none of the 34 file-tab ViewModel unit tests need to change. The eliminated smells have direct test coverage in `refactoring-examples/sub-team-6/file-tab/adapters/tests/FitsServiceAdapterTests.cs` (gateway-routed assertions) and `refactoring-examples/sub-team-6/contracts/tests/` (wire framing).
