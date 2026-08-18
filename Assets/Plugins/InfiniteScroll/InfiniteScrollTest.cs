using System.Collections.Generic;
using TMPro;
using R3;
using UnityEngine;

public class InfiniteScrollTest : MonoBehaviour
{
    [SerializeField] private InfiniteScroll infinite;
    private List<string> data;
    private ReactiveProperty<int> SelectedIndex = new ReactiveProperty<int>(-1);

    void Awake()
    {
        Debug.Log("Parent Awake");
        // Example data
        data = new List<string>(50);
        for (int i = 0; i < data.Capacity; i++) data.Add($"Row #{i}");

        // Initialize
        infinite.Init(data.Count, Bind);
        infinite.ScrollToIndex(0);
        SelectedIndex.Value = 0;

        SelectedIndex
        .DistinctUntilChanged()
        .Subscribe(index =>
        {
            infinite.ScrollToIndex(index);
        }).AddTo(gameObject);
    }

    void OnEnable()
    {
        Debug.Log("Parent OnEnable");
    }

    void Start()
    {
        Debug.Log("Parent Start");
    }


    void Bind(RectTransform item, int index)
    {
        var cell = item.GetComponent<CellInfiniteScroll>();
        cell.Bind(index, SelectedIndex);
    }
}
