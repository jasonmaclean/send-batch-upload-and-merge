using Sitecore.Configuration;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;

namespace SitecoreFundamentals.SendBatchUploadAndMerge.Helpers
{
    public class RoleDefinition
    {
        public static string MasterOrWebDatabase()
        {
            var appSetting = ConfigurationManager.AppSettings["role:define"];

            var instanceRoles = appSetting.Split("|,;"
                .ToCharArray())
                .Select(r => Regex.Match(r, "^\\s*(\\S*)\\s*$").Groups[1].Value)
                .Where(s => s.Length > 0)
                .Select(x => x.ToLowerInvariant())
                .Distinct()
                .ToList();

            if (instanceRoles.Contains(SitecoreRole.ContentDelivery.Name.ToLower()))
                return "web";

            if (instanceRoles.Contains(SitecoreRole.ContentManagement.Name.ToLower()) || instanceRoles.Contains(SitecoreRole.Standalone.Name.ToLower()))
                return "master";

            return string.Empty;
        }
   }
}