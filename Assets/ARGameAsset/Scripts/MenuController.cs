using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ARLocation.MapboxRoutes.SampleProject
{
    // Represents a predefined waypoint with a name and geographic location.
    [System.Serializable]
    public class PredefinedWaypoint
    {
        public string Name;
        public Location Location; // Using the existing Location struct/class from ARLocation
    }

    /// <summary>
    /// Manages the main menu and route visualization functionalities in the AR application. (ASSET SCRIPT + MODIFICATION)
    /// Handles map interaction, route rendering, and waypoint management.
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        public enum LineType
        {
            Route,
            NextTarget
        }

        public string MapboxToken = "pk.eyJ1IjoiaG9hbmdtaW5oLTE1MTAiLCJhIjoiY21iMzVxZWJlMG51MzJqb2VodjVvMTJ0cCJ9.nNFaOMidYnQxXm_m1ztnkA";
        public GameObject ARSession;
        public GameObject ARSessionOrigin;
        public GameObject RouteContainer;
        public Camera Camera;
        public Camera MapboxMapCamera;
        public MapboxRoute MapboxRoute;
        public AbstractRouteRenderer RoutePathRenderer;
        public AbstractRouteRenderer NextTargetPathRenderer;
        public Texture RenderTexture;
        public Mapbox.Unity.Map.AbstractMap Map;
        [Range(100, 800)]
        public int MapSize = 400;
        public DirectionsFactory DirectionsFactory;
        public int MinimapLayer;
        public Material MinimapLineMaterial;
        public float BaseLineWidth = 2;
        public float MinimapStepSize = 0.5f;

        [Header("Minimap Positioning")]
        public int TopRightSpacing = 20; // Spacing from top and right edges in pixels use for map spacing

        private AbstractRouteRenderer currentPathRenderer => s.LineType == LineType.Route ? RoutePathRenderer : NextTargetPathRenderer;

        public LineType PathRendererType
        {
            get => s.LineType;
            set
            {
                if (value != s.LineType)
                {
                    currentPathRenderer.enabled = false;
                    s.LineType = value;
                    currentPathRenderer.enabled = true;

                    if (s.View == View.Route)
                    {
                        MapboxRoute.RoutePathRenderer = currentPathRenderer;
                    }
                }
            }
        }

        // Enum representing the current view mode of the menu
        enum View
        {
            SearchMenu,
            Route,
        }

        // Holds data relevant to the current operational state of the menu and map
        [System.Serializable]
        private class State
        {
            public string QueryText = "";
            public List<GeocodingFeature> Results = new List<GeocodingFeature>();
            public View View = View.SearchMenu;
            public Location destination;
            public LineType LineType = LineType.NextTarget;
            public string ErrorMessage;
        }

        // Internal state management for the menu controller
        private State s = new State();

        private GUIStyle _textStyle;
        GUIStyle textStyle()
        {
            if (_textStyle == null)
            {
                _textStyle = new GUIStyle(GUI.skin.label);
                _textStyle.fontSize = 48;
                _textStyle.fontStyle = FontStyle.Bold;
            }

            return _textStyle;
        }

        private GUIStyle _textFieldStyle;
        GUIStyle textFieldStyle()
        {
            if (_textFieldStyle == null)
            {
                _textFieldStyle = new GUIStyle(GUI.skin.textField);
                _textFieldStyle.fontSize = 48;
            }
            return _textFieldStyle;
        }

        private GUIStyle _errorLabelStyle;
        GUIStyle errorLabelSytle()
        {
            if (_errorLabelStyle == null)
            {
                _errorLabelStyle = new GUIStyle(GUI.skin.label);
                _errorLabelStyle.fontSize = 24;
                _errorLabelStyle.fontStyle = FontStyle.Bold;
                _errorLabelStyle.normal.textColor = Color.red;
            }

            return _errorLabelStyle;
        }

        private GUIStyle _buttonStyle;
        GUIStyle buttonStyle()
        {
            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button);
                _buttonStyle.fontSize = 48;
            }

            return _buttonStyle;
        }


        void Awake()
        {
            // MapboxMapCamera.gameObject.SetActive(false);
            // Map.SetCenterLatitudeLongitude()
        }

        void Start()
        {
            NextTargetPathRenderer.enabled = false;
            RoutePathRenderer.enabled = false;
            ARLocationProvider.Instance.OnEnabled.AddListener(onLocationEnabled);
            Map.OnUpdated += OnMapRedrawn;
        }

        //(ASSET METHOD) 
        private void OnMapRedrawn()
        {
            // Debug.Log("OnMapRedrawn");
            if (currentResponse != null)
            {
                buildMinimapRoute(currentResponse);
            }
        }

        //(ASSET METHOD) 
        private void onLocationEnabled(Location location)
        {
            Map.SetCenterLatitudeLongitude(new Mapbox.Utils.Vector2d(location.Latitude, location.Longitude));
            // Map.SetZoom(18);
            Map.UpdateMap();
        }

        // Initializes the minimap camera and sets initial view. (ASSET METHOD) 
        void OnEnable()
        {
            Debug.Log("Enable!!!!!!!!");
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        // (ASSET METHOD) 
        void OnDisable()
        {
            // ARLocationProvider.Instance.OnEnabled.RemoveListener(onLocationEnabled);
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        //(ASSET METHOD) 
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"Scene Loaded: {scene.name}");
        }

        //Draw Minimap
        void drawMap()
        {
            if (RenderTexture == null) return;

            var tw = RenderTexture.width;
            var th = RenderTexture.height;

            var scale = MapSize / (float)th;
            var newWidth = scale * tw;

            // Calculate position for top-right corne
            var x = Screen.width - newWidth - TopRightSpacing;
            var y = TopRightSpacing;


            // Draw the minimap texture
            GUI.DrawTexture(new Rect(x, y, newWidth, MapSize), RenderTexture, ScaleMode.ScaleAndCrop);

            // This line draws a separator line below the minimap
            // GUI.DrawTexture(new Rect(0, Screen.height - MapSize - 20, Screen.width, 20), separatorTexture, ScaleMode.StretchToFill, false);
        }

        //Draw Progession display
        void DrawCompletionCounter(int totalWaypoints, int completedCount)
        {
            float width = 380;
            float height = 100;

            // x = Screen width - width of label - spacing from right edge
            float x = Screen.width - width - 20;
            // y = Screen height - height of label - spacing from bottom edge
            float y = Screen.height - height - 40;

            // Create the text content
            string counterText = $"Inventory: {completedCount} / {totalWaypoints}";

            // Draw the label
            GUI.Label(new Rect(x, y, width, height), counterText, textStyle());
        }

        //Draw UI (Waypoint selection, Minimap, Progession display)
        void OnGUI()
        {
            if (s.View == View.Route)
            {
                drawMap();
                return;
            }

            // Calculate completed count (read from PlayerPrefs)
            int completedCount = 0;
            int totalWaypoints = PredefinedWaypoints.Count;
            for (int i = 0; i < totalWaypoints; i++)
            {
                string completionKey = $"WaypointCompleted_{i}";
                // PlayerPrefs.GetInt(key, defaultValue) returns defaultValue if the key doesn't exist
                if (PlayerPrefs.GetInt(completionKey, 0) == 1) // Check if the flag is 1 (completed)
                {
                    completedCount++;
                }
            }

            DrawCompletionCounter(totalWaypoints, completedCount);


            float h = Screen.height - MapSize;
            GUILayout.BeginVertical(new GUIStyle() { padding = new RectOffset(20, 20, 70, 20) }, GUILayout.MaxHeight(h), GUILayout.Height(h));


            GUILayout.Label("Select Destination", textStyle());

            // Iterate through predefined waypoints and create buttons
            foreach (var waypoint in PredefinedWaypoints)
            {
                // Use the waypoint Name for the button text
                if (GUILayout.Button(waypoint.Name, new GUIStyle(buttonStyle()) { alignment = TextAnchor.MiddleLeft, fontSize = 24, fixedHeight = 0.05f * Screen.height }))
                {
                    // Call StartRoute with the Location from the predefined waypoint
                    StartRoute(waypoint.Location);
                }
            }

            GUILayout.EndVertical();
            
            drawMap();

        }

        private Texture2D _separatorTexture;
        private Texture2D separatorTexture
        {
            get
            {
                if (_separatorTexture == null)
                {
                    _separatorTexture = new Texture2D(1, 1);
                    _separatorTexture.SetPixel(0, 0, new Color(0.15f, 0.15f, 0.15f));
                    _separatorTexture.Apply();
                }

                return _separatorTexture;
            }
        }

        //(ASSET METHOD) 
        public void StartRoute(Location dest)
        {
            s.destination = dest;

            if (ARLocationProvider.Instance.IsEnabled)
            {
                loadRoute(ARLocationProvider.Instance.CurrentLocation.ToLocation());
            }
            else
            {
                ARLocationProvider.Instance.OnEnabled.AddListener(loadRoute);
            }
        }

        //(ASSET METHOD) 
        public void EndRoute()
        {
            ARLocationProvider.Instance.OnEnabled.RemoveListener(loadRoute);
            ARSession.SetActive(true);
            ARSessionOrigin.SetActive(true);
            RouteContainer.SetActive(false);
            Camera.gameObject.SetActive(false);
            s.View = View.SearchMenu;
        }

        // Load route using defined waypoint Location
        private void loadRoute(Location _)
        {
            if (s.destination != null)
            {
                var api = new MapboxApi(MapboxToken);
                var loader = new RouteLoader(api);
                StartCoroutine(
                        loader.LoadRoute(
                            new RouteWaypoint { Type = RouteWaypointType.UserLocation },
                            new RouteWaypoint { Type = RouteWaypointType.Location, Location = s.destination },
                            (err, res) =>
                            {
                                if (err != null)
                                {
                                    s.ErrorMessage = err;
                                    s.Results = new List<GeocodingFeature>();
                                    return;
                                }

                                ARSession.SetActive(true);
                                ARSessionOrigin.SetActive(true);
                                RouteContainer.SetActive(true);
                                Camera.gameObject.SetActive(false);
                                s.View = View.Route;

                                currentPathRenderer.enabled = true;
                                MapboxRoute.RoutePathRenderer = currentPathRenderer;
                                MapboxRoute.BuildRoute(res);
                                currentResponse = res;
                                buildMinimapRoute(res);
                            }));
            }
        }

        private GameObject minimapRouteGo;
        private RouteResponse currentResponse;

        //(ASSET METHOD) 
        private void buildMinimapRoute(RouteResponse res)
        {
            var geo = res.routes[0].geometry;
            var vertices = new List<Vector3>();
            var indices = new List<int>();

            var worldPositions = new List<Vector2>();

            foreach (var p in geo.coordinates)
            {
                /* var pos = Mapbox.Unity.Utilities.Conversions.GeoToWorldPosition(
                        p.Latitude,
                        p.Longitude,
                        Map.CenterMercator,
                        Map.WorldRelativeScale
                        ); */

                // Mapbox.Unity.Utilities.Conversions.GeoToWorldPosition
                var pos = Map.GeoToWorldPosition(new Mapbox.Utils.Vector2d(p.Latitude, p.Longitude), true);
                worldPositions.Add(new Vector2(pos.x, pos.z));
                // worldPositions.Add(new Vector2((float)pos.x, (float)pos.y));
            }

            if (minimapRouteGo != null)
            {
                minimapRouteGo.Destroy();
            }

            minimapRouteGo = new GameObject("minimap route game object");
            minimapRouteGo.layer = MinimapLayer;

            var mesh = minimapRouteGo.AddComponent<MeshFilter>().mesh;

            var lineWidth = BaseLineWidth * Mathf.Pow(2.0f, Map.Zoom - 18);
            LineBuilder.BuildLineMesh(worldPositions, mesh, lineWidth);

            var meshRenderer = minimapRouteGo.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = MinimapLineMaterial;
        }

        //search route for selected waypoint
        IEnumerator search()
        {
            var api = new MapboxApi(MapboxToken);

            yield return api.QueryLocal(s.QueryText, true);

            if (api.ErrorMessage != null)
            {
                s.ErrorMessage = api.ErrorMessage;
                s.Results = new List<GeocodingFeature>();
            }
            else
            {
                s.Results = api.QueryLocalResult.features;
            }
        }

        //(ASSET METHOD)
        Vector3 lastCameraPos;
        void Update()
        {
            if (s.View == View.Route)
            {
                var cameraPos = Camera.main.transform.position;

                var arLocationRootAngle = ARLocationManager.Instance.gameObject.transform.localEulerAngles.y;
                var cameraAngle = Camera.main.transform.localEulerAngles.y;
                var mapAngle = cameraAngle - arLocationRootAngle;

                MapboxMapCamera.transform.eulerAngles = new Vector3(90, mapAngle, 0);

                if ((cameraPos - lastCameraPos).magnitude < MinimapStepSize)
                {
                    return;
                }

                lastCameraPos = cameraPos;

                var location = ARLocationManager.Instance.GetLocationForWorldPosition(Camera.main.transform.position);

                Map.SetCenterLatitudeLongitude(new Mapbox.Utils.Vector2d(location.Latitude, location.Longitude));
                Map.UpdateMap();

            }
            else
            {
                MapboxMapCamera.transform.eulerAngles = new Vector3(90, 0, 0);
            }
        }

        [Header("Predefined Waypoints")]
        [SerializeField]
        public List<PredefinedWaypoint> PredefinedWaypoints = new List<PredefinedWaypoint>();
    }
}
