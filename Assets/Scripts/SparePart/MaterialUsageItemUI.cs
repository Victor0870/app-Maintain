using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class MaterialUsageItemUI : MonoBehaviour
{
    public TextMeshProUGUI materialNameText;
    public TMP_InputField quantityInput;
    public Button increaseButton;
    public Button decreaseButton;
    public int oldQuantity;

    private string _materialId;
    private int _stock;

    public string materialId => _materialId;
    public int quantity => int.Parse(quantityInput.text);
    public MaterialUIManager.ChangeType changeType { get; private set; } = MaterialUIManager.ChangeType.NoChange;

    public event Action<string, int> OnIncreaseQuantity;
    public event Action<string, int> OnDecreaseQuantity;

    // Thêm sự kiện mới để xử lý việc xóa vật tư
    public event Action<string> OnRemoveItemRequest;

    public void SetMaterialUsageData(string name, string id, int quantity, int stock)
    {
        materialNameText.text = name;
        _materialId = id;
        _stock = stock;
        quantityInput.text = quantity.ToString();
         oldQuantity = quantity;
        increaseButton.onClick.AddListener(IncreaseQuantity);
        decreaseButton.onClick.AddListener(DecreaseQuantity);

        UpdateButtons(quantity, stock);
    }

    private void IncreaseQuantity()
    {
        int newQuantity = int.Parse(quantityInput.text) + 1;
        if (newQuantity <= _stock)
        {
            quantityInput.text = newQuantity.ToString();
            changeType = MaterialUIManager.ChangeType.Increased;
            OnIncreaseQuantity?.Invoke(_materialId, newQuantity);
        }
    }

    private void DecreaseQuantity()
    {
        int currentQuantity = int.Parse(quantityInput.text);
        if (currentQuantity == 1)
        {
            // Nếu số lượng là 1, gửi yêu cầu xóa
            OnRemoveItemRequest?.Invoke(_materialId);
        }
        else
        {
            // Nếu số lượng lớn hơn 1, tiếp tục giảm
            int newQuantity = currentQuantity - 1;
            quantityInput.text = newQuantity.ToString();
            changeType = MaterialUIManager.ChangeType.Decreased;
            OnDecreaseQuantity?.Invoke(_materialId, newQuantity);
        }
    }

    public void UpdateButtons(int currentQuantity, int stock)
    {
        increaseButton.interactable = currentQuantity < stock;
        decreaseButton.interactable = currentQuantity >= 1; // Luôn cho phép giảm khi số lượng >= 1
    }
}