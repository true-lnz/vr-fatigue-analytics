> For the complete documentation index, see [llms.txt](https://emotiv.gitbook.io/cortex-api/llms.txt). Markdown versions of documentation pages are available by appending `.md` to page URLs; this page is available as [Markdown](https://emotiv.gitbook.io/cortex-api/connecting-to-the-cortex-api.md).

# Connecting to the Cortex API

On Windows and MacOS, the Cortex service is a background process that communicates with the EMOTIV headsets and the EMOTIV cloud. It is also a [WebSocket](https://en.wikipedia.org/wiki/WebSocket) server that uses the [JSON-RPC](https://www.jsonrpc.org/specification) protocol.

## WebSocket

Communicate with the Cortex API using the [WebSocket Secure](https://en.wikipedia.org/wiki/WebSocket) protocol.&#x20;

Create a **WebSocket client** and connect to **localhost** on the port **6868**, using the **wss** protocol.

Any WebSocket client should work in any programming language. You can even use a web browser plugin or an [online client](https://www.websocket.org/echo.html).

Please note that Cortex only supports the WebSocket Secure (wss) protocol. It doesn't support the plain WebSocket (ws) protocol without encryption.

{% hint style="info" %}
To try the Cortex API, open a secure WebSocket connection to **wss\://localhost:6868** and send the message **`{"id":1,"jsonrpc":"2.0","method":"getCortexInfo"}`**
{% endhint %}

## Cortex security certificate

The Cortex web socket server uses a self-signed certificate. When you install Emotiv softwares, the installer has already installed the Emotiv Root CA file and ask the system to trust it, so most of the time, your application don't need to configure anything else. However, for some programming languagues or for remote connections to Pi, you must configure a custom Certificate Authorities (CA) to trust Cortex websocket connection.

For example, on Android, you must configure the application like [`this`](https://github.com/Emotiv/cortex-v2-example/tree/mobile-example/mobile#android-1) or if you use python, you can configure the application like [this](https://github.com/Emotiv/cortex-v2-example/blob/master/python/cortex.py#L108).

Here is [the link to the Emotiv Root CA file](https://github.com/Emotiv/cortex-v2-example/blob/master/certificates/rootCA.pem).

## Remote connections

Since 2.7.1 release, we support remote connections to Cortex running on Raspberry Pi device. It requires some steps:

* Grant the permissions to a desktop machine before an app on that machine can connect to the Cortex web socket server on Raspberry Pi device, using [cortexaccess](/cortex-api/cortexaccess-tool.md) tool.
* Connect using the IP address:
  * Only applied if the IP address of your Raspberry Pi device is in the below range:&#x20;
    * 10.\[0-1].\[0-2].\[0-255]
    * 172.\[16-17].\[0-2].\[0-255]
    * 192.168.\[0-5].\[0-255]
  * Create a **WebSocket client** and connect to`<IP address of Raspberry Pi device>`

    &#x20;on the port **6868**, using the **wss** protocol.
* Connect using the DNS name:
  * Can be applied for any value of IP address of your Raspberry Pi device.
  * Add this line into the *host* file of the desktop machine:`<IP address of Raspberry Pi device> emotiv-cortex.remote`
  * Create a **WebSocket client** and connect to **emotiv-cortex.remote** on the port **6868**, using the **wss** protocol.

{% hint style="info" %}
Please note that websocket server on Raspberry Pi accepts only 1 remote connection at a time (not counting the remote connection from EMOTIVApp).
{% endhint %}

## JSON-RPC

After you have successfully opened the WebSocket connection, communicate with Cortex using the [JSON-RPC 2.0](https://www.jsonrpc.org/specification) protocol.

Call **methods**, with or without **parameters**, and Cortex sends back a **result** or an **error**.

Call an API method by sending a JSON object with these fields:

| Name    | Type               | Required | Description                                                                                                                                 |
| ------- | ------------------ | -------- | ------------------------------------------------------------------------------------------------------------------------------------------- |
| jsonrpc | `string`           | yes      | Must be "2.0"                                                                                                                               |
| method  | `string`           | yes      | The name of the API method you want to call                                                                                                 |
| params  | `object`           | no       | The parameters of the method, if it has any                                                                                                 |
| id      | `number or string` | no       | <p>The id of this request.<br>Cortex will include this id into the response message, so you can can match the response with the request</p> |

The response from Cortex is a JSON object with these fields:

| Name    | Type               | Description                                                                                              |
| ------- | ------------------ | -------------------------------------------------------------------------------------------------------- |
| jsonrpc | `string`           | Is always "2.0"                                                                                          |
| result  | `any`              | <p>If the method is successful, contains the result of the method.<br>The type depends on the method</p> |
| error   | `object`           | If the method failed, contains an error code and an error message                                        |
| id      | `number or string` | The id you included in the request                                                                       |

Here is a simple fictional example:

{% tabs %}
{% tab title="Request" %}

```javascript
{
    "id": 1,
    "jsonrpc": "2.0",
    "method": "area",
    "params": {
        "width": 6,
        "length": 7
    }
}
```

{% endtab %}

{% tab title="Response" %}

```javascript
{
    "id": 1,
    "jsonrpc": "2.0",
    "result": 42
}
```

{% endtab %}
{% endtabs %}

Please read the [JSON-RPC 2.0 specification](https://www.jsonrpc.org/specification) for details.

## Test API request and response

You can try with the most simple API of Cortex: [getCortexInfo](/cortex-api/connecting-to-the-cortex-api/getcortexinfo.md).

## Next step

The rest of this documentation will show you what methods are available, what parameters they take, and how to use these methods.

But first, you should start with the [overview of the API flow](/cortex-api/overview-of-api-flow.md).

