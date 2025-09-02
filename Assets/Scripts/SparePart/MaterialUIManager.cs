using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using MySpace;
using System;
using System.Linq;

public class MaterialUIManager
{
    private GameObject _sparePartListPanel;
    private GameObject _materialSelectPanel;
    private Transform _materialsListParent;
    private Transform _materialSelectParent; // Thêm parent mới
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

    private Dictionary<string, MaterialUsageItemUI> _currentUsageItems = new Dictionary<string, MaterialUsageItemUI>();

    public enum ChangeType { NoChange, Added, Increased, Decreased }

    public event Action<string, string, string, string> OnSearchOrFilterChanged;
    public event Action OnCloseListPanelClicked;
    public event Action OnCloseUsagePanelClicked;
    public event Action OnAddNewUsageClicked;
    public event Action OnConfirmUsageClicked;
    public event Action<string, int> OnAddMaterialToTaskClicked;
    public event Action<string, int, int> OnQuantityChanged;

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
        TMP_Dropdown selectCategoryFilterDropdown
    )
    {
        _sparePartListPanel = sparePartListPanel;
        _materialSelectPanel = materialSelectPanel;
        _materialsListParent = materialsListParent;
        _materialSelectParent = materialSelectParent; // Gán parent mới
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

        _selectSearchInputField = selectSearchInputField;
        _selectTypeFilterDropdown = selectTypeFilterDropdown;
        _selectLocationFilterDropdown = selectLocationFilterDropdown;
        _selectCategoryFilterDropdown = selectCategoryFilterDropdown;

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

    public void UpdateMaterialsListUI(List<E_SparePart> materials, bool isSelectPanel)
    {
        ClearMaterialsList(isSelectPanel); // Gọi hàm clear với tham số
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

    private void DisplayMaterial(E_SparePart materialData, bool isSelectPanel)
    {
        GameObject prefabToUse = isSelectPanel ? _materialSelectPanelItemPrefab : _materialItemPrefab;
        Transform parentToUse = isSelectPanel ? _materialSelectParent : _materialsListParent; // Chọn parent đúng
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
                itemScript.addButton.onClick.AddListener(() => OnAddMaterialToTaskClicked?.Invoke(materialId, 1));
                itemScript.addButton.interactable = materialData.f_Stock > 0;
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

        E_SparePart material = E_SparePart.FindEntity(entity => entity.f_No.ToString() == materialId);
        if (material == null) return;

        GameObject usageUI = GameObject.Instantiate(_usageItemPrefab, _usageListParent);
        MaterialUsageItemUI itemScript = usageUI.GetComponent<MaterialUsageItemUI>();
        if (itemScript != null)
        {
            itemScript.SetMaterialUsageData(material.f_name, materialId, int.Parse(quantity), material.f_Stock);
            _currentUsageItems[materialId] = itemScript;

            itemScript.OnIncreaseQuantity += (id, newQ) => OnQuantityChanged?.Invoke(id, newQ, int.Parse(quantity));
            itemScript.OnDecreaseQuantity += (id, newQ) => OnQuantityChanged?.Invoke(id, newQ, int.Parse(quantity));
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
}