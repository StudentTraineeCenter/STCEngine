using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace STCEngine.Components
{
    public class NPC : Component, IInteractibleGameObject
    {
        public override string Type => nameof(NPC);
        public Dialogue startingDialogue { get; set; }
        public Dialogue[] dialogues { get; set; }
        public string npcName { get; set; }

        [JsonIgnore] public bool currentlyTalking = false;
        [JsonIgnore] public int talkCount = 0; //counts how many times the player talked to this NPC
        [JsonConstructor] public NPC() { }
        private void StartDialogue(string? id = null)
        {
            currentlyTalking = true;
            Engine.EngineClass.NPCResponseCallback += ResponseChosen;
        }
        private void SkipDialogue()
        {
            currentlyTalking = false;
        }
        public void ResponseChosen(int index)
        {
            Debug.Log($"Chose response {index}");
        }


        public override void Initialize()
        {
            Task.Delay(10).ContinueWith(t => SetupInteractCollider(75));
        }

        public override void DestroySelf()
        {
            if (gameObject.GetComponent<NPC>() != null) { gameObject.RemoveComponent<NPC>(); return; }
        }

        public void Interact()
        {
            if (currentlyTalking)
            {
                SkipDialogue();
            }
            else
            {
                StartDialogue();
            }

        }
        public void StopInteract()
        {
            SkipDialogue();
        }
        public void Highlight()
        {
            Game.Game.MainGameInstance.pressEGameObject.isActive = true;
            Game.Game.MainGameInstance.pressEGameObject.transform.position = this.gameObject.transform.position + Vector2.up * (-100);
        }
        public void StopHighlight()
        {
            Game.Game.MainGameInstance.pressEGameObject.isActive = false;
        }
        public void SetupInteractCollider(int range)
        {
            gameObject.AddComponent(new CircleCollider(range, "Interactible", Vector2.zero, true, true));
        }
    }

    public struct Dialogue
    {
        public string id { get; set; }
        public DialoguePart[] parts { get; set; }

    }
    public struct DialoguePart
    {
        public string message { get; set; }
        public int delay { get; set; } 
    }
}
