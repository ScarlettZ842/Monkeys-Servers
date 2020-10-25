namespace Monkeys {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using static System.Console;
    using System.IO;
    using System.Collections;
    using System.Text;
    using Carter;
    using Carter.ModelBinding;
    using Carter.Request;
    using Carter.Response;
    using Microsoft.AspNetCore.Http;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;

    public class HomeModule : CarterModule {
        public HomeModule () {
            Post ("/try", async (req, res) => {
                var g = await req.Bind<TryRequest> ();
                var genome_id = g.id;
                var genome_parallel = g.parallel;
                var genome_monkeys = g.monkeys;
                var genome_length = g.length;
                var genome_crossover = g.crossover;
                var genome_mutation = g.mutation;
                var genome_limit = g.limit;
                WriteLine ($"..... POST /try {genome_id} {genome_parallel} {genome_monkeys} {genome_length} {genome_crossover} {genome_mutation} {genome_limit}");
                GeneticAlgorithm (g);
                await Task.Delay (0);
                return;
            });
        }

        async Task<AssessResponse> PostFitnessAssess (AssessRequest areq) {
            var client = new HttpClient ();
            client.BaseAddress = new Uri ("http://localhost:8091/");
            client.DefaultRequestHeaders.Accept.Clear ();
            client.DefaultRequestHeaders.Accept.Add (new MediaTypeWithQualityHeaderValue ("application/json"));

            var hrm = await client.PostAsJsonAsync ("/assess", areq);
            hrm.EnsureSuccessStatusCode ();

            await Task.Delay (0);
            var ares = await hrm.Content.ReadAsAsync <AssessResponse> ();

            //var ares = new AssessResponse (id = areq.id, scores = ares);
            //var ares = new AssessResponse ();
            return ares;
        }

        async Task PostClientTop (TopRequest treq) {
            var client = new HttpClient ();
            client.BaseAddress = new Uri ("http://localhost:"+treq.id+"/");
            client.DefaultRequestHeaders.Accept.Clear ();
            client.DefaultRequestHeaders.Accept.Add (new MediaTypeWithQualityHeaderValue ("application/json"));

            var newhrm = await client.PostAsJsonAsync ("/top", treq);
            newhrm.EnsureSuccessStatusCode ();

            //var bres = await newhrm.Content.ReadAsAsync();
            await Task.Delay (0);
            WriteLine ($"===== {treq.loop} {treq.score}\r\n{treq.genome}");
            //if (treq.score < 0) {break;}
            return;
        }

        private Random _random = new Random (1);

        private double NextDouble () {
            lock (this) {
                return _random.NextDouble ();
            }
        }

        private int NextInt (int a, int b) {
            lock (this) {
                return _random.Next (a, b);
            }
        }

        int ProportionalRandom (int[] weights, int sum) {
            var val = NextDouble () * sum;

            for (var i = 0; i < weights.Length; i ++) {
                if (val < weights[i]) return i;

                val -= weights[i];
            }

            WriteLine ($"***** Unexpected ProportionalRandom Error");
            return 0;
        }

        async void GeneticAlgorithm (TryRequest treq) {
            WriteLine ($"..... GeneticAlgorithm {treq}");
            await Task.Delay (0);

            // just an ad-hoc PR test - you will remove this
            // await ProportionalRandomTest ();

            //monkey receive try from client

            var id = treq.id; //port number
            var monkeys = treq.monkeys; //each monkeys with genome
            if (monkeys % 2 != 0) monkeys += 1;
            var length = treq.length;
            var crossover = treq.crossover / 100.0 ;
            var mutation = treq.mutation / 100.0;
            var limit = treq.limit;
            if (limit == 0) limit = 1000;

            var topscore = int.MaxValue;

            // Generate the first group of genomes (randomly)
            Random c = new Random();
            var genomes = Enumerable.Range(0, monkeys).Select(h => string.Join("", Enumerable.Range(0, length)
                .Select(n => (char)c.Next(32, 127)))).ToList();
            //genomes =["asd", "xcx", "afsf"] number of monkeys List<string> 4
            //declear and use the AssessRequest obj1[put the id inside]
            var obj1 = new AssessRequest { id = treq.id, genomes = genomes };

            for (int loop = 0; loop < limit; loop ++) {
                // Post obj1 to fitness, get the fitness scores
                AssessResponse x  =  await PostFitnessAssess(obj1);
                var obj1_id = x.id;
                var obj1_score = x.scores;

                //{"id" = 8888, "scores" = [1,2,3,2,3,4,5,6,1]} largest = 6

                int best_score = obj1_score.Min();
                int largest_score = obj1_score.Max();
                //Console.WriteLine ($"..... {best_score} {largest_score}");
                int index = obj1_score.IndexOf(best_score);
                var best_string = obj1 .genomes[index];
                //best score = 0, best genomes

                if (topscore > best_score)
                {
                    topscore = best_score;
                    var obj2 = new TopRequest { id = treq.id, loop = loop, score = topscore, genome = best_string };
                    var y = PostClientTop(obj2);
                }
                if (topscore < 0) { break; }
                //Post to client, ask client to Print
                //if best scores = 0, break; (already)

                //weight = [6,5,4,5,3,2,1,6]
                //sumofweights = weights.Sum()
                var weights = obj1_score.Select(bw => largest_score-bw+1).ToArray();
                var sumofweights = weights.Sum();

                var para = treq.parallel;
                if (para) {
                    var newgenomes = ParallelEnumerable.Range(1, monkeys/2)
                        .SelectMany<int,string> (i => {
                        var index1 = ProportionalRandom(weights, sumofweights);
                        var index2 = ProportionalRandom(weights, sumofweights);
                        var p1 = obj1 .genomes[index1];
                        var p2 = obj1 .genomes[index2];
                        Random rnd = new Random();
                        int seperatei = rnd.Next(1,100);
                        var c1 = "";
                        var c2 = "";
                        if (seperatei < crossover*100){
                            var crossoverIn = rnd.Next(0,treq.length);
                            c1 = string.Join("", p1.Substring(0,crossoverIn), p2.Substring(crossoverIn,treq.length-crossoverIn));
                            c2 = string.Join("", p2.Substring(0,crossoverIn), p1.Substring(crossoverIn,treq.length-crossoverIn));
                        }else{
                            c1 = p1;
                            c2 = p2;
                        }

                        Random newrnd = new Random();
                        int seperaten = newrnd.Next(1,100);
                        if (newrnd.Next(1,100) < mutation*100){
                            c1 = new StringBuilder(c1) {[newrnd.Next(0, 4)] = (char)newrnd.Next(32, 127)}.ToString();
                        }
                        if (newrnd.Next(1,100) < mutation*100){
                            c2 = new StringBuilder(c2) {[newrnd.Next(0, 4)] = (char)newrnd.Next(32, 127)}.ToString();
                        }
                        return new[] {c1,c2};
                    }) .ToList();
                    obj1.genomes = newgenomes;

                }else {
                    var newgenomes = Enumerable.Range(1, monkeys/2)
                        .SelectMany<int,string> (i => {
                        var index1 = ProportionalRandom(weights, sumofweights);
                        var index2 = ProportionalRandom(weights, sumofweights);
                        var p1 = obj1 .genomes[index1];
                        var p2 = obj1 .genomes[index2];
                        Random rnd = new Random();
                        int seperatei = rnd.Next(1,100);
                        var c1 = "";
                        var c2 = "";
                        if (seperatei < crossover*100){
                            var crossoverIn = rnd.Next(0,treq.length);
                            c1 = string.Join("", p1.Substring(0,crossoverIn), p2.Substring(crossoverIn,treq.length-crossoverIn));
                            c2 = string.Join("", p2.Substring(0,crossoverIn), p1.Substring(crossoverIn,treq.length-crossoverIn));
                        }else{
                            c1 = p1;
                            c2 = p2;
                        }

                        Random newrnd = new Random();
                        int seperaten = newrnd.Next(1,100);
                        if (newrnd.Next(1,100) < mutation*100){
                            c1 = new StringBuilder(c1) {[newrnd.Next(0, 4)] = (char)newrnd.Next(32, 127)}.ToString();
                        }
                        if (newrnd.Next(1,100) < mutation*100){
                            c2 = new StringBuilder(c2) {[newrnd.Next(0, 4)] = (char)newrnd.Next(32, 127)}.ToString();
                        }
                        return new[] {c1,c2};
                    }) .ToList();
                    obj1.genomes = newgenomes;
                }
            }
        }
    }

    // public class TargetRequest {
        // public int id { get; set; }
        // public bool parallel { get; set; }
        // public string target { get; set; }
        // public override string ToString () {
            // return $"{{{id}, {parallel}, \"{target}\"}}";
        // }
    // }

    public class TryRequest {
        public int id { get; set; }
        public bool parallel { get; set; }
        public int monkeys { get; set; }
        public int length { get; set; }
        public int crossover { get; set; }
        public int mutation { get; set; }
        public int limit { get; set; }
        public override string ToString () {
            return $"{{{id}, {parallel}, {monkeys}, {length}, {crossover}, {mutation}, {limit}}}";
        }
    }

    public class TopRequest {
        public int id { get; set; }
        public int loop { get; set; }
        public int score { get; set; }
        public string genome { get; set; }
        public override string ToString () {
            return $"{{{id}, {loop}, {score}, {genome}}}";
        }
    }

    public class AssessRequest {
        public int id { get; set; }
        public List<string> genomes { get; set; }
        public override string ToString () {
            return $"{{{id}, #{genomes.Count}}}";
        }
    }

    public class AssessResponse {
        public int id { get; set; }
        public List<int> scores { get; set; }
        public override string ToString () {
            return $"{{{id}, #{scores.Count}}}";
        }
    }
}

namespace Monkeys {
    using Carter;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;

    public class Startup {
        public void ConfigureServices (IServiceCollection services) {
            services.AddCarter ();
        }

        public void Configure (IApplicationBuilder app) {
            app.UseRouting ();
            app.UseEndpoints( builder => builder.MapCarter ());
        }
    }
}

namespace Monkeys {
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public class Program {
        public static void Main (string[] args) {
//          var host = Host.CreateDefaultBuilder (args)
//              .ConfigureWebHostDefaults (webBuilder => webBuilder.UseStartup<Startup>())

            var urls = new[] {"http://localhost:8081"};

            var host = Host.CreateDefaultBuilder (args)

                .ConfigureLogging (logging => {
                    logging
                        .ClearProviders ()
                        .AddConsole ()
                        .AddFilter (level => level >= LogLevel.Warning);
                })

                .ConfigureWebHostDefaults (webBuilder => {
                    webBuilder.UseStartup<Startup> ();
                    webBuilder.UseUrls (urls);  // !!!
                })

                .Build ();

            System.Console.WriteLine ($"..... starting on {string.Join (", ", urls)}");
            host.Run ();
        }
    }
}
