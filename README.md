# DotNet-Retry-Example

This repository provides an example of how to implement a custom `HttpClient` handler for retrying HTTP requests in the DocuWare platform server client. The example demonstrates handling HTTP 502 (Bad Gateway) and HTTP 504 (Gateway Timeout) errors by retrying the requests a specified number of times with a delay between each retry.

## Project Structure

The solution consists of multiple projects targeting different .NET frameworks:

- **retry-example-error-502-and-504-dot-net8**: A .NET 8.0 console application demonstrating the retry policy.
- **retry-example-error-502-and-504-dot-net-framework-472**: A .NET Framework 4.7.2 console application demonstrating the retry policy.
- **retry-example-error-502-and-504-retry-policy-dot-net-standard-2**: A .NET Standard 2.0 library implementing the retry policy.
- **retry-example-error-502-and-504-tests-dot-net8**: A .NET 8.0 test project containing unit tests for the retry policy.

## Getting Started

### Prerequisites

- .NET SDK 8.0 or later
- Visual Studio 2022 or later

### Building the Solution

To build the solution, run the following command in the root directory:

```sh
dotnet build src/retry-example-error-502-and-504/retry-example-error-502-and-504.sln
```
