// Copyright 2022 Owen Maule <o@owen-m.com>
// Find the Megaprimes

// When you said this exercise should take about an hour, you meant the runtime, right?
// I left workings/scaffold that I would normally have removed, wanted to show some of my dev/debug process
// This is my first C# program

// TODO:
// Refactor into multiple files / classes, reusability as library?

// Example results
// PS> .\CSharpMegaprimes.exe highest max sieve same
// Mode MegaprimesParallel IsPrime_WheelFactorisation
// 4294967295 => {3777777557} 53156 Megaprimes in 14331.8518 ms
// Mode SieveOfEratosthenesBitArrays
// 4294967295 => {3777777557} 53156 Megaprimes in 277114.6308 ms

// highest prime max
// Mode MegaprimesParallel IsPrime_WheelFactorisation
// 4294967295 => {4294967291} 203280221 primes in 1427272.9558 ms

// References
// https://en.wikipedia.org/wiki/Megaprime - No not that kind of megaprime :-)
// https://oeis.org/A019546
// https://stackoverflow.com/questions/453793/which-is-the-fastest-algorithm-to-find-prime-numbers
// https://en.wikipedia.org/wiki/Sieve_of_Eratosthenes
// https://en.wikipedia.org/wiki/Wheel_factorization
// https://web.archive.org/web/20170918132958/https://iquilezles.org/blog/?p=1558
// https://www.shadertoy.com/view/4slGRH
// https://www.shadertoy.com/view/XdsXz4
// TODO: Use other programs to generate test data for external validation testing?
// https://github.com/snwmelt/Megaprimes - Have not got it working yet
// https://github.com/kamilossan/Megaprimes - Slow, but can get a few values
// 3000 => {2,3,5,7,23,37,53,73,223,227,233,257,277,337,353,373,523,557,577,727,733,757,773,2237,2273,2333,2357,2377,2557,2753,2777}

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CSharpMegaprimes
{
    class Program
    {
        // Magic numbers
        private const UInt32 _benchRuns = 50000;
        private const UInt32 _benchLongRuns = 1000000;
        private const UInt32 _warmupRuns = 100;

        // User docs
        static void Help()
        {
            Console.WriteLine(@"Find the Megaprimes - Copyright 2022 Owen Maule <o@owen-m.com>
Megaprimes are prime numbers where each digit is also a prime.
Enter positive integer up to {0} to find megaprimes up to and including that maximum.
Options:
    simple - use Trial Division Simple for prime detection in scan
    div - use Trial Division Optimised for prime detection in scan
    wheel - use Wheel Factorisation for prime detection in scan (default)

    mega - find megaprimes (default)
    prime - find primes instead of megaprimes

    lazy - display results while generating, visual progress for high maximum
    eagar - store the results in an array, then display them (default)

Eagar modes:
    scan - use simple single-thread scan for megaprimes
    sieve - use Sieve of Eratosthenes scan for megaprimes
    " + //lookup - use hardcoded lookup for megaprimes (Feature idea rejected/postponed)
@"  par - use parallelised scan for megaprimes (default)

    results - list all results after scan (default)
    highest - display only highest result after scan

    max - use max value of {0}
    same - use the same number as previous, in subsequent requests
    test - run correctness tests (eagar mode)
    bench - benchmark subroutines for {1} runs
    benchlong - benchmark subroutines for {2} runs
    flip - reverse order of benchmark competitors
Examples:
    9000 - scan over 9000, scouting megaprimes
    1000 2000 3000 - request multiple scans
    div 1000 2000 3000 - request multiple scans with Trial Division Optimised
    simple 100 highest div 2000 sieve 500 scan wheel results 300 - multiple varieties in sequence
    highest simple 999999 div same wheel same sieve same - compare methods using same number
    lazy 760000000 - display results while generating them
    highest max - find the highest supported megaprime
    highest prime max - find the highest supported prime

    simple test - run tests on Trial Division Simple
    div test - run tests on Trial Division Optimised
    wheel test - run tests on Wheel Factorisation
    sieve test - run tests on Sieve of Eratosthenes
    scan simple test - run tests on single thread scan with Trial Division Simple
    simple test div test wheel test sieve test scan test - test all the things
    flip bench - run the benchmark competitors with reverse order
", UInt32.MaxValue, _benchRuns, _benchLongRuns);
        }

        // Strategy pattern : Selectable policies
        public static Action<UInt32, List<UInt32>> _ScanPolicy = MegaprimesParallel;
        public static Func<UInt32, bool> _IsPrimePolicy = IsPrime_WheelFactorisation;
        public static Func<UInt32, bool> _IsMegaprimePolicy = IsMegaprime_Numerical;
        public static bool _lazy = false;
        public static bool _highest = false;
        public static bool _benchFlip = false;

        static void Main(string[] args)
        {
            if (args.Length != 0) {
                bool validArg = false;
                UInt32 max = 0;

                // Mitigate some perf impact of cold start
                // (caching, JIT-ing, one-time initialisers)
                // TODO Check the benefit of this after significant code changes
                _ScanPolicy(_warmupRuns, new List<UInt32>());

                foreach (string arg in args) {
                    switch (arg) {
                        case "simple": _IsPrimePolicy = IsPrime_TrialDivisionSimple; break;
                        case "div": _IsPrimePolicy = IsPrime_TrialDivisionOptimised; break;
                        case "wheel": _IsPrimePolicy = IsPrime_WheelFactorisation; break;
                        case "lazy": _lazy = true; break;
                        case "eagar": _lazy = false; break;
                        case "scan": _ScanPolicy = Megaprimes; break;
                        case "sieve": _ScanPolicy = SieveOfEratosthenesBitArrays; break;
                        case "par": _ScanPolicy = MegaprimesParallel; break;
//                        case "lookup": _ScanPolicy = MegaprimeLookup; break;
                        case "results": _highest = false; break;
                        case "highest": _highest = true; break;
                        case "mega": _IsMegaprimePolicy = IsMegaprime_Numerical; break;
                        case "prime": _IsMegaprimePolicy = IsMegaprime_FindPrimes; break;
                        case "test": Tests(); validArg = true; break;
                        case "bench" : Benchmarks(_benchRuns); validArg = true; break;
                        case "benchlong" : Benchmarks(_benchLongRuns); validArg = true; break;
                        case "flip" : _benchFlip = !_benchFlip; break;
                        case "max": max = UInt32.MaxValue; goto default;
                        default: {
                            if (arg == "same" || arg == "max" || UInt32.TryParse(arg, out max)) {
                                if (_lazy)
                                    Lazy(max);
                                else
                                    Scan(max);
                                validArg = true;
                            }
                            break;
                        };
                    }
                }
                // If we had 'test', 'bench', 'same' or a UInt32, we are done
                if (validArg) return;
            }
            Help(); // No arg or no valid arg: show help
        }

        static void Lazy(UInt32 max)
        {
            Console.Write("Mode Lazy {0}\n{1} => {{", _IsPrimePolicy.Method.Name, max);
            UInt32 found = 0;
            foreach (UInt32 result in MegaprimeGenerator(max))
                Console.Write(found++ == 0 ? result : "," + result);
            Console.WriteLine("}} {0} {1}primes", found,
                _IsMegaprimePolicy == IsMegaprime_FindPrimes ? "" : "Mega");
        }

        public static void DisplayScanMode() {
            Console.WriteLine("Mode {0} {1}", _ScanPolicy.Method.Name,
                _ScanPolicy == Megaprimes || _ScanPolicy == MegaprimesParallel ? _IsPrimePolicy.Method.Name : "");
        }

        static void Scan(UInt32 max)
        {
            List<UInt32> results = new List<UInt32>(53156);
            DisplayScanMode();
            Stopwatch watch = Stopwatch.StartNew();
            _ScanPolicy(max, results);
            watch.Stop();
            Console.WriteLine("{0} => {{{1}}} {2} {4}primes in {3} ms", max,
                _highest && results.Count != 0 ? results.Last() : String.Join(",", results),
                results.Count, watch.Elapsed.TotalMilliseconds,
                _IsMegaprimePolicy == IsMegaprime_FindPrimes ? "" : "Mega");
        }

        // Simplest code
        public static bool IsPrime_TrialDivisionSimple(UInt32 candidate)
        {
            // Only need to search up to sqrt of candidate
            UInt32 divisor_max = 1 + (UInt32)Math.Sqrt((Double)candidate);
//          Console.WriteLine("Trial Divisor Simple validate {0} up to {1}", candidate, divisor_max);
            for (UInt32 divisor = 2; divisor != divisor_max; ++divisor) {
                if (candidate % divisor == 0) {
//                  Console.WriteLine("Found factor {0} for {1}", divisor, candidate);
                    return false; // found a factor
                }
            }
            return true;
        }

        // Argue over pennies
        public static bool IsPrime_TrialDivisionOptimised(UInt32 candidate)
        {            
//          Console.WriteLine("Megaprimes scan with " + _IsPrimePolicy.Method.Name);
            if (candidate == 2 || candidate == 3 || candidate == 5 || candidate == 7)
                return true;
            // candidate % 2 == 0 vs (candidate & 1) != 1 - bitwise appears slower!
            if (candidate == 0 || candidate == 1
                || candidate % 2 == 0 || candidate % 3 == 0 || candidate % 5 == 0)
                return false;

            // Only need to search up to sqrt of candidate
            UInt32 divisor_max = 1 + (UInt32)Math.Sqrt((Double)candidate);
//          Console.WriteLine("Trial Divisor validate {0} up to {1}", candidate, divisor_max);
            if (divisor_max > 7)
                for (UInt32 divisor = 7; divisor != divisor_max; ++divisor) {
                    if (candidate % divisor == 0) {
//                      Console.WriteLine("Found factor {0} for {1}", divisor, candidate);
                        return false; // found a factor
                    }
                }
            return true;
        }

        // Less modulo, more code complexity, performant
        public static bool IsPrime_WheelFactorisation(UInt32 candidate)
        {
            if (candidate == 2 || candidate == 3 || candidate == 5 || candidate == 7)
                return true;
            if (candidate == 0 || candidate == 1
                || candidate % 2 == 0 || candidate % 3 == 0 || candidate % 5 == 0)
                return false;

            // Only need to search up to sqrt of candidate
            UInt32 divisor_max = 1 + (UInt32)Math.Sqrt((Double)candidate);

            // Wheel factorisation: Skip multiples of 2, 3 and 5
            UInt32 divisor = 7;
            do {
                if (candidate % divisor == 0) return false;
                divisor += 4;
                if (divisor >= divisor_max) return true;
                if (candidate % divisor == 0) return false;
                divisor += 2;
                if (divisor >= divisor_max) return true;
                if (candidate % divisor == 0) return false;
                divisor += 4;
                if (divisor >= divisor_max) return true;
                if (candidate % divisor == 0) return false;
                divisor += 2;
                if (divisor >= divisor_max) return true;
                if (candidate % divisor == 0) return false;
                divisor += 4;
                if (divisor >= divisor_max) return true;
                if (candidate % divisor == 0) return false;
                divisor += 6;
                if (divisor >= divisor_max) return true;
                if (candidate % divisor == 0) return false;
                divisor += 2;
                if (divisor >= divisor_max) return true;
                if (candidate % divisor == 0) return false;
                divisor += 6;
            } while (divisor < divisor_max);
            return true;
        }

        public static bool IsMegaprime_String(UInt32 candidate)
        {
            // Assume: Already a prime
            string digits = candidate.ToString();
            foreach(char digit in digits) {
//              UInt32 number = (UInt32)digit - '0';
//              Console.WriteLine("{0} Check digit {1}, IsPrime {2}", candidate, digit, IsPrime(number));
                // Don't call IsPrime
                if (digit != '2' && digit != '3' && digit != '5' && digit != '7')
                    return false;
            }
            return true;
        }

        public static bool IsMegaprime_Numerical(UInt32 candidate)
        {
            // Assume: Already a prime
//          Console.WriteLine("IsMegaprime_Numerical candidate " + candidate);
            do {
                UInt32 digit = candidate % 10;
//              Console.WriteLine("Digit " + digit);
                if (digit != 2 && digit != 3 && digit != 5 && digit != 7) {
//                  Console.WriteLine("Not megaprime");
                    return false;
                }
                candidate /= 10;
            } while (candidate != 0);
//          Console.WriteLine("It's megaprime");
            return true;
        }

        public static bool IsMegaprime_FindPrimes(UInt32 candidate) { return true; }

        public static void Megaprimes(UInt32 max, List<UInt32> results)
        {
            results.Clear();
            if (max > 1) {
                ++max;
                for (UInt32 scan = 2; scan != max; ++scan)
                    // IsMegaPrime will be faster for large values, so call first
                    if (_IsMegaprimePolicy(scan) && _IsPrimePolicy(scan))
                        results.Add(scan);
            }
        }

        public static IEnumerable<UInt32> MegaprimeGenerator(UInt32 max)
        {
            if (max > 1) {
                ++max;
                for (UInt32 scan = 2; scan != max; ++scan)
                    if (_IsMegaprimePolicy(scan) && _IsPrimePolicy(scan))
                        yield return scan;
            }
        }

        public static void MegaprimesParallel(UInt32 max, List<UInt32> results)
        {
            results.Clear();
            if (max > 1) {
                ConcurrentBag<UInt32> resultsBag = new ConcurrentBag<UInt32>();
                Parallel.For(2L, (long)max + 1, scanlong => {
                    UInt32 scan = (UInt32)scanlong;
                    if (_IsMegaprimePolicy(scan) && _IsPrimePolicy(scan))
                        resultsBag.Add(scan);
                });
                results.AddRange(resultsBag);
                results.Sort();
            }
        }

        // https://www.wolframalpha.com/input?i=number+of+primes+less+than+4294967295
        // 203,280,221 primes, 4,091,687,074 composites of UInt32
        // Mad to store 4 bytes for each composite, approx 15.24 GiB, instead use array of bools or bitflags
        // Bytes 3.81 GiB or Bits 487.77 MiB
        public static void SieveOfEratosthenesBitArrays(UInt32 max, List<UInt32> results)
        {
            BitArray sieveLower = new BitArray(Int32.MaxValue),
                sieveUpper = new BitArray(Int32.MaxValue);

            results.Clear();
            if (max > 1) {
                // Not quite enough space in the BitArrays for the final 2 values of UInt32
                // but they're not prime, so fine to ignore
                UInt64 max64 = Math.Min(max, UInt32.MaxValue - 2);
                if (++max == 0 || max == UInt32.MaxValue)
                    max = UInt32.MaxValue - 1;
                for (UInt32 search = 2; search != max; ++search) {
                    if (search >= Int32.MaxValue ?
                        !sieveUpper[(Int32)(search - Int32.MaxValue)] : !sieveLower[(Int32)search])
                    {
//                      Console.WriteLine("Not found " + search);
                        if (_IsMegaprimePolicy(search))
                            results.Add(search);
                        // CAUTION: Potential overflow scenario (now safe)
                        for (UInt64 insert = (UInt64)search * (UInt64)search; insert <= max64; insert += search) {
                            /* How I made a bug, found it and fixed it. See DiffWheelAgainstSieve()
                            if (insert == 33353323)
                                Console.WriteLine("*** Adding {0} (prime) to sieve (thinks it's multiple of {1}) ***",
                                    insert, search);
                            */
                            if (insert >= (UInt64)Int32.MaxValue) {
                                Int32 upperInsert = (Int32)(insert - (UInt64)Int32.MaxValue);
//                              if (upperInsert == Int32.MaxValue) // Bug investigation
//                                  Console.WriteLine("Insert Upper {0} / {1} {2} -> {3}", insert, max, max64, upperInsert);
                                sieveUpper[upperInsert] = true;
                            } else
                                sieveLower[(Int32)insert] = true;
                        }
                    }
                }
            }
        }

        // SieveOfAtkinBytesParallel() etc left as an exercise for the reader
        /************************************************************************************************************/

#if false
        static void MegaprimeLookup(UInt32 max, List<UInt32> results)
        {
            results.Clear();
            for (UInt32 scan = 0; scan != _MegaprimesSource.Length; ++scan) {
                UInt32 Megaprime = _MegaprimesSource[scan];
                if (Megaprime > max)
                    return;
                results.Add(Megaprime);
            }
        }

        // NOTE Get up to specific quantity of megaprimes, not megaprimes up to a maximum magnitude
        static IEnumerable<UInt32> MegaprimeLookupGenerator(UInt32 elements)
        {
            elements = Math.Min(elements, (UInt32)_MegaprimesSource.Length);
            for (UInt32 index = 0; index != elements; ++index)
                yield return _MegaprimesSource[index];
        }

        // Populate and use for lookup method and test data?
        public readonly static UInt32[] _MegaprimesSource = {2,3,5,7,23,37};
#endif

        // Could make it generic, however quick hack here to find and fix overflow bug in SoE
        static void DiffWheelAgainstSieve(UInt32 max)
        {
            _ScanPolicy = MegaprimesParallel;
            _IsPrimePolicy = IsPrime_WheelFactorisation;
            _IsMegaprimePolicy = IsMegaprime_Numerical;
            List<UInt32> megaprimes = new List<UInt32>();
            Megaprimes(max, megaprimes);

            List<UInt32> sieveMegaprimes = new List<UInt32>();
            SieveOfEratosthenesBitArrays(max, sieveMegaprimes);

            Console.WriteLine("Diff Wheel vs Sieve to {0}: Found {1} vs {2}", max, megaprimes.Count,
                sieveMegaprimes.Count);
            List<UInt32> extra = new List<UInt32>(megaprimes.Except(sieveMegaprimes));
            if (extra.Count != 0)
                Console.WriteLine("Wheel extras: " + String.Join(",", extra));
            extra = sieveMegaprimes.Except(megaprimes).ToList();
            if (extra.Count != 0)
                Console.WriteLine("Sieve extras: " + String.Join(",", extra));
        }

        // Test Driven
        static void Tests()
        {
            int index = 0;
            DisplayScanMode();
            TestCase(++index, 10, new List<UInt32> {2,3,5,7});
            TestCase(++index, 37, new List<UInt32> {2,3,5,7,23,37});
            TestCase(++index, 1, new List<UInt32> {});
            TestCase(++index, 2, new List<UInt32> {2});
            TestCase(++index, 3, new List<UInt32> {2,3});

            // Generated using https://github.com/kamilossan/Megaprimes
            TestCase(++index, 3000, new List<UInt32> {2,3,5,7,23,37,53,73,223,227,233,257,277,337,353,373,523,557,577,
                727,733,757,773,2237,2273,2333,2357,2377,2557,2753,2777});

            // Self generated
            TestCase(++index, 9001, new List<UInt32> {2,3,5,7,23,37,53,73,223,227,233,257,277,337,353,373,523,557,577,
                727,733,757,773,2237,2273,2333,2357,2377,2557,2753,2777,3253,3257,3323,3373,3527,3533,3557,3727,3733,
                5227,5233,5237,5273,5323,5333,5527,5557,5573,5737,7237,7253,7333,7523,7537,7573,7577,7723,7727,7753,
                7757});

            Console.WriteLine();

            // Further testing
//          DiffWheelAgainstSieve(_benchLongRuns);

#if false
            Console.Write("Testing correctness IsMegaprime_String vs IsMegaprime_Numerical... ");
            bool success = true;
            for (UInt32 test = 0; test != _benchLongRuns; ++test) {
                bool ResultA = IsMegaprime_String(test),
                    ResultB = IsMegaprime_Numerical(test);

                if (ResultA != ResultB) {
                    Console.WriteLine("First fail at {0}: {1} vs {2}", test, ResultA, ResultB);
                    success = false;
                    break;
                }
            }
            if (success)
                Console.WriteLine("Great Success!");
#endif
        }

        static void TestCase(int index, UInt32 max, List<UInt32> expected)
        {
            List<UInt32> results = new List<UInt32>();
            _ScanPolicy(max, results);
            bool pass = results.SequenceEqual(expected);
            Console.WriteLine("Test {0}: {1} ({2})", index, pass ? "Pass" : "Fail", max);
            if (!pass)
                Console.WriteLine("{0} => {{{1}}} {2} expected {{{3}}} {4} ", max, String.Join(",", results),
                    results.Count, String.Join(",", expected), expected.Count);
        }

        // Benchmark Driven - Compare some subroutine alternatives
        static void Benchmarks(UInt32 runs)
        {
            const bool callOnce = true;

            Console.WriteLine("Benchmark: ForLoop < i++ vs != ++i");
            Benchmark(runs, ForLoopLessThan, ForLoopInequality, callOnce);
            Console.WriteLine("Benchmark: IsEven modulo vs bitwise");
            Benchmark(runs, IsEvenModulo, IsEvenBitwise);
#if false
            // Noisy Console.Write test
            Console.WriteLine("Benchmark: Output String.Join vs Console.Write");
            Benchmark(runs, OutputStringJoin, OutputConsoleWrite, callOnce);
#endif
            Console.WriteLine("Benchmark: IsMegaprime string vs numerical");
            Benchmark(runs, IsMegaprime_String, IsMegaprime_Numerical);
            Console.WriteLine("Benchmark: IsPrime Trial Division simple vs optimised");
            Benchmark(runs, IsPrime_TrialDivisionSimple, IsPrime_TrialDivisionOptimised);
            Console.WriteLine("Benchmark: IsPrime Trial Division simple vs Wheel Factorisation");
            Benchmark(runs, IsPrime_TrialDivisionSimple, IsPrime_WheelFactorisation);
        }

        static bool ForLoopLessThan(UInt32 value)
        {
            // including postfix increment
            for (UInt32 loop = 0; loop < value; loop++);
            return true;
        }
        static bool ForLoopInequality(UInt32 value)
        {
            // including prefix increment
            for (UInt32 loop = 0; loop != value; ++loop);
            return true;
        }

        static bool IsEvenModulo(UInt32 value) { return value % 2 == 0; }
        static bool IsEvenBitwise(UInt32 value) { return (value & 1) != 1; }

        static bool OutputStringJoin(UInt32 max)
        {
            Console.Write(String.Join(",", Enumerable.Range(0, (int)max)));
            Console.WriteLine();
            return true;
        }
        static bool OutputConsoleWrite(UInt32 max)
        {
            foreach (int value in Enumerable.Range(0, (int)max))
                Console.Write(value + ",");
            Console.WriteLine();
            return true;
        }

        static void Benchmark(UInt32 runs, Func<UInt32, bool> FuncA, Func<UInt32, bool> FuncB, bool callOnce = false)
        {
            if (!callOnce) { // warm up (there's a callOnce test with console output, plus promised to only call once)
                FuncA(_warmupRuns);
                FuncB(_warmupRuns);
            }

            if (_benchFlip) {
                Func<UInt32, bool> temp = FuncB;
                FuncB = FuncA;
                FuncA = temp;
            }
            Stopwatch watch = Stopwatch.StartNew();

            if (callOnce)
                FuncA(runs);
            else
                for (UInt32 loop = 0; loop != runs; ++loop)
                    FuncA(loop);

            watch.Stop();
            double Atime = watch.Elapsed.TotalMilliseconds; 
            watch.Reset();
            Console.WriteLine("A: {0} for {1} iterations took {2} ms", FuncA.Method.Name, runs, Atime);
            watch.Start();

            if (callOnce)
                FuncB(runs);
            else
                for (UInt32 loop = 0; loop != runs; ++loop)
                    FuncB(loop);

            watch.Stop();
            double Btime = watch.Elapsed.TotalMilliseconds; 
            Console.WriteLine("B: {0} for {1} iterations took {2} ms", FuncB.Method.Name, runs, Btime);

            // Keep DRY
            char fasterLetter; double fasterTime, slowerTime;
            if (Atime < Btime) { fasterLetter = 'A'; fasterTime = Atime; slowerTime = Btime;
            } else {             fasterLetter = 'B'; fasterTime = Btime; slowerTime = Atime; }
            Console.WriteLine("{0} was {1:.###}% faster\n", fasterLetter, slowerTime > 0.0 ?
                    100.0 * (slowerTime - fasterTime) / slowerTime : (fasterTime > 0.0 ? "infinity" : "equal"));
        }
    }
}
