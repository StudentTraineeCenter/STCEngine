using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STCEngine.Engine;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace STCEngine.Components
{
    #region Components
    /// <summary>
    /// Base for all components, components define the properties of GameObjects
    /// </summary>
    [JsonConverter(typeof(ComponentConverter))]
    public abstract class Component
    {
        public abstract string Type { get; }
        public bool enabled { get; set; } = true;
        /// <summary>
        /// the GameObject this component is attached to
        /// </summary>
        [JsonIgnore] public GameObject gameObject { get; set; }

        /// <summary>
        /// Function that gets called upon creating this component
        /// </summary>
        public abstract void Initialize();
        /// <summary>
        /// Function that gets called upon destroying this component or the GameObject its attached to
        /// </summary>
        public abstract void DestroySelf();
        [JsonConstructor] protected Component() { }
        /// <summary>
        /// Serializes the component and saves it in the given directory under name format $"{this?.gameObject.name}-{this.GetType().Name}.json"
        /// </summary>
        /// <param name="destinationDirectory"></param>
        public void SerializeToJSON(string destinationDirectory)
        {
            string serializedGameObjectString = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true, Converters = { new ComponentConverter() } });
            string fileName = $"{this?.gameObject.name}-{this.GetType().Name}.json";
            File.WriteAllText(destinationDirectory + "/" + fileName, serializedGameObjectString);
        }
    }



    /// <summary>
    /// A component responsible for position, rotation and scale of the GameObject
    /// </summary>
    public class Transform : Component
    {
        public override string Type { get; } = nameof(Transform);
        public Vector2 position { get; set; } = Vector2.one;
        /// <summary>
        /// Currently not yet implemented, does not rotate attached sprites/other
        /// </summary>
        public float rotation { get; set; }
        public Vector2 size { get; set; }

        /// <summary>
        /// Creates a Transform component with a given position, rotation and size
        /// </summary>
        public Transform(Vector2 position, float rotation, Vector2 size)
        {
            this.position = position == null ? Vector2.zero : position;
            this.rotation = rotation;
            this.size = size == null ? Vector2.one : size;
        }
        public override void DestroySelf()
        {
            Debug.LogWarning("Tried to destroy Transform component, destroying the whole GameObject");
            gameObject.DestroySelf();
        }
        public override void Initialize() { }
    }

    #region Rendering-related components
    /// <summary>
    /// A component responsible for holding visual information about the GameObject
    /// </summary>
    public class Sprite : Component
    {
        public override string Type { get; } = nameof(Sprite);
        [JsonIgnore] private Image? _image;
        [JsonIgnore] public Image image { get { if (_image == null) { _image = Image.FromFile(fileSourceDirectory); } return _image; } set { bool a = flipX; bool b = flipY; flipX = false; flipY = false;  _image = value; flipX = a; flipY = b; } /*makes sure its rotated correctly */}
        public int orderInLayer { get => _orderInLayer ??= -999; set { if (_orderInLayer != null) { EngineClass.ChangeSpriteRenderOrder(gameObject, value); } _orderInLayer = value; } }//higher numbers render on top of lower numbers
        [JsonIgnore] private int? _orderInLayer { get; set; }
        public string fileSourceDirectory { get; set; }
        public bool flipX { get => _flipX; set { if (_flipX != value) { image.RotateFlip(RotateFlipType.RotateNoneFlipX); } _flipX = value; } }
        private bool _flipX { get; set; }
        public bool flipY { get => _flipY; set { if (_flipY != value) { image.RotateFlip(RotateFlipType.RotateNoneFlipY); } _flipY = value; } }
        private bool _flipY { get; set; }
        public Sprite(string fileSourceDirectory, bool flipX = false, bool flipY = false, int orderInLayer = int.MaxValue)
        {
            this.fileSourceDirectory = fileSourceDirectory;
            this.image = Image.FromFile(fileSourceDirectory);
            this._orderInLayer = orderInLayer;
            this.flipX = flipX;
            this.flipY = flipY;
        }
        [JsonConstructor] public Sprite() { }
        public override void Initialize()
        {
            EngineClass.AddSpriteToRender(gameObject, orderInLayer);
            this._orderInLayer = EngineClass.spritesToRender.IndexOf(gameObject);
        }
        public override void DestroySelf()
        {
            if (gameObject.components.Contains(this)) { gameObject.RemoveComponent(this); return; }
            EngineClass.RemoveSpriteToRender(gameObject);
        }
    }
    /// <summary>
    /// A component responsible for rendering UI elements, essentially a basic sprite but handled differently while rendering
    /// </summary>
    public class UISprite : Component
    {
        public override string Type { get; } = nameof(UISprite);
        [JsonIgnore] private Image? _image;
        [JsonIgnore] public Image image { get { if (_image == null) { _image = Image.FromFile(fileSourceDirectory); } return _image; } set => _image = value; }
        public Anchor screenAnchor { get; set; } = Anchor.TopLeft;
        public Anchor pivotPointAnchor { get; set; } = Anchor.TopLeft;
        public Vector2 screenAnchorOffset
        {
            get
            {
                switch ((int)screenAnchor)
                {
                    case 0:
                        return Vector2.zero;
                    case 1:
                        return new Vector2(Game.Game.MainGameInstance.screenSize.x * 0.5f, 0);
                    case 2:
                        return new Vector2(Game.Game.MainGameInstance.screenSize.x, 0);
                    case 3:
                        return new Vector2(0, Game.Game.MainGameInstance.screenSize.y * 0.5f);
                    case 4:
                        return new Vector2(Game.Game.MainGameInstance.screenSize.x * 0.5f, Game.Game.MainGameInstance.screenSize.y * 0.5f);
                    case 5:
                        return new Vector2(Game.Game.MainGameInstance.screenSize.x, Game.Game.MainGameInstance.screenSize.y * 0.5f);
                    case 6:
                        return new Vector2(0, Game.Game.MainGameInstance.screenSize.y);
                    case 7:
                        return new Vector2(Game.Game.MainGameInstance.screenSize.x * 0.5f, Game.Game.MainGameInstance.screenSize.y);
                    case 8:
                        return Game.Game.MainGameInstance.screenSize;
                    default:
                        Debug.LogError("Invalid screen anchor, returning Vector2.zero");
                        return Vector2.zero;
                }
            }
        }
        public Vector2 pivotPointOffset
        {
            get
            {
                switch ((int)pivotPointAnchor)
                {
                    case 0:
                        return new Vector2(image.Width * gameObject.transform.size.x, image.Height * gameObject.transform.size.y) / (-2);
                    case 1:
                        return new Vector2(0, image.Height * gameObject.transform.size.y) /(-2);
                    case 2:
                        return new Vector2(image.Width * gameObject.transform.size.x, -image.Height * gameObject.transform.size.y) / 2;
                    case 3:
                        return new Vector2(-image.Width * gameObject.transform.size.x, 0) / 2;
                    case 4:
                        return new Vector2(0, 0);
                    case 5:
                        return new Vector2(image.Width * gameObject.transform.size.x, 0) / 2;
                    case 6:
                        return new Vector2(-image.Width * gameObject.transform.size.x, image.Height * gameObject.transform.size.y) / 2;
                    case 7:
                        return new Vector2(0, image.Height * gameObject.transform.size.y) / 2;
                    case 8:
                        return new Vector2(image.Width * gameObject.transform.size.x, image.Height * gameObject.transform.size.y) / 2;
                    default:
                        Debug.LogError("Invalid screen anchor, returning Vector2.zero");
                        return Vector2.zero;
                }
            }
        }
        public Vector2 offset { get; set; } = Vector2.zero;

        public int orderInUILayer { get => _orderInUILayer; set { EngineClass.ChangeUISpriteRenderOrder(gameObject, value); _orderInUILayer = value; } }//higher numbers render on top of lower numbers
        private int _orderInUILayer { get; set; }
        public string fileSourceDirectory { get; set; }
        public UISprite(string fileSourceDirectory, Anchor screenAnchor, Anchor pivotPointAnchor, Vector2 offset, int orderInLayer = int.MaxValue)
        {
            this.offset = offset;
            this.screenAnchor = screenAnchor;
            this.fileSourceDirectory = fileSourceDirectory;
            this.image = Image.FromFile(fileSourceDirectory);
            this._orderInUILayer = orderInLayer;
        }
        public UISprite(string fileSourceDirectory, Anchor screenAnchor, Anchor pivotPointAnchor, int orderInLayer = int.MaxValue)
        {
            this.screenAnchor = screenAnchor;
            this.fileSourceDirectory = fileSourceDirectory;
            this.image = Image.FromFile(fileSourceDirectory);
            this._orderInUILayer = orderInLayer;
        }
        public UISprite(string fileSourceDirectory, int orderInLayer = int.MaxValue)
        {
            this.fileSourceDirectory = fileSourceDirectory;
            this.image = Image.FromFile(fileSourceDirectory);
            this._orderInUILayer = orderInLayer;
        }
        [JsonConstructor] public UISprite() { } //not needed
        public override void Initialize()
        {
            EngineClass.AddUISpriteToRender(gameObject, orderInUILayer);
            this._orderInUILayer = EngineClass.UISpritesToRender.IndexOf(gameObject);
        }
        public override void DestroySelf()
        {
            if (gameObject.components.Contains(this)) { gameObject.RemoveComponent(this); return; }
            EngineClass.RemoveUISpriteToRender(gameObject);
        }
        public enum Anchor { TopLeft, TopCentre, TopRight, MiddleLeft, MiddleCentre, MiddleRight, LeftBottom, MiddleBottom, RightBottom }
    }
    /// <summary>
    /// A component responsible for rendering a grid of images
    /// </summary>
    public class Tilemap : Component
    {
        public override string Type { get; } = nameof(Tilemap);
        public int OrderInLayer { get => orderInLayer; set { EngineClass.ChangeSpriteRenderOrder(gameObject, value); orderInLayer = value; } }//higher numbers render on top of lower numbers
        private int orderInLayer { get; set; }
        public Dictionary<string, string> tileSources { get; set; }
        public string[] tilemapString { get; set; }
        [JsonIgnore] private Image[,]? _tiles;
        [JsonIgnore] public Image[,]? tiles { get => _tiles; set { _tiles = value; UpdateTiles(); } }
        [JsonIgnore] public Image tileMapImage; //{ get => GetCurrent};
        public Vector2 tileSize { get; set; }
        public Vector2 mapSize { get; set; }

        /// <summary>
        /// Creating tilemaps with this constructor is not recommended, rather use .json files with tilemap component
        /// </summary>
        /// <param name="tileSources"></param>
        /// <param name="tilemapString"></param>
        /// <param name="tileSize"></param>
        /// <param name="mapSize"></param>
        /// <param name="orderInLayer">Higher numbers render over lower numbers</param>
        public Tilemap(Dictionary<string, string> tileSources, string[] tilemapString, Vector2 tileSize, Vector2 mapSize, int orderInLayer)
        {
            this.tileSources = tileSources;
            this.tilemapString = tilemapString;
            this.tileSize = tileSize;
            this.mapSize = mapSize;
            this.orderInLayer = orderInLayer;
        }



        /// <summary>
        /// Creates tiles array and adds it to the render queue
        /// </summary>
        private void CreateTilemap()
        {
            try
            {
                _tiles = new Image[(int)mapSize.x, (int)mapSize.y];
                //creates the user key + path dictionary (ex. grass -> Assets/GrassTexture.png)
                //CreateDictionary();

                for (int y = 0; y < mapSize.y; y++)
                {
                    for (int x = 0; x < mapSize.x; x++)
                    {
                        _tiles[x, y] = tileSources.TryGetValue(tilemapString[x + y * (int)mapSize.x], out string? value) ? (value == "" ? EngineClass.emptyImage : Image.FromFile(value)) : throw new Exception("Wrong tilemap configuration");
                    }
                }
                tiles = _tiles;

            }
            catch (Exception e)
            {
                Debug.LogError("Error creating tilemap, error message: " + e.Message);
            }
        }

        /// <summary>
        /// Combines the multidimensional array of images into one image to be rendered
        /// </summary>
        private void UpdateTiles()
        {
            // Pomoc od ChatGPT :)
            // Calculate the size of the final combined image
            int totalWidth = tiles.GetLength(0) * (int)tileSize.x;
            int totalHeight = tiles.GetLength(1) * (int)tileSize.y;

            // Create a new bitmap to hold the combined image
            Bitmap combinedImage = new Bitmap(totalWidth, totalHeight);

            // Create a graphics object to draw on the combined image
            using (Graphics g = Graphics.FromImage(combinedImage))
            {
                // Iterate through the multidimensional array and draw each image onto the combined image
                for (int i = 0; i < tiles.GetLength(0); i++)
                {
                    for (int j = 0; j < tiles.GetLength(1); j++)
                    {
                        g.DrawImage(tiles[i, j], i * tileSize.x, j * tileSize.y);
                    }
                }
            }

            //saves it to render it
            tileMapImage = combinedImage;
        }

        //private class TilemapValues
        //{
        //    public string[] tilemapString { get; set; }
        //    public Dictionary<string, string> tileSources { get; set; }
        //    public float mapWidth { get; set; }
        //    public float mapHeight { get; set; }
        //    public float tileWidth { get; set; }
        //    public float tileHeight { get; set; }
        //}
        [JsonConstructor] public Tilemap() { }
        public override void Initialize()
        {
            CreateTilemap();
            EngineClass.AddSpriteToRender(gameObject, OrderInLayer);
            this.orderInLayer = EngineClass.spritesToRender.IndexOf(gameObject);
        }
        public override void DestroySelf()
        {
            if (gameObject.components.Contains(this)) { gameObject.RemoveComponent(this); return; }
            EngineClass.RemoveSpriteToRender(gameObject);
        }
    }
    #endregion

    #region Animation-related components and classes
    /// <summary>
    /// A component responsible for animating a gameobject with a sprite component
    /// </summary>
    public class Animator : Component
    {
        public override string Type { get; } = nameof(Animator);
        [JsonIgnore] public Sprite? sprite { get; set; }
        public Dictionary<string, Animation> animations { get; set; }
        /// <summary>
        /// Name of the animation to be played on load, if none leave empty
        /// </summary>
        public string? playOnLoad { get; set; }
        //[JsonIgnore] public Dictionary<string, Animation> _animations { get; private set; }
        public float playBackSpeed { get; set; }
        [JsonIgnore] public Animation? currentlyPlayingAnimation { get; private set; }
        [JsonIgnore] public bool isPlaying { get => currentlyPlayingAnimation != null; }
        public Animator(Animation animation, float playBackSpeed = 1)
        {
            this.animations = new Dictionary<string, Animation>();
            animations.Add(animation.name, animation);

            this.playBackSpeed = Math.Clamp(playBackSpeed, 0, float.PositiveInfinity);
        }

        /// <summary>
        /// Adds an animation to the animator, which can later be played using the Play function
        /// </summary>
        /// <param name="animation"></param>
        public void AddAnimation(Animation animation) { animations.Add(animation.name, animation); }
        /// <summary>
        /// Plays the animation of the given name
        /// </summary>
        /// <param name="animationName"></param>
        public void Play(string animationName)
        {
            if (sprite == null) { sprite = gameObject.GetComponent<Sprite>(); }
            if (isPlaying) { Stop(); }
            if (animations.TryGetValue(animationName, out Animation? animation)) { EngineClass.AddSpriteAnimation(animation); animation.sprite = sprite; currentlyPlayingAnimation = animation; animation.animator = this; }
            else { Debug.LogError($"Animation {animationName} not found and couldn't be played."); }
        }
        public void Stop()
        {
            try
            {
                EngineClass.RemoveSpriteAnimation(currentlyPlayingAnimation ?? throw new Exception("Trying to stop animator that isn't playing!"));
                currentlyPlayingAnimation = null;
            }
            catch (Exception e) { Debug.LogError("Error stopping animation, error message: " + e.Message); }
        }


        [JsonConstructor] public Animator() { }
        public override void Initialize() { if (playOnLoad?.Length > 0) { Play(playOnLoad); } }
        public override void DestroySelf()
        {
            if (isPlaying) { Stop(); }
            if (gameObject.components.Contains(this)) { gameObject.RemoveComponent(this); return; }
        }
    }
    /// <summary>
    /// A single frame of an animation
    /// </summary>
    public class AnimationFrame
    {
        [JsonIgnore] private Image? _image;
        [JsonIgnore] public Image image { get { if (_image == null) { _image = Image.FromFile(fileSourceDirectory); } return _image; } set => _image = value; }
        public string fileSourceDirectory { get; set; }
        /// <summary>
        /// How long the frame stays in ms
        /// </summary>
        public int time { get; set; }
        //public AnimationFrame(Image image, int time)
        //{
        //    this.image = image;
        //    this.time = time;
        //}

        ///<summary>
        /// Creates a frame of an animation from the source of the image and the time how long this image should stay in the animation in ms
        /// </summary>
        public AnimationFrame(String fileSourceDirectory, int time)
        {
            this.fileSourceDirectory = fileSourceDirectory;
            this.image = Image.FromFile(fileSourceDirectory);
            this.time = time;
        }
    }
    /// <summary>
    /// An animation to be used with the Animator component
    /// </summary>
    public class Animation
    {
        public string name { get; set; }
        public AnimationFrame[] animationFrames { get; set; }
        public bool loop { get; set; }
        [JsonIgnore] public Animator animator { get; set; }
        [JsonIgnore] private int timer { get; set; }
        [JsonIgnore] private int nextFrameTimer { get; set; }
        [JsonIgnore] private int animationFrame { get; set; }
        [JsonIgnore] public Sprite? sprite { get; set; }
        [JsonIgnore] public int duration { get { int dur = 0; foreach (AnimationFrame a in animationFrames) { dur += a.time; } return dur; } }

        /// <summary>
        /// Internal function, should NEVER be called by the user!
        /// To start an animation, call the "Play" function in the animator component!
        /// </summary>
        public void RunAnimation()
        {
            if (sprite == null) { Debug.LogError("Animation sprite not found (was the RunAnimation function called manually? To play an animation, use the \"Play\" function in the Animator component!)"); }
            if (timer > nextFrameTimer)
            {
                //Debug.Log(timer.ToString() + ", " + animationFrame);
                if (animationFrame < animationFrames.Count() - 1)
                {
                    sprite.image = animationFrames[animationFrame + 1].image;
                    nextFrameTimer = animationFrames[animationFrame + 1].time;
                    timer = 0;
                    animationFrame++;
                }
                else if (loop)
                {
                    sprite.image = animationFrames[0].image;
                    nextFrameTimer = animationFrames[0].time;
                    timer = 0;
                    animationFrame = 0;
                }
                else
                {
                    animator.Stop();
                    timer = 0;
                    animationFrame = 0;
                }
            }
            timer += 10;
        }

        public Animation(string name, AnimationFrame[] animationFrames, bool loop)
        {
            this.name = name;
            this.animationFrames = animationFrames;
            this.loop = loop;
            timer = 0; nextFrameTimer = animationFrames[0].time; animationFrame = 0;
        }
    }
    #endregion

    #region Colliders
    /// <summary>
    /// Base class for all components responsible for detecting collisions
    /// </summary>
    public abstract class Collider : Component
    {
        public bool isTrigger { get; set; } //whether it stops movement upon collision
        public Vector2 offset { get; set; }
        public string tag { get; set; }

        /// <summary>
        /// Checks if this collider is coliding with the given collider for all registered colliders or the given collider list
        /// </summary>
        /// <param name="otherCollider"></param>
        /// <returns>Whether this collider overlaps with the given collider</returns>
        public abstract bool IsColliding(Collider other);
        /// <summary>
        /// Checks if this collider is coliding with a collider with the given tag for all registered colliders or the given collider list
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="includeTriggers"></param>
        /// <param name="colliderList"></param>
        /// <returns>Whether this collider overlaps with a collider with the given tag</returns>
        public abstract bool IsColliding(string tag, bool includeTriggers, List<Collider>? colliderList = null);
        /// <summary>
        /// Checks if this collider is coliding with a collider with the given tag for all registered colliders or the given collider list
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="includeTriggers"></param>
        /// <param name="collider"></param>
        /// <param name="colliderList"></param>
        /// <returns>Whether this collider overlaps with a collider with the given tag</returns>
        public abstract bool IsColliding(string tag, bool includeTriggers, out Collider? collider, List<Collider>? colliderList = null);

        /// <summary>
        /// Returns all the colliders colliding with this one, option to include triggers, for all registered colliders or the given collider list
        /// </summary>
        /// <param name="includeTriggers"></param>
        /// <param name="colliderList"></param>
        /// <returns>Array of colliding colliers</returns>
        public abstract Collider[] OverlapCollider(bool includeTriggers = false, List<Collider>? colliderList = null);

        [JsonConstructor] protected Collider() { }

    }
    /// <summary>
    /// A component responsible for detecting collisions in a box area
    /// </summary>
    public class BoxCollider : Collider
    {
        public override string Type { get; } = nameof(BoxCollider);
        public Vector2 size { get; set; }


        public bool debugDraw { get; private set; }
        /// <summary>
        /// Creates the box collider of the given size and with a given offset from gameObjects position 
        /// </summary>
        /// <param name="size"></param>
        /// <param name="tag"></param>
        /// <param name="offset"></param>
        /// <param name="isTrigger"></param>
        /// <param name="debugDraw"></param>
        public BoxCollider(Vector2 size, string tag, Vector2? offset = null, bool isTrigger = false, bool debugDraw = false)
        {
            this.tag = tag;
            this.size = size;
            this.offset = offset ?? Vector2.zero;
            this.isTrigger = isTrigger;
            this.debugDraw = debugDraw;
        }

        
        public override bool IsColliding(Collider otherCollider)
        {
            if (!enabled || !otherCollider.enabled) { return false; }
            if (otherCollider.GetType() == typeof(BoxCollider))
            {
                BoxCollider otherCollider1 = otherCollider as BoxCollider;
                var relativeDistance = otherCollider.gameObject.transform.position + otherCollider.offset - gameObject.transform.position - offset;
                return ((Math.Abs(relativeDistance.x) < (size.x + otherCollider1.size.x) / 2) && (Math.Abs(relativeDistance.y) < (size.y + otherCollider1.size.y) / 2));
            }
            else if (otherCollider.GetType() == typeof(CircleCollider))
            {
                CircleCollider otherCollider1 = otherCollider as CircleCollider;
                //return ((Math.Abs(relativeDistance.x) < (size.x/2 + otherCollider1.radius)) && (Math.Abs(relativeDistance.y) < (size.y/2 + otherCollider1.radius)));
                //var relativeDistance = otherCollider.gameObject.transform.position + otherCollider.offset - gameObject.transform.position - offset;
                //return ((Math.Abs(relativeDistance.x) < (otherCollider1.size.x / 2 + radius)) && (Math.Abs(relativeDistance.y) < (otherCollider1.size.y / 2 + radius)));

                var relativeDistance = otherCollider.gameObject.transform.position + otherCollider.offset - gameObject.transform.position - offset;
                //yoinknuto z https://stackoverflow.com/questions/401847/circle-rectangle-collision-detection-intersection :)
                Vector2 circleDistance = Vector2.zero;
                circleDistance.x = MathF.Abs(relativeDistance.x);
                circleDistance.y = MathF.Abs(relativeDistance.y);

                if (circleDistance.x > (size.x / 2 + otherCollider1.radius)) { return false; }
                if (circleDistance.y > (size.y / 2 + otherCollider1.radius)) { return false; }

                if (circleDistance.x <= (size.x / 2)) { return true; }
                if (circleDistance.y <= (size.y / 2)) { return true; }

                int cornerDistance_sq = (int)(MathF.Pow((circleDistance.x - size.x / 2), 2) +
                                     MathF.Pow((circleDistance.y - size.y / 2), 2));

                return (cornerDistance_sq <= (MathF.Pow(otherCollider1.radius, 2)));
            }
            throw new Exception("Error with determining collider type");
        }


        public override bool IsColliding(string tag, bool includeTriggers, List<Collider>? colliderList = null)
        {
            foreach (Collider col in colliderList == null ? EngineClass.registeredColliders : colliderList)
            {
                if (col.tag == tag && IsColliding(col) && (!(!includeTriggers && col.isTrigger))) { return true; }
            }
            return false;
        }

        public override bool IsColliding(string tag, bool includeTriggers, out Collider? collider, List<Collider>? colliderList = null)
        {
            foreach (Collider col in colliderList == null ? EngineClass.registeredColliders : colliderList)
            {
                if (col.tag == tag && IsColliding(col) && (!(!includeTriggers && col.isTrigger))) { collider = col; return true; }
            }
            collider = null;
            return false;
        }


        public override Collider[] OverlapCollider(bool includeTriggers = false, List<Collider>? colliderList = null)
        {
            List<Collider> outList = new List<Collider>();
            foreach (Collider col in colliderList == null ? EngineClass.registeredColliders : colliderList)
            {
                if (col.IsColliding(this) && (!(!includeTriggers && col.isTrigger))) { outList.Add(col); }
            }
            return outList.ToArray();
        }

        [JsonConstructor] BoxCollider() { }
        public override void Initialize()
        {
            EngineClass.RegisterCollider(this);

            if (debugDraw)
            {
                EngineClass.AddDebugRectangle(this, 0);
            }
        }
        public override void DestroySelf()
        {
            if (gameObject.components.Contains(this)) { gameObject.RemoveComponent(this); return; }
            if (debugDraw) { EngineClass.RemoveDebugRectangle(this); }
            EngineClass.UnregisterCollider(this);

        }


    }

    /// <summary>
    /// A component responsible for detecting collisions in a circle area
    /// </summary>
    public class CircleCollider : Collider
    {
        public override string Type { get; } = nameof(CircleCollider);
        public int radius { get; set; }


        public bool debugDraw { get; private set; }
        /// <summary>
        /// Creates the box collider of the given size and with a given offset from gameObjects position 
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="tag"></param>
        /// <param name="offset"></param>
        /// <param name="isTrigger"></param>
        /// <param name="debugDraw"></param>
        public CircleCollider(int radius, string tag, Vector2? offset = null, bool isTrigger = false, bool debugDraw = false)
        {
            this.tag = tag;
            this.radius = radius;
            this.offset = offset ?? Vector2.zero;
            this.isTrigger = isTrigger;
            this.debugDraw = debugDraw;
        }

        public override bool IsColliding(Collider otherCollider)
        {
            if (otherCollider.GetType() == typeof(CircleCollider))
            {
                CircleCollider otherCollider1 = otherCollider as CircleCollider;
                var relativeDistance = otherCollider.gameObject.transform.position + otherCollider.offset - gameObject.transform.position - offset;
                return (MathF.Abs(relativeDistance.x) < radius + otherCollider1.radius && MathF.Abs(relativeDistance.y) < radius + otherCollider1.radius);
            }
            else if (otherCollider.GetType() == typeof(BoxCollider))
            {
                BoxCollider otherCollider1 = otherCollider as BoxCollider;
                //return ((Math.Abs(relativeDistance.x) < (otherCollider1.size.x / 2 + radius)) && (Math.Abs(relativeDistance.y) < (otherCollider1.size.y / 2 + radius)));
                var relativeDistance = otherCollider.gameObject.transform.position + otherCollider.offset - gameObject.transform.position - offset;

                //yoinknuto z https://stackoverflow.com/questions/401847/circle-rectangle-collision-detection-intersection :)
                Vector2 circleDistance = Vector2.zero;
                circleDistance.x = MathF.Abs(relativeDistance.x);
                circleDistance.y = MathF.Abs(relativeDistance.y);

                if (circleDistance.x > (otherCollider1.size.x / 2 + radius)) { return false; }
                if (circleDistance.y > (otherCollider1.size.y / 2 + radius)) { return false; }

                if (circleDistance.x <= (otherCollider1.size.x / 2)) { return true; }
                if (circleDistance.y <= (otherCollider1.size.y / 2)) { return true; }

                int cornerDistance_sq = (int)(MathF.Pow((circleDistance.x - otherCollider1.size.x / 2), 2) +
                                     MathF.Pow((circleDistance.y - otherCollider1.size.y / 2), 2));

                return (cornerDistance_sq <= (MathF.Pow(radius, 2)));
            }
            throw new Exception("Error with determining collider type");
        }

        public override bool IsColliding(string tag, bool includeTriggers, List<Collider>? colliderList = null)
        {
            foreach (Collider col in colliderList == null ? EngineClass.registeredColliders : colliderList)
            {
                if (col.tag == tag && IsColliding(col) && (!(!includeTriggers && col.isTrigger))) { return true; }
            }
            return false;
        }

        public override bool IsColliding(string tag, bool includeTriggers, out Collider? collider, List<Collider>? colliderList = null)
        {
            foreach (Collider col in colliderList == null ? EngineClass.registeredColliders : colliderList)
            {
                if (col.tag == tag && IsColliding(col) && (!(!includeTriggers && col.isTrigger))) { collider = col; return true; }
            }
            collider = null;
            return false;
        }


        public override Collider[] OverlapCollider(bool includeTriggers = false, List<Collider>? colliderList = null)
        {
            List<Collider> outList = new List<Collider>();
            foreach (Collider col in colliderList == null ? EngineClass.registeredColliders : colliderList)
            {
                if (col.IsColliding(this) && (!(!includeTriggers && col.isTrigger))) { outList.Add(col); }
            }
            return outList.ToArray();
        }

        [JsonConstructor] CircleCollider() { }
        public override void Initialize()
        {
            EngineClass.RegisterCollider(this);

            if (debugDraw)
            {
                EngineClass.AddDebugRectangle(this, 0);
            }
        }
        public override void DestroySelf()
        {
            if (gameObject.components.Contains(this)) { gameObject.RemoveComponent(this); return; }
            if (debugDraw) { EngineClass.RemoveDebugRectangle(this); }
            EngineClass.UnregisterCollider(this);

        }
    }
    #endregion

    /// <summary>
    /// Interface for all interactible objects with the interact button (E)
    /// </summary>
    public interface IInteractibleGameObject
    {
        /// <summary>
        /// Called upon pressing the interact button when nearby
        /// </summary>
        public void Interact();
        /// <summary>
        /// Called upon pressing the interact button when nearby and "interacting" or running too far away 
        /// </summary>
        public void StopInteract();
        /// <summary>
        /// Called upon coming into interact range
        /// </summary>
        public void Highlight();
        /// <summary>
        /// Called upon leaving interact range
        /// </summary>
        public void StopHighlight();
        /// <summary>
        /// Sets up the interact collider, usually called in delayed Init method
        /// </summary>
        public void SetupInteractCollider(int range);
    }

    /// <summary>
    /// Component responsible for an interactible object which can activate and deactivate a collider
    /// </summary>
    public class ToggleCollider : Component, IInteractibleGameObject
    {
        public override string Type => nameof(ToggleCollider);
        [JsonIgnore] public BoxCollider connectedCollider { get; private set; }
        [JsonIgnore] private CircleCollider interactCollider;

        [JsonIgnore] public bool state { get; set; } = true;
        /// <summary>
        /// Leave empty to not change sprite
        /// </summary>
        public string offSpriteImageSource { get; set; }
        /// <summary>
        /// Leave empty to not change sprite
        /// </summary>
        public string onSpriteImageSource { get; set; }
        [JsonIgnore] public Image offSpriteImage { get { if (_offSpriteImage == null) { _offSpriteImage = Image.FromFile(offSpriteImageSource); } return _offSpriteImage; } set => _offSpriteImage = value; }
        [JsonIgnore] private Image? _offSpriteImage;
        [JsonIgnore] public Image onSpriteImage { get { if (_onSpriteImage == null) { _onSpriteImage = Image.FromFile(onSpriteImageSource); } return _onSpriteImage; } set => _onSpriteImage = value; }
        [JsonIgnore] private Image? _onSpriteImage;


        [JsonConstructor] public ToggleCollider() { }

        public void Interact() //toggles the hitbox
        {
            Sprite sprite;
            if ((sprite = gameObject.GetComponent<Sprite>()) != null && !(offSpriteImageSource == null || offSpriteImageSource == ""))
            {
                if (state) //opened -> closed
                {
                    sprite.image = offSpriteImage;
                }
                else //closed -> opened
                {
                    sprite.image = onSpriteImage;
                }
                state = !state;
            }
            connectedCollider.enabled = !connectedCollider.enabled;
        }
        public void StopInteract() { }

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

        public override void DestroySelf()
        {
            if (gameObject.components.Contains(this)) { gameObject.RemoveComponent(this); }
            if (gameObject.components.Contains(interactCollider)) { gameObject.RemoveComponent(interactCollider); }
        }

        public override void Initialize()
        {
            Task.Delay(10).ContinueWith(t => DelayedInit());
        }
        private void DelayedInit()
        {
            BoxCollider? box = gameObject.GetComponent<BoxCollider>();
            if (box == null)
            {
                Debug.LogError($"Toggle collider on GameObject {gameObject.name} doesn't have a BoxCollider to work with! \nRemoving ToggleCollider component...");
                gameObject.RemoveComponent<ToggleCollider>();
            }
            else
            {
                connectedCollider = box;
                SetupInteractCollider(75);
                
                Sprite sprite;
                if ((sprite = gameObject.GetComponent<Sprite>()) != null && !(offSpriteImageSource == null || offSpriteImageSource == ""))
                {
                    if (connectedCollider.enabled) //opened -> closed
                    {
                        sprite.image = onSpriteImage;
                    }
                    else //closed -> opened
                    {
                        sprite.image = offSpriteImage;
                    }
                    state = connectedCollider.enabled;
                }
            }
        }
    }

    #endregion


    /// <summary>
    /// Any object inside the game, has a name and a list of components that defines its properties
    /// </summary>
    public class GameObject
    {

        public List<Component> components { get; set; } = new List<Component>(); //is converted to the specific derivatives when creating a json file
        public string name { get; set; }
        /// <summary>
        /// Defines whether the object is currently active/enabled in the game, inactive GameObjects do not affect the game in any way
        /// </summary>
        public bool isActive
        {
            get => _isActive;
            set
            {
                isActiveChanged(!_isActive); //zatim nevyuzito :)
                _isActive = value;
            }
        }
        private bool _isActive { get; set; }
        [JsonIgnore] public Transform transform { get; set; }

        #region Constructors and related
        [JsonConstructor] public GameObject() { }

        /// <summary>
        /// Creates a GameObject with given components
        /// </summary>
        public GameObject(string name, List<Component> components, bool isActive = true)
        {
            this.name = name;
            this.isActive = isActive;
            foreach (Component c in components) { AddComponent(c); }
            GameObjectCreated();
        }

        /// <summary>
        /// Creates a GameObject with Transform component at given position with no rotation and scale 0
        /// </summary>
        public GameObject(string name, Vector2 position, bool isActive = true)
        {
            this.name = name;
            this.isActive = isActive;
            AddComponent(new Transform(position, 0, Vector2.zero));
            GameObjectCreated();
        }
        /// <summary>
        /// Creates a GameObject with the given Transform component
        /// </summary>
        public GameObject(string name, Transform transform, bool isActive = true)
        {
            this.name = name;
            this.isActive = isActive;
            AddComponent(transform);
            GameObjectCreated();
        }

        /// <summary>
        /// Searches for a GameObject by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns>GameObject of the given name, if not found null</returns>
        public static GameObject? Find(string name)
        {
            return EngineClass.registeredGameObjects.TryGetValue(name, out GameObject value) ? value : null;
        }

        /// <summary>
        /// Searches for all GameObjects with a name containing the given string
        /// </summary>
        /// <param name="nameContains"></param>
        /// <returns>A list of GameObjects with name containing the given string</returns>
        public static List<GameObject> FindAll(string nameContains)
        {
            List<GameObject> outList = new List<GameObject>();
            foreach(GameObject g in EngineClass.registeredGameObjects.Values)
            {
                if (g.name.Contains(nameContains)) { outList.Add(g); }
            }
            return outList;
        }

        /// <summary>
        /// Creates a new GameObject from a JSON file with the given source path
        /// </summary>
        /// <param name="jsonSourceFilePath">Path to the json file to load it from</param>
        /// <returns>Reference to the new GameObject</returns>
        public static GameObject CreateGameObjectFromJSONFile(string jsonSourceFilePath)
        {
            try
            {
                string jsonString = File.ReadAllText(jsonSourceFilePath);
                var newGameObject = JsonSerializer.Deserialize<GameObject>(jsonString, new JsonSerializerOptions { WriteIndented = true, Converters = { new ComponentConverter() } });
                foreach (Component c in newGameObject.components)
                {
                    c.gameObject = newGameObject;
                    c.Initialize();
                }
                newGameObject.GameObjectCreated();
                return newGameObject;
            }
            catch (Exception e) { Debug.LogError("GameObject couldn't be created from json file, error message: " + e.Message + ", " + e.StackTrace); }
            return null;
        }
        /// <summary>
        /// Creates a new GameObject from a JSON file with the given json string
        /// </summary>
        /// <param name="jsonString">The json string to create the GameObject from</param>
        /// <returns>Reference to the new GameObject</returns>
        public static GameObject CreateGameObjectFromJSONString(string jsonString)
        {
            try
            {
                var newGameObject = JsonSerializer.Deserialize<GameObject>(jsonString, new JsonSerializerOptions { Converters = { new ComponentConverter() } });
                
                foreach (Component c in newGameObject.components)
                {
                    c.gameObject = newGameObject;
                    c.Initialize();
                }
                newGameObject.GameObjectCreated();
                return newGameObject;
            }
            catch (Exception e) { Debug.LogError("GameObject couldn't be created from json string, error message: " + e.Message); }
            return null;
        }
        /// <summary>
        /// Serializes the GameObject and saves it to the given directory
        /// </summary>
        /// <param name="destinationDirectory">Where the json file will be saved</param>
        public void SerializeGameObject(string destinationDirectory)
        {
            if(GetComponents<CircleCollider>()?.Length == 0)
            {
                foreach(CircleCollider c in GetComponents<CircleCollider>()) { if (c.tag == "Interactible") { RemoveComponent(c); } }
            }
            string serializedGameObjectString = JsonSerializer.Serialize<GameObject>(this, new JsonSerializerOptions { WriteIndented = true, Converters = { new ComponentConverter() } });
            string fileName = this.name + ".json";
            File.WriteAllText(destinationDirectory + "/" + fileName, serializedGameObjectString);

        }
        /// <summary>
        /// Serializes the given GameObject unindented and returns it as a string
        /// </summary>
        /// <returns>The GameObject in the form of a unindented json string</returns>
        public string SerializeGameObject()
        {
            string serializedGameObjectString = JsonSerializer.Serialize<GameObject>(this, new JsonSerializerOptions { WriteIndented = false, Converters = { new ComponentConverter() } });
            //string fileName = this.name + ".json";
            return serializedGameObjectString;
            //File.WriteAllText(destinationDirectory + "/" + fileName, serializedGameObjectString);

        }
        /// <summary>
        /// Called upon creating a GameObject, registers the object in the Engine class and prints a debug
        /// </summary>
        private void GameObjectCreated()
        {
            try
            {
                transform = components.OfType<Transform>().Single()/* ?? throw new Exception($"GameObject does not contain a (mandatory) Transform component")*/;
            }
            catch (Exception e)
            {
                Debug.LogError($"GameObject \"{name}\" couldn't be created, error message: " + e.Message + " (did you forget to add a Transform component?)");
                return;
            }
            EngineClass.RegisterGameObject(this);
            Debug.LogInfo($"GameObject \"{name}\" Registered");
        }
        #endregion

        public static void isActiveChanged(bool newState) { } //jeste nevyuzito :)

        #region Component Managment
        /// <summary>
        /// Adds the given component to the GameObject
        /// </summary>
        /// <param name="component"></param>
        /// <returns>The newly added componnent</returns>
        public T AddComponent<T>(T component) where T : Component
        {
            components.Add(component);
            component.gameObject = this;
            //Debug.LogInfo($"Component {component} has been added onto GameObject {this.name}");
            component.Initialize();
            return component;
        }
        /// <summary>
        /// Removes the component of given type from the GameObject
        /// </summary>
        /// <typeparam name="Component Type"></typeparam>
        public void RemoveComponent<T>() where T : Component
        {
            var component = GetComponent<T>();
            if (component == null) { Debug.LogError($"Tried removing a non-existing component {typeof(T)} from GameObject {name}!"); return; }

            Type targetType = typeof(T);
            if (targetType == typeof(Transform)) { Debug.LogWarning("Can't remove Transform component from GameObject!"); return; }

            components.Remove(component);
            component.DestroySelf();
            //Debug.LogInfo($"Component {targetType} has been removed from GameObject {this.name}");
        }
        public void RemoveComponent(Component component)
        {
            if (component == null) { Debug.LogError($"Tried removing a non-existing component {component.GetType()} from GameObject {name}!"); return; }
            if (component.GetType() == typeof(Transform)) { Debug.LogError($"Cannot remove Transform component from GameObject! (name: {name})"); return; }

            components.Remove(component);
            component.DestroySelf();
            //Debug.LogInfo($"Component {component.GetType()} has been removed from GameObject {this.name}");
        }
        /// <summary>
        /// Returns a reference to the component of given type on the GameObject
        /// </summary>
        /// <typeparam name="Component Type"></typeparam>
        /// <returns>Reference to the specified component</returns>
        public T? GetComponent<T>() where T : Component
        {
            Type targetType = typeof(T);
            var foundComponent = components.FirstOrDefault(c => targetType.IsAssignableFrom(c.GetType()));

            // Use reflection to create an instance of the specified type and cast the found component to it.
            return foundComponent != null ? (T)Convert.ChangeType(foundComponent, targetType) : null;
        }
        /// <summary>
        /// Returns references to all of the components of the given type on the GameObject
        /// </summary>
        /// <typeparam name="Component Type"></typeparam>
        /// <returns>Array of references to the components of the specified type</returns>
        public T[]? GetComponents<T>() where T : Component
        {
            Type targetType = typeof(T);
            var foundComponents = components.FindAll(c => targetType.IsAssignableFrom(c.GetType()));

            List<T>? convertedComponents = new List<T>();

            foreach (Component a in foundComponents)
            {
                // Use reflection to create an instance of the specified type and cast the found component to it.
                convertedComponents.Add((T)Convert.ChangeType(a, targetType));
            }
            return convertedComponents.Count != 0 ? convertedComponents.ToArray() : null;
        }

        #endregion
        /// <summary>
        /// Self destructs this GameObject and all its components
        /// </summary>
        public void DestroySelf()
        {
            int componentsAmount = components.Count;
            for (int i = 0; i < componentsAmount; i++) { if (components[0].GetType() != typeof(Transform)) { components[0].DestroySelf(); /*RemoveComponent<Component>();*/ } else if (components.Count > 1) { components[1].DestroySelf(); } }
            //this.isActive = false;
            if (components.Count > 1) { Debug.LogError($"Error destroying Gameobject, some component didn't destroy itself!"); return; }
            EngineClass.UnregisterGameObject(this);
            Debug.LogInfo($"GameObject {name} should be destroyed. (remove all references to this object)");
        }

    }



    /// <summary>
    /// Converts Components into specific derivatives of Components during serialization of a component
    /// </summary>
    public class ComponentConverter : JsonConverter<Component> //diky chatgpt :3
    {
        public override Component Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (var jsonDoc = JsonDocument.ParseValue(ref reader))
            {
                switch (jsonDoc.RootElement.GetProperty("Type").GetString())
                {
                    case nameof(Transform):
                        return jsonDoc.RootElement.Deserialize<Transform>(options) as Transform;
                    case nameof(Animator):
                        return jsonDoc.RootElement.Deserialize<Animator>(options) as Animator;
                    case nameof(Sprite):
                        return jsonDoc.RootElement.Deserialize<Sprite>(options) as Sprite;
                    case nameof(Tilemap):
                        return jsonDoc.RootElement.Deserialize<Tilemap>(options) as Tilemap;
                    case nameof(BoxCollider):
                        return jsonDoc.RootElement.Deserialize<BoxCollider>(options) as BoxCollider;
                    case nameof(CircleCollider):
                        return jsonDoc.RootElement.Deserialize<CircleCollider>(options) as CircleCollider;
                    case nameof(UISprite):
                        return jsonDoc.RootElement.Deserialize<UISprite>(options) as UISprite;
                    case nameof(Inventory):
                        return jsonDoc.RootElement.Deserialize<Inventory>(options) as Inventory;
                    case nameof(DroppedItem):
                        return jsonDoc.RootElement.Deserialize<DroppedItem>(options) as DroppedItem;
                    case nameof(ToggleCollider):
                        return jsonDoc.RootElement.Deserialize<ToggleCollider>(options) as ToggleCollider;
                    case nameof(NPC):
                        return jsonDoc.RootElement.Deserialize<NPC>(options) as NPC;
                    case nameof(CombatStats):
                        return jsonDoc.RootElement.Deserialize<CombatStats>(options) as CombatStats;
                    default:
                        throw new JsonException("'Type' doesn't match a known derived type");
                }

            }
        }

        public override void Write(Utf8JsonWriter writer, Component value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }

}

