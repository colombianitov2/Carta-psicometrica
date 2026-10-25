namespace FrioCalc.Psychrometrics.Points;

// ------------------------------ Sección 3.3.1 - Paleta de colores de puntos ------------------------------

public static class PointColorPalette
{
    private static readonly string[] Colors =
    {
        "#E53935",
        "#1E88E5",
        "#43A047",
        "#FB8C00",
        "#8E24AA",
        "#00897B",
        "#6D4C41",
        "#3949AB",
        "#C0CA33",
        "#D81B60"
    };

    public static string GetColorHex(
        int pointId)
    {
        int index =
            (pointId - 1)
            % Colors.Length;

        if (index < 0)
        {
            index +=
                Colors.Length;
        }

        return Colors[index];
    }
}

// ------------------------------ Fin de sección 3.3.1 ------------------------------
