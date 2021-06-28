using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Assimp;
using HelixToolkit.Wpf.SharpDX.Controls;
using HelixToolkit.Wpf.SharpDX.Model;
using HelixToolkit.Wpf.SharpDX.Model.Scene;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

namespace OATSimulation.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        CultureInfo _oatCulture = new CultureInfo("en-US");

        private Communication.TCPSimClient _tcpClient;
        private bool _isConnected = false;
        private string _isConnectedString = "Connect";

        // public ObservableDictonary<string, string> OATData { get; set; }

        private SynchronizationContext context = SynchronizationContext.Current;
        private HelixToolkitScene scene = null;
        private CompositionTargetEx compositeHelper = new CompositionTargetEx();
        private MainWindow mainWindow = null;

        // RA Transform
        Transform3DGroup _raTransformGrp = new Transform3DGroup();
        RotateTransform3D _raRT = new RotateTransform3D();
        AxisAngleRotation3D _raAR = new AxisAngleRotation3D();

        // DEC Transform
        private Transform3DGroup _decTransformGrp = new Transform3DGroup();
        private RotateTransform3D _decRT = new RotateTransform3D();
        private AxisAngleRotation3D _decAR = new AxisAngleRotation3D();

        // OAT Main Groups
        private GroupNode _raOffsetGrp { get; set; }
        private TranslateTransform3D _raOffsetGroupTranslate { get; set; }
        private Transform3DGroup _raOffsetGroupTransform { get; set; }

        public int[] MountAngles { get; } = new int[] { 20, 30, 40, 50};
        private int _mountAngle = 50;
        public int SelectMountAngle
        {
            get
            {
                return _mountAngle;
            }

            set
            {
                _mountAngle = value;
                SetMountAngle(value);
                OnPropertyChanged("SelectMountAngle");
            }
        }

        private GroupNode _raAngleGrp { get; set; }
        private Transform3DGroup _raAngleGroupTransform = new Transform3DGroup();
        private AxisAngleRotation3D _raAngleGroupRotateAxis = new AxisAngleRotation3D();
        private RotateTransform3D _raAngleGroupRotateTransform = new RotateTransform3D();
        
        private GroupNode RAGroup { get; set; }
        private GroupNode DECGroup { get; set; }
        private GroupNode Deg20Group { get; set; }
        private GroupNode Deg30Group { get; set; }
        private GroupNode Deg40Group { get; set; }
        private GroupNode Deg50Group { get; set; }

        private bool isLoading = false;
        private string _status = "Not connected...";
        private string _version = "0.0";
        private string _firmwareVersion = "0.0";

        private double _raAngle = 0.0;
        private double _decAngle = 0.0;
        private string _currentRAString = "--";
        private string _currentDECString = "--";
        private int _raStepper = 0;
        private int _decStepper = 0;
        private int _trkStepper = 0;
        private float _raStepsPerDegree = 0.0f;
        private float _decStepsPerDegree = 0.0f;
        private int _scopeRASlewMS = 0;
        private int _scopeRATrackMS = 0;
        private int _scopeDECSlewMS = 0;
        private int _scopeDECGuideMS = 0;
        private string _scopeLongitude = "-";
        private string _scopeLatitude = "-";

        private string _scopeSiderealTime = "0";
        private string _scopePolarisHourAngle = "0";

        // Lights
        // private bool _darkPresetActive = false;
        // private bool _lightPresetActive = true;

        // Animation
        // DispatcherTimer _animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33.3) };

        private double _raTargetAngle = 0.0;
        private double _decTargetAngle = 0.0;
        private double _raTargetDegSteps = 0.0;
        private double _decTargetDegSteps = 0.0;

        private bool renderEnvironmentMap = true;

        #region Properties
        // LIGHTS -------------------------------------------------------
        private int _lightPreset = 2;

        // - Dark Preset 
        public System.Windows.Media.Color DarkAmbientLightColor { get; set; }

        // -- Animated
        public Vector3D AnimatedLightDirection { get; set; }
        public System.Windows.Media.Color AnimatedLightColor { get; set; }
        public Transform3D AnimatedLightTransform { get; private set; }

        // -- Spot
        public Vector3D SpotlightAttenuation { get; set; }
        public Transform3D SpotlightDirectionTransform { get; private set; }
        public Vector3D SpotlightDirection { get; set; }
        public Transform3D SpotlightTransform { get; private set; }
        public System.Windows.Media.Color SpotlightColor { get; set; }

        // -- Point
        public Vector3D PointlightAttenuation { get; set; }
        public Transform3D PointlightDirectionTransform { get; private set; }
        public Vector3D PointlightDirection { get; set; }
        public Transform3D PointlightTransform { get; private set; }
        public System.Windows.Media.Color PointlightColor { get; set; }

        // -----
        public System.Windows.Media.Color LightAmbientLightColor { get; set; }

        public Transform3D Light1Transform { get; private set; }
        public Vector3D Light1Direction { get; set; }
        public System.Windows.Media.Color Light1Color { get; set; }
        public Transform3D Light1DirectionTransform { get; private set; }

        public int LightPresetNumber
        {
            get
            {
                return _lightPreset;
            }

            set
            {
                _lightPreset = value;
                ToggleScenePresets(value);
                OnPropertyChanged("LightPreset");
            }
        }

        private bool _lightPreset01Active = false;
        private bool _lightPreset02Active = true;

        public bool LightPreset01Active
        {
            get { return _lightPreset01Active; }
            set
            {
                _lightPreset01Active = value;
                OnPropertyChanged("LightPreset01Active");
            }
        }

        public bool LightPreset02Active
        {
            get { return _lightPreset02Active; }
            set
            {
                _lightPreset02Active = value;
                OnPropertyChanged("LightPreset02Active");
            }
        }
        
        // --------------------
        public HelixToolkit.Wpf.SharpDX.MeshGeometry3D Floor { get; private set; }
        public Transform3D FloorTransform { get; private set; }

        public MSAALevel MSAA
        {
            set; get;
        } = MSAALevel.Disable;

        public MSAALevel[] MSAAs { get; } = new MSAALevel[] { MSAALevel.Disable, MSAALevel.Two, MSAALevel.Four, MSAALevel.Eight, MSAALevel.Maximum };

        public FXAALevel FXAA
        {
            set; get;
        } = FXAALevel.None;

        public FXAALevel[] FXAAs { get; } = new FXAALevel[] { FXAALevel.None, FXAALevel.Low, FXAALevel.Medium, FXAALevel.High, FXAALevel.Ultra };

        public Size ShadowMapResolution { get; private set; }

        // Colors
        private Color4 _errorEmissiveColor = new Color4(0.4f, 0.0f, 0.0f, 1.0f);
        private Color4 _normalEmissiveColor = new Color4(0.0f, 0.0f, 0.0f, 1.0f);

        private System.Windows.Media.Color _albedoColorBase = Colors.White;
        public System.Windows.Media.Color AlbedoColorBase
        {
            get
            {
                return _albedoColorBase;
            }

            set
            {
                _albedoColorBase = value;
                PrintedMaterial.AlbedoColor = value.ToColor4();
                OnPropertyChanged("AlbedoColorBase");
            }
        }

        private System.Windows.Media.Color _albedoColorRA = Colors.White;
        public System.Windows.Media.Color AlbedoColorRA
        {
            get
            {
                return _albedoColorRA;
            }

            set
            {
                _albedoColorRA = value;
                PrintedRAMaterial.AlbedoColor = value.ToColor4();
                OnPropertyChanged("AlbedoColorRA");
            }
        }

        private System.Windows.Media.Color _albedoColorDEC = Colors.White;
        public System.Windows.Media.Color AlbedoColorDEC
        {
            get
            {
                return _albedoColorDEC;
            }

            set
            {
                _albedoColorDEC = value;
                PrintedDECMaterial.AlbedoColor = value.ToColor4();
                OnPropertyChanged("AlbedoColorDEC");
            }
        }

        private System.Windows.Media.Color _albedoColorGuider = Colors.White;
        public System.Windows.Media.Color AlbedoColorGuider
        {
            get
            {
                return _albedoColorGuider;
            }

            set
            {
                _albedoColorGuider = value;
                PrintedGuiderMaterial.AlbedoColor = value.ToColor4();
                OnPropertyChanged("AlbedoColorGuider");
            }
        }

        // Materials
        public PBRMaterial PrintedMaterial { get; }
        public PBRMaterial PrintedRAMaterial { get; }
        public PBRMaterial PrintedDECMaterial { get; }
        public PBRMaterial PrintedGuiderMaterial { get; }
        public PBRMaterial AluminiumMaterial { get; }
        public PBRMaterial FloorMaterial { get; }
        public PBRMaterial MetalMaterial { get; }
        public PBRMaterial BlackMetalMaterial { get; }
        public PBRMaterial GlassMaterial { get; }
        public PBRMaterial MarkersMaterial { get; }

        public bool RenderEnvironmentMap
        {
            set
            {
                if (SetValue(ref renderEnvironmentMap, value) && scene != null && scene.Root != null)
                {
                    foreach (var node in scene.Root.Traverse())
                    {
                        if (node is MaterialGeometryNode m && m.Material is PBRMaterialCore material)
                        {
                            material.RenderEnvironmentMap = value;
                        }
                    }
                }
            }
            get => renderEnvironmentMap;
        }

        public bool IsLoading
        {
            private set => SetValue(ref isLoading, value);
            get => isLoading;
        }

        public SceneNodeGroupModel3D GroupModel { get; } = new SceneNodeGroupModel3D();

        public TextureModel EnvironmentMap { get; }

        // Connection Properties
        public string IsConnectedString
        {

            get
            {
                return IsConnected ? "Disconnect" : "Connect";
            }
            set
            {
                _isConnectedString = value;
                OnPropertyChanged("IsConnectedString");
            }
        }

        public bool IsConnected
        {
            get
            {
                return _isConnected;
            }
            set
            {
                _isConnected = value;
                OnPropertyChanged("IsConnected");
            }
        }

        // Mount Properties
        public double RAAngle
        {
            get
            {
                return _raAngle;
            }

            set
            {
                _raAngle = value;
                OnPropertyChanged("RAAngle");
            }
        }
        public double DECAngle
        {
            get
            {
                return _decAngle;
            }

            set
            {
                _decAngle = value;
                OnPropertyChanged("DECAngle");
            }
        }
        public double RATargetAngle
        {
            get
            {
                return _raTargetAngle;
            }

            set
            {
                _raTargetAngle = value;
                OnPropertyChanged("RATargetAngle");
            }
        }
        public double DECTargetAngle
        {
            get
            {
                return _decTargetAngle;
            }

            set
            {
                _decTargetAngle = value;
                OnPropertyChanged("DECTargetAngle");
            }
        }
        public int RAStepper
        {
            get
            {
                return _raStepper;
            }

            set
            {
                _raStepper = value;
                OnPropertyChanged("RAStepper");
            }
        }
        public int DECStepper
        {
            get
            {
                return _decStepper;
            }

            set
            {
                _decStepper = value;
                OnPropertyChanged("DECStepper");
            }
        }
        public int TRKStepper
        {
            get
            {
                return _trkStepper;
            }

            set
            {
                _trkStepper = value;
                OnPropertyChanged("TRKStepper");
            }
        }

        public float RAStepsPerDegree
        {
            get
            {
                return _raStepsPerDegree;
            }

            set
            {
                _raStepsPerDegree = value;
                OnPropertyChanged("RAStepsPerDegree");
            }
        }
        public float DECStepsPerDegree
        {
            get
            {
                return _decStepsPerDegree;
            }

            set
            {
                _decStepsPerDegree = value;
                OnPropertyChanged("DECStepsPerDegree");
            }
        }

        public string Status
        {
            get
            {
                return _status;
            }

            set
            {
                _status = value;
                OnPropertyChanged("Status");
            }
        }

        public string Version
        {
            get
            {
                return _version;
            }

            set
            {
                _version = value;
                OnPropertyChanged("Version");
            }
        }

        public string FirmwareVersion
        {
            get
            {
                return _firmwareVersion;
            }

            set
            {
                _firmwareVersion = value;
                OnPropertyChanged("FirmwareVersion");
            }
        }

        public string ScopeSiderealTime
        {
            get
            {
                return _scopeSiderealTime;
            }

            set
            {
                _scopeSiderealTime = value;
                OnPropertyChanged("ScopeSiderealTime");
            }
        }

        public string ScopePolarisHourAngle
        {
            get
            {
                return _scopePolarisHourAngle;
            }

            set
            {
                _scopePolarisHourAngle = value;
                OnPropertyChanged("ScopePolarisHourAngle");
            }
        }

        public string CurrentRAString
        {
            get
            {
                return _currentRAString;
            }

            set
            {
                _currentRAString = value;
                OnPropertyChanged("CurrentRAString");
            }
        }

        public string CurrentDECString
        {
            get
            {
                return _currentDECString;
            }

            set
            {
                _currentDECString = value;
                OnPropertyChanged("CurrentDECString");
            }
        }

        public int ScopeRASlewMS
        {
            get
            {
                return _scopeRASlewMS;
            }

            set
            {
                _scopeRASlewMS = value;
                OnPropertyChanged("ScopeRASlewMS");
            }
        }

        public int ScopeRATrackMS
        {
            get
            {
                return _scopeRATrackMS;
            }

            set
            {
                _scopeRATrackMS = value;
                OnPropertyChanged("ScopeRATrackMS");
            }
        }

        public int ScopeDECSlewMS
        {
            get
            {
                return _scopeDECSlewMS;
            }

            set
            {
                _scopeDECSlewMS = value;
                OnPropertyChanged("ScopeDECSlewMS");
            }
        }

        public int ScopeDECGuideMS
        {
            get
            {
                return _scopeDECGuideMS;
            }

            set
            {
                _scopeDECGuideMS = value;
                OnPropertyChanged("ScopeDECGuideMS");
            }
        }

        public string ScopeLongitude
        {
            get
            {
                return _scopeLongitude;
            }

            set
            {
                _scopeLongitude = value;
                OnPropertyChanged("ScopeLongitude");
            }
        }

        public string ScopeLatitude
        {
            get
            {
                return _scopeLatitude;
            }

            set
            {
                _scopeLatitude = value;
                OnPropertyChanged("ScopeLatitude");
            }
        }

        #endregion

        #region Commands
        public ICommand ConnectCommand { get; }
        #endregion

        public MainViewModel(MainWindow window)
        {
            /*
            OATData = new ObservableDictonary<string, string>
            {
                { "RAStepper", "0" },
                { "DECStepper", "0" },
                { "TrkStepper", "0" },
                { "RAStepsPerDegree", "0" },
                { "DECStepsPerDegree", "0" },
                { "Version", "0" },
                { "FirmwareVersion", "0" },
                { "ScopeSiderealTime", "0" },
                { "ScopePolarisHourAngle", "0" },
                { "CurrentRAString", "0" },
                { "CurrentDECString", "0" },
                { "ScopeRASlewMS", "0" },
                { "ScopeRATrackMS", "0" },
                { "ScopeDECSlewMS", "0" },
                { "ScopeDECGuideMS", "0" },
                { "ScopeLongitude", "0" },
                { "ScopeLatitude", "0" }
            };
            */
            
            AppSettings.Instance.UpgradeVersion += OnUpgradeSettings;
            AppSettings.Instance.Load();

            EffectsManager = new DefaultEffectsManager();

            Title = "OAT Simulation v0.1";

            // RA Transform Setup
            RAAngle = 0.0;
            RATargetAngle = 0.0;
            _raAR.Axis = new Vector3D(0, 0, 1);
            _raAR.Angle = RAAngle;
            _raRT.Rotation = _raAR;
            _raTransformGrp.Children.Add(_raRT);

            // OAT Transforms Setup
            DECAngle = 0.0;
            DECTargetAngle = 0.0;
            _decAR.Axis = new Vector3D(1, 0, 0);
            _decAR.Angle = DECAngle;
            _decRT.Rotation = _decAR;
            _decTransformGrp.Children.Add(_decRT);

            _raAngleGroupRotateAxis.Axis = new Vector3D(1, 0, 0);
            _raAngleGroupRotateAxis.Angle = 50.0;
            _raAngleGroupRotateTransform.Rotation = _raAngleGroupRotateAxis;
            _raAngleGroupTransform.Children.Add(_raAngleGroupRotateTransform);

            _raOffsetGroupTransform = new Transform3DGroup();
            _raOffsetGroupTranslate = new TranslateTransform3D();
            _raOffsetGroupTransform.Children.Add(_raOffsetGroupTranslate);

            ShadowMapResolution = new Size(2048, 2048);

            mainWindow = window;

            EffectsManager = new DefaultEffectsManager();
            // Camera Setup
            Camera = new HelixToolkit.Wpf.SharpDX.PerspectiveCamera
            {
                Position = new Point3D(0, 1.2, 7),
                LookDirection = new Vector3D(0, 0, -7),
                UpDirection = new Vector3D(0, 1, 0),
                FarPlaneDistance = 100,
                NearPlaneDistance = 0.1f
            };

            // ----------------------------------------------
            DarkAmbientLightColor = System.Windows.Media.Color.FromRgb(12, 12, 12);
            LightAmbientLightColor = System.Windows.Media.Color.FromRgb(24, 24, 24);

            // Material Setup
            // double plaOcclusionFactor = 0.3;
            double plaRoughnessFactor = 0.53;
            double plaMetallicFactor = 0.0;
            double plaReflectanceFactor = 0.0;

            EnvironmentMap = TextureModel.Create("Assets\\Cubemap_Grandcanyon.dds");
            var normalMap = TextureModel.Create("Assets\\TextureNoise1_dot3.dds");


            FloorMaterial = new PBRMaterial()
            {
                AlbedoColor = new Color4(0.4f, 0.4f, 0.4f, 1.0f),
                RoughnessFactor = 0.8,
                MetallicFactor = 0.2,
                RenderShadowMap = true,
                EnableAutoTangent = true,
                //AmbientOcclusionFactor = 0.5
            };

            PrintedMaterial = new PBRMaterial()
            {
                Name = "PrintedMaterial",
                AlbedoColor = _albedoColorBase.ToColor4(),
                ClearCoatStrength = 0.0,
                RoughnessFactor = plaRoughnessFactor,
                MetallicFactor = plaMetallicFactor,
                ReflectanceFactor = plaReflectanceFactor,
                EnableAutoTangent = true,
                EnableTessellation = true,
                NormalMap = normalMap,
                RenderShadowMap = true,
                //AmbientOcclusionFactor = plaOcclusionFactor
            };

            PrintedRAMaterial = new PBRMaterial()
            {
                Name = "PrintedRAMaterial",
                AlbedoColor = _albedoColorRA.ToColor4(),
                ClearCoatStrength = 0.0,
                RoughnessFactor = plaRoughnessFactor,
                MetallicFactor = plaMetallicFactor,
                ReflectanceFactor = plaReflectanceFactor,
                EnableAutoTangent = true,
                EnableTessellation = true,
                NormalMap = normalMap,
                RenderShadowMap = true,
            };

            PrintedDECMaterial = new PBRMaterial()
            {
                Name = "PrintedDECMaterial",
                AlbedoColor = _albedoColorDEC.ToColor4(),
                ClearCoatStrength = 0.0,
                RoughnessFactor = plaRoughnessFactor,
                MetallicFactor = plaMetallicFactor,
                ReflectanceFactor = plaReflectanceFactor,
                EnableAutoTangent = true,
                EnableTessellation = true,
                NormalMap = normalMap,
                RenderShadowMap = true,
            };

            PrintedGuiderMaterial = new PBRMaterial()
            {
                Name = "PrintedGuderMaterial",
                AlbedoColor = _albedoColorGuider.ToColor4(),
                ClearCoatStrength = 0.0,
                RoughnessFactor = plaRoughnessFactor,
                MetallicFactor = plaMetallicFactor,
                ReflectanceFactor = plaReflectanceFactor,
                EnableAutoTangent = true,
                EnableTessellation = true,
                NormalMap = normalMap,
                RenderShadowMap = true,
            };

            AluminiumMaterial = new PBRMaterial()
            {
                Name = "AluminiumMaterial",
                AlbedoColor = new Color4(0.6f, 0.6f, 0.6f, 1.0f),
                RoughnessFactor = 0.0,
                MetallicFactor = 0.0,
                ReflectanceFactor = 0.8,
                RenderShadowMap = false,
                EnableAutoTangent = true,
                //AmbientOcclusionFactor = 0.8,
                RenderEnvironmentMap = RenderEnvironmentMap,
            };

            MetalMaterial = new PBRMaterial()
            {
                Name = "MetalMaterial",
                AlbedoColor = new SharpDX.Color4(0.6f, 0.6f, 0.6f, 1.0f),
                RoughnessFactor = 0.3,
                ClearCoatStrength = 0.01,
                ReflectanceFactor = 0.8,
                MetallicFactor = 1.0,
                RenderShadowMap = true,
                EnableAutoTangent = true,
                RenderEnvironmentMap = RenderEnvironmentMap,
            };

            BlackMetalMaterial = new PBRMaterial()
            {
                Name = "BlackMetalMaterial",
                AlbedoColor = new Color4(0.1f, 0.1f, 0.1f, 1.0f),
                RoughnessFactor = 0.3,
                ClearCoatStrength = 0.01,
                ReflectanceFactor = 0.2,
                MetallicFactor = 0.0,
                RenderShadowMap = true,
                EnableAutoTangent = true,
                AmbientOcclusionFactor = 0.2,
                RenderEnvironmentMap = RenderEnvironmentMap,
            };

            GlassMaterial = new PBRMaterial()
            {
                Name = "GlassMaterial",
                AlbedoColor = new Color4(0.0f, 0.0f, 0.0f, 0.8f),
                RoughnessFactor = 0.0,
                ClearCoatStrength = 1.0,
                ReflectanceFactor = 1.0,
                MetallicFactor = 1.0,
                RenderShadowMap = true,
                EnableAutoTangent = true,
                RenderEnvironmentMap = false,
            };

            MarkersMaterial = new PBRMaterial()
            {
                Name = "MarkersMaterial",
                AlbedoColor = new Color4(0.2f, 0.2f, 0.2f, 1.0f),
                ClearCoatStrength = 0.0,
                RoughnessFactor = 0.56,
                MetallicFactor = 0.0,
                ReflectanceFactor = 0.52,
                RenderShadowMap = true,
                EnableAutoTangent = true,
                AmbientOcclusionFactor = 0.2,
                RenderEnvironmentMap = RenderEnvironmentMap,
                EnableTessellation = false
            };

            // ----------------------------------------------
            // Light Preset 01
            //AmbientLightColor = System.Windows.Media.Color.FromRgb(28, 28, 28);

            AnimatedLightColor = System.Windows.Media.Color.FromRgb(10, 10, 78);// Colors.DarkBlue;
            AnimatedLightDirection = new Vector3D(0, -10, 0);
            AnimatedLightTransform = CreateAnimatedTransform1(-AnimatedLightDirection, new Vector3D(1, 0, 0), 10);

            SpotlightColor = Colors.Black; //Colors.LightBlue;
            SpotlightAttenuation = new Vector3D(0.1f, 0.1f, 0.0f);
            SpotlightDirection = new Vector3D(0, -5, -1);
            SpotlightTransform = CreateAnimatedTransform2(-SpotlightDirection * 2, new Vector3D(0, 1, 0), 50);
            SpotlightDirectionTransform = CreateAnimatedTransform2(-SpotlightDirection, new Vector3D(1, 0, 0), 50);

            PointlightColor = System.Windows.Media.Color.FromRgb(220, 220, 220);
            PointlightAttenuation = new Vector3D(0.1f, 0.1f, 0.1f);
            PointlightDirection = new Vector3D(0, -2, 2);
            PointlightTransform = CreateAnimatedTransform2(-PointlightDirection*2, new Vector3D(0, 1, 0), 50);

            // Light Preset 02
            Light1Color = Colors.Black;
            Light1Direction = new Vector3D(0, -1, 0);
            Light1Transform = new TranslateTransform3D(new Vector3D(0, 10, 0));

            //Light1DirectionTransform = new Vector3D(0, -1, 0);//CreateAnimatedTransform2(Light1Direction, new Vector3D(0, -1, 0), 24);

            MSAA = MSAALevel.Two;
            FXAA = FXAALevel.Medium;

            // ----------------------------------------------
            // floor
            var b2 = new MeshBuilder(true, true, true);
            b2.AddBox(new Vector3(0.0f, -0.5f, 0.0f), 15, 1, 15, BoxFaces.All);
            Floor = b2.ToMeshGeometry3D();
            FloorTransform = new TranslateTransform3D(0, 0, 0);
            var mat = new PhongMaterial
            {
                AmbientColor = Colors.Gray.ToColor4(),
                DiffuseColor = new Color4(0.75f, 0.75f, 0.75f, 1.0f),
                SpecularColor = Colors.White.ToColor4(),
                SpecularShininess = 100f,
                RenderShadowMap = true
            };
            LoadOATModels();
            
            // Create TCP Client
            _tcpClient = new Communication.TCPSimClient(this);
            ConnectCommand = new RelayCommand((o) =>
            {
                if (IsConnected)
                {
                    _tcpClient.Disconnect();
                    StopAnimation();
                }
                else
                {
                    _tcpClient.Connect(4035);
                    StartAnimation();
                }

            });
        }

        public void OnUpgradeSettings(object sender, UpgradeEventArgs e)
        {
            // If needed upgrade the settings file between versions here.
            // e.LoadedVersion vs. e.CurrentVersion;
        }

        public void OnClosing()
        {
            AppSettings.Instance.LightPreset = _lightPreset;
            AppSettings.Instance.MountAngle = _mountAngle;

            AppSettings.Instance.AlbedoColorBase = _albedoColorBase;
            AppSettings.Instance.AlbedoColorRA = _albedoColorRA;
            AppSettings.Instance.AlbedoColorDEC = _albedoColorDEC;
            AppSettings.Instance.AlbedoColorGuider = _albedoColorGuider;

            AppSettings.Instance.Save();
        }

        private void SceneLoaded()
        {
            SelectMountAngle = AppSettings.Instance.MountAngle;
            LightPresetNumber = AppSettings.Instance.LightPreset;

            AlbedoColorBase = AppSettings.Instance.AlbedoColorBase;
            AlbedoColorRA = AppSettings.Instance.AlbedoColorRA;
            AlbedoColorDEC = AppSettings.Instance.AlbedoColorDEC;
            AlbedoColorGuider = AppSettings.Instance.AlbedoColorGuider;

        }

        private void LoadOATModels()
        {
            IsLoading = true;
            _ = Task.Run(() =>
              {
                  var loader = new Importer();
                  loader.Configuration.GlobalScale = 0.01f;
                  return loader.Load("Assets\\OAT\\OAT_01.fbx");
              }).ContinueWith((result) =>
              {
                  IsLoading = false;
                  if (result.IsCompleted)
                  {
                      scene = result.Result;
                      // Animations.Clear();
                      GroupModel.Clear();

                      if (scene != null)
                      {
                          if (scene.Root != null)
                          {
                              foreach (var node in scene.Root.Traverse())
                              {
                                  if (node is GroupNode g)
                                  {
                                      if (node.Name == "ra_grp")
                                      {
                                          RAGroup = g;
                                      }
                                      else if (node.Name == "ra_angle_offset_grp")
                                      {
                                          _raAngleGrp = g;
                                      }
                                      else if (node.Name == "ra_offset_grp")
                                      {
                                          _raOffsetGrp = g;
                                      }

                                      else if (node.Name == "dec_grp")
                                      {
                                          DECGroup = g;
                                      }

                                      else if (node.Name == "deg_20_grp")
                                      {
                                          Deg20Group = g;
                                      }
                                      else if (node.Name == "deg_30_grp")
                                      {
                                          Deg30Group = g;
                                      }
                                      else if (node.Name == "deg_40_grp")
                                      {
                                          Deg40Group = g;
                                      }
                                      else if (node.Name == "deg_50_grp")
                                      {
                                          Deg50Group = g;
                                      }

                                  }

                                  if (node is MaterialGeometryNode m)
                                  {

                                      m.IsThrowingShadow = true;
                                      //m.Geometry.SetAsTransient();

                                      string name = m.Material.Name;

                                      if (m.Material.Name == "printed_mat")
                                      {
                                          m.Material = PrintedMaterial;
                                      }
                                      else if (m.Material.Name == "printed_ra_mat")
                                      {
                                          m.Material = PrintedRAMaterial;
                                      }
                                      else if (m.Material.Name == "printed_dec_mat")
                                      {
                                          m.Material = PrintedDECMaterial;
                                      }
                                      else if (m.Material.Name == "printed_guider_mat")
                                      {
                                          m.Material = PrintedGuiderMaterial;
                                      }
                                      else if (m.Material.Name == "aluminium_mat")
                                      {
                                          m.Material = AluminiumMaterial;
                                      }
                                      else if (m.Material.Name == "floor")
                                      {
                                          m.Material = FloorMaterial;
                                      }
                                      else if (m.Material.Name == "glass_mat")
                                      {
                                          m.Material = GlassMaterial;
                                      }
                                      else if (m.Material.Name == "metal_mat")
                                      {
                                          m.Material = MetalMaterial;
                                      }
                                      else if (m.Material.Name == "black_metal_mat")
                                      {
                                          m.Material = BlackMetalMaterial;
                                      }
                                      else if (m.Material.Name == "markers_mat")
                                      {
                                          m.Material = MarkersMaterial;
                                      }
                                      else if (m.Material is PBRMaterialCore pbr)
                                      {
                                          pbr.RenderEnvironmentMap = RenderEnvironmentMap;
                                      }
                                      else if (m.Material is PhongMaterialCore phong)
                                      {
                                          phong.RenderEnvironmentMap = RenderEnvironmentMap;
                                      }
                                  }
                              }
                          }

                          GroupModel.AddNode(scene.Root);

                          foreach (var n in scene.Root.Traverse())
                          {
                              n.Tag = new AttachedNodeViewModel(n);
                          }
                      }
                      isLoading = false;
                      SceneLoaded();
                      

                  }
                  else if (result.IsFaulted && result.Exception != null)
                  {
                      MessageBox.Show(result.Exception.Message);
                  }
              }, TaskScheduler.FromCurrentSynchronizationContext());

        }

        public void StartAnimation()
        {
            compositeHelper.Rendering += CompositeHelper_Rendering;
        }

        public void StopAnimation()
        {
            compositeHelper.Rendering -= CompositeHelper_Rendering;
        }
        private void CompositeHelper_Rendering(object sender, RenderingEventArgs e)
        {
            if (RAAngle != _raTargetAngle)
            {
                if (RAAngle < _raTargetAngle)
                {
                    RAAngle += _raTargetDegSteps;
                }
                else
                {
                    RAAngle -= _raTargetDegSteps;
                }
                RotateRA();
            }

            if (DECAngle != _decTargetAngle)
            {
                if (DECAngle < _decTargetAngle)
                {
                    DECAngle += _decTargetDegSteps;
                }
                else
                {
                    DECAngle -= _decTargetDegSteps;
                }
                RotateDEC();
            }
        }

        public void RotateRA()
        {
            if (RAGroup != null)
            {
                _raAR.Angle = RAAngle;
                RAGroup.ModelMatrix = _raTransformGrp.ToMatrix();
            }
        }

        public void RotateDEC()
        {
            if (DECGroup != null)
            {
                _decAR.Angle = DECAngle;
                DECGroup.ModelMatrix = _decTransformGrp.ToMatrix();
            }

            if (DECAngle > 35.0 || DECAngle < -180.0)
            {
                PrintedDECMaterial.EmissiveColor = _errorEmissiveColor;
            }
            else
            {
                PrintedDECMaterial.EmissiveColor = _normalEmissiveColor;
            }

        }

        private Transform3D CreateAnimatedTransform1(Vector3D translate, Vector3D axis, double speed = 4)
        {
            var lightTrafo = new Transform3DGroup();
            lightTrafo.Children.Add(new TranslateTransform3D(translate));

            var rotateAnimation = new Rotation3DAnimation
            {
                RepeatBehavior = RepeatBehavior.Forever,
                By = new AxisAngleRotation3D(axis, 90),
                Duration = TimeSpan.FromSeconds(speed / 4),
                IsCumulative = true,
            };

            var rotateTransform = new RotateTransform3D();
            rotateTransform.BeginAnimation(RotateTransform3D.RotationProperty, rotateAnimation);
            lightTrafo.Children.Add(rotateTransform);

            return lightTrafo;
        }

        private Transform3D CreateAnimatedTransform2(Vector3D translate, Vector3D axis, double speed = 4)
        {
            var lightTrafo = new Transform3DGroup();
            lightTrafo.Children.Add(new TranslateTransform3D(translate));

            var rotateAnimation = new Rotation3DAnimation
            {
                RepeatBehavior = RepeatBehavior.Forever,
                From = new AxisAngleRotation3D(axis, 135),
                To = new AxisAngleRotation3D(axis, 225),
                AutoReverse = true,
                Duration = TimeSpan.FromSeconds(speed / 4),
            };

            var rotateTransform = new RotateTransform3D();
            rotateTransform.BeginAnimation(RotateTransform3D.RotationProperty, rotateAnimation);
            lightTrafo.Children.Add(rotateTransform);
            return lightTrafo;
        }

        public void StatusUpdate()
        {
            if (ScopeRATrackMS != 0 && ScopeRASlewMS != 0)
            {
                float raTrkFactor = ScopeRATrackMS / ScopeRASlewMS;

                if (RAStepsPerDegree != 0.0)
                {
                    RATargetAngle = -(RAStepper + (TRKStepper / raTrkFactor)) / RAStepsPerDegree;
                    _raTargetDegSteps = Math.Abs((RAAngle - _raTargetAngle) / 30.0);
                }
            }

            if (ScopeDECSlewMS != 0 && ScopeDECGuideMS != 0 && DECStepsPerDegree != 0.0)
            {
                // float decTrkFactor = ScopeDECGuideMS / ScopeDECSlewMS;
                DECTargetAngle = -DECStepper / DECStepsPerDegree;
                _decTargetDegSteps = Math.Abs((DECAngle - _decTargetAngle) / 30.0);
            }

        }

        public void ToggleScenePresets(int presetNr)
        {
            switch(presetNr)
            {
                case 1:
                    LightPreset01Active = true;
                    LightPreset02Active = false;
                    break;

                case 2:
                    LightPreset01Active = false;
                    LightPreset02Active = true;
                    break;
            }
        }

        public void SetMountAngle(int degrees)
        {
            // Clear previous transforms
            _raOffsetGroupTransform.Children.Clear();

            switch (degrees)
            {
                case 20:
                    Deg20Group.ModelMatrix = new ScaleTransform3D(new Vector3D(1.0, 1.0, 1.0)).ToMatrix();
                    Deg30Group.ModelMatrix = new ScaleTransform3D(new Vector3D(0.0, 0.0, 0.0)).ToMatrix();
                    Deg40Group.ModelMatrix = new ScaleTransform3D(new Vector3D(0.0, 0.0, 0.0)).ToMatrix();
                    Deg50Group.ModelMatrix = new ScaleTransform3D(new Vector3D(0.0, 0.0, 0.0)).ToMatrix();

                    // Offset RA Group
                    _raAngleGroupRotateAxis.Angle = -20.0;
                    _raOffsetGroupTranslate = new TranslateTransform3D(0.0, 156.339, -128.94);
                    
                    break;
                case 30:
                    Deg20Group.ModelMatrix = new ScaleTransform3D(new Vector3D(0.0, 0.0, 0.0)).ToMatrix();
                    Deg30Group.ModelMatrix = new ScaleTransform3D(new Vector3D(1.0, 1.0, 1.0)).ToMatrix();
                    Deg40Group.ModelMatrix = new ScaleTransform3D(new Vector3D(0.0, 0.0, 0.0)).ToMatrix();
                    Deg50Group.ModelMatrix = new ScaleTransform3D(new Vector3D(0.0, 0.0, 0.0)).ToMatrix();

                    // Offset RA Group
                    _raAngleGroupRotateAxis.Angle = -30.0;
                    _raOffsetGroupTranslate = new TranslateTransform3D(0.0, 115.074, -131.541);
                    
                    break;
                case 40:
                    Deg20Group.ModelMatrix = new ScaleTransform3D(new Vector3D(0.0, 0.0, 0.0)).ToMatrix();
                    Deg30Group.ModelMatrix = new ScaleTransform3D(new Vector3D(0.0, 0.0, 0.0)).ToMatrix();
                    Deg40Group.ModelMatrix = new ScaleTransform3D(new Vector3D(1.0, 1.0, 1.0)).ToMatrix();
                    Deg50Group.ModelMatrix = new ScaleTransform3D(new Vector3D(0.0, 0.0, 0.0)).ToMatrix();

                    // Offset RA Group
                    _raAngleGroupRotateAxis.Angle = -40.0;
                    _raOffsetGroupTranslate = new TranslateTransform3D(0.0, 73.409, -134.276);
                    
                    break;
                case 50:
                    Deg20Group.ModelMatrix = new ScaleTransform3D(new Vector3D(0.0, 0.0, 0.0)).ToMatrix();
                    Deg30Group.ModelMatrix = new ScaleTransform3D(new Vector3D(0.0, 0.0, 0.0)).ToMatrix();
                    Deg40Group.ModelMatrix = new ScaleTransform3D(new Vector3D(0.0, 0.0, 0.0)).ToMatrix();
                    Deg50Group.ModelMatrix = new ScaleTransform3D(new Vector3D(1.0, 1.0, 1.0)).ToMatrix();

                    // Offset RA Group
                    _raAngleGroupRotateAxis.Angle = -50.0;
                    _raOffsetGroupTranslate = new TranslateTransform3D(0.0, 46.168, -136.247);
                    
                    break;
                default:
                    break;
            }

            _raAngleGrp.ModelMatrix = _raAngleGroupTransform.ToMatrix();
            _raOffsetGroupTransform.Children.Add(_raOffsetGroupTranslate);
            _raOffsetGrp.ModelMatrix = _raOffsetGroupTransform.ToMatrix();
        }

        public class ObservableDictonary<TKey, TValue> : Dictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
        {
            public event NotifyCollectionChangedEventHandler CollectionChanged;

            public event PropertyChangedEventHandler PropertyChanged;

            public new void Add(TKey key, TValue value)
            {
                base.Add(key, value);
                if (!TryGetValue(key, out _)) return;
                var index = Keys.Count;
                OnPropertyChanged(nameof(Count));
                OnPropertyChanged(nameof(Values));
                OnCollectionChanged(NotifyCollectionChangedAction.Add, value, index);
            }

            public new void Remove(TKey key)
            {
                if (!TryGetValue(key, out var value)) return;
                var index = IndexOf(Keys, key);
                OnPropertyChanged(nameof(Count));
                OnPropertyChanged(nameof(Values));
                OnCollectionChanged(NotifyCollectionChangedAction.Remove, value, index);
                base.Remove(key);
            }

            public new void Clear()
            {

            }

            protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
            {
                PropertyChanged?.Invoke(this, e);
            }

            protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
            {
                CollectionChanged?.Invoke(this, e);
            }

            private void OnPropertyChanged(string propertyName)
            {
                OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
            }

            private void OnCollectionChanged(NotifyCollectionChangedAction action, object item)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item));
            }

            private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
            }

            private int IndexOf(KeyCollection keys, TKey key)
            {
                var index = 0;
                foreach (var k in keys)
                {
                    if (Equals(k, key))
                        return index;
                    index++;
                }
                return -1;
            }
        }
        
    }
}
