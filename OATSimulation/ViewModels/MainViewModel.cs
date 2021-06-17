using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Animations;
using HelixToolkit.Wpf.SharpDX.Assimp;
using HelixToolkit.Wpf.SharpDX.Controls;
using HelixToolkit.Wpf.SharpDX.Model;
using HelixToolkit.Wpf.SharpDX.Model.Scene;
using SharpDX;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

        private TCPServer.TCPServer _tcpServer;

        private SynchronizationContext context = SynchronizationContext.Current;
        private HelixToolkitScene scene = null;
        private IAnimationUpdater animationUpdater = null;
        private CompositionTargetEx compositeHelper = new CompositionTargetEx();
        private MainWindow mainWindow = null;

        private string _status = "Not connected...";
        private string _version = "";

        private double _raAngle = 0.0;
        private double _decAngle = 0.0;
        private int _raStepper = 0;
        private int _decStepper = 0;
        private int _trkStepper = 0;
        private float _raStepsPerDegree = 0.0f;
        private float _decStepsPerDegree = 0.0f;

        private bool renderEnvironmentMap = true;

        #region Properties
        // Lights
        public System.Windows.Media.Color AmbientLightColor { get; set; }
        public Vector3D AnimatedLightDirection { get; set; }
        public System.Windows.Media.Color AnimatedLightColor { get; set; }
        public Transform3D AnimatedLightTransform { get; private set; }

        public Vector3D SpotlightAttenuation { get; set; }
        public Transform3D SpotlightDirectionTransform { get; private set; }
        public Vector3D SpotlightDirection { get; set; }
        public Transform3D SpotlightTransform { get; private set; }
        public System.Windows.Media.Color SpotlightColor { get; set; }

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
        public PBRMaterial AluminiumMaterial { get; }
        public PBRMaterial FloorMaterial { get; }

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
                _version = "v" + value;
                OnPropertyChanged("Version");
            }
        }

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

        private bool isLoading = false;
        public bool IsLoading
        {
            private set => SetValue(ref isLoading, value);
            get => isLoading;
        }

        public ObservableCollection<IAnimationUpdater> Animations { get; } = new ObservableCollection<IAnimationUpdater>();

        public SceneNodeGroupModel3D GroupModel { get; } = new SceneNodeGroupModel3D();

        private float speed = 1.0f;
        public float Speed
        {
            set
            {
                if (SetValue(ref speed, value))
                {
                    if (animationUpdater != null)
                        animationUpdater.Speed = value;
                }
            }
            get => speed;
        }

        public TextureModel EnvironmentMap { get; }

        #endregion
        
        public MainViewModel(MainWindow window)
        {
            // Create TCP server
            _tcpServer = new TCPServer.TCPServer();
            _tcpServer.StartServer(this);

            EnvironmentMap = TextureModel.Create("Assets\\Cubemap_Grandcanyon.dds");
            EffectsManager = new DefaultEffectsManager();

            Title = "OAT Simulation v0.1";

            RAAngle = 0.0;
            DECAngle = 0.0;

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
                RoughnessFactor = 0.0,
                MetallicFactor = 0.0,
                RenderShadowMap = true,
                EnableAutoTangent = true,
                AmbientOcclusionFactor = 0.4
            };

            AluminiumMaterial = new PBRMaterial()
            {
                AlbedoColor = new SharpDX.Color4(0.6f, 0.6f, 0.6f, 1.0f),
                RoughnessFactor = 0.0,
                MetallicFactor = 0.0,
                RenderShadowMap = true,
                EnableAutoTangent = true,
            };

            // ----------------------------------------------
            // Scene Setup
            AmbientLightColor = System.Windows.Media.Color.FromRgb(24, 24, 24);

            AnimatedLightColor = System.Windows.Media.Color.FromRgb(10, 10, 78);// Colors.DarkBlue;
            AnimatedLightDirection = new Vector3D(0, -10, 0);
            AnimatedLightTransform = CreateAnimatedTransform1(-AnimatedLightDirection, new Vector3D(1, 0, 0), 10);

            SpotlightColor = Colors.LightBlue;
            SpotlightAttenuation = new Vector3D(0.1f, 0.1f, 0.0f);
            SpotlightDirection = new Vector3D(0, -5, -1);
            SpotlightTransform = CreateAnimatedTransform2(-SpotlightDirection * 2, new Vector3D(0, 1, 0), 50);
            SpotlightDirectionTransform = CreateAnimatedTransform2(-SpotlightDirection, new Vector3D(1, 0, 0), 50);

            MSAA = MSAALevel.Four;
            FXAA = FXAALevel.High;

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
        }
        
        private void LoadOATModels()
        {
            IsLoading = true;
            _ = Task.Run(() =>
              {
                  var loader = new Importer();
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
                                    if (m.Material.Name == "printed")
                                      {
                                          m.Material = PrintedMaterial;
                                      }
                                      else if (m.Material.Name == "aluminium")
                                      {
                                          m.Material = AluminiumMaterial;
                                      }
                                      if (m.Material.Name == "floor")
                                      {
                                          m.Material = FloorMaterial;
                                      }
                                      if (m.Material is PBRMaterialCore pbr)
                                      {
                                        //pbr.RenderEnvironmentMap = RenderEnvironmentMap;
                                    }
                                      else if (m.Material is PhongMaterialCore phong)
                                      {
                                          phong.RenderEnvironmentMap = RenderEnvironmentMap;
                                      }
                                  }
                              }
                          }
                          GroupModel.AddNode(scene.Root);

                          /*
                          if (scene.HasAnimation)
                          {
                              var dict = scene.Animations.CreateAnimationUpdaters();
                              foreach (var ani in dict.Values)
                              {
                                  Animations.Add(ani);
                              }
                          }
                          */
                          foreach (var n in scene.Root.Traverse())
                          {
                              n.Tag = new AttachedNodeViewModel(n);
                          }
                      }
                      StartAnimation();

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
            RotateRA();
            RotateDEC();

            if (animationUpdater != null)
            {
                animationUpdater.Update(Stopwatch.GetTimestamp(), Stopwatch.Frequency);
            }
        }

        public void RotateRA()
        {
            Transform3DGroup grp;
            RotateTransform3D rt = new RotateTransform3D();
            AxisAngleRotation3D ar = new AxisAngleRotation3D();
            ar.Axis = new Vector3D(0, 0, 1);
            ar.Angle = RAAngle;

            rt.Rotation = ar;

            grp = new Transform3DGroup();
            grp.Children.Add(rt);

            if(RAGroup != null)
            {
                RAGroup.ModelMatrix = grp.ToMatrix();
            }
        }

        public void RotateDEC()
        {
            Transform3DGroup grp;
            RotateTransform3D rt = new RotateTransform3D();
            AxisAngleRotation3D ar = new AxisAngleRotation3D();
            ar.Axis = new Vector3D(1, 0, 0);
            ar.Angle = DECAngle;

            rt.Rotation = ar;

            grp = new Transform3DGroup();
            grp.Children.Add(rt);

            if(DECGroup != null)
            {
                DECGroup.ModelMatrix = grp.ToMatrix();
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
            RAAngle = -(RAStepper + TRKStepper) / RAStepsPerDegree;
            DECAngle = -DECStepper / DECStepsPerDegree;
        }
    }

    


}
