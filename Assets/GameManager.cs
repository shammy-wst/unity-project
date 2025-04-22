using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public int maxCubes = 5;
    public float timeBetweenWaves = 30f;
    public int enemiesPerWave = 5;
    public float enemySpawnInterval = 2f;
    
    [Header("UI References")]
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI cubeCountText;
    
    [Header("Prefabs")]
    public GameObject enemyPrefab;
    
    [Header("NavMesh")]
    public NavMeshSurface navMeshSurface;
    public float navMeshUpdateInterval = 1f;
    
    private int currentWave = 0;
    private int placedCubes = 0;
    private bool gameStarted = false;
    private List<GameObject> enemies = new List<GameObject>();
    private List<GameObject> defenseCubes = new List<GameObject>();
    private float nextNavMeshUpdate = 0f;
    private GameObject gameHUD;

    // Méthode publique pour vérifier si le jeu a commencé
    public bool HasGameStarted()
    {
        return gameStarted;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Vérifier si nous avons besoin de créer un Canvas pour le HUD
        if ((waveText == null || cubeCountText == null) && GameObject.Find("GameHUD") == null)
        {
            CreateGameHUD();
        }
        
        // S'assurer que le HUD est actif
        if (gameHUD != null)
        {
            gameHUD.SetActive(true);
        }
    }

    private void CreateGameHUD()
    {
        Debug.Log("Création d'un Canvas HUD pour les informations de jeu");
        
        // Créer un Canvas
        gameHUD = new GameObject("GameHUD");
        DontDestroyOnLoad(gameHUD); // Pour s'assurer qu'il ne disparaît pas lors des transitions
        
        Canvas canvas = gameHUD.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // S'assurer qu'il est au-dessus des autres Canvas
        
        // Ajouter un CanvasScaler
        CanvasScaler scaler = gameHUD.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Ajouter un GraphicRaycaster
        gameHUD.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Créer un Panel pour le fond
        GameObject panel = new GameObject("HUDPanel");
        panel.transform.SetParent(gameHUD.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0.9f); // Haut de l'écran
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.sizeDelta = new Vector2(0, 0);
        
        // Ajouter une image au panel
        UnityEngine.UI.Image panelImage = panel.AddComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0, 0, 0, 0.7f); // Noir semi-transparent
        
        // Créer le texte pour les vagues
        GameObject waveTextObj = new GameObject("WaveText");
        waveTextObj.transform.SetParent(panel.transform, false);
        RectTransform waveTextRect = waveTextObj.AddComponent<RectTransform>();
        waveTextRect.anchorMin = new Vector2(0, 0);
        waveTextRect.anchorMax = new Vector2(0.5f, 1);
        waveTextRect.offsetMin = new Vector2(20, 0);
        waveTextRect.offsetMax = new Vector2(-20, 0);
        
        // Ajouter le composant TextMeshPro
        TMPro.TextMeshProUGUI waveTextComponent = waveTextObj.AddComponent<TMPro.TextMeshProUGUI>();
        waveTextComponent.text = "Wave: 0";
        waveTextComponent.fontSize = 36;
        waveTextComponent.color = Color.white;
        waveTextComponent.alignment = TMPro.TextAlignmentOptions.Left;
        
        // Créer le texte pour le compteur de cubes
        GameObject cubeTextObj = new GameObject("CubeCountText");
        cubeTextObj.transform.SetParent(panel.transform, false);
        RectTransform cubeTextRect = cubeTextObj.AddComponent<RectTransform>();
        cubeTextRect.anchorMin = new Vector2(0.5f, 0);
        cubeTextRect.anchorMax = new Vector2(1, 1);
        cubeTextRect.offsetMin = new Vector2(20, 0);
        cubeTextRect.offsetMax = new Vector2(-20, 0);
        
        // Ajouter le composant TextMeshPro
        TMPro.TextMeshProUGUI cubeTextComponent = cubeTextObj.AddComponent<TMPro.TextMeshProUGUI>();
        cubeTextComponent.text = "Cubes: 0/5";
        cubeTextComponent.fontSize = 36;
        cubeTextComponent.color = Color.white;
        cubeTextComponent.alignment = TMPro.TextAlignmentOptions.Right;
        
        // Assigner les références
        waveText = waveTextComponent;
        cubeCountText = cubeTextComponent;
        
        // Cacher le HUD au début
        gameHUD.SetActive(false);
    }

    private void Start()
    {
        UpdateUI();
        if (navMeshSurface == null)
        {
            navMeshSurface = FindFirstObjectByType<NavMeshSurface>();
            if (navMeshSurface == null)
            {
                Debug.LogError("Aucun NavMeshSurface trouvé dans la scène ! Les ennemis ne pourront pas se déplacer.");
            }
            else
            {
                // Force la construction du NavMesh au démarrage
                navMeshSurface.BuildNavMesh();
            }
        }
    }
    
    private void Update()
    {
        // Mettre à jour régulièrement le NavMesh pour les surfaces AR qui changent
        if (navMeshSurface != null && Time.time > nextNavMeshUpdate)
        {
            navMeshSurface.BuildNavMesh();
            nextNavMeshUpdate = Time.time + navMeshUpdateInterval;
        }
        
        // Activer le HUD seulement quand le jeu a commencé
        if (gameHUD != null)
        {
            // Le HUD doit être actif seulement quand le jeu a commencé OU 
            // quand nous sommes en mode surface et que des cubes ont été placés
            bool shouldBeActive = gameStarted;
            
            // Si nous ne sommes pas en jeu mais en mode de placement, montrer le HUD si des cubes ont été placés
            if (!gameStarted && FindFirstObjectByType<PlaceOnPlane>() != null)
            {
                var placeOnPlane = FindFirstObjectByType<PlaceOnPlane>();
                if (placeOnPlane.isActiveAndEnabled && placedCubes > 0)
                {
                    shouldBeActive = true;
                }
            }
            
            if (gameHUD.activeSelf != shouldBeActive)
            {
                gameHUD.SetActive(shouldBeActive);
            }
        }
    }

    public bool CanPlaceCube()
    {
        return placedCubes < maxCubes;
    }

    public void AddCube(GameObject cube)
    {
        if (CanPlaceCube())
        {
            placedCubes++;
            defenseCubes.Add(cube);
            UpdateUI();

            // Mettre à jour le NavMesh immédiatement après avoir placé un cube
            if (navMeshSurface != null)
            {
                navMeshSurface.BuildNavMesh();
            }

            if (placedCubes >= maxCubes && !gameStarted)
            {
                StartGame();
            }
        }
    }

    private void StartGame()
    {
        gameStarted = true;
        Debug.Log("Le jeu commence ! Première vague d'ennemis...");
        
        // Activer le HUD une fois le jeu commencé
        if (gameHUD != null)
        {
            gameHUD.SetActive(true);
        }
        
        StartCoroutine(WaveSystem());
    }

    private IEnumerator WaveSystem()
    {
        while (gameStarted)
        {
            currentWave++;
            UpdateUI();
            Debug.Log($"Vague {currentWave} commence !");
            yield return StartCoroutine(SpawnWave());
            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    private IEnumerator SpawnWave()
    {
        // Attendre que les plans soient bien détectés
        yield return new WaitForSeconds(2f);
        
        // Construire le NavMesh avant de faire apparaître les ennemis
        if (navMeshSurface != null)
        {
            Debug.Log("Construction du NavMesh avant de faire apparaître les ennemis...");
            navMeshSurface.BuildNavMesh();
            
            // Attendre plus longtemps que le NavMesh soit construit
            yield return new WaitForSeconds(2f);
            
            // Force une seconde construction pour s'assurer que c'est à jour
            navMeshSurface.BuildNavMesh();
            yield return new WaitForSeconds(0.5f);
        }
        
        // Vérifier que le NavMesh est bien construit
        NavMeshHit hit;
        if (defenseCubes.Count > 0 && NavMesh.SamplePosition(defenseCubes[0].transform.position, out hit, 5f, NavMesh.AllAreas))
        {
            Debug.Log("NavMesh construit avec succès, position valide trouvée!");
        }
        else
        {
            Debug.LogWarning("Le NavMesh peut ne pas être correctement construit. Tentative de continuer...");
        }
        
        // Utiliser une position fixe pour le premier ennemi (pour débogage)
        Debug.Log("Tentative de création d'un ennemi à une position fixe pour déboguer");
        GameObject firstEnemy = CreateEnemyAtFixedPosition();
        if (firstEnemy != null)
        {
            Debug.Log("Premier ennemi créé avec succès à une position fixe!");
            
            // Forcer l'ennemi à trouver une cible immédiatement
            Enemy enemyScript = firstEnemy.GetComponent<Enemy>();
            if (enemyScript != null)
            {
                enemyScript.ForceUpdateTarget();
            }
            
            yield return new WaitForSeconds(enemySpawnInterval);
        }
        else
        {
            Debug.LogError("Échec de la création du premier ennemi. Tentative d'une approche différente...");
            yield return CreateEmergencyEnemy();
        }
        
        // Continuer avec le reste des ennemis
        for (int i = 1; i < enemiesPerWave; i++)
        {
            GameObject enemy = SpawnEnemy();
            if (enemy != null)
            {
                Debug.Log($"Ennemi {i+1}/{enemiesPerWave} créé");
                
                // Forcer l'ennemi à trouver une cible immédiatement
                Enemy enemyScript = enemy.GetComponent<Enemy>();
                if (enemyScript != null)
                {
                    enemyScript.ForceUpdateTarget();
                }
            }
            else
            {
                // Tentative de secours si la méthode normale échoue
                yield return CreateEmergencyEnemy();
            }
            yield return new WaitForSeconds(enemySpawnInterval);
        }
    }
    
    private IEnumerator CreateEmergencyEnemy()
    {
        Debug.Log("Tentative de création d'urgence d'un ennemi...");
        
        // Dernière chance - position fixe dans l'espace global
        if (defenseCubes.Count > 0)
        {
            Vector3 cubePosition = defenseCubes[0].transform.position;
            // Créer directement à la position du premier cube + un peu au-dessus
            Vector3 safePosition = cubePosition + Vector3.up * 1.5f;
            
            GameObject enemy = Instantiate(enemyPrefab, safePosition, Quaternion.identity);
            
            // Forcer une position et l'échelle pour la visibilité
            enemy.transform.position = safePosition;
            enemy.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            
            // Rendre l'ennemi plus visible avec un matériau brillant
            MeshRenderer renderer = enemy.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = Color.red;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.red * 2f);
                renderer.material = mat;
            }
            
            enemies.Add(enemy);
            Debug.Log($"Ennemi d'urgence créé à la position: {safePosition}");
            yield return enemy;
        }
        else
        {
            Debug.LogError("Impossible de créer un ennemi d'urgence, aucun cube de défense trouvé.");
            yield return null;
        }
    }
    
    private GameObject CreateEnemyAtFixedPosition()
    {
        // Créer l'ennemi à une position fixe par rapport au premier cube
        if (defenseCubes.Count > 0 && defenseCubes[0] != null)
        {
            Vector3 cubePosition = defenseCubes[0].transform.position;
            // Position à 1 mètre devant le cube (au lieu de 3 mètres qui peut être en dehors du NavMesh)
            Vector3 spawnPosition = cubePosition + new Vector3(0, 0.5f, 1f);
            
            // Faire plusieurs tentatives à différentes positions autour du cube
            NavMeshHit hit;
            float[] testDistances = new float[] { 1f, 1.5f, 2f, 3f, 0.5f };
            
            foreach (float distance in testDistances)
            {
                // Essayer différentes directions
                Vector3[] directions = new Vector3[] {
                    new Vector3(0, 0.5f, distance),
                    new Vector3(distance, 0.5f, 0),
                    new Vector3(-distance, 0.5f, 0),
                    new Vector3(0, 0.5f, -distance),
                    new Vector3(distance, 0.5f, distance),
                    new Vector3(-distance, 0.5f, -distance)
                };
                
                foreach (Vector3 dir in directions)
                {
                    Vector3 testPos = cubePosition + dir;
                    if (NavMesh.SamplePosition(testPos, out hit, 1f, NavMesh.AllAreas))
                    {
                        spawnPosition = hit.position;
                        
                        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
                        
                        // S'assurer que l'ennemi est bien visible
                        MeshRenderer renderer = enemy.GetComponent<MeshRenderer>();
                        if (renderer != null)
                        {
                            // Créer un matériau rouge brillant
                            Material mat = new Material(Shader.Find("Standard"));
                            mat.color = Color.red;
                            mat.EnableKeyword("_EMISSION");
                            mat.SetColor("_EmissionColor", Color.red * 2f); // Plus lumineux
                            renderer.material = mat;
                            
                            // S'assurer que l'échelle est suffisante
                            enemy.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                        }
                        
                        // Assurer que le NavMeshAgent est bien configuré
                        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
                        if (agent != null)
                        {
                            agent.Warp(spawnPosition); // Forcer la position sur le NavMesh
                        }
                        
                        enemies.Add(enemy);
                        Debug.Log($"Ennemi créé à la position {spawnPosition} après test de plusieurs positions");
                        return enemy;
                    }
                }
            }
            
            Debug.LogWarning("Position fixe n'est pas sur le NavMesh, essai au centre...");
            // Essayer directement à la position du cube
            spawnPosition = cubePosition;
            if (NavMesh.SamplePosition(spawnPosition, out hit, 2f, NavMesh.AllAreas))
            {
                spawnPosition = hit.position;
                GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
                
                // Même configuration que ci-dessus
                MeshRenderer renderer = enemy.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = Color.red;
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.red * 2f);
                    renderer.material = mat;
                    enemy.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                }
                
                enemies.Add(enemy);
                Debug.Log($"Ennemi créé à la position du cube: {spawnPosition}");
                return enemy;
            }
            
            // Si tout échoue, créer l'ennemi directement à la position du cube sans utiliser NavMesh
            Debug.LogWarning("Création de l'ennemi directement à la position du cube sans NavMesh");
            GameObject emergencyEnemy = Instantiate(enemyPrefab, cubePosition + Vector3.up, Quaternion.identity);
            enemies.Add(emergencyEnemy);
            return emergencyEnemy;
        }
        
        return null;
    }

    private GameObject SpawnEnemy()
    {
        if (defenseCubes.Count == 0)
        {
            Debug.LogWarning("Aucun cube de défense trouvé pour le calcul de la position de l'ennemi");
            return null;
        }

        // Trouver une position valide sur le NavMesh
        Vector3 validPosition = Vector3.zero;
        bool positionFound = false;
        
        // Essayer d'abord près d'un cube au hasard
        int maxAttempts = 20;  // Augmenter le nombre de tentatives
        for (int i = 0; i < maxAttempts; i++)
        {
            int randomCubeIndex = Random.Range(0, defenseCubes.Count);
            if (defenseCubes[randomCubeIndex] == null) continue;
            
            Vector3 cubePosition = defenseCubes[randomCubeIndex].transform.position;
            
            // Essayer différentes distances
            float[] testDistances = new float[] { 1f, 1.5f, 2f, 3f, 0.5f };
            foreach (float spawnRadius in testDistances)
            {
                // Position aléatoire autour du cube
                float angle = Random.Range(0f, 360f);
                float radius = Random.Range(0.5f, spawnRadius);
                Vector3 randomOffset = new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * radius, 
                    0.5f, 
                    Mathf.Sin(angle * Mathf.Deg2Rad) * radius
                );
                Vector3 potentialPosition = cubePosition + randomOffset;
                
                NavMeshHit hit;
                if (NavMesh.SamplePosition(potentialPosition, out hit, 2f, NavMesh.AllAreas))
                {
                    validPosition = hit.position;
                    positionFound = true;
                    break;
                }
            }
            
            if (positionFound) break;
        }
        
        // Si aucune position n'a été trouvée, utiliser directement la position d'un cube
        if (!positionFound && defenseCubes.Count > 0)
        {
            foreach (GameObject cube in defenseCubes)
            {
                if (cube == null) continue;
                
                // Essayer plusieurs hauteurs
                for (float yOffset = 0.3f; yOffset <= 1.5f; yOffset += 0.3f)
                {
                    Vector3 testPos = cube.transform.position + new Vector3(0, yOffset, 0);
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(testPos, out hit, 2f, NavMesh.AllAreas))
                    {
                        validPosition = hit.position;
                        positionFound = true;
                        break;
                    }
                }
                
                if (positionFound) break;
            }
        }
        
        if (!positionFound)
        {
            Debug.LogError("Impossible de trouver une position valide sur le NavMesh pour l'ennemi");
            
            // Dernière tentative: placer directement sur un cube
            if (defenseCubes.Count > 0 && defenseCubes[0] != null)
            {
                validPosition = defenseCubes[0].transform.position + Vector3.up;
                positionFound = true;
                Debug.LogWarning("Position de secours utilisée: directement au-dessus du premier cube");
            }
            else
            {
                return null;
            }
        }
        
        // Créer l'ennemi à la position validée
        GameObject enemy = Instantiate(enemyPrefab, validPosition, Quaternion.identity);
        
        // Rendre l'ennemi plus visible avec un matériau brillant
        MeshRenderer renderer = enemy.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = Color.red;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.red * 2f);
            renderer.material = mat;
            
            // Assurer une taille visible
            enemy.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        }
        
        enemies.Add(enemy);
        Debug.Log($"Ennemi créé à la position: {validPosition}");
        return enemy;
    }

    private Vector3 GetRandomSpawnPosition()
    {
        // Get a random position outside the play area
        float radius = 10f; // Adjust this value based on your play area
        float angle = Random.Range(0f, 360f);
        Vector3 position = new Vector3(
            Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
            0f,
            Mathf.Sin(angle * Mathf.Deg2Rad) * radius
        );
        return position;
    }

    private void UpdateUI()
    {
        if (waveText != null)
        {
            waveText.text = $"Wave: {currentWave}";
        }
        if (cubeCountText != null)
        {
            cubeCountText.text = $"Cubes: {placedCubes}/{maxCubes}";
        }
    }

    public List<GameObject> GetDefenseCubes()
    {
        return defenseCubes;
    }
    
    // Méthode pour nettoyer les ennemis morts de la liste
    public void RemoveEnemy(GameObject enemy)
    {
        if (enemies.Contains(enemy))
        {
            enemies.Remove(enemy);
        }
    }
} 