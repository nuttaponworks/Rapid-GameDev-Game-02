using UnityEngine;


public enum ElementType
{
    None,
    Fire,
    Water,
    Wind
}
public class PlayerInventory : MonoBehaviour
{
    public ElementType currentElement = ElementType.None;

    public void SetElement(ElementType element)
    {
        currentElement = element;
        Debug.Log("Collected Element: " + currentElement);
    }

}
