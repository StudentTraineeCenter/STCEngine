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
        [JsonIgnore] private Collider interactCollider;
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
            if (gameObject.components.Contains(this)) { gameObject.RemoveComponent(this); }
            if (gameObject.components.Contains(interactCollider)) { gameObject.RemoveComponent(interactCollider); }
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
            interactCollider = gameObject.AddComponent(new CircleCollider(range, "Interactible", Vector2.zero, true, true));
        }
    }

    /// <summary>
    /// A part of the NPC dialogue system, stores all information about how the conversation goes
    /// </summary>
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
    /// <summary>
    /// A part of the NPC dialogue system to be used in a Dialogue to continue with a specific DialoguePart
    /// </summary>
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
    /// <summary>
    /// A part of the NPC dialogue system to be used in a Dialogue with a message and the time it'll be shown
    /// </summary>
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
    /// Component responsible for storing combat stats about a GameObject, adds a Hurtbox and an EnemyHitbox to enemies
    /// </summary>
    public class CombatStats : Component
    {
        public override string Type => nameof(CombatStats);
        public bool isPlayerStats { get; set; }
        public int health { get; set; }
        public int damage { get; set; }
        public int movementSpeed { get; set; }
        public int agroRange { get; set; }
        public int deagroRange { get; set; }
        public float knockbackMultiplier { get; set; }
        public int staggerTime { get; set; }
        //[JsonIgnore] public bool agroed { get; set; }
        /// <summary>
        /// How long the entity is immune to damage after getting hit
        /// </summary>
        public int immuneTime { get; set; }
        [JsonIgnore] public bool attackable { get; private set; } = true;
        //public int armor { get; set; } for future extension of the combat system :)
        [JsonIgnore] public Collider connectedHurtbox { get; private set; }
        [JsonIgnore] public Collider connectedHitbox { get; private set; }
        public CombatStats(int health, int damage, int movementSpeed, int immuneTime, bool isPlayerStats)
        {
            this.health = health;
            this.damage = damage;
            this.immuneTime = immuneTime;
            this.isPlayerStats = isPlayerStats;
        }
        [JsonConstructor] public CombatStats() { }

        public override void DestroySelf()
        {
            if (gameObject.components.Contains(this)) { gameObject.RemoveComponent(this); return; }
            if (!isPlayerStats)
            {
                EngineClass.UnregisterEnemyHurtbox(connectedHurtbox);
                EngineClass.UnregisterEnemyHitbox(connectedHitbox);
            }
        }

        /// <summary>
        /// Deals damage to this entity
        /// </summary>
        /// <param name="damage">The amount of damage this entity took (can be negative to heal)</param>
        /// <returns>Whether the entity has died</returns>
        public bool TakeDamage(int damage)
        {
            if (!attackable) { return false; }

            //knockback
            if(knockbackMultiplier != 0)
            {
                gameObject.transform.position += (gameObject.transform.position - Game.Game.MainGameInstance.player.transform.position).normalized * knockbackMultiplier * damage;
            }

            //the actual damage
            health -= damage;
            Debug.LogInfo($"{this.gameObject.name} got hit for {damage}HP and has {health}HP left");
            attackable = false;
            if (health > 0) { 
                //stagger
                if(staggerTime > 0)
                {
                    var a = movementSpeed;
                    movementSpeed = 0;
                    //gameObject.GetComponent<Sprite>().image //color change -> scrapped, spaghetti code would be present ;)
                    Task.Delay(staggerTime).ContinueWith(t => movementSpeed = a);
                }
                //immunity
                Task.Delay(immuneTime).ContinueWith(t => attackable = true); 
                return false; 
            }
            return true;
        }

        public override void Initialize()
        {
            Task.Delay(10).ContinueWith(t => DelayedInit());
        }
        private void DelayedInit() //has to be delayed, else the GameObject creation with a list of components given breaks
        {
            if (!isPlayerStats)
            {
                connectedHurtbox = gameObject.AddComponent(new CircleCollider(30, "EnemyHurtbox", Vector2.zero, true, true));
                connectedHitbox = gameObject.AddComponent(new CircleCollider(30, "EnemyHitbox", Vector2.zero, true, true));
                EngineClass.RegisterEnemyHurtbox(connectedHurtbox);
                EngineClass.RegisterEnemyHitbox(connectedHitbox);
            }
            //Potentially give the player a hitbox - rn using his collision collider as hitbox
        }

    }

    #endregion
}
