using UnityEngine;
using System.Collections;

public class SimpleWeapon : MonoBehaviour
{
    [Header("Silah Ayarları")]
    public float damage = 25f;
    public float range = 100f;
    public float fireRate = 0.5f; // Saniye cinsinden ateş aralığı
    public int maxAmmo = 30;
    public float reloadTime = 2f;
    
    [Header("Efektler")]
    public ParticleSystem muzzleFlash; // Namlu alevi
    public AudioSource audioSource;
    public AudioClip fireSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
    
    [Header("Raycast Ayarları")]
    public Transform firePoint; // Namlu alevi ve efektler için nokta
    public Transform aimPoint; // Raycast için nokta (kameraya bağlanacak)
    public LayerMask enemyLayerMask = -1; // Hangi layer'lara zarar vereceği
    
    [Header("Hit Effects")]
    public GameObject hitEffectPrefab; // Hit effect for when bullets hit enemies
    public float hitEffectDuration = 2f;
    
    [Header("Recoil Settings")]
    public float recoilAmount = 15f; // Degrees of upward rotation
    public float recoilSpeed = 8f; // How fast the recoil happens
    public float returnSpeed = 4f; // How fast it returns to normal
    public float sideRecoilAmount = 2f; // Random left/right recoil
    public AnimationCurve recoilCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // Recoil animation curve
    
    [Header("Reload Animation Settings")]
    public float reloadRotationAmount = 30f; // Degrees to rotate during reload
    public float reloadPositionOffset = 0.2f; // How far to move the gun down/back
    public float reloadAnimationSpeed = 3f; // Speed of reload animation
    public AnimationCurve reloadCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // Reload animation curve
    
    private int currentAmmo;
    private float nextFireTime = 0f;
    private bool isReloading = false;
    
    // Recoil variables
    private Vector3 originalRotation;
    private Vector3 originalPosition;
    private Vector3 currentRecoil;
    private Vector3 targetRecoil;
    private bool isRecoiling = false;
    
    // Reload animation variables
    private bool isReloadAnimating = false;
    private float reloadAnimationTime = 0f;
    private Vector3 reloadStartRotation;
    private Vector3 reloadStartPosition;
    
    void Start()
    {
        currentAmmo = maxAmmo;
        
        // If no fire point is assigned, use this transform
        if (firePoint == null)
        {
            firePoint = transform;
        }
        
        // Auto-assign aimPoint to main camera if not set
        if (aimPoint == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                aimPoint = mainCam.transform;
                Debug.Log("Auto-assigned aimPoint to Main Camera");
            }
            else
            {
                // Fallback to firePoint if no camera found
                aimPoint = firePoint;
                Debug.LogWarning("No Main Camera found! Using firePoint as aimPoint fallback.");
            }
        }
        
        // Store the original rotation and position for animations
        originalRotation = transform.localEulerAngles;
        originalPosition = transform.localPosition;
        currentRecoil = Vector3.zero;
        targetRecoil = Vector3.zero;
    }
    
    void Update()
    {
        // Handle reload animation
        HandleReloadAnimation();
        
        // Handle recoil animation (only if not reloading)
        if (!isReloadAnimating)
        {
            HandleRecoil();
        }
        
        // Ateş etme (Sol mouse tuşu) - disabled during reload animation
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime && !isReloading && !isReloadAnimating)
        {
            if (currentAmmo > 0)
            {
                Fire();
                nextFireTime = Time.time + fireRate;
            }
            else
            {
                // Mermi bittiğinde ses çal
                PlayEmptySound();
            }
        }
        
        // Şarjör değiştirme (R tuşu) - disabled during reload animation
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < maxAmmo && !isReloading && !isReloadAnimating)
        {
            StartCoroutine(Reload());
        }
    }
    
    void Fire()
    {
        currentAmmo--;
        
        // Trigger recoil
        TriggerRecoil();
        
        // Ses efekti
        PlayFireSound();
        
        // Visual efekt
        if (muzzleFlash != null)
        {
            Debug.Log("Muzzle flash triggered");
            muzzleFlash.Play();
        }
        
        // Raycast ile hedef tespit (aimPoint kullanarak)
        RaycastHit hit;
        Vector3 aimDirection = aimPoint.forward;
        Vector3 aimOrigin = aimPoint.position;
        
        if (Physics.Raycast(aimOrigin, aimDirection, out hit, range, enemyLayerMask))
        {
            Debug.Log($"Hit: {hit.collider.name}");
            
            // Check for Enemy script (your main enemy system)
            Enemy enemy = hit.collider.GetComponent<Enemy>();
            if (enemy != null && !enemy.IsDead())
            {
                // Damage the enemy using your Enemy script
                enemy.TakeDamage((int)damage);
                Debug.Log($"Enemy {hit.collider.name} took {damage} damage from pistol");
                
                // Spawn hit effect at impact point
                SpawnHitEffect(hit.point, hit.normal);
            }
            else
            {
                // Check if hit object has Enemy script on parent (for complex enemy hierarchies)
                Enemy parentEnemy = hit.collider.GetComponentInParent<Enemy>();
                if (parentEnemy != null && !parentEnemy.IsDead())
                {
                    parentEnemy.TakeDamage((int)damage);
                    Debug.Log($"Parent enemy {parentEnemy.name} took {damage} damage from pistol");
                    
                    // Spawn hit effect at impact point
                    SpawnHitEffect(hit.point, hit.normal);
                }
                else
                {
                    Debug.Log($"Hit {hit.collider.name} but it's not a damageable enemy");
                }
            }
            
            // Debug için çizgi çiz (kırmızı = hit) - aimPoint'ten başlayarak
            Debug.DrawRay(aimOrigin, aimDirection * hit.distance, Color.red, 0.5f);
        }
        else
        {
            // Hedefe ulaşmadıysa maksimum menzil kadar çizgi çiz (beyaz = miss)
            Debug.DrawRay(aimOrigin, aimDirection * range, Color.white, 0.1f);
            Debug.Log("Shot missed - no target in range");
        }
    }
    
    void SpawnHitEffect(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (hitEffectPrefab != null)
        {
            // Spawn effect at hit point, oriented along the hit surface normal
            GameObject effect = Instantiate(hitEffectPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
            
            // Clean up effect after specified duration
            Destroy(effect, hitEffectDuration);
        }
    }
    
    void TriggerRecoil()
    {
        // Calculate random recoil
        float randomSideRecoil = Random.Range(-sideRecoilAmount, sideRecoilAmount);
        
        // Set target recoil (upward + slight random side movement)
        targetRecoil = new Vector3(-recoilAmount, randomSideRecoil, 0f);
        
        isRecoiling = true;
    }
    
    void HandleRecoil()
    {
        if (isRecoiling)
        {
            // Move towards target recoil position
            currentRecoil = Vector3.Slerp(currentRecoil, targetRecoil, recoilSpeed * Time.deltaTime);
            
            // Check if we've reached the target recoil
            if (Vector3.Distance(currentRecoil, targetRecoil) < 0.1f)
            {
                currentRecoil = targetRecoil;
                isRecoiling = false;
                targetRecoil = Vector3.zero; // Start returning to original position
            }
        }
        else
        {
            // Return to original position
            currentRecoil = Vector3.Slerp(currentRecoil, Vector3.zero, returnSpeed * Time.deltaTime);
        }
        
        // Apply the recoil rotation (only if not reload animating)
        if (!isReloadAnimating)
        {
            Vector3 finalRotation = originalRotation + currentRecoil;
            transform.localEulerAngles = finalRotation;
        }
    }
    
    void HandleReloadAnimation()
    {
        if (isReloadAnimating)
        {
            // Increment animation time
            reloadAnimationTime += Time.deltaTime * reloadAnimationSpeed;
            
            // Calculate animation progress (0 to 1 and back)
            float progress;
            if (reloadAnimationTime <= 1f)
            {
                // First half: gun moves to reload position
                progress = reloadCurve.Evaluate(reloadAnimationTime);
            }
            else if (reloadAnimationTime <= 2f)
            {
                // Second half: gun returns to original position
                progress = reloadCurve.Evaluate(2f - reloadAnimationTime);
            }
            else
            {
                // Animation complete
                isReloadAnimating = false;
                reloadAnimationTime = 0f;
                transform.localEulerAngles = originalRotation;
                transform.localPosition = originalPosition;
                return;
            }
            
            // Calculate reload rotation (tilts gun for magazine change)
            Vector3 reloadRotation = new Vector3(
                originalRotation.x + (reloadRotationAmount * progress),
                originalRotation.y + (reloadRotationAmount * 0.5f * progress), // Slight Y rotation
                originalRotation.z - (reloadRotationAmount * 0.3f * progress)  // Slight Z rotation
            );
            
            // Calculate reload position (moves gun down and back)
            Vector3 reloadPosition = new Vector3(
                originalPosition.x + (reloadPositionOffset * 0.3f * progress), // Slight right movement
                originalPosition.y - (reloadPositionOffset * progress),        // Down movement
                originalPosition.z - (reloadPositionOffset * 0.5f * progress)  // Back movement
            );
            
            // Apply the reload animation
            transform.localEulerAngles = reloadRotation;
            transform.localPosition = reloadPosition;
        }
    }
    
    System.Collections.IEnumerator Reload()
    {
        isReloading = true;
        
        // Start reload animation
        StartReloadAnimation();
        
        // Şarjör değiştirme sesi
        PlayReloadSound();
        
        Debug.Log("Reloading...");
        
        yield return new WaitForSeconds(reloadTime);
        
        currentAmmo = maxAmmo;
        isReloading = false;
        
        Debug.Log("Reload complete!");
    }
    
    void StartReloadAnimation()
    {
        isReloadAnimating = true;
        reloadAnimationTime = 0f;
        reloadStartRotation = transform.localEulerAngles;
        reloadStartPosition = transform.localPosition;
    }
    
    void PlayFireSound()
    {
        if (audioSource != null && fireSound != null)
        {
            audioSource.PlayOneShot(fireSound);
        }
    }
    
    void PlayReloadSound()
    {
        if (audioSource != null && reloadSound != null)
        {
            audioSource.PlayOneShot(reloadSound);
        }
    }
    
    void PlayEmptySound()
    {
        if (audioSource != null && emptySound != null && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(emptySound);
        }
    }
    
    // UI için getter metodları
    public int GetCurrentAmmo()
    {
        return currentAmmo;
    }
    
    public int GetMaxAmmo()
    {
        return maxAmmo;
    }
    
    public bool IsReloading()
    {
        return isReloading;
    }
    
    // Debug method to test weapon without enemies
    public void TestFire()
    {
        if (currentAmmo > 0)
        {
            Fire();
        }
    }
}