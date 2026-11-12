using UnityEngine;
using UnityEngine.EventSystems;
public class Tile : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
  public Vector2Int gridPosition;

  [Header("Colors")]
  public Color originalColor;
  public Color highlightColor = Color.yellow;
  public Color selectedColor = Color.blue;

  private Renderer tileRenderer;
  private static Tile selectedTile;

  private void Start()
  {
    tileRenderer = GetComponentInChildren<Renderer>();
    originalColor = tileRenderer.material.color;
  }

  public void ChangeColor(Color newColor)
  {
    tileRenderer.material.color = newColor;
  }

  public void OnPointerEnter(PointerEventData eventData)
  {
    if (selectedTile != this)
    {
      ChangeColor(highlightColor);
    }
  }

  public void OnPointerExit(PointerEventData eventData)
  {
    if (selectedTile != this)
    {
      ChangeColor(originalColor);
    }
  }
  public void OnPointerDown(PointerEventData eventData)
  {
    if (selectedTile)
    {
      selectedTile.ChangeColor(selectedTile.originalColor);
    }

    selectedTile = this;
    ChangeColor(selectedColor);

    FindAnyObjectByType<Unit>().MoveTo(transform.position, gridPosition);
  }
}
