using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MaterialItemUI : MonoBehaviour
{
    public TextMeshProUGUI materialNameText;
    public TextMeshProUGUI stockText;
    public TextMeshProUGUI locationText;
    public TextMeshProUGUI purposeText;
    public TextMeshProUGUI categoryText;
    public TextMeshProUGUI typeText;

    // Thêm biến public Button để tham chiếu nút "Thêm vào"
    public Button addButton;
    // Thêm biến mới để tham chiếu nút bấm của item trong danh sách
    public Button itemButton;

    public void SetMaterialData(string name, string stock, string location, string purpose, string category, string type)
    {
        if (materialNameText != null) materialNameText.text = name;
        if (stockText != null) stockText.text = stock;
        if (locationText != null) locationText.text = location;
        if (purposeText != null) purposeText.text = purpose;
        if (categoryText != null) categoryText.text = category;
        if (typeText != null) typeText.text = type;
    }
}
