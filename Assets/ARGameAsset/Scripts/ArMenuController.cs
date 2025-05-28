using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace ARLocation.MapboxRoutes.SampleProject
{
    /// <summary>
    /// Represents a single linear interpolation (tween) for a Vector3 position (ASSET CLASS)
    /// Used for smooth transitions of UI elements 
    /// </summary>
    public class Tween
    {
        Vector3 start;
        Vector3 end;
        Vector3 current;
        float speed;
        float t;

        public Vector3 Position => current;

        public Tween(Vector3 startPos, Vector3 endPos, float tweenSpeed = 1)
        {
            start = startPos;
            end = endPos;
            speed = tweenSpeed;
        }

        public bool Update()
        {
            current = start * (1 - t) + end * t;

            t += Time.deltaTime * speed;

            if (t > 1)
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Stores data for tweening a RectTransform's position and rotation (ASSET CLASS)
    /// </summary>
    public class TweenRectTransform
    {
        public RectTransform Rt;
        public Vector3 PositionStart;
        public Vector3 PositionEnd;
        public Quaternion RotationStart;
        public Quaternion RotationEnd;

        public TweenRectTransform(RectTransform rectTransform, Vector3 targetPosition, Quaternion targetRotation)
        {
            Rt = rectTransform;
            PositionStart = rectTransform.position;
            RotationStart = rectTransform.rotation;
            PositionEnd = targetPosition;
            RotationEnd = targetRotation;
        }
    }

    /// <summary>
    /// Manages a group of RectTransform tweens, allowing for synchronized animations (ASSET CLASS)
    /// Includes various easing functions
    /// </summary>
    public class TweenRectTransformGroup
    {
        public enum EaseFunc
        {
            Linear,
            EaseOutBack,
            EaseInCubic,
        }

        public List<TweenRectTransform> Elements = new List<TweenRectTransform>();
        float speed;
        float t;
        Func<float, float, float, float> easeFunc;

        public TweenRectTransformGroup(float speed, EaseFunc easeFuncType)
        {
            this.speed = speed;

            switch (easeFuncType)
            {
                case EaseFunc.EaseOutBack:
                    this.easeFunc = EaseOutBack;
                    break;

                case EaseFunc.EaseInCubic:
                    this.easeFunc = EaseInCubic;
                    break;

                case EaseFunc.Linear:
                    this.easeFunc = EaseLinear;
                    break;
            }
        }

        public Vector3 ease(Vector3 start, Vector3 end, float t)
        {
            var x = easeFunc(start.x, end.x, t);
            var y = easeFunc(start.y, end.y, t);
            var z = easeFunc(start.z, end.z, t);

            return new Vector3(x, y, z);
        }

        public bool Update()
        {
            foreach (var e in Elements)
            {
                e.Rt.position = ease(e.PositionStart, e.PositionEnd, t); //e.PositionStart * (1 - t) + e.PositionEnd * t;
                e.Rt.rotation = Quaternion.Lerp(e.RotationStart, e.RotationEnd, t);
            }

            t += speed * Time.deltaTime;

            if (t > 1)
            {
                foreach (var e in Elements)
                {
                    e.Rt.position = e.PositionEnd;
                    e.Rt.rotation = e.RotationEnd;
                }

                return true;
            }

            return false;
        }

        public static float EaseOutBack(float start, float end, float value)
        {
            float s = 1.70158f;
            end -= start;
            value = (value) - 1;
            return end * ((value) * value * ((s + 1) * value + s) + 1) + start;
        }

        public static Vector3 EaseOutBack(Vector3 start, Vector3 end, float t)
        {
            float x = EaseOutBack(start.x, end.x, t);
            float y = EaseOutBack(start.y, end.y, t);
            float z = EaseOutBack(start.z, end.z, t);

            return new Vector3(x, y, z);
        }

        public static float EaseInCubic(float start, float end, float value)
        {
            end -= start;
            return end * value * value * value + start;
        }

        public static float EaseLinear(float start, float end, float value)
        {
            return start * (1 - value) * end * value;
        }

    }

    /// <summary>
    /// Manages the Augmented Reality (AR) menu, including UI visibility,
    /// button interactions, and navigation to story scenes based on Hotspot activation (ASSET CLASS + MODIFICATION)
    /// </summary>
    public class ArMenuController : MonoBehaviour
    {
        public enum StateType
        {
            Closed,
            Open,
            OpenTransition,
            CloseTransition
        }

        [System.Serializable]
        public class StateData
        {
            public StateType CurrentState = StateType.Closed;
            public TweenRectTransformGroup tweenGroup;
        }

        [System.Serializable]
        public class ElementsData
        {
            public Button BtnToggle;
            public Button BtnNext;
            public Button BtnPrev;
            public Button BtnRestart;
            public Button BtnExit;
            public Button BtnLineRender;

            public Text LabelNext;
            public Text LabelPrev;
            public Text LabelRestart;
            public Text LabelSearch;
            public Text LabelTargetRender;

            public RectTransform TargetNext;
            public RectTransform TargetPrev;
            public RectTransform TargetRestart;
            public RectTransform TargetExit;
            public RectTransform TargetLineRender;

            [Header("Story Buttons")]
            public List<GameObject> StoryButtons;
        }

        [System.Serializable]
        public class SettingsData
        {
            public MapboxRoute MapboxRoute;
            public MenuController MenuController;
            public float TransitionSpeed = 2.0f;
        }

        public SettingsData Settings;
        public ElementsData Elements;
        private StateData s = new StateData();


        // Initializes UI elements and ensures story buttons are initially hidden (ASSET METHOD)
        public void Awake()
        {
            s = new StateData();

            showOnlyToggleButton();
        }


        // Sets only the toggle button active, hiding all other menu elements (ASSET METHOD)
        void showOnlyToggleButton()
        {
            Elements.BtnExit.gameObject.SetActive(false);
            Elements.BtnLineRender.gameObject.SetActive(false);
            Elements.BtnNext.gameObject.SetActive(false);
            Elements.BtnPrev.gameObject.SetActive(false);
            Elements.BtnRestart.gameObject.SetActive(false);
            Elements.BtnToggle.gameObject.SetActive(true);
        }


        // Makes all main menu buttons visible (ASSET METHOD)
        void showAllButtons()
        {
            Elements.BtnExit.gameObject.SetActive(true);
            Elements.BtnLineRender.gameObject.SetActive(true);
            Elements.BtnNext.gameObject.SetActive(true);
            Elements.BtnPrev.gameObject.SetActive(true);
            Elements.BtnRestart.gameObject.SetActive(true);
            Elements.BtnToggle.gameObject.SetActive(true);
        }

        // Called when the object becomes active. Adds event listeners for UI buttons (ASSET METHOD)
        public void OnEnable()
        {
            Elements.BtnToggle.onClick.AddListener(OnTogglePress);
            Elements.BtnNext.onClick.AddListener(OnNextPress);
            Elements.BtnPrev.onClick.AddListener(OnPrevPress);
            Elements.BtnRestart.onClick.AddListener(OnRestartPress);
            Elements.BtnExit.onClick.AddListener(OnSearchPress);
            Elements.BtnLineRender.onClick.AddListener(OnLineRenderPress);
            updateLineRenderButtonLabel();
        }

        // Called when the object becomes disabled or inactive. (ASSET METHOD)
        public void OnDisable()
        {
            Elements.BtnToggle.onClick.RemoveListener(OnTogglePress);
            Elements.BtnNext.onClick.RemoveListener(OnNextPress);
            Elements.BtnPrev.onClick.RemoveListener(OnPrevPress);
            Elements.BtnRestart.onClick.RemoveListener(OnRestartPress);
            Elements.BtnExit.onClick.RemoveListener(OnSearchPress);
            Elements.BtnLineRender.onClick.RemoveListener(OnSearchPress);
        }

        // Updates the text label for the line render button. (ASSET METHOD)
        private void updateLineRenderButtonLabel()
        {
            var mc = Settings.MenuController;

            if (mc.PathRendererType == MenuController.LineType.Route)
            {
                Elements.LabelTargetRender.text = "Route Path";
            }
            else
            {
                Elements.LabelTargetRender.text = "Line To Target";
            }

        }

        // (ASSET METHOD)
        private void OnLineRenderPress()
        {
            var mc = Settings.MenuController;

            if (mc.PathRendererType == MenuController.LineType.Route)
            {
                mc.PathRendererType = MenuController.LineType.NextTarget;
                Elements.LabelTargetRender.text = "Line To Target";
            }
            else
            {
                mc.PathRendererType = MenuController.LineType.Route;
                Elements.LabelTargetRender.text = "Route Path";
            }
        }

        // (ASSET METHOD)
        private void OnSearchPress()
        {
            Settings.MenuController.EndRoute();
        }

        // (ASSET METHOD)
        private void OnRestartPress()
        {
            Settings.MapboxRoute.ClosestTarget();
        }

        // (ASSET METHOD)
        private void OnPrevPress()
        {
            Settings.MapboxRoute.PrevTarget();
        }

        // (ASSET METHOD)
        private void OnNextPress()
        {
            Settings.MapboxRoute.NextTarget();
        }

        // (ASSET METHOD)
        private void OnTogglePress()
        {
            toggleMenu();
        }

        // (ASSET METHOD)
        void toggleMenu()
        {
            if (s.CurrentState == StateType.Closed)
            {
                openMenu();
            }
            else if (s.CurrentState == StateType.Open)
            {
                closeMenu();
            }
        }

        // (ASSET METHOD)
        void openMenu()
        {
            switch (s.CurrentState)
            {
                case StateType.Closed:

                    showAllButtons();

                    s.tweenGroup = new TweenRectTransformGroup(Settings.TransitionSpeed, TweenRectTransformGroup.EaseFunc.EaseInCubic);

                    s.tweenGroup.Elements.Add(new TweenRectTransform(Elements.BtnNext.GetComponent<RectTransform>(), Elements.TargetNext.position, Quaternion.identity));
                    s.tweenGroup.Elements.Add(new TweenRectTransform(Elements.BtnPrev.GetComponent<RectTransform>(), Elements.TargetPrev.position, Quaternion.identity));
                    s.tweenGroup.Elements.Add(new TweenRectTransform(Elements.BtnRestart.GetComponent<RectTransform>(), Elements.TargetRestart.position, Quaternion.identity));
                    s.tweenGroup.Elements.Add(new TweenRectTransform(Elements.BtnExit.GetComponent<RectTransform>(), Elements.TargetExit.position, Quaternion.identity));
                    s.tweenGroup.Elements.Add(new TweenRectTransform(Elements.BtnLineRender.GetComponent<RectTransform>(), Elements.TargetLineRender.position, Quaternion.identity));

                    s.tweenGroup.Elements.Add(new TweenRectTransform(Elements.BtnToggle.GetComponent<RectTransform>(), Elements.BtnToggle.GetComponent<RectTransform>().position, Quaternion.Euler(0, 0, 180)));

                    //s.BtnNextTween = new Tween(Elements.BtnNext.GetComponent<RectTransform>().position, Elements.TargetNext.position, Settings.TransitionSpeed);
                    //s.BtnPrevTween = new Tween(Elements.BtnPrev.GetComponent<RectTransform>().position, Elements.TargetPrev.position, Settings.TransitionSpeed);
                    //s.BtnRestartTween = new Tween(Elements.BtnRestart.GetComponent<RectTransform>().position, Elements.TargetRestart.position, Settings.TransitionSpeed);
                    //s.BtnExitTween = new Tween(Elements.BtnExit.GetComponent<RectTransform>().position, Elements.TargetExit.position, Settings.TransitionSpeed);

                    Elements.BtnToggle.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, 180);

                    s.CurrentState = StateType.OpenTransition;

                    break;
            }
        }

        // (ASSET METHOD)
        void closeMenu()
        {
            switch (s.CurrentState)
            {
                case StateType.Open:

                    s.tweenGroup = new TweenRectTransformGroup(Settings.TransitionSpeed, TweenRectTransformGroup.EaseFunc.EaseInCubic);

                    var togglerRt = Elements.BtnToggle.GetComponent<RectTransform>();

                    s.tweenGroup.Elements.Add(new TweenRectTransform(Elements.BtnNext.GetComponent<RectTransform>(), togglerRt.position, Quaternion.identity));
                    s.tweenGroup.Elements.Add(new TweenRectTransform(Elements.BtnPrev.GetComponent<RectTransform>(), togglerRt.position, Quaternion.identity));
                    s.tweenGroup.Elements.Add(new TweenRectTransform(Elements.BtnRestart.GetComponent<RectTransform>(), togglerRt.position, Quaternion.identity));
                    s.tweenGroup.Elements.Add(new TweenRectTransform(Elements.BtnExit.GetComponent<RectTransform>(), togglerRt.position, Quaternion.identity));
                    s.tweenGroup.Elements.Add(new TweenRectTransform(Elements.BtnLineRender.GetComponent<RectTransform>(), togglerRt.position, Quaternion.identity));

                    s.tweenGroup.Elements.Add(new TweenRectTransform(Elements.BtnToggle.GetComponent<RectTransform>(), Elements.BtnToggle.GetComponent<RectTransform>().position, Quaternion.Euler(0, 0, 0)));

                    //Elements.BtnToggle.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, 0);

                    s.CurrentState = StateType.CloseTransition;

                    //showOnlyToggleButton();
                    //var pos = Elements.BtnToggle.GetComponent<RectTransform>().position;
                    //Elements.BtnNext.GetComponent<RectTransform>().position = pos;
                    //Elements.BtnPrev.GetComponent<RectTransform>().position = pos;
                    //Elements.BtnRestart.GetComponent<RectTransform>().position = pos;
                    //Elements.BtnExit.GetComponent<RectTransform>().position = pos;
                    //Elements.BtnToggle.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, 0);
                    //s.CurrentState = StateType.Closed;
                    break;
            }
        }

        // (ASSET METHOD)
        void Start()
        {

        }

        // (ASSET METHOD)
        void showButtonLabels()
        {
            Elements.LabelNext.gameObject.SetActive(true);
            Elements.LabelPrev.gameObject.SetActive(true);
            Elements.LabelRestart.gameObject.SetActive(true);
            Elements.LabelSearch.gameObject.SetActive(true);
            Elements.LabelTargetRender.gameObject.SetActive(true);
        }

        // (ASSET METHOD)
        void hideButtonLabels()
        {
            Elements.LabelNext.gameObject.SetActive(false);
            Elements.LabelPrev.gameObject.SetActive(false);
            Elements.LabelRestart.gameObject.SetActive(false);
            Elements.LabelTargetRender.gameObject.SetActive(false);

        }

        // Show or hide a specific story button based on its waypoint ID. Called by Hotspot events.
        public void SetStoryButtonVisibility(int waypointId, bool show)
        {
            //Debug.Log($"ArMenuController: SetStoryButtonVisibility called for Waypoint ID {waypointId} with show = {show}");

            // Check if the waypoint ID is within the bounds of the StoryButtons list.
            if (waypointId >= 0 && waypointId < Elements.StoryButtons.Count)
            {
                GameObject buttonGo = Elements.StoryButtons[waypointId];
                if (buttonGo != null)
                {
                    buttonGo.SetActive(show);
                    //Debug.Log($"ArMenuController: Button for Waypoint ID {waypointId} active state set to: {buttonGo.activeSelf}");
                }
                else
                {
                    // Log an error if the button reference is missing in the Inspector.
                    Debug.LogError($"ArMenuController: Button GameObject for Waypoint ID {waypointId} is null in Elements.StoryButtons list!");
                }
            }
            else
            {
                // Log a warning if the waypoint ID is invalid for the current list size.
                Debug.LogWarning($"ArMenuController: Waypoint ID {waypointId} is out of bounds for StoryButtons list (size {Elements.StoryButtons.Count}). Cannot set visibility.");
            }
        }

        // Making the associated story button visible.
        public void HandleHotspotActivated(int hotspotId)
        {
            //Debug.Log($"ArMenuController: Hotspot Activated event received for ID: {hotspotId}");
            SetStoryButtonVisibility(hotspotId, true);
        }

        // Making the associated story button invisible.
        public void HandleHotspotDeactivated(int hotspotId)
        {
            //Debug.Log($"ArMenuController: Hotspot Deactivated event received for ID: {hotspotId}");
            SetStoryButtonVisibility(hotspotId, false);
        }

        // Saves the active waypoint ID and loads the corresponding story scene.
        public void OnWaypointButtonPressed(int waypointId)
        {
            //Debug.Log($"ArMenuController: Waypoint button pressed for ID: {waypointId}.");

            // Save the active waypoint ID using PlayerPrefs for progress tracking.
            PlayerPrefs.SetInt("ActiveWaypointID", waypointId);
            PlayerPrefs.Save(); // Ensure the data is written to disk immediately
            //Debug.Log($"Saved ActiveWaypointID {waypointId} to PlayerPrefs.");

            // Determine which story scene to load based on the waypoint ID
            string storySceneToLoad = "";

            if (waypointId == 0)
            {
                storySceneToLoad = "LM1Scene";
            }
            else if (waypointId == 1)
            {
                storySceneToLoad = "LM2Scene";
            }


            if (!string.IsNullOrEmpty(storySceneToLoad))
            {
                //Debug.Log($"Loading story scene: {storySceneToLoad}");
                SceneManager.LoadScene(storySceneToLoad);
            }
            else
            {
                //Debug.LogWarning($"No story scene defined for waypoint ID: {waypointId}. Loading main menu scene as fallback.");
                SceneManager.LoadScene("Mapbox Route"); // Replace with your main AR scene name
            }

            // Hide all story buttons after any button is pressed
            HideAllStoryButtons();
        }

        // Hides all story UI buttons.
        private void HideAllStoryButtons()
        {
            //Debug.Log("ArMenuController: Hiding all story buttons.");
            // Iterate directly over the Elements.StoryButtons list
            foreach (var buttonGo in Elements.StoryButtons)
            {
                if (buttonGo != null)
                {
                    buttonGo.SetActive(false);
                }
            }
        }

        // Update dynamic menu transitions (ASSET METHOD)
        void Update()
        {
            switch (s.CurrentState)
            {
                case StateType.OpenTransition:
                    if (s.tweenGroup.Update())
                    {
                        showButtonLabels();
                        s.CurrentState = StateType.Open;
                    }

                    break;

                case StateType.CloseTransition:
                    if (s.tweenGroup.Update())
                    {
                        hideButtonLabels();
                        showOnlyToggleButton();
                        s.CurrentState = StateType.Closed;
                    }
                    break;
            }
        }
    }
}
