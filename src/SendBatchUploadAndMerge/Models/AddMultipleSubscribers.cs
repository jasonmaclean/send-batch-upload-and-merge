using System.Collections.Generic;

namespace SitecoreFundamentals.SendBatchUploadAndMerge.Models
{
    public class AddMultipleSubscribers
    {
        public bool HasExternalDoubleOptIn { get; set; } = true;
        public List<AddToListSubscriber> Subscribers { get; set; } = new List<AddToListSubscriber>();
    }
    public class AddToListSubscriber
    {
        public void AddCustomField(string name, string value)
        {
            if (CustomFields == null)
                CustomFields = new List<string>();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrEmpty(value))
                return;

            CustomFields.Add($"{name.Trim()}={value}");
        }

        public string Name { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public List<string> CustomFields { get; set; } = new List<string>();
    }

    /// <summary>
    /// Identfy a column as standard, skipped or custom.    
    /// </summary>
    internal class BatchEmailListCsvColumn
    {
        public string ColumnName { get; set; }
        public bool IsStandard { get; set; } = false;
        public bool IsList { get; set; } = false;
        public bool IsSkipped { get; set; } = false;
        public bool IsStatus { get; set; } = false;
        public bool IsCustom { get; set; } = false;
    }

    internal static class Lists
    {
        internal static List<BatchEmailListCsvColumn> BatchEmailListCsvColumns()
        {
            return new List<BatchEmailListCsvColumn>
            {
                new BatchEmailListCsvColumn { ColumnName = "Name", IsStandard = true },
                new BatchEmailListCsvColumn { ColumnName = "Email", IsStandard = true },
                new BatchEmailListCsvColumn { ColumnName = "Mobile", IsStandard = true },
                new BatchEmailListCsvColumn { ColumnName = "Tags", IsList = true },
                new BatchEmailListCsvColumn { ColumnName = "Source", IsSkipped = true },
                new BatchEmailListCsvColumn { ColumnName = "Status", IsStatus = true },
                new BatchEmailListCsvColumn { ColumnName = "Preferences", IsSkipped = true },
                new BatchEmailListCsvColumn { ColumnName = "Date Added", IsSkipped = true },
                new BatchEmailListCsvColumn { ColumnName = "", IsCustom = true }
            };
        }
    }
}