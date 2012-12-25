using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace HareDu
{
    public class MessageCountTicker
    {
        private readonly static Lazy<MessageCountTicker> _instance = new Lazy<MessageCountTicker>(() => new MessageCountTicker());
        private readonly static object _marketStateLock = new object();
        private readonly ConcurrentDictionary<string, MessageCount> _messageCounts = new ConcurrentDictionary<string, MessageCount>();
        private readonly double _rangePercent = .002; //stock can go up or down by a percentage of this factor on each change
        private readonly int _updateInterval = 250; //ms
        // This is used as an singleton instance so we'll never both disposing the timer
        private Timer _timer;
        private readonly object _updateStockPricesLock = new object();
        private bool _updatingStockPrices;
        private readonly Random _updateOrNotRandom = new Random();
        private MarketState _marketState = MarketState.Closed;
        private readonly Lazy<IHubConnectionContext> _clientsInstance = new Lazy<IHubConnectionContext>(() => GlobalHost.ConnectionManager.GetHubContext<MessageCountTickerHub>().Clients);

        private MessageCountTicker()
        {
            LoadMessageCounts();
        }

        public static MessageCountTicker Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        private IHubConnectionContext Clients
        {
            get { return _clientsInstance.Value; }
        }

        public MarketState MarketState
        {
            get { return _marketState; }
            private set { _marketState = value; }
        }

        public IEnumerable<MessageCount> GetAllMessageCounts()
        {
            return _messageCounts.Values;
        }

        public void OpenMarket()
        {
            if (MarketState != MarketState.Open || MarketState != MarketState.Opening)
            {
                lock (_marketStateLock)
                {
                    if (MarketState != MarketState.Open || MarketState != MarketState.Opening)
                    {
                        MarketState = MarketState.Opening;
                        _timer = new Timer(UpdateStockPrices, null, _updateInterval, _updateInterval);
                        MarketState = MarketState.Open;
                        BroadcastMarketStateChange(MarketState.Open);
                    }
                }
            }
        }

        public void CloseMarket()
        {
            if (MarketState == MarketState.Open || MarketState == MarketState.Opening)
            {
                lock (_marketStateLock)
                {
                    if (MarketState == MarketState.Open || MarketState == MarketState.Opening)
                    {
                        MarketState = MarketState.Closing;
                        if (_timer != null)
                        {
                            _timer.Dispose();
                        }
                        MarketState = MarketState.Closed;
                        BroadcastMarketStateChange(MarketState.Closed);
                    }
                }
            }
        }

        public void Reset()
        {
            lock (_marketStateLock)
            {
                if (MarketState != MarketState.Closed)
                {
                    throw new InvalidOperationException("Market must be closed before it can be reset.");
                }
                _messageCounts.Clear();
                LoadMessageCounts();
                BroadcastMarketStateChange(MarketState.Reset);
            }
        }

        private void LoadMessageCounts()
        {
            new List<MessageCount>
            {
                new MessageCount { Symbol = "MSFT", Count = 30.31m },
                new MessageCount { Symbol = "APPL", Count = 578.18m },
                new MessageCount { Symbol = "GOOG", Count = 570.30m }
            }.ForEach(messageCount => _messageCounts.TryAdd(messageCount.Symbol, messageCount));
        }

        private void UpdateStockPrices(object state)
        {
            // This function must be re-entrant as it's running as a timer interval handler
            if (_updatingStockPrices)
            {
                return;
            }

            lock (_updateStockPricesLock)
            {
                if (!_updatingStockPrices)
                {
                    _updatingStockPrices = true;

                    foreach (var messageCount in _messageCounts.Values)
                    {
                        if (UpdateStockPrice(messageCount))
                        {
                            BroadcastStockPrice(messageCount);
                        }
                    }

                    _updatingStockPrices = false;
                }
            }
        }

        private bool UpdateStockPrice(MessageCount stock)
        {
            // Randomly choose whether to udpate this stock or not
            var r = _updateOrNotRandom.NextDouble();
            if (r > .1)
            {
                return false;
            }

            // Update the stock price by a random factor of the range percent
            var random = new Random((int)Math.Floor(stock.Count));
            var percentChange = random.NextDouble() * _rangePercent;
            var pos = random.NextDouble() > .51;
            var change = Math.Round(stock.Count * (decimal)percentChange, 2);
            change = pos ? change : -change;

            stock.Count += change;
            return true;
        }

        private void BroadcastMarketStateChange(MarketState marketState)
        {
            switch (marketState)
            {
                case MarketState.Open:
                    Clients.All.marketOpened();
                    break;
                case MarketState.Closed:
                    Clients.All.marketClosed();
                    break;
                case MarketState.Reset:
                    Clients.All.marketReset();
                    break;
                default:
                    break;
            }
        }

        private void BroadcastStockPrice(MessageCount messageCount)
        {
            Clients.All.updateStockPrice(messageCount);
        }
    }

    public enum MarketState
    {
        Open,
        Opening,
        Closing,
        Closed,
        Reset
    }
}