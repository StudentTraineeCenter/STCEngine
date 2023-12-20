using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using STCEngine.Engine;

namespace STCEngine.Components
{
    #region Friendly NPC
    /// <summary>
    /// Interactible component responsible for creating a dialogue with the player
    /// </summary>
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
        [JsonIgnore] private bool exitFlag;
        [JsonConstructor] public NPC() { }
        public NPC(string npcName, Dialogue startingDialogue, List<Dialogue> dialogues)
        {
            this.startingDialogue = startingDialogue;
            this.dialogues = dialogues;
            this.npcName = npcName;
        }
        /// <summary>
        /// Starts the dialogue with the given id
        /// </summary>
        /// <param name="id"></param>
        private void StartDialogue(string? id = null)
        {
            Debug.LogInfo($"Starting dialogue with id {id ?? "startingDialogue"}");
            EngineClass.NPCDialoguePanel.Visible = true;
            currentlyTalking = true;
            EngineClass.NPCDialogueName.Text = npcName;
            if (id == null)
            {
                RunDialogue(startingDialogue);
            }
            else
            {
                try
                {
                    RunDialogue(dialogues.First(p => p.id == id));
                }
                catch (Exception e) { Debug.LogError($"Error starting dialogue with id {id}, error message: {e.Message}"); }
            }

        }

        /// <summary>
        /// Goes through the dialogue with this NPC and handles the text
        /// </summary>
        /// <param name="dialogue"></param>
        /// <returns></returns>
        private async Task RunDialogue(Dialogue dialogue)
        {

            for (int i = 0; i < dialogue.parts.Length; i++)
            {
                if (exitFlag) { return; }
                //interrupt skipem diky chatGPT :>
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
            if (dialogue.responses.Length == 0)
            {
                Debug.LogInfo("Finished Dialogue");
                EndDialogue();
            }
            else
            {
                ShowResponses(dialogue.responses);
            }


        }
        /// <summary>
        /// Skips the wait for the next dialogue part
        /// </summary>
        private void SkipDialogue()
        {
            cancellationTokenSource?.Cancel();
        }
        /// <summary>
        /// Called when the player wants to stop interacting with this NPC
        /// </summary>
        public void EndDialogue()
        {
            exitFlag = true;
            cancellationTokenSource?.Cancel();
            Game.Game.MainGameInstance.activeNPC = null;
            currentlyActiveResponses = Array.Empty<Response>();
            HideResponses();
            EngineClass.NPCDialoguePanel.Visible = false;
            currentlyTalking = false;
            EngineClass.NPCResponseCallback -= ResponseChosen;
        }
        /// <summary>
        /// Shows the response buttons
        /// </summary>
        /// <param name="responses"></param>
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
            if (responses.Length > 2)
            {
                EngineClass.NPCDialogueResponse3.Text = responses[2].responseText;
                EngineClass.NPCDialogueResponse3.Visible = true;
            }
        }
        /// <summary>
        /// Hides the response buttons
        /// </summary>
        private void HideResponses()
        {
            EngineClass.NPCDialogueResponse1.Visible = false;
            EngineClass.NPCDialogueResponse2.Visible = false;
            EngineClass.NPCDialogueResponse3.Visible = false;
        }
        /// <summary>
        /// Called upon a response button being clicked, starts the connected dialogue
        /// </summary>
        /// <param name="index"></param>
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
            if (gameObject.GetComponent<CircleCollider>() != null) { gameObject.RemoveComponent<CircleCollider>(); exitFlag = true; }
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
                exitFlag = false;
                EngineClass.NPCResponseCallback += ResponseChosen;
                StartDialogue();
            }

        }
        public void StopInteract()
        {
            Debug.Log("Stop interact");
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
        [JsonConstructor] public Response() { }
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
        [JsonConstructor] public DialoguePart() { }
        public DialoguePart(string message, int delay)
        {
            this.message = message;
            this.delay = delay;
        }
    }
    #endregion

    #region Enemy NPC

    /// <summary>
    /// Compo
    /// </summary>
    public class CombatStats : Component
    {
        public override string Type => nameof(CombatStats);
        public int health { get; set; }
        public int damage { get; set; }
        public int immuneTimer { get; set; }
        //public int armor { get; set; } for future extension of the combat system :)

        private int activeImmuneTimer;

        public override void DestroySelf()
        {
            if (gameObject.GetComponent<CombatStats>() != null) { gameObject.RemoveComponent<CombatStats>(); }
        }

        public override void Initialize()
        {
            Task.Delay(10).ContinueWith(t => DelayedInit());
        }
        private void DelayedInit() //has to be delayed, else the GameObject creation with a list of components given breaks
        {
            gameObject.AddComponent(new CircleCollider(30, "Hurtbox", Vector2.zero, true, true));
            gameObject.AddComponent(new CircleCollider(30, "EnemyHitbox", Vector2.zero, true, true));
        }

    }
    #endregion
}
