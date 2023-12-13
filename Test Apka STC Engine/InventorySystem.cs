using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace STCEngine
{
    public class Inventory : Component, IInteractibleGameObject
    {
        [JsonIgnore] public readonly int slotSpacing = 30; [JsonIgnore] public readonly int slotSize = 64; [JsonIgnore] public readonly Image inventorySlotBackgroundImage = Image.FromFile("Assets/Inventory-Background.png");
        public override string Type { get; } = nameof(Inventory);
        [JsonIgnore] public int emptySlots { get => 15 - items.Count; }
        public List<ItemInInventory> items { get; set; } = new List<ItemInInventory>();
        public bool isPlayerInventory;
        //[JsonIgnore] public List<InventorySlot> inventorySlots = new List<InventorySlot>();

        [JsonConstructor] public Inventory() { }
        public Inventory(bool isPlayerInventory) { this.isPlayerInventory = isPlayerInventory; }
        /// <summary>
        /// Attempts to add the item to the inventory
        /// </summary>
        /// <param name="item"></param>
        /// <returns>Whether the action was succesful</returns>
        public bool AddItem(ItemInInventory item)
        {
            if(emptySlots == 0) { return false; }
            if(items.Any(p => p.itemName == item.itemName))
            {
                items.First(p => p.itemName == item.itemName).itemCount += item.itemCount;
            }
            else
            {
                items.Add(item);
            }

            RefreshInventory();
            return true;
        }
        /// <summary>
        /// Attempts to add an array of items to the inventory
        /// </summary>
        /// <param name="item"></param>
        /// <returns>Whether the action was succesful</returns>
        public bool AddItem(ItemInInventory[] items)
        {
            var emptySlots1 = emptySlots;
            foreach(ItemInInventory i in items)
            {
                if(!items.Any(p => p.itemName == i.itemName)) { emptySlots1--; }
            }
            if(emptySlots1 < 0) { return false; }
            foreach(ItemInInventory i in items)
            {
                AddItem(i);
            }
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
                RefreshInventory(true);
                return true;
            }
            catch { return false; }
        }
        public void ShowInventory()
        {
            RefreshInventory(false, true);
        }
        public void HideInventory()
        {
        }
        /// <summary>
        /// Updates the inventory UI
        /// </summary>
        /// <param name="removedItem"></param>
        public void RefreshInventory(bool removedItem = false, bool updateWholeInventory = false)
        {
            if (updateWholeInventory)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    //combines the image of the item with the amount
                    Bitmap combinedImage = new Bitmap(96, 96);
                    using (Graphics g = Graphics.FromImage(combinedImage))
                    {
                        g.DrawImage(items[i].icon, 0, 0, 96, 96);
                        if (items[i].itemCount != 1) { g.DrawString(items[i].itemCount.ToString(), new Font("Arial", 15, FontStyle.Regular), new SolidBrush(Color.Black), 0, 0); }
                    }

                    if (isPlayerInventory)
                    {
                        Engine.EngineClass.playerInventoryUI.Rows[(int)(i / 5)].Cells[i % 5].Value = combinedImage;
                        Engine.EngineClass.playerInventoryUI.Rows[(int)(i / 5)].Cells[i % 5].ToolTipText = $"{items[i].itemName} x {items[i].itemCount}";
                    }
                    else
                    {
                        Engine.EngineClass.otherInventoryUI.Rows[(int)(i / 5)].Cells[i % 5].Value = combinedImage;
                        Engine.EngineClass.otherInventoryUI.Rows[(int)(i / 5)].Cells[i % 5].ToolTipText = $"{items[i].itemName} x {items[i].itemCount}";
                    }
                }
                for (int i = items.Count; i < items.Count + emptySlots; i++)
                {
                    if (isPlayerInventory)
                    {
                        Engine.EngineClass.playerInventoryUI.Rows[(int)(i / 5)].Cells[i % 5].Value = Engine.EngineClass.emptyImage;
                        Engine.EngineClass.playerInventoryUI.Rows[(int)(i / 5)].Cells[i % 5].ToolTipText = "";
                    }
                    else
                    {
                        Engine.EngineClass.otherInventoryUI.Rows[(int)(i / 5)].Cells[i % 5].Value = Engine.EngineClass.emptyImage;
                        Engine.EngineClass.otherInventoryUI.Rows[(int)(i / 5)].Cells[i % 5].ToolTipText = "";
                    }
                }
            }
            for (int i = 0; i < items.Count; i++)
            {
                //combines the image of the item with the amount
                Bitmap combinedImage = new Bitmap(96, 96);
                using (Graphics g = Graphics.FromImage(combinedImage))
                {
                    g.DrawImage(items[i].icon, 0, 0, 96, 96);
                    if (items[i].itemCount != 1) { g.DrawString(items[i].itemCount.ToString(), new Font("Arial", 15, FontStyle.Regular), new SolidBrush(Color.Black), 0, 0); }
                }
                
                if (isPlayerInventory)
                {
                    Engine.EngineClass.playerInventoryUI.Rows[(int)(i / 5)].Cells[i % 5].Value = combinedImage;
                    Engine.EngineClass.playerInventoryUI.Rows[(int)(i / 5)].Cells[i % 5].ToolTipText = $"{items[i].itemName} x {items[i].itemCount}";
                }
                else 
                {
                    Engine.EngineClass.otherInventoryUI.Rows[(int)(i / 5)].Cells[i % 5].Value = combinedImage;
                    Engine.EngineClass.otherInventoryUI.Rows[(int)(i / 5)].Cells[i % 5].ToolTipText = $"{items[i].itemName} x {items[i].itemCount}";
                }
            }
            if (removedItem)
            {
                if (isPlayerInventory)
                {
                    Engine.EngineClass.playerInventoryUI.Rows[(int)(items.Count / 5)].Cells[items.Count % 5].Value = Engine.EngineClass.emptyImage;
                    Engine.EngineClass.playerInventoryUI.Rows[(int)(items.Count / 5)].Cells[items.Count % 5].ToolTipText = "";
                }
                else
                {
                    Engine.EngineClass.otherInventoryUI.Rows[(int)(items.Count / 5)].Cells[items.Count % 5].Value = Engine.EngineClass.emptyImage;
                    Engine.EngineClass.otherInventoryUI.Rows[(int)(items.Count / 5)].Cells[items.Count % 5].ToolTipText = "";
                }
            }

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
        /// <summary>
        /// Called when an inventory slot is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ItemClicked(object? sender, DataGridViewCellEventArgs e)
        {
            //Debug.Log(sender.GetType());
            Debug.Log($"{e.ColumnIndex}, {e.RowIndex}");
            if(items.Count <= e.RowIndex * 5 + e.ColumnIndex) { Debug.Log("clicked empty slot"); return; }
            if (Game.Game.MainGameInstance.twoInventoriesOpen)
            {
                if (!TransferItem(items[e.RowIndex*5+e.ColumnIndex], isPlayerInventory ? Game.Game.MainGameInstance.otherInventory : Game.Game.MainGameInstance.playerInventory)) { Debug.LogError("Error moving items between inventories"); }
            }
            else
            {
                DropItem(items[e.RowIndex * 5 + e.ColumnIndex]);
            }
        }
        /// <summary>
        /// Drops the specified item to the ground
        /// </summary>
        /// <param name="item"></param>
        public void DropItem(ItemInInventory item)
        {
            Debug.LogInfo($"Dropped {item.itemName}x{item.itemCount}");
            RemoveItem(item);
            GameObject droppedItem = new GameObject($"Dropped Item {item.itemName}x{item.itemCount}, {new Random().Next()}", new List<Component> { new DroppedItem(item), new Transform(Game.Game.MainGameInstance.player.transform.position, 0, Vector2.one*2.5f), new Sprite(item.fileSourceDirectory) });
            droppedItem.GetComponent<DroppedItem>().enabled = false;
            Task.Delay(3000).ContinueWith(t => (droppedItem.GetComponent<DroppedItem>().enabled = true));
            //await Task.Delay(3000);
            //droppedItem.GetComponent<DroppedItem>().enabled = true;

        }
        /// <summary>
        /// Drops the item at the specified index to the ground
        /// </summary>
        /// <param name="itemIndex"></param>
        public void DropItem(int itemIndex)
        {
            DropItem(items[itemIndex]);
            //Debug.Log($"Dropped {items[itemIndex].itemName}x{items[itemIndex].itemCount}");
        }
        /// <summary>
        /// Interact button pressed when this is the closest interactible object in range
        /// </summary>
        public void Interact()
        {
            if (isPlayerInventory) { return; }
            if (Game.Game.MainGameInstance.twoInventoriesOpen) { Game.Game.MainGameInstance.CloseOtherInventory(); }
            else { Game.Game.MainGameInstance.OpenOtherInventory(this); if (!Game.Game.MainGameInstance.playerInventoryPanel.Visible) { Game.Game.MainGameInstance.playerInventoryPanel.Visible = true; } }
        }
        public void Highlight()
        {
            if (isPlayerInventory) { return; }

            Game.Game.MainGameInstance.pressEGameObject.isActive = true;
            Game.Game.MainGameInstance.pressEGameObject.transform.position = this.gameObject.transform.position + Vector2.up * (-100);
        }
        public void StopHighlight()
        {
            if (isPlayerInventory) { return; }

            Game.Game.MainGameInstance.pressEGameObject.isActive = false;
        }
        public void SetupInteractCollider(int range)
        {
            if (isPlayerInventory) { return; }
            gameObject.AddComponent(new CircleCollider(range, "Interactible", Vector2.zero, true, true));
        }
        public override void Initialize()
        {
            Task.Delay(10).ContinueWith(t => SetupInteractCollider(75));
        }

        public override void DestroySelf()
        {
            items.Clear();
        }

    }
    
    public class DroppedItem : Component
    {
        public override string Type { get; } = nameof(DroppedItem);
        public ItemInInventory item { get; set; }
        //[JsonIgnore] public readonly int collectDistance = 100;
        [JsonIgnore] public BoxCollider collectionHitbox;
        
        [JsonConstructor] public DroppedItem() { }
        public DroppedItem(ItemInInventory item)
        {
            this.item = item;
            //collectionHitbox = new BoxCollider(Vector2.one * 100, "droppedItem", Vector2.zero, true);
            
        }

        public void CollectItem()
        {
            if (!enabled) { return; }
            Game.Game.MainGameInstance.playerInventory.AddItem(item);
            gameObject.DestroySelf();
            //gameObject.RemoveComponent<DroppedItem>();
            
        }

        public override void DestroySelf()
        {
            if (gameObject.GetComponent<DroppedItem>() != null) { gameObject.RemoveComponent<DroppedItem>(); return; }
            if (gameObject.GetComponent<BoxCollider>() != null) { gameObject.RemoveComponent<BoxCollider>(); return; }

        }

        public override void Initialize()
        {
            gameObject.AddComponent(new BoxCollider(Vector2.one * 100, "droppedItem", Vector2.zero, true));
            Task.Delay(5).ContinueWith(t => (gameObject.transform.size = Vector2.one * 2.5f));
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

}
