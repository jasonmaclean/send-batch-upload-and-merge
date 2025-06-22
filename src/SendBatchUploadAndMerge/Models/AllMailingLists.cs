using System;
using System.Collections.Generic;

namespace SitecoreFundamentals.SendBatchUploadAndMerge.Models
{
    public class AllMailingLists : ResponseBase
    {
        public MailingListContext Context { get; set; }
    }
    public class MailingListContext
    {
        public List<MailingList> MailingLists { get; set; } = new List<MailingList>();
    }
    public class MailingList
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int ActiveMemberCount { get; set; }
        public int BouncedMemberCount { get; set; }
        public int RemovedMemberCount { get; set; }
        public int UnsubscribedMemberCount { get; set; }
        public int Status { get; set; }
        public List<CustomFieldDefinition> CustomFieldsDefinition { get; set; } = new List<CustomFieldDefinition>();
    }
    public class CustomFieldDefinition
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Context { get; set; }
        public bool IsRequired { get; set; }
        public int Type { get; set; }
    }
}