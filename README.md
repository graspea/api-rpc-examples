# api-rpc-examples
Examples for RPC API in C# and Python.

## For more documentation check:
- C#:
    - https://github.com/microsoft/vs-streamjsonrpc/blob/master/doc/index.md
- Python
    - Client: https://jsonrpcclient.readthedocs.io/en/latest/ 
    - Server: https://jsonrpcserver.readthedocs.io/en/latest/
  
## Thread-safety considerations
C#'s JsonRpc always invokes local methods by posting to its SynchronizationContext.

