using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets._vGISComponents.Components.InputModule.Gestures.Scripts.Dispatchers
{
    /// <summary>
    /// Re-dispatches drag events to another GameObject's handlers.
    ///
    /// Unity's EventSystem only delivers <see cref="IBeginDragHandler"/>/<see cref="IDragHandler"/>/
    /// <see cref="IEndDragHandler"/> to the nearest ancestor (starting at the raycast-hit object)
    /// that implements them, so a closer drag handler — e.g. a <see cref="UnityEngine.UI.ScrollRect"/>
    /// on a child — prevents a gesture recognizer higher up the hierarchy (such as
    /// <see cref="SwipeGesture"/>) from ever receiving the same drag.
    ///
    /// Add this component to the same GameObject as the child drag handler that is "winning"
    /// the event (e.g. the ScrollRect) and point <see cref="_target"/> at the ancestor GameObject
    /// that should also react to the drag. Both handlers then run independently off the same
    /// pointer data.
    /// </summary>
    public class DragEventsDispatcher : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField]
        [Tooltip("GameObject whose drag handlers should also receive this drag.")]
        private GameObject _target;

        public void OnBeginDrag(PointerEventData eventData) =>
            ExecuteEvents.Execute(_target, eventData, ExecuteEvents.beginDragHandler);

        public void OnDrag(PointerEventData eventData) =>
            ExecuteEvents.Execute(_target, eventData, ExecuteEvents.dragHandler);

        public void OnEndDrag(PointerEventData eventData) =>
            ExecuteEvents.Execute(_target, eventData, ExecuteEvents.endDragHandler);
    }
}
