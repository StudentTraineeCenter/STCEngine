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
        public bool isPlayerInventory;
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
                //Engine.EngineClass.playerInventoryUI.Rows[(int)(items.IndexOf(item) / 5)].Cells[items.IndexOf(item) % 5].Value = Engine.EngineClass.emptyImage;
                items.Remove(item);
                RefreshInventory(true);
                return true;
            }
            catch { return false; }
        }
        public void ShowInventory()
        {
            RefreshInventory();
            //for(int i = 0; i < items.Count; i++)
            //{
            //    inventorySlots[i].Visible = true;
            //    inventorySlots[i].Enabled = true;

            //    RefreshInventory();
            //}
        }
        public void HideInventory()
        {
            //for (int i = 0; i < items.Count; i++)
            //{
            //    //inventorySlots[i].Visible = false;
            //    //inventorySlots[i].Enabled = false;
            //}
        }
        public void RefreshInventory(bool removedItem = false)
        {
            if (isPlayerInventory)
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
                    Engine.EngineClass.playerInventoryUI.Rows[(int)(i / 5)].Cells[i % 5].Value = combinedImage;
                    Engine.EngineClass.playerInventoryUI.Rows[(int)(i / 5)].Cells[i % 5].ToolTipText = $"{items[i].itemName} x {items[i].itemCount}";
                    //Engine.EngineClass.playerInventoryUI.Refresh();
                }
                if (removedItem) 
                { 
                    Engine.EngineClass.playerInventoryUI.Rows[(int)(items.Count / 5)].Cells[items.Count % 5].Value = Engine.EngineClass.emptyImage;
                    Engine.EngineClass.playerInventoryUI.Rows[(int)(items.Count / 5)].Cells[items.Count % 5].ToolTipText = "";
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

        public void ItemClicked(object? sender, DataGridViewCellEventArgs e)
        {
            //Debug.Log(sender.GetType());
            Debug.Log($"{e.ColumnIndex}, {e.RowIndex}");
            if(items.Count <= e.RowIndex * 5 + e.ColumnIndex) { Debug.Log("clicked empty slot"); return; }
            if (Game.Game.MainGameInstance.twoInventoriesOpen)
            {
                if (!TransferItem(items[e.RowIndex*5+e.ColumnIndex], Game.Game.MainGameInstance.otherInventory)) { Debug.LogError("Error moving items between inventories"); }
            }
            else
            {
                DropItem(items[e.RowIndex * 5 + e.ColumnIndex]);
            }
        }
        public void DropItem(ItemInInventory item)
        {
            Debug.Log($"Dropped {item.itemName}x{item.itemCount}");
            RemoveItem(item);
            GameObject droppedItem = new GameObject($"Dropped Item {item.itemName}x{item.itemCount}, {new Random().Next()}", new List<Component> { new DroppedItem(item), new Transform(Game.Game.MainGameInstance.player.transform.position, 0, Vector2.one), new Sprite(item.fileSourceDirectory) });
            droppedItem.GetComponent<DroppedItem>().enabled = false;
            Task.Delay(3000).ContinueWith(t => (droppedItem.GetComponent<DroppedItem>().enabled = true));
            //await Task.Delay(3000);
            //droppedItem.GetComponent<DroppedItem>().enabled = true;

        }
        public void DropItem(int itemIndex)
        {
            DropItem(items[itemIndex]);
            //Debug.Log($"Dropped {items[itemIndex].itemName}x{items[itemIndex].itemCount}");
        }

        public override void Initialize()
        {
            
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
