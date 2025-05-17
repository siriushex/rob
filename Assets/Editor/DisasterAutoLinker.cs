using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class DisasterAutoLinker : EditorWindow
{
    // Настраиваемые параметры (можно в коде поменять значения по умолчанию)
    private static float neighborRadius = 1.5f;    // Максимальная дистанция между блоками, чтобы считать их "соседями"
    private static float foundationHeight = 0.1f;  // Если центр блока ниже этой высоты (Y), считаем, что он стоит на фундаменте
    private static bool attachToNeighbors = true;  // Флаг: прикреплять ли блоки друг к другу
    private static bool attachToFoundation = true; // Флаг: прикреплять ли нижние блоки к фундаменту

    // -----------------------------------------------------------------------------------
    // Пункт меню — откроет простое окошко настроек
    [MenuItem("Tools/Disaster Tools/Auto Link Blocks")]
    private static void ShowWindow()
    {
        // Открыть окно (необязательно, если не хотите окошко)
        var window = GetWindow<DisasterAutoLinker>("Auto Link Blocks");
        window.Show();
    }

    // В методе OnGUI можно отрисовать поля для настройки
    private void OnGUI()
    {
        EditorGUILayout.LabelField("Параметры привязки", EditorStyles.boldLabel);

        neighborRadius = EditorGUILayout.FloatField("Радиус соседей", neighborRadius);
        foundationHeight = EditorGUILayout.FloatField("Высота фундамента (Y)", foundationHeight);

        attachToNeighbors = EditorGUILayout.Toggle("Прикреплять к соседям", attachToNeighbors);
        attachToFoundation = EditorGUILayout.Toggle("Прикреплять к фундаменту", attachToFoundation);

        if (GUILayout.Button("Выполнить Linking"))
        {
            SetupAutoLinks(neighborRadius, foundationHeight, attachToNeighbors, attachToFoundation);
        }
    }

    // -----------------------------------------------------------------------------------
    // Основной метод: находит все DisasterDestructible, создаёт Rigidbody, Joints и т.д.
    public static void SetupAutoLinks(float neighRadius, float foundHeight, bool linkNeighbors, bool linkFoundation)
    {
        // 1) Ищем все объекты с DisasterDestructible
        var destructibles = Object.FindObjectsOfType<DisasterDestructible>(true);
        if (destructibles.Length == 0)
        {
            Debug.LogWarning("[DisasterAutoLinker] Не найдено объектов с DisasterDestructible");
            return;
        }

        // 2) Ищем "фундамент" по тегу (если нужно)
        Rigidbody foundationRb = null;
        if (linkFoundation)
        {
            var foundationObj = GameObject.FindGameObjectWithTag("Foundation");
            if (foundationObj == null)
            {
                Debug.LogWarning("[DisasterAutoLinker] Не найден объект с тегом 'Foundation'");
            }
            else
            {
                foundationRb = foundationObj.GetComponent<Rigidbody>();
                // Если у фундамента нет Rigidbody, добавим (и сделаем кинематическим)
                if (foundationRb == null)
                {
                    foundationRb = Undo.AddComponent<Rigidbody>(foundationObj);
                    foundationRb.isKinematic = true;
                }
            }
        }

        // Преобразуем в список для удобства
        var allBlocks = new List<DisasterDestructible>(destructibles);

        // 3) Сначала у всех блоков убеждаемся, что есть Rigidbody (кинематический)
        foreach (var block in allBlocks)
        {
            if (block == null) continue;

            Rigidbody rb = block.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = Undo.AddComponent<Rigidbody>(block.gameObject);
                rb.isKinematic = true;
            }
            else
            {
                // фиксируем изменения, чтобы работало Undo
                Undo.RecordObject(rb, "Make Kinematic");
                rb.isKinematic = true;
            }
        }

        // 4) Если нужно прикреплять к соседям, сделаем пары (i < j)
        //    Поиск "соседей" по расстоянию
        if (linkNeighbors)
        {
            for (int i = 0; i < allBlocks.Count; i++)
            {
                var blockA = allBlocks[i];
                if (blockA == null) continue;
                Rigidbody rbA = blockA.GetComponent<Rigidbody>();
                if (rbA == null) continue;

                Vector3 posA = rbA.worldCenterOfMass;

                for (int j = i + 1; j < allBlocks.Count; j++)
                {
                    var blockB = allBlocks[j];
                    if (blockB == null) continue;
                    Rigidbody rbB = blockB.GetComponent<Rigidbody>();
                    if (rbB == null) continue;

                    Vector3 posB = rbB.worldCenterOfMass;
                    // Считаем расстояние по горизонтали (можно и 3D-расстояние)
                    float dist = Vector3.Distance(posA, posB);
                    if (dist <= neighRadius)
                    {
                        // Создаём Joint на блоке A, прикрепляя к B
                        CreateFixedJoint(blockA.gameObject, rbB);

                        // Если хотите, можно ещё и наоборот (обычно достаточно одного, 
                        // так как FixedJoint - жёсткое соединение)
                        // CreateFixedJoint(blockB.gameObject, rbA);
                    }
                }
            }
        }

        // 5) Если нужно прикреплять к фундаменту (по высоте)
        if (linkFoundation && foundationRb != null)
        {
            foreach (var block in allBlocks)
            {
                if (block == null) continue;
                Rigidbody rb = block.GetComponent<Rigidbody>();
                if (rb == null) continue;

                // Проверим высоту центра объекта
                float blockY = rb.worldCenterOfMass.y;
                if (blockY <= foundHeight)
                {
                    // Создаём Joint, связывая с фундаментом
                    CreateFixedJoint(block.gameObject, foundationRb);
                }
            }
        }

        Debug.Log("[DisasterAutoLinker] Автоматическая привязка завершена!");
    }


    // -----------------------------------------------------------------------------------
    // Метод для создания FixedJoint на одном объекте, привязанном к другому Rigidbody
    private static void CreateFixedJoint(GameObject owner, Rigidbody connectTo)
    {
        // Проверяем, нет ли уже FixedJoint, соединяющегося с той же «целью»
        // (чтобы не дублировать) 
        FixedJoint[] existing = owner.GetComponents<FixedJoint>();
        foreach (var fj in existing)
        {
            if (fj.connectedBody == connectTo)
            {
                // Уже есть такой Joint
                return;
            }
        }

        // Создаём новый Joint
        FixedJoint newJoint = Undo.AddComponent<FixedJoint>(owner);

        // Настраиваем
        Undo.RecordObject(newJoint, "Create FixedJoint");
        newJoint.connectedBody = connectTo;
        newJoint.breakForce = Mathf.Infinity;
        newJoint.breakTorque = Mathf.Infinity;
        newJoint.enableCollision = false;
        newJoint.enablePreprocessing = true;
        newJoint.massScale = 1f;
        newJoint.connectedMassScale = 1f;
    }
}
