using System.Collections;
using UnityEngine;

public class BGTransitionManager : MonoBehaviour
{
    [Header("Debug Input (1=Red, 2=Green, 3=Blue)")]
    public bool enableDebugKeys = true;

    [Header("Sorting (all sprites share this layer)")]
    public string bgSortingLayerName = "BG";

    // --- Orders ---
    const int ORDER_DEFAULT = -2; // ปิด/ไม่ได้ใช้งาน
    const int ORDER_PREV_LO = -1; // สำหรับมาสก์ของ previous (back)
    const int ORDER_PREV    =  0; // previous (sprite)
    const int ORDER_CURR    =  1; // current (sprite)
    // current mask = 1/1
    // previous mask = -1/0
    // default mask = -2/-2

    [Header("Timing")]
    public float clearDelaySeconds = 5f;

    public enum ColorKey { Red, Green, Blue }

    [System.Serializable]
    public class ColorSet
    {
        [Header("Mask")]
        public GameObject maskGO;   // redMaskGameObject / greenMaskGameObject / blueMaskGameObject
        public SpriteMask mask;     // maskRed / maskGreen / maskBlue

        [Header("BG Root (children have multiple SpriteRenderers)")]
        public Transform bgRoot;    // พาเรนต์ของ BG สีนี้

        [HideInInspector] public SpriteRenderer[] bgAll;

        public void CacheChildren()
        {
            bgAll = bgRoot ? bgRoot.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];
        }

        public void SetBGOrder(int layerID, int order)
        {
            if (bgAll == null) return;
            for (int i = 0; i < bgAll.Length; i++)
            {
                var sr = bgAll[i];
                if (!sr) continue;
                sr.sortingLayerID = layerID;
                sr.sortingOrder   = order;
                sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            }
        }
    }

    [Header("Assign all 3 colors")]
    public ColorSet red;
    public ColorSet green;
    public ColorSet blue;

    // internal
    int _layerId;
    ColorKey? _current = null;
    ColorKey? _pendingReset = null;
    Coroutine _resetCo;

    // ====================== LIFECYCLE ======================
    void Awake()
    {
        _layerId = SortingLayer.NameToID(bgSortingLayerName);

        red.CacheChildren();
        green.CacheChildren();
        blue.CacheChildren();

        InitSet(red);
        InitSet(green);
        InitSet(blue);
    }

    void Start()
    {
        // สมัคร event ของบอส: Fire=Red, Water=Blue, Grass=Green, None=รีเซ็ตทั้งหมด
        BossController.OnElementChanged += HandleElementChanged;
    }

    void OnDisable()
    {
        BossController.OnElementChanged -= HandleElementChanged;
    }

    void Update()
    {
        if (!enableDebugKeys) return;
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchTo(ColorKey.Red);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchTo(ColorKey.Green);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchTo(ColorKey.Blue);
    }

    // ====================== EVENT HANDLER ======================
    void HandleElementChanged(BossElementType elem)
    {
        switch (elem)
        {
            case BossElementType.Fire:  SwitchTo(ColorKey.Red);   break;
            case BossElementType.Water: SwitchTo(ColorKey.Blue);  break;
            case BossElementType.Grass: SwitchTo(ColorKey.Green); break;
            case BossElementType.None:
            default:
                // รีเซ็ตทุกสีกลับค่า default
                ForceReset(ColorKey.Red);
                ForceReset(ColorKey.Green);
                ForceReset(ColorKey.Blue);
                _current = null;
                _pendingReset = null;
                if (_resetCo != null) { StopCoroutine(_resetCo); _resetCo = null; }
                break;
        }
    }

    // ====================== PUBLIC API (manual) ======================
    public void SwitchTo(ColorKey to)
    {
        // ถ้ามีตัวที่กำลังรอรีเซ็ต (ค้างอยู่ 0/-1..0) และไม่ใช่สีที่จะไป → รีเซ็ตทิ้ง
        if (_pendingReset.HasValue && _pendingReset.Value != to)
            ForceReset(_pendingReset.Value);

        if (!_current.HasValue)
        {
            PromoteToCurrent(to); // ครั้งแรก: sr=1, mask=1/1
            return;
        }
        if (_current.Value == to) return;

        // 1) ลด current → previous: sr=0, mask = back=-1 / front=0
        DemoteCurrentToPrevious();

        // 2) โปรโมตสีใหม่ → current: sr=1, mask=1/1
        PromoteToCurrent(to);

        // 3) จับเวลา ~5 วิ แล้วรีเซ็ต previous -> -2/-2 + inactive
        if (_resetCo != null) StopCoroutine(_resetCo);
        _pendingReset = GetPreviousCandidate(_current.Value);
        _resetCo = StartCoroutine(ResetPreviousAfterDelay(_pendingReset.Value));
    }

    // ====================== CORE STEPS ======================
    void PromoteToCurrent(ColorKey key)
    {
        var set = GetSet(key);
        SafeSetActive(set.maskGO, true);
        set.SetBGOrder(_layerId, ORDER_CURR);
        SetMaskRange(set.mask, _layerId, ORDER_CURR, ORDER_CURR); // 1/1
        _current = key;
    }

    void DemoteCurrentToPrevious()
    {
        if (!_current.HasValue) return;
        var set = GetSet(_current.Value);
        set.SetBGOrder(_layerId, ORDER_PREV);                         // sr = 0
        SetMaskRange(set.mask, _layerId, ORDER_PREV_LO, ORDER_PREV);  // mask = -1/0 (เผื่อ back)
        // GO ยังเปิดไว้ เพื่อค้างเอฟเฟกต์
    }

    IEnumerator ResetPreviousAfterDelay(ColorKey keyToReset)
    {
        float t = 0f;
        while (t < clearDelaySeconds) { t += Time.deltaTime; yield return null; }
        ForceReset(keyToReset);
        _pendingReset = null; _resetCo = null;
    }

    void ForceReset(ColorKey key)
    {
        var set = GetSet(key);
        set.SetBGOrder(_layerId, ORDER_DEFAULT);                          // sr = -2
        SetMaskRange(set.mask, _layerId, ORDER_DEFAULT, ORDER_DEFAULT);   // mask = -2/-2
        SafeSetActive(set.maskGO, false);
    }

    // ====================== SETUP / HELPERS ======================
    void InitSet(ColorSet set)
    {
        set.SetBGOrder(_layerId, ORDER_DEFAULT); // sr = -2
        if (set.mask) set.mask.isCustomRangeActive = true;
        SetMaskRange(set.mask, _layerId, ORDER_DEFAULT, ORDER_DEFAULT); // mask = -2/-2
        SafeSetActive(set.maskGO, false);
    }

    void SetMaskRange(SpriteMask mask, int layerID, int backOrder, int frontOrder)
    {
        if (!mask) return;
        mask.isCustomRangeActive = true;
        mask.backSortingLayerID  = layerID;
        mask.frontSortingLayerID = layerID;
        mask.backSortingOrder    = backOrder;
        mask.frontSortingOrder   = frontOrder;
    }

    void SafeSetActive(GameObject go, bool active)
    {
        if (go && go.activeSelf != active) go.SetActive(active);
    }

    ColorSet GetSet(ColorKey key)
    {
        switch (key)
        {
            case ColorKey.Red:   return red;
            case ColorKey.Green: return green;
            case ColorKey.Blue:  return blue;
        }
        return red;
    }

    // หา “previous” โดยดูใคร sr=0 และ maskGO ยังเปิดอยู่
    ColorKey GetPreviousCandidate(ColorKey current)
    {
        if (red.maskGO && red.maskGO.activeInHierarchy   && HasOrder(red, ORDER_PREV))   return ColorKey.Red;
        if (green.maskGO && green.maskGO.activeInHierarchy && HasOrder(green, ORDER_PREV)) return ColorKey.Green;
        if (blue.maskGO && blue.maskGO.activeInHierarchy  && HasOrder(blue, ORDER_PREV))  return ColorKey.Blue;

        // fallback: เลือกสีที่ไม่ใช่ current
        return current == ColorKey.Red ? ColorKey.Green :
               current == ColorKey.Green ? ColorKey.Blue : ColorKey.Red;
    }

    bool HasOrder(ColorSet set, int order)
    {
        if (set.bgAll == null || set.bgAll.Length == 0) return false;
        var sr = set.bgAll[0];
        return sr && sr.sortingOrder == order;
    }
}
