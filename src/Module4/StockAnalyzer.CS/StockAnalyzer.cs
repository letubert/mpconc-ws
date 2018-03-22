using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Diagnostics;
using System.Net.Http;
using Functional.Async;

namespace StockAnalyzer.CS
{
    public struct StockData
    {
        public StockData(DateTime date, double open, double high, double low, double close)
        {
            Date = date;
            Open = open;
            High = high;
            Low = low;
            Close = close;
        }

        public DateTime Date { get; }
        public Double Open { get; }
        public Double High { get; }
        public Double Low { get; }
        public Double Close { get; }
    }

    public class StockAnalyzer
    {
        public static readonly string[] stocks = new[] { "APPL", "AMZN", "FB", "GOOG", "MSFT" };

        //  The Or combinator applies to falls back behavior
        Func<string, string> googleSourceUrl = (symbol) =>
            $"http://www.google.com/finance/historical?q={symbol}&output=csv";

        Func<string, string> yahooSourceUrl = (symbol) =>
            $"http://ichart.finance.yahoo.com/table.csv?s={symbol}";

        //  Stock prices history analysis
        async Task<StockData[]> ConvertStockHistory(string stockHistory)
        {
            return await Task.Run(() =>
            {
                string[] stockHistoryRows =
                    stockHistory.Split(Environment.NewLine.ToCharArray(),
                                       StringSplitOptions.RemoveEmptyEntries);
                return (from row in stockHistoryRows.Skip(1)
                        let cells = row.Split(',')
                        let date = DateTime.Parse(cells[0])
                        let open = double.Parse(cells[1])
                        let high = double.Parse(cells[2])
                        let low = double.Parse(cells[3])
                        let close = double.Parse(cells[4])
                        select new StockData(date, open, high, low, close)
                       ).ToArray();
            });
        }

        async Task<string> DownloadStockHistory(string symbol)
        {
            string url = googleSourceUrl(symbol);

            var request = WebRequest.Create(url);
            using (var response = await request.GetResponseAsync()
                                              .ConfigureAwait(false))
            using (var reader = new StreamReader(response.GetResponseStream()))
                return await reader.ReadToEndAsync().ConfigureAwait(false);
        }

        async Task<Tuple<string, StockData[]>> ProcessStockHistory(string symbol)
        {
            string stockHistory = await DownloadStockHistory(symbol);
            StockData[] stockData = await ConvertStockHistory(stockHistory);
            return Tuple.Create(symbol, stockData);
        }

        public async Task AnalyzeStockHistory(string[] stockSymbols)
        {
            var sw = Stopwatch.StartNew();

            IEnumerable<Task<Tuple<string, StockData[]>>> stockHistoryTasks =
              stockSymbols.Select(stock => ProcessStockHistory(stock));

            var stockHistories = new List<Tuple<string, StockData[]>>();
            foreach (var stockTask in stockHistoryTasks)
                stockHistories.Add(await stockTask);

            ShowChart(stockHistories, sw.ElapsedMilliseconds);
        }

        async Task<string> DownloadStockHistory(string symbol,
                                                CancellationToken token)
        {
            string stockUrl = googleSourceUrl(symbol);
            var request = await new HttpClient().GetAsync(stockUrl, token);
            return await request.Content.ReadAsStringAsync();
        }
        async Task AnalyzeStockHistory(string[] stockSymbols,
                               CancellationToken token)
        {
            var sw = Stopwatch.StartNew();

            //  Cancellation of Asynchronous operation manual checks
            List<Task<Tuple<string, StockData[]>>> stockHistoryTasks =
                stockSymbols.Select(async symbol =>
                {
                    var request = HttpWebRequest.Create(googleSourceUrl(symbol));
                    using (var response = await request.GetResponseAsync())
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        token.ThrowIfCancellationRequested();

                        var csvData = await reader.ReadToEndAsync();
                        var prices = await ConvertStockHistory(csvData);

                        token.ThrowIfCancellationRequested();
                        return Tuple.Create(symbol, prices.ToArray());
                    }
                }).ToList();

            await Task.WhenAll(stockHistoryTasks)
                .ContinueWith(stockData => ShowChart(stockData.Result, sw.ElapsedMilliseconds), token);
        }

        //  The Bind operator in action
        // TODO : 4.7
        // take a look at the Bind operator
        // replace using SelectMany and then use the Linq expression semantic (from ** in)
        async Task<Tuple<string, StockData[]>> ProcessStockHistoryBind(string symbol)
        {
            return await DownloadStockHistory(symbol)
                    .Bind(stockHistory => ConvertStockHistory(stockHistory))
                    .Bind(stockData => Task.FromResult(Tuple.Create(symbol,
                                                               stockData)));
        }

        async Task<string> DownloadStockHistory(Func<string, string> sourceStock,
                                                                    string symbol)
        {
            string stockUrl = sourceStock(symbol);
            var request = WebRequest.Create(stockUrl);
            using (var response = await request.GetResponseAsync())
            using (var reader = new StreamReader(response.GetResponseStream()))
                return await reader.ReadToEndAsync();
        }

        // TODO : 4.9
        // Process the Stock-History analysis for all the stocks in parallel
        public async Task ProcessStockHistoryParallel(Chart chart, SynchronizationContext ctx)
        {
            var sw = Stopwatch.StartNew();

            // TODO
            // (1) Process the stock analysis in parallel
            // When all the computation complete, then update the chart
            // Than control the level of parallelism processing max 2 stocks at a given time
            // Suggestion, use the RequestGate class
            // SUggestion, use the Otherwise operator to load local tickers (from folder Tickers) in case there is a problem to connect to the Web
            List<Task<Tuple<string, StockData[]>>> stockHistoryTasks = null; ;



            // (2) update the chart
            // ShowChart(......

            // (3) process each Task as they complete
            // replace point (1)
            // update the code to process the stocks in parallel and update the chart as the results arrive
            // to update the chart use
            // Suggestion, to update the chart you should use the correct SynchronizationContext
            // for example, ctx.Send(_ => UpdateChart(chart, ........
            // you can grab the context earlier and pass it into the signature of this method
            // SynchronizationContext ctx
        }

        async Task<Tuple<string, StockData[]>> ProcessStockHistoryConditional(string symbol)
        {
            Func<Func<string, string>, Func<string, Task<string>>> downloadStock =
                service => stock => DownloadStockHistory(service, stock);

            Func<string, Task<string>> googleService =
                                    downloadStock(googleSourceUrl);
            Func<string, Task<string>> yahooService =
                                    downloadStock(yahooSourceUrl);

            // TODO : 4.8
            // Take a look at the operators
            // AsyncEx.Retry
            // AsyncEx.Otherwise
            // in \Common\Functional.cs\Concurrency\AsyncEx
            // Implement a reliable way to retrieve the stocks using these operators
            // Suggestion, there are 2 endpoints available Google and Yahoo finance
            // ideally, you should use both Retry and Otherwise



            return null;
        }

        private void ShowChart(IEnumerable<Tuple<string, StockData[]>> stockHistories, long elapsedTime)
        {
            // Create a chart containing a default area
            var chart = new Chart { Dock = DockStyle.Fill };
            chart.ChartAreas.Add(new ChartArea("MainArea"));
            chart.Legends.Add(new Legend());
            chart.Titles.Add($"Time elapsed {elapsedTime} ms");

            // Create series and add it to the chart
            foreach (var s in stockHistories)
            {
                var series = new Series
                {
                    LegendText = s.Item1,
                    ChartType = SeriesChartType.Candlestick
                };
                chart.Series.Add(series);

                foreach (var d in s.Item2)
                {
                    series.Points.AddXY(d.Date, d.Open, d.High, d.Low, d.Close);
                }
            }

            // Show chart on the form
            var form = new Form { Visible = true, Width = 700, Height = 500 };
            form.Controls.Add(chart);
            Application.Run(form);
        }

        private Chart CreateChart()
        {
            // Create a chart containing a default area
            var chart = new Chart { Dock = DockStyle.Fill };
            chart.ChartAreas.Add(new ChartArea("MainArea"));
            return chart;
        }

        private void UpdateChart(Chart chart, Tuple<string, StockData[]> stockHistory, long elapsedMilliseconds)
        {
            var series = new Series
            {
                LegendText = stockHistory.Item1,
                ChartType = SeriesChartType.Candlestick
            };
            chart.Series.Add(series);

            foreach (var d in stockHistory.Item2)
                series.Points.AddXY(d.Date, d.Open, d.High, d.Low, d.Close);

            chart.Titles.Clear();
            chart.Titles.Add($"Time elapsed {elapsedMilliseconds} ms");
        }
    }
}