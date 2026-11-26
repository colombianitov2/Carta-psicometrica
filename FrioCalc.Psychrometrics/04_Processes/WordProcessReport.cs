using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using FrioCalc.Psychrometrics.Points;
using FrioCalc.Psychrometrics.Core;

namespace FrioCalc.Psychrometrics.Processes;

// [4.8.1] Documento Word Open XML editable sin requerir Office instalado.
public static class WordProcessReport
{
    private static readonly XNamespace M="http://schemas.openxmlformats.org/officeDocument/2006/math";
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace CT="http://schemas.openxmlformats.org/package/2006/content-types";
    private static readonly XNamespace R="http://schemas.openxmlformats.org/package/2006/relationships";

    public static void Write(string fileName,double pressure,IReadOnlyList<ChartPoint> points,
        IReadOnlyList<PsychrometricProcess> processes, UnitSystem units=UnitSystem.SI)
    {
        string report=ProcessReportService.BuildMarkdown(pressure,points,processes,units);
        var body=new XElement(W+"body");
        var lines=report.Split('\n').Select(s=>s.TrimEnd('\r')).ToArray();
        for(int i=0;i<lines.Length;i++)
        {
            string line=lines[i].Trim();
            if(line.Length==0)continue;
            if(line=="## Bibliografía")
            {
                body.Add(Paragraph("Ecuaciones",2));
                foreach(var key in new[]{"W","RH","PwsExp","Atmospheric","h","v","Mdot","Heat","Condensate","Coil","MixW","MixH","SHR","BF","BFW"})
                    if(AshraeStudyCatalog.FindEquation(key) is AshraeEquation formula)
                    {
                        body.Add(Paragraph(formula.Title,3));
                        body.Add(Equation(EducationalUnits.Equation(formula,units)));
                    }
            }
            if(line.StartsWith("|",StringComparison.Ordinal))
            {
                var rows=new List<string[]>();
                while(i<lines.Length && lines[i].Trim().StartsWith("|",StringComparison.Ordinal))
                {
                    var cells=lines[i].Trim().Trim('|').Split('|').Select(x=>x.Trim()).ToArray();
                    if(!cells.All(c=>c.All(ch=>ch=='-'||ch==':'||char.IsWhiteSpace(ch)))) rows.Add(cells);
                    i++;
                }
                i--;
                if(rows.Count>0)body.Add(Table(rows));
            }
            else
            {
                int level=line.StartsWith("### ")?3:line.StartsWith("## ")?2:line.StartsWith("# ")?1:0;
                if(level>0)line=line[(level+1)..];
                line=line.TrimStart('>',' ').Replace("**","");
                body.Add(Paragraph(line,level));
            }
        }
        body.Add(new XElement(W+"sectPr",
            new XElement(W+"pgSz",new XAttribute(W+"w","16838"),new XAttribute(W+"h","11906"),new XAttribute(W+"orient","landscape")),
            new XElement(W+"pgMar",new XAttribute(W+"top","840"),new XAttribute(W+"bottom","840"),
                new XAttribute(W+"left","840"),new XAttribute(W+"right","840"),new XAttribute(W+"header","420"),new XAttribute(W+"footer","420"))));
        var doc=new XDocument(new XElement(W+"document",new XAttribute(XNamespace.Xmlns+"w",W),new XAttribute(XNamespace.Xmlns+"m",M),body));
        var types=new XDocument(new XElement(CT+"Types",
            new XElement(CT+"Default",new XAttribute("Extension","rels"),new XAttribute("ContentType","application/vnd.openxmlformats-package.relationships+xml")),
            new XElement(CT+"Default",new XAttribute("Extension","xml"),new XAttribute("ContentType","application/xml")),
            new XElement(CT+"Override",new XAttribute("PartName","/word/document.xml"),
                new XAttribute("ContentType","application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"))));
        var relations=new XDocument(new XElement(R+"Relationships",
            new XElement(R+"Relationship",new XAttribute("Id","rId1"),new XAttribute("Type","http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),
                new XAttribute("Target","word/document.xml"))));
        using var stream=new FileStream(fileName,FileMode.Create,FileAccess.Write);
        using var zip=new ZipArchive(stream,ZipArchiveMode.Create);
        void Add(string path,XDocument xml) {using var target=zip.CreateEntry(path).Open();xml.Save(target);}
        Add("[Content_Types].xml",types);Add("_rels/.rels",relations);Add("word/document.xml",doc);
    }

    private static IEnumerable<XElement> MathRuns(string value)
    {
        XElement Run(string t)=>new XElement(M+"r",new XElement(M+"t",new XAttribute(XNamespace.Xml+"space","preserve"),t));
        value=MathematicalNotation.NormalizeSymbols(value);
        int offset=0;
        // OMML editable: subíndices y superíndices reales.
        foreach(Match match in Regex.Matches(value,@"([\p{L}\p{N}*̇]+)([_^])(?:\{([^}]+)\}|([a-zA-Z0-9]+))"))
        {
            if(match.Index>offset)yield return Run(value.Substring(offset,match.Index-offset));
            bool super=match.Groups[2].Value=="^";
            yield return new XElement(M+(super?"sSup":"sSub"),
                new XElement(M+"e",Run(match.Groups[1].Value)),
                new XElement(M+(super?"sup":"sub"),Run(match.Groups[3].Success?match.Groups[3].Value:match.Groups[4].Value)));
            offset=match.Index+match.Length;
        }
        if(offset<value.Length)yield return Run(value.Substring(offset));
    }

    private static XElement Equation(AshraeEquation formula)
    {
        var math=new XElement(M+"oMath",MathRuns(formula.Left+" = "));
        if(formula.Denominator=="1")math.Add(MathRuns(formula.Numerator));
        else math.Add(new XElement(M+"f",new XElement(M+"num",MathRuns(formula.Numerator)),
            new XElement(M+"den",MathRuns(formula.Denominator))));
        return new XElement(W+"p",math);
    }

    private static XElement Paragraph(string text,int heading=0,bool cell=false,bool bold=false)
    {
        int size=heading==1?34:heading==2?26:heading==3?22:cell?19:22;
        var properties=new XElement(W+"rPr",new XElement(W+"rFonts",new XAttribute(W+"ascii","Calibri"),new XAttribute(W+"hAnsi","Calibri")),
            new XElement(W+"sz",new XAttribute(W+"val",size)));
        if(heading>0||bold)properties.AddFirst(new XElement(W+"b"));
        if(heading>0)properties.Add(new XElement(W+"color",new XAttribute(W+"val","163A5F")));
        return new XElement(W+"p",new XElement(W+"pPr",new XElement(W+"spacing",new XAttribute(W+"after",cell?"50":"100"))),
            new XElement(W+"r",properties,new XElement(W+"t",new XAttribute(XNamespace.Xml+"space","preserve"),text)));
    }

    private static XElement Table(List<string[]> rows)
    {
        int n=rows.Max(x=>x.Length);
        var borders=new XElement(W+"tblBorders",new[]{"top","left","bottom","right","insideH","insideV"}
            .Select(s=>new XElement(W+s,new XAttribute(W+"val","single"),new XAttribute(W+"sz",4),new XAttribute(W+"color","C8D4DF"))));
        var table=new XElement(W+"tbl",new XElement(W+"tblPr",new XElement(W+"tblW",new XAttribute(W+"type","pct"),new XAttribute(W+"w",5000)),borders),
            new XElement(W+"tblGrid",Enumerable.Range(0,n).Select(_=>new XElement(W+"gridCol",new XAttribute(W+"w",15000/n)))));
        for(int r=0;r<rows.Count;r++)
        {
            var tr=new XElement(W+"tr");
            if(r==0)tr.Add(new XElement(W+"trPr",new XElement(W+"tblHeader")));
            foreach(string value in rows[r].Concat(Enumerable.Repeat("",n-rows[r].Length)))
            {
                var p=new XElement(W+"tcPr");
                if(r==0)p.Add(new XElement(W+"shd",new XAttribute(W+"fill","E4EDF5")));
                tr.Add(new XElement(W+"tc",p,Paragraph(value,cell:true,bold:r==0)));
            }
            table.Add(tr);
        }
        return table;
    }
}
