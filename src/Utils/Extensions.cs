using System.Text.RegularExpressions;

namespace PainelUtilidades.Utils.Extensions;

public static class Extensions
{
    public static string FormatarCnpj(this string cnpj)
    {
        var r = new Regex("[^0-9a-zA-Z]+");
        return r.Replace(cnpj, "");
    }
}