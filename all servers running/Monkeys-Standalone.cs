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


    public class HomeModule {
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

        string target = "";  // simplified Fitness stuff, shouldn't be here

        async Task<AssessResponse> PostFitnessAssess (AssessRequest areq) {
            // fake Fitness post request&response
            // replace this code by actual POST /assess request&response
            // var client = new HttpClient ();
            // ...

            await Task .Delay (0);

            var scores = areq .genomes .Select ( g => {
                var len = Math.Min (target.Length, g.Length);
                var h = Enumerable .Range (0, len)
                    .Sum (i => Convert.ToInt32 (target[i] != g[i]));
                h = h + Math.Max (target.Length, g.Length) - len;
                return h;
            }) .ToList ();

            return new AssessResponse { id = areq.id, scores = scores };
        }

        async Task PostClientTop (TopRequest treq) {
            // replace this by actual POST /top request
            //var client = new HttpClient ();
            //client.BaseAddress = new Uri ("http://localhost:"+treq.id+"/");
            //client.DefaultRequestHeaders.Accept.Clear ();
            //client.DefaultRequestHeaders.Accept.Add (new MediaTypeWithQualityHeaderValue ("application/json"));

            //var newhrm = await PostAsync (client, "/top", treq);
            //newhrm.EnsureSuccessStatusCode ();

            //var bres = await newhrm.Content.ReadAsAsync();
            //if (treq.score == 0) Exit ();
            
            await Task .Delay (0);
            WriteLine ($"===== {treq.loop} {treq.score}\r\n{treq.genome}");
            
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

        async Task ProportionalRandomTest () {
            WriteLine ($"..... ProportionalRandomTest");
            await Task.Delay (0);

            var weights = new[] { 1, 6, 1, 3, };
            var histo = new int[weights.Length];
            var sum = weights .Sum ();

            var n = 10000;
            for (var k = 0; k < n; k ++) {
                var p = ProportionalRandom (weights, sum);
                histo[p] += 1;
            }

            var sum2 = (double) sum;
            var n2 = (double) n;

            var weights2 = weights.Select (w => (w/sum2).ToString("F2"));
            var histo2 = histo.Select (h => (h/n2).ToString("F2"));

            WriteLine ($"..... w: {string.Join(", ", weights)}");
            WriteLine ($"..... h: {string.Join(", ", histo)}");
            WriteLine ($"..... w: {string.Join(", ", weights2)}");
            WriteLine ($"..... w: {string.Join(", ", histo2)}");
         }

        async void GeneticAlgorithm (TryRequest treq) {
            WriteLine ($"..... GeneticAlgorithm {treq}");
            await Task.Delay (0);

            // just an ad-hoc PR test - you will remove this
            // await ProportionalRandomTest ();

            // YOU CODE GOES HERE
            // FOLLOW THE GIVEN PSEUDOCODE

            //...
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
            //[32, 126]
            Random c = new Random(); 
            var genomes = Enumerable.Range(0, monkeys).Select(h => string.Join("", Enumerable.Range(0, length)
                .Select(n => (char)c.Next(32, 127)))).ToList();
            //genomes =["asd", "xcx", "afsf"] number of monkeys List<string> 4
            //id = 8888
            //declear and use the AssessRequest obj1[put the id inside]
            var obj1 = new AssessRequest { id = treq.id, genomes = genomes };

            for (int loop = 0; loop < limit; loop ++) {
              //GA
                // Post obj1 to fitness, get the fitness scores
                
                AssessResponse x  =  await PostFitnessAssess(obj1);
                var obj1_id = x.id;
                var obj1_score = x.scores;
                //x.id = to get id
                //x.score = to get scores

                //{"id" = 8888, "scores" = [1,2,3,2,3,4,5,6,1]} largest = 6
                int best_score = obj1_score.Max();
                int index = obj1_score.IndexOf(best_score);
                var best_string = obj1 .genomes[index];
                //best score = 0, best genomes

                //TopRequest obj2
                if (topscore > best_score)
                {
                    topscore = best_score;
                    var obj2 = new TopRequest { id = treq.id, loop = loop, score = topscore, genome = best_string };
                    var y = PostClientTop(obj2);
                }
                if (topscore == 0) { break; }
            //var obj2 = new TopRequest { id = treq.id, loop = loop, score = best_score, genome = best_string };

                //Post to client, ask client to Print
            // var y = PostClientTop(obj2);

                //if best scores = 0, break; (already)

                //GA body

                //fit_scores to compute weights
                //higher-order
                //Select()
                //weight = [6,5,4,5,3,2,1,6]
                //sumofweights = weights.Sum()
                var weights = obj1_score.Select(bw => topscore-bw+1).ToArray();
                var sumofweights = weights.Sum();


                var newgenomes = Enumerable.Range(1, monkeys/2) //or ParallelEnumberable
                    .SelectMany<int,string> (i => {
                    var index1 = ProportionalRandom(weights, sumofweights);
                    var index2 = ProportionalRandom(weights, sumofweights);
                    //p1 = genome[35],
                    //p2 = genome[3],
                    var p1 = obj1 .genomes[index1];
                    var p2 = obj1 .genomes[index2];
                    Random rnd = new Random();
                    int seperatei = rnd.Next(1,100);
                    var c1 = "";
                    var c2 = "";
                    if (seperatei < crossover*100){
                        //Random cross = new Random();
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

        async Task ReceiveClientTarget (TargetRequest t) {  // Simulate the POST /target function -- Fitness stuff, shouldn't remain here
            WriteLine ($"..... receive target {t}");
            await Task.Delay (0);

            target = t.target;
            return;  // emphatic empty return
        }

        async Task ReceiveClientTry (TryRequest t) {  // Simulate the POST /try function
            WriteLine ($"..... receive try {t}");
            await Task.Delay (0);

            GeneticAlgorithm (t);

            return;  // emphatic empty return
        }

       public static async Task Start (int port) {  // Client-like stuff, shouldn't remain here
            await Task.Delay (1000);

            var line1 = Console.ReadLine() ?.Trim();
            var line2 = Console.ReadLine() ?.Trim();

            var targetjson = string.IsNullOrEmpty (line1)? "{\"id\":0, \"target\": \"abcd\"}": line1;
            var tryjson = string.IsNullOrEmpty (line2)? "{\"id\": 0, \"parallel\": true, \"monkeys\": 20, \"length\": 4, \"crossover\": 90, \"mutation\": 20 }": line2;

            var target = JsonSerializer.Deserialize<TargetRequest> (targetjson);
            var trie = JsonSerializer.Deserialize<TryRequest> (tryjson);

            target.id = port;
            trie.id = port;

            //Console.WriteLine ($"..... target: {target}");
            //Console.WriteLine ($"..... try: {trie}");

            var self = new HomeModule ();

            await self .ReceiveClientTarget (target);  // Fitness stuff, shouldn't remain here
            await self .ReceiveClientTry (trie);
        }

        public static async Task Main (string[] args) { // Program stuff, shouldn't remain here
            await Task.Delay (0);
            WriteLine ($"..... Main");

            var port = 0;
            if (args.Length > 0 && int.TryParse (args[0], out port)) { ; }
            else { port = 8101; }

            await HomeModule.Start (port);
        }
    }

    public class TargetRequest {
        public int id { get; set; }
        public bool parallel { get; set; }
        public string target { get; set; }
        public override string ToString () {
            return $"{{{id}, {parallel}, \"{target}\"}}";
        }
    }

    public class TryRequest { // client to monkeys
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

    public class TopRequest { // monekys to client
        public int id { get; set; }
        public int loop { get; set; }
        public int score { get; set; }
        public string genome { get; set; }
        public override string ToString () {
            return $"{{{id}, {loop}, {score}, {genome}}}";
        }
    }

    public class AssessRequest { // monkeys to fitness
        public int id { get; set; }
        public List<string> genomes { get; set; }
        public override string ToString () {
            return $"{{{id}, {genomes.Count}}}";
        }
    }

    public class AssessResponse { // fitness to monkeys
        public int id { get; set; }
        public List<int> scores { get; set; }
        public override string ToString () {
            return $"{{{id}, {scores.Count}}}";
        }
    }
}


//Post ("/assess", async (req, res) => {
                    //var scores = obj1 .genomes .Select ( g => {
                        //var len = Math.Min (target.Length, g.Length);
                        //var h = Enumerable .Range (0, len)
                            ///.Sum (i => Convert.ToInt32 (target[i] != g[i]));
                        //h = h + Math.Max (target.Length, g.Length) - len;
                        //return h;
                    //}) .ToList ();
                    //await res.AsJson (scores);
                    //return;
                //});