using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_BossNotify : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject panelNotify;
    [SerializeField] private TMP_Text textElementName;
    [SerializeField] private Image iconColor;

    private void OnEnable()
    {
        BossController.OnElementChanged += HandleElementChanged;
    }

    private void OnDisable()
    {
        BossController.OnElementChanged -= HandleElementChanged;
    }

    private void HandleElementChanged(BossElementType elem)
    {
        string displayName;
        Color color;

        switch (elem)
        {
            case BossElementType.Fire:
                displayName = "Infrared";
                color = Color.red;
                break;
            case BossElementType.Water:
                displayName = "Cobalt";
                color = new Color(0.20f, 0.65f, 1.00f);
                break;
            case BossElementType.Grass:
                displayName = "Viridian";
                color = new Color(0.15f, 0.75f, 0.30f);
                break;
            case BossElementType.None:
            default:
                displayName = "Luminous";
                color = Color.white;
                break;
        }

        if (textElementName) textElementName.text = displayName;
        if (iconColor) iconColor.color = color;

        // แสดงทันที ไม่ต้องรอ และไม่มี auto-hide
        if (panelNotify) panelNotify.SetActive(true);
    }

    // เผื่อทดสอบจาก Inspector
    [ContextMenu("Test: Fire")]
    private void _TestFire() => HandleElementChanged(BossElementType.Fire);
}