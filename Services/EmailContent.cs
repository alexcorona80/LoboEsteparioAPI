using System.Net;
using System.Text.RegularExpressions;

namespace LoboEsteparioAPI.Services;

/// <summary>Utilidades para correos HTML: detección, versión en texto plano e inserción del píxel de apertura.</summary>
public static class EmailContent
{
    private const RegexOptions Opts = RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled;

    private static readonly Regex HeadBlocks = new(@"<(head|style|script)\b.*?</\1>", Opts);
    private static readonly Regex Comments = new(@"<!--.*?-->", Opts);
    private static readonly Regex HiddenBlocks = new(@"<div[^>]*display\s*:\s*none[^>]*>.*?</div>", Opts);
    private static readonly Regex Anchors = new(@"<a\b[^>]*?href\s*=\s*""([^""]*)""[^>]*>(.*?)</a>", Opts);
    private static readonly Regex ListItems = new(@"<li\b[^>]*>", Opts);
    private static readonly Regex LineBreaks = new(@"<br\s*/?>|</(p|div|tr|h[1-6]|li|table)>", Opts);
    private static readonly Regex AnyTag = new(@"<[^>]+>", Opts);
    private static readonly Regex Spaces = new(@"[ \t ]+", Opts);
    private static readonly Regex BlankLines = new(@"\n[ \t]*(\n[ \t]*)+", Opts);
    private static readonly Regex BodyClose = new(@"</body\s*>", RegexOptions.IgnoreCase | RegexOptions.RightToLeft | RegexOptions.Compiled);

    /// <summary>Considera HTML el cuerpo que empieza con una etiqueta y contiene etiquetas de cierre.</summary>
    public static bool LooksLikeHtml(string body)
    {
        var trimmed = body.TrimStart();
        return trimmed.StartsWith('<')
               && (trimmed.Contains("</", StringComparison.Ordinal) || trimmed.Contains("/>", StringComparison.Ordinal));
    }

    /// <summary>Coloca la etiqueta del píxel justo antes de &lt;/body&gt; (o al final si no existe).</summary>
    public static string InjectPixel(string html, string pixelTag)
    {
        var match = BodyClose.Match(html);
        return match.Success ? html.Insert(match.Index, pixelTag) : html + pixelTag;
    }

    /// <summary>Versión en texto plano del HTML para la parte text/plain del correo (mejora la entregabilidad).</summary>
    public static string ToPlainText(string html)
    {
        var text = HeadBlocks.Replace(html, string.Empty);
        text = Comments.Replace(text, string.Empty);
        text = HiddenBlocks.Replace(text, string.Empty);
        text = Anchors.Replace(text, AnchorToText);
        text = ListItems.Replace(text, "• ");
        text = LineBreaks.Replace(text, "\n");
        text = AnyTag.Replace(text, string.Empty);
        text = WebUtility.HtmlDecode(text);
        text = Spaces.Replace(text, " ");
        text = string.Join("\n", text.Split('\n').Select(line => line.Trim()));
        text = BlankLines.Replace(text, "\n\n");
        return text.Trim();
    }

    private static string AnchorToText(Match match)
    {
        var href = WebUtility.HtmlDecode(match.Groups[1].Value).Trim();
        var label = WebUtility.HtmlDecode(AnyTag.Replace(match.Groups[2].Value, string.Empty)).Trim();

        if (href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            var address = href["mailto:".Length..].Split('?')[0];
            return label.Contains('@') || label.Length == 0 ? address : $"{label} ({address})";
        }

        if (href.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
        {
            return label.Length == 0 ? href["tel:".Length..] : label;
        }

        if (label.Length == 0)
        {
            return string.Empty; // ancla que solo envuelve una imagen (ej. el logotipo)
        }

        if (label.Equals(href, StringComparison.OrdinalIgnoreCase))
        {
            return href;
        }

        return $"{label} ({href})";
    }
}
