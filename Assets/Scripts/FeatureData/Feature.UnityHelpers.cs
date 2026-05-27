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
using LineRenderer;
using UnityEngine;

namespace DataFeatures
{
    public partial class Feature
    {
        public static void SetCubeColors(CuboidLine cube, Color baseColor, bool colorAxes)
        {
            cube.Color = baseColor;

            if (colorAxes)
            {
                var colorAxisX = new Color(1.0f, 0.3f, 0.3f);
                var colorAxisY = new Color(0.3f, 1.0f, 0.3f);
                var colorAxisZ = new Color(0.3f, 0.3f, 1.0f);
                cube.SetColor(colorAxisX, 7);
                cube.SetColor(colorAxisY, 4);
                cube.SetColor(colorAxisZ, 8);
            }
        }
    }
}