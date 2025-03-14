using System.Windows.Controls;

namespace DarkHub
{
    public partial class DllInjector : Page
    {
        public DllInjector()
        {
            InitializeComponent();
            DataContext = new InjectorViewModel();
        }
    }
}