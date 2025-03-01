using System.Windows.Controls;

namespace DarkHub
{
    public partial class CrunchyrollAcc : Page
    {
        public CrunchyrollAcc()
        {
            InitializeComponent();
            GoogleSheetView.Source = new Uri("https://docs.google.com/spreadsheets/d/1jAhc4FiczoKC2TNygBuA9yc3Rep9w5ewUEiIOU3mRbw/edit?usp=sharing");
        }
    }
}