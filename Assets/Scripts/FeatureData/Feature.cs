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
 *
 * Additional information and disclaimers regarding liability and third-party
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 *
 */
using UnityEngine;

namespace DataFeatures
{
    // Feature is the basic unit of marking up the volume
    public partial class Feature
    {
        public bool Temporary;
        public string Comment;
        public float Metric;
        public int Index { get; set; }
        public int Id { get; }
        public string Name { get; }

        public string Flag { get; set; }
        private bool _selected;
        private FeatureColor _color;
        private bool _active;
        private Vector3[] _corners = new Vector3[2];
        public string[] RawData { get; set; }
        public IFeatureDirtyNotifier FeatureSetParent { get; set; }

        public bool StatusChanged;

        public Feature(Vector3 cubeMin, Vector3 cubeMax, FeatureColor cubeColor, string name, string flag, int index, int id, string[] rawData, bool startVisible)
        {
            FeatureSetParent = null;
            Index = index;
            Id = id;
            _color = cubeColor;
            Name = name;
            Flag = flag;
            SetBounds(cubeMin, cubeMax);
            RawData = rawData;
            Visible = startVisible;
        }

        public void ShowAxes(bool show)
        {
            // TODO: Handle this
        }

        public Vector3 CornerMin => Vector3.Min(_corners[0], _corners[1]);

        public Vector3 CornerMax => Vector3.Max(_corners[0], _corners[1]);

        public Vector3 Center
        {
            get => (_corners[0] + _corners[1]) / 2.0f;
            set
            {
                var diff = value - Center;
                _corners[0] += diff;
                _corners[1] += diff;
                NotifyDirty();
            }
        }

        public Vector3 Size
        {
            //  Size is padded by one, because the bounding box includes both the min and max voxels
            get => (Vector3.Max(_corners[0], _corners[1]) - Vector3.Min(_corners[0], _corners[1]) + Vector3.one);
            set
            {
                var currentCenter = Center;
                _corners[0] = currentCenter - value / 2.0f;
                _corners[1] = currentCenter + value / 2.0f;
                NotifyDirty();
            }
        }

        public float Volume
        {
            get
            {
                var s = Size;
                return s.x * s.y * s.z;
            }
        }

        public bool ContainsPoint(Vector3 point)
        {
            var min = CornerMin;
            var max = CornerMax;
            return point.x >= min.x && point.x <= max.x
                && point.y >= min.y && point.y <= max.y
                && point.z >= min.z && point.z <= max.z;
        }

        public FeatureColor CubeColor
        {
            get => _color;
            set
            {
                if (_color != value)
                    FeatureSetParent?.SetFeatureAsDirty(Index);
                _color = value;
            }
        }

        public bool Visible
        {
            get => _active;
            set
            {
                if (_active != value)
                    FeatureSetParent?.SetFeatureAsDirty(Index);
                _active = value;
            }
        }

        public bool Selected
        {
            get => _selected;
            set
            {
                if (_selected != value)
                    FeatureSetParent?.SetFeatureAsDirty(Index);
                _selected = value;
            }
        }

        public void SetBounds(Vector3 cornerMin, Vector3 cornerMax)
        {
            _corners[0] = cornerMin;
            _corners[1] = cornerMax;
            NotifyDirty();
        }

        public Vector3 GetMinBounds() => _corners[0];

        public Vector3 GetMaxBounds() => _corners[1];

        private void NotifyDirty()
        {
            FeatureSetParent?.SetFeatureAsDirty(Index);
        }
    }
}