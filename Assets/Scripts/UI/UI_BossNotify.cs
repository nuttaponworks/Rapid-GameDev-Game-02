using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_BossNotify : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject panelNotify;
    [SerializeField] private TMP_Text textElementName;
    [SerializeField] private Image iconColor;

    private void Start()
    {
        BossController.OnElementChanged += HandleElementChanged;
    }

    private void OnDisable()
    {
        BossController.OnElementChanged -= HandleElementChanged;
    }

    private void HandleElementChanged(BossElementType elem)
    {
        Debug.Log("Boss Element's changed on UIBOSSNOTIFY");
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
        AudioManager.instance.PlaySFX(6);

        // ทำให้ {display} มีสีตามธาตุ
        string hex = ColorUtility.ToHtmlStringRGB(color);               // e.g. FF0000
        string displayColored = $"<color=#{hex}>{displayName}</color>"; // {display}

        // ข้อความภาษาอังกฤษที่ต้องการ
        string message = $"Break the {displayColored} prism—boss takes 2× damage!";

        TextIndicator.Display(message);
    }


    // เผื่อทดสอบจาก Inspector
    [ContextMenu("Test: Fire")]
    private void _TestFire() => HandleElementChanged(BossElementType.Fire);
}