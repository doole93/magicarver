namespace MagiCarver
{
    public class Constants
    {
        #region Strings

        public const string TITLE = "MagiCarver 0.5";
        public const string TEXT_READY = "Ready";
        public const string TEXT_WORKING = "Working...";
        public const string TEXT_REFRESHING_CACHE = "Refreshing cache...";

        #endregion

        #region Numericals

        public const double PERCANTAGE_OF_SCREEN_HEIGHT = 1;
        public const double PERCANTAGE_OF_SCREEN_WIDTH = 1;
        public const int MAX_ENERGY = 10000;
        public const int MIN_ENERGY = -10000;
        public const int VALIDATION_FACTOR = 100;

        #endregion

        #region Enums

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
            ENERGY
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
