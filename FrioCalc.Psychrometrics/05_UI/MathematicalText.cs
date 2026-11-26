using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using FrioCalc.Psychrometrics.Processes;

namespace FrioCalc.Psychrometrics.UI;

// [5.9] Composición WPF de fracciones anidadas, índices y exponentes.
// No se modifican las cifras ni se interpretan las barras de las unidades.
public static class MathematicalText
{
    private static readonly Regex Script=new(@"([_^])(?:\{([^}]+)\}|([a-zA-Z0-9]+))",RegexOptions.Compiled);
    public static void Format(TextBlock block,string value)
    {
        string text=MathematicalNotation.Prepare(value);
        block.Inlines.Clear();
        if(!Script.IsMatch(text)&&!text.Contains('⟦')){block.Text=text;return;}
        Add(block,text,0);
    }

    private static void Add(TextBlock target,string text,int depth)
    {
        int i=0;
        while(i<text.Length)
        {
            var script=Script.Match(text,i);
            int nextFraction=text.IndexOf('⟦',i);
            int nextScript=script.Success?script.Index:int.MaxValue;
            int fractionIndex=nextFraction>=0?nextFraction:int.MaxValue;
            int next=Math.Min(nextScript,fractionIndex);
            if(next==int.MaxValue){target.Inlines.Add(new Run(text[i..]));break;}
            if(next>i)target.Inlines.Add(new Run(text[i..next]));
            if(next==fractionIndex&&depth<8&&TryReadFraction(text,next,out int separator,out int end))
            {
                double size=Math.Max(9,target.FontSize*.95);
                TextBlock Part(string value)
                {
                    var part=new TextBlock{FontFamily=new FontFamily("Cambria Math"),FontSize=size,
                        TextAlignment=TextAlignment.Center,TextWrapping=TextWrapping.Wrap,MaxWidth=460};
                    Add(part,value,depth+1);return part;
                }
                var fraction=new StackPanel{Orientation=Orientation.Vertical,Margin=new Thickness(2,1,2,1)};
                fraction.Children.Add(Part(text[(next+1)..separator]));
                fraction.Children.Add(new Border{Height=1,Background=target.Foreground,Margin=new Thickness(1,2,1,2)});
                fraction.Children.Add(Part(text[(separator+1)..end]));
                target.Inlines.Add(new InlineUIContainer(fraction){BaselineAlignment=BaselineAlignment.Center});
                i=end+1;continue;
            }
            if(next==nextScript)
            {
                string content=script.Groups[2].Success?script.Groups[2].Value:script.Groups[3].Value;
                target.Inlines.Add(new Run(content){FontFamily=new FontFamily("Cambria Math"),
                    BaselineAlignment=script.Groups[1].Value=="^"?BaselineAlignment.Superscript:BaselineAlignment.Subscript,
                    FontSize=target.FontSize*.75});
                i=script.Index+script.Length;continue;
            }
            target.Inlines.Add(new Run(text[next].ToString()));i=next+1;
        }
    }

    private static bool TryReadFraction(string text,int start,out int separator,out int end)
    {
        separator=-1;end=-1;int level=0;
        for(int i=start;i<text.Length;i++)
        {
            if(text[i]=='⟦')level++;
            else if(text[i]=='⟧')
            {
                level--;
                if(level==0){end=i;return separator>start&&separator<end;}
            }
            else if(text[i]=='¦'&&level==1)separator=i;
        }
        return false;
    }
}
