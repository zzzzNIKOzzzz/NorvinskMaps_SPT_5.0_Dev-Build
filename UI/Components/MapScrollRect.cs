using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace NorvinskMaps.UI.Components
{
    public class MapScrollRect : ScrollRect
    {
        public System.Action OnBeginDragCallback;
        public override void OnBeginDrag(PointerEventData eventData)
        {
            try
            {
                base.OnBeginDrag(eventData);
                OnBeginDragCallback?.Invoke();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Ошибка в MapScrollRect.OnBeginDrag: {ex.Message}");
            }
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            try
            {
                base.OnEndDrag(eventData);
                
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Ошибка в MapScrollRect.OnEndDrag: {ex.Message}");
            }
        }

        public override void OnDrag(PointerEventData eventData)
        {
            try
            {
                base.OnDrag(eventData);
                
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Ошибка в MapScrollRect.OnDrag: {ex.Message}");
            }
        }
    }
}
