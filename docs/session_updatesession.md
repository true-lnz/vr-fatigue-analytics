> For the complete documentation index, see [llms.txt](https://emotiv.gitbook.io/cortex-api/llms.txt). Markdown versions of documentation pages are available by appending `.md` to page URLs; this page is available as [Markdown](https://emotiv.gitbook.io/cortex-api/session/updatesession.md).

# updateSession

This method is to activate or close a session. See [Sessions](/cortex-api/session.md) for details.

## Parameters

| Name        | Type     | Required | Description                                                                     |
| ----------- | -------- | -------- | ------------------------------------------------------------------------------- |
| cortexToken | `string` | yes      | The token returned by [authorize](/cortex-api/authentication/authorize.md).     |
| session     | `string` | yes      | A session id returned by [createSession](/cortex-api/session/createsession.md). |
| status      | `string` | yes      | Must be "active" or "close".                                                    |

## Result

The result is a [session object](/cortex-api/session/session-object.md) representing the updated session.

## Examples

### Activate a session

{% tabs %}
{% tab title="Request" %}

```javascript
{
    "id": 1,
    "jsonrpc": "2.0",
    "method": "updateSession",
    "params": {
        "cortexToken": "xxx",
        "session": "38fef6d7-979b-420b-aa25-dd60ef5edc02",
        "status": "active"
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
        "id": "38fef6d7-979b-420b-aa25-dd60ef5edc02",
        "status":"activated",
        ...
    }
}
```

{% endtab %}
{% endtabs %}

### Close a session

{% tabs %}
{% tab title="Request" %}

```javascript
{
    "id": 1,
    "jsonrpc": "2.0",
    "method": "updateSession",
    "params": {
        "cortexToken": "xxx",
        "session": "38fef6d7-979b-420b-aa25-dd60ef5edc02",
        "status": "close"
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
        "appId": "com.jon.snow.winterfell-app",
        "headset": {
            "connectedBy": "dongle",
            "customName": "",
            "dongle": "6ff",
            "firmware": "925",
            "id": "INSIGHT-5A68XXXX",
            "motionSensors": [
                "GYROX",
                "GYROY",
                "GYROZ",
                "ACCX",
                "ACCY",
                "ACCZ",
                "MAGX",
                "MAGY",
                "MAGZ"
            ],
            "sensors": [
                "AF3",
                "T7",
                "Pz",
                "T8",
                "AF4"
            ],
            "settings": {
                "eegRate": 128,
                "eegRes": 14,
                "memsRate": 128,
                "memsRes": 14,
                "mode": "UNKNOWN"
            },
            "status": "connected"
        },
        "id": "38fef6d7-979b-420b-aa25-dd60ef5edc02",
        "license": "xxx",
        "owner": "jon.snow",
        "recordIds": [
            "d8fe7658-71f1-4cd6-bb5d-f6775b03438f"
        ],
        "recording": false,
        "started": "2019-06-06T11:41:53.168+07:00",
        "status": "closed",
        "stopped": "2019-06-06T11:42:23.531+07:00",
        "streams": []
    }
}
```

{% endtab %}
{% endtabs %}

