using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_BossNotify : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject panelNotify;
    [SerializeField] private TMP_Text textElementName;
    [SerializeField] private Image iconColor;

    [Header("Behavior")]
    [SerializeField] private bool showPanelOnChange = true;
    [SerializeField] private float autoHideSeconds = 2.0f; // 0 = ไม่ซ่อนอัตโนมัติ

    private Coroutine _hideCo;

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
        // ชื่อ + สีตามสเปก
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
                color = new Color(0.20f, 0.65f, 1.00f); // ฟ้าโคบอลต์
                break;
            case BossElementType.Grass:
                displayName = "Viridian";
                color = new Color(0.15f, 0.75f, 0.30f); // เขียวอมฟ้า
                break;
            case BossElementType.None:
            default:
                displayName = "Luminous";
                color = Color.white;
                break;
        }

        if (textElementName) textElementName.text = displayName;
        if (iconColor) iconColor.color = color;

        if (panelNotify && showPanelOnChange)
        {
            panelNotify.SetActive(true);
            if (autoHideSeconds > 0f)
            {
                if (_hideCo != null) StopCoroutine(_hideCo);
                _hideCo = StartCoroutine(HideLater(autoHideSeconds));
            }
        }
    }

    private System.Collections.IEnumerator HideLater(float t)
    {
        yield return new WaitForSeconds(t);
        if (panelNotify) panelNotify.SetActive(false);
        _hideCo = null;
    }

    // เผื่อทดสอบจาก Inspector
    [ContextMenu("Test: Fire")]
    private void _TestFire() => HandleElementChanged(BossElementType.Fire);
}
