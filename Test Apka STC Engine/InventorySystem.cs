using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace STCEngine
{
    public class Inventory : Component
    {
        [JsonIgnore] public readonly int slotSpacing = 30; [JsonIgnore] public readonly int slotSize = 64; [JsonIgnore] public readonly Image inventorySlotBackgroundImage = Image.FromFile("Assets/Inventory-Background.png");
        public override string Type { get; } = nameof(Inventory);
        [JsonIgnore] public int emptySlots { get => 30 - items.Count; }
        public List<ItemInInventory> items { get; set; } = new List<ItemInInventory>();
        //[JsonIgnore] public List<InventorySlot> inventorySlots = new List<InventorySlot>();

        public Inventory() { }
        /// <summary>
        /// Attempts to add the item to the inventory
        /// </summary>
        /// <param name="item"></param>
        /// <returns>Whether the action was succesful</returns>
        public bool AddItem(ItemInInventory item)
        {
            if(emptySlots == 0) { return false; }
            
            items.Add(item);
            return true;
        }
        /// <summary>
        /// Attempts to add an array of items to the inventory
        /// </summary>
        /// <param name="item"></param>
        /// <returns>Whether the action was succesful</returns>
        public bool AddItem(ItemInInventory[] items)
        {
            if (emptySlots - items.Length <= 0) { return false; }

            this.items.AddRange(items);
            return true;
        }
        /// <summary>
        /// Removes the given item from this inventory
        /// </summary>
        /// <param name="item"></param>
        /// <returns>Whether the action was succesful</returns>
        public bool RemoveItem(ItemInInventory item)
        {
            try
            {
                items.Remove(item);
                return true;
            }
            catch { return false; }
        }
        public void ShowInventory()
        {
            //for(int i = 0; i < items.Count; i++)
            //{
            //    inventorySlots[i].Visible = true;
            //    inventorySlots[i].Enabled = true;

            //    RefreshInventory();
            //}
        }
        public void HideInventory()
        {
            for (int i = 0; i < items.Count; i++)
            {
                //inventorySlots[i].Visible = false;
                //inventorySlots[i].Enabled = false;
            }
        }
        public void RefreshInventory()
        {
            //for (int i = 0; i < items.Count; i++)
            //{
            //    if (inventorySlots[i].item != items[i] || inventorySlots[i].Image == null) { inventorySlots[i].Image = inventorySlots[i].item.icon; inventorySlots[i].item = items[i]; }
            //}
        }

        /// <summary>
        /// Moves the item from this inventory to the specified inventory
        /// </summary>
        /// <param name="item"></param>
        /// <param name="otherInventory"></param>
        /// <returns>Whether the action was succesful</returns>
        private bool TransferItem(ItemInInventory item, Inventory otherInventory)
        {
            if (items.Contains(item) && otherInventory.emptySlots > 0)
            {
                RemoveItem(item);
                otherInventory.AddItem(item);
                return true;
            }
            else { return false; }
        }

        private void ItemClicked(object? sender, EventArgs e)
        {
            InventorySlot senderItemSlot = sender as InventorySlot;
            if (Game.Game.MainGameInstance.twoInventoriesOpen)
            {
                if (!TransferItem(senderItemSlot.item, Game.Game.MainGameInstance.otherInventory)) { Debug.LogError("Error moving items between inventories"); }
            }
            else
            {
                senderItemSlot.DropItem();
            }
        }

        public override void Initialize()
        {
            
        }

        public override void DestroySelf()
        {
            items.Clear();
        }

    }
    
    public class ItemInInventory 
    {
        public int itemCount { get; set; }
        public string itemName { get; set; }
        //[JsonIgnore] public Image _inGameSprite;
        [JsonIgnore] private Image? _icon;
        [JsonIgnore] public Image icon { get { if (_icon == null) { _icon = Image.FromFile(fileSourceDirectory); } return _icon; } set => _icon = value; }
        public string fileSourceDirectory { get; set; }
        public ItemInInventory(string itemName, int count, string fileSourceDirectory) { this.itemName = itemName; itemCount = count; this.fileSourceDirectory = fileSourceDirectory; }
        [JsonConstructor] public ItemInInventory() { }
    }
    public class InventorySlot : Button
    {
        public ItemInInventory item;

        public void DropItem() { Debug.Log($"Item {item.itemName}x{item.itemCount} dropped"); }
        
    }

}
