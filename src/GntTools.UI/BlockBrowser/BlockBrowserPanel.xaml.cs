using System.Windows.Controls;
using System.Windows.Input;

namespace GntTools.UI.BlockBrowser
{
    public partial class BlockBrowserPanel : UserControl
    {
        public BlockBrowserPanel()
        {
            InitializeComponent();
        }

        private void OnBlockDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount < 2) return;

            var fe = sender as System.Windows.FrameworkElement;
            var item = fe?.DataContext as BlockItem;
            if (item == null) return;

            var vm = DataContext as BlockBrowserViewModel;
            vm?.InsertBlock(item);
        }
    }
}
