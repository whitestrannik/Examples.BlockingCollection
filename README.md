# Examples.BlockingCollection
An example of implementation simple blocking collection:
- it's just example - so is not suitable for real code;
- it has only Take\Add\GetConsumerEnumerable methods without cancellation, timeout etc.;
- does not use any locks;
- has CompleteAdding semantics like in original BlockingCollection;
- has some unit tests but not fully tested;

