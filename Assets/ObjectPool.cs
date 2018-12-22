using System;
using System.Collections.Concurrent;
public interface IPoolable
{
    /// <summary>
    /// 对象池 用于标记层别是否可被回收
    /// </summary>
    AllocateState AllocateState { get; set; }
}
public enum AllocateState
{
     InUse,//使用中
     Recycled //已回收
}

public sealed class ObjectPool<T> where T : IPoolable 
{
    internal ConcurrentStack<T> items; //线程安全 栈
    private Func<T> factory; //埋入的实例化 规则
    public int Count //池子有多少货
    {
        get
        {
            return items.Count;
        }
    }
    public int Capacity { get; set; } //池子有多大

    internal ObjectPool(Func<T> factory,int capacity = 100)
    {
        this.factory = factory;
        this.Capacity = capacity;
        items = new ConcurrentStack<T>();
    }
    public void Clear()
    {
        items.Clear();
    }
    public T Allocate() //分配
    {
        T item = default(T);
        if (items.IsEmpty || !items.TryPop(out item))
        {
            item =factory.Invoke();
        }
        item.AllocateState = AllocateState.InUse; //标记为使用中
        return item;
    }
    public void Release(T target) //释放
    {
        //池爆炸了再多都不再要了，当然，如果不是 InUse 的就更别想挤进来~
        if (target.AllocateState.Equals(AllocateState.InUse) && items.Count < Capacity) 
        {
            items.Push(target);
        }
    }
}
public class A : IPoolable
{
    public AllocateState AllocateState { get; set; }
    public string feild;
    public void RestData() // 重置数据
    {
        feild = string.Empty;
    }
}

public class B
{
    public void Test()
    {
        ObjectPool<A> pool = new ObjectPool<A>(InstantiateRule); //造个池子，埋入实例化规则
        A a = pool.Allocate(); //分配对象
        a.feild = "假装在使用这个对象";
        a.RestData(); //重置数据准备回收啦
        pool.Release(a); //回收到对象池
    }
    private A InstantiateRule()//实例化规则
    {
        return new A(); //当然可以写更多定制化的东西在里面
    }
}


