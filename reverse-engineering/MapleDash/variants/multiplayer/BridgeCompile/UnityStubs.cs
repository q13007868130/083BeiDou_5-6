// Compile-only substitutes for the UnityEngine symbols used by the bridge/presenter.
// Never include this file in a production Unity project.
namespace UnityEngine
{
    public class Object
    {
        public static void Destroy(Object obj) { }
    }
    public class MonoBehaviour : Object
    {
        public readonly Transform transform = new Transform();
        public T GetComponent<T>() where T : class { return null; }
    }
    public class Transform : Object
    {
        public Vector3 localPosition;
        public Vector3 localScale;
        public void SetParent(Transform parent, bool worldPositionStays) { }
    }
    public class GameObject : Object
    {
        public readonly Transform transform = new Transform();
        public GameObject(string name) { }
        public T AddComponent<T>() where T : new() { return new T(); }
    }
    public struct Vector3
    {
        public Vector3(float x, float y, float z) { }
    }
    public struct Vector2
    {
        public Vector2(float x, float y) { }
    }
    public struct Rect
    {
        public Rect(float x, float y, float width, float height) { }
    }
    public struct Color
    {
        public static Color white => new Color(1f, 1f, 1f, 1f);
        public Color(float r, float g, float b, float a) { }
    }
    public enum TextureFormat { RGBA32 }
    public class Texture2D : Object
    {
        public Texture2D(int width, int height, TextureFormat format, bool mipChain) { }
        public void SetPixel(int x, int y, Color color) { }
        public void Apply() { }
    }
    public class Sprite : Object
    {
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit)
            => new Sprite();
    }
    public class SpriteRenderer
    {
        public Sprite sprite;
        public Color color;
        public int sortingOrder;
    }
    public enum TextAnchor { MiddleCenter }
    public class TextMesh
    {
        public string text;
        public TextAnchor anchor;
        public float characterSize;
        public int fontSize;
    }
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public sealed class SerializeField : System.Attribute { }
    public static class Debug { public static void LogError(object message) { } }
}
