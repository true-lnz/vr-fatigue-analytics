> For the complete documentation index, see [llms.txt](https://emotiv.gitbook.io/cortex-api/llms.txt). Markdown versions of documentation pages are available by appending `.md` to page URLs; this page is available as [Markdown](https://emotiv.gitbook.io/cortex-api/authentication/getlicenseinfo.md).

# getLicenseInfo

This method returns information about the license currently used by your application.&#x20;

## Parameters

| Name        | Type     | Required | Description                                                              |
| ----------- | -------- | -------- | ------------------------------------------------------------------------ |
| cortexToken | `string` | yes      | A token returned by [authorize](/cortex-api/authentication/authorize.md) |

## Result

The result is an object containing a **license** object and the **isOnline** flag.

| Name     | Type      | Description                                                                                                                                                 |
| -------- | --------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------- |
| license  | `object`  | An object containing all the information about the current license                                                                                          |
| isOnline | `boolean` | <p>True, if Cortex was able to get the license information from the EMOTIV cloud</p><p>False, if Cortex got the license information for its local cache</p> |

## Example

{% tabs %}
{% tab title="Request" %}

```javascript
{
    "id": 1,
    "jsonrpc": "2.0",
    "method": "getLicenseInfo",
    "params": {
        "cortexToken": "xxx"
    }
}
```

{% endtab %}

{% tab title="Response" %}

```javascript
{
    "id": 1,
    "jsonrpc": "2.0",
    "result": {
        "isOnline":true,
        "license":{
            "applications":["com.emotiv.emotivpro"],
            "billingFrom":"2018-08-23T07:00:00.000+07:00",
            "billingTo":"2019-09-23T07:00:00.000+07:00",
            "deviceInfo":{
                "deviceLimit":15,
                "devicesPerSeat":3,
                "sessionLimit":{"day":null,"month":null,"year":null}
            },
            "expired":false,
            "extenderLimit":30,
            "hardLimitTime":"2019-07-01T06:59:59.999+07:00",
            "isCommercial":false,
            "licenseId":"xxx",
            "licenseName":"PRO license",
            "localQuota":0,
            "maxDebit":null,
            "scopes":["eeg","pm"],
            "seatCount":5,
            "sessionCount":0,
            "softLimitTime":"2019-06-24T06:59:59.999+07:00",
            "totalDebit":0,
            "totalRegisteredDevices":1,
            "validFrom":"2018-08-23T07:00:00.000+07:00",
            "validTo":"2019-09-24T06:59:59.999+07:00"
        }
    }
}
```

{% endtab %}
{% endtabs %}

