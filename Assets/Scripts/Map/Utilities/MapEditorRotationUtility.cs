public static class MapEditorRotationUtility
{
    public static int NormalizeQuarterTurn(int rotation)
    {
        int normalized = rotation % 360;

        if (normalized < 0)
        {
            normalized += 360;
        }

        return (normalized / 90) * 90;
    }
}
