using System.Windows.Media;
using Autodesk.AutoCAD.DatabaseServices;

namespace GntTools.UI.BlockBrowser
{
    public class BlockItem
    {
        public string Name { get; set; }
        public ObjectId BlockId { get; set; }
        public ImageSource Thumbnail { get; set; }
    }
}
