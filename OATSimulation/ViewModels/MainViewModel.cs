using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Assimp;
using HelixToolkit.Wpf.SharpDX.Controls;
using HelixToolkit.Wpf.SharpDX.Model;
using HelixToolkit.Wpf.SharpDX.Model.Scene;
using SharpDX;
using System;
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

        private SynchronizationContext context = SynchronizationContext.Current;
        private HelixToolkitScene scene = null;
        private CompositionTargetEx compositeHelper = new CompositionTargetEx();
        private MainWindow mainWindow = null;

        // RA Transform
        Transform3DGroup _raTransformGrp = new Transform3DGroup();
        RotateTransform3D _raRT = new RotateTransform3D();
        AxisAngleRotation3D _raAR = new AxisAngleRotation3D();

        // DEC Transform
        Transform3DGroup _decTransformGrp = new Transform3DGroup();
        RotateTransform3D _decRT = new RotateTransform3D();
        AxisAngleRotation3D _decAR = new AxisAngleRotation3D();

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

        private string _scopeSiderealTime = "0";
        private string _scopePolarisHourAngle = "0";

        // Animation
        // DispatcherTimer _animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33.3) };

        private double _raTargetAngle = 0.0;
        private double _decTargetAngle = 0.0;
        private double _raTargetDegSteps = 0.0;
        private double _decTargetDegSteps = 0.0;

        private bool renderEnvironmentMap = true;

        #region Properties
        // Lights
        public System.Windows.Media.Color AmbientLightColor { get; set; }

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

        // OAT Main Groups
        private GroupNode RAGroup { get; set; }
        private GroupNode DECGroup { get; set; }

        // Materials
        public PBRMaterial PrintedMaterial { get; }
        public PBRMaterial PrintedRAMaterial { get; }
        public PBRMaterial PrintedDECMaterial { get; }
        public PBRMaterial AluminiumMaterial { get; }
        public PBRMaterial FloorMaterial { get; }
        public PBRMaterial MetalMaterial { get; }
        public PBRMaterial BlackMetalMaterial { get; }
        public PBRMaterial GlassMaterial { get; }
        public PBRMaterial MarkersMaterial { get; }

        private Color4 _errorColor = new Color4(1.0f, 0.0f, 0.0f, 1.0f);
        private Color4 _printedBaseColor = new Color4(0.4f, 0.4f, 0.4f, 1.0f);

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

        #endregion

        #region Commands
        public ICommand ConnectCommand { get; }
        #endregion

        public MainViewModel(MainWindow window)
        {
            EnvironmentMap = TextureModel.Create("Assets\\Cubemap_Grandcanyon.dds");
            EffectsManager = new DefaultEffectsManager();

            Title = "OAT Simulation v0.1";

            // RA Transform Setup
            RAAngle = 0.0;
            RATargetAngle = 0.0;
            _raAR.Axis = new Vector3D(0, 0, 1);
            _raAR.Angle = RAAngle;
            _raRT.Rotation = _raAR;
            _raTransformGrp.Children.Add(_raRT);

            // DEC Transform Setup
            DECAngle = 0.0;
            DECTargetAngle = 0.0;
            _decAR.Axis = new Vector3D(1, 0, 0);
            _decAR.Angle = DECAngle;
            _decRT.Rotation = _decAR;
            _decTransformGrp.Children.Add(_decRT);

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
            // Material Setup
            FloorMaterial = new PBRMaterial()
            {
                AlbedoColor = new SharpDX.Color4(0.4f, 0.4f, 0.4f, 1.0f),
                RoughnessFactor = 0.8,
                MetallicFactor = 0.2,
                RenderShadowMap = true,
                EnableAutoTangent = true,
                AmbientOcclusionFactor = 0.5
            };

            PrintedMaterial = new PBRMaterial()
            {
                AlbedoColor = new SharpDX.Color4(0.4f, 0.4f, 0.4f, 1.0f),
                ClearCoatStrength = 0.0,
                RoughnessFactor = 0.56,
                MetallicFactor = 0.0,
                ReflectanceFactor = 0.52,
                RenderShadowMap = true,
                EnableAutoTangent = true,
                AmbientOcclusionFactor = 0.2,
                RenderEnvironmentMap = RenderEnvironmentMap,
                EnableTessellation = true
            };

            PrintedRAMaterial = new PBRMaterial()
            {
                AlbedoColor = new SharpDX.Color4(0.4f, 0.4f, 0.4f, 1.0f),
                ClearCoatStrength = 0.0,
                RoughnessFactor = 0.56,
                MetallicFactor = 0.0,
                ReflectanceFactor = 0.52,
                RenderShadowMap = true,
                EnableAutoTangent = true,
                AmbientOcclusionFactor = 0.2,
                RenderEnvironmentMap = RenderEnvironmentMap,
                EnableTessellation = true
            };

            PrintedDECMaterial = new PBRMaterial()
            {
                AlbedoColor = new SharpDX.Color4(0.4f, 0.4f, 0.4f, 1.0f),
                ClearCoatStrength = 0.0,
                RoughnessFactor = 0.56,
                MetallicFactor = 0.0,
                ReflectanceFactor = 0.52,
                RenderShadowMap = true,
                EnableAutoTangent = true,
                AmbientOcclusionFactor = 0.2,
                RenderEnvironmentMap = RenderEnvironmentMap,
                EnableTessellation = true
            };

            AluminiumMaterial = new PBRMaterial()
            {
                AlbedoColor = new SharpDX.Color4(0.6f, 0.6f, 0.6f, 1.0f),
                RoughnessFactor = 0.0,
                MetallicFactor = 0.0,
                ReflectanceFactor = 0.8,
                RenderShadowMap = false,
                EnableAutoTangent = true,
                AmbientOcclusionFactor = 0.8,
                RenderEnvironmentMap = RenderEnvironmentMap,
            };

            MetalMaterial = new PBRMaterial()
            {
                AlbedoColor = new SharpDX.Color4(0.6f, 0.6f, 0.6f, 1.0f),
                RoughnessFactor = 0.3,
                ClearCoatStrength = 0.01,
                ReflectanceFactor = 0.8,
                MetallicFactor = 1.0,
                RenderShadowMap = true,
                EnableAutoTangent = true,
                AmbientOcclusionFactor = 1.0,
                RenderEnvironmentMap = RenderEnvironmentMap,
            };

            BlackMetalMaterial = new PBRMaterial()
            {
                AlbedoColor = new SharpDX.Color4(0.1f, 0.1f, 0.1f, 1.0f),
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
                AlbedoColor = new SharpDX.Color4(0.0f, 0.0f, 0.8f, 0.8f),
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
                AlbedoColor = new SharpDX.Color4(0.2f, 0.2f, 0.2f, 1.0f),
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
            // Lights and scene setup
            AmbientLightColor = System.Windows.Media.Color.FromRgb(24, 24, 24);

            AnimatedLightColor = System.Windows.Media.Color.FromRgb(10, 10, 78);// Colors.DarkBlue;
            AnimatedLightDirection = new Vector3D(0, -10, 0);
            AnimatedLightTransform = CreateAnimatedTransform1(-AnimatedLightDirection, new Vector3D(1, 0, 0), 10);

            SpotlightColor = Colors.LightBlue;
            SpotlightAttenuation = new Vector3D(0.1f, 0.1f, 0.0f);
            SpotlightDirection = new Vector3D(0, -5, -1);
            SpotlightTransform = CreateAnimatedTransform2(-SpotlightDirection * 2, new Vector3D(0, 1, 0), 50);
            SpotlightDirectionTransform = CreateAnimatedTransform2(-SpotlightDirection, new Vector3D(1, 0, 0), 50);

            PointlightColor = System.Windows.Media.Color.FromRgb(220, 220, 220);
            PointlightAttenuation = new Vector3D(0.1f, 0.1f, 0.1f);
            PointlightDirection = new Vector3D(0, -2, 5);
            PointlightTransform = CreateAnimatedTransform2(-PointlightDirection, new Vector3D(0, 1, 0), 50);

            MSAA = MSAALevel.Two;
            FXAA = FXAALevel.Medium;

            // ----------------------------------------------
            // floor
            var b2 = new MeshBuilder(true, true, true);
            b2.AddBox(new Vector3(0.0f, -0.5f, 0.0f), 15, 1, 15, BoxFaces.All);
            this.Floor = b2.ToMeshGeometry3D();
            this.FloorTransform = new TranslateTransform3D(0, 0, 0);
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
                }
                else
                {
                    _tcpClient.Connect();
                    StartAnimation();
                }

            });

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
                                      else if (node.Name == "dec_grp")
                                      {
                                          DECGroup = g;
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
            /*
            if (DECAngle > 5.0)
            {
                PrintedDECMaterial.AlbedoColor = _errorColor;
            }
            else
            {
                PrintedDECMaterial.AlbedoColor = _printedBaseColor;
            }
            */
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
            if (RAStepsPerDegree != 0.0)
            {
                RATargetAngle = -(RAStepper + TRKStepper) / RAStepsPerDegree;
                _raTargetDegSteps = Math.Abs((RAAngle - _raTargetAngle) / 30.0);
            }

            if (DECStepsPerDegree != 0.0)
            {
                DECTargetAngle = -DECStepper / DECStepsPerDegree;
                _decTargetDegSteps = Math.Abs((DECAngle - _decTargetAngle) / 30.0);
            }

        }
    }
}
