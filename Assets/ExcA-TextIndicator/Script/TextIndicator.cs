using System;
using UnityEngine;

public class TextIndicator : MonoBehaviour
{
    [SerializeField] private SlotText slotText;
    [SerializeField] private Transform spawnContent;
    public static TextIndicator instance;
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(this.gameObject);
    }

    public static void Display(string textToDisplay)
    {
        SlotText _slotText = Instantiate(instance.slotText,instance.spawnContent);
        _slotText.Init(textToDisplay);
    }
}
