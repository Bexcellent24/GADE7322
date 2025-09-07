using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonAudio : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        AudioManager.Instance.PlaySFX("hover");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        AudioManager.Instance.PlaySFX("click");
    }
}