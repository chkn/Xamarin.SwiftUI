# Tooling Notes for Xamarin.SwiftUI

## Nullability

C# nullable reference types should be enabled, as nullable values are automatically bridged to Swift Optionals. If `null` is passed in any other context, an exception is raised. To take advantage of this automatic bridging, the C# compiler must not be configured to remove nullable metadata for private members (https://github.com/dotnet/roslyn/blob/master/docs/features/nullable-metadata.md#private-members).

F# Option and ValueOption are also automatically bridged as Swift Optional. For automatic bridging of F# ValueOption, you must be using a version of `FSharp.Core` newer than 4.5.2.

## Body Property

We should have an analyzer to ensure the View.Body property is correctly implemented.
