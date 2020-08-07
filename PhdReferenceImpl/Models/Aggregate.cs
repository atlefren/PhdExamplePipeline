using System;

namespace PhdReferenceImpl.Models
{
    public class Aggregate<TData>
    {
        public Guid Id;
        public long Version;
        public TData Data;
    }
}