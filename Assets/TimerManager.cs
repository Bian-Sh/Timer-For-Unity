using System;
using UnityEngine;
using System.Collections.Generic;

namespace QFramework.TimeExtend
{
    public class Timer
    {
        static List<Timer> timers = new List<Timer>();
        private Action<float> UpdateEvent;
        private Action EndEvent;

        /// <summary>
        /// 是否循环执行
        /// </summary>
        private bool _loop;
        /// <summary>
        /// 是否忽略Timescale
        /// </summary>
        private bool _ignoreTimescale;
        /// <summary>
        /// 用户指定的定时器标志，便于手动清除、暂停、恢复
        /// </summary>
        private string _flag;
        private float CurrentTime
        {
            get
            {
                return driver.GetCurrentTime(this._ignoreTimescale);
            }
        }
        private static TimerDriver driver = null;
        /// <summary>
        /// 缓存时间
        /// </summary>
        private float cachedTime;
        /// <summary>
        /// 已经流逝的时光
        /// </summary>
        float timePassed;
        /// <summary>
        /// 计时器是否结束
        /// </summary>
        private bool _isFinish = false;

        /// <summary>
        /// 计时器是否暂停
        /// </summary>
        private bool _isPause = false;

        /// <summary>
        /// 确认是否输出Debug信息(全局设置)
        /// </summary>
        public static bool ShowLog { set; get; } = true;
        private bool showLog_Single = true;
        private bool showlog_internal { get { return ShowLog && showLog_Single; } }
        /// <summary>
        /// 当前定时器设定的时间
        /// </summary>
        public float Duration { get; private set; } = -1;

        /// <summary>
        /// 暂停计时器
        /// </summary>
        public bool IsPause
        {
            get { return _isPause; }
            set
            {
                if (value)
                {
                    Pause();
                }
                else
                {
                    Resume();
                }
            }
        }
        /// <summary>
        /// 运行中的定时器的总数
        /// </summary>
        public static int Count
        {
            get
            {
                lock (timers)
                {
                    return timers.Count;
                }
            }
        }

        /// <summary>
        /// 初始化 Timer 驱动 ，一般都是懒汉加载，但如要在 非主线程使用，请务必手动初始化
        /// </summary>
        public static void IntializeDriver()
        {
            driver = driver ?? TimerDriver.Initialize();
        }

        /// <summary>
        /// 构造定时器
        /// </summary>
        /// <param name="time">定时时长</param>
        /// <param name="flag">定时器标识符,相同的标识符将导致Timer的重新配置，请留意</param>
        /// <param name="loop">是否循环</param>
        /// <param name="ignorTimescale">是否忽略TimeScale</param>
        private Timer Config(float time, string flag, bool loop = false, bool ignorTimescale = true)
        {
            IntializeDriver(); //初始化Time驱动
            _ignoreTimescale = ignorTimescale;
            Duration = time;
            _loop = loop;
            cachedTime = CurrentTime;
            _isFinish = false;
            _isPause = false;
            _flag = string.IsNullOrEmpty(flag) ? GetHashCode().ToString() : flag;//设置辨识标志符
            return this;
        }


        /// <summary>  
        /// 暂停计时  
        /// </summary>  
        private Timer Pause()
        {
            if (_isFinish)
            {
                if (showlog_internal) Debug.LogWarning("【TimerTrigger（容错）】:计时已经结束！");
            }
            else
            {
                _isPause = true;
            }
            return this;
        }
        /// <summary>  
        /// 继续计时  
        /// </summary>  
        private Timer Resume()
        {
            if (_isFinish)
            {
                if (showlog_internal) Debug.LogWarning("【TimerTrigger（容错）】:计时已经结束！");
            }
            else
            {
                if (_isPause)
                {
                    cachedTime = CurrentTime - timePassed;
                    _isPause = false;
                }
                else
                {
                    if (showlog_internal) Debug.LogWarning("【TimerTrigger（容错）】:计时并未处于暂停状态！");
                }
            }
            return this;
        }
        /// <summary>
        /// 刷新定时器
        /// </summary>
        private void Update()
        {
            if (!_isFinish && !_isPause) //运行中
            {
                timePassed = CurrentTime - cachedTime;
                UpdateEvent?.Invoke(Mathf.Clamp01(timePassed / Duration));
                if (timePassed >= Duration)
                {
                    EndEvent?.Invoke();
                    if (_loop)
                    {
                        cachedTime = CurrentTime;
                    }
                    else
                    {
                        Stop();
                    }
                }
            }
        }

        /// <summary>
        /// 回收定时器
        /// </summary>
        private void Stop()
        {
            if (timers.Contains(this))
            {
                lock (timers)
                {
                    timers.Remove(this);
                }
                Duration = -1;
                _flag = string.Empty;
                _isFinish = true;
                _isPause = false;
                UpdateEvent = null;
                EndEvent = null;
            }
        }


        #region--------------------------Static Function Extend-------------------------------------
        #region-------------AddEntity---------------
        /// <summary>
        /// 添加定时触发器
        /// </summary>
        /// <param name="time">定时时长</param>
        /// <param name="flag">定时器标识符</param>
        /// <param name="loop">是否循环</param>
        /// <param name="ignorTimescale">是否忽略TimeScale</param>
        /// <returns></returns>
        public static Timer AddTimer(float time, string flag = "", bool loop = false, bool ignorTimescale = true)
        {
            Timer timer = GetTimer(flag);
            timer = timer??new Timer();
            timer.Config(time, flag, loop, ignorTimescale);
            lock (timers)
            {
                timers.Add(timer);
            }
            return timer;
        }
        #endregion

        #region-------------UpdateAllTimer---------------
        static List<Timer> handlingTimers = new List<Timer>();
        /// <summary>
        /// 在TimerDriver Update中驱动，请勿调用
        /// </summary>
        public static void UpdateAllTimer()
        {
            if (timers.Count > 0)
            {
                lock (timers)
                {
                    handlingTimers.Clear();
                    handlingTimers.AddRange(timers);
                }
                handlingTimers.ForEach(v => v?.Update());
            }
        }
        #endregion

        #region-------------ValidateCheckTimer---------------
        /// <summary>
        /// 确认是否存在指定的定时器
        /// </summary>
        /// <param name="flag">标志位指定</param>
        public static bool Exist(string flag)
        {
            return timers.Exists(v => v._flag == flag);
        }
        /// <summary>
        /// 确认是否存在指定的定时器
        /// </summary>
        /// <param name="flag">指定的定时器</param>
        public static bool Exist(Timer timer)
        {
            return timers.Contains(timer);
        }

        /// <summary>
        /// 获得指定的定时器 
        /// </summary>
        /// <param name="flag">标志位指定</param>
        public static Timer GetTimer(string flag)
        {
            return timers.Find(v => v._flag == flag);
        }

        #endregion


        #region-------------DelEntity---------------
        /// <summary>
        /// 删除用户指定的计时触发器
        /// </summary>
        /// <param name="flag">指定的标识符</param>
        public static void DelTimer(string flag)
        {
            Timer timer = GetTimer(flag);
            timer?.Stop();
            if (ShowLog) Debug.Assert(null != timer, "【TimerTrigger（容错）】:此定时器已完成触发或无此定时器！");
        }
        /// <summary>
        /// 删除用户指定的计时触发器
        /// </summary>
        /// <param name="flag">指定的标识符</param>
        public static void DelTimer(Timer target)
        {
            target?.Stop();
        }
        /// <summary>
        /// 删除用户指定的计时触发器
        /// </summary>
        /// <param name="completedEvent">指定的完成事件(直接赋值匿名函数无效)</param>
        public static void DelTimer(Action completedEvent)
        {
            Timer timer = timers.Find(v => v.EndEvent.Equals(completedEvent));
            timer?.Stop();
            if (ShowLog) Debug.Assert(null != timer, "【TimerTrigger（容错）】:查无此定时器！---方法名：【" + completedEvent.Method.Name + "】。");
        }
        /// <summary>
        /// 删除用户指定的计时触发器
        /// </summary>
        /// <param name="updateEvent">指定的Update事件(直接赋值匿名函数无效)</param>
        public static void DelTimer(Action<float> updateEvent)
        {
            Timer timer = timers.Find(v => v.UpdateEvent.Equals(updateEvent));
            timer?.Stop();
            if (ShowLog) Debug.Assert(null != timer, "【TimerTrigger（容错）】:查无此定时器！---方法名：【" + updateEvent.Method.Name + "】。");
        }

        /// <summary>
        /// 删除运行中所有计时触发器
        /// </summary>
        public static void RemoveAll()
        {
            lock (timers)
            {
                for (int i = 0; i < timers.Count; i++)
                {
                    timers[i]?.Stop();
                }
                timers.Clear();
            }
        }
        #endregion
        #endregion

        #region-------------AddEvent-------------------
        public void AddEvent(Action completedEvent)
        {
            if (null == EndEvent || EndEvent.GetInvocationList().Length == 0)
            {
                EndEvent = completedEvent;
            }
            else
            {
                Delegate[] delegates = EndEvent.GetInvocationList();
                if (!Array.Exists(delegates, v => v == (Delegate)completedEvent))
                {
                    EndEvent += completedEvent;
                }
                else
                {
                    if (ShowLog) Debug.LogWarning("【TimerTrigger（容错）】:定时器无法执行 Complete 的重复添加！---方法名：【" + completedEvent.Method.Name + "】。");
                }
            }
        }
        public void AddEvent(Action<float> updateEvent)
        {
            if (null == UpdateEvent || updateEvent.GetInvocationList().Length == 0)
            {
                UpdateEvent = updateEvent;
            }
            else
            {
                Delegate[] delegates = UpdateEvent.GetInvocationList();
                if (!Array.Exists(delegates, v => v == (Delegate)updateEvent))
                {
                    UpdateEvent += updateEvent;
                }
                else
                {
                    if (ShowLog) Debug.LogWarning("【TimerTrigger（容错）】:定时器无法执行 Update 事件的重复添加！---方法名：【" + updateEvent.Method.Name + "】。");
                }
            }
        }
        #endregion

        #region ---------------运行中的定时器参数修改（Log/SetTime/SetLoop/SetIgnoreTimeScale）-----------
        /// <summary>
        /// 是否显示 Log 信息,只对该Timer生效 
        /// </summary>
        /// <param name="value">true：在全局设置允许显示Log时 会输出log ，默认输出</param>
        /// <returns></returns>
        public Timer Log(bool value)
        {
            if (_isFinish)
            {
                if (showlog_internal) Debug.LogWarning("【TimerTrigger（容错）】:计时已经结束！");
            }
            else
            {
                this.showLog_Single = value;
            }
            return this;
        }

        /// <summary>
        /// 重新设置运行中的定时器的时间
        /// </summary>
        /// <param name="endTime">定时时长</param>
        public Timer SetTime(float endTime)
        {
            if (_isFinish)
            {
                if (showlog_internal) Debug.LogWarning("【TimerTrigger（容错）】:计时已经结束！");
            }
            else
            {
                if (endTime == Duration)
                {
                    if (showlog_internal) Debug.Log("【TimerTrigger（容错）】:时间已被设置，请勿重复操作！");
                }
                else
                {
                    if (endTime < 0)
                    {
                        if (showlog_internal) Debug.Log("【TimerTrigger（容错）】:时间不支持负数，已自动取正！");
                        endTime = Mathf.Abs(endTime);
                    }
                    if (endTime < timePassed)//如果用户设置时间已错失
                    {
                        if (showlog_internal) Debug.Log(string.Format("【TimerTrigger（容错）】:时间设置过短【passed:set=>{0}:{1}】,事件提前触发！", timePassed, endTime));
                    }
                    Duration = endTime;
                }
            }
            return this;
        }
        /// <summary>
        /// 设置运行中的定时器的loop状态
        /// </summary>
        /// <param name="loop"></param>
        public Timer Setloop(bool loop)
        {
            if (!_isFinish)
            {
                _loop = loop;
            }
            else
            {
                if (showlog_internal) Debug.Log("【TimerTrigger（容错）】:定时器已失效,设置Loop Fail！");
            }
            return this;
        }

        /// <summary>
        /// 设置运行中的定时器的ignoreTimescale状态
        /// </summary>
        /// <param name="loop"></param>
        public Timer SetIgnoreTimeScale(bool ignoreTimescale)
        {
            if (!_isFinish)
            {
                _ignoreTimescale = ignoreTimescale;
            }
            else
            {
                if (showlog_internal) Debug.Log("【TimerTrigger（容错）】:定时器已失效，设置IgnoreTimescale Fail！");
            }
            return this;
        }
        #endregion
    }

    public class TimerDriver : MonoBehaviour
    {
        #region 单例
        private static TimerDriver _instance;
        private static bool hasInit = false;
        private void Awake()
        {
            _instance = this;
            hasInit = true;
        }
        #endregion
        private float _rerealtimeSinceStartup, _time;
        /// <summary>
        /// 获得当前时间
        /// </summary>
        /// <param name="_ignoreTimescale">  是否忽略Timescale</param>
        /// <returns></returns>
        public float GetCurrentTime(bool _ignoreTimescale)
        {
            return _ignoreTimescale ? _rerealtimeSinceStartup : _time;
        }
        /// <summary>
        /// 初始化计时器驱动
        /// </summary>
        /// <returns>驱动的实例</returns>
        public static TimerDriver Initialize()
        {
            if (!Application.isPlaying)
                return null;
            if (!hasInit)
            {
                hasInit = true;
                _instance = new GameObject("TimerEntity").AddComponent<TimerDriver>();
                DontDestroyOnLoad(_instance.gameObject);
                _instance.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }
            return _instance;
        }

        private void Update()
        {
            _rerealtimeSinceStartup = UnityEngine.Time.realtimeSinceStartup;
            _time = UnityEngine.Time.time;
            Timer.UpdateAllTimer();
        }
    }
    public static class TimerExtend
    {
        /// <summary>
        /// 当计时器计数完成时执行的事件链
        /// </summary>
        /// <param name="timer"></param>
        /// <param name="completedEvent"></param>
        /// <returns></returns>
        public static Timer OnCompleted(this Timer timer, Action completedEvent)
        {
            if (null == timer)
            {
                return null;
            }
            timer.AddEvent(completedEvent);
            return timer;
        }
        /// <summary>
        /// 当计数器计时进行中执行的事件链
        /// </summary>
        /// <param name="timer"></param>
        /// <param name="updateEvent"></param>
        /// <returns></returns>
        public static Timer OnUpdated(this Timer timer, Action<float> updateEvent)
        {
            timer?.AddEvent(updateEvent);
            return timer;
        }

        #region-------------Pause AND Resume Timer---------------
        /// <summary>
        /// 暂停用户指定的计时触发器
        /// </summary>
        /// <param name="flag">指定的标识符</param>
        public static Timer Pause(this Timer timer)
        {
            if (null != timer)
            {
                timer.IsPause = true;
            }
            else if (Timer.ShowLog)
            {
                Debug.LogError("【TimerTrigger（容错）】:定时器不存在 ，暂停失败！");
            }

            return timer;
        }
        /// <summary>
        /// 暂停用户指定的计时触发器
        /// </summary>
        /// <param name="flag">指定的标识符</param>
        public static Timer Resume(this Timer timer)
        {
            if (null != timer)
            {
                timer.IsPause = false;
            }
            else if (Timer.ShowLog)
            {
                Debug.LogWarning("【TimerTrigger（容错）】:定时器不存在 ，恢复失败！");
            }
            return timer;
        }
        #endregion
    }
}