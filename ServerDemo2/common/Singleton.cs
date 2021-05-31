public abstract class Singleton<T> where T : Singleton<T>, new()
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new T();
                instance.Initialize();
            }
            return instance;
        }
    }

    protected virtual void Initialize() { } // 可以重写初始化方法

    public virtual void Destroy()
    {
        instance = null;
    }
}