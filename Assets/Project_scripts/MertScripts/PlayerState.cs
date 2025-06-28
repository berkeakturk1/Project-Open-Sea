using System.Collections;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class PlayerState : MonoBehaviour
{
    public static PlayerState Instance { get; set; }

    //--- player health ---//
    public float currentHealth;
    public float maxHealth;

    //--- Breath Bar ---//
    public float currentBreath;
    public float maxBreath;

    //--- Armor ---//
    public float currentArmor;
    public float maxArmor;

    public GameObject playerBody;

    [Header("Respawn")]
    public GameObject controllerObject; // RigidbodyFPSController atanacak
    private Vector3 respawnPosition = new Vector3(892.622314f, 49.1568565f, -2616.02417f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        currentHealth = maxHealth;
        currentBreath = maxBreath;

        maxArmor = 100f;
        currentArmor = maxArmor;
    }

    IEnumerator decreaseBreath()
    {
        while (true)
        {
            currentBreath -= 1;
            yield return new WaitForSeconds(2);
        }
    }

    public void takeDamage(float damage)
    {
        if (currentArmor > 0)
        {
            if (currentArmor >= damage)
            {
                currentArmor -= damage;
            }
            else
            {
                float remainingDamage = damage - currentArmor;
                currentArmor = 0;
                currentHealth -= remainingDamage;
            }
        }
        else
        {
            currentHealth -= damage;
        }
    }
    
    
    public void takeDamageHealth(float damage)
    {
        currentHealth -= damage;
    }
    public void AddArmor(float amount)
    {
        currentArmor += amount;

        if (currentArmor > maxArmor)
            currentArmor = maxArmor;

        Debug.Log("Armor added: +" + amount + " → Current Armor: " + currentArmor + "/" + maxArmor);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
    }

    public void setCurrentBreath(float val)
    {
        currentBreath = val;
    }

    public void Heal(int val)
    {
        currentHealth = Mathf.Min(currentHealth + val, maxHealth);
    }

    void Update()
    {
        if (currentHealth <= 0)
        {
            DieAndRespawn();
            
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            takeDamage(10f);
        }
    }

    private void DieAndRespawn()
    {
        Debug.Log("Player died. Respawning...");

        FadeController fade = FindObjectOfType<FadeController>();

        System.Action respawnAction = () =>
        {
            currentHealth = maxHealth = 100f;
            currentBreath = maxBreath = 100f;
            currentArmor = maxArmor = 100f;

            Vector3 respawnPosition = new Vector3(892.622314f, 49.1568565f, -2616.02417f);

            if (controllerObject != null)
            {
                Rigidbody rb = controllerObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                }

                controllerObject.transform.position = respawnPosition;
                Debug.Log("Teleported to: " + respawnPosition);
            }
        };

        if (fade != null)
        {
            fade.FadeOutAndIn(respawnAction);
        }
        else
        {
            Debug.LogWarning("FadeController not found, respawning without fade.");
            respawnAction.Invoke();
        }
    }
}