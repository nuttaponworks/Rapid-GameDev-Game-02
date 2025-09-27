using System;
using System.Collections.Generic;
using TarodevController;
using UnityEngine;
using UnityEngine.UI;
public class UI_PlayerStat : MonoBehaviour
{
    [Header("Stamina")]
    [SerializeField] private Slider sliderStamina;

    [Header("Hearts")]
    [SerializeField] private Transform heartParent;
    [SerializeField] private GameObject heartTemplate; // prefab 1 ชิ้น
    private readonly List<GameObject> _hearts = new();

    private PlayerStat _stat;
    private PlayerController _pc;

    void Start()
    {
        _stat = GameStateManager.Instance.playerStat;
        _pc = _stat.GetComponent<PlayerController>();

        if (_pc) { sliderStamina.maxValue = _pc.maxStamina; sliderStamina.value = _pc.stamina; }
        if (_stat)
        {
            EnsureHeartCount(_stat.playerHP);
            UpdateHeartsActive(_stat.playerHP);
            _stat.HpChanged += OnHpChanged;
        }
    }
    void OnDisable()
    {
        if (_stat) _stat.HpChanged -= OnHpChanged;
    }

    void Update() // อัปเดตแค่สแตมินา (หรือเปลี่ยนเป็น event-driven เหมือนกันก็ได้)
    {
        if (_pc) sliderStamina.value = _pc.stamina;
    }

    private void OnHpChanged(int hp)
    {
        EnsureHeartCount(hp);
        UpdateHeartsActive(hp);
    }

    private void EnsureHeartCount(int count)
    {
        // เพิ่ม
        while (_hearts.Count < count)
        {
            var go = Instantiate(heartTemplate, heartParent);
            go.SetActive(true);
            _hearts.Add(go);
        }
        // ลด (ลบจากท้าย)
        while (_hearts.Count > count)
        {
            var last = _hearts[^1];
            _hearts.RemoveAt(_hearts.Count - 1);
            if (last) Destroy(last);
        }
    }

    private void UpdateHeartsActive(int hp)
    {
        for (int i = 0; i < _hearts.Count; i++)
            if (_hearts[i]) _hearts[i].SetActive(i < hp);
    }
}
