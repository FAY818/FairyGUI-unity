using System.Collections.Generic;
using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    /// 
    /// </summary>
    public class EventContext
    {
        /// <summary>
        /// 事件的派发着
        /// </summary>
        public EventDispatcher sender { get; internal set; }

        /// <summary>
        /// 事件的发起对象
        /// </summary>
        public object initiator { get; internal set; }

        /// <summary>
        /// 输入事件对象
        /// </summary>
        public InputEvent inputEvent { get; internal set; }

        /// <summary>
        /// 事件类型
        /// </summary>
        public string type;

        /// <summary>
        /// 自定义参数
        /// </summary>
        public object data;

        internal bool _defaultPrevented;
        internal bool _stopsPropagation;
        internal bool _touchCapture;

        // 事件的触发链
        internal List<EventBridge> callChain = new List<EventBridge>();

        /// <summary>
        /// 停止传播
        /// </summary>
        public void StopPropagation()
        {
            _stopsPropagation = true;
        }

        /// <summary>
        /// 默认阻塞
        /// </summary>
        public void PreventDefault()
        {
            _defaultPrevented = true;
        }

        /// <summary>
        /// 捕获触控
        /// </summary>
        public void CaptureTouch()
        {
            _touchCapture = true;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool isDefaultPrevented
        {
            get { return _defaultPrevented; }
        }

        static Stack<EventContext> pool = new Stack<EventContext>();
        internal static EventContext Get()
        {
            if (pool.Count > 0)
            {
                EventContext context = pool.Pop();
                context._stopsPropagation = false;
                context._defaultPrevented = false;
                context._touchCapture = false;
                return context;
            }
            else
                return new EventContext();
        }

        internal static void Return(EventContext value)
        {
            pool.Push(value);
        }



#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitializeOnLoad()
        {
            pool.Clear();
        }
#endif
    }

}
