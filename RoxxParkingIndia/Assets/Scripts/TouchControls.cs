using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RoxxParking
{
    public sealed class HoldControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Action<bool> Changed;
        private int pointer = int.MinValue;
        public void OnPointerDown(PointerEventData e)
        {
            if (pointer != int.MinValue) return;
            pointer = e.pointerId;
            Changed?.Invoke(true);
        }
        public void OnPointerUp(PointerEventData e)
        {
            if (pointer != e.pointerId) return;
            pointer = int.MinValue;
            Changed?.Invoke(false);
        }
        private void OnDisable()
        {
            pointer = int.MinValue;
            Changed?.Invoke(false);
        }
    }

    public sealed class SteeringControl : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Action<float> Changed;
        private int pointer = int.MinValue;
        private float lastAngle;
        private float angle;
        public void OnPointerDown(PointerEventData e)
        {
            if (pointer != int.MinValue) return;
            pointer = e.pointerId;
            lastAngle = PointerAngle(e);
        }
        public void OnDrag(PointerEventData e)
        {
            if (pointer != e.pointerId) return;
            float next = PointerAngle(e);
            angle = Mathf.Clamp(angle + Mathf.DeltaAngle(lastAngle, next), -125, 125);
            lastAngle = next;
            Changed?.Invoke(-angle / 125f);
        }
        public void OnPointerUp(PointerEventData e)
        {
            if (pointer == e.pointerId) ResetInput();
        }
        private float PointerAngle(PointerEventData e)
        {
            Vector2 center = RectTransformUtility.WorldToScreenPoint(e.pressEventCamera, transform.position);
            Vector2 point = e.position - center;
            if (point.sqrMagnitude < 400) return lastAngle;
            return Mathf.Atan2(point.y, point.x) * Mathf.Rad2Deg;
        }
        private void ResetInput()
        {
            pointer = int.MinValue; angle = 0;
            Changed?.Invoke(0);
        }
        private void OnDisable() => ResetInput();
    }
}
