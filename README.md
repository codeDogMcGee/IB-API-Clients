# Interactive Brokers API

Both CSharp and Python clients connect to IB, download trades, and then disconnect. More functionality will be added in the future. 

## CSharp Client
Microsoft.NETCore.App 3.1.0

Uses __TWS API - Version 9.76.01 - CSharp__ dll reference.

Requires an appsettings.json file with IB connection settings:
```
{
    "IbPort": "your_port",
    "IbConnectionId": "any_integer"
}
```

## Python Client
Written using Python 3.9.1, uses dependencies found in _requirements.txt_.
Requires an ib_api_config.py file in the client root directory with IB connection settings:
```
class Config:
    IB_PORT = your_port
    IB_CONNECTION_ID = any_integer
```