using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ReactiveStock.ActorModel.Actors.Core
{
    // TASK
    // implement a stateful agent using TPL DataFlow ActionBlock
    // The state of the Agent should be define with an initial value (seed), which can be pass as argument
    // Then, it should have an arbitrary function that transform the current state, the new message receive and return a new (or not) state

    // public class StatefulDataflowAgent<TState, TMessage> : IAgent<TMessage>

    // TASK
    // implement a stateless agent using TPL DataFlow ActionBlock
    // The agent should have an arbitrary function that process the incoming messages

    // public class StatelessDataflowAgent<TMessage> : IAgent<TMessage>

}
