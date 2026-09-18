using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PieceView : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    public List<Vector2Int> Cells { get; private set; }
    public int Slot { get; private set; }
    public RectTransform Rect { get; private set; }

    private BlockPuzzleGame game;
    private Sprite cellSprite;
    private Vector2 homeCenter;
    private float currentCellSize;

    public void Initialize(
        BlockPuzzleGame owner,
        List<Vector2Int> cells,
        int slot,
        Vector2 center,
        Sprite sprite)
    {
        game = owner;
        Cells = new List<Vector2Int>(cells);
        Slot = slot;
        homeCenter = center;
        cellSprite = sprite;

        Rect = GetComponent<RectTransform>();
        Rect.anchorMin = new Vector2(0.5f, 0.5f);
        Rect.anchorMax = new Vector2(0.5f, 0.5f);
        Rect.pivot = new Vector2(0f, 1f);

        Image hitArea = gameObject.AddComponent<Image>();
        hitArea.color = Color.clear;
        hitArea.raycastTarget = true;

        ReturnHome(false);
    }

    public void Rebuild(float cellSize)
    {
        currentCellSize = cellSize;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }

        Vector2Int size = ShapeData.GetSize(Cells);

        Rect.sizeDelta = new Vector2(
            size.x * cellSize,
            size.y * cellSize);

        foreach (Vector2Int cell in Cells)
        {
            var tile = new GameObject(
                "PieceCell",
                typeof(RectTransform),
                typeof(Image));

            RectTransform tileRect =
                tile.GetComponent<RectTransform>();

            tileRect.SetParent(Rect, false);
            tileRect.anchorMin = new Vector2(0f, 1f);
            tileRect.anchorMax = new Vector2(0f, 1f);
            tileRect.pivot = new Vector2(0f, 1f);

            tileRect.anchoredPosition = new Vector2(
                cell.x * cellSize + 1.5f,
                -cell.y * cellSize - 1.5f);

            tileRect.sizeDelta =
                Vector2.one * Mathf.Max(1f, cellSize - 3f);

            Image image = tile.GetComponent<Image>();
            image.sprite = cellSprite;
            image.color = Color.white;
            image.raycastTarget = false;
        }
    }

    public void CenterAt(Vector2 localPointer)
    {
        Rect.anchoredPosition =
            localPointer +
            new Vector2(
                -Rect.sizeDelta.x * 0.5f,
                Rect.sizeDelta.y * 0.5f);
    }

    public void Rotate()
    {
        Cells = ShapeData.RotateClockwise(Cells);
        Rebuild(currentCellSize);
    }

    public void ReturnHome(bool animate)
    {
        Rect.DOKill();
        Rect.localScale = Vector3.one;

        Rebuild(game.TrayCellSize);

        Vector2 destination =
            homeCenter +
            new Vector2(
                -Rect.sizeDelta.x * 0.5f,
                Rect.sizeDelta.y * 0.5f);

        if (animate)
        {
            Rect.DOAnchorPos(destination, 0.18f)
                .SetEase(Ease.OutQuad);
        }
        else
        {
            Rect.anchoredPosition = destination;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        game.BeginDrag(this, eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        game.MoveDrag(this, eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        game.EndDrag(this);
    }

    private void OnDestroy()
    {
        if (Rect != null)
            Rect.DOKill();
    }
}