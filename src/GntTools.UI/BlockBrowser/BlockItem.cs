using System.Windows.Media;
using Autodesk.AutoCAD.DatabaseServices;

namespace GntTools.UI.BlockBrowser
{
    public class BlockItem
    {
        public string Name { get; set; }
        public ObjectId BlockId { get; set; }
        public ImageSource Thumbnail { get; set; }
        public string Alias { get; set; }
        public bool IsFavorite { get; set; }

        /// <summary>별칭 있으면 별칭, 없으면 블록이름</summary>
        public string DisplayName => string.IsNullOrEmpty(Alias) ? Name : Alias;

        /// <summary>즐겨찾기면 ★, 아니면 빈 문자</summary>
        public string FavoriteStar => IsFavorite ? "★ " : "";

        /// <summary>리스트 모드 서브텍스트: 별칭 있으면 원래이름, 없으면 빈값</summary>
        public string SubText => string.IsNullOrEmpty(Alias) ? "" : Name;
    }
}
