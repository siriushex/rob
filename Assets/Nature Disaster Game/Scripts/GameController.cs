using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Unity.VisualScripting;
public class GameController : MonoBehaviour
{

    [Header("Scriptable_object Data")]
    public GameData gameData;

    [Header("Link")]
    [SerializeField] private Transform playerPrefab;
    [Header("UI")]
    [SerializeField] private UIAnimator WelcomePanel;
    [SerializeField] private DisasterWarning_UI DisasterWarning;
    [SerializeField] private Transform DisasterWarningParent;
   public UI_GameController uI_GameController;
    [SerializeField] private MultiDisasterUi_inGame multiDisasterUi;
    private List<Disaster> currentDisasters = new List<Disaster>();

    public static GameController Instance;

    public static event Action onCloud;
    public static event Action onFog;

    public Transform[] startSpawnPoint;

    public static event System.Action<Transform[]> OnSpawnAll;
    public static event System.Action<Transform[]> OnReturnAll;

    public List<PlayerParametrs> alivePlayer_list = new List<PlayerParametrs>();
    private int lastId = 0;

    [SerializeField] private Transform mapsContainer;

    private MapsData currentMap;

    [SerializeField] private Transform coinSpawnContainer;
    [SerializeField] private Coin coin;

    [Header("Disaster")]
    [SerializeField] private GameObject Tornado, BlackHole;

    public int MultiDisaster = 1;
  public static event Action OnCoinCollectionStart;
    public static event Action OnCoinCollectionEnd;

    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        TeleportAllToStart();
          WelcomePanel.Show();
    }

   public void HideWelcomePanel()
    {
        WelcomePanel.Hide();
    }


    public  void RigistredPlayer()
    {
        Debug.Log("RigistredPlayer");
        PlayerParametrs newPlayer;
        newPlayer = gameData.player;
        newPlayer.playerId = 0;
        alivePlayer_list.Add(newPlayer);
    }

    public PlayerParametrs RegistredBot()
    {
        Debug.Log("RegistredBot");
        PlayerParametrs newPlayer;
        newPlayer = GeneratePlayerParametrs();
        newPlayer.playerId = GetNewID();
        alivePlayer_list.Add(newPlayer);
        return newPlayer;
    }

    // ��������� ���������� ID
    private int GetNewID()
    {
        lastId++;
        return lastId;
    }


    public PlayerParametrs GeneratePlayerParametrs()
    {
        PlayerParametrs playerParametrs = new PlayerParametrs();
        playerParametrs.player_Icon = gameData.player_Icons[UnityEngine.Random.Range(0, gameData.player_Icons.Length)];
        playerParametrs.player_Name = gameData.player_Names[UnityEngine.Random.Range(0, gameData.player_Names.Length)];
        return playerParametrs;

    }

    private void OnEnable()
    {
        DisasterTimer.Instance.DisasterEnd += OnDisasterEndEvent;
        weatrherMachine.addMultipleDisaster += AddDisasterCount;
    }
    public void StartGame()
    {
        HideWelcomePanel();
        StartCoroutine(StartGameCur());
    }


    public void GenerateRandomMap()
    {
        currentMap = gameData.maps[UnityEngine.Random.Range(0, gameData.maps.Length)];

    }

    public void OnDisasterEndEvent()
    {
        GetCoinLevel();
        //��������� VFX ��������
        //������� ������ ��������
        //�������� ������ �������� ����� ����� �������� 10-15 ���.
    }

    public void ClearLives(int playerId, GameObject playerObj)
    {
        var player = alivePlayer_list.FirstOrDefault(p => p.playerId == playerId);
        if (player != null)
        {
            player.isDie = true;
            alivePlayer_list.Remove(player);
            Debug.Log($"����� {player.player_Name} (ID: {playerId}) �����");
            if (playerObj != null)
            {
                playerObj.transform.position = RandomStarPointPosition().position;
            }
        }

    }

    public void TeleportAllToMap()
    {
        Debug.Log("TeleportAllToMap");
        GameObject currentMap = GameObject.FindGameObjectWithTag("Map");
        Transform[] SpawnPos = currentMap.GetComponent<SpawnPos>().GetSpawnPoint();
        OnSpawnAll?.Invoke(SpawnPos);
        SetCurrentMod();
        Invoke("StartTimerToDisaster", 2f);
        Debug.Log("TeleportEnd");
    }

    void SetCurrentMod()
    {
        for (int i = 0; i < MultiDisaster; i++)
        {
            Disaster curDisaster = gameData.disasters[UnityEngine.Random.Range(0, gameData.disasters.Length)];
            currentDisasters.Add(curDisaster);
        }

        foreach(Disaster curDisaster in currentDisasters)
        {
            if (curDisaster.hasCloud)
            {
                Debug.Log("cloud is coming");
                onCloud?.Invoke();
            }
            if (curDisaster.hasFog)
            {
                onFog?.Invoke();
            }
        }
        
    }

    public void AddDisasterCount()
    {
        MultiDisaster++;    
    }

    public void SpawnNewPlayer()
    {
       // Instantiate(playerPrefab, startSpawnPoint[UnityEngine.Random.Range(0, startSpawnPoint.Length)].position, Quaternion.identity);
        Transform spawnPoint = startSpawnPoint[UnityEngine.Random.Range(0, startSpawnPoint.Length)];
    GameObject player = Instantiate(playerPrefab.gameObject, spawnPoint.position, Quaternion.identity);
    
    // Получаем ссылку на компонент контроллера
    var controller = player.GetComponent<Invector.vCharacterController.vThirdPersonController>();
    
    if (controller != null)
    {
        // Устанавливаем ID игрока
        controller.playerId = 0;
        
        // Обновляем ссылки в vGameController
        if (Invector.vGameController.instance != null)
        {
            Invector.vGameController.instance.currentPlayer = player;
            Invector.vGameController.instance.currentController = controller;
            
            // ВАЖНО: Подключаем обработчик смерти к новому игроку
            controller.onDead.AddListener(Invector.vGameController.instance.OnCharacterDead);
        }
    }
    }

    public void TeleportAllToStart()
    {
        OnReturnAll?.Invoke(startSpawnPoint);
    }

    public IEnumerator StartGameCur()
    {
        Debug.Log("StartGame");
        GenerateRandomMap();
        yield return new WaitForSeconds(gameData.nextRound_startTime);
        Debug.Log("SetnewMap_0");
        SetNewMap();
        yield return new WaitForSeconds(2f);
        StartCoroutine(uI_GameController.MapPreview(currentMap));
        yield return new WaitForSeconds(4f);
        TeleportAllToMap();
        if(MultiDisaster > 1)
        {
            multiDisasterUi.Initialize(MultiDisaster);
        }
    }
public void ClearAllDisasterEffects()
{
    // Отключаем все объекты катастроф
    if (Tornado) Tornado.SetActive(false);
    if (BlackHole) BlackHole.SetActive(false);
    
    // Находим и отключаем другие эффекты катастроф
   TsunamiDisaster tsunami = FindObjectOfType<TsunamiDisaster>();
    if (tsunami) Destroy(tsunami.gameObject);
    
    AcidRainController acidRain = FindObjectOfType<AcidRainController>();
    if (acidRain) acidRain.StopAcidRain();
    
    // Находим и останавливаем любые другие эффекты катастроф
    // [Здесь добавьте код для других типов катастроф]
    
    // Очищаем список активных катастроф
    currentDisasters.Clear();
}
    public void SetNewMap()
    {
        ClearCurrentMap();
        Instantiate(currentMap.map, mapsContainer);
    }

    public void ClearCurrentMap()
    {
        foreach (Transform oldMaps in mapsContainer)
        {
            Destroy(oldMaps.gameObject);
        }
    }

  public void ClearAllBot()
{
    BotAI[] bots = FindObjectsByType<BotAI>(FindObjectsSortMode.None);
    foreach (var bot in bots)
    {
        Destroy(bot.gameObject);
    }
    
    // Очищаем список живых ботов
    alivePlayer_list.RemoveAll(p => p.playerId > 0);
}

    /*
    void SpawnCharacter()
    {
        GameObject currentMap = GameObject.FindGameObjectWithTag("Map");
       Transform RandomSpawnPos = currentMap.GetComponent<SpawnPos>().GetRandomPos();
        player.position = RandomSpawnPos.position;
        player.rotation = RandomSpawnPos.rotation;
        Invoke("StartTimerToDisaster", 2f);
    }
    */
    /*
   
    */
    public Transform RandomStarPointPosition()
    {
        Transform point = startSpawnPoint[UnityEngine.Random.Range(0, startSpawnPoint.Length)];
        return point;
    }
    void StartTimerToDisaster()
    {
        DisasterTimer.Instance.StartTime(gameData.disasterWaitTime, true);
    }

    List<Vector3> coinPosList = new List<Vector3>();

//[SerializeField] private Transform botDanceSpawnContainer;

  public void GetCoinLevel()
{
      Debug.Log("CoinLevel: Активация режима сбора монет");
    ClearCurrentMap();
    coinPosList.Clear(); // Очищаем список точек для монет

    // Загружаем карту для монет
    foreach (Transform child in coinSpawnContainer)
    {
        coinPosList.Add(child.position);
    }
    
    // Спавним монеты на карте
    for (int i = 0; i < 20; i++)
    {
        Vector3 randomPos = coinPosList[UnityEngine.Random.Range(0, coinPosList.Count)];
        SpawnCoinInRandomPos(randomPos);
    }

   // BotManager botManager = GetComponent<BotManager>();
    
   // foreach(Transform child in coinSpawnContainer)
    //{
      // child.gameObject.GetComponent<MeshRenderer>().enabled = false;   
    //   GameObject DanceBot = botManager.botPrefab[UnityEngine.Random.Range(0, botManager.botPrefab.Length)];
     //  Instantiate(DanceBot, child.position, Quaternion.identity);
   // }
    // Получаем безопасные позиции для спавна игроков/ботов
    if (MapArea.Instance != null)
    {
        // Телепортируем игроков/ботов на случайные точки в безопасной зоне
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        
        // Получаем список случайных позиций в безопасной зоне
        List<Vector3> safePositions = MapArea.Instance.GetRandomSafePositions(players.Length);
        
        // Телепортируем каждого игрока/бота
        for (int i = 0; i < players.Length; i++)
        {
            Vector3 spawnPos = (i < safePositions.Count) ? 
                safePositions[i] : 
                MapArea.Instance.GetRandomSafePosition();
                players[i].transform.position = spawnPos;
        }
    }
    
    // Запускаем обратный отсчет для режима сбора монет
    DisasterTimer.Instance.StartTime(15, false);
    DisasterTimer.Instance.DisasterEnd += OnCoinCollectionTimeEnd;

     GameObject[] bots = GameObject.FindGameObjectsWithTag("Player");
    foreach (var botObj in bots)
    {
        BotAI bot = botObj.GetComponent<BotAI>();
        if (bot != null)
        {
            // Напрямую вызываем переход в режим сбора
            bot.SendMessage("OnCoinCollectionStarted", SendMessageOptions.DontRequireReceiver);
        }
    }
    // Запускаем режим сбора монет для ботов
    if (OnCoinCollectionStart != null)
        OnCoinCollectionStart();
}

// Новый обработчик для завершения режима сбора монет по таймеру
private void OnCoinCollectionTimeEnd()
{
    DisasterTimer.Instance.DisasterEnd -= OnCoinCollectionTimeEnd;
    
    // Завершаем режим сбора монет
    if (OnCoinCollectionEnd != null)
        OnCoinCollectionEnd();
    
    TeleportAllToStart();
    uI_GameController.OpenSurvivePlayersList();
}

    IEnumerator WaitCoinLvl()
    {
        yield return new WaitForSeconds(15f);
        
        // Завершаем режим сбора монет
        OnCoinCollectionEnd?.Invoke();
        
        TeleportAllToStart();
        uI_GameController.OpenSurvivePlayersList();
    }

    void SpawnCoinInRandomPos(Vector3 pos)
    {
        Debug.Log("SpawnCoin");
        Instantiate(coin.gameObject, pos, Quaternion.identity);
    }

    public IEnumerator GenerateDisaster()
    {
        Debug.Log("GenerateDisaster");
        foreach(Disaster disaster in currentDisasters)
        {
             DisasterWarning_UI disasterWarning_UI = Instantiate(DisasterWarning, DisasterWarningParent);
            disasterWarning_UI.Init(disaster);
            yield return new WaitForSeconds(1.5f);
        }
        //Debug.Log("DISASTER IS COMING_0");
       // yield return new WaitForSeconds(5);
        //DisasterWarning.StopShowWarning();
        DisasterTimer.Instance.StartTime(gameData.roundTime, false);
        Debug.Log("DISASTER IS COMING");

        var reactives = GameObject.FindObjectsOfType<MonoBehaviour>().OfType<IDisasterReactive>();
        foreach (Disaster disaster in currentDisasters)
        {
            switch (disaster.type)
            {
                case Disaster.DisasterType.Flood:
                    GameObject.FindObjectOfType<FloodDisaster>().StartFlood();
                    foreach (var r in reactives)
                    {
                        r.OnFlood();
                    }
                    break;
                case Disaster.DisasterType.Hurricane:
                    Instantiate(Tornado);
                    foreach (var r in reactives)
                    {
                        r.OnTornado();
                    }
                    break;
                case Disaster.DisasterType.Volcano:
                    GameObject.FindObjectOfType<Vulcano>().ShowVulcano();
                    foreach (var r in reactives)
                    {
                        r.OnVolcanoEroption();
                    }
                    break;
                case Disaster.DisasterType.AxidRain:
                    GameObject.FindObjectOfType<AcidRainController>().StartAcidRain();
                    foreach (var r in reactives)
                    {
                        r.OnAcidRain(gameData.stateMaterials);
                    }
                    break;
                case Disaster.DisasterType.BlackHole:
                    Instantiate(BlackHole);
                    foreach (var r in reactives)
                    {
                        r.OnBlackHole();
                    }
                    break;
                case Disaster.DisasterType.Fire:
                    FireSpreadManager.Instance.StartFire();
                    break;
                case Disaster.DisasterType.Explosion:
                    ExplosionDisasterManager.Instance.StartExplosionStorm(gameData.explosionSettings);
                    break;
                case Disaster.DisasterType.Tsunami:
                    TsunamiManager.Instance.StartTsunami() ;
                    break;


            }
        }

      
    }

}
