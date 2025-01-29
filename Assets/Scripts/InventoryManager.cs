using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public InventorySlot[] inventorySlots; //boş olan slotlar için bir array
    public GameObject inventoryItemPrefab;

    public bool AddItem(Item item){//boş slot fonksiyonu.Boş yuva varsa true, yoksa false dönecek şekilde ayarlandı
        for(int i = 0 ; i<inventorySlots.Length ; i++){
            InventorySlot slot = inventorySlots[i];
            InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();
            if(itemInSlot == null){
                SpawnNewItem(item);//slot koymayı unutma
                return true;
            }
        }
        return false;
    }

    //Yt kodu 
    /* void SpawnNewItem(Item item , InventorySlot slot){//bununla ekledikten sonra item hangi alan boş ona bakacak.Boş olan slota girecek
    
        GameObject newItemGo = Instantiate(inventoryItemPrefab, slot.transform);
        InventoryItem inventoryItem = newItemGo.GetComponent<InventoryItem>();
        inventoryItem.InitialiseItem(item);

    }*/


   public void SpawnNewItem(Item item){
    // InventorySlots dizisindeki her bir slotu kontrol et
    foreach (InventorySlot slot in inventorySlots)
    {
        // Eğer slot dolu değilse (slot içinde bir InventoryItem yoksa)
        if (slot.transform.childCount == 0)
        {
            // Yeni item prefab'ını bu slota oluştur
            GameObject newItemGo = Instantiate(inventoryItemPrefab, slot.transform);

            // Item prefab'ının RectTransform ayarlarını sıfırla
            RectTransform rectTransform = newItemGo.GetComponent<RectTransform>();
            rectTransform.localPosition = Vector3.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;

            // InventoryItem bileşenini al ve item verilerini ata
            InventoryItem inventoryItem = newItemGo.GetComponent<InventoryItem>();
            if (inventoryItem != null)
            {
                inventoryItem.InitialiseItem(item);
            }
           // Debug.Log($"Yeni item {item.name}, slot {slot.name} içine yerleştirildi.");
            return; // İşlem tamamlandıktan sonra metottan çık
        }
    }

    // Eğer tüm slotlar doluysa uyarı ver
    //Debug.LogWarning("Tüm slotlar dolu, yeni item eklenemedi!");
}

}
