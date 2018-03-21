using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Functional.Validation
{

    public static partial class ErrorHelpers
    {
        public static Error<T> Exceptional<T>(T value) => new Error<T>(value);

        public static Func<T, Error<T>> Return<T>() => t => t;

        public static Error<R> Of<R>(Exception left) => new Error<R>(left);

        public static Error<R> Of<R>(R right) => new Error<R>(right);

        // applicative

        public static Error<R> Apply<T, R>
           (this Error<Func<T, R>> @this, Error<T> arg)
           => @this.Match(
              Exception: ex => ex,
              Success: func => arg.Match(
                 Exception: ex => ex,
                 Success: t => new Error<R>(func(t))));

        // functor

        public static Error<RR> Map<R, RR>(this Error<R> @this
           , Func<R, RR> func) => @this.Success ? func(@this.Value) : new Error<RR>(@this.Ex);

        public static Error<RR> Bind<R, RR>(this Error<R> @this
           , Func<R, Error<RR>> func)
            => @this.Success ? func(@this.Value) : new Error<RR>(@this.Ex);


        // LINQ
        public static Error<R> Select<T, R>(this Error<T> @this
           , Func<T, R> map) => @this.Map(map);

        public static Error<RR> SelectMany<T, R, RR>(this Error<T> @this
           , Func<T, Error<R>> bind, Func<T, R, RR> project)
        {
            if (@this.Exception) return new Error<RR>(@this.Ex);
            var bound = bind(@this.Value);
            return bound.Exception
               ? new Error<RR>(bound.Ex)
               : project(@this.Value, bound.Value);
        }
    }

    public struct Error<T>
    {
        internal Exception Ex { get; }
        internal T Value { get; }

        public bool Success => Ex == null;
        public bool Exception => Ex != null;

        internal Error(Exception ex)
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));
            Ex = ex;
            Value = default(T);
        }

        internal Error(T right)
        {
            Value = right;
            Ex = null;
        }

        public static implicit operator Error<T>(Exception left) => new Error<T>(left);
        public static implicit operator Error<T>(T right) => new Error<T>(right);

        public TR Match<TR>(Func<Exception, TR> Exception, Func<T, TR> Success)
           => this.Exception ? Exception(Ex) : Success(Value);

        public Unit Match(Action<Exception> Exception, Action<T> Success)
           => Match(Exception.ToFunc(), Success.ToFunc());

        public override string ToString()
           => Match(
              ex => $"Exception({ex.Message})",
              t => $"Success({t})");
    }
}