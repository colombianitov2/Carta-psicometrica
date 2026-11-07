using System;

namespace FrioCalc.Psychrometrics.Points;

// [3.3.1] Colores individuales deterministas: distribución angular áurea.
public static class PointColorPalette
{
    public static string GetColorHex(int pointId)
    {
        if (pointId <= 0) throw new ArgumentOutOfRangeException(nameof(pointId));
        double hue = ((pointId - 1) * 137.50776405) % 360.0;
        const double saturation=.72, luminance=.44;
        double chroma=(1-Math.Abs(2*luminance-1))*saturation;
        double sector=hue/60.0;
        double x=chroma*(1-Math.Abs(sector%2-1));
        double r=0,g=0,b=0;
        if (sector<1){r=chroma;g=x;}
        else if(sector<2){r=x;g=chroma;}
        else if(sector<3){g=chroma;b=x;}
        else if(sector<4){g=x;b=chroma;}
        else if(sector<5){r=x;b=chroma;}
        else {r=chroma;b=x;}
        double m=luminance-chroma/2;
        int ri=(int)Math.Round((r+m)*255);
        int gi=(int)Math.Round((g+m)*255);
        int bi=(int)Math.Round((b+m)*255);
        return $"#{Math.Clamp(ri,0,255):X2}{Math.Clamp(gi,0,255):X2}{Math.Clamp(bi,0,255):X2}";
    }
}
