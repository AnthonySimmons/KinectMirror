using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace KinectMirror
{
    using KinectManager;

    public delegate void ColorStreamChanged(bool isEnabled);
    public delegate void DepthStreamChanged(bool isEnabled);
    public delegate void SkeletonStreamChanged(bool isEnabled);

    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class Options : Window
    {
        public event ColorStreamChanged ColorStreamChanged;
        public event DepthStreamChanged DepthStreamChanged;
        public event SkeletonStreamChanged SkeletonStreamChanged;

        private bool _isElevationAngleDrag;

        private readonly KinectManager _kinectManager;

        public Options(KinectManager kinectManager)
        {
            InitializeComponent();
            _kinectManager = kinectManager;
            ElevationSlider.Value = _kinectManager.ElevationAngle;
        }
        
        protected virtual void OnElevationAngleChanged()
        {
            try
            {
                _kinectManager.ElevationAngle = (int)ElevationSlider.Value;
            }
            catch
            {

            }
        }

        private void ElevationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isElevationAngleDrag)
            {
                ElevationSlider.IsEnabled = false;
                OnElevationAngleChanged();
                ElevationSlider.IsEnabled = true;
            }
        }

        private void ElevationSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            OnElevationAngleChanged();
            _isElevationAngleDrag = false;
        }
        
        private void ElevationSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _isElevationAngleDrag = true;
        }

        private void RadioButtonColor_CheckChanged(object sender, RoutedEventArgs e)
        {
            OnColorStreamChanged();
        }

        private void RadioButtonDepth_CheckChanged(object sender, RoutedEventArgs e)
        {
            OnDepthStreamChanged();
        }

        private void CheckBox_CheckChanged(object sender, RoutedEventArgs e)
        {
            OnSkeletonStreamChanged();
        }

        protected virtual void OnSkeletonStreamChanged()
        {
            SkeletonStreamChanged?.Invoke(CheckBoxSkeleton.IsChecked ?? false);
        }

        protected virtual void OnColorStreamChanged()
        {
            ColorStreamChanged?.Invoke(RadioButtonColor.IsChecked ?? false);
        }

        protected virtual void OnDepthStreamChanged()
        {
            DepthStreamChanged?.Invoke(RadioButtonDepth.IsChecked ?? false);
        }
    }
}
