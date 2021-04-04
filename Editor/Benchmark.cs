using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace Pumkin.Benchmark
{
    class Benchmark
    {
        string name;
        int totalRuns;

        Stopwatch stopwatch;
        int runsLeft;

        double totalTime;
        double runTime;
        double avgRunTime;
        double minRunTime;
        double maxRunTime;

        const int nameMaxChars = 50;

        public bool HasRunsLeft => runsLeft > 0;

        private Benchmark() { }

        public Benchmark(string name, int timesToRun = 1)
        {
            this.name = name.Length > nameMaxChars
                ? name.Substring(0,nameMaxChars - 1)
                : name + new string(' ', nameMaxChars - name.Length);

            totalRuns = timesToRun;
            runsLeft = totalRuns;
        }

        public void Start()
        {
            if(runsLeft <= 0)
                return;

            stopwatch = Stopwatch.StartNew();
        }

        public void Stop()
        {
            if(!stopwatch.IsRunning)
                return;

            stopwatch.Stop();
            runTime = stopwatch.ElapsedMilliseconds;
            totalTime += runTime;

            runsLeft--;
            avgRunTime = totalTime / totalRuns - runsLeft;

            if(minRunTime == 0 || minRunTime > runTime)
                minRunTime = runTime;
            if(maxRunTime == 0 || maxRunTime < runTime)
                maxRunTime = runTime;

            if(!HasRunsLeft)
                DisplayStats();
        }

        void DisplayStats()
        {
            if(totalTime > 0)
                Debug.Log(ToString());
        }

        public override string ToString()
        {
            if(totalRuns == 1)
                return $"<b>{name}</b>\t| Time: <color='blue'>{totalTime}</color>ms";
            return $"<b>{name}</b>\t| Runs: <color='blue'>{totalRuns}</color> | Total: <color='blue'>{totalTime}</color>ms | Min: <color='blue'>{minRunTime}</color>ms | Max: <color='blue'>{maxRunTime}</color>ms | Avg: <color='blue'>{avgRunTime}</color>ms";
        }

        public static implicit operator bool(Benchmark bm)
        {
            return bm != null;
        }
    }
}
