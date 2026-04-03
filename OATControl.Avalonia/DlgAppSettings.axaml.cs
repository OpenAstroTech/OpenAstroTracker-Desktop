using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using OATControl.ViewModels;

namespace OATControl.Avalonia
{
    public partial class DlgAppSettings : Window, INotifyPropertyChanged
    {
        private MountVM? _mount;
        private PointOfInterest? _selectedPoint;

        private string _selectedBaudRate = string.Empty;
        private ChecklistShowOn _showChecklist;
        private string _raDirection = "East";
        private string _decDirection = "South";
        private float _raDistance;
        private float _decDistance;
        private float _altLimit;
        private float _azLimit;
        private float _polarAlignmentMinimumTotalError;
        private bool _invertALTCorrections;
        private bool _invertAZCorrections;
        private bool _monitorNinaForPA;
        private string _ninaLogFolder = string.Empty;
        private bool _monitorSharpCapForPA;
        private string _sharpCapLogFolder = string.Empty;
        private bool _alwaysShowConnectDialog;
        private int _selectedCategoryIndex;
        private int _selectedTabIndex;
        private string _sortField = "Name";
        private int _sortDirection = 1;
        private List<PointOfInterest> _pointsView = new List<PointOfInterest>();

        public DlgAppSettings()
        {
            DataContext = this;
            InitializeComponent();
        }

        public DlgAppSettings(MountVM mount)
        {
            _mount = mount;
            SelectedBaudRate = AppSettings.Instance.BaudRate;
            RaDirection = AppSettings.Instance.AutoHomeRaDirection;
            RaDistance = AppSettings.Instance.AutoHomeRaDistance;
            DecDirection = AppSettings.Instance.AutoHomeDecDirection;
            DecDistance = AppSettings.Instance.AutoHomeDecDistance;
            ShowChecklist = AppSettings.Instance.ShowChecklist;
            ALTLimit = AppSettings.Instance.ALTLimit;
            AZLimit = AppSettings.Instance.AZLimit;
            PolarAlignmentMinimumTotalError = AppSettings.Instance.PolarAlignmentMinimumTotalError;
            InvertALTCorrections = AppSettings.Instance.InvertALTCorrections;
            InvertAZCorrections = AppSettings.Instance.InvertAZCorrections;
            MonitorNinaForPA = AppSettings.Instance.MonitorNinaPA;
            NinaLogFolder = AppSettings.Instance.NinaLogFolder;
            MonitorSharpCapForPA = AppSettings.Instance.MonitorSharpCapPA;
            SharpCapLogFolder = AppSettings.Instance.SharpCapLogFolder;
            AlwaysShowConnectDialog = AppSettings.Instance.AlwaysShowConnectDialog;
            _pointsView = _mount.AllPointsOfInterest.ToList();
            ApplySorting();

            DataContext = this;
            InitializeComponent();
        }

        public IEnumerable<string> AvailableBaudRates => _mount?.AvailableBaudRates ?? Array.Empty<string>();
        public Array AvailableChecklistShowOn => ChecklistShowOnEnumHelper.ChecklistShowOnValues;
        public IEnumerable<string> AvailableRaDirections => new[] { "East", "West" };
        public IEnumerable<string> AvailableDecDirections => new[] { "South", "North" };
        public IEnumerable<PointOfInterest> AllPointsOfInterest => _pointsView;

        public int SelectedCategoryIndex
        {
            get => _selectedCategoryIndex;
            set
            {
                if (_selectedCategoryIndex != value)
                {
                    _selectedCategoryIndex = value;
                    OnPropertyChanged();
                    if (_selectedTabIndex != value)
                    {
                        _selectedTabIndex = value;
                        OnPropertyChanged(nameof(SelectedTabIndex));
                    }
                }
            }
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (_selectedTabIndex != value)
                {
                    _selectedTabIndex = value;
                    OnPropertyChanged();
                    if (_selectedCategoryIndex != value)
                    {
                        _selectedCategoryIndex = value;
                        OnPropertyChanged(nameof(SelectedCategoryIndex));
                    }
                }
            }
        }

        public string NameSortIndicator => SortIndicatorFor("Name");
        public string CatalogSortIndicator => SortIndicatorFor("Catalog");
        public string ShowSortIndicator => SortIndicatorFor("Show");
        public string RASortIndicator => SortIndicatorFor("RA");
        public string DECSortIndicator => SortIndicatorFor("DEC");

        public bool IsPointSelected => SelectedPoint != null;

        public PointOfInterest? SelectedPoint
        {
            get => _selectedPoint;
            set
            {
                if (_selectedPoint != value)
                {
                    _selectedPoint = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsPointSelected));
                }
            }
        }

        public string SelectedBaudRate
        {
            get => _selectedBaudRate;
            set => SetPropertyValue(ref _selectedBaudRate, value);
        }

        public ChecklistShowOn ShowChecklist
        {
            get => _showChecklist;
            set => SetPropertyValue(ref _showChecklist, value);
        }

        public string RaDirection
        {
            get => _raDirection;
            set => SetPropertyValue(ref _raDirection, value);
        }

        public string DecDirection
        {
            get => _decDirection;
            set => SetPropertyValue(ref _decDirection, value);
        }

        public float RaDistance
        {
            get => _raDistance;
            set => SetPropertyValue(ref _raDistance, value);
        }

        public float DecDistance
        {
            get => _decDistance;
            set => SetPropertyValue(ref _decDistance, value);
        }

        public float ALTLimit
        {
            get => _altLimit;
            set => SetPropertyValue(ref _altLimit, value);
        }

        public float AZLimit
        {
            get => _azLimit;
            set => SetPropertyValue(ref _azLimit, value);
        }

        public float PolarAlignmentMinimumTotalError
        {
            get => _polarAlignmentMinimumTotalError;
            set => SetPropertyValue(ref _polarAlignmentMinimumTotalError, value);
        }

        public bool InvertALTCorrections
        {
            get => _invertALTCorrections;
            set => SetPropertyValue(ref _invertALTCorrections, value);
        }

        public bool InvertAZCorrections
        {
            get => _invertAZCorrections;
            set => SetPropertyValue(ref _invertAZCorrections, value);
        }

        public bool MonitorNinaForPA
        {
            get => _monitorNinaForPA;
            set => SetPropertyValue(ref _monitorNinaForPA, value);
        }

        public string NinaLogFolder
        {
            get => _ninaLogFolder;
            set => SetPropertyValue(ref _ninaLogFolder, value);
        }

        public bool MonitorSharpCapForPA
        {
            get => _monitorSharpCapForPA;
            set => SetPropertyValue(ref _monitorSharpCapForPA, value);
        }

        public bool AlwaysShowConnectDialog
        {
            get => _alwaysShowConnectDialog;
            set => SetPropertyValue(ref _alwaysShowConnectDialog, value);
        }

        public string SharpCapLogFolder
        {
            get => _sharpCapLogFolder;
            set => SetPropertyValue(ref _sharpCapLogFolder, value);
        }

        private async void OnAddPoint(object? sender, RoutedEventArgs e)
        {
            if (_mount == null)
            {
                return;
            }

            var dlg = new DlgEditPoint(_mount)
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            await dlg.ShowDialog<object?>(this);
            RefreshPoints();
        }

        private async void OnEditPoint(object? sender, RoutedEventArgs e)
        {
            if (_mount == null || _selectedPoint == null)
            {
                return;
            }

            var dlg = new DlgEditPoint(_mount, _selectedPoint)
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            await dlg.ShowDialog<object?>(this);
            RefreshPoints();
        }

        private async void OnConfigureChecklist(object? sender, RoutedEventArgs e)
        {
            if (_mount == null)
            {
                return;
            }

            var dlg = new global::OATControl.DlgChecklistEditor(_mount.ChecklistFilePath)
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            await dlg.ShowDialog<object?>(this);
        }

        private void OnCloseClick(object? sender, RoutedEventArgs e)
        {
            if (_mount == null)
            {
                Close();
                return;
            }

            AppSettings.Instance.BaudRate = SelectedBaudRate;
            AppSettings.Instance.AutoHomeRaDirection = RaDirection;
            AppSettings.Instance.AutoHomeRaDistance = RaDistance;
            AppSettings.Instance.AutoHomeDecDirection = DecDirection;
            AppSettings.Instance.AutoHomeDecDistance = DecDistance;
            AppSettings.Instance.ShowChecklist = ShowChecklist;
            AppSettings.Instance.AZLimit = AZLimit;
            AppSettings.Instance.ALTLimit = ALTLimit;
            AppSettings.Instance.PolarAlignmentMinimumTotalError = PolarAlignmentMinimumTotalError;
            AppSettings.Instance.InvertALTCorrections = InvertALTCorrections;
            AppSettings.Instance.InvertAZCorrections = InvertAZCorrections;
            AppSettings.Instance.MonitorNinaPA = MonitorNinaForPA;
            AppSettings.Instance.NinaLogFolder = NinaLogFolder;
            AppSettings.Instance.MonitorSharpCapPA = MonitorSharpCapForPA;
            AppSettings.Instance.SharpCapLogFolder = SharpCapLogFolder;
            AppSettings.Instance.AlwaysShowConnectDialog = AlwaysShowConnectDialog;
            AppSettings.Instance.Save();

            _mount.SelectedBaudRate = SelectedBaudRate;
            _mount.AutoHomeRaDirection = RaDirection;
            _mount.AutoHomeRaDistance = RaDistance;
            _mount.AutoHomeDecDirection = DecDirection;
            _mount.AutoHomeDecDistance = DecDistance;
            _mount.ShowChecklist = ShowChecklist;
            _mount.AZLimit = AZLimit;
            _mount.ALTLimit = ALTLimit;
            _mount.PolarAlignmentMinimumTotalError = PolarAlignmentMinimumTotalError;
            _mount.InvertALTCorrections = InvertALTCorrections;
            _mount.InvertAZCorrections = InvertAZCorrections;
            _mount.MonitorNinaForPA = MonitorNinaForPA;
            _mount.NinaLogFolder = NinaLogFolder;
            _mount.MonitorSharpCapForPA = MonitorSharpCapForPA;
            _mount.SharpCapLogFolder = SharpCapLogFolder;

            _mount.SavePointsOfInterest();
            Close();
        }

        private void OnCloseCancelClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnTargetsDoubleTapped(object? sender, TappedEventArgs e)
        {
            if (SelectedPoint != null)
            {
                OnEditPoint(sender, new RoutedEventArgs());
            }
        }

        private void OnSortTargets(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string field)
            {
                if (_sortField == field)
                {
                    _sortDirection *= -1;
                }
                else
                {
                    _sortField = field;
                    _sortDirection = 1;
                }

                ApplySorting();
            }
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SetPropertyValue<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (!Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
            }
        }

        private void RefreshPoints()
        {
            if (_mount != null)
            {
                _pointsView = _mount.AllPointsOfInterest.ToList();
                ApplySorting();
            }
        }

        private void ApplySorting()
        {
            var points = _pointsView.ToList();
            points.Sort((a, b) =>
            {
                int result = _sortField switch
                {
                    "Name" => CompareWithFallback(a.Name, b.Name, a.CatalogName, b.CatalogName),
                    "Catalog" => CompareWithFallback(a.CatalogName, b.CatalogName, a.Name, b.Name),
                    "Show" => a.Enabled.CompareTo(b.Enabled),
                    "RA" => a.RA.CompareTo(b.RA),
                    "DEC" => a.DEC.CompareTo(b.DEC),
                    _ => CompareWithFallback(a.Name, b.Name, a.CatalogName, b.CatalogName)
                };

                return _sortDirection * result;
            });

            _pointsView = points;
            OnPropertyChanged(nameof(AllPointsOfInterest));
            OnPropertyChanged(nameof(NameSortIndicator));
            OnPropertyChanged(nameof(CatalogSortIndicator));
            OnPropertyChanged(nameof(ShowSortIndicator));
            OnPropertyChanged(nameof(RASortIndicator));
            OnPropertyChanged(nameof(DECSortIndicator));
        }

        private static int CompareWithFallback(string a, string b, string fallbackA, string fallbackB)
        {
            if (!string.IsNullOrEmpty(a) || !string.IsNullOrEmpty(b))
            {
                return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
            }

            return string.Compare(fallbackA, fallbackB, StringComparison.OrdinalIgnoreCase);
        }

        private string SortIndicatorFor(string field)
        {
            if (_sortField != field)
            {
                return string.Empty;
            }

            return _sortDirection >= 0 ? "▲" : "▼";
        }
    }
}
