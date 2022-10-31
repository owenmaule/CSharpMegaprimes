# CSharpMegaprimes
Find the Megaprimes - Copyright 2022 Owen Maule <o@owen-m.com>
Megaprimes are prime numbers where each digit is also a prime.
Enter positive integer up to 4294967295 to find megaprimes up to and including that maximum.
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
    par - use parallelised scan for megaprimes (default)

    results - list all results after scan (default)
    highest - display only highest result after scan

    max - use max value of 4294967295
    same - use the same number as previous, in subsequent requests
    test - run correctness tests (eagar mode)
    bench - benchmark subroutines for 50000 runs
    benchlong - benchmark subroutines for 1000000 runs
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
