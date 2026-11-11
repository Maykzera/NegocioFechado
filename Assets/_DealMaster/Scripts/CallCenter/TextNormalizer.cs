using System.Globalization;
using System.Text;

/// <summary>Normaliza texto em português: minúsculas, sem acentos e com espaços simples.</summary>
public static class TextNormalizer
{
    public static string Normalize(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        string decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        bool space = false;
        foreach (char c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            bool isSpace = char.IsWhiteSpace(c);
            if (isSpace && space) continue;
            sb.Append(isSpace ? ' ' : c);
            space = isSpace;
        }
        return sb.ToString().Trim();
    }

    /// <summary>Verifica se o texto normalizado contém a expressão (também normalizada).</summary>
    public static bool Has(string normalized, string phrase) =>
        !string.IsNullOrEmpty(phrase) && (" " + normalized + " ").Contains(" " + Normalize(phrase) + " ")
        || (phrase.Length > 4 && normalized.Contains(Normalize(phrase)));
}
