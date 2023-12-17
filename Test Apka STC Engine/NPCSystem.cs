using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using STCEngine.Engine;

namespace STCEngine.Components
{
    public class NPC : Component, IInteractibleGameObject
    {
        public override string Type => nameof(NPC);
        public Dialogue startingDialogue { get; set; }
        public List<Dialogue> dialogues { get; set; }
        public string npcName { get; set; }

        [JsonIgnore] public bool currentlyTalking = false;
        //[JsonIgnore] public int talkCount = 0; //counts how many times the player talked to this NPC
        [JsonIgnore] private static CancellationTokenSource cancellationTokenSource;
        [JsonIgnore] private Response[] currentlyActiveResponses;
        [JsonConstructor] public NPC() { }
        public NPC(string npcName, Dialogue startingDialogue, List<Dialogue> dialogues)
        {
            this.startingDialogue = startingDialogue;
            this.dialogues = dialogues;
            this.npcName = npcName;
        }
        private void StartDialogue(string? id = null)
        {
            Debug.LogInfo($"Starting dialogue with id {id ?? "startingDialogue"}");
            EngineClass.NPCDialoguePanel.Visible = true;
            currentlyTalking = true;
            EngineClass.NPCDialogueName.Text = npcName;
            if(id == null)
            {
                RunDialogue(startingDialogue);
            }
            else
            {
                try
                {
                    RunDialogue(dialogues.First(p => p.id == id));
                }catch(Exception e) { Debug.LogError($"Error starting dialogue with id {id}, error message: {e.Message}"); }
            }
            
        }

        //interrupt skipem diky chatGPT :>
        private async Task RunDialogue(Dialogue dialogue)
        {

            for (int i = 0; i < dialogue.parts.Length; i++)
            {
                cancellationTokenSource = new CancellationTokenSource();
                CancellationToken cancellationToken = cancellationTokenSource.Token;
                try
                {
                    // Your dialogue logic here
                    EngineClass.NPCDialogueText.Text = dialogue.parts[i].message;

                    // Simulate waiting for a set amount of time (e.g., 5 seconds)
                    await Task.Delay(dialogue.parts[i].delay, cancellationToken);

                }
                catch (TaskCanceledException)
                {
                    Debug.LogInfo("Dialogue Skipped");
                }
                finally
                {
                    cancellationTokenSource?.Dispose();
                    cancellationTokenSource = null;
                }
            }
            if(dialogue.responses.Length == 0)
            {
                Debug.LogInfo("Finished Dialogue");
                EndDialogue();
            }
            else
            {
                ShowResponses(dialogue.responses);
            }
            

        }
        private void SkipDialogue()
        {
            cancellationTokenSource?.Cancel();
        }
        public void EndDialogue()
        {
            Game.Game.MainGameInstance.activeNPC = null;
            currentlyActiveResponses = Array.Empty<Response>();
            HideResponses();
            EngineClass.NPCDialoguePanel.Visible = false;
            currentlyTalking = false;
            EngineClass.NPCResponseCallback -= ResponseChosen;
        }
        private void ShowResponses(Response[] responses)
        {

            currentlyActiveResponses = responses;

            EngineClass.NPCDialogueResponse1.Text = responses[0].responseText;
            EngineClass.NPCDialogueResponse1.Visible = true;

            if (responses.Length > 1)
            {
                EngineClass.NPCDialogueResponse2.Text = responses[1].responseText;
                EngineClass.NPCDialogueResponse2.Visible = true;
            }
            if(responses.Length > 2)
            {
                EngineClass.NPCDialogueResponse3.Text = responses[2].responseText;
                EngineClass.NPCDialogueResponse3.Visible = true;
            }
        }
        private void HideResponses()
        {
            EngineClass.NPCDialogueResponse1.Visible = false;
            EngineClass.NPCDialogueResponse2.Visible = false;
            EngineClass.NPCDialogueResponse3.Visible = false;
        }
        public void ResponseChosen(int index)
        {
            Debug.LogInfo($"Chose response {currentlyActiveResponses[index].responseText} with linkID {currentlyActiveResponses[index].linkID}");
            StartDialogue(currentlyActiveResponses[index].linkID);
            HideResponses();
        }


        public override void Initialize()
        {
            Task.Delay(10).ContinueWith(t => SetupInteractCollider(75));
        }

        public override void DestroySelf()
        {
            bool exitFlag = false;
            if (gameObject.GetComponent<NPC>() != null) { gameObject.RemoveComponent<NPC>(); exitFlag = true; }
            if(gameObject.GetComponent<CircleCollider>() != null) { gameObject.RemoveComponent<CircleCollider>(); exitFlag = true; }
            if (exitFlag) { return; }
        }

        public void Interact()
        {
            if (currentlyTalking)
            {
                SkipDialogue();
            }
            else
            {
                Game.Game.MainGameInstance.activeNPC = this;
                EngineClass.NPCResponseCallback += ResponseChosen;
                StartDialogue();
            }

        }
        public void StopInteract()
        {
            EndDialogue();
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

    public class Dialogue
    {
        public string id { get; set; }
        public DialoguePart[] parts { get; set; }
        public Response[] responses { get; set; }
        [JsonConstructor] public Dialogue() { }

        public Dialogue(string id, DialoguePart[] parts, Response[]? responses = null)
        {
            this.id = id;
            this.parts = parts;
            this.responses = responses ?? Array.Empty<Response>();
        }
    }
    public class Response
    {
        public string linkID { get; set; }
        public string responseText { get; set; }
        [JsonConstructor]public Response() { }
        public Response(string linkID, string responseText)
        {
            this.linkID = linkID;
            this.responseText = responseText;
        }
    }
    public class DialoguePart
    {
        public string message { get; set; }
        public int delay { get; set; }
        [JsonConstructor]public DialoguePart() { }
        public DialoguePart(string message, int delay)
        {
            this.message = message;
            this.delay = delay;
        }
    }
}
