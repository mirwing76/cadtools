using System.Windows;
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

        private void OnBlockClick(object sender, MouseButtonEventArgs e)
        {
            var fe = sender as FrameworkElement;
            var item = fe?.DataContext as BlockItem;
            if (item == null) return;

            var vm = DataContext as BlockBrowserViewModel;
            if (vm == null) return;

            // 선택 + 삽입 (싱글 클릭, 중복 방지)
            vm.SelectBlock(item);
            vm.RequestInsert(item);
        }

        private void OnBlockRightClick(object sender, MouseButtonEventArgs e)
        {
            var fe = sender as FrameworkElement;
            var item = fe?.DataContext as BlockItem;
            if (item == null) return;

            var vm = DataContext as BlockBrowserViewModel;
            if (vm == null) return;

            vm.SelectBlock(item);

            var menu = new ContextMenu();

            var aliasItem = new MenuItem { Header = "Set Alias..." };
            aliasItem.Click += (s, args) =>
            {
                string current = item.Alias ?? "";
                string input = Microsoft.VisualBasic.Interaction.InputBox(
                    "Enter block alias (empty to remove):",
                    "Set Alias", current);
                if (input != current)
                    vm.SetAlias(item, input);
            };
            menu.Items.Add(aliasItem);

            var favItem = new MenuItem
            {
                Header = item.IsFavorite ? "Remove from Favorites" : "Add to Favorites"
            };
            favItem.Click += (s, args) => vm.ToggleFavorite(item);
            menu.Items.Add(favItem);

            fe.ContextMenu = menu;
            menu.IsOpen = true;
        }
    }
}
