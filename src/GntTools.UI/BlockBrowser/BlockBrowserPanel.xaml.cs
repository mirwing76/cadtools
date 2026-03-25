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

        private void OnBlockMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount < 2) return;

            var fe = sender as FrameworkElement;
            var item = fe?.DataContext as BlockItem;
            if (item == null) return;

            var vm = DataContext as BlockBrowserViewModel;
            vm?.InsertBlock(item);
        }

        private void OnBlockRightClick(object sender, MouseButtonEventArgs e)
        {
            var fe = sender as FrameworkElement;
            var item = fe?.DataContext as BlockItem;
            if (item == null) return;

            var vm = DataContext as BlockBrowserViewModel;
            if (vm == null) return;

            var menu = new ContextMenu();

            // 별칭 설정
            var aliasItem = new MenuItem { Header = "별칭 설정..." };
            aliasItem.Click += (s, args) =>
            {
                string current = item.Alias ?? "";
                string input = Microsoft.VisualBasic.Interaction.InputBox(
                    "블록 별칭을 입력하세요 (빈 값이면 삭제):",
                    "별칭 설정", current);
                if (input != current)
                    vm.SetAlias(item, input);
            };
            menu.Items.Add(aliasItem);

            // 즐겨찾기 토글
            var favItem = new MenuItem
            {
                Header = item.IsFavorite ? "즐겨찾기 제거" : "즐겨찾기 추가"
            };
            favItem.Click += (s, args) => vm.ToggleFavorite(item);
            menu.Items.Add(favItem);

            fe.ContextMenu = menu;
            menu.IsOpen = true;
        }
    }
}
