using TMPro;
using UnityEngine;

public class SlotText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textDisplay;
    [SerializeField] private float destroyDelay;
    
    public void Init(string textToDisplay)
    {
        textDisplay.text = textToDisplay;
        Destroy(gameObject,destroyDelay);
    }
}
