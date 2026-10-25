> For the complete documentation index, see [llms.txt](https://emotiv.gitbook.io/cortex-api/llms.txt). Markdown versions of documentation pages are available by appending `.md` to page URLs; this page is available as [Markdown](https://emotiv.gitbook.io/cortex-api/headset/queryheadsets.md).

# queryHeadsets

Discover details about headsets connected to your device via USB dongle, USB cable, or Bluetooth or virtual headsets enabled through the EMOTIV Launcher. Additionally, search for a specific headset using its ID or apply a wildcard for partial matches.

## Parameters

| Name                | Type      | Required | Description                                                                                                                                            |
| ------------------- | --------- | -------- | ------------------------------------------------------------------------------------------------------------------------------------------------------ |
| id                  | `string`  | No       | A headset id or a wildcard                                                                                                                             |
| includeFlexMappings | `boolean` | No       | Set this parameter to **true** to include the mapping of each EPOC FLEX headset in the result [headset object](/cortex-api/headset/headset-object.md). |

For the parameter **id**, you can use these wildcards:

| Wildcard    | Filter                 |
| ----------- | ---------------------- |
| INSIGHT-\*  | All Insight headsets   |
| EPOC-\*     | All Epoc headsets      |
| EPOCPLUS-\* | All Epoc Plus headsets |

## Result

The result is an array of [headset objects](/cortex-api/headset/headset-object.md), or an empty array if no headset matches the request.

## Examples

### Query all the headsets

{% tabs %}
{% tab title="Request" %}

```javascript
{
    "id": 1,
    "jsonrpc": "2.0",
    "method": "queryHeadsets"
}
```

{% endtab %}

{% tab title="Response" %}

```javascript
{
    "id": 1,
    "jsonrpc": "2.0",
    "result": [
        {
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
        }
    ]
}
```

{% endtab %}
{% endtabs %}

If no headset matches your request, then the result is an empty array.

{% tabs %}
{% tab title="Request" %}

```javascript
{
    "id": 1,
    "jsonrpc": "2.0",
    "method": "queryHeadsets"
}
```

{% endtab %}

{% tab title="Response" %}

```javascript
{
    "id": 1,
    "jsonrpc": "2.0",
    "result": []
}
```

{% endtab %}
{% endtabs %}

### Query a headset by its id

{% tabs %}
{% tab title="Request" %}

```javascript
{
    "id": 1,
    "jsonrpc": "2.0",
    "method": "queryHeadsets",
    "params": {
        "id": "EPOCPLUS-3B9AXXXX"
    }
}
```

{% endtab %}

{% tab title="Response" %}

```javascript
{
    "id": 1,
    "jsonrpc": "2.0",
    "result": [
        {
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
        }
    ]
}
```

{% endtab %}
{% endtabs %}

### Query all the Insight headsets

{% tabs %}
{% tab title="Request" %}

```javascript
{
    "id": 1,
    "jsonrpc": "2.0",
    "method": "queryHeadsets",
    "params": {
        "id": "INSIGHT-*"
    }
}
```

{% endtab %}

{% tab title="Response" %}

```javascript
{
  "id": 1,
  "jsonrpc": "2.0",
  "result": [
    {
      "id": "INSIGHT-AAAA0000",
      "status": "connected",
      "connectedBy": "dongle",
      "customName": "",
      "dongle": "6ff",
      "firmware": "930",
      "headbandPosition": null,
      "motionSensors": [
        "Q0",
        "Q1",
        "Q2",
        "Q3",
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
        "memsRate": 64,
        "memsRes": 14,
        "mode": "INSIGHT"
      }
    }
  ]
}
```

{% endtab %}
{% endtabs %}

