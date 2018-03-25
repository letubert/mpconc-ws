﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveStock.ActorModel.Messages;
using ReactiveAgent.Agents;

namespace ReactiveStock.ActorModel.Actors
{
    // TODO : 5.11
    // Complete the agent.actor coordinator
    // Add an internal state used to register Agent
    // think about this as a parent agent where children agents can registers
    // each time a new Stock-Symbol is received as message, a new agent is crated to manage the specific stock
    // the state of the children agent could be a Collection that maps a symbol to an agent
    public class StocksCoordinatorActor_TODO
    {
        private readonly IAgent<ChartSeriesMessage> _chartingActor;

        public IAgent<StocksCoordinatorMessage> Actor { get; private set; }

        public StocksCoordinatorActor_TODO(IAgent<ChartSeriesMessage> chartingActor)
        {
            _chartingActor = chartingActor;

            // TODO
            // implement agent that handles StocksCoordinatorMessage messages
            // base on the message receive, the agent associated with the symbol in the message is enabled (WatchStock) or disable
            // Actor =
        }

        private void WatchStock(WatchStockMessage message)
        {
            // TODO
            // if the agent that handle the stock symbol in the message does not exist,
            // create a new one and add the instance to the local state
            // -- Agent can be created using the StockActor.Create function

            _chartingActor.Post(new AddChartSeriesMessage(message.StockSymbol));
        }

        private void UnWatchStock(UnWatchStockMessage message)
        {
            // TODO
            // complete method to un-subscribe
        }

    }



    #region Solution
    class StocksCoordinatorActor
    {
        private readonly IAgent<ChartSeriesMessage> _chartingActor;
        private readonly Dictionary<string, IAgent<StockAgentMessage>> _stockActors;

        public IAgent<StocksCoordinatorMessage> Actor { get; private set; }

        public StocksCoordinatorActor(IAgent<ChartSeriesMessage> chartingActor)
        {
            _chartingActor = chartingActor;
            _stockActors = new Dictionary<string, IAgent<StockAgentMessage>>();

            Actor = Agent.Start<StocksCoordinatorMessage>(message =>
            {
                switch (message)
                {
                    case WatchStockMessage msg:
                        WatchStock(msg);
                        break;
                    case UnWatchStockMessage msg:
                        UnWatchStock(msg);
                        break;
                    default:
                        throw new ArgumentException(
                           message: "message is not a recognized",
                           paramName: nameof(message));
                }
            });
        }

        private void WatchStock(WatchStockMessage message)
        {
            bool childActorNeedsCreating = !_stockActors.ContainsKey(message.StockSymbol);

            if (childActorNeedsCreating)
            {
                var newChildActor =
                    StockActor.Create(message.StockSymbol);

                _stockActors.Add(message.StockSymbol, newChildActor);
            }

            _chartingActor.Post(new AddChartSeriesMessage(message.StockSymbol));

            _stockActors[message.StockSymbol]
                .Post(new SubscribeToNewStockPricesMessage(_chartingActor));
        }

        private void UnWatchStock(UnWatchStockMessage message)
        {
            if (!_stockActors.ContainsKey(message.StockSymbol))
            {
                return;
            }

            _chartingActor.Post(new RemoveChartSeriesMessage(message.StockSymbol));

            _stockActors[message.StockSymbol]
                .Post(new UnSubscribeFromNewStockPricesMessage(_chartingActor));
        }

    }
    #endregion

}
