using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Collections.Generic;

public class NewBotSetup : EditorWindow
{
    private GameObject modelPrefab;
    private ModelImporter modelImporter;
    private Avatar botAvatar;
    
    private Object runAnimation;
    private Object walkAnimation;
    private Object idleAnimation;
    private Object danceAnimation;
    
    private string botName = "NewBot";
    private string prefabPath = "Assets/Prefabs/Bots";
    private string modelDirectory; // Директория расположения модели
    
    private AnimationClip processedRunClip;
    private AnimationClip processedWalkClip;
    private AnimationClip processedIdleClip;
    private AnimationClip processedDanceClip;
    
    private AnimatorController animatorController;
    private List<string> sourceAnimationPaths = new List<string>(); // Для отслеживания исходных файлов

    // Вкладки редактора
    private enum EditorTab { Setup, Components }
    private EditorTab currentTab = EditorTab.Setup;
    
    // Переменные для вкладки компонентов
    private GameObject sourceBotPrefab; // Исходный бот с компонентами
    private GameObject targetBotPrefab; // Целевой бот для обновления

    [MenuItem("Tools/Bot Setup Wizard")]
    public static void ShowWindow()
    {
        GetWindow<NewBotSetup>("Bot Setup Wizard");
    }

    private void OnGUI()
    {
        GUILayout.Label("Мастер настройки бота", EditorStyles.boldLabel);
        
        // Вкладки
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(currentTab == EditorTab.Setup, "Создание", EditorStyles.toolbarButton))
            currentTab = EditorTab.Setup;
        
        if (GUILayout.Toggle(currentTab == EditorTab.Components, "Компоненты", EditorStyles.toolbarButton))
            currentTab = EditorTab.Components;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // Отображаем содержимое в зависимости от выбранной вкладки
        switch (currentTab)
        {
            case EditorTab.Setup:
                DrawSetupTab();
                break;
            case EditorTab.Components:
                DrawComponentsTab();
                break;
        }

        
    }


    
    // Вкладка настройки бота
    private void DrawSetupTab()
    {
        // Раздел модели
        GUILayout.Label("Шаг 1: Загрузка и настройка модели", EditorStyles.boldLabel);
        modelPrefab = EditorGUILayout.ObjectField("Модель бота:", modelPrefab, typeof(GameObject), false) as GameObject;
        
        if (modelPrefab != null)
        {
            string assetPath = AssetDatabase.GetAssetPath(modelPrefab);
            modelDirectory = Path.GetDirectoryName(assetPath).Replace('\\', '/'); // Запоминаем директорию модели
            
            modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            
            if (modelImporter != null)
            {
                EditorGUI.BeginDisabledGroup(modelImporter.animationType == ModelImporterAnimationType.Human);
                if (GUILayout.Button("Конвертировать в Humanoid"))
                {
                    ConvertModelToHumanoid();
                }
                EditorGUI.EndDisabledGroup();
                
                if (modelImporter.animationType == ModelImporterAnimationType.Human)
                {
                    EditorGUILayout.HelpBox("Модель настроена как Humanoid ✓", MessageType.Info);
                    botAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(assetPath);
                }
            }
        }
        
        EditorGUILayout.Space(10);
        
        // Раздел анимаций
        GUILayout.Label("Шаг 2: Загрузка и настройка анимаций", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(botAvatar == null);
        
        runAnimation = EditorGUILayout.ObjectField("Run Animation:", runAnimation, typeof(Object), false);
        walkAnimation = EditorGUILayout.ObjectField("Walk Animation:", walkAnimation, typeof(Object), false);
        idleAnimation = EditorGUILayout.ObjectField("Idle Animation:", idleAnimation, typeof(Object), false);
        danceAnimation = EditorGUILayout.ObjectField("Dance Animation:", danceAnimation, typeof(Object), false);
        
        if (GUILayout.Button("Обработать анимации"))
        {
            ProcessAllAnimations();
        }
        
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.Space(10);
        
        // Раздел настройки и создания
        GUILayout.Label("Шаг 3: Создание аниматора и префаба", EditorStyles.boldLabel);
        botName = EditorGUILayout.TextField("Имя бота:", botName);
        
        EditorGUI.BeginDisabledGroup(processedIdleClip == null);
        if (GUILayout.Button("Создать аниматор"))
        {
            CreateAnimatorController();
        }
        EditorGUI.EndDisabledGroup();
        
        EditorGUI.BeginDisabledGroup(animatorController == null);
        if (GUILayout.Button("Создать префаб и добавить на сцену"))
        {
            CreateBotPrefab();
        }
        EditorGUI.EndDisabledGroup();
    }
    
    // Вкладка копирования компонентов
    private void DrawComponentsTab()
    {
        GUILayout.Label("Установка компонентов бота", EditorStyles.boldLabel);
        
        EditorGUILayout.Space(10);
        
        sourceBotPrefab = EditorGUILayout.ObjectField("Исходный бот (донор):", sourceBotPrefab, typeof(GameObject), true) as GameObject;
        targetBotPrefab = EditorGUILayout.ObjectField("Целевой бот (получатель):", targetBotPrefab, typeof(GameObject), true) as GameObject;
        
        EditorGUILayout.Space(5);
        
        EditorGUI.BeginDisabledGroup(sourceBotPrefab == null || targetBotPrefab == null);
        if (GUILayout.Button("Перенести компоненты", GUILayout.Height(30)))
        {
            CopyComponents();
        }
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.HelpBox(
            "Этот инструмент копирует компоненты с исходного бота на целевой.\n" +
            "Компоненты, которые уже существуют у целевого бота, не будут изменены.",
            MessageType.Info);
    }

    private void CopyComponents()
    {
        if (sourceBotPrefab == null || targetBotPrefab == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "Выберите исходного и целевого бота", "OK");
            return;
        }
        
        bool isPrefabAsset = PrefabUtility.IsPartOfPrefabAsset(targetBotPrefab);
        bool isPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(targetBotPrefab);
        
        GameObject targetObject = targetBotPrefab;
        string prefabPath = "";
        
        // Если цель - это префаб из Asset, создаем его временный экземпляр
        if (isPrefabAsset)
        {
            prefabPath = AssetDatabase.GetAssetPath(targetBotPrefab);
            targetObject = PrefabUtility.InstantiatePrefab(targetBotPrefab) as GameObject;
        }
        
        // Получаем все компоненты исходного бота (кроме Transform)
        Component[] sourceComponents = sourceBotPrefab.GetComponents<Component>();
        int copiedCount = 0;
        
        // Копируем компоненты на целевой объект
        foreach (Component sourceComp in sourceComponents)
        {
            // Пропускаем Transform, его копировать не нужно
            if (sourceComp is Transform) continue;
            
            // Проверяем, есть ли уже такой компонент у целевого бота
            System.Type compType = sourceComp.GetType();
            Component targetComp = targetObject.GetComponent(compType);
            
            // Если такого компонента нет, копируем его
            if (targetComp == null)
            {
                targetComp = targetObject.AddComponent(compType);
                UnityEditorInternal.ComponentUtility.CopyComponent(sourceComp);
                UnityEditorInternal.ComponentUtility.PasteComponentValues(targetComp);
                copiedCount++;
            }
        }
        
        // Если работали с экземпляром префаба, применяем изменения и уничтожаем временный объект
        if (isPrefabAsset && !string.IsNullOrEmpty(prefabPath))
        {
            PrefabUtility.SaveAsPrefabAsset(targetObject, prefabPath);
            DestroyImmediate(targetObject);
            AssetDatabase.SaveAssets();
        }
        // Если это экземпляр префаба на сцене, применяем изменения к префабу
        else if (isPrefabInstance)
        {
            PrefabUtility.ApplyPrefabInstance(targetObject, InteractionMode.AutomatedAction);
        }
        
        EditorUtility.DisplayDialog("Успешно", $"Скопировано {copiedCount} компонентов на целевой объект.", "OK");
    }

    private void ConvertModelToHumanoid()
    {
        if (modelImporter != null)
        {
            // Запомним путь к файлу
            string assetPath = AssetDatabase.GetAssetPath(modelPrefab);
            
            // Настраиваем как humanoid
            modelImporter.animationType = ModelImporterAnimationType.Human;
            modelImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            
            // Применяем изменения и перезагружаем ассет
            modelImporter.SaveAndReimport();
            
            // Загружаем созданный аватар
            botAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(assetPath);
            
            EditorUtility.DisplayDialog("Успех", "Модель успешно конвертирована в Humanoid", "OK");
        }
    }

    private void ProcessAllAnimations()
    {
        if (botAvatar == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "Сначала сконвертируйте модель в Humanoid", "OK");
            return;
        }
        
        // Очищаем список исходных путей анимаций
        sourceAnimationPaths.Clear();
        
        // Обрабатываем каждую анимацию
        processedRunClip = ProcessAnimation(runAnimation, "Run");
        processedWalkClip = ProcessAnimation(walkAnimation, "Walk");
        processedIdleClip = ProcessAnimation(idleAnimation, "Idle");
        processedDanceClip = ProcessAnimation(danceAnimation, "Dance");
        
        // Удаляем исходные файлы, если они были временными
        DeleteSourceAnimationFiles();
        
        EditorUtility.DisplayDialog("Успех", "Анимации обработаны успешно и сохранены в директории модели", "OK");
    }

    private AnimationClip ProcessAnimation(Object animationObject, string animName)
    {
        if (animationObject == null) return null;
        
        string sourcePath = AssetDatabase.GetAssetPath(animationObject);
        sourceAnimationPaths.Add(sourcePath); // Добавляем путь для последующего удаления
        
        ModelImporter animImporter = AssetImporter.GetAtPath(sourcePath) as ModelImporter;
        
        if (animImporter == null)
        {
            Debug.LogError($"Не удалось получить ModelImporter для анимации {animName}");
            return null;
        }
        
        // Настраиваем импортер анимации
        animImporter.animationType = ModelImporterAnimationType.Human;
        animImporter.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        animImporter.sourceAvatar = botAvatar;
        
        // Применяем изменения
        animImporter.SaveAndReimport();
        
        // Находим анимацию в ассете
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(sourcePath);
        AnimationClip sourceClip = null;
        
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip)
            {
                sourceClip = clip;
                break;
            }
        }
        
        if (sourceClip == null)
        {
            Debug.LogError($"Не удалось найти AnimationClip в {sourcePath}");
            return null;
        }
        
        // Создаем новый клип в директории модели
        string newClipPath = $"{modelDirectory}/{botName}_{animName}.anim";
        
        // Копируем клип
        AnimationClip newClip = new AnimationClip();
        EditorUtility.CopySerialized(sourceClip, newClip);
        
        // Устанавливаем Loop Time
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(newClip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(newClip, settings);
        
        // Сохраняем новый клип
        AssetDatabase.CreateAsset(newClip, newClipPath);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"Анимация {animName} обработана и сохранена в {newClipPath}");
        
        return newClip;
    }
    
    // Метод для удаления исходных файлов анимаций
    private void DeleteSourceAnimationFiles()
    {
        foreach (string path in sourceAnimationPaths)
        {
            if (File.Exists(path))
            {
                AssetDatabase.DeleteAsset(path);
                Debug.Log($"Удален исходный файл анимации: {path}");
            }
        }
        
        // Очищаем список после удаления
        sourceAnimationPaths.Clear();
        
        // Обновляем базу данных ассетов
        AssetDatabase.Refresh();
    }

    private void CreateAnimatorController()
    {
        // Создаем контроллер в директории модели
        string controllerPath = $"{modelDirectory}/{botName}_Controller.controller";
        
        // Создаем контроллер
        animatorController = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        
        // Получаем корневую машину состояний
        AnimatorStateMachine rootStateMachine = animatorController.layers[0].stateMachine;
        
        // Создаем пустые состояния
        AnimatorState idleState = rootStateMachine.AddState("Idle", new Vector3(300, 100, 0));
        AnimatorState jumpState = rootStateMachine.AddState("Jump", new Vector3(300, 200, 0));
        
        // Добавляем остальные состояния
        if (processedRunClip != null)
        {
            AnimatorState runState = rootStateMachine.AddState("Run", new Vector3(500, 100, 0));
            runState.motion = processedRunClip;
        }
        
        if (processedWalkClip != null)
        {
            AnimatorState walkState = rootStateMachine.AddState("Walk", new Vector3(500, 200, 0));
            walkState.motion = processedWalkClip;
        }
        
        if (processedIdleClip != null)
        {
            idleState.motion = processedIdleClip;
        }
        
        if (processedDanceClip != null)
        {
            AnimatorState danceState = rootStateMachine.AddState("Dance", new Vector3(500, 300, 0));
            danceState.motion = processedDanceClip;
        }
        
        // Устанавливаем Idle как состояние по умолчанию
        rootStateMachine.defaultState = idleState;
        
        // Сохраняем изменения
        EditorUtility.SetDirty(animatorController);
        AssetDatabase.SaveAssets();
        
        EditorUtility.DisplayDialog("Успех", $"Аниматор создан: {controllerPath}", "OK");
    }

    private void CreateBotPrefab()
    {
        if (modelPrefab == null || animatorController == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "Модель или аниматор не настроены", "OK");
            return;
        }
        
        // Создаем директорию для префабов, если её нет
        if (!Directory.Exists(prefabPath))
        {
            Directory.CreateDirectory(prefabPath);
        }
        
        // Создаем экземпляр модели на сцене
        GameObject botInstance = Instantiate(modelPrefab);
        botInstance.name = botName;
        
        // Добавляем компонент Animator, если его нет
        Animator animator = botInstance.GetComponent<Animator>();
        if (animator == null)
        {
            animator = botInstance.AddComponent<Animator>();
        }
        
        // Настраиваем аниматор
        animator.runtimeAnimatorController = animatorController;
        animator.avatar = botAvatar;
        
        // Создаем префаб
        string finalPrefabPath = $"{prefabPath}/{botName}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(botInstance, finalPrefabPath);
        
        // Размещаем экземпляр на сцене
        Selection.activeObject = botInstance;
        
        EditorUtility.DisplayDialog("Успех", $"Префаб создан и размещен на сцене: {finalPrefabPath}", "OK");
    }
}