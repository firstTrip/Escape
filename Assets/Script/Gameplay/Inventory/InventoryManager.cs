using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public event Action OnChanged;

    private readonly List<InventoryItemSO> items = new();
    public IReadOnlyList<InventoryItemSO> Items => items;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public bool Has(InventoryItemSO item)
    {
        return item != null && items.Contains(item);
    }

    public bool HasId(string itemId)
    {
        return items.Exists(i => i.itemId == itemId);
    }

    public void Add(InventoryItemSO item)
    {
        if (item == null || items.Contains(item)) return;
        items.Add(item);
        OnChanged?.Invoke();
    }

    public void Remove(InventoryItemSO item)
    {
        if (item == null) return;
        if (items.Remove(item))
            OnChanged?.Invoke();
    }
}
