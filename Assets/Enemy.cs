using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour
{
    [Header("Enemy Stats")]
    public float maxHealth = 100f;
    public float moveSpeed = 3f;
    
    [Header("Movement")]
    public float targetUpdateRate = 1f;
    public float minDistanceToTarget = 0.5f;
    public bool showDebugPath = true;

    private float currentHealth;
    private NavMeshAgent agent;
    private GameObject currentTarget;
    private float nextTargetUpdateTime;
    private LineRenderer pathVisualizer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("Pas de NavMeshAgent sur l'ennemi !");
            enabled = false;
            return;
        }
        
        // Make sure we have a collider for projectile hits
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.Log("Adding a collider to the enemy for projectile detection");
            SphereCollider sphereCol = gameObject.AddComponent<SphereCollider>();
            sphereCol.radius = 0.5f;
            sphereCol.isTrigger = true;
        }
        
        // Configurer le LineRenderer pour visualiser le chemin (optionnel)
        if (showDebugPath)
        {
            pathVisualizer = gameObject.AddComponent<LineRenderer>();
            pathVisualizer.startWidth = 0.1f;
            pathVisualizer.endWidth = 0.1f;
            pathVisualizer.material = new Material(Shader.Find("Sprites/Default"));
            pathVisualizer.startColor = Color.red;
            pathVisualizer.endColor = Color.red;
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;
        
        // Configurer l'agent
        agent.speed = moveSpeed;
        agent.stoppingDistance = minDistanceToTarget;
        agent.updateRotation = true;
        
        // Important pour les environnements AR !
        agent.updatePosition = true;
        agent.autoTraverseOffMeshLink = true;
        
        // Forcer la première mise à jour de la cible
        ForceUpdateTarget();
    }

    private void Update()
    {
        if (agent == null || !agent.isOnNavMesh)
        {
            Debug.LogWarning("L'ennemi n'est pas sur le NavMesh !");
            return;
        }
        
        if (Time.time >= nextTargetUpdateTime)
        {
            UpdateTarget();
            nextTargetUpdateTime = Time.time + targetUpdateRate;
        }

        if (currentTarget != null && agent.isOnNavMesh)
        {
            agent.SetDestination(currentTarget.transform.position);
            
            // Visualiser le chemin (optionnel)
            if (showDebugPath && pathVisualizer != null)
            {
                DrawPath();
            }
        }
        
        // Vérifier si l'agent est bloqué
        if (agent.isOnNavMesh && agent.velocity.magnitude < 0.1f && !agent.isStopped && currentTarget != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
            if (distanceToTarget > agent.stoppingDistance + 0.5f)
            {
                Debug.Log($"L'ennemi est bloqué, tentative de recalcul du chemin...");
                agent.ResetPath();
                agent.SetDestination(currentTarget.transform.position);
            }
        }
    }
    
    private void DrawPath()
    {
        if (agent.hasPath)
        {
            pathVisualizer.positionCount = agent.path.corners.Length;
            pathVisualizer.SetPositions(agent.path.corners);
        }
        else
        {
            pathVisualizer.positionCount = 0;
        }
    }

    public void ForceUpdateTarget()
    {
        UpdateTarget();
        nextTargetUpdateTime = Time.time + targetUpdateRate;
    }

    private void UpdateTarget()
    {
        var defenseCubes = GameManager.Instance.GetDefenseCubes();
        if (defenseCubes.Count == 0) return;

        // Find the nearest cube
        float nearestDistance = float.MaxValue;
        GameObject nearestCube = null;

        foreach (var cube in defenseCubes)
        {
            if (cube == null) continue;
            
            float distance = Vector3.Distance(transform.position, cube.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestCube = cube;
            }
        }

        if (nearestCube != null && nearestCube != currentTarget)
        {
            currentTarget = nearestCube;
            if (agent.isOnNavMesh)
            {
                agent.SetDestination(currentTarget.transform.position);
                Debug.Log($"Nouvelle cible définie pour l'ennemi: {currentTarget.name}");
            }
        }
    }

    public void TakeDamage(float damage)
    {
        Debug.Log($"Enemy TakeDamage called! Damage: {damage}");
        currentHealth -= damage;
        Debug.Log($"Ennemi touché ! Santé restante: {currentHealth}/{maxHealth}");
        
        // Visual feedback - flash the enemy red
        StartCoroutine(FlashRed());
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private System.Collections.IEnumerator FlashRed()
    {
        // Get the renderer
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            // Store the original color
            Color originalColor = renderer.material.color;
            
            // Set to red
            renderer.material.color = Color.red;
            
            // Wait a short time
            yield return new WaitForSeconds(0.1f);
            
            // Return to original color
            renderer.material.color = originalColor;
        }
    }

    private void Die()
    {
        Debug.Log("Ennemi détruit !");
        
        // Nettoyer la liste des ennemis dans le GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RemoveEnemy(gameObject);
        }
        
        // Ajouter des effets visuels, du son ou un score ici
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Empêcher les collisions avec les cubes de défense
        if (collision.gameObject.GetComponent<DefenseCube>() != null)
        {
            Physics.IgnoreCollision(collision.collider, GetComponent<Collider>());
        }
    }
} 