/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * This file is part of the iDaVIE project.
 *
 * iDaVIE is free software: you can redistribute it and/or modify it under the terms
 * of the GNU Lesser General Public License (LGPL) as published by the Free Software
 * Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 * PURPOSE. See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with
 * iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Concurrent;
using System.IO;
using iDaVIE.Persistence.Application;
using iDaVIE.Persistence.Application.Interfaces;
using iDaVIE.Persistence.Domain;
using iDaVIE.Persistence.Infrastructure;
using iDaVIE.Persistence.Adapters;
using UnityEngine;
using VolumeData;

namespace iDaVIE.Persistence
{
    /// <summary>
    /// Bootstraps the persistence pipeline inside Unity.
    ///
    /// Wiring order:
    ///   Awake  → resolve scene refs, build all services
    ///   Start  → CrashDetector.CheckAndAcquire() → optional restore → AutosaveService.Start()
    ///   Update → drain _pendingSaves queue (main-thread save execution)
    ///   OnApplicationQuit / OnDestroy → release crash lock, dispose timer
    /// </summary>
    public class PersistenceController : MonoBehaviour
    {
        [Header("Scene References")]
        [Tooltip("The VolumeDataSetRenderer in the scene.")]
        public VolumeDataSetRenderer volumeRenderer;

        [Tooltip("The VolumeInputController in the scene.")]
        public VolumeInputController volumeInputController;

        [Tooltip("The FeatureSetManager in the scene.")]
        public FeatureData.FeatureSetManager featureSetManager;

        // ── Services (built in Awake) ─────────────────────────────────────────
        private AutosaveService      _autosaveService;
        private CrashDetector        _crashDetector;
        private RestoreOrchestrator  _restoreOrchestrator;
        private SaveWorkspaceUseCase _saveUseCase;
        private SnapshotRing         _ring;
        private SnapshotSerializer   _serializer;

        // Thread-safe queue: timer thread posts work items; Update() drains them
        private readonly ConcurrentQueue<Action> _pendingSaves = new ConcurrentQueue<Action>();

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            string snapshotDir = ResolveSnapshotDirectory();
            Debug.Log($"[Persistence] Snapshot directory: {snapshotDir}");

            _ring          = new SnapshotRing(snapshotDir, Config.Instance.snapshotCapacity);
            _serializer    = new SnapshotSerializer();
            _crashDetector = new CrashDetector(snapshotDir);

            var dataIoAdapter      = new DataIoStateAdapter(volumeRenderer);
            var renderingAdapter   = new RenderingStateAdapter(volumeRenderer);
            var featureAdapter     = new FeatureStateAdapter(featureSetManager);
            var interactionAdapter = new InteractionStateAdapter(volumeInputController);
            var guiAdapter         = new GuiStateAdapter(volumeRenderer);

            IWorkspaceStateCollector collector = new UnityWorkspaceStateCollector(
                dataIoAdapter, renderingAdapter, featureAdapter, interactionAdapter, guiAdapter);

            _saveUseCase = new SaveWorkspaceUseCase(
                collector,
                new WorkspaceStubFactory(),
                new WorkspaceValidator(),
                _serializer,
                _ring);

            _restoreOrchestrator = new RestoreOrchestrator(
                dataIoAdapter, renderingAdapter, featureAdapter, interactionAdapter, guiAdapter);

            _autosaveService = new AutosaveService(_saveUseCase, Config.Instance.autosaveIntervalSeconds);
            _autosaveService.SaveCompleted += skipped =>
                Debug.Log(skipped
                    ? "[Persistence] Autosave cycle skipped (save already in progress)."
                    : "[Persistence] Autosave snapshot written.");
        }

        private void Start()
        {
            bool crashed = _crashDetector.CheckAndAcquire();

            if (crashed)
            {
                Debug.LogWarning("[Persistence] Crash detected — attempting recovery from last snapshot.");
                TryRestore();
            }
            else
            {
                Debug.Log("[Persistence] Clean start — no crash detected.");
            }

            _autosaveService.Start();
            Debug.Log($"[Persistence] Autosave started (interval = {Config.Instance.autosaveIntervalSeconds}s).");
        }

        private void Update()
        {
            Action action;
            while (_pendingSaves.TryDequeue(out action))
                action();
        }

        private void OnApplicationQuit()
        {
            CleanShutdown();
        }

        private void OnDestroy()
        {
            CleanShutdown();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Triggers an immediate save and resets the autosave timer.
        /// Call from a UI button handler on the main thread.
        /// </summary>
        public void SaveNow()
        {
            _autosaveService.TriggerNow();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void TryRestore()
        {
            try
            {
                var handle = _ring.Peek();
                if (handle == null)
                {
                    Debug.LogWarning("[Persistence] No snapshot found — nothing to restore.");
                    return;
                }

                Debug.Log($"[Persistence] Restoring from: {handle.FilePath} (saved {handle.SavedAt:u})");
                var snapshot = _serializer.Deserialize(handle.FilePath);
                _restoreOrchestrator.Restore(snapshot);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Persistence] Restore failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void CleanShutdown()
        {
            if (_autosaveService != null)
            {
                _autosaveService.Stop();
                _autosaveService.Dispose();
            }
            if (_crashDetector != null)
                _crashDetector.Release();
            Debug.Log("[Persistence] Clean shutdown — crash lock released.");
        }

        private static string ResolveSnapshotDirectory()
        {
            string dir = Config.Instance.workspaceSaveDirectory;
            if (!string.IsNullOrEmpty(dir))
                return dir;

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "iDaVIE", "workspaces", "default", "snapshots");
        }
    }
}
