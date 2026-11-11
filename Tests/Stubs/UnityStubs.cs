// Minimal Unity API stubs for compile-checking only.
using System;
using System.Collections;
using System.Collections.Generic;
#pragma warning disable 0067, 0649, 0626, 0824
namespace UnityEngine
{
    public class Object {
        public string name; public HideFlags hideFlags; [System.Obsolete("Use GetEntityId instead", true)] public int GetInstanceID() { return 0; } public static T FindAnyObjectByType<T>() where T : Object { return null; }
        public static void Destroy(Object o) {} public static void Destroy(Object o, float t) {} public static void DontDestroyOnLoad(Object o) {}
        public static void DestroyImmediate(Object o) {}
        public static T[] FindObjectsByType<T>(FindObjectsSortMode m) where T : Object { return new T[0]; }
        public static implicit operator bool(Object o) { return o != null; }
    }
    public class Component : Object {
        public Transform transform; public GameObject gameObject; public string tag;
        public T GetComponent<T>() { return default(T); }
        public T GetComponentInParent<T>() { return default(T); }
        public T[] GetComponentsInChildren<T>() { return new T[0]; } public T[] GetComponentsInChildren<T>(bool inactive) { return new T[0]; } public T GetComponentInChildren<T>() { return default(T); }
        public bool CompareTag(string t) { return true; }
    }
    public class Behaviour : Component { public bool enabled; public bool isActiveAndEnabled; }
    public class MonoBehaviour : Behaviour { public Coroutine StartCoroutine(IEnumerator e) { return null; } public void StopCoroutine(Coroutine c) {} public void StopAllCoroutines() {} }
    public class ScriptableObject : Object { public static T CreateInstance<T>() where T : ScriptableObject { return null; } }
    public class GameObject : Object {
        public GameObject(string n) {} public GameObject(string n, params Type[] c) {}
        public Transform transform; public int layer; public string tag; public bool isStatic; public bool activeSelf, activeInHierarchy;
        public T AddComponent<T>() where T : Component { return null; }
        public T GetComponent<T>() { return default(T); } public T GetComponentInChildren<T>() { return default(T); } public T[] GetComponentsInChildren<T>(bool inactive) { return new T[0]; } public UnityEngine.SceneManagement.Scene scene; public Component GetComponent(Type t) { return null; } public Component AddComponent(Type t) { return null; }
        public void SetActive(bool b) { activeSelf = b; }
        public static GameObject Find(string n) { return null; }
        public static GameObject FindGameObjectWithTag(string t) { return null; }
        public static GameObject CreatePrimitive(PrimitiveType t) { return null; }
    }
    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }
    public enum Space { World, Self }
    public enum FindObjectsSortMode { None, InstanceID }
    public class Transform : Component, IEnumerable {
        public Vector3 lossyScale; public Vector3 position, localPosition, localScale, eulerAngles, localEulerAngles, forward, right, up;
        public Quaternion rotation, localRotation; public Transform parent; public int childCount; public Matrix4x4 localToWorldMatrix;
        public void SetParent(Transform p) { parent = p; } public void SetParent(Transform p, bool w) { parent = p; }
        public void Rotate(Vector3 e) {} public void Rotate(float x, float y, float z) {} public void Rotate(Vector3 e, Space s) {} public void Rotate(Vector3 a, float ang) {} public void Rotate(Vector3 a, float ang, Space s) {}
        public Transform GetChild(int i) { return null; } public bool IsChildOf(Transform t) { var c = this; while (c != null) { if (c == t) return true; c = c.parent; } return false; }
        public void SetPositionAndRotation(Vector3 p, Quaternion r) {} public Vector3 InverseTransformDirection(Vector3 d) { return d; } public Vector3 TransformPoint(Vector3 p) { return p; } public Vector3 TransformDirection(Vector3 d) { return d; } public Vector3 InverseTransformPoint(Vector3 p) { return p; } public void LookAt(Vector3 p) {} public void SetSiblingIndex(int i) {} public Transform Find(string n) { return null; }
        public IEnumerator GetEnumerator() { return null; }
    }
    public class RectTransform : Transform { public Vector2 anchorMin, anchorMax, anchoredPosition, sizeDelta, pivot, offsetMin, offsetMax; public Rect rect; }
    public class ExecuteAlways : Attribute {}
    [Flags] public enum HideFlags { None = 0, HideInHierarchy = 1, HideInInspector = 2, DontSave = 52 }
    public class CanvasGroup : Behaviour { public float alpha = 1f; public bool interactable = true, blocksRaycasts = true; }
    public struct Matrix4x4 {}
    public class Vector2 { public Vector2() {}
        public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero, one; public float magnitude;
        public static Vector2 operator *(Vector2 a, float k) { return new Vector2(a.x * k, a.y * k); } public static Vector2 operator +(Vector2 a, Vector2 b) { return new Vector2(a.x + b.x, a.y + b.y); }
        public static Vector2 ClampMagnitude(Vector2 v, float m) { return v; }
    }
    public class Vector3 { public Vector3() {}
        public float x, y, z; public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero, one, up, down, forward, right; public static bool operator ==(Vector3 a, Vector3 b) { return true; } public static bool operator !=(Vector3 a, Vector3 b) { return false; } public float magnitude, sqrMagnitude; public Vector3 normalized;
        public static Vector3 operator +(Vector3 a, Vector3 b) { return a; } public static Vector3 operator -(Vector3 a, Vector3 b) { return a; }
        public static Vector3 operator -(Vector3 a) { return a; }
        public static Vector3 operator *(Vector3 a, float b) { return a; } public static Vector3 operator /(Vector3 a, float b) { return a; } public static Vector3 operator *(float b, Vector3 a) { return a; }
        public static Vector3 MoveTowards(Vector3 a, Vector3 b, float d) { return a; }
        public static float Distance(Vector3 a, Vector3 b) { return 0; } public static float Angle(Vector3 a, Vector3 b) { return 0; }
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) { return a; }
    }
    public class Quaternion {
        public static Quaternion identity; public static Quaternion Euler(float x, float y, float z) { return identity; }
        public static Quaternion Euler(Vector3 v) { return identity; }
        public static Quaternion AngleAxis(float a, Vector3 ax) { return identity; }
        public static Quaternion LookRotation(Vector3 f) { return identity; } public static Quaternion LookRotation(Vector3 f, Vector3 u) { return identity; }
        public static Quaternion RotateTowards(Quaternion a, Quaternion b, float d) { return a; }
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) { return a; }
        public static Quaternion operator *(Quaternion a, Quaternion b) { return a; }
        public static Vector3 operator *(Quaternion a, Vector3 b) { return b; }
    }
    public struct Color {
        public float r, g, b, a; public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1; }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white, black, red, cyan, green, yellow, gray;
        public static Color Lerp(Color a, Color b, float t) { return a; }
        public static Color operator *(Color a, float f) { return a; }
    }
    public static class ColorUtility { public static string ToHtmlStringRGB(Color c) { return ""; } public static bool TryParseHtmlString(string s, out Color c) { c = Color.white; return true; } }
    public struct Bounds { public Vector3 center, extents, min, max, size; public Bounds(Vector3 c, Vector3 s) { center = c; extents = s; min = c; max = c; size = s; } public bool Intersects(Bounds b) { return false; } }
    public struct Rect { public float xMin, xMax, yMin, yMax; public float width { get { return xMax - xMin; } } public float height { get { return yMax - yMin; } } public Rect(float x, float y, float w, float h) { xMin=x; yMin=y; xMax=x+w; yMax=y+h; } public bool Contains(Vector2 p) { return false; } }
    public struct Vector2Int { public int x, y; public Vector2Int(int x, int y) { this.x=x; this.y=y; } }
    public enum RuntimeInitializeLoadType { SubsystemRegistration, AfterSceneLoad, BeforeSceneLoad }
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) {} }
    public struct Ray { public Ray(Vector3 o, Vector3 d) {} }
    public struct RaycastHit { public Collider collider; public Transform transform; public Vector3 point; public float distance; public Vector3 normal; }
    public class Collision { }
    public enum ForceMode { Force, VelocityChange }
    public enum CollisionDetectionMode { Discrete, ContinuousDynamic }
    public class Rigidbody : Component { public float mass; public CollisionDetectionMode collisionDetectionMode; public void AddForce(Vector3 f, ForceMode m) {} }
    public struct Vector4 { public float x, y, z, w; public Vector4(float x, float y, float z, float w) { this.x=x; this.y=y; this.z=z; this.w=w; } }
    public struct LayerMask { public static implicit operator int(LayerMask m) { return 0; } public static implicit operator LayerMask(int i) { return new LayerMask(); } }
    public enum QueryTriggerInteraction { UseGlobal, Ignore, Collide }
    public static class Physics { public static Vector3 gravity; public static RaycastHit[] RaycastAll(Ray r, float d, int m, QueryTriggerInteraction q) { return new RaycastHit[0]; } public static void IgnoreCollision(Collider a, Collider b) {}
        public static bool Raycast(Ray r, out RaycastHit h, float d, int m, QueryTriggerInteraction q) { h = default(RaycastHit); return false; }
        public static bool SphereCast(Vector3 o, float r, Vector3 d, out RaycastHit h, float dist, int m, QueryTriggerInteraction q) { h = default(RaycastHit); return false; }
        public static bool Linecast(Vector3 a, Vector3 b, out RaycastHit h, int m, QueryTriggerInteraction q) { h = default(RaycastHit); return false; }
        public static int RaycastNonAlloc(Vector3 o, Vector3 d, RaycastHit[] r, float dist, int m, QueryTriggerInteraction q) { return 0; }
    }
    public static class Mathf {
        public const float PI = 3.14f;
        public static float Clamp(float v, float a, float b) { return v; } public static int Clamp(int v, int a, int b) { return v; }
        public static float Clamp01(float v) { return v; } public static float Lerp(float a, float b, float t) { return a; }
        public static float InverseLerp(float a, float b, float v) { return a; } public static float MoveTowards(float a, float b, float d) { return a; }
        public static float Min(float a, float b) { return Math.Min(a,b); } public static int Min(int a, int b) { return Math.Min(a,b); }
        public static float Max(float a, float b) { return Math.Max(a,b); } public static float Exp(float x) { return (float)Math.Exp(x); } public static int Max(int a, int b) { return Math.Max(a,b); }
        public static float Sin(float a) { return a; } public static float PerlinNoise(float a, float b) { return a; }
        public static float SmoothStep(float a, float b, float t) { return a; }
        public static int FloorToInt(float f) { return 0; } public static int RoundToInt(float f) { return 0; } public const float Rad2Deg = 57.3f; public static float Atan2(float y, float x) { return 0; } public static float DeltaAngle(float a, float b) { return 0; } public static float Repeat(float t, float l) { return 0; } public static float PingPong(float t, float l) { return 0; } public static float Abs(float f) { return f; } public static float Sign(float f) { return 1; } public static float Sqrt(float f) { return f; } public static float Cos(float f) { return f; } public static float Ceil(float f) { return f; } public static int CeilToInt(float f) { return 0; }
    }
    public static class Random { public static Vector3 insideUnitSphere; public static float value; public static float Range(float a, float b) { return a; } public static int Range(int a, int b) { return a; } }
    public static class Time { public static float unscaledTime, deltaTime, time, timeScale, unscaledDeltaTime, realtimeSinceStartup; public static int frameCount; }
    public enum KeyCode { None, R, M, Space, V, F, H, Mouse0, Mouse1, Alpha4, Alpha5, Alpha6, Alpha7, Alpha8, Alpha9, JoystickButton7, JoystickButton9, E, C, I, G, Q, W, S, A, D, LeftArrow, RightArrow, JoystickButton2, JoystickButton3, Tab, Escape, Return, Backspace, UpArrow, DownArrow, Alpha1, Alpha2, Alpha3, JoystickButton4, JoystickButton5, LeftShift, LeftControl, JoystickButton0, JoystickButton1, JoystickButton6, JoystickButton8 }
    public static class Input {
        public static float GetAxis(string a) { return 0; } public static float GetAxisRaw(string a) { return 0; }
        public static bool GetKey(KeyCode k) { return false; } public static bool GetKeyDown(KeyCode k) { return false; }
        public static bool GetMouseButtonDown(int b) { return false; }
    }
    public enum CursorLockMode { None, Locked, Confined }
    public static class Cursor { public static CursorLockMode lockState; public static bool visible; }
    public static class Debug {
        public static void Log(object o) {} public static void LogWarning(object o) {} public static void LogWarning(object o, Object c) {}
        public static void LogError(object o, Object c) {} public static void LogError(object o) {} public static void LogException(Exception e) {}
    }
    public class Collider : Component { public bool isTrigger; public bool enabled; public Bounds bounds; }
    public class BoxCollider : Collider { public Vector3 center, size; }
    public class CapsuleCollider : Collider { public Vector3 center; public float height, radius; }
    [System.Flags] public enum CollisionFlags { None = 0, Sides = 1, Above = 2, Below = 4 }
    public class CharacterController : Collider {
        public float height, radius, stepOffset; public Vector3 center, velocity; public bool isGrounded;
        public CollisionFlags Move(Vector3 m) { return CollisionFlags.None; }
    }
    public class Shader : Object { public static Shader Find(string n) { return null; } public static int PropertyToID(string n) { return 0; } }
    public enum MaterialGlobalIlluminationFlags { None }
    public class Material : Object {
        public Material(Shader s) {} public Material(Material m) {} public Color color; public Shader shader; public MaterialGlobalIlluminationFlags globalIlluminationFlags;
        public void SetColor(string n, Color c) {} public void SetFloat(string n, float f) {} public bool HasProperty(string n) { return true; }
        public void EnableKeyword(string k) {} public void DisableKeyword(string k) {} public Texture mainTexture; public int renderQueue;
        public bool HasProperty(int id) { return true; } public Color GetColor(int id) { return new Color(); } public Color GetColor(string n) { return new Color(); }
        public void SetTexture(string n, Texture t) {} public Texture GetTexture(string n) { return null; } public void SetTextureScale(string n, Vector2 s) {} public float GetFloat(string n) { return 0f; } public void SetInt(string n, int v) {}
        public bool IsKeywordEnabled(string k) { return false; }
    }
    public class MaterialPropertyBlock { public void SetColor(int id, Color c) {} public void SetColor(string n, Color c) {} public void SetVector(int id, Vector4 v) {} public void SetTexture(int id, Texture t) {} }
    public class Renderer : Component { public bool enabled, receiveShadows; public Bounds bounds; public Rendering.ShadowCastingMode shadowCastingMode;
        public Material material, sharedMaterial;
        public void GetPropertyBlock(MaterialPropertyBlock b) {} public void SetPropertyBlock(MaterialPropertyBlock b) {}
    }
    public enum LightType { Spot, Directional, Point }
    public enum LightShadows { None, Hard, Soft }
    public class Light : Behaviour { public LightType type; public Color color; public float range, intensity, spotAngle, shadowStrength, shadowBias, shadowNormalBias; public LightShadows shadows; }
    public enum CameraClearFlags { Skybox, SolidColor }
    public class Camera : Behaviour { public static Camera main; public bool allowHDR; public float farClipPlane, nearClipPlane, fieldOfView; public CameraClearFlags clearFlags; public Color backgroundColor; }
    public class AudioListener : Behaviour { public static float volume = 1f; }
    public enum FullScreenMode { ExclusiveFullScreen, FullScreenWindow, MaximizedWindow, Windowed }
    public static class Screen { public static FullScreenMode fullScreenMode; public static bool fullScreen; public static int width = 1920, height = 1080; }
    public static class QualitySettings { public static string[] names = { "Mobile", "PC" }; static int level = 1; public static int GetQualityLevel() { return level; } public static void SetQualityLevel(int i, bool e) { level = i; } }
    public class AudioClip : Object {}
    public class AudioSource : Behaviour {
        public bool playOnAwake, loop, isPlaying; public float spatialBlend, pitch, volume; public void Play() {} public void Stop() {}
        public void PlayOneShot(AudioClip c) {} public void PlayOneShot(AudioClip c, float v) {}
        public static void PlayClipAtPoint(AudioClip c, Vector3 p) {}
    }
    public static class Gizmos {
        public static Color color; public static Matrix4x4 matrix;
        public static void DrawWireSphere(Vector3 c, float r) {} public static void DrawSphere(Vector3 c, float r) {}
        public static void DrawCube(Vector3 c, Vector3 s) {} public static void DrawWireCube(Vector3 c, Vector3 s) {}
        public static void DrawRay(Vector3 o, Vector3 d) {} public static void DrawLine(Vector3 a, Vector3 b) {}
    }
    public static class Application { public static bool isPlaying, isBatchMode; public static void Quit() {} public static bool CanStreamedLevelBeLoaded(string s) { return true; } }
    public enum FogMode { Linear, Exponential, ExponentialSquared }
    public static class RenderSettings {
        public static Material skybox; public static Rendering.AmbientMode ambientMode; public static Color ambientLight, fogColor;
        public static bool fog; public static FogMode fogMode; public static float fogDensity, fogStartDistance, fogEndDistance;
    }
    public class Sprite : Object { public Texture2D texture; public static Sprite Create(Texture2D t, Rect r, Vector2 pivot, float ppu) { return new Sprite { texture = t }; } }
    public enum RenderMode { ScreenSpaceOverlay }
    public class Canvas : Behaviour { public RenderMode renderMode; public int sortingOrder; }
    public enum TextAnchor { LowerRight, MiddleCenter }
    public class YieldInstruction {} public class Coroutine : YieldInstruction {}
    public class WaitForSeconds : YieldInstruction { public WaitForSeconds(float s) {} }

    public class SerializeField : Attribute {}
    public class HeaderAttribute : Attribute { public HeaderAttribute(string h) {} }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string h) {} }
    public class RangeAttribute : Attribute { public RangeAttribute(float a, float b) {} }
    public class MinAttribute : Attribute { public MinAttribute(float a) {} }
    public class TextAreaAttribute : Attribute { public TextAreaAttribute() {} public TextAreaAttribute(int a, int b) {} }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)] public class RequireComponent : Attribute { public RequireComponent(Type t) {} }
    public class CreateAssetMenuAttribute : Attribute { public string menuName, fileName; }
    public class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int o) {} }
}
namespace UnityEngine {
    public struct MinMaxCurve { public MinMaxCurve(float a, float b) {} public MinMaxCurve(float m, AnimationCurve c) {} public static implicit operator MinMaxCurve(float f) { return new MinMaxCurve(); } }
    public struct MinMaxGradient { public MinMaxGradient(Gradient g) {} public static implicit operator MinMaxGradient(Color c) { return new MinMaxGradient(); } }
    public struct Keyframe { public Keyframe(float t, float v) {} }
    public class AnimationCurve { public AnimationCurve(params Keyframe[] k) {} public static AnimationCurve Linear(float t0, float v0, float t1, float v1) { return new AnimationCurve(); } }
    public struct GradientColorKey { public GradientColorKey(Color c, float t) {} }
    public struct GradientAlphaKey { public GradientAlphaKey(float a, float t) {} }
    public class Gradient { public void SetKeys(GradientColorKey[] c, GradientAlphaKey[] a) {} }
    public enum ParticleSystemSimulationSpace { Local, World }
    public enum ParticleSystemShapeType { Sphere, Cone, Box }
    public enum ParticleSystemStopBehavior { StopEmittingAndClear, StopEmitting }
    public enum ParticleSystemRenderMode { Billboard, Stretch, HorizontalBillboard, VerticalBillboard, Mesh }
    public class ParticleSystem : Component {
        public struct MinMaxCurve { public MinMaxCurve(float a, float b) {} public MinMaxCurve(float m, AnimationCurve c) {} public static implicit operator MinMaxCurve(float f) { return new MinMaxCurve(); } public static implicit operator UnityEngine.MinMaxCurve(MinMaxCurve c) { return new UnityEngine.MinMaxCurve(); } }
        public struct MinMaxGradient { public MinMaxGradient(Gradient g) {} public static implicit operator MinMaxGradient(Color c) { return new MinMaxGradient(); } public static implicit operator UnityEngine.MinMaxGradient(MinMaxGradient c) { return new UnityEngine.MinMaxGradient(); } }
        public struct MainModule { public bool loop { get; set; } public bool playOnAwake { get; set; } public bool prewarm { get; set; } public UnityEngine.MinMaxCurve startLifetime { get; set; } public UnityEngine.MinMaxCurve startSpeed { get; set; } public UnityEngine.MinMaxCurve startSize { get; set; } public UnityEngine.MinMaxCurve gravityModifier { get; set; } public UnityEngine.MinMaxGradient startColor { get; set; } public int maxParticles { get; set; } public ParticleSystemSimulationSpace simulationSpace { get; set; } }
        public struct EmissionModule { public bool enabled { get; set; } public UnityEngine.MinMaxCurve rateOverTime { get; set; } }
        public struct ShapeModule { public ParticleSystemShapeType shapeType { get; set; } public float radius { get; set; } public float angle { get; set; } public Vector3 scale { get; set; } }
        public struct SizeOverLifetimeModule { public bool enabled { get; set; } public UnityEngine.MinMaxCurve size { get; set; } }
        public struct ColorOverLifetimeModule { public bool enabled { get; set; } public UnityEngine.MinMaxGradient color { get; set; } }
        public struct NoiseModule { public bool enabled { get; set; } public UnityEngine.MinMaxCurve strength { get; set; } public float frequency { get; set; } }
        public MainModule main; public EmissionModule emission; public ShapeModule shape; public SizeOverLifetimeModule sizeOverLifetime; public ColorOverLifetimeModule colorOverLifetime; public NoiseModule noise;
        public void Emit(int n) {} public bool isPlaying; public void Play() {} public void Stop(bool children, ParticleSystemStopBehavior b) {}
    }
    public class MeshRenderer : Renderer {}
    public class MeshFilter : Component {}
    public class ParticleSystemRenderer : Renderer { public ParticleSystemRenderMode renderMode; public float velocityScale, lengthScale; }
    public class LineRenderer : Renderer { public bool useWorldSpace, loop; public Color startColor, endColor; public float widthMultiplier; public int positionCount; public void SetPosition(int i, Vector3 p) {} }
}
namespace UnityEngine.Events { public class UnityEvent { public void Invoke() {} } }
namespace UnityEngine.SceneManagement {
    public struct Scene { public string path; public string name; public GameObject[] GetRootGameObjects() { return new GameObject[0]; } }
    public enum LoadSceneMode { Single, Additive }
    public static class SceneManager { public static void LoadScene(string n) {} public static event System.Action<Scene, LoadSceneMode> sceneLoaded; public static Scene GetActiveScene() { return default(Scene); } }
}
namespace UnityEngine.Rendering {
    public enum AmbientMode { Skybox, Trilight, Flat } public enum ShadowCastingMode { Off, On, TwoSided, ShadowsOnly }
    public static class GraphicsSettings { public static Object currentRenderPipeline; }
}
namespace UnityEngine.AI {
    public class NavMeshAgent : Behaviour {
        public float speed, radius, height, angularSpeed, acceleration, stoppingDistance, remainingDistance;
        public bool isOnNavMesh, pathPending, isStopped;
        public Vector3 destination; public int destinationsSet; public bool Warp(Vector3 p) { return true; } public bool SetDestination(Vector3 p) { destination = p; destinationsSet++; return true; } public void ResetPath() {}
    }
    public struct NavMeshHit { public Vector3 position; }
    public struct NavMeshBuildSettings {}
    public struct NavMeshBuildSource {}
    public struct NavMeshBuildMarkup { public Transform root; public bool ignoreFromBuild; }
    public class NavMeshData : Object {}
    public struct NavMeshDataInstance { public bool valid; public void Remove() {} }
    public enum NavMeshCollectGeometry { RenderMeshes, PhysicsColliders }
    public static class NavMesh {
        public const int AllAreas = -1;
        public static bool SamplePosition(Vector3 p, out NavMeshHit h, float d, int m) { h = new NavMeshHit { position = p }; return true; }
        public static bool Raycast(Vector3 a, Vector3 b, out NavMeshHit h, int m) { h = new NavMeshHit { position = b }; return false; }
        public static NavMeshBuildSettings GetSettingsByID(int id) { return default(NavMeshBuildSettings); }
        public static NavMeshDataInstance AddNavMeshData(NavMeshData d) { return default(NavMeshDataInstance); }
    }
    public static class NavMeshBuilder {
        public static void CollectSources(Bounds b, int mask, NavMeshCollectGeometry g, int area, List<NavMeshBuildMarkup> m, List<NavMeshBuildSource> r) {}
        public static NavMeshData BuildNavMeshData(NavMeshBuildSettings s, List<NavMeshBuildSource> src, Bounds b, Vector3 p, Quaternion q) { return null; }
    }
}
namespace UnityEngine.UI {
    public class CanvasRenderer : Component { public float alpha = 1f; public void SetAlpha(float a) { alpha = a; } }
    public class Graphic : Behaviour { public Color color; public bool raycastTarget; public RectTransform rectTransform; public CanvasRenderer canvasRenderer = new CanvasRenderer(); }
    public class Image : Graphic { public Sprite sprite; public bool preserveAspect; public enum Type { Simple, Sliced, Tiled, Filled } public Type type; }
    public class RawImage : Graphic { public Texture texture; public Rect uvRect; }
    public struct ColorBlock { public Color normalColor, highlightedColor, pressedColor, selectedColor, disabledColor; public float colorMultiplier, fadeDuration; public static ColorBlock defaultColorBlock; }
    public class CanvasScaler : Behaviour { public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize } public ScaleMode uiScaleMode; public Vector2 referenceResolution; public float matchWidthOrHeight; }
    public class GraphicRaycaster : Behaviour {}
    public class VerticalLayoutGroup : Behaviour { public TextAnchor childAlignment; public float spacing; public bool childControlWidth, childControlHeight, childForceExpandWidth, childForceExpandHeight; }
}
namespace TMPro {
    public enum TextAlignmentOptions { Center, TopLeft, Right, Left, Top }
    public enum FontStyles { Normal, Bold, Italic }
    public class TMP_TextInfo { public int characterCount; }
    public class TextMeshPro : UnityEngine.MonoBehaviour { public string text; public float fontSize; public TextAlignmentOptions alignment; public UnityEngine.Color color; public UnityEngine.RectTransform rectTransform; }
    public class TextMeshProUGUI : UnityEngine.UI.Graphic {
        public FontStyles fontStyle; public bool enableWordWrapping, enableAutoSizing; public float fontSizeMax, fontSizeMin; public string text; public int maxVisibleCharacters; public float fontSize; public TextAlignmentOptions alignment; public TMP_TextInfo textInfo;
        public void ForceMeshUpdate() {}
    }
}
namespace UnityEditor {
    using UnityEngine;
    public class MenuItem : Attribute { public MenuItem(string p) {} public MenuItem(string p, bool v) {} public MenuItem(string p, bool v, int prio) {} }
    public static class Menu { public static void SetChecked(string p, bool c) {} }
    public static class EditorPrefs { public static bool GetBool(string k, bool d) { return d; } public static void SetBool(string k, bool v) {} }
    public class SceneAsset : Object {}
    public class InitializeOnLoadAttribute : Attribute {}
    public static class EditorApplication { public static Action delayCall; public static bool isPlayingOrWillChangePlaymode; }
    public static class SessionState { public static bool GetBool(string k, bool d) { return d; } public static void SetBool(string k, bool v) {} }
    public static class AssetDatabase {
        public static bool IsValidFolder(string p) { return true; } public static string CreateFolder(string a, string b) { return ""; }
        public static T LoadAssetAtPath<T>(string p) where T : Object { return null; } public static void CreateAsset(Object o, string p) {}
        public static void SaveAssets() {}
        public static string[] FindAssets(string f, string[] folders) { return new string[0]; } public static string GUIDToAssetPath(string g) { return g; }
        public static void ImportAsset(string p, ImportAssetOptions o) {} public static void AddObjectToAsset(Object o, Object a) {}
    }
    public enum ImportAssetOptions { Default, ForceUpdate }
    public class AssetImporter : Object { public static AssetImporter GetAtPath(string p) { return null; } public void SaveAndReimport() {} }
    public enum TextureImporterType { Default, NormalMap }
    public class TextureImporter : AssetImporter { public TextureImporterType textureType; public bool sRGBTexture; public TextureWrapMode wrapMode; public int anisoLevel; }
    public enum SerializedPropertyType { Generic, Integer, ObjectReference }
    public static class EditorUtility { public static void SetDirty(Object o) {} public static bool DisplayDialog(string t, string m, string ok, string cancel) { return false; } public static bool DisplayDialog(string t, string m, string ok) { return true; } public static void DisplayProgressBar(string t, string i, float p) {} public static void ClearProgressBar() {} }
    public enum InteractionMode { AutomatedAction, UserAction }
    public static class PrefabUtility { public static GameObject SaveAsPrefabAssetAndConnect(GameObject g, string p, InteractionMode m) { return null; } }
    public class EditorBuildSettingsScene { public string path; public EditorBuildSettingsScene(string p, bool e) {} }
    public static class EditorBuildSettings { public static EditorBuildSettingsScene[] scenes; }
    public static class Selection { public static GameObject activeGameObject; }
    public class SerializedProperty {
        public SerializedPropertyType propertyType; public bool Next(bool enter) { return false; }
        public Object objectReferenceValue; public string stringValue; public int intValue, enumValueIndex, arraySize; public float floatValue;
        public bool boolValue; public Color colorValue; public Vector3 vector3Value; public Vector2 vector2Value;
        public SerializedProperty GetArrayElementAtIndex(int i) { return null; } public SerializedProperty FindPropertyRelative(string n) { return null; }
    }
    public class SerializedObject : IDisposable { public void Dispose() {} public SerializedObject(Object o) {} public SerializedProperty FindProperty(string n) { return null; } public SerializedProperty GetIterator() { return new SerializedProperty(); } public bool ApplyModifiedPropertiesWithoutUndo() { return true; } }
}
namespace UnityEditor.SceneManagement {
    using UnityEngine.SceneManagement;
    public enum NewSceneSetup { EmptyScene, DefaultGameObjects }
    public enum NewSceneMode { Single, Additive }
    public enum OpenSceneMode { Single, Additive }
    public static class EditorSceneManager {
        public static bool SaveCurrentModifiedScenesIfUserWantsTo() { return true; }
        public static Scene NewScene(NewSceneSetup s, NewSceneMode m) { return default(Scene); }
        public static bool SaveScene(Scene s, string p) { return true; } public static bool SaveScene(Scene s) { return true; }
        public static Scene OpenScene(string p, OpenSceneMode m) { return default(Scene); }
        public static Scene GetActiveScene() { return default(Scene); }
        public static bool MarkSceneDirty(Scene s) { return true; }
        public static SceneAsset playModeStartScene;
    }
}

namespace UnityEngine {
    public enum FilterMode { Point, Bilinear, Trilinear }
    public class Texture : Object { public TextureWrapMode wrapMode; public FilterMode filterMode; public int width, height; }
    public enum TextureFormat { RGBA32, ARGB32 }
    public enum TextureWrapMode { Repeat, Clamp }
    public class Texture2D : Texture {
        public Texture2D(int w, int h, TextureFormat f, bool mip) { width = w; height = h; }
        public void SetPixel(int x, int y, Color c) {} public void SetPixels(Color[] c) {} public void Apply() {} public byte[] EncodeToPNG() { return new byte[0]; }
    }
    public static class PlayerPrefs {
        static System.Collections.Generic.Dictionary<string,object> d = new System.Collections.Generic.Dictionary<string,object>();
        public static int GetInt(string k, int def) { object v; return d.TryGetValue(k, out v) ? (int)v : def; }
        public static string GetString(string k, string def) { object v; return d.TryGetValue(k, out v) ? (string)v : def; }
        public static float GetFloat(string k, float def) { object v; return d.TryGetValue(k, out v) ? (float)v : def; } public static void SetFloat(string k, float v) { d[k] = v; }
        public static bool HasKey(string k) { return d.ContainsKey(k); } public static void DeleteKey(string k) { d.Remove(k); }
        public static void SetInt(string k, int v) { d[k] = v; } public static void SetString(string k, string v) { d[k] = v; } public static void Save() {}
    }
}
