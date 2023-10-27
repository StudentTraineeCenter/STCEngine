using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STCEngine.Engine;

namespace STCEngine
{
    /// <summary>
    /// Base for all components, components define the properties of GameObjects
    /// </summary>
    public abstract class Component 
    {
        public bool enabled;
        public GameObject gameObject;
        public abstract void DestroySelf();
    }

    /// <summary>
    /// Component responsible for position, rotation and scale of the GameObject
    /// </summary>
    public class Transform : Component
    {
        public Vector2 position;
        public float rotation;
        public Vector2 size;

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
    }
    /// <summary>
    /// Component responsible for holding visual information about the GameObject
    /// </summary>
    public class Sprite : Component
    {
        public Image image;
        public Sprite(string fileSourceDirectory, GameObject gameObject)
        {
            this.image = Image.FromFile(fileSourceDirectory);
            EngineClass.AddSpriteToRender(gameObject);
        }

        public override void DestroySelf()
        {
            EngineClass.RemoveSpriteToRender(gameObject);
            gameObject.RemoveComponent<Sprite>();
        }
    }

    /// <summary>
    /// Any object inside the game, has a name and a list of components that defines its properties
    /// </summary>
    public class GameObject
    {
        public List<Component> components = new List<Component>();
        public string name;
        /// <summary>
        /// Defines whether the object is currently active/enabled in the game, inactive GameObjects do not affect the game in any way
        /// </summary>
        public bool isActive;
        public Transform transform;

        #region Constructors
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
        /// Called upon creating a GameObject, registers the object in the Engine class and prints a debug
        /// </summary>
        #endregion
        private void GameObjectCreated()
        {
            try
            {
                transform = components.OfType<Transform>().Single()/* ?? throw new Exception($"GameObject does not contain a (mandatory) Transform component")*/;
            }
            catch(Exception e)
            {
                Debug.LogError($"GameObject \"{name}\" couldn't be created, error message: " + e.Message + " (did you forget to add a Transform component?)");
                return;
            }
            EngineClass.RegisterGameObject(this);
            Debug.LogInfo($"GameObject \"{name}\" Registered");
        }
        public void DestroySelf()
        {
            foreach(Component c in components) { if(c.GetType() != typeof(Transform)){ DestroySelf(); } }
            Debug.LogInfo($"GameObject {name} should be destroyed. (remove all references to this object)");
        }

        #region Component Managment
        public void AddComponent(Component component)
        {
            components.Add(component);
            component.gameObject = this;
            Debug.LogInfo($"Component {component} has been added onto GameObject {this.name}");
        }
        public void RemoveComponent<T>() where T : Component
        {
            Type targetType = typeof(T);
            if (targetType == typeof(Transform)) { Debug.LogWarning("Can't remove Transform component from GameObject!"); return; }
            components.Remove(GetComponent<T>());
            Debug.LogInfo($"Component {targetType} has been removed from GameObject {this.name}");
        }
        public T GetComponent<T>() where T : Component
        {
            Type targetType = typeof(T);
            var foundComponent = components.FirstOrDefault(c => targetType.IsAssignableFrom(c.GetType()));
            
            // Use reflection to create an instance of the specified type and cast the found component to it.
            return foundComponent != null ? (T)Convert.ChangeType(foundComponent, targetType) : null;
        }
        #endregion
    }
}
