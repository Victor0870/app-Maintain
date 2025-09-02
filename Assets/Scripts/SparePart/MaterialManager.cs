using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using MySpace;
using BansheeGz.BGDatabase;
using System.Threading.Tasks;
using System.Linq;

public class MaterialManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject sparePartManagerPanel;
    public GameObject sparePartListPanel;
    public GameObject materialUsagePanel;
    public GameObject materialSelectPanel;

    [Header("UI Buttons")]
    public Button showListButton;
    public Button showPurchaseButton;
    public Button closeListButton;

    [Header("UI Elements - List Panel")]
    public Transform materialsListParent;
    public GameObject materialItemPrefab;

    [Header("UI Elements - Select Panel")] // Thêm parent riêng
    public Transform materialSelectParent;
    public GameObject materialSelectPanelItemPrefab;

    public TMP_InputField searchInputField;

    [Header("UI Elements - Filters")]
    public TMP_Dropdown typeFilterDropdown;
    public TMP_Dropdown locationFilterDropdown;
    public TMP_Dropdown categoryFilterDropdown;

    [Header("UI Elements - Select Panel Filters")]
    public TMP_InputField selectSearchInputField;
    public TMP_Dropdown selectTypeFilterDropdown;
    public TMP_Dropdown selectLocationFilterDropdown;
    public TMP_Dropdown selectCategoryFilterDropdown;

    [Header("UI Elements - Usage Panel")]
    public Transform usageListParent;
    public GameObject usageItemPrefab;
    public Button closeUsagePanelButton;
    public Button addNewUsageButton;
    public Button confirmUsageButton;

    private List<E_SparePart> _allMaterials = new List<E_SparePart>();
    private string _currentTaskId;

    private MaterialUIManager _materialUIManager;
    private FirebaseToBGDatabaseSynchronizer _synchronizer;
    private MaterialDataHandler _materialDataHandler;

    public BGDatabaseToFirebaseSynchronizer _synchronizerToFirebase;

    private Dictionary<string, object> _temporaryTaskMaterials = new Dictionary<string, object>();

    async void Start()
    {
        if (FirebaseManager.Instance == null || FirebaseManager.Instance.db == null)
        {
            Debug.LogError("FirebaseManager chưa được khởi tạo hoặc Firebase chưa sẵn sàng. Thử lại sau 1 giây.");
            Invoke("Start", 1f);
            return;
        }
        //_synchronizerToFirebase = GetComponent<BGDatabaseToFirebaseSynchronizer>();
        _synchronizer = GetComponent<FirebaseToBGDatabaseSynchronizer>();
        if (_synchronizer == null)
        {
            Debug.LogError("Thiếu script FirebaseToBGDatabaseSynchronizer trên cùng GameObject. Vui lòng thêm nó.");
            return;
        }

        _materialDataHandler = new MaterialDataHandler(FirebaseManager.Instance.db, FirebaseManager.Instance.GetCanvasAppId());

        _materialUIManager = new MaterialUIManager(
            sparePartListPanel, materialSelectPanel, materialsListParent, materialSelectParent, materialItemPrefab, materialSelectPanelItemPrefab,
            closeListButton, searchInputField, typeFilterDropdown, locationFilterDropdown, categoryFilterDropdown,
            materialUsagePanel, usageListParent, usageItemPrefab, closeUsagePanelButton, addNewUsageButton, confirmUsageButton,
            selectSearchInputField, selectTypeFilterDropdown, selectLocationFilterDropdown, selectCategoryFilterDropdown
        );

        if (showListButton != null) showListButton.onClick.AddListener(HandleShowListClicked);
        if (_materialUIManager != null)
        {
            _materialUIManager.OnCloseListPanelClicked += HandleCloseListPanelClicked;
            _materialUIManager.OnSearchOrFilterChanged += HandleSearchOrFilterChanged;
            _materialUIManager.OnCloseUsagePanelClicked += HandleCloseUsagePanelClicked;
            _materialUIManager.OnAddNewUsageClicked += HandleAddNewUsageClicked;
            _materialUIManager.OnConfirmUsageClicked += HandleConfirmUsageClicked;
            _materialUIManager.OnAddMaterialToTaskClicked += HandleAddMaterialToTaskClicked;
            _materialUIManager.OnQuantityChanged += HandleQuantityChanged;
        }

        if (_materialDataHandler != null)
        {
            _materialDataHandler.OnTaskMaterialsChanged += _materialUIManager.UpdateMaterialUsageUI;
        }

        if (sparePartListPanel != null) sparePartListPanel.SetActive(false);
        if (materialUsagePanel != null) materialUsagePanel.SetActive(false);
        if (materialSelectPanel != null) materialSelectPanel.SetActive(false);

        LoadAllMaterialsFromBGDatabase();

        bool syncSuccess = await _synchronizer.SynchronizeSparePartsFromFirebase();
        if (syncSuccess)
        {
            LoadAllMaterialsFromBGDatabase();
            PopulateFilterDropdowns();
        }
    }

    private void PopulateFilterDropdowns()
    {
        if (_allMaterials == null || _allMaterials.Count == 0)
        {
            Debug.LogWarning("Danh sách vật tư trống, không thể tạo bộ lọc.");
            return;
        }

        var types = new HashSet<string>();
        var locations = new HashSet<string>();
        var categories = new HashSet<string>();

        foreach (var material in _allMaterials)
        {
            types.Add(material.f_Type);
            locations.Add(material.f_Location);
            categories.Add(material.f_Category);
        }

        if (_materialUIManager != null)
        {
            _materialUIManager.UpdateFilterDropdown(typeFilterDropdown, types);
            _materialUIManager.UpdateFilterDropdown(locationFilterDropdown, locations);
            _materialUIManager.UpdateFilterDropdown(categoryFilterDropdown, categories);

            _materialUIManager.UpdateFilterDropdown(selectTypeFilterDropdown, types);
            _materialUIManager.UpdateFilterDropdown(selectLocationFilterDropdown, locations);
            _materialUIManager.UpdateFilterDropdown(selectCategoryFilterDropdown, categories);
        }
    }

    private void LoadAllMaterialsFromBGDatabase()
    {
        _allMaterials.Clear();
        E_SparePart.ForEachEntity(entity => _allMaterials.Add(entity));
        Debug.Log($"Đã tải {E_SparePart.CountEntities} vật tư từ BGDatabase cục bộ.");
    }

    void OnDestroy()
    {
        if (showListButton != null) showListButton.onClick.RemoveListener(HandleShowListClicked);
        if (_materialUIManager != null)
        {
            _materialUIManager.OnCloseListPanelClicked -= HandleCloseListPanelClicked;
            _materialUIManager.OnSearchOrFilterChanged -= HandleSearchOrFilterChanged;
            _materialUIManager.OnCloseUsagePanelClicked -= HandleCloseUsagePanelClicked;
            _materialUIManager.OnAddNewUsageClicked -= HandleAddNewUsageClicked;
            _materialUIManager.OnConfirmUsageClicked -= HandleConfirmUsageClicked;
            _materialUIManager.OnAddMaterialToTaskClicked -= HandleAddMaterialToTaskClicked;
            _materialUIManager.OnQuantityChanged -= HandleQuantityChanged;
        }

        if (_materialDataHandler != null)
        {
            _materialDataHandler.OnTaskMaterialsChanged -= _materialUIManager.UpdateMaterialUsageUI;
        }

        _materialDataHandler.StopListeningForTaskMaterials();
    }

    public void ShowMaterialUsagePanel(string taskId)
    {
        _currentTaskId = taskId;
        _materialUIManager.ShowMaterialUsagePanel();
        _materialDataHandler.StartListeningForTaskMaterials(taskId);
    }

    private void HandleShowListClicked()
    {
        _materialUIManager.ShowMaterialsListPanel();
        _materialUIManager.UpdateMaterialsListUI(_allMaterials, false);
    }

    private void HandleCloseListPanelClicked()
    {
        _materialUIManager.HideMaterialsListPanel();
    }

    private void HandleSearchOrFilterChanged(string searchTerm, string type, string location, string category)
    {
        List<E_SparePart> searchResults = new List<E_SparePart>();
        string lowerSearchTerm = searchTerm.ToLower();

        foreach (var material in _allMaterials)
        {
            bool matchesSearch = string.IsNullOrEmpty(lowerSearchTerm) || material.f_name.ToLower().Contains(lowerSearchTerm);
            bool matchesType = string.IsNullOrEmpty(type) || material.f_Type == type;
            bool matchesLocation = string.IsNullOrEmpty(location) || material.f_Location == location;
            bool matchesCategory = string.IsNullOrEmpty(category) || material.f_Category == category;

            if (matchesSearch && matchesType && matchesLocation && matchesCategory)
            {
                searchResults.Add(material);
            }
        }

        _materialUIManager.UpdateMaterialsListUI(searchResults, _materialUIManager.IsMaterialSelectPanelActive());
    }

    private void HandleCloseUsagePanelClicked()
    {
        _materialUIManager.HideMaterialUsagePanel();
        _materialDataHandler.StopListeningForTaskMaterials();
        _temporaryTaskMaterials.Clear();
    }

    private void HandleAddNewUsageClicked()
    {
        //_materialUIManager.HideMaterialUsagePanel();
        _materialUIManager.ShowMaterialSelectPanel();
        _materialUIManager.UpdateMaterialsListUI(_allMaterials, true);
    }

    private async void HandleConfirmUsageClicked()
    {
        var allUsageItems = _materialUIManager.GetAllMaterialUsageItems();
        var existingTaskMaterials = new HashSet<string>(_materialDataHandler.CurrentTaskMaterials.Select(m => m["materialId"].ToString()));

        foreach (var item in allUsageItems)
        {
            // Kiểm tra xem vật tư này đã tồn tại trên Firestore hay chưa
            if (existingTaskMaterials.Contains(item.materialId))
            {
                // Nếu vật tư đã tồn tại và có sự thay đổi
                if (item.changeType != MaterialUIManager.ChangeType.NoChange)
                {
                    int quantityChange = item.quantity - item.oldQuantity;
                    if (quantityChange != 0)
                    {
                        // Cập nhật stock cục bộ (BGDatabase)
                        var localMaterial = E_SparePart.FindEntity(entity => entity.f_No.ToString() == item.materialId);
                        if (localMaterial != null)
                        {
                            localMaterial.f_Stock -= quantityChange;
                        }
                    }
                    await _materialDataHandler.UpdateMaterialUsage(_currentTaskId, item.materialId, item.quantity, item.changeType);
                }
            }
            else
            {
                // Nếu là vật tư mới, thêm vào Firestore và trừ stock cục bộ
                await _materialDataHandler.AddMaterialToTask(_currentTaskId, item.materialId, item.quantity);
                var localMaterial = E_SparePart.FindEntity(entity => entity.f_No.ToString() == item.materialId);
                if (localMaterial != null)
                {
                    localMaterial.f_Stock -= item.quantity;
                }
            }
        }

        // Sau khi cập nhật tất cả vật tư cục bộ, đồng bộ lên Firebase
        _synchronizerToFirebase.SynchronizeSparePartsData();

        _temporaryTaskMaterials.Clear();
    }

    private void HandleAddMaterialToTaskClicked(string materialId, int initialQuantity)
    {
        _materialUIManager.HideMaterialSelectPanel();

        if (!_temporaryTaskMaterials.ContainsKey(materialId))
        {
            var newUsageItem = new Dictionary<string, object>
            {
                { "materialId", materialId },
                { "quantity", initialQuantity },
                { "status", "Đang chờ xác nhận" }
            };
            _temporaryTaskMaterials[materialId] = newUsageItem;
        }

        var combinedList = new List<Dictionary<string, object>>();

        if (_materialDataHandler.CurrentTaskMaterials != null)
        {
            combinedList.AddRange(_materialDataHandler.CurrentTaskMaterials);
        }

        foreach(var item in _temporaryTaskMaterials.Values)
        {
             combinedList.Add((Dictionary<string, object>)item);
        }
        _materialUIManager.UpdateMaterialUsageUI(combinedList);
    }

    private void HandleQuantityChanged(string materialId, int newQuantity, int oldQuantity)
    {
        E_SparePart materialToUpdate = E_SparePart.FindEntity(entity => entity.f_No.ToString() == materialId);
        if (materialToUpdate != null)
        {
            _materialUIManager.UpdateQuantityButtons(materialId, newQuantity, materialToUpdate.f_Stock);
        }
    }
}