using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    // Singleton instance
    private static HealthSystem instance;

    // Current health
    private int currentHealth;

    // Maximum health
    public int maxHealth = 100;

    // Event triggered when health changes
    public delegate void HealthChangedDelegate(int currentHealth, int maxHealth);
    public event HealthChangedDelegate OnHealthChanged;

    // Property to get current health
    public int CurrentHealth
    {
        get { return currentHealth; }
    }

    // Property to get maximum health
    public int MaxHealth
    {
        get { return maxHealth; }
    }

    // Singleton instance property
    public static HealthSystem Instance
    {
        get
        {
            if (instance == null)
            {
                // If no instance exists, create one
                GameObject singletonObject = new GameObject("HealthSystem");
                instance = singletonObject.AddComponent<HealthSystem>();
            }

            return instance;
        }
    }

    // Initialize the health system
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            // If an instance already exists, destroy this one
            Destroy(this.gameObject);
        }
        else
        {
            // Set the instance to this object
            instance = this;
            DontDestroyOnLoad(this.gameObject);

            // Initialize health
            currentHealth = maxHealth;
        }
    }

    // Apply damage to the health system
    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Trigger the OnHealthChanged event
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Check for death
        if (currentHealth == 0)
        {
            Die();
        }
    }

    // Apply healing to the health system
    public void Heal(int healAmount)
    {
        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Trigger the OnHealthChanged event
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // Handle death
    private void Die()
    {
        Debug.Log("Player has died!");
        // Implement death logic here
    }
}
