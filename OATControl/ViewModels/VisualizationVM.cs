using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HelixToolkit.Wpf;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using System.Windows;

namespace OATControl.ViewModels
{
    public class VisualizationVM : ViewModelBase
    {
        #region Variables
        Model3D _OATModel;

        Model3DGroup _oatGroup;
        Model3DGroup _oatCompass;
        Model3DGroup _oatBase;
        Model3DGroup _oatRA;
        Model3DGroup _oatDEC;

        double _angleRA;
        double _angleDEC;

        double _targetRA;
        double _targetDEC;
        double _stepIncrement;

        private DispatcherTimer _animationTimer;
        private Dispatcher dispatcher;

        bool _isInitialized;

        #endregion

        #region Properties
        public Model3D OATModel
        {
            get { return _OATModel; }
            set { SetPropertyValue(ref _OATModel, value); }
        }

        public double RAAngle
        {
            get { return _angleRA; }
            set {
                SetPropertyValue(ref _targetRA, value);
            }
        }

        public double DECAngle
        {
            get { return _angleDEC; }
            set
            {
                SetPropertyValue(ref _targetDEC, value);
            }
        }

        public bool IsInitialized
        {
            get { return _isInitialized; }
            set
            {
                SetPropertyValue(ref _isInitialized, value);
            }
        }

        #endregion

        public VisualizationVM()
        {
            IsInitialized = false;
            LoadModels();
        }

        #region Methods
        public async void LoadModelsAsync()
        {
            await Task.Run(() => LoadModels());
        }

        private async Task<Model3DGroup> LoadAsync(string model3DPath, bool freeze)
        {
            return await Task.Factory.StartNew(() =>
            {
                var mi = new ModelImporter();

                if (freeze)
                {
                    // Alt 1. - freeze the model 
                    return mi.Load(model3DPath, null, true);
                }

                // Alt. 2 - create the model on the UI dispatcher
                return mi.Load(model3DPath, this.dispatcher);
            });
        }

        public async void LoadModels()
        {
            _oatGroup = new Model3DGroup();
            //_oatCompass = new ObjReader().Read(@"Visualization/OBJs/OAT_Compass.obj");
            _oatCompass = await Task.Run(() => LoadAsync(@"Visualization/OBJs/OAT_Compass.obj", true).Result);
            _oatBase = await Task.Run(() => LoadAsync(@"Visualization/OBJs/OAT_Base.obj", true).Result);
            var _oatRAF = await Task.Run(() => LoadAsync(@"Visualization/OBJs/OAT_RA.obj", true).Result);
            var _oatDECF = await Task.Run(() => LoadAsync(@"Visualization/OBJs/OAT_DEC.obj", true).Result);

            // Get around frozen state
            _oatRA = _oatRAF.Clone();
            _oatDEC = _oatDECF.Clone();

            //add them to the group
            _oatGroup.Children.Add(_oatCompass);
            _oatGroup.Children.Add(_oatBase);
            _oatGroup.Children.Add(_oatRA);
            _oatRA.Children.Add(_oatDEC);

            //rotate entire OAT group 90 degrees
            RotateTransform3D myRotateTransform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 90));
            myRotateTransform.CenterX = 0;
            myRotateTransform.CenterY = 0;
            myRotateTransform.CenterZ = 0;
            _oatGroup.Transform = myRotateTransform;

            // Assign final group
            OATModel = _oatGroup;

            _stepIncrement = 0.05;
            _animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
            _animationTimer.Tick += AnimationUpdate;
            _animationTimer.Start();

            // Not working, probably DP
            // LoadModelsAsync();

            IsInitialized = true;

            OnMoveRA(0.0);
            OnMoveDEC(0.0);

        }

        void OnMoveRA(double angle)
        {
            if (!IsInitialized)
                return;

            Transform3DGroup raTransform3DGroup = new Transform3DGroup();

            RotateTransform3D _raRX = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), -49.954));
            RotateTransform3D _raRY = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), -1.056));
            RotateTransform3D _raRZ = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), -0.887 + angle));

            raTransform3DGroup.Children.Add(_raRZ);
            raTransform3DGroup.Children.Add(_raRX);
            raTransform3DGroup.Children.Add(_raRY);

            TranslateTransform3D raTranslate = new TranslateTransform3D(0.171, 5.016, -12.753);
            raTransform3DGroup.Children.Add(raTranslate);

            _oatRA.Transform = raTransform3DGroup;
        }

        void OnMoveDEC(double angle)
        {
            if (!IsInitialized)
                return;

            Transform3DGroup decTransform3DGroup = new Transform3DGroup();

            TranslateTransform3D decTranslate = new TranslateTransform3D(0, 0, 20.0);
            RotateTransform3D decRX = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), -18.628 + angle));

            decTransform3DGroup.Children.Add(decRX);
            decTransform3DGroup.Children.Add(decTranslate);

            _oatDEC.Transform = decTransform3DGroup;
        }

        private void AnimationUpdate(object sender, EventArgs e)
        {
            if (!IsInitialized)
                return;

            // TODO: Calculate the degrees per second to reach exact target
            if (_angleRA < _targetRA)
            {
                _angleRA += _stepIncrement;
                OnMoveRA(_angleRA);
            }
            else if (_angleRA > _targetRA)
            {
                _angleRA -= _stepIncrement;
                OnMoveRA(_angleRA);
            }

            if (_angleDEC < _targetDEC)
            {
                _angleDEC += _stepIncrement;
                OnMoveDEC(_angleDEC);
            }
            else if (_angleDEC > _targetDEC)
            {
                _angleDEC -= _stepIncrement;
                OnMoveDEC(_angleDEC);
            }
        }

        #endregion
    }
}
