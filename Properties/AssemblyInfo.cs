using System.Runtime.CompilerServices;

// Allow the test project to interact with the DatabaseService test hooks without widening
// their visibility beyond internal.
[assembly: InternalsVisibleTo("YasGMP.Tests")]
