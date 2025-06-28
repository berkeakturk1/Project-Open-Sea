using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ArmorBar : MonoBehaviour
{
    public Slider slider;
    public TMP_Text armorCounter;

    public GameObject playerState; // tercihen Inspector'dan atanır
    private PlayerState ps;

    void Awake()
    {
        slider = GetComponent<Slider>();
        SetSliderColorToYellow();

        if (playerState == null)
        {
            playerState = GameObject.FindWithTag("Player");

            if (playerState == null)
            {
                Debug.LogError("ArmorBar: Sahnedeki 'Player' tag'li nesne bulunamadı!");
                return;
            }
        }

        ps = playerState.GetComponent<PlayerState>();

        if (ps == null)
        {
            Debug.LogError("ArmorBar: PlayerState component'i bulunamadı!");
        }
    }

    void Start()
    {
        // UI'nın ilk karede doğru görünmesini garantilemek için
        UpdateArmorUI();
    }

    void Update()
    {
        UpdateArmorUI();
    }

    void UpdateArmorUI()
    {
        if (ps == null) return;

        float currentArmor = ps.currentArmor;
        float maxArmor = ps.maxArmor;

        float fillValue = maxArmor > 0 ? currentArmor / maxArmor : 0;
        slider.value = fillValue;

        armorCounter.text = Mathf.RoundToInt(currentArmor) + " / " + Mathf.RoundToInt(maxArmor);
    }

    void SetSliderColorToYellow()
    {
        if (slider.fillRect != null)
        {
            Image fillImage = slider.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = Color.yellow;
            }
        }
    }
}