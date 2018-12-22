using UnityEngine;
using System.Threading.Tasks;
using QFramework.TimeExtend;
using Timer = QFramework.TimeExtend.Timer;
using UnityEngine.UI;

public class TestForTimer : MonoBehaviour
{
    public Text text;
    private void Awake()
    {
        Timer.IntializeDriver(); //首次初始化不能放在非主线程内。
        Loom.Initialize(); //首次初始化不能放在非主线程内。
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            this.FunctionA();
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            this.FunctionB();
        }
    }
    internal void FunctionA()
    {
        Task task = new Task(() =>
        {
            string _name = "CreateWithTimer ←";
            Timer.AddTimer(0).OnCompleted(() =>
            {
                text.text = _name;  //先演示异常
                new GameObject(_name);
            });
        });
        task.Start();
    }
    internal void FunctionB()
    {
        Task task = new Task(() =>
        {
            string _name = "CreateWithLoom ←";
            Loom.QueueOnMainThread(v =>
            {
                text.text = _name;
                new GameObject(_name);
            }, null);
        });
        task.Start();
    }
}
