using UnityEngine;

public class SelfHide : MonoBehaviour
{
    public void HideThis()
    {
        gameObject.SetActive(false);
    }
}
