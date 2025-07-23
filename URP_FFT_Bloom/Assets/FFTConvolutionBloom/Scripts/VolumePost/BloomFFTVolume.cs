using System;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable, VolumeComponentMenu("Post-processing/Custom/Bloom FFT")]
public class BloomFFTVolume : VolumeComponent, IPostProcessComponent
{
    public ClampedFloatParameter intensity = new ClampedFloatParameter(1f, 0f, 10f);
    public FloatParameter threshold = new FloatParameter(1f);

    public bool IsActive() => intensity.value > 0f;
    public bool IsTileCompatible() => false;
}
