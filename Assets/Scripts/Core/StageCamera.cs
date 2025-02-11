using System;
using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    /// Stage Camera is an orthographic camera for UI rendering.
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu("FairyGUI/UI Camera")]
    public class StageCamera : MonoBehaviour
    {
        /// <summary>
        /// 是否是固定的相机size，勾选 constantSize 后，相机会确保 UI 元素在不同分辨率下保持相同的物理尺寸
        /// </summary>
        public bool constantSize = true;

        /// <summary>
        /// 每像素对应的单位数，主要用于控制 UI 的分辨率适应性。
        /// </summary>
        [NonSerialized]
        public float unitsPerPixel = 0.02f;
        
        [NonSerialized]
        public Transform cachedTransform;
        [NonSerialized]
        public Camera cachedCamera;

        [NonSerialized]
        int screenWidth;
        [NonSerialized]
        int screenHeight;
        [NonSerialized]
        bool isMain; // 当前的cachedCamera是否为主相机
        [NonSerialized]
        Display _display; // 相机的渲染目标

        /// <summary>
        /// UI主相机
        /// </summary>
        [NonSerialized]
        public static Camera main;

        /// <summary>
        /// 屏幕尺寸的变化计数
        /// </summary>
        [NonSerialized]
        public static int screenSizeVer = 1;
        
        public const string Name = "Stage Camera";
        /// <summary>
        /// Stage Camera的渲染层
        /// </summary>
        public const string LayerName = "UI";

        /// <summary>
        /// 默认的相机适口size
        /// </summary>
        public static float DefaultCameraSize = 6;
        /// <summary>
        /// 默认单位像素比
        /// </summary>
        public static float DefaultUnitsPerPixel = 0.02f;

#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            main = null;
        }
#endif

        void OnEnable()
        {
            // 主相机设置
            cachedTransform = this.transform;
            cachedCamera = this.GetComponent<Camera>();
            if (this.gameObject.name == Name)
            {
                main = cachedCamera;
                isMain = true;
            }

            if (Display.displays.Length > 1 && cachedCamera.targetDisplay != 0 && cachedCamera.targetDisplay < Display.displays.Length)
                _display = Display.displays[cachedCamera.targetDisplay];

            if (_display == null)
                OnScreenSizeChanged(Screen.width, Screen.height);
            else
                OnScreenSizeChanged(_display.renderingWidth, _display.renderingHeight);
        }

        void Update()
        {
            if (_display == null)
            {
                if (screenWidth != Screen.width || screenHeight != Screen.height)
                    OnScreenSizeChanged(Screen.width, Screen.height);
            }
            else
            {
                if (screenWidth != _display.renderingWidth || screenHeight != _display.renderingHeight)
                    OnScreenSizeChanged(_display.renderingWidth, _display.renderingHeight);
            }
        }

        /// <summary>
        /// 屏幕尺寸变化响应函数
        /// </summary>
        /// <param name="newWidth"></param>
        /// <param name="newHeight"></param>
        void OnScreenSizeChanged(int newWidth, int newHeight)
        {
            if (newWidth == 0 || newHeight == 0)
                return;

            screenWidth = newWidth;
            screenHeight = newHeight;
            
            if (constantSize)
            {
                // 根据默认的相机size，推算单位像素比
                cachedCamera.orthographicSize = DefaultCameraSize;
                unitsPerPixel = cachedCamera.orthographicSize * 2 / screenHeight;
            }
            else
            {
                // 根据默认的单位像素比，推算相机size
                unitsPerPixel = DefaultUnitsPerPixel; 
                cachedCamera.orthographicSize = screenHeight / 2 * unitsPerPixel;
            }
            cachedTransform.localPosition = new Vector3(cachedCamera.orthographicSize * screenWidth / screenHeight, -cachedCamera.orthographicSize);

            if (isMain)
            {
                screenSizeVer++;
                if (Application.isPlaying)
                    Stage.inst.HandleScreenSizeChanged(screenWidth, screenHeight, unitsPerPixel);
                else
                {
#if UNITY_2022_2_OR_NEWER
                    UIContentScaler scaler = GameObject.FindFirstObjectByType<UIContentScaler>();
#else
                    UIContentScaler scaler = GameObject.FindObjectOfType<UIContentScaler>();
#endif
                    if (scaler != null)
                        scaler.ApplyChange();
                    else
                        UIContentScaler.scaleFactor = 1;
                }
            }
        }

        void OnRenderObject()
        {
            //Update和OnGUI在EditMode的调用都不那么及时，OnRenderObject则比较频繁，可以保证界面及时刷新。所以使用OnRenderObject
            if (isMain && !Application.isPlaying)
            {
                EMRenderSupport.Update();
            }
        }

        public void ApplyModifiedProperties()
        {
            screenWidth = 0; //force OnScreenSizeChanged called in next update
        }

        /// <summary>
        /// Check if there is a stage camera in the scene. If none, create one.
        /// </summary>
        public static void CheckMainCamera()
        {
            if (GameObject.Find(Name) == null)
            {
                int layer = LayerMask.NameToLayer(LayerName);
                CreateCamera(Name, 1 << layer);
            }

            HitTestContext.cachedMainCamera = Camera.main; // 点击测试的依赖相机
        }

        /// <summary>
        /// 
        /// </summary>
        public static void CheckCaptureCamera()
        {
            if (GameObject.Find(Name) == null)
            {
                int layer = LayerMask.NameToLayer(LayerName);
                CreateCamera(Name, 1 << layer);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cullingMask"></param>
        /// <returns></returns>
        public static Camera CreateCamera(string name, int cullingMask)
        {
            GameObject cameraObject = new GameObject(name);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.depth = 1;
            camera.cullingMask = cullingMask;
            camera.clearFlags = CameraClearFlags.Depth;
            camera.orthographic = true;
            camera.orthographicSize = DefaultCameraSize;
            camera.nearClipPlane = -30;
            camera.farClipPlane = 30;
            camera.stereoTargetEye = StereoTargetEyeMask.None;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            cameraObject.AddComponent<StageCamera>();
            return camera;
        }
    }
}
