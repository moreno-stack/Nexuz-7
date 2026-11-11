namespace UnityEngine.Events { public delegate void UnityAction(); }
namespace UnityEngine.UI {
    public struct Navigation { public enum Mode { None, Automatic } public Mode mode; }
    public class Selectable : Behaviour { public Graphic targetGraphic; public bool interactable; public Navigation navigation; public ColorBlock colors; }
    public class Button : Selectable {
        public class ButtonClickedEvent { public void AddListener(UnityEngine.Events.UnityAction a) {} public void RemoveAllListeners() {} }
        public ButtonClickedEvent onClick = new ButtonClickedEvent();
    }
    public class Slider : Selectable {
        public enum Direction { LeftToRight, RightToLeft, BottomToTop, TopToBottom }
        public class SliderEvent { public void AddListener(System.Action<float> a) {} }
        public float minValue, maxValue, value; public bool wholeNumbers; public Direction direction;
        public UnityEngine.RectTransform fillRect, handleRect; public SliderEvent onValueChanged = new SliderEvent();
    }
}
namespace UnityEngine.EventSystems {
    public class EventSystem : UnityEngine.Behaviour { public static EventSystem current; public void SetSelectedGameObject(UnityEngine.GameObject g) {} }
    public class StandaloneInputModule : UnityEngine.Behaviour {}
    public class BaseEventData {}
    public enum EventTriggerType { PointerEnter, PointerExit, Select, Submit }
    public class EventTrigger : UnityEngine.Behaviour {
        public class TriggerEvent { public void AddListener(System.Action<BaseEventData> a) {} }
        public class Entry { public EventTriggerType eventID; public TriggerEvent callback = new TriggerEvent(); }
        public System.Collections.Generic.List<Entry> triggers = new System.Collections.Generic.List<Entry>();
    }
}
namespace UnityEngine.AI {
    public enum NavMeshObstacleShape { Capsule, Box }
    public class NavMeshObstacle : UnityEngine.Behaviour { public NavMeshObstacleShape shape; public UnityEngine.Vector3 center, size; public bool carving, carveOnlyStationary; }
}
