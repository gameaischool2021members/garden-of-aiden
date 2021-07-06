public static class GetJaggedLibrary
{
    public static T[][] GetJaggedExplicit<T>(T[,] raw)
    {
        int lenX = raw.GetLength(0);
        int lenY = raw.GetLength(1);

        T[][] jagged = new T[lenX][];

        for (int x = 0; x < lenX; x++)
        {
            jagged[x] = new T[lenY];
            for (int y = 0; y < lenY; y++)
            {
                jagged[x][y] = raw[x, y];
            }
        }

        return jagged;
    }

    public static T[][] GetJagged<T>(this T[,] raw)
    {
        return GetJaggedExplicit(raw);
    }
}
