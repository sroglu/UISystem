using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace mehmetsrl.UISystem.Data
{
    [Serializable]
    public struct ColorPalette
    {
        [BoxGroup("Primary")]
        public Color Primary;
        [BoxGroup("Primary")]
        public Color OnPrimary;
        [BoxGroup("Primary")]
        public Color PrimaryContainer;
        [BoxGroup("Primary")]
        public Color OnPrimaryContainer;

        [BoxGroup("Secondary")]
        public Color Secondary;
        [BoxGroup("Secondary")]
        public Color OnSecondary;
        [BoxGroup("Secondary")]
        public Color SecondaryContainer;
        [BoxGroup("Secondary")]
        public Color OnSecondaryContainer;

        [BoxGroup("Surface")]
        public Color Surface;
        [BoxGroup("Surface")]
        public Color OnSurface;
        [BoxGroup("Surface")]
        public Color SurfaceVariant;
        [BoxGroup("Surface")]
        public Color OnSurfaceVariant;

        [BoxGroup("Error")]
        public Color Error;
        [BoxGroup("Error")]
        public Color OnError;

        [BoxGroup("Outline")]
        public Color Outline;
        [BoxGroup("Outline")]
        public Color OutlineVariant;

        [BoxGroup("Background")]
        public Color Background;

        [BoxGroup("Inverse")]
        public Color InverseSurface;
        [BoxGroup("Inverse")]
        public Color InverseOnSurface;
        [BoxGroup("Inverse")]
        public Color InversePrimary;
    }
}
