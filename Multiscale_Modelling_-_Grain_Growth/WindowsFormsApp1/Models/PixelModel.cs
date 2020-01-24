using WindowsFormsApp1;
namespace WindowsFormsApp1.Models
{
    public class PixelModel
    {
        public Metadata.PixelState State { get; set; } = Metadata.PixelState.Default;
        public bool Block { get; set; } = false;

    }
}
