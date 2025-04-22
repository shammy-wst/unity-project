using UnityEngine;

public class ProjectileBehavior : MonoBehaviour
{
    public float damage = 10f;
    public float speed = 10f;
    public float lifetime = 2f;
    public bool destroyOnHit = true;
    public GameObject hitEffect;
    
    private Rigidbody rb;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
        }
    }
    
    public void Initialize(Vector3 direction, float projectileSpeed, float projectileLifetime)
    {
        speed = projectileSpeed;
        lifetime = projectileLifetime;
        
        // Don't set isKinematic when we want to use velocity
        rb.useGravity = false;
        rb.linearVelocity = direction * speed;
        
        // Log the initialization
        Debug.Log($"Projectile initialized with speed: {speed}, direction: {direction}, lifetime: {lifetime}");
        
        // Détruire le projectile après sa durée de vie
        Destroy(gameObject, lifetime);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Projectile trigger hit: {other.gameObject.name}");
        
        // Vérifier si c'est un ennemi
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            // Appliquer les dégâts
            enemy.TakeDamage(damage);
            
            // Effets visuels de l'impact
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }
            
            // Détruire le projectile s'il est configuré pour cela
            if (destroyOnHit)
            {
                Destroy(gameObject);
            }
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Projectile collision with: {collision.gameObject.name}");
        
        // Vérifier si c'est un ennemi
        Enemy enemy = collision.gameObject.GetComponent<Enemy>();
        if (enemy != null)
        {
            // Appliquer les dégâts
            enemy.TakeDamage(damage);
            
            // Effets visuels de l'impact
            if (hitEffect != null)
            {
                Instantiate(hitEffect, collision.contacts[0].point, Quaternion.identity);
            }
            
            // Détruire le projectile s'il est configuré pour cela
            if (destroyOnHit)
            {
                Destroy(gameObject);
            }
        }
    }
} 