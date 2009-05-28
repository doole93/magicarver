namespace MagiCarver
{
    public class Constants
    {
        public const string TITLE = "MagiCarver 0.1a";

        public const double PERCANTAGE_OF_SCREEN_HEIGHT = 1;
        public const double PERCANTAGE_OF_SCREEN_WIDTH = 1;

        public const string TEXT_READY = "Ready";
        public const string TEXT_WORKING = "Working...";

        public enum Direction
        {
            VERTICAL,
            HORIZONTAL,
            OPTIMAL
        } ;

        public enum Maps
        {
            NORMAL,
            ENERGY,
            HORIZONTAL_INDEX,
            VERTICAL_INDEX
        } ;

        public enum EnergyType
        {
            NORMAL,
            MAX,
            MIN
        }

        public enum NeighbourType
        {
            LEFT,
            RIGHT,
            STRAIGHT
        } ;

        public enum SeamPixelDirection
        {
            STRAIGHT,
            LEFT,
            RIGHT
        } ;
    }
}
