using System;
using System.Collections.Generic;
using System.Linq;
using GeoAPI.Geometries;
using PhdReferenceImpl.Models;

namespace PhdReferenceImpl.DataConflation
{
    public class DataConflator<TGeometry, TAttributesA, TAttributesB, TAttributesOut> where TGeometry: IGeometry
    {
        private readonly IConflatorQueue<TGeometry, TAttributesA, TAttributesB> _conflatorQueue;
        private readonly Func<Feature<TGeometry, TAttributesA>, Feature<TGeometry, TAttributesB>, bool> _requiresConflation;
        private readonly Func<TAttributesA, TAttributesOut> _mapAttributesA;
        private readonly Func<TAttributesB, TAttributesOut> _mapAttributesB;

        public DataConflator(
            Func<Feature<TGeometry, TAttributesA>, Feature<TGeometry, TAttributesB>, bool> requiresConflation, 
            Func<TAttributesA, TAttributesOut> mapAttributesA,
            Func<TAttributesB, TAttributesOut> mapAttributesB, IConflatorQueue<TGeometry, TAttributesA, TAttributesB> conflatorQueue)
        {
            _requiresConflation = requiresConflation;
            _mapAttributesA = mapAttributesA;
            _mapAttributesB = mapAttributesB;
            _conflatorQueue = conflatorQueue;
        }

        public IEnumerable<Feature<TGeometry, TAttributesOut>> Conflate(
            IEnumerable<Feature<TGeometry, TAttributesA>> datasetA,
            IEnumerable<Feature<TGeometry, TAttributesB>> datasetB)
        {
            var pairs = GetPairs(datasetA, datasetB);
            var toConflate = pairs.Where(p => p.NeedsConflation());
            foreach (var pair in toConflate)
            {
                _conflatorQueue.AddConflationTask(pair.A, pair.B);
            }

            return pairs
                .Where(p => !p.NeedsConflation())
                .Select(p => p.GetFeature(_mapAttributesA, _mapAttributesB));
        }

        private List<MyPair<TGeometry, TAttributesA, TAttributesB>> GetPairs(
            IEnumerable<Feature<TGeometry, TAttributesA>> datasetA,
            IEnumerable<Feature<TGeometry, TAttributesB>> datasetB)
        {
            var datasetBFeatures = datasetB.ToList();
            var res = new List<MyPair<TGeometry, TAttributesA, TAttributesB>>();
            foreach (var featureA in datasetA)
            {
                var featureB = datasetBFeatures.FirstOrDefault(b => _requiresConflation(featureA, b));
                if (featureB != null)
                {
                    datasetBFeatures.Remove(featureB);
                }
                res.Add(new MyPair<TGeometry, TAttributesA, TAttributesB>() { A = featureA, B = featureB });
            }

            res.AddRange(datasetBFeatures.Select(feature => new MyPair<TGeometry, TAttributesA, TAttributesB>() {B = feature}));
            return res;
    
        }

        internal class MyPair<TGeometry, TAttributesA, TAttributesB> where TGeometry: IGeometry
        {
            public Feature<TGeometry, TAttributesA> A { get; set; }
            public Feature<TGeometry, TAttributesB> B { get; set; }

            public bool NeedsConflation() => A != null && B != null;

            public Feature<TGeometry, TAttributesOut> GetFeature(Func<TAttributesA, TAttributesOut> mapAttributesA, Func<TAttributesB, TAttributesOut> mapAttributesB)
            {
                
                if (A != null)
                {
                    return new Feature<TGeometry, TAttributesOut>()
                    {
                        Geometry = A.Geometry,
                        Attributes = mapAttributesA(A.Attributes)
                    };
                }

                if(B != null)
                {
                    return new Feature<TGeometry, TAttributesOut>()
                    {
                        Geometry = B.Geometry,
                        Attributes = mapAttributesB(B.Attributes)
                    };
                }

                return null;
            }
        }
    }
}
