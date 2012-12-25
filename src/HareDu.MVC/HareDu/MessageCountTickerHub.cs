using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Hubs;

namespace HareDu
{
    [HubName("messageCountTicker")]
    public class MessageCountTickerHub : Hub
    {
        private readonly MessageCountTicker _messageCountTicker;

        public MessageCountTickerHub() : this(MessageCountTicker.Instance) { }

        public MessageCountTickerHub(MessageCountTicker messageCountTicker)
        {
            _messageCountTicker = messageCountTicker;
        }

        public IEnumerable<MessageCount> GetAllMessageCounts()
        {
            return _messageCountTicker.GetAllMessageCounts();
        }

        public string GetMarketState()
        {
            return _messageCountTicker.MarketState.ToString();
        }

        public void OpenMarket()
        {
            _messageCountTicker.OpenMarket();
        }

        public void CloseMarket()
        {
            _messageCountTicker.CloseMarket();
        }

        public void Reset()
        {
            _messageCountTicker.Reset();
        }
    }
}