namespace OurGame.Systems
{
    public static class PocketNames
    {
        public const string LeftPocket = "LeftPocket";
        public const string RightPocket = "RightPocket";
        public const string CentralPocket = "CentralPocket";
        public const string UpperPocket = "UpperPocket";
        public const string BottomPocket = "BottomPocket";
        public const string Hotbar = "__Hotbar__";

        public static readonly string[] Ordered =
        {
            CentralPocket,
            LeftPocket,
            RightPocket,
            UpperPocket,
            BottomPocket
        };
    }
}
