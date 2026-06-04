// brief §6.6 | File tab AFTER — FileTabView (Unity-assembly, thin View)

// Single responsibility: translate Unity UI events into ViewModel calls, and ViewModel PropertyChanged events into UI updates.
// No business logic. No transform. No file I/O.
// Satisfies ADR-009 (MVVM split) and NFR-MOD-2.

// FileTabView.cs is in a different folder to FileTabViewModel.cs as adapters and skeletons are inherently different. A skeleton is pure C#, Unity-free, and is testable. adapters/ are Unit-coupled
// FileTabView.cs here is a MonoBehaviour, it inherents from the Unity Engine
// Adapters reference Skeletons, but Skeletons don't reference Adapters
using System.ComponentModel;
using System.IO;
using iDaVIE.Desktop.FileTab;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace iDaVIE.Desktop.Adapters.FileTab
{
    // Thin MonoBehaviour view for the File tab panel.
    // UI references are assigned via the Unity Inspector (no transform.Find).
    // The composition root calls BindTo once; thereafter the View is purely reactive.
    public sealed class FileTabView : MonoBehaviour
    {
        // Inspector-assigned UI references
        // [Header(...)] just draws a bold section title in the Inspector. It is visual grouping for whoever wires the slots and has no effect at runtime

        // Read-only text the VM pushes out: chosen paths, FITS header dump, validation message.
        [Header("Labels")]
        [SerializeField] private TMP_Text _imagePathLabel  = null!;
        [SerializeField] private TMP_Text _maskPathLabel   = null!;
        [SerializeField] private TMP_Text _headerText      = null!;
        [SerializeField] private TMP_Text _validationLabel = null!;

        // User choices fed back into the VM: which HDU, which axis is Z, aspect-ratio mode.
        [Header("Dropdowns")]
        [SerializeField] private TMP_Dropdown _hduDropdown   = null!;
        [SerializeField] private TMP_Dropdown _zAxisDropdown = null!;
        [SerializeField] private TMP_Dropdown _ratioDropdown = null!;

        // Optional crop region: the toggle + the two panels shown/hidden with it,
        // plus the six X/Y/Z min-max bounds inputs.
        [Header("Subset panel")]
        [SerializeField] private GameObject     _subsetPanel  = null!;
        [SerializeField] private GameObject     _zAxisPanel   = null!;
        [SerializeField] private Toggle         _subsetToggle = null!;
        [SerializeField] private TMP_InputField _xMinInput    = null!;
        [SerializeField] private TMP_InputField _xMaxInput    = null!;
        [SerializeField] private TMP_InputField _yMinInput    = null!;
        [SerializeField] private TMP_InputField _yMaxInput    = null!;
        [SerializeField] private TMP_InputField _zMinInput    = null!;
        [SerializeField] private TMP_InputField _zMaxInput    = null!;

        // The four actions, wired in BindTo to the VM's commands (browse image/mask, load, clear mask).
        [Header("Buttons")]
        [SerializeField] private Button _browseImageBtn = null!;
        [SerializeField] private Button _browseMaskBtn  = null!;
        [SerializeField] private Button _loadBtn        = null!;
        [SerializeField] private Button _clearMaskBtn   = null!;

        private IFileTabViewModel? _vm;

        // Public binding point (called by FileTabCompositionRoot)

        public void BindTo(IFileTabViewModel vm)
        {
            if (_vm != null)
                _vm.PropertyChanged -= OnPropertyChanged;

            _vm = vm;
            _vm.PropertyChanged += OnPropertyChanged;

            // Wire buttons → async ViewModel commands
            _browseImageBtn.onClick.AddListener(() => _ = _vm.BrowseImageCommand.ExecuteAsync());
            _browseMaskBtn.onClick.AddListener(()  => _ = _vm.BrowseMaskCommand.ExecuteAsync());
            _loadBtn.onClick.AddListener(()        => _ = _vm.LoadCommand.ExecuteAsync());
            _clearMaskBtn.onClick.AddListener(()   =>     _vm.ClearMaskCommand.Execute());

            // Wire CanExecuteChanged → button interactability
            _vm.BrowseImageCommand.CanExecuteChanged += (_, _) =>
                _browseImageBtn.interactable = _vm.BrowseImageCommand.CanExecute();
            _vm.BrowseMaskCommand.CanExecuteChanged += (_, _) =>
                _browseMaskBtn.interactable = _vm.BrowseMaskCommand.CanExecute();
            _vm.LoadCommand.CanExecuteChanged += (_, _) =>
                _loadBtn.interactable = _vm.LoadCommand.CanExecute();
            _vm.ClearMaskCommand.CanExecuteChanged += (_, _) =>
                _clearMaskBtn.interactable = _vm.ClearMaskCommand.CanExecute();

            // Wire dropdowns → ViewModel (two-way)
            _hduDropdown.onValueChanged.AddListener(idx => _vm.SelectedHduIndex = idx);
            _zAxisDropdown.onValueChanged.AddListener(idx => _vm.SelectedZAxisIndex = idx);
            _ratioDropdown.onValueChanged.AddListener(idx => _vm.RatioMode = (RatioMode)idx);
            RebuildRatioDropdown();

            // Wire subset toggle → ViewModel
            _subsetToggle.onValueChanged.AddListener(on => _vm.SubsetEnabled = on);

            // Wire subset input fields → SubsetBoundsViewModel (on focus-lost)
            _xMinInput.onEndEdit.AddListener(v => { if (int.TryParse(v, out int n)) _vm.Subset.XMin = n; });
            _xMaxInput.onEndEdit.AddListener(v => { if (int.TryParse(v, out int n)) _vm.Subset.XMax = n; });
            _yMinInput.onEndEdit.AddListener(v => { if (int.TryParse(v, out int n)) _vm.Subset.YMin = n; });
            _yMaxInput.onEndEdit.AddListener(v => { if (int.TryParse(v, out int n)) _vm.Subset.YMax = n; });
            _zMinInput.onEndEdit.AddListener(v => { if (int.TryParse(v, out int n)) _vm.Subset.ZMin = n; });
            _zMaxInput.onEndEdit.AddListener(v => { if (int.TryParse(v, out int n)) _vm.Subset.ZMax = n; });

            // Wire SubsetBoundsViewModel.PropertyChanged → input fields (View ← ViewModel)
            // This replaces CanvassDesktop.setSubsetBounds() / checkSubsetBounds() / updateSubsetZMax()
            _vm.Subset.PropertyChanged += OnSubsetPropertyChanged;

            // Initial population
            SyncAll();
        }

        private void OnDestroy()
        {
            if (_vm == null) return;

            // Unsubscribe named handlers. CanExecuteChanged subscribers are anonymous
            // lambdas; they are released when the command objects are GC'd with the VM.
            _vm.PropertyChanged        -= OnPropertyChanged;
            _vm.Subset.PropertyChanged -= OnSubsetPropertyChanged;

            // Remove Unity UI listeners to prevent callbacks into a destroyed view.
            _browseImageBtn.onClick.RemoveAllListeners();
            _browseMaskBtn.onClick.RemoveAllListeners();
            _loadBtn.onClick.RemoveAllListeners();
            _clearMaskBtn.onClick.RemoveAllListeners();
            _hduDropdown.onValueChanged.RemoveAllListeners();
            _zAxisDropdown.onValueChanged.RemoveAllListeners();
            _ratioDropdown.onValueChanged.RemoveAllListeners();
            _subsetToggle.onValueChanged.RemoveAllListeners();
            _xMinInput.onEndEdit.RemoveAllListeners();
            _xMaxInput.onEndEdit.RemoveAllListeners();
            _yMinInput.onEndEdit.RemoveAllListeners();
            _yMaxInput.onEndEdit.RemoveAllListeners();
            _zMinInput.onEndEdit.RemoveAllListeners();
            _zMaxInput.onEndEdit.RemoveAllListeners();

            _vm = null;
        }

        // ViewModel → View: top-level properties

        // The VM raises PropertyChanged with the name of whatever property just changed; this switch routes that name to the one  widget that displays it, reads the new value back off the VM, and pushes it in.
        // All logic stays in the VM; the View only translates a property name into a UI update.
        private void OnPropertyChanged(object? _, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(IFileTabViewModel.ImagePath):
                    _imagePathLabel.text = string.IsNullOrEmpty(_vm!.ImagePath)
                        ? "..." : Path.GetFileName(_vm.ImagePath);
                    break;

                case nameof(IFileTabViewModel.MaskPath):
                    _maskPathLabel.text = string.IsNullOrEmpty(_vm!.MaskPath)
                        ? "..." : Path.GetFileName(_vm.MaskPath);
                    break;

                case nameof(IFileTabViewModel.HduOptions):
                    RebuildHduDropdown();
                    break;

                case nameof(IFileTabViewModel.ZAxisOptions):
                    RebuildZAxisDropdown();
                    break;

                case nameof(IFileTabViewModel.IsLoadable):
                    _subsetPanel.SetActive(_vm!.IsLoadable);
                    break;

                case nameof(IFileTabViewModel.SubsetEnabled):
                    _subsetToggle.SetIsOnWithoutNotify(_vm!.SubsetEnabled);
                    break;

                case nameof(IFileTabViewModel.RatioMode):
                    _ratioDropdown.SetValueWithoutNotify((int)_vm!.RatioMode);
                    break;

                case nameof(IFileTabViewModel.HeaderText):
                    _headerText.text = _vm!.HeaderText ?? string.Empty;
                    break;

                case nameof(IFileTabViewModel.ValidationMessage):
                    _validationLabel.text   = _vm!.ValidationMessage ?? string.Empty;
                    _validationLabel.gameObject.SetActive(_vm.ValidationMessage != null);
                    break;

                case nameof(IFileTabViewModel.IsLoading):
                    // Spinner feedback — buttons are already guarded by CanExecuteChanged
                    break;
            }
        }

        // ViewModel → View: subset bounds

        // Same inbound pattern, but for the nested SubsetBoundsViewModel: when the VM clamps or corrects a bound, it pushes the corrected number back into the matching input field.
        // SetTextWithoutNotify writes the field without re-firing onEndEdit, so the correction doesn't bounce straight back into the VM.
        private void OnSubsetPropertyChanged(object? _, PropertyChangedEventArgs e)
        {
            var s = _vm!.Subset;
            switch (e.PropertyName)
            {
                case nameof(SubsetBoundsViewModel.XMin): _xMinInput.SetTextWithoutNotify(s.XMin.ToString()); break;
                case nameof(SubsetBoundsViewModel.XMax): _xMaxInput.SetTextWithoutNotify(s.XMax.ToString()); break;
                case nameof(SubsetBoundsViewModel.YMin): _yMinInput.SetTextWithoutNotify(s.YMin.ToString()); break;
                case nameof(SubsetBoundsViewModel.YMax): _yMaxInput.SetTextWithoutNotify(s.YMax.ToString()); break;
                case nameof(SubsetBoundsViewModel.ZMin): _zMinInput.SetTextWithoutNotify(s.ZMin.ToString()); break;
                case nameof(SubsetBoundsViewModel.ZMax): _zMaxInput.SetTextWithoutNotify(s.ZMax.ToString()); break;
            }
        }

        // Dropdown

        private void RebuildHduDropdown()
        {
            _hduDropdown.ClearOptions();
            foreach (var h in _vm!.HduOptions)
                _hduDropdown.options.Add(new TMP_Dropdown.OptionData($"{h.Index}: {h.Name}"));
            // Only show the HDU dropdown when there is more than one HDU
            _hduDropdown.gameObject.SetActive(_vm.HduOptions.Count > 1);
            _hduDropdown.RefreshShownValue();
        }

        private void RebuildZAxisDropdown()
        {
            _zAxisDropdown.ClearOptions();
            foreach (var z in _vm!.ZAxisOptions)
                _zAxisDropdown.options.Add(new TMP_Dropdown.OptionData(z));
            _zAxisPanel.SetActive(_vm.ZAxisOptions.Count > 0);
            _zAxisDropdown.interactable = _vm.ZAxisOptions.Count > 0;
            _zAxisDropdown.RefreshShownValue();
        }

        private void RebuildRatioDropdown()
        {
            _ratioDropdown.ClearOptions();
            foreach (var label in _vm!.RatioModeOptions)
                _ratioDropdown.options.Add(new TMP_Dropdown.OptionData(label));
            _ratioDropdown.SetValueWithoutNotify((int)_vm.RatioMode);
            _ratioDropdown.RefreshShownValue();
        }

        // Initial full sync

        private void SyncAll()
        {
            foreach (var name in new[]
            {
                nameof(IFileTabViewModel.ImagePath),
                nameof(IFileTabViewModel.MaskPath),
                nameof(IFileTabViewModel.HduOptions),
                nameof(IFileTabViewModel.ZAxisOptions),
                nameof(IFileTabViewModel.IsLoadable),
                nameof(IFileTabViewModel.SubsetEnabled),
                nameof(IFileTabViewModel.RatioMode),
                nameof(IFileTabViewModel.HeaderText),
                nameof(IFileTabViewModel.ValidationMessage),
            })
                OnPropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }
}
