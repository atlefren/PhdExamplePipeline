using System;

namespace PhdReferenceImpl.Models
{
    public class Event<TEventData>
    {
        public Operation Operation;
        public Guid AggregateId { get; set; }
        public long Version { get; set; }
        public TEventData EventData { get; set; }
        public DateTime Timestamp { get; set; }
    }
}