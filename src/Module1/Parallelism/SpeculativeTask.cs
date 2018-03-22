using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Parallelism
{
    public class SpeculativeTask
    {
        public Temperature SpeculativeTempCityQuery(string city, params Uri[] weatherServices)
        {
            var cts = new CancellationTokenSource();
            var tasks =
            (from uri in weatherServices
             select Task.Factory.StartNew<Temperature>(() =>
                        queryService(uri, city), cts.Token)).ToArray();

            int taskIndex = Task.WaitAny(tasks);
            Temperature tempCity = tasks[taskIndex].Result;
            cts.Cancel();
            return tempCity;
        }

        private Temperature queryService(Uri uri, string city)
        {
            throw new NotImplementedException();
        }
    }


    public struct Temperature
    {
        Temperature(float temperature)
        {
            Temp = temperature;
        }

        public float Temp { get; }

    }
}
