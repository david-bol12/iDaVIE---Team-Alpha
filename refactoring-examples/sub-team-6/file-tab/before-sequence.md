# File tab — BEFORE sequence diagram (Mermaid)

This is the Mermaid rendering of [`before-trace.md`](before-trace.md). Every message is sourced from the trace document; line citations live there. Pair this diagram with the trace when presenting to the panel.

The two clicks (`Open` and `Load`) are drawn as one continuous diagram with a `Note` separator. The visible "two-click reality" is itself part of the proposal's argument — collapsing it into a single user action is one of the AFTER design's claims.

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant OpenBtn as OpenButton<br/>(scene asset)
    participant LoadBtn as LoadButton<br/>(scene asset)
    participant CD as CanvassDesktop<br/>(1899-line MonoBehaviour)
    participant SFB as StandaloneFileBrowser
    participant FR as FitsReader<br/>(static wrapper)
    participant DLL as idavie_native<br/>(C/C++ DLL)
    participant VCC as VolumeCommandController<br/>(FindObjectOfType)
    participant VDSR as VolumeDataSetRenderer<br/>(instantiated from prefab)

    %% ═══════════════════════════════════════════════════════════════════
    %% PHASE A — User picks a file (metadata read, no cube yet)
    %% ═══════════════════════════════════════════════════════════════════
    rect rgb(245, 245, 220)
    Note over User,VDSR: PHASE A — Open dialog, read FITS metadata
    User->>OpenBtn: click "File > Open"
    OpenBtn->>CD: BrowseImageFile()<br/>[Inspector-wired]
    activate CD
    CD->>CD: PlayerPrefs.GetString("LastPath")
    CD->>SFB: OpenFilePanelAsync(..., callback)
    deactivate CD

    SFB-->>User: native OS file picker
    User->>SFB: select *.fits
    SFB-->>CD: callback(paths)
    activate CD

    CD->>CD: _browseImageFile(paths[0])
    CD->>FR: FitsOpenFile(out fptr, _imagePath)
    FR->>DLL: [DllImport] FitsOpenFileReadOnly(...)
    Note right of DLL: ★ ACL violation:<br/>native handle leaks into MonoBehaviour scope
    DLL-->>FR: fptr, status=0
    FR-->>CD: fptr

    CD->>FR: FitsGetHduCount(fptr)
    FR->>DLL: [DllImport] FitsGetHduCount(...)
    DLL-->>FR: hduNum
    FR-->>CD: hduNum

    loop for each HDU (i = 1..hduNum)
        CD->>FR: FitsMovabsHdu(fptr, i)
        FR->>DLL: [DllImport]
        CD->>FR: FitsReadKey(EXTNAME / HDUNAME)
        FR->>DLL: [DllImport]
    end

    CD->>FR: FitsMovabsHdu(fptr, 1)  %% reset to primary
    CD->>CD: transform.Find("HeaderTitle_container/<br/>Hdu_container/Hdu_dropdown")<br/>.GetComponent&lt;Dropdown&gt;()
    Note right of CD: scene-hierarchy hardwiring → untestable

    CD->>CD: UpdateHeaderFromFits(fptr)<br/>(further FR.FitsReadKey calls)
    CD->>FR: FitsCloseFile(fptr)
    FR->>DLL: [DllImport]
    Note right of CD: manual lifetime management<br/>of unmanaged handle

    CD->>CD: IsLoadable() — inline axis-count rule
    CD-->>User: Load button becomes interactable;<br/>subset selector appears
    deactivate CD
    end

    Note over User,VDSR: User now sees the file is "valid";<br/>must click a second button to actually load

    %% ═══════════════════════════════════════════════════════════════════
    %% PHASE B — User confirms load (cube becomes visible)
    %% ═══════════════════════════════════════════════════════════════════
    rect rgb(220, 235, 245)
    Note over User,VDSR: PHASE B — Load coroutine, cube becomes visible
    User->>LoadBtn: click "Load"
    LoadBtn->>CD: LoadFileFromFileSystem()<br/>[Inspector-wired]
    activate CD
    CD->>CD: StartCoroutine(LoadCubeCoroutine)
    deactivate CD

    activate CD
    Note over CD: coroutine reentry — same lifeline,<br/>async lifetime
    CD->>CD: show progress bar;<br/>CheckMemSpaceForCubes(...)

    opt previous cube exists
        CD->>VCC: RemoveDataSet(firstActiveRenderer)
        CD->>CD: FindObjectOfType&lt;VolumeInputController&gt;()<br/>.SetActive(false/true)  %% fragile "reset"
        CD->>VCC: DisablePaintMode() /<br/>endThresholdEditing() /<br/>endZAxisEditing()
        CD->>VDSR: Data.CleanUp(...) /<br/>Mask?.CleanUp(false) /<br/>Destroy(renderer)
    end

    CD->>VDSR: Instantiate(cubeprefab) → newCube
    CD->>VDSR: newCube.GetComponent&lt;VolumeDataSetRenderer&gt;()
    CD->>VDSR: direct field writes:<br/>.subsetBounds, .FileName, .MaskFileName,<br/>.SelectedHdu, .loadText, .progressBar,<br/>.CubeDepthAxis, .FileChanged
    Note right of VDSR: ★ no encapsulation —<br/>UI pokes public mutable fields

    CD->>VCC: AddDataSet(volDSRender)
    CD->>VDSR: StartCoroutine(_startFunc())
    activate VDSR
    VDSR->>VDSR: texture upload, GPU resources
    VDSR-->>CD: started = true
    deactivate VDSR

    loop while !volDSRender.started
        CD->>CD: yield return WaitForSeconds(0.1f)
    end
    Note right of CD: ★ busy-wait via yield —<br/>async-as-pseudo-sync

    CD-->>User: cube appears in scene

    CD->>CD: postLoadFileFileSystem()<br/>— hides LoadingText, progressBar,<br/>WelcomeMenu; enables Rendering/Stats/Sources/Paint;<br/>auto-clicks Stats_Button
    CD-->>User: WelcomeMenu disappears;<br/>tabs come alive; Stats tab shown
    deactivate CD
    end
```

---

## Reading guide for the panel

The diagram makes four smells visible at a glance:

1. **`CD → FR → DLL` triangle** repeated on every metadata read — the ACL boundary leaks. Every `[DllImport]` arrow is a Section 4.2 violation.
2. **Two `activate` bars on `CanvassDesktop`** — one for the SFB callback, one for the load coroutine. Both re-enter the same lifeline asynchronously and mutate scene state, which is why `CanvassDesktop` cannot be exercised outside the Unity scene.
3. **`transform.Find` self-message** and `FindObjectOfType` arrows — both are hardwired to scene hierarchy / global state, so neither is reachable from a unit test.
4. **The `Phase A` / `Phase B` separator** — the visible "two-click reality." The AFTER design folds metadata-read and cube-load into a single user action (Load button enabled only when validation passes, no separate Open step).

## Smell anchors (cross-reference with the trace)

| Diagram marker | Trace smell ID | Code citation |
|---|---|---|
| `★ ACL violation` after `FitsOpenFileReadOnly` | S1 | `CanvassDesktop.cs:349` |
| `★ no encapsulation` after field-write block | S5 | `CanvassDesktop.cs:1086–1094` |
| `★ busy-wait via yield` | S6 | `CanvassDesktop.cs:1116–1119` |
| `Inspector-wired` notes on both clicks | S7 | scene asset (no code-side wiring) |
| `transform.Find` self-call | S3 | `CanvassDesktop.cs:382–394` |
| `FindObjectOfType` arrow on input controller reset | S4 | `CanvassDesktop.cs:1054` |
| Coroutine `activate` bar + busy-wait loop | S2, S6 | `CanvassDesktop.cs:929, 1116` |

Pair the diagram with [`after-sequence.md`](after-sequence.md) on a side-by-side slide: the AFTER replaces every `→ DLL` and `→ VCC` arrow with a single `→ serviceGateway` message.
