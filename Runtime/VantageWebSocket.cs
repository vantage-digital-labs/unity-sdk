using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace VantageLabs.Core
{
    internal class VantageWebSocket
    {
        private ClientWebSocket _ws;
        private readonly string _endpoint;
        private readonly string _apiKey;
        private CancellationTokenSource _cts;

        public event Action OnOpen;
        public event Action<string> OnMessage;
        public event Action<string> OnError;
        public event Action OnClose;

        public bool IsConnected => _ws?.State == WebSocketState.Open;

        public VantageWebSocket(string endpoint, string apiKey)
        {
            _endpoint = endpoint.Replace("https://", "wss://");
            _apiKey = apiKey;
        }

        public async void Connect()
        {
            try
            {
                _cts = new CancellationTokenSource();
                _ws = new ClientWebSocket();
                _ws.Options.SetRequestHeader("Authorization", $"Bearer {_apiKey}");
                _ws.Options.SetRequestHeader("X-SDK-Version", "2.4.0-unity");

                await _ws.ConnectAsync(new Uri($"{_endpoint}/v2/ws"), _cts.Token);
                OnOpen?.Invoke();
                _ = ReceiveLoop();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
            }
        }

        public async Task Send(string message)
        {
            if (_ws?.State != WebSocketState.Open) return;

            var bytes = Encoding.UTF8.GetBytes(message);
            await _ws.SendAsync(new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text, true, _cts.Token);
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[4096];
            try
            {
                while (_ws.State == WebSocketState.Open)
                {
                    var result = await _ws.ReceiveAsync(
                        new ArraySegment<byte>(buffer), _cts.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        OnClose?.Invoke();
                        break;
                    }

                    var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    OnMessage?.Invoke(msg);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
            }
        }

        public void Disconnect()
        {
            _cts?.Cancel();
            if (_ws?.State == WebSocketState.Open)
            {
                _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
        }
    }
}
