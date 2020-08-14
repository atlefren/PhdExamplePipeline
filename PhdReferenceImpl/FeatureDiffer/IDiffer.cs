namespace PhdReferenceImpl.FeatureDiffer
{
    public interface IDiffer<TData, TDiff>
    {
        TDiff Diff(TData v1, TData v2);
        TData Patch(TData v1, TDiff diff);
    }
}