namespace UnityEngine.Events { public delegate void UnityAction(); }
namespace UnityEngine.UI {
    public struct Navigation { public enum Mode { None, Automatic } public Mode mode; }
    public class Selectable : Behaviour { public Graphic targetGraphic; public bool interactable; public Navigation navigation; }
    public class Button : Selectable {
        public class ButtonClickedEvent { public void AddListener(UnityEngine.Events.UnityAction a) {} public void RemoveAllListeners() {} }
        public ButtonClickedEvent onClick = new ButtonClickedEvent();
    }
}
namespace UnityEngine.EventSystems {
    public class EventSystem : UnityEngine.Behaviour { public static EventSystem current; public void SetSelectedGameObject(UnityEngine.GameObject g) {} }
    public class StandaloneInputModule : UnityEngine.Behaviour {}
}
namespace UnityEngine.AI {
    public enum NavMeshObstacleShape { Capsule, Box }
    public class NavMeshObstacle : UnityEngine.Behaviour { public NavMeshObstacleShape shape; public UnityEngine.Vector3 center, size; public bool carving, carveOnlyStationary; }
}
