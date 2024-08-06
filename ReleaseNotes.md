# Release Notes

### 1.3.0
- Added ability to specify the order in which entries are dequeued (FIFO by default). See IQueue::SetOrder and CustomOrder unit tests for usage.
- Added IQueue interface to facilitate mocks/testing
- Upgraded target to net 8.0
- Updated dependencies, particularly LiteDB 5.0.21
- Updated copyright statements