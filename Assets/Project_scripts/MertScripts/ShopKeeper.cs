using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopKeeper : MonoBehaviour
{
    public static ShopKeeper Instance;

    [Header("Player Interaction")]
    public bool playerInRange;
    public bool isTalkingWithPlayer;

    [Header("UI References")]
    public GameObject shopkeeperDialogUI;
    public Button buyBTN;
    public Button sellBTN;
    public Button exitBTN;

    public GameObject buyPanelUI;
    public GameObject sellPanelUI;

    public TextMeshProUGUI interaction_Info_UI;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    private void Start()
    {
        if (shopkeeperDialogUI != null)
            shopkeeperDialogUI.SetActive(false);

        if (buyBTN != null)
            buyBTN.onClick.AddListener(BuyMode);
        if (sellBTN != null)
            sellBTN.onClick.AddListener(SellMode);
        if (exitBTN != null)
            exitBTN.onClick.AddListener(StopTalking);

        if (interaction_Info_UI != null)
            interaction_Info_UI.gameObject.SetActive(false);

        buyPanelUI?.SetActive(false);
        sellPanelUI?.SetActive(false);
    }

    private void Update()
    {
        if (playerInRange && !isTalkingWithPlayer && Input.GetKeyDown(KeyCode.F))
        {
            Talk();
        }
    }

    private void BuyMode()
    {
        sellPanelUI?.SetActive(false);
        buyPanelUI?.SetActive(true);
        HideDialogUI();
    }

    private void SellMode()
    {
        sellPanelUI?.SetActive(true);
        buyPanelUI?.SetActive(false);
        HideDialogUI();
    }

    public void DialogMode()
    {
        DisplayDialogUI();
        sellPanelUI?.SetActive(false);
        buyPanelUI?.SetActive(false);
    }

    public void Talk()
    {
        isTalkingWithPlayer = true;
        DisplayDialogUI();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void StopTalking()
    {
        isTalkingWithPlayer = false;
        HideDialogUI();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void DisplayDialogUI()
    {
        shopkeeperDialogUI?.SetActive(true);
    }

    private void HideDialogUI()
    {
        shopkeeperDialogUI?.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;

            if (interaction_Info_UI != null)
            {
                interaction_Info_UI.text = "Press [F] to Talk";
                interaction_Info_UI.gameObject.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            if (isTalkingWithPlayer)
            {
                StopTalking();
            }

            if (interaction_Info_UI != null)
            {
                interaction_Info_UI.gameObject.SetActive(false);
            }
        }
    }
}
