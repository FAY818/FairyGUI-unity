using System;
using System.Collections.Generic;

namespace FairyGUI
{
    public delegate void EventCallback0();
    public delegate void EventCallback1(EventContext context);

    /// <summary>
    /// 事件派发者
    /// </summary>
    public class EventDispatcher : IEventDispatcher
    {
        Dictionary<string, EventBridge> _dic;

        public EventDispatcher()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strType"></param>
        /// <param name="callback"></param>
        public void AddEventListener(string strType, EventCallback1 callback)
        {
            GetBridge(strType).Add(callback);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strType"></param>
        /// <param name="callback"></param>
        public void AddEventListener(string strType, EventCallback0 callback)
        {
            GetBridge(strType).Add(callback);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strType"></param>
        /// <param name="callback"></param>
        public void RemoveEventListener(string strType, EventCallback1 callback)
        {
            if (_dic == null)
                return;

            EventBridge bridge = null;
            if (_dic.TryGetValue(strType, out bridge))
                bridge.Remove(callback);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strType"></param>
        /// <param name="callback"></param>
        public void RemoveEventListener(string strType, EventCallback0 callback)
        {
            if (_dic == null)
                return;

            EventBridge bridge = null;
            if (_dic.TryGetValue(strType, out bridge))
                bridge.Remove(callback);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strType"></param>
        /// <param name="callback"></param>
        public void AddCapture(string strType, EventCallback1 callback)
        {
            GetBridge(strType).AddCapture(callback);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strType"></param>
        /// <param name="callback"></param>
        public void RemoveCapture(string strType, EventCallback1 callback)
        {
            if (_dic == null)
                return;

            EventBridge bridge = null;
            if (_dic.TryGetValue(strType, out bridge))
                bridge.RemoveCapture(callback);
        }

        /// <summary>
        /// 
        /// </summary>
        public void RemoveEventListeners()
        {
            RemoveEventListeners(null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strType"></param>
        public void RemoveEventListeners(string strType)
        {
            if (_dic == null)
                return;

            if (strType != null)
            {
                EventBridge bridge;
                if (_dic.TryGetValue(strType, out bridge))
                    bridge.Clear();
            }
            else
            {
                foreach (KeyValuePair<string, EventBridge> kv in _dic)
                    kv.Value.Clear();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strType"></param>
        /// <returns></returns>
        public bool hasEventListeners(string strType)
        {
            EventBridge bridge = TryGetEventBridge(strType);
            if (bridge == null)
                return false;

            return !bridge.isEmpty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strType"></param>
        /// <returns></returns>
        public bool isDispatching(string strType)
        {
            EventBridge bridge = TryGetEventBridge(strType);
            if (bridge == null)
                return false;

            return bridge._dispatching;
        }

        internal EventBridge TryGetEventBridge(string strType)
        {
            if (_dic == null)
                return null;

            EventBridge bridge = null;
            _dic.TryGetValue(strType, out bridge);
            return bridge;
        }

        internal EventBridge GetEventBridge(string strType)
        {
            if (_dic == null)
                _dic = new Dictionary<string, EventBridge>();

            EventBridge bridge = null;
            if (!_dic.TryGetValue(strType, out bridge))
            {
                bridge = new EventBridge(this);
                _dic[strType] = bridge;
            }
            return bridge;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strType"></param>
        /// <returns></returns>
        public bool DispatchEvent(string strType)
        {
            return DispatchEvent(strType, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strType"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool DispatchEvent(string strType, object data)
        {
            return InternalDispatchEvent(strType, null, data, null);
        }

        public bool DispatchEvent(string strType, object data, object initiator)
        {
            return InternalDispatchEvent(strType, null, data, initiator);
        }

        static InputEvent sCurrentInputEvent = new InputEvent();

        /// <summary>
        /// 普通派发事件
        /// </summary>
        /// <param name="strType"></param>
        /// <param name="bridge"></param>
        /// <param name="data"></param>
        /// <param name="initiator"></param>
        /// <returns></returns>
        internal bool InternalDispatchEvent(string strType, EventBridge bridge, object data, object initiator)
        {
            if (bridge == null)
                bridge = TryGetEventBridge(strType);

            EventBridge gBridge = null;
            if ((this is DisplayObject) && ((DisplayObject)this).gOwner != null)
                gBridge = ((DisplayObject)this).gOwner.TryGetEventBridge(strType);

            bool b1 = bridge != null && !bridge.isEmpty; // 检测自身的事件桥接器不为空
            bool b2 = gBridge != null && !gBridge.isEmpty; // 检测父级的事件桥接器不为空
            if (b1 || b2)
            {
                // 事件上下文参数构造
                EventContext context = EventContext.Get(); // 从池中取出对象；
                context.initiator = initiator != null ? initiator : this; // 不指定发起者就默认是当前对象
                context.type = strType;
                context.data = data;
                if (data is InputEvent)
                    sCurrentInputEvent = (InputEvent)data;
                context.inputEvent = sCurrentInputEvent;

                if (b1)
                {
                    bridge.CallCaptureInternal(context);
                    bridge.CallInternal(context);
                }

                if (b2)
                {
                    gBridge.CallCaptureInternal(context);
                    gBridge.CallInternal(context);
                }

                EventContext.Return(context); // 归还
                context.initiator = null;
                context.sender = null;
                context.data = null;

                return context._defaultPrevented;
            }
            else
                return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public bool DispatchEvent(EventContext context)
        {
            EventBridge bridge = TryGetEventBridge(context.type);
            EventBridge gBridge = null;
            if ((this is DisplayObject) && ((DisplayObject)this).gOwner != null)
                gBridge = ((DisplayObject)this).gOwner.TryGetEventBridge(context.type);

            EventDispatcher savedSender = context.sender;

            if (bridge != null && !bridge.isEmpty)
            {
                bridge.CallCaptureInternal(context);
                bridge.CallInternal(context);
            }

            if (gBridge != null && !gBridge.isEmpty)
            {
                gBridge.CallCaptureInternal(context);
                gBridge.CallInternal(context);
            }

            context.sender = savedSender;
            return context._defaultPrevented;
        }

        /// <summary>
        /// 冒泡，向上派发事件，中午可以停止传播
        /// </summary>
        /// <param name="strType"></param>
        /// <param name="data"></param>
        /// <param name="addChain">需要额外触发的List<EventBridge></param>
        /// <returns></returns>
        internal bool BubbleEvent(string strType, object data, List<EventBridge> addChain)
        {
            // 事件上下文对象的构造
            EventContext context = EventContext.Get();
            context.initiator = this;
            context.type = strType;
            context.data = data;
            if (data is InputEvent)
                sCurrentInputEvent = (InputEvent)data;
            context.inputEvent = sCurrentInputEvent;
            List<EventBridge> bubbleChain = context.callChain;
            bubbleChain.Clear();

            GetChainBridges(strType, bubbleChain, true);

            // 链式触发捕获事件
            int length = bubbleChain.Count;
            for (int i = length - 1; i >= 0; i--) // 冒泡反向遍历
            {
                bubbleChain[i].CallCaptureInternal(context); // 触发捕获事件
                if (context._touchCapture)
                {
                    // 标记捕获touch事件
                    context._touchCapture = false;
                    if (strType == "onTouchBegin")
                        Stage.inst.AddTouchMonitor(context.inputEvent.touchId, bubbleChain[i].owner);
                }
            }

            if (!context._stopsPropagation)
            {
                for (int i = 0; i < length; ++i)
                {
                    bubbleChain[i].CallInternal(context); // 触发响应事件

                    if (context._touchCapture)
                    {
                        context._touchCapture = false;
                        if (strType == "onTouchBegin")
                            Stage.inst.AddTouchMonitor(context.inputEvent.touchId, bubbleChain[i].owner);
                    }

                    if (context._stopsPropagation)
                        break;
                }

                // 额外的触发事件桥接器列表
                if (addChain != null)
                {
                    length = addChain.Count;
                    for (int i = 0; i < length; ++i)
                    {
                        EventBridge bridge = addChain[i];
                        if (bubbleChain.IndexOf(bridge) == -1)
                        {
                            bridge.CallCaptureInternal(context);
                            bridge.CallInternal(context);
                        }
                    }
                }
            }

            EventContext.Return(context);
            context.initiator = null;
            context.sender = null;
            context.data = null;
            return context._defaultPrevented;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strType"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool BubbleEvent(string strType, object data)
        {
            return BubbleEvent(strType, data, null);
        }

        /// <summary>
        /// 广播，派发事件给所在容器树中的每一个根和叶子
        /// </summary>
        /// <param name="strType"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool BroadcastEvent(string strType, object data)
        {
            // 参数构造
            EventContext context = EventContext.Get();
            context.initiator = this;
            context.type = strType;
            context.data = data;
            if (data is InputEvent)
                sCurrentInputEvent = (InputEvent)data;
            context.inputEvent = sCurrentInputEvent;
            List<EventBridge> bubbleChain = context.callChain;
            bubbleChain.Clear();

            // 递归获取所有子节点事件桥接器列表
            if (this is Container)
                GetChildEventBridges(strType, (Container)this, bubbleChain);
            else if (this is GComponent)
                GetChildEventBridges(strType, (GComponent)this, bubbleChain);

            int length = bubbleChain.Count;
            for (int i = 0; i < length; ++i)
                bubbleChain[i].CallInternal(context);

            EventContext.Return(context);
            context.initiator = null;
            context.sender = null;
            context.data = null;
            return context._defaultPrevented;
        }

        /// <summary>
        /// 从EventBridge字典中获取对应事件类型的EventBridge，如果没有则new一个EventBridge对象并加入字典
        /// </summary>
        /// <param name="strType">事件类型</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        EventBridge GetBridge(string strType)
        {
            if (strType == null)
                throw new Exception("event type cant be null");

            if (_dic == null)
                _dic = new Dictionary<string, EventBridge>();

            EventBridge bridge = null;
            if (!_dic.TryGetValue(strType, out bridge))
            {
                bridge = new EventBridge(this);
                _dic[strType] = bridge;
            }

            return bridge;
        }

        /// <summary>
        /// 获取指定Container以及所有子节点的事件桥接器列表
        /// </summary>
        /// <param name="strType"></param>
        /// <param name="container"></param>
        /// <param name="bridges"></param>
        static void GetChildEventBridges(string strType, Container container, List<EventBridge> bridges)
        {
            EventBridge bridge = container.TryGetEventBridge(strType);
            if (bridge != null)
                bridges.Add(bridge); // 自身
            if (container.gOwner != null)
            {
                bridge = container.gOwner.TryGetEventBridge(strType);
                if (bridge != null && !bridge.isEmpty)
                    bridges.Add(bridge); // 父级
            }

            int count = container.numChildren;
            for (int i = 0; i < count; ++i)
            {
                DisplayObject obj = container.GetChildAt(i);
                if (obj is Container)
                    GetChildEventBridges(strType, (Container)obj, bridges); // 递归查找添加同级
                else
                {
                    bridge = obj.TryGetEventBridge(strType);
                    if (bridge != null && !bridge.isEmpty)
                        bridges.Add(bridge);

                    if (obj.gOwner != null)
                    {
                        bridge = obj.gOwner.TryGetEventBridge(strType);
                        if (bridge != null && !bridge.isEmpty)
                            bridges.Add(bridge);
                    }
                }
            }
        }

        /// <summary>
        /// 获取指定GComponent以及所有子节点的事件桥接器列表
        /// </summary>
        /// <param name="strType"></param>
        /// <param name="container"></param>
        /// <param name="bridges"></param>
        static void GetChildEventBridges(string strType, GComponent container, List<EventBridge> bridges)
        {
            EventBridge bridge = container.TryGetEventBridge(strType);
            if (bridge != null)
                bridges.Add(bridge);

            int count = container.numChildren;
            for (int i = 0; i < count; ++i)
            {
                GObject obj = container.GetChildAt(i);
                if (obj is GComponent)
                    GetChildEventBridges(strType, (GComponent)obj, bridges);
                else
                {
                    bridge = obj.TryGetEventBridge(strType);
                    if (bridge != null)
                        bridges.Add(bridge);
                }
            }
        }

        /// <summary>
        /// 冒泡获取事件桥接器列表（事件链）
        /// </summary>
        /// <param name="strType"></param>
        /// <param name="chain"></param>
        /// <param name="bubble"></param>
        internal void GetChainBridges(string strType, List<EventBridge> chain, bool bubble)
        {
            EventBridge bridge = TryGetEventBridge(strType);
            if (bridge != null && !bridge.isEmpty)
                chain.Add(bridge); // 自身的事件桥接器添加

            if ((this is DisplayObject) && ((DisplayObject)this).gOwner != null)
            {
                bridge = ((DisplayObject)this).gOwner.TryGetEventBridge(strType);
                if (bridge != null && !bridge.isEmpty)
                    chain.Add(bridge); // 父级的事件桥接器添加
            }

            if (!bubble)
                return;

            // 以下是冒泡🫧传递的逻辑
            
            if (this is DisplayObject)
            {
                DisplayObject element = (DisplayObject)this;
                while ((element = element.parent) != null) // todo：学习循环的写法
                {
                    bridge = element.TryGetEventBridge(strType);
                    if (bridge != null && !bridge.isEmpty)
                        chain.Add(bridge); // 循环♻️遍历添加父容器的事件桥接器

                    if (element.gOwner != null)
                    {
                        bridge = element.gOwner.TryGetEventBridge(strType);
                        if (bridge != null && !bridge.isEmpty)
                            chain.Add(bridge); // 循环♻️遍历添加父对象的事件桥接器
                    }
                }
            }
            else if (this is GObject)
            {
                GObject element = (GObject)this;
                while ((element = element.parent) != null)
                {
                    bridge = element.TryGetEventBridge(strType);
                    if (bridge != null && !bridge.isEmpty)
                        chain.Add(bridge); // 循环♻️遍历添加父对象的事件桥接器
                }
            }
        }
    }
}
