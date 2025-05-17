using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Invector
{
    using UnityEngine.Events;
    using vCharacterController;
    [vClassHeader("Simple GameController Example", openClose = false)]
    public class vGameController : vMonoBehaviour
    {
        [System.Serializable]
        public class OnRealoadGame : UnityEngine.Events.UnityEvent { }
        [vHelpBox("Assign your Character Prefab to be instantiate at the SpawnPoint, leave it unassigned to Restart the Scene instead")]
        public GameObject playerPrefab;
        [vHelpBox("Assign a empty transform to spawn the Player to a specific location")]
        public Transform spawnPoint;
        [vHelpBox("Time to wait until the scene restart or the player will be spawned again")]
        public float respawnTimer = 4f;
        [vHelpBox("Check this if you want to destroy the dead body after the respawn")]
        public bool destroyBodyAfterDead;
        [vHelpBox("Display a message using the FadeText UI")]
        public bool displayInfoInFadeText = true;

        [HideInInspector]
        public OnRealoadGame OnReloadGame = new OnRealoadGame();
        [HideInInspector]
        public GameObject currentPlayer;
        public vThirdPersonController currentController;
        public static vGameController instance;
        private GameObject oldPlayer;

        public UnityEvent onSpawn;
        public bool dontDestroyOnLoad = true;

        protected virtual void Start()
        {
            if (instance == null)
            {
                instance = this;
                if (dontDestroyOnLoad)
                {
                    DontDestroyOnLoad(this.gameObject);
                }

                this.gameObject.name = gameObject.name + " Instance";
            }
            else
            {
                Destroy(this.gameObject);
                return;
            }

            SceneManager.sceneLoaded += OnLevelFinishedLoading;
            if (displayInfoInFadeText && vHUDController.instance)
            {
                vHUDController.instance.ShowText("Init Scene");
            }

            FindPlayer();
        }

        /// <summary>
        /// Show/Hide Cursor
        /// </summary>
        /// <param name="value"></param>
        public virtual void ShowCursor(bool value)
        {
            Cursor.visible = value;
        }

        /// <summary>
        /// Lock/Unlock the cursor to the center of screen
        /// </summary>
        /// <param name="value"></param>
        public virtual void LockCursor(bool value)
        {
            if (value)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
            }
        }

       public  void OnCharacterDead(GameObject _gameObject)
        {
            oldPlayer = _gameObject;

            if (playerPrefab != null)
            {
                StartCoroutine(RespawnRoutine());
            }
            else
            {
                if (displayInfoInFadeText && vHUDController.instance)
                {
                    vHUDController.instance.ShowText("Restarting Scene...");
                }

                Invoke("ResetScene", respawnTimer);
            }
        }

        protected virtual IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnTimer);

            if (playerPrefab != null && spawnPoint != null)
            {
                if (oldPlayer != null && destroyBodyAfterDead)
                {
                    if (displayInfoInFadeText && vHUDController.instance)
                    {
                        vHUDController.instance.ShowText("Player destroyed: " + oldPlayer.name.Replace("(Clone)", "").Replace("Instance", ""));
                    }

                    Destroy(oldPlayer);
                }
                else
                {
                    if (displayInfoInFadeText && vHUDController.instance)
                    {
                        vHUDController.instance.ShowText("Remove Player Components: " + oldPlayer.name.Replace("(Clone)", "").Replace("Instance", ""));
                    }

                    DestroyPlayerComponents(oldPlayer);
                }

                yield return new WaitForEndOfFrame();

                currentPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
                currentController = currentPlayer.GetComponent<vThirdPersonController>();
                currentController.onDead.AddListener(OnCharacterDead);

                if (displayInfoInFadeText && vHUDController.instance)
                {
                    vHUDController.instance.ShowText("Respawn player: " + currentPlayer.name.Replace("(Clone)", ""));
                }

                Debug.Log("MAIN PlayerDied_1");
                
                OnReloadGame.Invoke();
                GameController.Instance.ClearLives(currentController.playerId, currentPlayer);
                onSpawn.Invoke();
            }
        }

        protected virtual void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
        {
            Debug.Log("MAIN PlayerDied_0");
            FindPlayer();
            if (currentController == null)
            {
                return;
            }

            if (currentController.currentHealth > 0)
            {
                if (displayInfoInFadeText && vHUDController.instance)
                {
                    vHUDController.instance.ShowText("Load Scene: " + scene.name);
                }

                return;
            }
            if (displayInfoInFadeText && vHUDController.instance)
            {
                vHUDController.instance.ShowText("Reload Scene");
            }
            Debug.Log("MAIN PlayerDied");

            OnReloadGame.Invoke();
        }

        protected virtual void FindPlayer()
        {
            var player = GameObject.FindObjectOfType<vThirdPersonController>();

            if (player)
            {
                currentPlayer = player.gameObject;
                currentController = player;
                player.onDead.AddListener(OnCharacterDead);
                if (displayInfoInFadeText && vHUDController.instance)
                {
                    vHUDController.instance.ShowText("Found player: " + currentPlayer.name.Replace("(Clone)", "").Replace("Instance", ""));
                }
            }
            else if (currentPlayer == null && playerPrefab != null && spawnPoint != null)
            {
                SpawnAtPoint(spawnPoint);
            }
        }

        protected virtual void DestroyPlayerComponents(GameObject target)
        {
            if (!target)
            {
                return;
            }

            var comps = target.GetComponentsInChildren<MonoBehaviour>();
            for (int i = 0; i < comps.Length; i++)
            {
                Destroy(comps[i]);
            }
            var coll = target.GetComponent<Collider>();
            if (coll != null)
            {
                Destroy(coll);
            }

            var rigidbody = target.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                Destroy(rigidbody);
            }

            var animator = target.GetComponent<Animator>();
            if (animator != null)
            {
                Destroy(animator);
            }
        }

        /// <summary>
        /// Set a custom spawn point (or use it as checkpoint to your level) 
        /// </summary>
        /// <param name="newSpawnPoint"> new point to spawn</param>
        public virtual void SetSpawnPoint(Transform newSpawnPoint)
        {
            spawnPoint = newSpawnPoint;
        }

        /// <summary>
        /// Set the default player prefab
        /// </summary>
        /// <param name="prefab"></param>
        public void SetPlayerPrefab(GameObject prefab)
        {
            playerPrefab = prefab;
        }

        /// <summary>
        /// Spawn New Player at a specific point
        /// </summary>
        /// <param name="targetPoint"> Point to spawn player</param>
        public virtual void SpawnAtPoint(Transform targetPoint)
        {
            if (playerPrefab != null)
            {
                if (oldPlayer != null && destroyBodyAfterDead)
                {
                    if (displayInfoInFadeText && vHUDController.instance)
                    {
                        vHUDController.instance.ShowText("Player destroyed: " + oldPlayer.name.Replace("(Clone)", "").Replace("Instance", ""));
                    }

                    Destroy(oldPlayer);
                }

                else if (oldPlayer != null)
                {
                    if (displayInfoInFadeText && vHUDController.instance)
                    {
                        vHUDController.instance.ShowText("Remove Player Components: " + oldPlayer.name.Replace("(Clone)", "").Replace("Instance", ""));
                    }

                    DestroyPlayerComponents(oldPlayer);
                }

                currentPlayer = Instantiate(playerPrefab, targetPoint.position, targetPoint.rotation);
                currentController = currentPlayer.GetComponent<vThirdPersonController>();
                currentController.onDead.AddListener(OnCharacterDead);
                OnReloadGame.Invoke();

                if (displayInfoInFadeText && vHUDController.instance)
                {
                    vHUDController.instance.ShowText("Spawn player: " + currentPlayer.name.Replace("(Clone)", ""));
                }
            }
        }

        /// <summary>
        /// Spawn player
        /// </summary>
        /// <param name="prefab"></param>
        public virtual void SpawnPlayer(GameObject prefab)
        {
            if (prefab != null && spawnPoint != null)
            {
                var targetPoint = spawnPoint;
                if (oldPlayer != null && destroyBodyAfterDead)
                {
                    if (displayInfoInFadeText && vHUDController.instance)
                    {
                        vHUDController.instance.ShowText("Player destroyed: " + oldPlayer.name.Replace("(Clone)", "").Replace("Instance", ""));
                    }

                    Destroy(oldPlayer);
                }

                else if (oldPlayer != null)
                {
                    if (displayInfoInFadeText && vHUDController.instance)
                    {
                        vHUDController.instance.ShowText("Remove Player Components: " + oldPlayer.name.Replace("(Clone)", "").Replace("Instance", ""));
                    }

                    DestroyPlayerComponents(oldPlayer);
                }

                currentPlayer = Instantiate(prefab, targetPoint.position, targetPoint.rotation);
                currentController = currentPlayer.GetComponent<vThirdPersonController>();
                currentController.onDead.AddListener(OnCharacterDead);
                OnReloadGame.Invoke();

                if (displayInfoInFadeText && vHUDController.instance)
                {
                    vHUDController.instance.ShowText("Spawn player: " + currentPlayer.name.Replace("(Clone)", ""));
                }
            }
        }

        /// <summary>
        /// Reload  current Scene and current Player
        /// </summary>
    public virtual void ResetScene()
{
    if (oldPlayer)
    {
        DestroyPlayerComponents(oldPlayer);
    }

    // Затемняем экран
    SceneTransitionManager.Instance.FadeIn(1f, 0f, () => {
        Debug.Log("MAIN PlayerDied_reset");
        
        // Показываем список выживших
    
        
        // Запускаем последовательность действий
        StartCoroutine(DelayedFadeOut(2.5f));
        
        StartCoroutine(EndSurvive());
        
    });
    
    if (oldPlayer && destroyBodyAfterDead)
    {
        Destroy(oldPlayer);
    }
}

// Вспомогательная корутина для задержки FadeOut
private IEnumerator DelayedFadeOut(float delay)
{
    // Увеличиваем задержку перед осветлением, чтобы не вызвать лаги
    yield return new WaitForSeconds(0.5f);
    // Осветление экрана происходит плавно
    SceneTransitionManager.Instance.FadeOut(1.5f, 0f);
}

IEnumerator EndSurvive()
{  
     // Спавним игрока ВАЖНО: используем наш обновленный метод SpawnNewPlayer
    GameController.Instance.SpawnNewPlayer();
    // Даем время для просмотра списка
    yield return new WaitForSeconds(2f);
    
    // Сбрасываем таймер
    if (DisasterTimer.Instance != null)
    {
        DisasterTimer.Instance.ResetAndHideTimer();
    }
        GameController.Instance.uI_GameController.OpenSurvivePlayersList();
    // Очищаем эффекты и ботов - разделяем по кадрам для избежания зависаний
    GameController.Instance.ClearAllDisasterEffects();
    yield return null;
    
    GameController.Instance.ClearAllBot();
    yield return null;
    
    GameController.Instance.ClearCurrentMap(); 
    yield return null;

      BotManager botManager = FindObjectOfType<BotManager>();
    if (botManager != null) 
    {
        Debug.Log("SpawnNewBots");
        // Задаем случайное количество ботов
        botManager.SetBotCount(Random.Range(GameController.Instance.gameData.minBot, 
                                          GameController.Instance.gameData.maxBot));
        StartCoroutine(botManager.SpawnRoutine());
    }
    else
    {
        Debug.LogWarning("BotManager not found in the scene.");
    }
    
    yield return new WaitForSeconds(5f);
    // Закрываем список выживших
    GameController.Instance.uI_GameController.CloseSurvivePlayersList();
    
   
    
    // Небольшая задержка
 //   yield return new WaitForSeconds(1f);
    
    // Осветляем экран
   // SceneTransitionManager.Instance.FadeOut(1.5f, 0f);
    
    // Запускаем новый раунд через некоторое время
    yield return new WaitForSeconds(2f);
    StartCoroutine(GameController.Instance.StartGameCur());
}
   }}
  