using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using UnpTracker.Data;

namespace UnpTracker.Models;

public static class Utilities
{
    public static bool IsValidEmail(this string email)
    {
        var trimmedEmail = email.Trim();

        if (trimmedEmail.EndsWith(".")) return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(trimmedEmail);
            return addr.Address == trimmedEmail;
        }
        catch
        {
            return false;
        }
    }

    public static IEnumerable<string> GetValidUnps(this IEnumerable<string> unps)
    {
        return unps.Where(unp => unp.IsVaildUnp());
    }

    public static bool IsVaildUnp(this string unp)
    {
        return Regex.IsMatch(unp, @"^\d{9}$");
    }

    public static bool IsInLocalDb(this string unp, NutshellContext dbContext)
    {
        if (!unp.IsVaildUnp()) return false;

        return dbContext.LocalPayers
        .Include(lp => lp.Payer)
        .Any(lp => lp.Payer.Unp == unp);
    }

    public static async Task<bool> IsInStateDb(this string unp)
    {
        if (!unp.IsVaildUnp()) return false;
        
        var client = new HttpClient();
        string requestUri = $"http://grp.nalog.gov.by/api/grp-public/data?unp={unp}&charset=UTF-8&type=json";
        
        try
        {
            HttpResponseMessage response = await client.GetAsync(requestUri);
            if (!response.IsSuccessStatusCode) return false;
        }
        catch
        {
            return false;
        }

        return true;
    }
}