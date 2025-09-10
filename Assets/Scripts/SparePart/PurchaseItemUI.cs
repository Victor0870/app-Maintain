using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PurchaseItemUI : MonoBehaviour
{
    public TextMeshProUGUI materialNameText;
    public TMP_InputField poNumberInput;
    public TMP_InputField quantityInput;
    public TMP_InputField supplierInput;
    // --- Thêm trường nhập liệu cho giá ---
    public TMP_InputField priceInput;

    public string materialId { get; private set; }

    public void SetPurchaseData(string materialName, string materialId)
    {
        this.materialNameText.text = materialName;
        this.materialId = materialId;
    }
}
