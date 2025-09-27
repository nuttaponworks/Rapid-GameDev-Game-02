using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UI_BossStat : MonoBehaviour
{
    [SerializeField] private BossController boss;
    [FormerlySerializedAs("sliderStamina")]
    [Header("UI")]
    [SerializeField] private Slider sliderHealth;
    private void Awake()
    {
    }

    private void Start()
    {
        boss = GameStateManager.Instance.bossController;
        if (boss != null) sliderHealth.maxValue = boss.maxHP;
    }

    private void Update()
    {
        if (boss != null) sliderHealth.value  = boss.currentHP;
    }
}
