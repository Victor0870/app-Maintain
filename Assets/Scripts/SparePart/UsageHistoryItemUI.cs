using UnityEngine;
using TMPro;
using System;

public class UsageHistoryItemUI : MonoBehaviour
{
    public TextMeshProUGUI quantityText;
    public TextMeshProUGUI timestampText;

    public void SetData(int quantity, DateTime timestamp)
    {
        if (quantityText != null)
        {
            quantityText.text = $"Số lượng: {quantity}";
        }
        if (timestampText != null)
        {
            timestampText.text = $"Thời gian: {timestamp.ToString("dd/MM/yyyy HH:mm")}";
        }
    }
}
