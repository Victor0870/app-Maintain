using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using MySpace;
using System;
using System.Linq;
using BansheeGz.BGDatabase;

public class MaterialUIManager
{
    private GameObject _sparePartListPanel;
    private GameObject _materialSelectPanel;
    private Transform _materialsListParent;
    private Transform _materialSelectParent;
    private GameObject _materialItemPrefab;
    private GameObject _materialSelectPanelItemPrefab;
    private Button _closeListButton;
    private TMP_InputField _searchInputField;

    private TMP_Dropdown _typeFilterDropdown;
    private TMP_Dropdown _locationFilterDropdown;
    private TMP_Dropdown _categoryFilterDropdown;

    private TMP_InputField _selectSearchInputField;
    private TMP_Dropdown _selectTypeFilterDropdown;
    private TMP_Dropdown _selectLocationFilterDropdown;
    private TMP_Dropdown _selectCategoryFilterDropdown;

    private GameObject _materialUsagePanel;
    private Transform _usageListParent;
    private GameObject _usageItemPrefab;
    private Button _closeUsagePanelButton;
    private Button _addNewUsageButton;
    private Button _confirmUsageButton;

    private GameObject _confirmPanel;
    private Button _confirmYesButton;
    private Button _confirmNoButton;
    private TextMeshProUGUI _confirmPopupText;

    private GameObject _purchasePanel;
    
    private GameObject _materialDetailsPanel;
    private TextMeshProUGUI _detailsNameText, _detailsStockText, _detailsLocationText, _detailsPurposeText, _detailsCategoryText, _detailsTypeText;
    private Transform _usageHistoryParent, _purchaseHistoryParent;
    private GameObject _usageHistoryItemPrefab, _purchaseHistoryItemPrefab;
    private Button _closeDetailsButton;

    private GameObject _addMaterialPanel;
    private AddMaterialPanelUI _addMaterialPanelUI;

    private Dictionary<string, MaterialUsageItemUI> _currentUsageItems = new Dictionary<string, MaterialUsageItemUI>();

    public enum ChangeType { NoChange, Added, Increased, Decreased, Removed }

    public event Action<string, string, string, string> OnSearchOrFilterChanged;
    public event Action OnCloseListPanelClicked;
    public event Action OnCloseUsagePanelClicked;
    public event Action OnAddNewUsageClicked;
    public event Action OnConfirmUsageClicked;
    public event Action<string, int> OnAddMaterialToTaskClicked;
    public event Action<string, int> OnAddMaterialToPurchaseClicked;
    public event Action<string, int, int> OnQuantityChanged;
    public event Action<string, string> OnRemoveItemRequest;
    public event Action<string> OnRemoveMaterialConfirmed;
    
    public event Action<string> OnMaterialItemSelected;
    
    public event Action<E_SparePart> OnAddMaterialConfirmed;

    public MaterialUIManager(
        GameObject sparePartListPanel,
        GameObject materialSelectPanel,
        Transform materialsListParent,
        Transform materialSelectParent,
        GameObject materialItemPrefab,
        GameObject materialSelectPanelItemPrefab,
        Button closeListButton,
        TMP_InputField searchInputField,
        TMP_Dropdown typeFilterDropdown,
        TMP_Dropdown locationFilterDropdown,
        TMP_Dropdown categoryFilterDropdown,
        GameObject materialUsagePanel,
        Transform usageListParent,
        GameObject usageItemPrefab,
        Button closeUsagePanelButton,
        Button addNewUsageButton,
        Button confirmUsageButton,
        TMP_InputField selectSearchInputField,
        TMP_Dropdown selectTypeFilterDropdown,
        TMP_Dropdown selectLocationFilterDropdown,
        TMP_Dropdown selectCategoryFilterDropdown,
        GameObject confirmPanel,
        TextMeshProUGUI confirmPopupText,
        Button confirmYesButton,
        Button confirmNoButton,
        GameObject purchasePanel,
        GameObject materialDetailsPanel,
        Transform usageHistoryParent,
        Transform purchaseHistoryParent,
        GameObject usageHistoryItemPrefab,
        GameObject purchaseHistoryItemPrefab,
        Button closeDetailsButton,
        // --- Thêm tham số mới cho panel thêm vật tư ---
        GameObject addMaterialPanel,
        Button addMaterialCloseButton,
        Button addMaterialSaveButton
    )
    {
        _sparePartListPanel = sparePartListPanel;
        _materialSelectPanel = materialSelectPanel;
        _materialsListParent = materialsListParent;
        _materialItemPrefab = materialItemPrefab;
        _materialSelectPanelItemPrefab = materialSelectPanelItemPrefab;
        _closeListButton = closeListButton;
        _searchInputField = searchInputField;
        _typeFilterDropdown = typeFilterDropdown;
        _locationFilterDropdown = locationFilterDropdown;
        _categoryFilterDropdown = categoryFilterDropdown;
        _materialUsagePanel = materialUsagePanel;
        _usageListParent = usageListParent;
        _usageItemPrefab = usageItemPrefab;
        _closeUsagePanelButton = closeUsagePanelButton;
        _addNewUsageButton = addNewUsageButton;
        _confirmUsageButton = confirmUsageButton;

        _materialSelectParent = materialSelectParent;
        _selectSearchInputField = selectSearchInputField;
        _selectTypeFilterDropdown = selectTypeFilterDropdown;
        _selectLocationFilterDropdown = selectLocationFilterDropdown;
        _selectCategoryFilterDropdown = selectCategoryFilterDropdown;

        _confirmPanel = confirmPanel;
        _confirmPopupText = confirmPopupText;
        _confirmYesButton = confirmYesButton;
        _confirmNoButton = confirmNoButton;
        _purchasePanel = purchasePanel;
        
        _materialDetailsPanel = materialDetailsPanel;
        _usageHistoryParent = usageHistoryParent;
        _purchaseHistoryParent = purchaseHistoryParent;
        _usageHistoryItemPrefab = usageHistoryItemPrefab;
        _purchaseHistoryItemPrefab = purchaseHistoryItemPrefab;
        _closeDetailsButton = closeDetailsButton;
        
        if (_materialDetailsPanel != null)
        {
            _detailsNameText = _materialDetailsPanel.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
            _detailsStockText = _materialDetailsPanel.transform.Find("StockText").GetComponent<TextMeshProUGUI>();
            _detailsLocationText = _materialDetailsPanel.transform.Find("LocationText").GetComponent<TextMeshProUGUI>();
            _detailsPurposeText = _materialDetailsPanel.transform.Find("PurposeText").GetComponent<TextMeshProUGUI>();
            _detailsCategoryText = _materialDetailsPanel.transform.Find("CategoryText").GetComponent<TextMeshProUGUI>();
            _detailsTypeText = _materialDetailsPanel.transform.Find("TypeText").GetComponent<TextMeshProUGUI>();
        }

        _addMaterialPanel = addMaterialPanel;
        if (addMaterialCloseButton != null) addMaterialCloseButton.onClick.AddListener(HideAddMaterialPanel);
        if (addMaterialSaveButton != null) addMaterialSaveButton.onClick.AddListener(() => OnAddMaterialConfirmed?.Invoke(_addMaterialPanel.GetComponent<AddMaterialPanelUI>().GetMaterialData()));

        SetupUIListeners();
    }

    private void SetupUIListeners()
    {
        if (_closeListButton != null) _closeListButton.onClick.AddListener(() => OnCloseListPanelClicked?.Invoke());
        if (_searchInputField != null) _searchInputField.onValueChanged.AddListener(value => TriggerSearchOrFilter());
        if (_typeFilterDropdown != null) _typeFilterDropdown.onValueChanged.AddListener(index => TriggerSearchOrFilter());
        if (_locationFilterDropdown != null) _locationFilterDropdown.onValueChanged.AddListener(index => TriggerSearchOrFilter());
        if (_categoryFilterDropdown != null) _categoryFilterDropdown.onValueChanged.AddListener(index => TriggerSearchOrFilter());
        if (_closeUsagePanelButton != null) _closeUsagePanelButton.onClick.AddListener(() => OnCloseUsagePanelClicked?.Invoke());
        if (_addNewUsageButton != null) _addNewUsageButton.onClick.AddListener(() => OnAddNewUsageClicked?.Invoke());
        if (_confirmUsageButton != null) _confirmUsageButton.onClick.AddListener(() => OnConfirmUsageClicked?.Invoke());

        if (_selectSearchInputField != null) _selectSearchInputField.onValueChanged.AddListener(value => TriggerSearchOrFilter());
        if (_selectTypeFilterDropdown != null) _selectTypeFilterDropdown.onValueChanged.AddListener(index => TriggerSearchOrFilter());
        if (_selectLocationFilterDropdown != null) _selectLocationFilterDropdown.onValueChanged.AddListener(index => TriggerSearchOrFilter());
        if (_selectCategoryFilterDropdown != null) _selectCategoryFilterDropdown.onValueChanged.AddListener(index => TriggerSearchOrFilter());

        if (_confirmNoButton != null) _confirmNoButton.onClick.AddListener(HideConfirmPanel);
        
        if (_closeDetailsButton != null) _closeDetailsButton.onClick.AddListener(HideMaterialDetailsPanel);
    }

    public void ShowConfirmPanel(string message, string materialId)
    {
        if (_confirmPanel != null)
        {
            _confirmPanel.SetActive(true);
            if (_confirmPopupText != null)
            {
                _confirmPopupText.text = message;
            }
            if (_confirmYesButton != null)
            {
                _confirmYesButton.onClick.RemoveAllListeners();
                _confirmYesButton.onClick.AddListener(() => {
                    OnRemoveMaterialConfirmed?.Invoke(materialId);
                    HideConfirmPanel();
                });
            }
        }
    }

    public void HideConfirmPanel()
    {
        if (_confirmPanel != null)
        {
            _confirmPanel.SetActive(false);
        }
    }

    private void TriggerSearchOrFilter()
    {
        string searchTerm;
        string type;
        string location;
        string category;

        if (_sparePartListPanel.activeSelf)
        {
            searchTerm = _searchInputField.text;
            type = _typeFilterDropdown.options[_typeFilterDropdown.value].text;
            location = _locationFilterDropdown.options[_locationFilterDropdown.value].text;
            category = _categoryFilterDropdown.options[_categoryFilterDropdown.value].text;
        }
        else if (_materialSelectPanel.activeSelf)
        {
            searchTerm = _selectSearchInputField.text;
            type = _selectTypeFilterDropdown.options[_selectTypeFilterDropdown.value].text;
            location = _selectLocationFilterDropdown.options[_selectLocationFilterDropdown.value].text;
            category = _selectCategoryFilterDropdown.options[_selectCategoryFilterDropdown.value].text;
        }
        else
        {
            return;
        }

        OnSearchOrFilterChanged?.Invoke(searchTerm, type, location, category);
    }

    public void UpdateFilterDropdown(TMP_Dropdown dropdown, HashSet<string> values)
    {
        dropdown.ClearOptions();
        var options = new List<string> { "" };
        options.AddRange(values);
        dropdown.AddOptions(options);
    }

    public bool IsMaterialSelectPanelActive()
    {
        return _materialSelectPanel != null && _materialSelectPanel.activeSelf;
    }

    public void ShowMaterialsListPanel()
    {
        if (_sparePartListPanel != null)
        {
            _sparePartListPanel.SetActive(true);
            if (_materialSelectPanel != null) _materialSelectPanel.SetActive(false);
            if (_materialDetailsPanel != null) _materialDetailsPanel.SetActive(false);
            if (_addMaterialPanel != null) _addMaterialPanel.SetActive(false);
        }
    }

    public void HideMaterialsListPanel()
    {
        if (_sparePartListPanel != null)
        {
            _sparePartListPanel.SetActive(false);
        }
    }

    public void ShowMaterialUsagePanel()
    {
        if (_materialUsagePanel != null)
        {
            _materialUsagePanel.SetActive(true);
            if (_sparePartListPanel != null) _sparePartListPanel.SetActive(false);
            if (_materialSelectPanel != null) _materialSelectPanel.SetActive(false);
        }
    }

    public void HideMaterialUsagePanel()
    {
        if (_materialUsagePanel != null)
        {
            _materialUsagePanel.SetActive(false);
        }
    }

    public void ShowMaterialSelectPanel()
    {
        if (_materialSelectPanel != null)
        {
            _materialSelectPanel.SetActive(true);
            if (_sparePartListPanel != null) _sparePartListPanel.SetActive(false);
        }
    }

    public void HideMaterialSelectPanel()
    {
        if (_materialSelectPanel != null)
        {
            _materialSelectPanel.SetActive(false);
        }
    }

    public void ShowMaterialDetailsPanel(string name, string stock, string location, string purpose, string category, string type)
    {
        if (_materialDetailsPanel != null)
        {
            _materialDetailsPanel.SetActive(true);
            if (_sparePartListPanel != null) _sparePartListPanel.SetActive(false);

            _detailsNameText.text = name;
            _detailsStockText.text = stock;
            _detailsLocationText.text = location;
            _detailsPurposeText.text = purpose;
            _detailsCategoryText.text = category;
            _detailsTypeText.text = type;
        }
    }

    public void HideMaterialDetailsPanel()
    {
        if (_materialDetailsPanel != null)
        {
            _materialDetailsPanel.SetActive(false);
            if (_sparePartListPanel != null) _sparePartListPanel.SetActive(true);
        }
    }
    
    public void ShowAddMaterialPanel()
    {
        if (_addMaterialPanel != null)
        {
            _addMaterialPanel.SetActive(true);
            if (_sparePartListPanel != null) _sparePartListPanel.SetActive(false);
        }
    }
    
    public void HideAddMaterialPanel()
    {
        if (_addMaterialPanel != null)
        {
            _addMaterialPanel.SetActive(false);
            _addMaterialPanel.GetComponent<AddMaterialPanelUI>().ClearInputs();
            if (_sparePartListPanel != null) _sparePartListPanel.SetActive(true);
        }
    }

        public void UpdateUsageHistoryUI(List<E_UsageHistory> usageHistory)
    {
        ClearList(_usageHistoryParent);
        foreach (var record in usageHistory)
        {
            GameObject historyItemUI = GameObject.Instantiate(_usageHistoryItemPrefab, _usageHistoryParent);
            UsageHistoryItemUI itemScript = historyItemUI.GetComponent<UsageHistoryItemUI>();
            if (itemScript != null)
            {
                string taskName = "Không rõ";
                string createdBy = "Không rõ";
                
                // Tìm thông tin công việc từ taskId
                var taskEntity = E_Task.FindEntity(e => e.f_Id == record.f_taskId);
                if (taskEntity != null)
                {
                    taskName = taskEntity.f_name;
                    createdBy = taskEntity.f_createdBy; // Lấy tên người yêu cầu
                }
                
                // Truyền cả tên công việc và người yêu cầu vào hàm SetData
                itemScript.SetData(record.f_quantity, record.f_timestamp, taskName, createdBy);
            }
        }
    }

    public void UpdatePurchaseHistoryUI(List<E_PurchaseHistory> purchaseHistory)
    {
        ClearList(_purchaseHistoryParent);
        foreach (var record in purchaseHistory)
        {
            GameObject historyItemUI = GameObject.Instantiate(_purchaseHistoryItemPrefab, _purchaseHistoryParent);
            PurchaseHistoryItemUI itemScript = historyItemUI.GetComponent<PurchaseHistoryItemUI>();
            if (itemScript != null)
            {
                itemScript.SetData(record.f_name, record.f_quantity, record.f_supplier, record.f_timestamp, record.f_price);
            }
        }
    }

    public void UpdateMaterialsListUI(List<E_SparePart> materials, bool isSelectPanel)
    {
        ClearMaterialsList(isSelectPanel);
        foreach (var materialData in materials)
        {
            DisplayMaterial(materialData, isSelectPanel);
        }
    }

    public void UpdateMaterialUsageUI(List<Dictionary<string, object>> materials)
    {
        _currentUsageItems.Clear();
        ClearUsageList();
        foreach (var materialData in materials)
        {
            string materialId = materialData.TryGetValue("materialId", out object idVal) ? idVal.ToString() : "N/A";
            string quantity = materialData.TryGetValue("quantity", out object quantityVal) ? quantityVal.ToString() : "N/A";
            DisplayMaterialForUsage(materialId, quantity);
        }
    }

    public List<MaterialUsageItemUI> GetAllMaterialUsageItems()
    {
        return _currentUsageItems.Values.ToList();
    }

    public void RemoveTemporaryMaterial(string materialId)
    {
        if (_currentUsageItems.ContainsKey(materialId))
        {
            GameObject.Destroy(_currentUsageItems[materialId].gameObject);
            _currentUsageItems.Remove(materialId);
        }
    }

    private void DisplayMaterial(E_SparePart materialData, bool isSelectPanel)
    {
        GameObject prefabToUse = isSelectPanel ? _materialSelectPanelItemPrefab : _materialItemPrefab;
        Transform parentToUse = isSelectPanel ? _materialSelectParent : _materialsListParent;
        if (prefabToUse == null || parentToUse == null)
        {
            Debug.LogError("Prefab hoặc Parent Transform chưa được gán!");
            return;
        }

        GameObject materialUI = GameObject.Instantiate(prefabToUse, parentToUse);
        MaterialItemUI itemScript = materialUI.GetComponent<MaterialItemUI>();
        if (itemScript != null)
        {
            string name = materialData.f_name;
            string stock = materialData.f_Stock.ToString();
            string location = materialData.f_Location;
            string purpose = materialData.f_Purpose;
            string category = materialData.f_Category;
            string type = materialData.f_Type;
            string materialId = materialData.f_No.ToString();

            itemScript.SetMaterialData(name, stock, location, purpose, category, type);
            if (isSelectPanel && itemScript.addButton != null)
            {
                if (_purchasePanel != null && _purchasePanel.activeSelf)
                {
                    itemScript.addButton.onClick.AddListener(() => OnAddMaterialToPurchaseClicked?.Invoke(materialId, 1));
                }
                else
                {
                    itemScript.addButton.onClick.AddListener(() => OnAddMaterialToTaskClicked?.Invoke(materialId, 1));
                }
                itemScript.addButton.interactable = materialData.f_Stock > 0;
            }
            if (!isSelectPanel && itemScript.itemButton != null)
            {
                itemScript.itemButton.onClick.AddListener(() => OnMaterialItemSelected?.Invoke(materialId));
            }
        }
    }

    private void DisplayMaterialForUsage(string materialId, string quantity)
    {
        if (_usageItemPrefab == null || _usageListParent == null)
        {
            Debug.LogError("Usage Item Prefab hoặc Parent Transform chưa được gán!");
            return;
        }

        var material = E_SparePart.FindEntity(entity => entity.f_No.ToString() == materialId);
        if (material == null) return;

        GameObject usageUI = GameObject.Instantiate(_usageItemPrefab, _usageListParent);
        MaterialUsageItemUI itemScript = usageUI.GetComponent<MaterialUsageItemUI>();
        if (itemScript != null)
        {
            itemScript.SetMaterialUsageData(material.f_name, materialId, int.Parse(quantity), material.f_Stock);
            _currentUsageItems[materialId] = itemScript;

            itemScript.OnIncreaseQuantity += (id, newQ) => OnQuantityChanged?.Invoke(id, newQ, int.Parse(quantity));
            itemScript.OnDecreaseQuantity += (id, newQ) => OnQuantityChanged?.Invoke(id, newQ, int.Parse(quantity));
            itemScript.OnRemoveItemRequest += (id, message) => ShowConfirmPanel(message, id);
        }
    }

    public void UpdateQuantityButtons(string materialId, int newQuantity, int stock)
    {
        if (_currentUsageItems.ContainsKey(materialId))
        {
            _currentUsageItems[materialId].UpdateButtons(newQuantity, stock);
        }
    }

    private void ClearMaterialsList(bool isSelectPanel)
    {
        Transform parentToClear = isSelectPanel ? _materialSelectParent : _materialsListParent;
        if (parentToClear == null) return;
        foreach (Transform child in parentToClear)
        {
            GameObject.Destroy(child.gameObject);
        }
    }

    private void ClearUsageList()
    {
        foreach (Transform child in _usageListParent)
        {
            GameObject.Destroy(child.gameObject);
        }
    }
    
    private void ClearList(Transform parent)
    {
        if (parent == null) return;
        foreach (Transform child in parent)
        {
            GameObject.Destroy(child.gameObject);
        }
    }
}

