using System;
using System.Collections.Generic;

namespace SitecoreFundamentals.SendBatchUploadAndMerge.Models
{
    public class MailingListSubscribers : ResponseBase
    {
        public SubscriberContext Context { get; set; }
    }
    public class SubscriberContext
    {
        public List<MailingListSubscriber> Subscribers { get; set; } = new List<MailingListSubscriber>();
    }
    public class MailingListSubscriber
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public DateTime? UnsubscribedOn { get; set; }
        public int SubscribeType { get; set; }
        public int SubscribeMethod { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public List<MailingListSubscriberCustomField> CustomFields { get; set; } = new List<MailingListSubscriberCustomField>();
    }
    public class MailingListSubscriberCustomField
    {
        public Guid CustomFieldID { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }
}