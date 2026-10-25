> For the complete documentation index, see [llms.txt](https://emotiv.gitbook.io/cortex-api/llms.txt). Markdown versions of documentation pages are available by appending `.md` to page URLs; this page is available as [Markdown](https://emotiv.gitbook.io/cortex-api/session/createsession.md).

# createSession

This method is to open a session with an EMOTIV headset.

To open a session with a headset, the status of the headset must be "connected". If the status is "discovered", then you must call [controlDevice](/cortex-api/headset/controldevice.md) to connect the headset. You cannot open a session with a headset connected by a USB cable. You can use [queryHeadsets](/cortex-api/headset/queryheadsets.md) to check the status and connection type of the headset.

You can use [updateSession](/cortex-api/session/updatesession.md) to activate or close a session.

See [Sessions](/cortex-api/session.md) for details.

{% hint style="info" %}
Make sure the headset has the status "connected" before you call this method. Use [queryHeadsets](/cortex-api/headset/queryheadsets.md) to check the status of the headset.
{% endhint %}

## Parameters

| Name        | Type     | Required | Description                                                                                                                                                   |
| ----------- | -------- | -------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| cortexToken | `string` | yes      | A token returned by [authorize](/cortex-api/authentication/authorize.md).                                                                                     |
| status      | `string` | yes      | Must be "open" or "active".                                                                                                                                   |
| headset     | `string` | no       | A headset id returned by [queryHeadsets](/cortex-api/headset/queryheadsets.md). If you omit this parameter, then Cortex will use the first connected headset. |

If the status is "open", then the session won't be activated. You can activate the session later by calling [updateSession](/cortex-api/session/updatesession.md).

## Result

The result is a [session object](/cortex-api/session/session-object.md) representing the created session.

## Examples

Open a session.

{% tabs %}
{% tab title="Request" %}

```javascript
{
    "id": 1,
    "jsonrpc": "2.0",
    "method": "createSession",
    "params": {
        "cortexToken": "xxx",
        "headset": "EPOCPLUS-3B9AXXX",
        "status": "open"
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
            "firmware": "625",
            "id": "EPOCPLUS-3B9AXXXX",
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
                "F7",
                "F3",
                "FC5",
                "T7",
                "P7",
                "O1",
                "O2",
                "P8",
                "T8",
                "FC6",
                "F4",
                "F8",
                "AF4"
            ],
            "settings": {
                "eegRate": 256,
                "eegRes": 16,
                "memsRate": 64,
                "memsRes": 16,
                "mode": "EPOCPLUS"
            },
            "status": "connected"
        },
        "id": "8bfc26de-754b-4c1f-9771-acfd1a7da02c",
        "license": "",
        "owner": "jon.snow",
        "recordIds": [],
        "recording": false,
        "started": "2019-05-29T14:18:45.108+07:00",
        "status": "opened",
        "stopped": "",
        "streams": []
    }
}
```

{% endtab %}
{% endtabs %}

Open a session and activate it.

{% tabs %}
{% tab title="Request" %}

```javascript
{
    "id": 1,
    "jsonrpc": "2.0",
    "method": "createSession",
    "params": {
        "cortexToken": "xxx",
        "headset": "EPOCPLUS-3B9AXXX",
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
    }
}
```

{% endtab %}
{% endtabs %}

