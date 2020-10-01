# PhD reference implementation

This repository contains sample implementation of An Event-Based Pipeline for Geospatial Vector Data Management, that lies at the core of my PhD thesis.

This code is written for illustrative purposes, and does not consititute a fully functioning pipeline.

## Tests
A good starting point are the unit tests in the Tests-folder.

- EventCreationTest: Shows how change detection and diff creation is combined to create events, and store them using the Event Store API and message bus.
- FeatureDiffPatchTest: Shows how diffs can be created for features, using a combination of GeomDiff and JsonDiffPatch.net.
- ReadprojectionWriterTest: Shows how to set up a read projection with filters and transformations.
- DataConflatorTest: Shows how data conflation using an async micro-tasking step are envisioned to work.

