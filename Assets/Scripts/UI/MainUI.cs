using System.Collections;
using TMPro;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

namespace GameOfLife.UI
{
    public class MainUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Dropdown executeDropdown;
        [SerializeField] private Slider tickSlider;
        [SerializeField] private TMP_Text tickText;
        [SerializeField] private Button playButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button userGenerationButton;

        [Header("Button Texts")]
        public string startEditText = "Ручная генерация";
        public string stopEditText = "Выйти из редактирования";

        private CameraController _cameraController;
        private EntityQuery _spawnCellsEntityQuery;
        private EntityQuery _simulateCellsEntityQuery;
        private Entity _executeEntity;
        private bool _isUserGenerationMode = false;
        private TextMeshProUGUI _userGenerationButtonText;
        private bool _wasSimulationRunning = false;

        // Для хранения коллайдеров
        private System.Collections.Generic.List<GameObject> _cellColliders = new();
        private bool _collidersCreated = false;

        private void Awake()
        {
            FindUIElements();

            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                _spawnCellsEntityQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<SpawnCellsConfig>());
                _simulateCellsEntityQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<SimulateCellsConfig>());
            }
        }

        private void FindUIElements()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            if (executeDropdown == null)
                executeDropdown = FindComponentInChildren<TMP_Dropdown>(canvas.transform, "ExecuteDropdown");

            if (tickSlider == null)
                tickSlider = FindComponentInChildren<Slider>(canvas.transform, "TickSlider");

            if (tickText == null)
                tickText = FindComponentInChildren<TMP_Text>(canvas.transform, "TickText");

            if (playButton == null)
                playButton = FindComponentInChildren<Button>(canvas.transform, "PlayButton");

            if (pauseButton == null)
                pauseButton = FindComponentInChildren<Button>(canvas.transform, "PauseButton");

            if (userGenerationButton == null)
            {
                userGenerationButton = FindComponentInChildren<Button>(canvas.transform, "UserGenerationButton");
                if (userGenerationButton == null)
                {
                    CreateUserGenerationButton(canvas.transform);
                }
            }
        }

        private T FindComponentInChildren<T>(Transform parent, string name) where T : Component
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    T component = child.GetComponent<T>();
                    if (component != null) return component;
                }

                T foundInChildren = FindComponentInChildren<T>(child, name);
                if (foundInChildren != null) return foundInChildren;
            }
            return null;
        }

        private void CreateUserGenerationButton(Transform parent)
        {
            GameObject buttonGO = new GameObject("UserGenerationButton");
            buttonGO.transform.SetParent(parent);

            RectTransform rect = buttonGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0, 20f);
            rect.sizeDelta = new Vector2(200, 40);

            Image image = buttonGO.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            Button button = buttonGO.AddComponent<Button>();
            button.targetGraphic = image;

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform);
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = startEditText;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 14;

            userGenerationButton = button;
            _userGenerationButtonText = text;
        }

        private IEnumerator Start()
        {
            InitializeUserGenerationButton();

            yield return new WaitUntil(() => World.DefaultGameObjectInjectionWorld != null);

            _cameraController = FindObjectOfType<CameraController>();

            EntityQuery executeQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<Execute.Execute>());

            float timeout = Time.time + 5f;
            while (!executeQuery.TryGetSingletonEntity<Execute.Execute>(out _executeEntity) && Time.time < timeout)
            {
                yield return null;
            }

            _cameraController?.UpdatePosition();

            if (_simulateCellsEntityQuery.TryGetSingleton<SimulateCellsConfig>(out var simulateCellsConfig))
            {
                if (tickSlider != null)
                {
                    tickSlider.value = simulateCellsConfig.TickDuration;
                    tickSlider.onValueChanged.AddListener(OnTickValueChanged);
                }
                if (tickText != null) tickText.text = simulateCellsConfig.TickDuration.ToString("0.#");
                UpdatePlayPauseButtons(simulateCellsConfig.IsEnabled);
            }

            if (playButton != null) playButton.onClick.AddListener(Play);
            if (pauseButton != null) pauseButton.onClick.AddListener(Pause);

            UpdateExecutionDropdown();

            // Создаем коллайдеры
            yield return StartCoroutine(CreateCollidersForCells());

            Randomize();
        }

        private IEnumerator CreateCollidersForCells()
        {
            // Ждем пока создадутся клетки
            yield return new WaitForSeconds(1f);

            var world = World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;

            // Находим все Entity с клетками
            var query = entityManager.CreateEntityQuery(typeof(Cell), typeof(LocalTransform));
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            Debug.Log($"Найдено {entities.Length} клеток для создания коллайдеров");

            foreach (var entity in entities)
            {
                var transform = entityManager.GetComponentData<LocalTransform>(entity);

                // Создаем GameObject для коллайдера
                GameObject colliderObj = new GameObject($"CellCollider_{entity.Index}");
                colliderObj.transform.position = transform.Position;
                colliderObj.tag = "Cell"; // Используем тег вместо слоя

                // Добавляем коллайдер
                BoxCollider collider = colliderObj.AddComponent<BoxCollider>();
                collider.size = new Vector3(0.9f, 0.1f, 0.9f);
                collider.isTrigger = true;

                // Добавляем компонент для хранения ссылки на Entity
                var cellRef = colliderObj.AddComponent<CellEntityReference>();
                cellRef.Entity = entity;
                cellRef.EntityManager = world.EntityManager;

                _cellColliders.Add(colliderObj);
            }

            entities.Dispose();
            _collidersCreated = true;
            Debug.Log($"Создано {_cellColliders.Count} коллайдеров для клеток");
        }

        private void Update()
        {
            if (_isUserGenerationMode && Input.GetMouseButtonDown(0))
            {
                HandleCellClick();
            }
        }

        private void HandleCellClick()
        {
            if (Camera.main == null)
            {
                Debug.Log("Камера не найдена");
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Raycast без указания слоя - будет работать со всеми коллайдерами
            if (Physics.Raycast(ray, out hit, 100f))
            {
                GameObject clickedObject = hit.collider.gameObject;
                Debug.Log($"Клик по объекту: {clickedObject.name}");

                CellEntityReference cellRef = clickedObject.GetComponent<CellEntityReference>();

                if (cellRef != null)
                {
                    Debug.Log("Найден CellEntityReference, переключаем клетку");
                    ToggleCell(cellRef.Entity);
                }
                else
                {
                    Debug.Log("CellEntityReference не найден");
                }
            }
            else
            {
                Debug.Log("Raycast не попал ни в один коллайдер");
            }
        }

        private void ToggleCell(Entity cellEntity)
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (entityManager.HasComponent<Cell>(cellEntity))
            {
                var cell = entityManager.GetComponentData<Cell>(cellEntity);
                bool newState = !cell.IsAlive;

                Debug.Log($"Переключаем клетку с {cell.IsAlive} на {newState}");

                // Обновляем состояние клетки
                cell.IsAlive = newState;
                cell.IsAliveNext = newState;
                entityManager.SetComponentData(cellEntity, cell);

                // Обновляем цвет
                if (entityManager.HasComponent<URPMaterialPropertyBaseColor>(cellEntity))
                {
                    var color = entityManager.GetComponentData<URPMaterialPropertyBaseColor>(cellEntity);
                    color.Value = newState ?
                        new Unity.Mathematics.float4(0f, 1f, 0f, 1f) :
                        new Unity.Mathematics.float4(0f, 0f, 0f, 1f);
                    entityManager.SetComponentData(cellEntity, color);
                }

                Debug.Log($"Клетка стала {(newState ? "живой" : "мертвой")}");
            }
            else
            {
                Debug.Log("У Entity нет компонента Cell");
            }
        }

        private void InitializeUserGenerationButton()
        {
            if (userGenerationButton == null)
            {
                Debug.LogError("Кнопка userGenerationButton не найдена!");
                return;
            }

            _userGenerationButtonText = userGenerationButton.GetComponentInChildren<TextMeshProUGUI>();
            if (_userGenerationButtonText != null)
                _userGenerationButtonText.text = startEditText;

            userGenerationButton.onClick.RemoveAllListeners();
            userGenerationButton.onClick.AddListener(OnUserGenerationButtonClick);

            Debug.Log("Кнопка ручного режима инициализирована");
        }

        private void UpdateExecutionDropdown()
        {
            if (executeDropdown == null)
            {
                Debug.LogWarning("ExecuteDropdown не найден");
                return;
            }

            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (entityManager.HasComponent<Execute.MainThread>(_executeEntity))
                executeDropdown.value = 0;
            else if (entityManager.HasComponent<Execute.SingleThreadedJob>(_executeEntity))
                executeDropdown.value = 1;
            else if (entityManager.HasComponent<Execute.ParallelJob>(_executeEntity))
                executeDropdown.value = 2;

            Debug.Log("Dropdown обновлен");
        }

        public void OnUserGenerationButtonClick()
        {
            Debug.Log("Нажата кнопка ручного режима");

            _isUserGenerationMode = !_isUserGenerationMode;

            if (_isUserGenerationMode)
                EnterEditMode();
            else
                ExitEditMode();

            UpdateButtonText();
            UpdateEditModeUI();
        }

        private void EnterEditMode()
        {
            Debug.Log("Вход в режим редактирования");

            if (_simulateCellsEntityQuery.TryGetSingleton<SimulateCellsConfig>(out var config))
                _wasSimulationRunning = config.IsEnabled;

            Pause();

            // Включаем все коллайдеры
            EnableCellColliders(true);

            Debug.Log("Режим редактирования: ВКЛЮЧЕН - кликайте по клеткам!");
        }

        private void ExitEditMode()
        {
            Debug.Log("Выход из режима редактирования");

            // Выключаем все коллайдеры
            EnableCellColliders(false);

            if (_wasSimulationRunning)
                Play();

            Debug.Log("Режим редактирования: ВЫКЛЮЧЕН");
        }

        private void EnableCellColliders(bool enable)
        {
            if (!_collidersCreated)
            {
                Debug.Log("Коллайдеры еще не созданы");
                return;
            }

            int enabledCount = 0;
            foreach (var colliderObj in _cellColliders)
            {
                if (colliderObj != null)
                {
                    var collider = colliderObj.GetComponent<Collider>();
                    if (collider != null)
                    {
                        collider.enabled = enable;
                        enabledCount++;
                    }
                }
            }

            Debug.Log($"{(enable ? "Включено" : "Выключено")} {enabledCount} коллайдеров");
        }

        private void UpdateButtonText()
        {
            if (_userGenerationButtonText != null)
            {
                _userGenerationButtonText.text = _isUserGenerationMode ? stopEditText : startEditText;
                Debug.Log($"Текст кнопки изменен на: {_userGenerationButtonText.text}");
            }
        }

        private void UpdateEditModeUI()
        {
            if (executeDropdown != null)
            {
                executeDropdown.interactable = !_isUserGenerationMode;
                Debug.Log($"Dropdown interactable: {executeDropdown.interactable}");
            }
            if (tickSlider != null)
            {
                tickSlider.interactable = !_isUserGenerationMode;
                Debug.Log($"Slider interactable: {tickSlider.interactable}");
            }
            if (playButton != null)
            {
                playButton.interactable = !_isUserGenerationMode;
                Debug.Log($"Play button interactable: {playButton.interactable}");
            }
            if (pauseButton != null)
            {
                pauseButton.interactable = !_isUserGenerationMode;
                Debug.Log($"Pause button interactable: {pauseButton.interactable}");
            }
        }

        private void UpdatePlayPauseButtons(bool isPlaying)
        {
            if (playButton != null)
            {
                playButton.gameObject.SetActive(!isPlaying);
                Debug.Log($"Play button active: {playButton.gameObject.activeSelf}");
            }
            if (pauseButton != null)
            {
                pauseButton.gameObject.SetActive(isPlaying);
                Debug.Log($"Pause button active: {pauseButton.gameObject.activeSelf}");
            }
        }

        public void Spawn()
        {
            if (_isUserGenerationMode)
            {
                Debug.LogWarning("Нельзя создавать клетки в режиме редактирования");
                return;
            }

            World.DefaultGameObjectInjectionWorld.Unmanaged.GetExistingSystemState<SpawnCellsSystem>().Enabled = true;
            _cameraController?.UpdatePosition();

            // Пересоздаем коллайдеры после спавна
            StartCoroutine(RecreateColliders());
        }

        private IEnumerator RecreateColliders()
        {
            // Удаляем старые коллайдеры
            foreach (var collider in _cellColliders)
            {
                if (collider != null) Destroy(collider);
            }
            _cellColliders.Clear();
            _collidersCreated = false;

            // Ждем и создаем новые
            yield return new WaitForSeconds(0.5f);
            yield return StartCoroutine(CreateCollidersForCells());
        }

        public void Randomize()
        {
            if (_isUserGenerationMode)
            {
                Debug.LogWarning("Нельзя рандомизировать в режиме редактирования");
                return;
            }

            World.DefaultGameObjectInjectionWorld.Unmanaged.GetExistingSystemState<RandomizeCellsSystem>().Enabled = true;
        }

        public void OnTickValueChanged(float value)
        {
            if (_simulateCellsEntityQuery.TryGetSingletonRW<SimulateCellsConfig>(out var config))
            {
                config.ValueRW.TickDuration = value;
                if (tickText != null) tickText.text = value.ToString("0.#");
            }
        }

        public void Play()
        {
            if (_isUserGenerationMode)
            {
                Debug.LogWarning("Нельзя запустить симуляцию в режиме редактирования");
                return;
            }

            if (_simulateCellsEntityQuery.TryGetSingletonRW<SimulateCellsConfig>(out var config))
            {
                config.ValueRW.IsEnabled = true;
                UpdatePlayPauseButtons(true);
            }
        }

        public void Pause()
        {
            if (_simulateCellsEntityQuery.TryGetSingletonRW<SimulateCellsConfig>(out var config))
            {
                config.ValueRW.IsEnabled = false;
                UpdatePlayPauseButtons(false);
            }
        }

        public bool IsUserGenerationMode => _isUserGenerationMode;

        public void ForceExitEditMode()
        {
            if (_isUserGenerationMode)
            {
                _isUserGenerationMode = false;
                ExitEditMode();
                UpdateButtonText();
                UpdateEditModeUI();
            }
        }

        private void OnDestroy()
        {
            // Очищаем созданные GameObject
            foreach (var colliderObj in _cellColliders)
            {
                if (colliderObj != null)
                {
                    Destroy(colliderObj);
                }
            }
            _cellColliders.Clear();
        }
    }

    // Компонент для хранения ссылки на Entity клетки
    public class CellEntityReference : MonoBehaviour
    {
        public Entity Entity;
        public EntityManager EntityManager;
    }
}