﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Functional
{
    public static partial class Functional
    {
        public static Func<T2, Func<T1, R>> Flip<T1, T2, R>(this Func<T1, Func<T2, R>> func) => p2 => p1 => func(p1)(p2);

        public static Func<T2, T1, R> Flip<T1, T2, R>(this Func<T1, T2, R> func) => (t2, t1) => func(t1, t2);

        public static Func<T, T> Tee<T>(Action<T> act) => x => { act(x); return x; };
        public static T PipeTo<T>(this T input, Action<T> func) => Tee(func)(input);

        public static R Pipe<T, R>(this T @this, Func<T, R> func) => func(@this);
    }
}