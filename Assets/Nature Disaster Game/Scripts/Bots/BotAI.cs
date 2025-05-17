using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent), typeof(Rigidbody), typeof(Animator))]
public class BotAI : MonoBehaviour, IDisasterReactive
{
    // Компоненты
    private NavMeshAgent agent;
    private Rigidbody rb;
    private Animator anim;
    private string currentAnim = "";

    // Настройки лобби
    [Header("Lobby")]
    [SerializeField] private float lobbyRadius = 10f; // Увеличено для большего радиуса перемещения
    [SerializeField] private Vector2 lobbyWalkTime = new Vector2(1f, 2.5f); // Сокращено для более частых действий
    [SerializeField, Range(0, 1)] private float jumpChance = 0.4f; // Увеличено для большего количества прыжков
    [SerializeField] private float jumpForce = 6f; // Увеличено для более высоких прыжков
    [SerializeField, Range(0, 1)] private float lobbyRunChance = 0.5f; // Шанс на бег в лобби
    [SerializeField, Range(0, 1)] private float doubleJumpChance = 0.3f; // Шанс на двойной прыжок

    // Настройки патрулирования
    [Header("Roaming")]
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float runSpeed = 4.5f;
    [SerializeField] private float arrivalDistance = 0.5f;
    [SerializeField] private float wanderRadius = 30f;
    [SerializeField] private float minDistanceToWaypoint = 10f; // Минимальное расстояние до следующей точки
    [SerializeField] private float pathUpdateInterval = 3f; // Как часто пересчитывать путь

    [Header("Waypoints")]
    public Transform[] waypoints;
    [SerializeField] private int waypointPickAttempts = 20;
    private Transform currentWaypoint;
    private Transform[] waypointsBackup; // Для временного хранения при режиме монет

    // Настройки истории перемещений
    [Header("Path History")]
    [SerializeField] private int historyLength = 4; // Сколько последних точек запоминать
    [SerializeField] private float pathChangeDelay = 8f; // Минимальное время до смены маршрута
    private List<Transform> recentWaypoints = new List<Transform>();
    private float lastWaypointChangeTime;

    // Настройки прыжков
    [Header("Jumping")]
    [SerializeField] private float checkDistance = 1f;
    [SerializeField] private float jumpableHeight = 0.4f;
    [SerializeField] private float forwardBoost = 2f;
    
    // Настройки сбора монет
    [Header("Coin Collection")]
    [SerializeField] private float coinDetectionRadius = 50f;
    [SerializeField] private LayerMask coinLayer;
    [SerializeField] private float coinCollectDistance = 1.5f;
    private GameObject targetCoin = null;

    // Состояние
    private enum BotState { Lobby, Roaming, CoinCollection, HeightSeeking }
    private BotState currentState = BotState.Lobby;
    private bool inRound = false;
    private float nextLobbyAction = 0f;
    private bool isChangingDestination = false;
    private float lastPathUpdateTime = 0f;
    private Vector3 lastPosition;
    private float stuckTime = 0f;
    private const float maxStuckTime = 3f; // Максимальное время зависания перед сменой точки

    // Константы
    const float walkThreshold = 2f;
    const float runThreshold = 4f;
    const float buffer = 0.2f;

    private float agentDisabledTime = 0f;
    private bool isPreferFarthestPoint; // Флаг стратегии выбора точки (самая дальняя или случайная)
    private bool wasNavMeshAgentEnabled = true;

    // Добавить в раздел с переменными:
[Header("Высотная навигация")]
[SerializeField] private float tsunamiSafeHeight = 15f;
[SerializeField] private float ladderDetectionRadius = 3f;
private RoofWaypoint targetRoofWaypoint = null;

[Header("Реакции на катастрофы")]
[SerializeField, Range(0f, 1f)] private float tsunamiReactionProbability = 0.8f; // 80% вероятность реакции на цунами
[SerializeField, Range(0f, 1f)] private float acidRainReactionProbability = 0.7f; // 70% вероятность реакции на кислотный дождь
[SerializeField, Range(0f, 1f)] private float floodReactionProbability = 0.8f; // 80% вероятность реакции на наводнение
private bool isReactingToDisaster = false;
private bool isClimbing = false;

 public PlayerParametrs thisBotParamtrs;


    public void OnFlood()
    {
        if (Random.value < floodReactionProbability)
        {
            SeekSafeHeight();
        }
    }
    
    // Реакция на торнадо
    public void OnTornado()
    {
        // При торнадо боты могут искать укрытие или убегать от центра торнадо
        if (Random.value < 0.6f)
        {
            FindShelter();
        }
    }
    
    // Реакция на извержение вулкана
    public void OnVolcanoEroption()
    {
        // При извержении вулкана боты могут убегать от вулкана
      //  FindSafeLocationAwayFrom("Volcano");
    }
    
    // Реакция на кислотный дождь
    public void OnAcidRain(Material[] materials)
    {
        if (Random.value < acidRainReactionProbability)
        {
            FindShelter();
        }
    }
    
    // Реакция на черную дыру
    public void OnBlackHole()
    {
       // FindSafeLocationAwayFrom("BlackHole");
    }



    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        rb.isKinematic = true;
        agent.stoppingDistance = arrivalDistance;
        agent.autoBraking = true;
        agent.avoidancePriority = Random.Range(0, 100); // Разный приоритет для избегания столкновений
        
        // Определяем стратегию бота (50/50) на основе instanceID объекта
        isPreferFarthestPoint = (GetInstanceID() % 2 == 0);
    }

    void Start()
    {
        currentState = BotState.Lobby;
        inRound = false;
        nextLobbyAction = Time.time + Random.Range(lobbyWalkTime.x, lobbyWalkTime.y);
        agent.speed = walkSpeed;
        lastPosition = transform.position;
        lastWaypointChangeTime = Time.time;
        
        // Убедимся, что у бота есть тег Player для режима сбора монет
        if (string.IsNullOrEmpty(gameObject.tag))
        {
            gameObject.tag = "Player";
        }

        if (waypoints == null || waypoints.Length == 0)
        {
            FindWaypoints();
        }
    }

 void Update()
{
    // Мониторинг состояния NavMeshAgent
    MonitorNavMeshAgent();

    if (!agent.isOnNavMesh) return;

    CheckIfStuck();

    if (Input.GetKeyDown(KeyCode.T))
    {
        TestClimbLadder();
    }
    switch (currentState)
    {
        case BotState.Lobby:
            LobbyBehaviour();
            break;
        case BotState.Roaming:
            // Периодически проверяем путь на наличие препятствий
            if (agent.hasPath && !isChangingDestination && Time.time - lastPathUpdateTime > 1f)
            {
                CheckPathForObstacles();
                lastPathUpdateTime = Time.time;
            }
            RoamBehaviour();
            break;
        case BotState.CoinCollection:
            CoinCollectionBehaviour();
            break;
        case BotState.HeightSeeking:
            HeightSeekingBehaviour(); // Вызов нового поведения
            break;
    }

    HandleAnimations();
}

 // Вспомогательный метод для сброса флага реакции на катастрофу
    private IEnumerator ResetDisasterReactionFlag(float delay)
    {
        yield return new WaitForSeconds(delay);
        isReactingToDisaster = false;
    }
 // Метод для поиска укрытия от дождя/кислотного дождя
    private void FindShelter()
    {
        if (isReactingToDisaster) return;
        isReactingToDisaster = true;
        
        // Находим объекты с тегом "Shelter"
        GameObject[] shelters = GameObject.FindGameObjectsWithTag("Shelter");
        
        if (shelters.Length == 0)
        {
            // Если нет специальных укрытий, ищем любые объекты с коллайдерами, под которыми можно укрыться
            Collider[] potentialShelters = Physics.OverlapSphere(transform.position, 50f);
            List<Transform> validShelters = new List<Transform>();
            
            foreach (var collider in potentialShelters)
            {
                // Проверяем, что объект достаточно высокий и широкий, чтобы служить укрытием
                if (collider.bounds.size.y >= 3f && collider.bounds.size.x >= 2f && collider.bounds.size.z >= 2f)
                {
                    // Убедимся, что это не земля и не сам бот
                    if (collider.transform != transform && !collider.CompareTag("Ground"))
                    {
                        validShelters.Add(collider.transform);
                    }
                }
            }
            
            if (validShelters.Count > 0)
            {
                // Находим ближайшее укрытие
                Transform closestShelter = null;
                float minDistance = float.MaxValue;
                
                foreach (var shelter in validShelters)
                {
                    float dist = Vector3.Distance(transform.position, shelter.position);
                    if (dist < minDistance)
                    {
                        // Проверяем, можно ли добраться до укрытия
                        NavMeshPath path = new NavMeshPath();
                        if (agent.CalculatePath(shelter.position, path) && path.status == NavMeshPathStatus.PathComplete)
                        {
                            minDistance = dist;
                            closestShelter = shelter;
                        }
                    }
                }
                
                if (closestShelter != null)
                {
                    // Выбираем точку под укрытием
                    Vector3 shelterPoint = closestShelter.position;
                    shelterPoint.y = transform.position.y; // На уровне земли
                    
                    StartCoroutine(SetDestinationRoutine(shelterPoint));
                    Debug.Log($"Бот {gameObject.name} ищет укрытие от кислотного дождя");
                }
            }
        }
        else
        {
            // Используем заранее расставленные укрытия с тегом "Shelter"
            Transform closestShelter = null;
            float minDistance = float.MaxValue;
            
            foreach (var shelter in shelters)
            {
                float dist = Vector3.Distance(transform.position, shelter.transform.position);
                if (dist < minDistance)
                {
                    NavMeshPath path = new NavMeshPath();
                    if (agent.CalculatePath(shelter.transform.position, path) && path.status == NavMeshPathStatus.PathComplete)
                    {
                        minDistance = dist;
                        closestShelter = shelter.transform;
                    }
                }
            }
            
            if (closestShelter != null)
            {
                StartCoroutine(SetDestinationRoutine(closestShelter.position));
                Debug.Log($"Бот {gameObject.name} ищет укрытие от кислотного дождя");
            }
        }
        
        // Сбрасываем флаг реакции через некоторое время
        StartCoroutine(ResetDisasterReactionFlag(10f));
    }
private void HeightSeekingBehaviour()
{
    if (isChangingDestination || isClimbing) return;
    
    // ДОБАВЛЕНО: Проверка, находится ли бот на NavMesh
    if (!agent.isOnNavMesh)
    {
        Debug.LogWarning($"Бот {gameObject.name} не на NavMesh! Пытаемся исправить...");
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
        {
            // Перемещаем бота на NavMesh
            transform.position = hit.position;
        }
        else
        {
            // Если не удалось найти точку NavMesh, возвращаемся к роамингу
            Debug.LogError($"Невозможно найти NavMesh рядом с ботом {gameObject.name}!");
            currentState = BotState.Roaming;
            return;
        }
    }
    
    // Проверяем, достигли ли мы начала лестницы
    CheckForLadderReached();
    
    // Остальной код...
}
// Публичный метод для тестирования через кнопки или другие события
public void TestClimbLadder()
{
    SeekSafeHeight();
    Debug.Log($"Бот {gameObject.name} начал тестирование поиска безопасной высоты");
}
    public void SeekSafeHeight()
{
    // Находим все RoofWaypoints
    RoofWaypoint[] roofPoints = FindObjectsOfType<RoofWaypoint>();
    
    if (roofPoints.Length == 0) return;
    
    // Фильтруем точки, которые безопасны от цунами
    List<RoofWaypoint> safePoints = new List<RoofWaypoint>();
    foreach (var point in roofPoints)
    {
        if (point.isSafeFromTsunami && point.ladderStart != null && point.ladderEnd != null)
        {
            safePoints.Add(point);
        }
    }
    
    if (safePoints.Count == 0) return;
    
    // Выбираем случайную безопасную точку
    targetRoofWaypoint = safePoints[Random.Range(0, safePoints.Count)];
    
    // Идём к началу лестницы
    StartCoroutine(SetDestinationRoutine(targetRoofWaypoint.ladderStart.position));
    currentState = BotState.HeightSeeking;
}
private void CheckForLadderReached()
{
    if (isClimbing || targetRoofWaypoint == null) return;
    
    // Если мы достигли начала лестницы
    if (Vector3.Distance(transform.position, targetRoofWaypoint.ladderStart.position) < 1.0f)
    {
        // Начинаем подъём
        StartCoroutine(SimpleLadderClimb());
    }
}

// Простой метод подъёма по лестнице
// Улучшенный метод подъёма по лестнице с предотвращением проваливания
private IEnumerator SimpleLadderClimb()
{
    if (targetRoofWaypoint == null || isClimbing) yield break;
    
    isClimbing = true;
    
    // 1. Отключаем навигацию и подготовка
    agent.enabled = false;
    rb.isKinematic = true;
    
    // 2. Поворачиваемся лицом к лестнице
    Vector3 ladderDirection = (targetRoofWaypoint.ladderEnd.position - targetRoofWaypoint.ladderStart.position).normalized;
    transform.forward = new Vector3(ladderDirection.x, 0, ladderDirection.z);
    
    // 3. Запускаем анимацию лазания
    if (anim.HasState(0, Animator.StringToHash("ClimbLadder")))
        anim.Play("ClimbLadder");
    else
        Debug.LogWarning($"Бот {gameObject.name}: анимация ClimbLadder не найдена!");
    
    // 4. Плавное перемещение от начала к концу лестницы
    float progress = 0f;
    float climbSpeed = targetRoofWaypoint.climbSpeed;
    Vector3 startPos = targetRoofWaypoint.ladderStart.position;
    Vector3 endPos = targetRoofWaypoint.ladderEnd.position;
    
    while (progress < 1f)
    {
        progress += Time.deltaTime * climbSpeed;
        transform.position = Vector3.Lerp(startPos, endPos, Mathf.Clamp01(progress));
        yield return null;
    }
    
    // 5. Ключевая часть - ПОИСК ТОЧКИ НА КРЫШЕ
    Vector3 roofCenter = targetRoofWaypoint.transform.position;
    float roofY = roofCenter.y; // Высота крыши
    
    // Начинаем спиральный поиск точки NavMesh на крыше
    Vector3 finalPosition = endPos;
    NavMeshHit navHit;
    bool foundNavMeshPoint = false;
    
    // Поиск по спирали с увеличивающимся радиусом
    for (int r = 1; r <= 10; r += 2)
    {
        for (int i = 0; i < 8; i++) // 8 направлений
        {
            float angle = i * 45f;
            Vector3 checkDir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            Vector3 checkPoint = roofCenter + checkDir * r;
            
            // Сохраняем высоту крыши
            checkPoint.y = roofY;
            
            Debug.DrawRay(checkPoint, Vector3.up, Color.yellow, 3f);
            
            if (NavMesh.SamplePosition(checkPoint, out navHit, 2f, NavMesh.AllAreas))
            {
                // Проверяем, что точка действительно на крыше
                if (Mathf.Abs(navHit.position.y - roofY) < 1.0f)
                {
                    finalPosition = navHit.position + Vector3.up * 0.1f;
                    foundNavMeshPoint = true;
                    Debug.Log($"Бот {gameObject.name}: найдена точка NavMesh на крыше на высоте {navHit.position.y}");
                    Debug.DrawLine(endPos, finalPosition, Color.green, 5f);
                    break;
                }
            }
        }
        if (foundNavMeshPoint) break;
    }
    
    // 6. "Прыжок" на крышу с визуальной дугой
    float teleportDuration = 0.5f;
    float teleportTimer = 0f;
    Vector3 teleportStartPos = endPos;
    
    while (teleportTimer < teleportDuration)
    {
        teleportTimer += Time.deltaTime;
        float t = teleportTimer / teleportDuration;
        
        // Дуга для прыжка
        float height = Mathf.Sin(t * Mathf.PI) * 0.8f;
        Vector3 pos = Vector3.Lerp(teleportStartPos, finalPosition, t);
        pos.y += height;
        
        transform.position = pos;
        yield return null;
    }
    
    // 7. Фиксация финальной позиции и восстановление навигации
    transform.position = finalPosition;
    yield return new WaitForSeconds(0.3f);
    
    rb.isKinematic = true;
    agent.enabled = true;
    
    yield return new WaitForSeconds(0.2f);
    
  if (agent.isOnNavMesh)
{
    // НЕ используйте напрямую targetRoofWaypoint.transform.position
    
    // Вместо этого используйте ранее найденную валидную точку
    // или найдите ближайшую валидную точку к целевой
    Vector3 safeDestination = finalPosition; // Используем точку, где мы уже находимся
    
    // Попробуем найти точку ближе к центру крыши
    if (NavMesh.SamplePosition(targetRoofWaypoint.transform.position, out navHit, 10f, NavMesh.AllAreas))
    {
        // Проверяем, что точка на той же высоте (крыша)
        if (Mathf.Abs(navHit.position.y - finalPosition.y) < 1.0f)
        {
            safeDestination = navHit.position;
            Debug.Log($"Бот {gameObject.name} использует безопасную точку навигации на крыше");
        }
    }
    
    // Используем уже проверенную точку на NavMesh
    StartCoroutine(SetDestinationRoutine(safeDestination));
    Debug.Log($"Бот {gameObject.name} успешно переместился на NavMesh крыши");
}
    
    isClimbing = false;
}

private void CheckPathForObstacles()
{
    if (!agent.hasPath || agent.pathPending || isChangingDestination) return;
    
    // Проверяем, есть ли препятствия по прямой линии до следующей точки в пути
    if (agent.path.corners.Length > 1)
    {
        Vector3 nextCorner = agent.path.corners[1];
        Vector3 direction = (nextCorner - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, nextCorner);
        
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, direction, distance))
        {
            // Если обнаружено препятствие, пересчитываем путь
            Vector3 destination = agent.destination;
            StartCoroutine(SetDestinationRoutine(destination));
        }
    }
}
    // Мониторинг состояния NavMeshAgent
    private void MonitorNavMeshAgent()
    {
        if (wasNavMeshAgentEnabled && !agent.enabled)
        {
            // NavMeshAgent только что отключился
            agentDisabledTime = Time.time;
            wasNavMeshAgentEnabled = false;
        }
        else if (!wasNavMeshAgentEnabled && !agent.enabled)
        {
            // NavMeshAgent всё ещё отключен, проверяем время
            if (Time.time - agentDisabledTime > 1.0f)
            {
                // Прошла секунда, пробуем восстановить NavMeshAgent
                TryRestoreNavMeshAgent();
            }
        }
        else if (!wasNavMeshAgentEnabled && agent.enabled)
        {
            // NavMeshAgent снова включен
            wasNavMeshAgentEnabled = true;
        }
    }
    
    // Попытка восстановить NavMeshAgent
    private void TryRestoreNavMeshAgent()
    {
        Debug.Log($"Восстановление NavMeshAgent для {gameObject.name}");
        
        // Сохраняем текущую позицию и поворот перед перезапуском
        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = transform.rotation;
        
        // Перезапускаем NavMeshAgent
        agent.enabled = false;
        rb.isKinematic = true;
        
        // Возможно, нужно убедиться, что позиция валидна для NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(currentPosition, out hit, 5.0f, NavMesh.AllAreas))
        {
            currentPosition = hit.position;
        }
        
        transform.position = currentPosition;
        transform.rotation = currentRotation;
        
        agent.enabled = true;
        wasNavMeshAgentEnabled = true;
        
        // Восстанавливаем цель движения в зависимости от текущего состояния
        if (currentState == BotState.CoinCollection && targetCoin != null)
        {
            StartCoroutine(SetDestinationRoutine(targetCoin.transform.position));
        }
        else if (currentState == BotState.Roaming && currentWaypoint != null)
        {
            StartCoroutine(SetDestinationRoutine(currentWaypoint.position));
        }
    }
    
    // Проверка на зависание
    private void CheckIfStuck()
    {
        if (Vector3.Distance(transform.position, lastPosition) < 0.1f)
        {
            stuckTime += Time.deltaTime;
            
            if (stuckTime > maxStuckTime)
            {
                HandleStuckSituation();
                stuckTime = 0f;
            }
        }
        else
        {
            stuckTime = 0f;
        }
        
        lastPosition = transform.position;
    }

    // Обработка зависания
private void HandleStuckSituation()
{
    if (currentState == BotState.Lobby) return;

    // Пытаемся найти обходной путь вокруг места застревания
    Vector3 currentDestination = agent.destination;
    
    // Генерируем несколько случайных точек вокруг текущей позиции
    for (int i = 0; i < 8; i++) // Пробуем 8 направлений
    {
        float angle = i * 45f; // Разные углы для полного круга (8 * 45 = 360)
        Vector3 checkDir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
        Vector3 checkPoint = transform.position + checkDir * 3f; // 3 метра в выбранном направлении
        
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(checkPoint, out navHit, 3f, NavMesh.AllAreas))
        {
            // Если нашли точку на NavMesh, идём туда
            StartCoroutine(SetDestinationRoutine(navHit.position));
            Debug.Log($"Бот {gameObject.name} застрял, выбрал новое направление: {navHit.position}");
            return;
        }
    }
    
    // Если не нашли обходной путь, пробуем прыгать или выбираем новую целевую точку
    if (Random.value > 0.5f)
    {
        StartCoroutine(JumpRoutine());
    }
    else
    {
        // Используем существующую логику выбора новой точки
        if (currentState == BotState.CoinCollection)
        {
            targetCoin = null;
            FindNearestCoin();
        }
        else if (currentState == BotState.Roaming)
        {
            PickRandomWaypoint();
        }
    }
}

    // Поведение в лобби - УЛУЧШЕННАЯ ВЕРСИЯ
private void LobbyBehaviour()
{
    // Если бот уже выполняет действие, ждем
    if (Time.time < nextLobbyAction || isChangingDestination) return;

    // Перемещение к случайной точке
    Vector3 randomPoint = RandomNavPoint(transform.position, lobbyRadius);
    agent.speed = walkSpeed; // Устанавливаем скорость ходьбы
    StartCoroutine(SetDestinationRoutine(randomPoint));

    // Устанавливаем задержку до следующего действия
    nextLobbyAction = Time.time + Random.Range(lobbyWalkTime.x, lobbyWalkTime.y);
}

    // Поведение на карте
    private void RoamBehaviour()
    {
        if (isChangingDestination) return;

        TrySmallJump();

        // Периодическое обновление пути
        if (Time.time - lastPathUpdateTime > pathUpdateInterval)
        {
            agent.SetDestination(agent.destination);
            lastPathUpdateTime = Time.time;
        }

        // Проверяем, дошли ли до точки или путь невалиден
        if (agent.remainingDistance <= arrivalDistance && !agent.pathPending || 
            !agent.hasPath && agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            PickRandomWaypoint();
        }
    }
    
    // Поведение в режиме сбора монет
    private void CoinCollectionBehaviour()
{
    if (isChangingDestination) return;

    // Если нет целевой монеты или она была уничтожена
    if (targetCoin == null)
    {
        FindNearestCoin();
        return;
    }

    // Проверяем, достигли ли монеты
    float distanceToCoin = Vector3.Distance(transform.position, targetCoin.transform.position);
    if (distanceToCoin <= coinCollectDistance)
    {
        // "Собираем" монету
        CollectCoin(targetCoin);
        targetCoin = null;
        
        // Сразу же ищем следующую монету
        FindNearestCoin();
    }
    
    // Если спустя некоторое время не дошли до монеты, возможно путь заблокирован
    // Периодически проверяем наличие ближайших монет
    coinCheckTimer += Time.deltaTime;
    if (coinCheckTimer >= coinCheckInterval)
    {
        coinCheckTimer = 0f;
        
        // Проверим, может появилась монета ближе или текущая недоступна
        GameObject[] coins = GameObject.FindGameObjectsWithTag("Coin");
        if (coins.Length > 0 && targetCoin != null)
        {
            foreach (GameObject coin in coins)
            {
                if (coin == null || coin == targetCoin) continue;
                
                // Если нашли монету ближе текущей на 30%, пойдем к ней
                if (Vector3.Distance(transform.position, coin.transform.position) < 
                    Vector3.Distance(transform.position, targetCoin.transform.position) * 0.7f)
                {
                    targetCoin = coin;
                    StartCoroutine(SetDestinationRoutine(targetCoin.transform.position));
                    break;
                }
            }
        }
    }
}
private float coinCheckTimer = 0f;
private readonly float coinCheckInterval = 1.5f; // проверка каждые 1.5 секунды
    // Поиск ближайшей монеты
   private void FindNearestCoin()
{
    // Явно ищем все монеты с тегом Coin
    GameObject[] coins = GameObject.FindGameObjectsWithTag("Coin");
    
    Debug.Log($"Бот {gameObject.name}: найдено {coins.Length} монет");
    
    if (coins.Length == 0)
    {
        // Если монет не осталось - просто выбираем случайную точку
        Vector3 randomPoint = RandomNavPoint(transform.position, wanderRadius * 0.75f);
        StartCoroutine(SetDestinationRoutine(randomPoint));
        return;
    }

    // Сортируем монеты по расстоянию для эффективности
    System.Array.Sort(coins, (a, b) => 
        Vector3.Distance(transform.position, a.transform.position)
        .CompareTo(Vector3.Distance(transform.position, b.transform.position)));
    
    // Проходим по отсортированному списку, начиная с ближайших
    foreach (GameObject coin in coins)
    {
        if (coin == null) continue;
        
        // Проверяем путь к монете
        NavMeshPath path = new NavMeshPath();
        if (agent.CalculatePath(coin.transform.position, path) && 
            path.status == NavMeshPathStatus.PathComplete)
        {
            targetCoin = coin;
            StartCoroutine(SetDestinationRoutine(targetCoin.transform.position));
            agent.speed = runSpeed; // Бежим за монетами
            Debug.Log($"Бот {gameObject.name} направляется к монете на расстоянии {Vector3.Distance(transform.position, coin.transform.position)}");
            return;
        }
    }
    
    // Если не нашли доступных монет - просто выбираем случайную точку
    Vector3 randPoint = RandomNavPoint(transform.position, wanderRadius * 0.75f);
    StartCoroutine(SetDestinationRoutine(randPoint));
}
    // "Сбор" монеты
    private void CollectCoin(GameObject coin)
    {
        if (coin != null)
        {
            // Здесь может быть логика для подсчета монет, эффекты и т.д.
            Debug.Log($"Бот {gameObject.name} собрал монету!");
            Destroy(coin);
        }
    }

    // Выбор случайного waypoint
    private void PickRandomWaypoint()
    {
        // Если с последней смены точки прошло меньше времени, чем pathChangeDelay - продолжаем идти к текущей точке
        if (Time.time - lastWaypointChangeTime < pathChangeDelay && currentWaypoint != null)
        {
            StartCoroutine(SetDestinationRoutine(currentWaypoint.position));
            return;
        }
        
        if (waypoints == null || waypoints.Length == 0) 
        {
            Vector3 randomPoint = RandomNavPoint(transform.position, wanderRadius);
            StartCoroutine(SetDestinationRoutine(randomPoint));
            return;
        }

        // Создаем список доступных вейпоинтов, исключая недавние
        List<Transform> availableWaypoints = new List<Transform>();
        
        foreach (var wp in waypoints)
        {
            if (wp == null) continue;
            
            if (!recentWaypoints.Contains(wp) && 
                wp != currentWaypoint && 
                Vector3.Distance(transform.position, wp.position) > minDistanceToWaypoint)
            {
                availableWaypoints.Add(wp);
            }
        }

        // Если список пуст, расширяем поиск, разрешая точки на минимальном расстоянии
        if (availableWaypoints.Count == 0)
        {
            foreach (var wp in waypoints)
            {
                if (wp == null) continue;
                
                if (!recentWaypoints.Contains(wp) && wp != currentWaypoint)
                {
                    availableWaypoints.Add(wp);
                }
            }
        }
        
        // Если всё еще пусто - берем любые, кроме текущей
        if (availableWaypoints.Count == 0)
        {
            foreach (var wp in waypoints)
            {
                if (wp == null) continue;
                
                if (wp != currentWaypoint)
                {
                    availableWaypoints.Add(wp);
                }
            }
        }

        // Выбираем новую точку
        if (availableWaypoints.Count > 0)
        {
            Transform selected;
            
            if (isPreferFarthestPoint)
            {
                // Стратегия "выбирать самую дальнюю точку" из доступных
                selected = FindFarthestWaypoint(availableWaypoints);
            }
            else
            {
                // Стратегия "выбирать случайную точку" из доступных
                selected = availableWaypoints[Random.Range(0, availableWaypoints.Count)];
            }
            
            // Обновляем историю посещений
            UpdateWaypointHistory(selected);
            
            // Устанавливаем новую точку
            currentWaypoint = selected;
            StartCoroutine(SetDestinationRoutine(selected.position));
            
            // Обновляем время последней смены маршрута
            lastWaypointChangeTime = Time.time;
        }
        else
        {
            // Fallback на случайную точку, если доступных вейпоинтов нет
            Vector3 randomPoint = RandomNavPoint(transform.position, wanderRadius);
            StartCoroutine(SetDestinationRoutine(randomPoint));
            
            // Сбрасываем текущий вейпоинт
            currentWaypoint = null;
            
            // Обновляем время последней смены маршрута
            lastWaypointChangeTime = Time.time;
        }
    }

    // Обновление истории посещенных точек
    private void UpdateWaypointHistory(Transform waypoint)
    {
        // Добавляем новую точку в историю
        recentWaypoints.Add(waypoint);
        
        // Удаляем старые, если история слишком длинная
        while (recentWaypoints.Count > historyLength)
        {
            recentWaypoints.RemoveAt(0);
        }
    }

    // Поиск самой дальней точки
    private Transform FindFarthestWaypoint(List<Transform> waypoints)
    {
        Transform farthest = null;
        float maxDistance = 0f;
        
        foreach (var wp in waypoints)
        {
            if (wp == null) continue;
            
            float distance = Vector3.Distance(transform.position, wp.position);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                farthest = wp;
            }
        }
        
        return farthest ?? waypoints[Random.Range(0, waypoints.Count)];
    }
    
    // Установка точки назначения
    private IEnumerator SetDestinationRoutine(Vector3 destination)
{
    isChangingDestination = true;
    
    // Проверяем, можно ли установить точку на NavMesh
    NavMeshHit hit;
    if (NavMesh.SamplePosition(destination, out hit, 3f, NavMesh.AllAreas))
    {
        destination = hit.position;
    }
    
    // НОВЫЙ КОД: Проверяем наличие препятствий по прямой линии
    Vector3 avoidancePoint = FindPathAroundObstacle(transform.position, destination);
    
    // НОВЫЙ КОД: Если была найдена точка обхода, сначала идём к ней
    if (avoidancePoint != destination)
    {
        // Добавляем промежуточную точку для обхода препятствия
        NavMeshPath partialPath = new NavMeshPath();
        agent.CalculatePath(avoidancePoint, partialPath);
        agent.SetPath(partialPath);
        
        // Ждем, пока бот не подойдет ближе к промежуточной точке
        yield return new WaitUntil(() => 
            Vector3.Distance(transform.position, avoidancePoint) < 2f || 
            !agent.hasPath);
            
        // Затем устанавливаем окончательную точку назначения
        NavMeshPath finalPath = new NavMeshPath();
        agent.CalculatePath(destination, finalPath);
        agent.SetPath(finalPath);
    }
    else
    {
        // СУЩЕСТВУЮЩИЙ КОД: Просто устанавливаем конечную точку назначения
        NavMeshPath path = new NavMeshPath();
        if (agent.CalculatePath(destination, path) && path.status == NavMeshPathStatus.PathComplete)
        {
            agent.SetPath(path);
        }
        else
        {
            agent.SetDestination(destination);
        }
    }

    lastPathUpdateTime = Time.time;
    yield return new WaitForSeconds(0.1f);
    
    isChangingDestination = false;
}

    // Новый метод: двойной прыжок
    private IEnumerator DoubleJumpRoutine()
    {
        isChangingDestination = true;
        
        // Выбираем случайное направление для более интересного движения
        Vector3 direction = agent.velocity.sqrMagnitude > 0.1f ? 
                          agent.velocity.normalized : transform.forward;
        
        // Добавляем случайный поворот
        direction = Quaternion.Euler(0, Random.Range(-40f, 40f), 0) * direction;

        anim.SetTrigger("Jump");
        agent.enabled = false;
        rb.isKinematic = false;
        
        // Первый прыжок
        rb.AddForce(Vector3.up * jumpForce * 1.1f + direction * forwardBoost * 0.7f, ForceMode.Impulse);

        // Ждем на пике прыжка
        yield return new WaitForSeconds(0.3f);
        
        // Второй прыжок
        rb.velocity = new Vector3(rb.velocity.x * 0.2f, 0, rb.velocity.z * 0.2f);
        rb.AddForce(Vector3.up * jumpForce * 0.9f + direction * forwardBoost * 1.2f, ForceMode.Impulse);

        yield return new WaitUntil(() => Physics.Raycast(
            transform.position + Vector3.up * 0.1f, Vector3.down, 0.3f));

        rb.velocity = Vector3.zero;
        rb.isKinematic = true;
        agent.enabled = true;
        
        yield return new WaitForSeconds(0.3f);
        isChangingDestination = false;
    }

    // Улучшенный прыжок
    private IEnumerator JumpRoutine(bool isLobbyJump = false)
    {
        isChangingDestination = true;
        
        // Настраиваем силу и направление прыжка
        float actualJumpForce = jumpForce;
        float actualForwardBoost = forwardBoost;
        
        if (isLobbyJump)
        {
            // В лобби прыжки более разнообразные
            actualJumpForce *= Random.Range(0.8f, 1.4f);
            actualForwardBoost *= Random.Range(0.6f, 1.8f);
        }
        
        Vector3 direction = agent.velocity.sqrMagnitude > 0.1f ? 
                          agent.velocity.normalized : transform.forward;
                          
        // Случайный поворот для разнообразия траекторий
        if (isLobbyJump && Random.value > 0.3f)
        {
            float randomTurn = Random.Range(-60f, 60f);
            direction = Quaternion.Euler(0, randomTurn, 0) * direction;
        }

        anim.SetTrigger("Jump");
        agent.enabled = false;
        rb.isKinematic = false;
        rb.AddForce(Vector3.up * actualJumpForce + direction * actualForwardBoost, ForceMode.Impulse);

        yield return new WaitUntil(() => Physics.Raycast(
            transform.position + Vector3.up * 0.1f, Vector3.down, 0.25f));

        rb.velocity = Vector3.zero;
        rb.isKinematic = true;
        agent.enabled = true;
        
        yield return new WaitForSeconds(0.3f); // Сокращено для более быстрой реакции
        isChangingDestination = false;
    }

    // Автопрыжок через препятствия
    private void TrySmallJump()
    {
        if (agent.isStopped || !agent.isOnNavMesh || isChangingDestination) return;

        Vector3 direction = agent.velocity.normalized;
        if (direction.sqrMagnitude < 0.01f) return;

        Vector3 origin = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(origin, direction, out var hit, checkDistance))
        {
            float height = hit.point.y - transform.position.y;
            if (height > 0f && height <= jumpableHeight)
            {
                StartCoroutine(JumpRoutine());
            }
        }
    }

    // Управление анимациями
    private void HandleAnimations()
    {
        if (!rb.isKinematic) return;

        float speed = new Vector3(agent.velocity.x, 0, agent.velocity.z).magnitude;
        string targetAnim = speed < walkThreshold - buffer ? "Idle" :
                          speed < runThreshold - buffer ? "Walk" : "Run";

        if (currentAnim != targetAnim)
        {
            anim.Play(targetAnim);
            currentAnim = targetAnim;
        }
    }

    // Поиск waypoints
    private void FindWaypoints()
    {
        GameObject[] wpObjects = GameObject.FindGameObjectsWithTag("Waypoint");
        waypoints = new Transform[wpObjects.Length];
        for (int i = 0; i < wpObjects.Length; i++)
        {
            waypoints[i] = wpObjects[i].transform;
        }
    }

    // Генерация случайной точки на NavMesh
    private Vector3 RandomNavPoint(Vector3 origin, float radius)
    {
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomPoint = origin + Random.insideUnitSphere * radius;
            if (NavMesh.SamplePosition(randomPoint, out var hit, 1f, NavMesh.AllAreas))
            {
                return hit.position;      
            }
        }
        return origin; 
    }

    // Обработчики событий GameController
    private void OnRoundStart(Transform[] spawnPoints)
    {
        currentState = BotState.Roaming;
        inRound = true;
        agent.isStopped = false;
        agent.speed = runSpeed;
        recentWaypoints.Clear();
        targetCoin = null;

        Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
        agent.Warp(spawn.position);
        transform.rotation = spawn.rotation;

        if (waypoints == null || waypoints.Length == 0)
        {
            FindWaypoints();
        }

        PickRandomWaypoint();
    }

    private void OnReturnLobby(Transform[] lobbyPoints)
    {
        currentState = BotState.Lobby;
        inRound = false;
        agent.isStopped = false; // Изменено с true на false, чтобы боты сразу начинали перемещаться
        agent.speed = walkSpeed;
        currentWaypoint = null;
        recentWaypoints.Clear();
        targetCoin = null;

        Transform lobbyPoint = lobbyPoints[Random.Range(0, lobbyPoints.Length)];
        agent.Warp(lobbyPoint.position);
        transform.rotation = lobbyPoint.rotation;

        // Уменьшаем задержку до первого действия для большей активности
        nextLobbyAction = Time.time + Random.Range(lobbyWalkTime.x * 0.5f, lobbyWalkTime.y * 0.7f);
    }
    
    // Обработчики событий для режима сбора монет
    private void OnCoinCollectionStarted()
    {
        currentState = BotState.CoinCollection;
        agent.isStopped = false;
        agent.speed = runSpeed;
        targetCoin = null;
        
        // Сохраняем текущие вейпоинты и отключаем их на время сбора монет
        waypointsBackup = waypoints;
        waypoints = new Transform[0]; // Очищаем вейпоинты
        currentWaypoint = null;      // Сбрасываем текущий вейпоинт
        
        // Сразу ищем первую монету
        FindNearestCoin();
    }
// Добавить этот метод в класс BotAI
private Vector3 FindPathAroundObstacle(Vector3 currentPosition, Vector3 targetPosition)
{
    // Создаем луч от текущей позиции к целевой
    Vector3 direction = (targetPosition - currentPosition).normalized;
    float distance = Vector3.Distance(currentPosition, targetPosition);
    
    // Проверяем, есть ли препятствие на пути
    if (Physics.Raycast(currentPosition + Vector3.up * 0.5f, direction, out RaycastHit hit, distance))
    {
        Debug.DrawLine(currentPosition, hit.point, Color.red, 1f);
        
        // Определяем нормаль к поверхности препятствия
        Vector3 obstacleNormal = hit.normal;
        
        // Выбираем направление обхода (вправо или влево от препятствия)
        Vector3 avoidanceDirection;
        
        // Создаем вектор поворота перпендикулярно к нормали
        Vector3 rotationAxis = Vector3.up;
        Vector3 rotatedNormal = Quaternion.AngleAxis(90, rotationAxis) * obstacleNormal;
        
        // Проверяем оба направления (вправо и влево)
        bool rightClear = !Physics.Raycast(hit.point, rotatedNormal, 3f);
        bool leftClear = !Physics.Raycast(hit.point, -rotatedNormal, 3f);
        
        if (rightClear && !leftClear)
        {
            avoidanceDirection = rotatedNormal; // Обход справа
        }
        else if (!rightClear && leftClear)
        {
            avoidanceDirection = -rotatedNormal; // Обход слева
        }
        else
        {
            // Если оба направления свободны, выбираем случайное
            avoidanceDirection = Random.value > 0.5f ? rotatedNormal : -rotatedNormal;
        }
        
        // Вычисляем точку обхода
        Vector3 avoidancePoint = hit.point + avoidanceDirection * 3f;
        
        // Проверяем, валидна ли точка на NavMesh
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(avoidancePoint, out navHit, 5f, NavMesh.AllAreas))
        {
            return navHit.position;
        }
    }
    
    // Если препятствий нет или не удалось найти обходной путь, возвращаем исходную точку
    return targetPosition;
}
    private void OnCoinCollectionEnded()
    {
        targetCoin = null;
        
        // Восстанавливаем вейпоинты из бэкапа
        if (waypointsBackup != null)
        {
            waypoints = waypointsBackup;
            waypointsBackup = null;
        }
        
        // Возвращаемся к обычному режиму патрулирования или лобби
        currentState = inRound ? BotState.Roaming : BotState.Lobby;
    }

    void OnEnable()
    {
        GameController.OnSpawnAll += OnRoundStart;
        GameController.OnReturnAll += OnReturnLobby;
        GameController.OnCoinCollectionStart += OnCoinCollectionStarted;
        GameController.OnCoinCollectionEnd += OnCoinCollectionEnded;
    }

    void OnDisable()
    {
        GameController.OnSpawnAll -= OnRoundStart;
        GameController.OnReturnAll -= OnReturnLobby;
        GameController.OnCoinCollectionStart -= OnCoinCollectionStarted;
        GameController.OnCoinCollectionEnd -= OnCoinCollectionEnded;
    }

    // Отладка
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, lobbyRadius);

        if (agent != null && agent.hasPath)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, agent.destination);
            Gizmos.DrawSphere(agent.destination, 0.3f);
        }
        
        // Рисуем историю посещенных точек
        if (recentWaypoints != null && recentWaypoints.Count > 0)
        {
            Gizmos.color = Color.red;
            foreach (var wp in recentWaypoints)
            {
                if (wp != null)
                {
                    Gizmos.DrawSphere(wp.position, 0.5f);
                }
            }
        }
        
        // Рисуем целевую монету
        if (targetCoin != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetCoin.transform.position);
            Gizmos.DrawWireSphere(targetCoin.transform.position, 0.5f);
        }
    }
}