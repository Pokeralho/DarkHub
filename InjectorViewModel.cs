using Microsoft.Win32;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace DarkHub
{
    public class InjectorViewModel : INotifyPropertyChanged
    {
        private readonly DllInjectionService _injectionService;
        private string _processName;
        private string _dllPath;
        private string _status;
        private bool _isInjecting;

        public InjectorViewModel()
        {
            _injectionService = new DllInjectionService();
            InjectCommand = new RelayCommand(_ => InjectDll(), _ => CanInjectDll());
            BrowseDllCommand = new RelayCommand(_ => BrowseDll());
            Status = ResourceManagerHelper.Instance.ReadyStatus;
        }

        public string ProcessName
        {
            get => _processName;
            set
            {
                if (_processName != value)
                {
                    _processName = value;
                    OnPropertyChanged();
                    (InjectCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string DllPath
        {
            get => _dllPath;
            set
            {
                if (_dllPath != value)
                {
                    _dllPath = value;
                    OnPropertyChanged();
                    (InjectCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsInjecting
        {
            get => _isInjecting;
            set
            {
                if (_isInjecting != value)
                {
                    _isInjecting = value;
                    OnPropertyChanged();
                    (InjectCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string DllInjectorTitle => ResourceManagerHelper.Instance.DllInjectorTitle;
        public string ProcessNameLabel => ResourceManagerHelper.Instance.ProcessNameLabel;
        public string DllPathLabel => ResourceManagerHelper.Instance.DllPathLabel;
        public string BrowseButton => ResourceManagerHelper.Instance.BrowseButton;
        public string InjectDllButton => ResourceManagerHelper.Instance.InjectDllButton;
        public string ProcessNameToolTip => ResourceManagerHelper.Instance.ProcessNameToolTip;
        public string ReadyStatus => ResourceManagerHelper.Instance.ReadyStatus;
        public string InjectingStatus => ResourceManagerHelper.Instance.InjectingStatus;
        public string InjectionSuccessStatus => ResourceManagerHelper.Instance.InjectionSuccessStatus;
        public string ProcessNotFoundStatus => ResourceManagerHelper.Instance.ProcessNotFoundStatus;
        public string InjectionFailedWithError => ResourceManagerHelper.Instance.InjectionFailedWithError;
        public string ErrorStatus => ResourceManagerHelper.Instance.ErrorStatus;
        public string DllFileFilter => ResourceManagerHelper.Instance.DllFileFilter;
        public string SelectDllTitle => ResourceManagerHelper.Instance.SelectDllTitle;

        public ICommand InjectCommand { get; }
        public ICommand BrowseDllCommand { get; }

        private async void InjectDll()
        {
            try
            {
                IsInjecting = true;
                Status = ResourceManagerHelper.Instance.InjectingStatus;

                int processId = DllInjectionService.GetProcessIdByName(ProcessName);
                if (processId == 0)
                {
                    Status = ResourceManagerHelper.Instance.ProcessNotFoundStatus;
                    return;
                }

                bool success = await Task.Run(() => _injectionService.InjectDll(processId, DllPath));
                Status = success ? ResourceManagerHelper.Instance.InjectionSuccessStatus
                                : string.Format(ResourceManagerHelper.Instance.InjectionFailedWithError, _injectionService.LastError);
            }
            catch (Exception ex)
            {
                Status = string.Format(ResourceManagerHelper.Instance.ErrorStatus, ex.Message);
            }
            finally
            {
                IsInjecting = false;
            }
        }

        private bool CanInjectDll()
        {
            return !IsInjecting &&
                   !string.IsNullOrWhiteSpace(ProcessName) &&
                   !string.IsNullOrWhiteSpace(DllPath);
        }

        private void BrowseDll()
        {
            var dialog = new OpenFileDialog
            {
                Filter = ResourceManagerHelper.Instance.DllFileFilter,
                Title = ResourceManagerHelper.Instance.SelectDllTitle
            };

            if (dialog.ShowDialog() == true)
            {
                DllPath = dialog.FileName;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}