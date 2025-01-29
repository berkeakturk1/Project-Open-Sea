using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class InventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler{
    [Header("UI")]
    public Image image;//normalde Image image olarak oluşturdum ancak hata verdi.Buna çevirdim 
   // public TMPro.TextMeshProUGUI countText;//Oyuncuya aynı itemdan kaç tane var onu göstermek için bir text
   public Text countText;//Oyuncuya aynı itemdan kaç tane var onu göstermek için bir text
    [HideInInspector] public Item item;
    [HideInInspector] public int count = 1;//Her item için default değer
    [HideInInspector]public Transform parentAfterDrag;
     
     /*void Start(){
        InitialiseItem(item);
    }*/
    public void InitialiseItem(Item newItem){
        item = newItem;
        image.sprite = newItem.image;
       RefreshCount();
    }
    public void RefreshCount(){//Stacklenebilir itemlar için fonksiyon
        countText.text = count.ToString();
    }
    public void OnBeginDrag(PointerEventData eventData){
        Debug.Log("Begin");
        image.raycastTarget = false;
        parentAfterDrag = transform.parent;
        transform.SetParent(transform.root);
        transform.SetAsLastSibling();
    }
    public void OnDrag(PointerEventData eventData){
        transform.position = Input.mousePosition;
        Debug.Log("Dragging");
    }
    public void OnEndDrag(PointerEventData eventData){
         image.raycastTarget = true;
         Debug.Log("End Drag");
         transform.SetParent(parentAfterDrag);   
    }
}
