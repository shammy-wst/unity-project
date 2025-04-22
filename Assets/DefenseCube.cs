using UnityEngine;
using System.Collections.Generic;

public class DefenseCube : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackRadius = 5f;
    public float attackDamage = 10f;
    public float attackRate = 1f;
    public LayerMask enemyLayer;

    [Header("Projectile Settings")]
    public bool useProjectiles = true;
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;
    public float projectileLifetime = 2f;
    public Color projectileColor = Color.cyan;

    [Header("Visual Feedback")]
    public bool showAttackRadius = true;
    public Color radiusColor = new Color(1f, 0f, 0f, 0.2f);
    
    private float nextAttackTime;
    private List<GameObject> enemiesInRange = new List<GameObject>();
    private Transform targetEnemy;

    private void Start()
    {
        // Configure enemy layer if not set
        if (enemyLayer == 0)
        {
            enemyLayer = LayerMask.GetMask("Default");
            Debug.LogWarning("Enemy layer not set on defense cube. Using Default layer.");
        }
        
        // Créer un projectile par défaut si non assigné
        if (useProjectiles && projectilePrefab == null)
        {
            projectilePrefab = CreateDefaultProjectile();
        }
    }

    private GameObject CreateDefaultProjectile()
    {
        // Créer un GameObject pour le projectile
        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = "DefaultProjectile";
        projectile.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        
        // Ajouter le Rigidbody et configurer
        Rigidbody rb = projectile.AddComponent<Rigidbody>();
        rb.useGravity = false;
        // Don't set isKinematic - we want physics to work
        
        // Configurer le collider
        SphereCollider collider = projectile.GetComponent<SphereCollider>();
        collider.isTrigger = true;
        
        // Ajouter un script de projectile
        ProjectileBehavior projectileBehavior = projectile.AddComponent<ProjectileBehavior>();
        projectileBehavior.damage = attackDamage;
        
        // Configurer le matériau
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = projectileColor;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", projectileColor * 0.5f);
        projectile.GetComponent<MeshRenderer>().material = mat;
        
        // Désactiver le projectile modèle
        projectile.SetActive(false);
        
        return projectile;
    }

    private void Update()
    {
        FindEnemiesInRange();
        
        if (Time.time >= nextAttackTime && enemiesInRange.Count > 0)
        {
            Attack();
            nextAttackTime = Time.time + attackRate;
        }
    }

    private void FindEnemiesInRange()
    {
        enemiesInRange.Clear();
        targetEnemy = null;
        
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRadius, enemyLayer);
        
        float closestDistance = float.MaxValue;
        
        foreach (var hitCollider in hitColliders)
        {
            Enemy enemy = hitCollider.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemiesInRange.Add(hitCollider.gameObject);
                
                // Trouver l'ennemi le plus proche pour le ciblage
                float distanceToEnemy = Vector3.Distance(transform.position, hitCollider.transform.position);
                if (distanceToEnemy < closestDistance)
                {
                    closestDistance = distanceToEnemy;
                    targetEnemy = hitCollider.transform;
                }
            }
        }
        
        // Log if we found enemies for debugging
        if (enemiesInRange.Count > 0)
        {
            Debug.Log($"Found {enemiesInRange.Count} enemies in range of defense cube");
        }
    }

    private void Attack()
    {
        if (targetEnemy == null) return;
        
        Debug.Log($"Attacking enemy: {targetEnemy.name}");
        
        if (useProjectiles && projectilePrefab != null)
        {
            FireProjectile();
        }
        else
        {
            // Appliquer directement les dégâts à tous les ennemis à portée
            foreach (var enemyObject in enemiesInRange)
            {
                Enemy enemy = enemyObject.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(attackDamage);
                }
            }
        }
    }
    
    private void FireProjectile()
    {
        if (targetEnemy == null) return;
        
        // Créer un projectile à partir du préfab
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        projectile.SetActive(true);
        
        // Configurer la direction vers la cible
        Vector3 direction = (targetEnemy.position - transform.position).normalized;
        
        Debug.Log($"Firing projectile at {targetEnemy.name}, direction: {direction}");
        
        // Initialiser le projectile
        ProjectileBehavior behavior = projectile.GetComponent<ProjectileBehavior>();
        if (behavior != null)
        {
            behavior.damage = attackDamage;
            behavior.Initialize(direction, projectileSpeed, projectileLifetime);
        }
        else
        {
            // Si pas de script personnalisé, ajouter une simple vitesse
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false;
                rb.linearVelocity = direction * projectileSpeed;
                
                // Log for debugging
                Debug.Log($"No ProjectileBehavior script found, using Rigidbody directly with velocity: {rb.linearVelocity}");
            }
            
            // Détruire après la durée de vie
            Destroy(projectile, projectileLifetime);
        }
    }

    private void OnDrawGizmos()
    {
        if (showAttackRadius)
        {
            Gizmos.color = radiusColor;
            Gizmos.DrawWireSphere(transform.position, attackRadius);
        }
    }
} 