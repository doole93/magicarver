namespace MagiCarver
{
    public static class Constants
    {
        #region Strings

        public const string TITLE = "MagiCarver 0.9";
        public const string TEXT_READY = "Ready";
        public const string TEXT_WORKING = "Working...";
        public const string TEXT_REFRESHING_CACHE = "Refreshing cache...";

        #endregion

        #region Numericals

        public const double PERCANTAGE_OF_SCREEN_HEIGHT = 1;
        public const double PERCANTAGE_OF_SCREEN_WIDTH = 1;
        public const int MAX_ENERGY = 10000;
        public const int MIN_ENERGY = -10000;
        public const int DEFAULT_CACHELIMIT = 0;

        #endregion

        #region Enums

        public enum EnergyFunctions
        {
            SOBEL,
            PREWITT,
            ROBERTS,
            HOG
        } ;

        public enum Direction
        {
            VERTICAL,
            HORIZONTAL,
            BOTH,
            NONE,
            OPTIMAL
        } ;

        public enum ActionType
        {
            NONE,
            SHIRNK,
            ENLARGE
        } ;

        public enum Maps
        {
            NORMAL,
            ENERGY,
            VERTICAL_INDEX_MAP,
            HORIZONTAL_INDEX_MAP
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

        #endregion
    }
}
