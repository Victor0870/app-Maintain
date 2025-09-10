using UnityEngine;
using TMPro;
using System;

public class PurchaseHistoryItemUI : MonoBehaviour
{
    public TextMeshProUGUI poNumberText;
    public TextMeshProUGUI quantityText;
    public TextMeshProUGUI supplierText;
    public TextMeshProUGUI timestampText;

    public void SetData(string poNumber, int quantity, string supplier, DateTime timestamp)
    {
        if (poNumberText != null)
        {
            poNumberText.text = $"PO: {poNumber}";
        }
        if (quantityText != null)
        {
            quantityText.text = $"Số lượng: {quantity}";
        }
        if (supplierText != null)
        {
            supplierText.text = $"NCC: {supplier}";
        }
        if (timestampText != null)
        {
            timestampText.text = $"Thời gian: {timestamp.ToString("dd/MM/yyyy HH:mm")}";
        }
    }
}
