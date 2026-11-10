// Definiciones simplificadas de URP (Volume y efectos de posprocesado) para compilar fuera de Unity.
using System;
namespace UnityEngine.Rendering {
    public enum BlendMode { Zero = 0, One = 1, SrcAlpha = 5, OneMinusSrcAlpha = 10 }
    public enum RenderQueue { Geometry = 2000, Transparent = 3000 }
    public class VolumeParameter<T> { public T value; public bool overrideState; public void Override(T v) { value = v; overrideState = true; } }
    public class ClampedFloatParameter : VolumeParameter<float> {}
    public class MinFloatParameter : VolumeParameter<float> {}
    public class FloatParameter : VolumeParameter<float> {}
    public class BoolParameter : VolumeParameter<bool> {}
    public class ColorParameter : VolumeParameter<Color> {}
    public class Vector4Parameter : VolumeParameter<Vector4> {}
    public class VolumeComponent : ScriptableObject { public bool active = true; }
    public class VolumeProfile : ScriptableObject {
        public System.Collections.Generic.List<VolumeComponent> components = new System.Collections.Generic.List<VolumeComponent>();
        public T Add<T>(bool overrides = false) where T : VolumeComponent { var c = (T)Activator.CreateInstance(typeof(T)); components.Add(c); return c; }
        public bool TryGet<T>(out T c) where T : VolumeComponent { foreach (var x in components) if (x is T) { c = (T)x; return true; } c = null; return false; }
        public bool Has<T>() where T : VolumeComponent { T c; return TryGet(out c); }
    }
    public class Volume : Behaviour { public bool isGlobal; public float priority, weight = 1f, blendDistance; public VolumeProfile sharedProfile; public VolumeProfile profile { get { return sharedProfile; } set { sharedProfile = value; } } }
}
namespace UnityEngine.Rendering.Universal {
    public enum TonemappingMode { None, Neutral, ACES }
    public enum FilmGrainLookup { Thin1, Thin2, Medium1 }
    public class TonemappingModeParameter : VolumeParameter<TonemappingMode> {}
    public class FilmGrainLookupParameter : VolumeParameter<FilmGrainLookup> {}
    public class Bloom : VolumeComponent { public MinFloatParameter threshold = new MinFloatParameter(), intensity = new MinFloatParameter(); public ClampedFloatParameter scatter = new ClampedFloatParameter(); public BoolParameter highQualityFiltering = new BoolParameter(); public ColorParameter tint = new ColorParameter(); }
    public class Vignette : VolumeComponent { public ClampedFloatParameter intensity = new ClampedFloatParameter(), smoothness = new ClampedFloatParameter(); public ColorParameter color = new ColorParameter(); public BoolParameter rounded = new BoolParameter(); }
    public class ChromaticAberration : VolumeComponent { public ClampedFloatParameter intensity = new ClampedFloatParameter(); }
    public class Tonemapping : VolumeComponent { public TonemappingModeParameter mode = new TonemappingModeParameter(); }
    public class WhiteBalance : VolumeComponent { public ClampedFloatParameter temperature = new ClampedFloatParameter(), tint = new ClampedFloatParameter(); }
    public class ColorAdjustments : VolumeComponent { public FloatParameter postExposure = new FloatParameter(); public ClampedFloatParameter contrast = new ClampedFloatParameter(), saturation = new ClampedFloatParameter(), hueShift = new ClampedFloatParameter(); public ColorParameter colorFilter = new ColorParameter(); }
    public class ShadowsMidtonesHighlights : VolumeComponent { public Vector4Parameter shadows = new Vector4Parameter(), midtones = new Vector4Parameter(), highlights = new Vector4Parameter(); }
    public class FilmGrain : VolumeComponent { public FilmGrainLookupParameter type = new FilmGrainLookupParameter(); public ClampedFloatParameter intensity = new ClampedFloatParameter(), response = new ClampedFloatParameter(); }
    public class UniversalAdditionalCameraData : MonoBehaviour { public bool renderPostProcessing; public bool stopNaN; public bool dithering; }
    public static class CameraExtensions { public static UniversalAdditionalCameraData GetUniversalAdditionalCameraData(this Camera c) { return c.gameObject.AddComponent<UniversalAdditionalCameraData>(); } }
}
