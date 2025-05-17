using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Пошагово создаёт ботов и отправляет их на стартовую платформу.
/// </summary>
public class BotManager : MonoBehaviour
{
    [Header("Префаб бота (должен содержать BotAI + NavMeshAgent)")]
    [SerializeField] private GameObject[] botPrefab;

    [Header("Сколько ботов создать")]
     int botCount;

    [Header("Интервал между спавном (сек)")]
    [SerializeField] private float spawnInterval = 1f;

    public static BotManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public  List<BotAI> bots = new List<BotAI>();

    private void Start()
    {
        botCount = Random.Range(GameController.Instance.gameData.minBot, GameController.Instance.gameData.maxBot);
        StartCoroutine(SpawnRoutine());
    }

    public void SetBotCount(int count)
{
    botCount = count;
}

    public IEnumerator SpawnRoutine()
    {
        for (int i = 0; i < botCount; i++)
        {
            Debug.Log("SpawnBot");
            GameObject bot = botPrefab[Random.Range(0, botPrefab.Length)];
            Instantiate(bot, GameController.Instance.RandomStarPointPosition().position, Quaternion.identity );
            BotAI ai = bot.GetComponent<BotAI>();
            ai.thisBotParamtrs = GameController.Instance.RegistredBot();
            bots.Add(ai);


            yield return new WaitForSeconds(spawnInterval);
        }
    }
}
