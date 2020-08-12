using System;

namespace PhdReferenceImpl.Models
{
    public class Event
    {
        public Operation Operation;
        public Guid AggregateId { get; set; }
        public long Version { get; set; }
    }
}