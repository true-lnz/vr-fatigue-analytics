> For the complete documentation index, see [llms.txt](https://emotiv.gitbook.io/cortex-api/llms.txt). Markdown versions of documentation pages are available by appending `.md` to page URLs; this page is available as [Markdown](https://emotiv.gitbook.io/cortex-api/data-subscription/subscribe.md).

# subscribe

This method is to subscribe to one or more data streams. You can call [unsubscribe](/cortex-api/data-subscription/unsubscribe.md) to cancel a subscription. After you successfully subscribe to a stream, Cortex will keep sending you [data sample objects](/cortex-api/data-subscription/data-sample-object.md).

See [Data Subscription](/cortex-api/data-subscription.md) for details.

## Parameters

| Name        | Type               | Required | Description                                                                     |
| ----------- | ------------------ | -------- | ------------------------------------------------------------------------------- |
| cortexToken | `string`           | yes      | A token returned by [authorize](/cortex-api/authentication/authorize.md).       |
| session     | `string`           | yes      | A session id returned by [createSession](/cortex-api/session/createsession.md). |
| streams     | `array of strings` | yes      | The data streams you want to subscribe to.                                      |

The parameter **streams** must contain one or more values, chosen from this list: "eeg", "mot", "dev", "eq", "pow", "met", "com",  "fac", "sys". See [Data Subscription](/cortex-api/data-subscription.md) for details.

{% hint style="info" %}
To subscribe to the "eeg" stream, you must have a paid license and [activate](/cortex-api/session/updatesession.md) the session.
{% endhint %}

## Result

The result is an object that includes these fields:

| Name    | Type               | Description                                                                                                                    |
| ------- | ------------------ | ------------------------------------------------------------------------------------------------------------------------------ |
| success | `array of objects` | For each stream you successfully subscribed to, this array contains an object that describe the data structure of this stream. |
| failure | `array of objects` | For each stream you failed to subscribed to, this array contains an object that describes the error.                           |

In case of success, you get an object with these fields:

| Name       | Type               | Description                                             |
| ---------- | ------------------ | ------------------------------------------------------- |
| streamName | `string`           | The name of the stream.                                 |
| cols       | `array of strings` | The labels of the columns that are part of this stream. |
| sid        | `string`           | The session id.                                         |

Please note that the field **cols** is especially important. This field tells you which values you will get in the data stream, and in which order. See [Data sample object](/cortex-api/data-subscription/data-sample-object.md) for details.

In case of failure, you get an object with these fields:

| Name       | Type     | Description             |
| ---------- | -------- | ----------------------- |
| streamName | `string` | The name of the stream. |
| code       | `number` | The error code.         |
| message    | `string` | The error message.      |

## Examples

Subscribe successfully to motion and performance metrics.

{% tabs %}
{% tab title="Request" %}

```javascript
{
    "id": 1,
    "jsonrpc": "2.0",
    "method": "subscribe",
    "params": {
        "cortexToken": "xxx",
        "session": "f8cb7289-9a92-438b-8281-e5fdffe8166e",
        "streams": ["met","mot"]
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
        "failure": [],
        "success": [
            {
                "cols": ["eng","exc","lex","str","rel","int","foc"],
                "sid": "f8cb7289-9a92-438b-8281-e5fdffe8166e",
                "streamName": "met"
            },
            {
                "cols": [
                    "COUNTER_MEMS",
                    "INTERPOLATED_MEMS",
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
                "sid": "f8cb7289-9a92-438b-8281-e5fdffe8166e",
                "streamName": "mot"
            }
        ]
    }
}
```

{% endtab %}
{% endtabs %}

Subscribe successfully to performance metrics, but fail to subscribe to raw EEG.

{% tabs %}
{% tab title="Request" %}

```javascript
{
    "id": 1,
    "jsonrpc": "2.0",
    "method": "subscribe",
    "params": {
        "cortexToken": "xxx",
        "session": "49eb6cb0-9df6-414a-b3e8-cfa17198daa0",
        "streams": ["met","eeg"]
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
        "failure": [
            {
                "code": -32016,
                "message": "The stream is unavailable or unsupported.",
                "streamName": "eeg"
            }
        ],
        "success": [
            {
                "cols": ["eng","exc","lex","str","rel","int","foc"],
                "sid": "49eb6cb0-9df6-414a-b3e8-cfa17198daa0",
                "streamName": "met"
            }
        ]
    }
}
```

{% endtab %}
{% endtabs %}

