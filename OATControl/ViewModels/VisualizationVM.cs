using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HelixToolkit.Wpf;
using System.Windows.Media;
using System.Windows.Media.Media3D;

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
                OnMoveRA(value);
                SetPropertyValue(ref _angleRA, value);
            }
        }

        public double DECAngle
        {
            get { return _angleDEC; }
            set
            {
                OnMoveDEC(value);
                SetPropertyValue(ref _angleDEC, value);
            }
        }

        #endregion

        public VisualizationVM()
        {
            LoadModels();

            // Not working, probably DP
            // LoadModelsAsync();
        }

        #region Methods
        public async void LoadModelsAsync()
        {
            await Task.Run(() => LoadModels());
        }

        public void LoadModels()
        {
            _oatGroup = new Model3DGroup();
            _oatCompass = new ObjReader().Read(@"Visualization/OBJs/OAT_Compass.obj");
            _oatBase = new ObjReader().Read(@"Visualization/OBJs/OAT_Base.obj");
            _oatRA = new ObjReader().Read(@"Visualization/OBJs/OAT_RA.obj");
            _oatDEC = new ObjReader().Read(@"Visualization/OBJs/OAT_DEC.obj");

            RAAngle = 0.0;
            DECAngle = 0.0;

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
        }

        void OnMoveRA(double angle)
        {
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
            Transform3DGroup decTransform3DGroup = new Transform3DGroup();

            TranslateTransform3D decTranslate = new TranslateTransform3D(0, 0, 20.0);
            RotateTransform3D decRX = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), -18.628 + angle));

            decTransform3DGroup.Children.Add(decRX);
            decTransform3DGroup.Children.Add(decTranslate);

            _oatDEC.Transform = decTransform3DGroup;
        }
        #endregion
    }
}
