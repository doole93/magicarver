namespace MagiCarver
{
    public class Constants
    {
        public const string TITLE = "MagiCarver 0.1a";

        public const double PERCANTAGE_OF_SCREEN_HEIGHT = 0.8;
        public const double PERCANTAGE_OF_SCREEN_WIDTH = 0.8;

        public const string TEXT_READY = "Ready";
        public const string TEXT_WORKING = "Wroking...";

        public enum Direction
        {
            VERTICAL,
            HORIZONTAL,
            OPTIMAL
        } ;

        public enum SeamPixelDirection
        {
            STRAIGHT,
            LEFT,
            RIGHT
        } ;
    }
}
