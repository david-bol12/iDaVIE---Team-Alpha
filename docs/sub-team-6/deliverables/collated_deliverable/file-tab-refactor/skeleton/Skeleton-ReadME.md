# File-tab skeleton — what lives here

Everything in this folder is **pure, Unity-free C#**: no `UnityEngine`, no
`SteamVR`, no native `[DllImport]`. That's the membership rule for the folder.

The test for any class is never "is it a ViewModel or an interface?" — it's
**"can it compile and be tested without Unity?"**

- Yes → it belongs in `skeleton/`.
- Needs Unity or native calls → it belongs in `adapters/`.

Because the skeleton has no Unity dependency, it compiles on its own in CI and the ViewModels can be unit-tested with fakes (ADR-002 ACL, NFR-MOD-2).

## Inventory

| Kind | Files | Interface? |
|---|---|---|
| **ViewModels** | `FileTabViewModel.cs`, `SubsetBoundsViewModel.cs` | No (concrete) |
| **Interfaces** | `IFileTabViewModel`, `ICommand`, `IFitsService`, `IFitsHandle`, `IFileDialogService`, `IVolumeService`, `IMemoryProbe` | Yes |
| **DTOs / data carriers** | `FitsFileInfo`, `LoadCubeRequest`, `SubsetBounds`, `CubeLoadedEventArgs` | No (concrete data) |
| **Pure logic helper** | `FitsMetadataHelper` | No (concrete static class) |
| **Concrete commands** | `RelayCommand`, `AsyncRelayCommand` (inside `FileTabViewModel.cs`) | No |

## Why the interfaces live here, not in `adapters/`

An interface belongs to the code that **needs** it, not the code that
**implements** it (Dependency Inversion). `FileTabViewModel` needs to read FITS
files, so `IFitsService` lives beside it; the concrete `FitsServiceAdapter` lives
in `adapters/` and references this folder — never the other way round.

```
adapters  ──references──▶  skeleton
          (never ◀──)
```

If an interface moved into `adapters/`, the skeleton would have to reference the adapter assembly to see it — a circular dependency, and Unity would leak back in.
