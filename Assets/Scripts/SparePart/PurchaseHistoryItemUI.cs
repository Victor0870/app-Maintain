using UnityEngine;
using TMPro;
using System;

public class PurchaseHistoryItemUI : MonoBehaviour
{
    public TextMeshProUGUI poNumberText;
    public TextMeshProUGUI quantityText;
    public TextMeshProUGUI supplierText;
    public TextMeshProUGUI timestampText;
    public TextMeshProUGUI priceText;

    public void SetData(string poNumber, int quantity, string supplier, DateTime timestamp, int price)
    {
        if (poNumberText != null)
        {
            poNumberText.text = $"{poNumber}";
        }
        if (quantityText != null)
        {
            quantityText.text = $"{quantity}";
        }
        if (supplierText != null)
        {
            supplierText.text = $"{supplier}";
        }
        if (timestampText != null)
        {
            timestampText.text = $"{timestamp.ToString("dd/MM/yyyy HH:mm")}";
        }
        if (priceText != null)
        {
            priceText.text = $"{price}";
        }
    }
}
